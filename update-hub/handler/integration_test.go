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

	"update-hub/database"
	"update-hub/middleware"
	"update-hub/model"
	"update-hub/storage"
)

// ── Integration test helpers ──

// setupIntegrationServer creates a test server with all handlers wired using
// Go 1.22 ServeMux patterns, auth middleware, and a temp directory for storage.
func setupIntegrationServer(t *testing.T, token string) (*httptest.Server, *storage.Store, *database.DB) {
	t.Helper()
	dir := t.TempDir()
	store := storage.NewStore(dir)

	// Initialize SQLite metadata database
	dbPath := filepath.Join(dir, "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init database: %v", err)
	}
	t.Cleanup(func() { db.Close() })

	// Create handlers
	uploadHandler := NewUploadHandler(store, db)
	listHandler := NewListHandler(store)
	promoteHandler := NewPromoteHandler(store, db)
	deleteHandler := NewDeleteHandler(store, db)
	staticHandler := NewStaticHandler(store)
	appListHandler := NewAppListHandler(db)
	versionListHandler := NewVersionListHandler(db)

	// Wire Go 1.22 ServeMux patterns (same as main.go)
	mux := http.NewServeMux()
	mux.HandleFunc("POST /api/apps/{appId}/channels/{channel}/releases", uploadHandler.ServeHTTP)
	mux.HandleFunc("GET /api/apps/{appId}/channels/{channel}/releases", listHandler.ServeHTTP)
	mux.HandleFunc("POST /api/apps/{appId}/channels/{target}/promote", promoteHandler.ServeHTTP)
	mux.HandleFunc("DELETE /api/apps/{appId}/channels/{channel}/versions/{version}", deleteHandler.ServeHTTP)
	mux.HandleFunc("GET /api/apps", appListHandler.ServeHTTP)
	mux.HandleFunc("GET /api/apps/{appId}/channels/{channel}/versions", versionListHandler.ServeHTTP)
	mux.Handle("/{appId}/", staticHandler)

	// Apply auth middleware
	authed := middleware.BearerAuth(token, nil)(mux)

	server := httptest.NewServer(authed)
	return server, store, db
}

// uploadMultipart performs a multipart POST to the upload endpoint.
// files maps filename → content. The multipart field name is always "file".
func uploadMultipart(t *testing.T, serverURL, token, appId, channel string, files map[string]string) (*http.Response, map[string]interface{}) {
	t.Helper()

	return uploadMultipartWithNotes(t, serverURL, token, appId, channel, files, "")
}

// uploadMultipartWithNotes performs a multipart POST with an optional notes field.
func uploadMultipartWithNotes(t *testing.T, serverURL, token, appId, channel string, files map[string]string, notes string) (*http.Response, map[string]interface{}) {
	t.Helper()
	var buf bytes.Buffer
	writer := multipart.NewWriter(&buf)

	for filename, content := range files {
		part, err := writer.CreateFormFile("file", filename)
		if err != nil {
			t.Fatalf("create form file %s: %v", filename, err)
		}
		if _, err := part.Write([]byte(content)); err != nil {
			t.Fatalf("write form file %s: %v", filename, err)
		}
	}

	// Add optional notes field
	if notes != "" {
		if err := writer.WriteField("notes", notes); err != nil {
			t.Fatalf("write notes field: %v", err)
		}
	}

	writer.Close()

	url := fmt.Sprintf("%s/api/apps/%s/channels/%s/releases", serverURL, appId, channel)
	req, err := http.NewRequest(http.MethodPost, url, &buf)
	if err != nil {
		t.Fatalf("create request: %v", err)
	}
	req.Header.Set("Content-Type", writer.FormDataContentType())
	if token != "" {
		req.Header.Set("Authorization", "Bearer "+token)
	}

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		t.Fatalf("request failed: %v", err)
	}

	// Read and decode body
	bodyBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		t.Fatalf("read response body: %v", err)
	}
	resp.Body.Close()

	var body map[string]interface{}
	json.Unmarshal(bodyBytes, &body)

	// Re-wrap body so callers can still read it
	resp.Body = io.NopCloser(bytes.NewReader(bodyBytes))

	return resp, body
}

// makeFeed creates a Velopack-compatible feed JSON string.
func makeFeed(t *testing.T, assets []model.ReleaseAsset) string {
	t.Helper()
	feed := model.ReleaseFeed{Assets: assets}
	data, err := json.Marshal(feed)
	if err != nil {
		t.Fatalf("marshal feed: %v", err)
	}
	return string(data)
}

// ── End-to-end integration test ──

// TestFullMultiAppWorkflow proves the complete Velopack-compatible workflow:
// upload → feed serve → .nupkg serve → list → promote → delete → auth rejection.
// It also validates multi-app isolation and multi-OS feed support.
func TestFullMultiAppWorkflow(t *testing.T) {
	token := "test-secret"
	server, store, _ := setupIntegrationServer(t, token)
	defer server.Close()

	client := server.Client()

	// ── Upload to DocuFiller/beta ──
	t.Run("UploadToDocuFillerBeta", func(t *testing.T) {
		feedJSON := makeFeed(t, []model.ReleaseAsset{
			{PackageId: "DocuFiller", Version: "1.2.0", Type: "Full", FileName: "DocuFiller-1.2.0-full.nupkg", Size: 1024},
		})
		files := map[string]string{
			"releases.win.json":            feedJSON,
			"DocuFiller-1.2.0-full.nupkg": "fake-nupkg-v1.2.0",
		}

		resp, body := uploadMultipart(t, server.URL, token, "docufiller", "beta", files)
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d: %v", resp.StatusCode, body)
		}
		if body["channel"] != "beta" {
			t.Errorf("expected channel=beta, got %v", body["channel"])
		}
		if int(body["files_received"].(float64)) != 2 {
			t.Errorf("expected 2 files received, got %v", body["files_received"])
		}
	})

	// ── Upload to go-tool/stable (second app, different OS) ──
	t.Run("UploadToGoAppStable", func(t *testing.T) {
		feedJSON := makeFeed(t, []model.ReleaseAsset{
			{PackageId: "go-tool", Version: "0.5.0", Type: "Full", FileName: "go-tool-0.5.0-full.nupkg", Size: 2048},
		})
		files := map[string]string{
			"releases.linux.json":       feedJSON,
			"go-tool-0.5.0-full.nupkg": "linux-binary-data",
		}

		resp, body := uploadMultipart(t, server.URL, token, "go-tool", "stable", files)
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d: %v", resp.StatusCode, body)
		}
		if body["channel"] != "stable" {
			t.Errorf("expected channel=stable, got %v", body["channel"])
		}
	})

	// ── Serve DocuFiller feed via static path (no auth needed) ──
	t.Run("ServeDocuFillerFeed", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/docufiller/beta/releases.win.json")
		if err != nil {
			t.Fatalf("GET feed failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var feed model.ReleaseFeed
		if err := json.NewDecoder(resp.Body).Decode(&feed); err != nil {
			t.Fatalf("decode feed: %v", err)
		}
		if len(feed.Assets) != 1 {
			t.Fatalf("expected 1 asset, got %d", len(feed.Assets))
		}
		if feed.Assets[0].Version != "1.2.0" {
			t.Errorf("expected version 1.2.0, got %s", feed.Assets[0].Version)
		}
		if feed.Assets[0].PackageId != "DocuFiller" {
			t.Errorf("expected PackageId DocuFiller, got %s", feed.Assets[0].PackageId)
		}
	})

	// ── Serve .nupkg via static path (no auth needed) ──
	t.Run("ServeDocuFillerNupkg", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/docufiller/beta/DocuFiller-1.2.0-full.nupkg")
		if err != nil {
			t.Fatalf("GET nupkg failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d", resp.StatusCode)
		}

		data, err := io.ReadAll(resp.Body)
		if err != nil {
			t.Fatalf("read body: %v", err)
		}
		if string(data) != "fake-nupkg-v1.2.0" {
			t.Errorf("unexpected nupkg content: %s", data)
		}

		// Verify content type
		ct := resp.Header.Get("Content-Type")
		if ct != "application/octet-stream" {
			t.Errorf("expected application/octet-stream, got %s", ct)
		}
	})

	// ── Serve go-tool Linux feed (multi-OS proof) ──
	t.Run("ServeGoAppFeed", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/go-tool/stable/releases.linux.json")
		if err != nil {
			t.Fatalf("GET feed failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d", resp.StatusCode)
		}

		var feed model.ReleaseFeed
		if err := json.NewDecoder(resp.Body).Decode(&feed); err != nil {
			t.Fatalf("decode feed: %v", err)
		}
		if len(feed.Assets) != 1 {
			t.Fatalf("expected 1 asset, got %d", len(feed.Assets))
		}
		if feed.Assets[0].Version != "0.5.0" {
			t.Errorf("expected version 0.5.0, got %s", feed.Assets[0].Version)
		}
	})

	// ── List DocuFiller/beta versions (public GET) ──
	t.Run("ListDocuFillerVersions", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/api/apps/docufiller/channels/beta/releases")
		if err != nil {
			t.Fatalf("list request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var listResp ListResponse
		if err := json.NewDecoder(resp.Body).Decode(&listResp); err != nil {
			t.Fatalf("decode response: %v", err)
		}
		if listResp.Channel != "beta" {
			t.Errorf("expected channel=beta, got %s", listResp.Channel)
		}
		if listResp.TotalVersions != 1 {
			t.Errorf("expected 1 version, got %d", listResp.TotalVersions)
		}
		if len(listResp.Versions) > 0 && listResp.Versions[0].Version != "1.2.0" {
			t.Errorf("expected version 1.2.0, got %s", listResp.Versions[0].Version)
		}
	})

	// ── Promote DocuFiller 1.2.0 from beta → stable ──
	t.Run("PromoteToStable", func(t *testing.T) {
		url := fmt.Sprintf("%s/api/apps/docufiller/channels/stable/promote?from=beta&version=1.2.0", server.URL)
		req, err := http.NewRequest(http.MethodPost, url, nil)
		if err != nil {
			t.Fatalf("create request: %v", err)
		}
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
		if err := json.NewDecoder(resp.Body).Decode(&promResp); err != nil {
			t.Fatalf("decode response: %v", err)
		}
		if promResp.Promoted != "1.2.0" {
			t.Errorf("expected promoted=1.2.0, got %s", promResp.Promoted)
		}
		if promResp.From != "beta" || promResp.To != "stable" {
			t.Errorf("expected from=beta to=stable, got from=%s to=%s", promResp.From, promResp.To)
		}
		if promResp.FilesCopied < 1 {
			t.Errorf("expected at least 1 file copied, got %d", promResp.FilesCopied)
		}

		// Verify files exist in stable on disk
		stableDir := filepath.Join(store.DataDir, "docufiller", "stable")
		feedPath := filepath.Join(stableDir, "releases.win.json")
		if _, err := os.Stat(feedPath); os.IsNotExist(err) {
			t.Error("stable releases.win.json should exist after promote")
		}
		nupkgPath := filepath.Join(stableDir, "DocuFiller-1.2.0-full.nupkg")
		if _, err := os.Stat(nupkgPath); os.IsNotExist(err) {
			t.Error("stable .nupkg should exist after promote")
		}
	})

	// ── Verify stable serves promoted version ──
	t.Run("StableServesPromotedVersion", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/docufiller/stable/releases.win.json")
		if err != nil {
			t.Fatalf("GET stable feed failed: %v", err)
		}
		defer resp.Body.Close()

		var feed model.ReleaseFeed
		json.NewDecoder(resp.Body).Decode(&feed)
		if len(feed.Assets) == 0 || feed.Assets[0].Version != "1.2.0" {
			t.Errorf("stable feed should have version 1.2.0, got: %+v", feed.Assets)
		}
	})

	// ── Delete from beta ──
	t.Run("DeleteFromBeta", func(t *testing.T) {
		url := fmt.Sprintf("%s/api/apps/docufiller/channels/beta/versions/1.2.0", server.URL)
		req, err := http.NewRequest(http.MethodDelete, url, nil)
		if err != nil {
			t.Fatalf("create request: %v", err)
		}
		req.Header.Set("Authorization", "Bearer "+token)

		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("delete request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var delResp DeleteResponse
		json.NewDecoder(resp.Body).Decode(&delResp)
		if delResp.Version != "1.2.0" {
			t.Errorf("expected version=1.2.0, got %s", delResp.Version)
		}

		// Verify .nupkg removed from disk
		nupkgPath := filepath.Join(store.DataDir, "docufiller", "beta", "DocuFiller-1.2.0-full.nupkg")
		if _, err := os.Stat(nupkgPath); !os.IsNotExist(err) {
			t.Error("beta .nupkg should be deleted from disk")
		}

		// Verify feed updated
		feed, _ := store.ReadReleaseFeed("docufiller", "beta", "releases.win.json")
		for _, a := range feed.Assets {
			if a.Version == "1.2.0" {
				t.Error("version 1.2.0 should be removed from beta feed")
			}
		}
	})

	// ── Auth rejection: POST without token ──
	t.Run("AuthRejection", func(t *testing.T) {
		files := map[string]string{
			"test.nupkg": "data",
		}
		resp, _ := uploadMultipart(t, server.URL, "", "docufiller", "stable", files)
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusUnauthorized {
			t.Errorf("expected 401 without token, got %d", resp.StatusCode)
		}
	})

	// ── Auth rejection: POST with wrong token ──
	t.Run("AuthRejection_BadToken", func(t *testing.T) {
		files := map[string]string{
			"test.nupkg": "data",
		}
		resp, _ := uploadMultipart(t, server.URL, "wrong-token", "docufiller", "stable", files)
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusUnauthorized {
			t.Errorf("expected 401 with wrong token, got %d", resp.StatusCode)
		}
	})

	// ── Multi-app isolation: docufiller/beta doesn't contain go-tool files ──
	t.Run("MultiAppIsolation", func(t *testing.T) {
		// docufiller/beta feed should not contain go-tool assets
		feed, err := store.ReadReleaseFeed("docufiller", "beta", "releases.win.json")
		if err != nil {
			t.Fatalf("read docufiller feed: %v", err)
		}
		for _, a := range feed.Assets {
			if a.PackageId == "go-tool" {
				t.Error("docufiller feed should not contain go-tool assets")
			}
		}

		// go-tool files should not be in docufiller directory
		goToolDir := filepath.Join(store.DataDir, "go-tool", "stable")
		docuDir := filepath.Join(store.DataDir, "docufiller")
		entries, _ := os.ReadDir(docuDir)
		for _, e := range entries {
			if e.Name() == "go-tool" {
				t.Error("docufiller directory should not contain go-tool subdirectory")
			}
		}

		// Verify go-tool dir exists independently
		if _, err := os.Stat(goToolDir); os.IsNotExist(err) {
			t.Error("go-tool/stable directory should exist")
		}

		_ = entries // suppress unused warning
	})

	// ── Dynamic channel: upload to non-standard channel name ──
	t.Run("DynamicChannel", func(t *testing.T) {
		feedJSON := makeFeed(t, []model.ReleaseAsset{
			{PackageId: "DocuFiller", Version: "2.0.0-beta1", Type: "Full", FileName: "DocuFiller-2.0.0-beta1-full.nupkg", Size: 2048},
		})
		files := map[string]string{
			"releases.win.json":                    feedJSON,
			"DocuFiller-2.0.0-beta1-full.nupkg": "nightly-build-data",
		}

		resp, body := uploadMultipart(t, server.URL, token, "docufiller", "nightly", files)
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200 for dynamic channel, got %d: %v", resp.StatusCode, body)
		}

		// Verify feed served from nightly channel
		feedResp, err := client.Get(server.URL + "/docufiller/nightly/releases.win.json")
		if err != nil {
			t.Fatalf("GET nightly feed failed: %v", err)
		}
		defer feedResp.Body.Close()

		if feedResp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200 for nightly feed, got %d", feedResp.StatusCode)
		}

		var feed model.ReleaseFeed
		json.NewDecoder(feedResp.Body).Decode(&feed)
		if len(feed.Assets) != 1 || feed.Assets[0].Version != "2.0.0-beta1" {
			t.Errorf("nightly feed should have 2.0.0-beta1, got: %+v", feed.Assets)
		}

		// Verify directory structure was auto-created
		nightlyDir := filepath.Join(store.DataDir, "docufiller", "nightly")
		if _, err := os.Stat(nightlyDir); os.IsNotExist(err) {
			t.Error("nightly directory should have been auto-created")
		}
	})

	// ── 404 for non-existent file ──
	t.Run("NotFoundForMissingFile", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/docufiller/beta/nonexistent-file.nupkg")
		if err != nil {
			t.Fatalf("GET failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusNotFound {
			t.Errorf("expected 404 for missing file, got %d", resp.StatusCode)
		}
	})

	// ── 404 for non-existent app ──
	t.Run("NotFoundForMissingApp", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/nonexistent-app/stable/releases.win.json")
		if err != nil {
			t.Fatalf("GET failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusNotFound {
			t.Errorf("expected 404 for missing app, got %d", resp.StatusCode)
		}
	})
}

// TestAuthDisabled proves that when token is empty, all operations work without auth.
func TestAuthDisabled(t *testing.T) {
	server, _, _ := setupIntegrationServer(t, "") // no token
	defer server.Close()

	// Upload without auth should succeed when token is empty
	feedJSON := makeFeed(t, []model.ReleaseAsset{
		{PackageId: "TestApp", Version: "1.0.0", FileName: "TestApp-1.0.0-full.nupkg", Size: 100},
	})
	files := map[string]string{
		"releases.win.json":        feedJSON,
		"TestApp-1.0.0-full.nupkg": "data",
	}

	resp, _ := uploadMultipart(t, server.URL, "", "testapp", "stable", files)
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		t.Fatalf("expected 200 with auth disabled, got %d", resp.StatusCode)
	}
}

// ── Metadata-specific integration tests ──

// TestMetadataFlow proves end-to-end metadata persistence through the API lifecycle:
// upload with notes → query apps → query versions → promote sync → delete cleanup.
func TestMetadataFlow(t *testing.T) {
	token := "test-secret"
	server, _, _ := setupIntegrationServer(t, token)
	defer server.Close()

	client := server.Client()

	// ── Upload with notes ──
	t.Run("UploadWithNotes", func(t *testing.T) {
		feedJSON := makeFeed(t, []model.ReleaseAsset{
			{PackageId: "NoteApp", Version: "3.0.0", Type: "Full", FileName: "NoteApp-3.0.0-full.nupkg", Size: 4096},
		})
		files := map[string]string{
			"releases.win.json":        feedJSON,
			"NoteApp-3.0.0-full.nupkg": "binary-data",
		}

		resp, body := uploadMultipartWithNotes(t, server.URL, token, "noteapp", "alpha", files, "Initial alpha release with cool features")
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d: %v", resp.StatusCode, body)
		}
		if body["channel"] != "alpha" {
			t.Errorf("expected channel=alpha, got %v", body["channel"])
		}
	})

	// ── List apps via metadata endpoint ──
	t.Run("ListApps", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/api/apps")
		if err != nil {
			t.Fatalf("GET /api/apps failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var apps []database.AppInfo
		if err := json.NewDecoder(resp.Body).Decode(&apps); err != nil {
			t.Fatalf("decode apps: %v", err)
		}

		if len(apps) == 0 {
			t.Fatal("expected at least 1 app, got empty list")
		}

		// Find noteapp
		var found *database.AppInfo
		for i := range apps {
			if apps[i].ID == "noteapp" {
				found = &apps[i]
				break
			}
		}
		if found == nil {
			t.Fatalf("noteapp not found in apps list: %+v", apps)
		}

		hasAlpha := false
		for _, ch := range found.Channels {
			if ch == "alpha" {
				hasAlpha = true
				break
			}
		}
		if !hasAlpha {
			t.Errorf("noteapp should have alpha channel, got: %v", found.Channels)
		}
	})

	// ── List versions with notes ──
	t.Run("ListVersionsWithNotes", func(t *testing.T) {
		resp, err := client.Get(server.URL + "/api/apps/noteapp/channels/alpha/versions")
		if err != nil {
			t.Fatalf("GET versions failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		var versions []database.VersionEntry
		if err := json.NewDecoder(resp.Body).Decode(&versions); err != nil {
			t.Fatalf("decode versions: %v", err)
		}

		if len(versions) != 1 {
			t.Fatalf("expected 1 version, got %d", len(versions))
		}

		v := versions[0]
		if v.Version != "3.0.0" {
			t.Errorf("expected version 3.0.0, got %s", v.Version)
		}
		if v.Notes != "Initial alpha release with cool features" {
			t.Errorf("expected notes to be preserved, got: %s", v.Notes)
		}
		if v.AppID != "noteapp" {
			t.Errorf("expected app_id=noteapp, got %s", v.AppID)
		}
		if v.Channel != "alpha" {
			t.Errorf("expected channel=alpha, got %s", v.Channel)
		}
	})

	// ── Promote with metadata sync ──
	t.Run("PromoteMetadataSync", func(t *testing.T) {
		url := fmt.Sprintf("%s/api/apps/noteapp/channels/stable/promote?from=alpha&version=3.0.0", server.URL)
		req, err := http.NewRequest(http.MethodPost, url, nil)
		if err != nil {
			t.Fatalf("create request: %v", err)
		}
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

		// Verify version appears in stable channel via metadata endpoint
		versionsResp, err := client.Get(server.URL + "/api/apps/noteapp/channels/stable/versions")
		if err != nil {
			t.Fatalf("GET stable versions failed: %v", err)
		}
		defer versionsResp.Body.Close()

		var versions []database.VersionEntry
		if err := json.NewDecoder(versionsResp.Body).Decode(&versions); err != nil {
			t.Fatalf("decode versions: %v", err)
		}

		if len(versions) != 1 {
			t.Fatalf("expected 1 version in stable, got %d", len(versions))
		}

		if versions[0].Version != "3.0.0" {
			t.Errorf("expected version 3.0.0, got %s", versions[0].Version)
		}
		// Notes should be carried over from alpha
		if versions[0].Notes != "Initial alpha release with cool features" {
			t.Errorf("expected notes to be carried over, got: %s", versions[0].Notes)
		}
	})

	// ── Delete with metadata cleanup ──
	t.Run("DeleteMetadataCleanup", func(t *testing.T) {
		url := fmt.Sprintf("%s/api/apps/noteapp/channels/alpha/versions/3.0.0", server.URL)
		req, err := http.NewRequest(http.MethodDelete, url, nil)
		if err != nil {
			t.Fatalf("create request: %v", err)
		}
		req.Header.Set("Authorization", "Bearer "+token)

		resp, err := client.Do(req)
		if err != nil {
			t.Fatalf("delete request failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			body, _ := io.ReadAll(resp.Body)
			t.Fatalf("expected 200, got %d: %s", resp.StatusCode, body)
		}

		// Verify version no longer appears in alpha channel
		versionsResp, err := client.Get(server.URL + "/api/apps/noteapp/channels/alpha/versions")
		if err != nil {
			t.Fatalf("GET alpha versions failed: %v", err)
		}
		defer versionsResp.Body.Close()

		var versions []database.VersionEntry
		if err := json.NewDecoder(versionsResp.Body).Decode(&versions); err != nil {
			t.Fatalf("decode versions: %v", err)
		}

		if len(versions) != 0 {
			t.Errorf("expected 0 versions in alpha after delete, got %d", len(versions))
		}

		// Stable should still have the version
		stableResp, err := client.Get(server.URL + "/api/apps/noteapp/channels/stable/versions")
		if err != nil {
			t.Fatalf("GET stable versions failed: %v", err)
		}
		defer stableResp.Body.Close()

		var stableVersions []database.VersionEntry
		if err := json.NewDecoder(stableResp.Body).Decode(&stableVersions); err != nil {
			t.Fatalf("decode versions: %v", err)
		}

		if len(stableVersions) != 1 {
			t.Errorf("expected 1 version in stable after alpha delete, got %d", len(stableVersions))
		}
	})
}

// TestMetadataEndpoints_NilDB proves the metadata endpoints return empty arrays when DB is nil.
func TestMetadataEndpoints_NilDB(t *testing.T) {
	mux := http.NewServeMux()
	appListHandler := NewAppListHandler(nil)
	versionListHandler := NewVersionListHandler(nil)
	mux.HandleFunc("GET /api/apps", appListHandler.ServeHTTP)
	mux.HandleFunc("GET /api/apps/{appId}/channels/{channel}/versions", versionListHandler.ServeHTTP)

	server := httptest.NewServer(mux)
	defer server.Close()

	t.Run("AppListNilDB", func(t *testing.T) {
		resp, err := http.Get(server.URL + "/api/apps")
		if err != nil {
			t.Fatalf("GET /api/apps failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d", resp.StatusCode)
		}

		body, _ := io.ReadAll(resp.Body)
		if string(body) != "[]\n" && string(body) != "[]" {
			t.Errorf("expected empty JSON array, got: %s", body)
		}
	})

	t.Run("VersionListNilDB", func(t *testing.T) {
		resp, err := http.Get(server.URL + "/api/apps/test/channels/stable/versions")
		if err != nil {
			t.Fatalf("GET versions failed: %v", err)
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			t.Fatalf("expected 200, got %d", resp.StatusCode)
		}

		body, _ := io.ReadAll(resp.Body)
		if string(body) != "[]\n" && string(body) != "[]" {
			t.Errorf("expected empty JSON array, got: %s", body)
		}
	})
}

// TestMetadataVersionList_EmptyChannel proves version list returns empty array for unknown app/channel.
func TestMetadataVersionList_EmptyChannel(t *testing.T) {
	server, _, _ := setupIntegrationServer(t, "")
	defer server.Close()

	resp, err := http.Get(server.URL + "/api/apps/nonexistent/channels/ghost/versions")
	if err != nil {
		t.Fatalf("GET versions failed: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		t.Fatalf("expected 200, got %d", resp.StatusCode)
	}

	var versions []database.VersionEntry
	if err := json.NewDecoder(resp.Body).Decode(&versions); err != nil {
		t.Fatalf("decode: %v", err)
	}
	if len(versions) != 0 {
		t.Errorf("expected empty array for unknown app/channel, got %d", len(versions))
	}
}
