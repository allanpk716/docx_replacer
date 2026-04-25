package handler

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"mime/multipart"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"

	"docufiller-update-server/middleware"
	"docufiller-update-server/model"
	"docufiller-update-server/storage"
)

// TestFullUploadWorkflow tests the complete upload → list → promote → static serve pipeline.
func TestFullUploadWorkflow(t *testing.T) {
	dir := t.TempDir()
	store := storage.NewStore(dir)
	store.EnsureChannelDir("stable")
	store.EnsureChannelDir("beta")

	apiHandler := NewAPIHandler(store)
	staticHandler := NewStaticHandler(store)

	token := "test-secret-token"
	authMiddleware := middleware.BearerAuth(token)

	// Wire up the mux manually (like main.go does)
	mux := http.NewServeMux()
	mux.Handle("/api/channels/", apiHandler)
	mux.Handle("/", staticHandler)
	server := httptest.NewServer(authMiddleware(mux))
	defer server.Close()

	client := server.Client()

	// Step 1: Upload to beta with auth
	t.Run("UploadToBeta", func(t *testing.T) {
		var buf bytes.Buffer
		writer := multipart.NewWriter(&buf)

		// Add .nupkg file
		part, _ := writer.CreateFormFile("package", "App-1.0.0-full.nupkg")
		part.Write([]byte("package-content-v1"))

		// Add releases.win.json
		feed := model.ReleaseFeed{
			Assets: []model.ReleaseAsset{
				{PackageId: "App", Version: "1.0.0", Type: "Full", FileName: "App-1.0.0-full.nupkg", Size: 16},
			},
		}
		feedData, _ := json.Marshal(feed)
		part2, _ := writer.CreateFormFile("feed", "releases.win.json")
		part2.Write(feedData)

		writer.Close()

		req, _ := http.NewRequest(http.MethodPost, server.URL+"/api/channels/beta/releases", &buf)
		req.Header.Set("Content-Type", writer.FormDataContentType())
		req.Header.Set("Authorization", "Bearer "+token)

		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("upload request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var result UploadResponse
		json.NewDecoder(resp.Body).Decode(&result)
		if result.Channel != "beta" || result.FilesReceived != 2 {
			t.Errorf("unexpected response: %+v", result)
		}
	})

	// Step 2: Verify static serving of releases.win.json (no auth needed for GET)
	t.Run("StaticServeBetaFeed", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/beta/releases.win.json")
		if err != nil {
			t.Fatalf("GET feed failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d", resp.StatusCode)
		}

		var feed model.ReleaseFeed
		json.NewDecoder(resp.Body).Decode(&feed)
		if len(feed.Assets) != 1 || feed.Assets[0].Version != "1.0.0" {
			t.Errorf("unexpected feed: %+v", feed.Assets)
		}
	})

	// Step 3: List beta versions (requires auth for API GET)
	t.Run("ListBetaVersions", func(t *testing.T) {
		req, _ := http.NewRequest(http.MethodGet, server.URL+"/api/channels/beta/releases", nil)
		req.Header.Set("Authorization", "Bearer "+token)
		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("list request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var listResp ListResponse
		json.NewDecoder(resp.Body).Decode(&listResp)
		if listResp.TotalVersions != 1 {
			t.Errorf("expected 1 version, got %d", listResp.TotalVersions)
		}
	})

	// Step 4: Promote from beta to stable
	t.Run("PromoteBetaToStable", func(t *testing.T) {
		req, _ := http.NewRequest(http.MethodPost, server.URL+"/api/channels/stable/promote?from=beta&version=1.0.0", nil)
		req.Header.Set("Authorization", "Bearer "+token)
		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("promote request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var promResp PromoteResponse
		json.NewDecoder(resp.Body).Decode(&promResp)
		if promResp.From != "beta" || promResp.To != "stable" || promResp.Promoted != "1.0.0" {
			t.Errorf("unexpected promote response: %+v", promResp)
		}
	})

	// Step 5: Verify stable now has the version
	t.Run("StableHasPromotedVersion", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/stable/releases.win.json")
		if err != nil {
			t.Fatalf("GET stable feed failed: %v", err)
		}
		defer resp.Body.Close()

		var feed model.ReleaseFeed
		json.NewDecoder(resp.Body).Decode(&feed)
		if len(feed.Assets) != 1 || feed.Assets[0].Version != "1.0.0" {
			t.Errorf("stable feed should have 1.0.0: %+v", feed.Assets)
		}
	})

	// Step 6: Auth rejection - no token
	t.Run("AuthRejection_NoToken", func(t *testing.T) {
		var buf bytes.Buffer
		writer := multipart.NewWriter(&buf)
		part, _ := writer.CreateFormFile("file", "test.nupkg")
		part.Write([]byte("data"))
		writer.Close()

		req, _ := http.NewRequest(http.MethodPost, server.URL+"/api/channels/beta/releases", &buf)
		req.Header.Set("Content-Type", writer.FormDataContentType())
		// No Authorization header

		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusUnauthorized {
			t.Errorf("expected 401 without token, got %d", resp.StatusCode)
		}
	})

	// Step 7: Auth rejection - bad token
	t.Run("AuthRejection_BadToken", func(t *testing.T) {
		var buf bytes.Buffer
		writer := multipart.NewWriter(&buf)
		part, _ := writer.CreateFormFile("file", "test.nupkg")
		part.Write([]byte("data"))
		writer.Close()

		req, _ := http.NewRequest(http.MethodPost, server.URL+"/api/channels/beta/releases", &buf)
		req.Header.Set("Content-Type", writer.FormDataContentType())
		req.Header.Set("Authorization", "Bearer wrong-token")

		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusUnauthorized {
			t.Errorf("expected 401 with wrong token, got %d", resp.StatusCode)
		}
	})

	// Step 8: Verify .nupkg file was physically copied
	t.Run("PhysicalFileExists", func(t *testing.T) {
		stablePkgPath := filepath.Join(dir, "stable", "App-1.0.0-full.nupkg")
		data, err := os.ReadFile(stablePkgPath)
		if err != nil {
			t.Fatalf("stable .nupkg not found: %v", err)
		}
		if string(data) != "package-content-v1" {
			t.Errorf("stable .nupkg content = %q, want %q", data, "package-content-v1")
		}
	})
}

// TestUploadAndCleanup tests uploading 11 versions triggers auto-cleanup.
func TestUploadAndCleanup(t *testing.T) {
	dir := t.TempDir()
	store := storage.NewStore(dir)
	store.EnsureChannelDir("beta")

	h := NewUploadHandler(store)

	// Upload 11 versions
	for i := 0; i < 11; i++ {
		version := fmt.Sprintf("1.0.%d", i)
		filename := "App-" + version + "-full.nupkg"

		var buf bytes.Buffer
		writer := multipart.NewWriter(&buf)

		// Upload the .nupkg
		part, _ := writer.CreateFormFile("file", filename)
		part.Write([]byte("data-" + version))

		// Upload corresponding feed entry
		feed := model.ReleaseFeed{
			Assets: []model.ReleaseAsset{
				{PackageId: "App", Version: version, Type: "Full", FileName: filename, Size: 10},
			},
		}
		feedData, _ := json.Marshal(feed)
		part2, _ := writer.CreateFormFile("feed", "releases.win.json")
		part2.Write(feedData)

		writer.Close()

		req := httptest.NewRequest(http.MethodPost, "/api/channels/beta/releases", &buf)
		req.Header.Set("Content-Type", writer.FormDataContentType())
		w := httptest.NewRecorder()
		h.ServeHTTP(w, req)

		if w.Code != http.StatusOK {
			t.Fatalf("upload %s failed: %d %s", version, w.Code, w.Body.String())
		}
	}

	// After 11 uploads, only 10 should remain (oldest removed)
	feed, err := store.ReadReleaseFeed("beta")
	if err != nil {
		t.Fatalf("ReadReleaseFeed: %v", err)
	}

	versions := map[string]bool{}
	for _, a := range feed.Assets {
		versions[a.Version] = true
	}

	// Version 1.0.0 should have been cleaned up (it's the oldest)
	if versions["1.0.0"] {
		t.Error("version 1.0.0 should have been removed by auto-cleanup")
	}

	// All others should remain
	if len(feed.Assets) > 10 {
		t.Errorf("expected at most 10 assets after cleanup, got %d", len(feed.Assets))
	}

	// Verify the .nupkg for 1.0.0 was also deleted from disk
	if _, err := store.ReadFile("beta", "App-1.0.0-full.nupkg"); err == nil {
		t.Error("1.0.0 .nupkg should have been deleted from disk")
	}
}
