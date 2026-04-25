package handler

import (
	"bytes"
	"encoding/json"
	"mime/multipart"
	"net/http"
	"net/http/httptest"
	"testing"

	"docufiller-update-server/model"
	"docufiller-update-server/storage"
)

// setupStore creates a temp-dir-backed Store for testing.
func setupStore(t *testing.T) *storage.Store {
	t.Helper()
	dir := t.TempDir()
	s := storage.NewStore(dir)
	s.EnsureChannelDir("stable")
	s.EnsureChannelDir("beta")
	return s
}

// addVersionAssets writes a feed with given versions and creates .nupkg files.
func addVersionAssets(t *testing.T, s *storage.Store, channel string, versions []string) {
	t.Helper()
	var assets []model.ReleaseAsset
	for _, v := range versions {
		filename := "App-" + v + "-full.nupkg"
		assets = append(assets, model.ReleaseAsset{
			PackageId: "App",
			Version:   v,
			Type:      "Full",
			FileName:  filename,
			Size:      1024,
		})
		data := []byte("package-data-" + v)
		if err := s.WriteFile(channel, filename, data); err != nil {
			t.Fatalf("write test asset: %v", err)
		}
	}
	feed := &model.ReleaseFeed{Assets: assets}
	if err := s.WriteReleaseFeed(channel, feed); err != nil {
		t.Fatalf("write test feed: %v", err)
	}
}

// --- Upload Handler Tests ---

func TestUploadHandler_ValidMultipart(t *testing.T) {
	s := setupStore(t)
	h := NewUploadHandler(s)

	// Build multipart form with a .nupkg file
	var buf bytes.Buffer
	writer := multipart.NewWriter(&buf)
	part, _ := writer.CreateFormFile("file", "App-1.0.0-full.nupkg")
	part.Write([]byte("pkg-data"))
	writer.Close()

	req := httptest.NewRequest(http.MethodPost, "/api/channels/beta/releases", &buf)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}

	var resp UploadResponse
	json.NewDecoder(w.Body).Decode(&resp)
	if resp.Channel != "beta" {
		t.Errorf("expected channel=beta, got %s", resp.Channel)
	}
	if resp.FilesReceived != 1 {
		t.Errorf("expected 1 file, got %d", resp.FilesReceived)
	}

	// Verify file was written
	data, err := s.ReadFile("beta", "App-1.0.0-full.nupkg")
	if err != nil {
		t.Fatalf("file not found on disk: %v", err)
	}
	if string(data) != "pkg-data" {
		t.Errorf("file content = %q, want %q", data, "pkg-data")
	}
}

func TestUploadHandler_WithReleaseFeed(t *testing.T) {
	s := setupStore(t)
	h := NewUploadHandler(s)

	feed := model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "App", Version: "1.2.0", FileName: "App-1.2.0-full.nupkg", Size: 2048},
		},
	}
	feedData, _ := json.Marshal(feed)

	var buf bytes.Buffer
	writer := multipart.NewWriter(&buf)
	part, _ := writer.CreateFormFile("file", "releases.win.json")
	part.Write(feedData)
	writer.Close()

	req := httptest.NewRequest(http.MethodPost, "/api/channels/beta/releases", &buf)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}

	// Verify feed was written
	readFeed, err := s.ReadReleaseFeed("beta")
	if err != nil {
		t.Fatalf("ReadReleaseFeed: %v", err)
	}
	if len(readFeed.Assets) != 1 || readFeed.Assets[0].Version != "1.2.0" {
		t.Errorf("unexpected feed: %+v", readFeed.Assets)
	}
}

func TestUploadHandler_MethodNotAllowed(t *testing.T) {
	s := setupStore(t)
	h := NewUploadHandler(s)

	req := httptest.NewRequest(http.MethodGet, "/api/channels/beta/releases", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusMethodNotAllowed {
		t.Errorf("expected 405, got %d", w.Code)
	}
}

func TestUploadHandler_BadChannel(t *testing.T) {
	s := setupStore(t)
	h := NewUploadHandler(s)

	var buf bytes.Buffer
	writer := multipart.NewWriter(&buf)
	part, _ := writer.CreateFormFile("file", "test.nupkg")
	part.Write([]byte("data"))
	writer.Close()

	req := httptest.NewRequest(http.MethodPost, "/api/channels/bad!channel/releases", &buf)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for bad channel, got %d", w.Code)
	}
}

func TestUploadHandler_InvalidPath(t *testing.T) {
	s := setupStore(t)
	h := NewUploadHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/api/channels/", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for invalid path, got %d", w.Code)
	}
}

// --- Promote Handler Tests ---

func TestPromoteHandler_ValidPromote(t *testing.T) {
	s := setupStore(t)
	addVersionAssets(t, s, "beta", []string{"1.0.0"})

	h := NewPromoteHandler(s)
	req := httptest.NewRequest(http.MethodPost, "/api/channels/stable/promote?from=beta&version=1.0.0", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}

	var resp PromoteResponse
	json.NewDecoder(w.Body).Decode(&resp)
	if resp.Promoted != "1.0.0" {
		t.Errorf("expected promoted=1.0.0, got %s", resp.Promoted)
	}
	if resp.From != "beta" || resp.To != "stable" {
		t.Errorf("from=%s to=%s, want from=beta to=stable", resp.From, resp.To)
	}

	// Verify .nupkg was copied to stable
	if _, err := s.ReadFile("stable", "App-1.0.0-full.nupkg"); err != nil {
		t.Errorf("file not copied to stable: %v", err)
	}

	// Verify stable feed updated
	stableFeed, _ := s.ReadReleaseFeed("stable")
	if len(stableFeed.Assets) != 1 {
		t.Errorf("expected 1 asset in stable feed, got %d", len(stableFeed.Assets))
	}
}

func TestPromoteHandler_MissingVersion(t *testing.T) {
	s := setupStore(t)
	addVersionAssets(t, s, "beta", []string{"1.0.0"})

	h := NewPromoteHandler(s)
	req := httptest.NewRequest(http.MethodPost, "/api/channels/stable/promote?from=beta&version=9.9.9", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusNotFound {
		t.Errorf("expected 404 for missing version, got %d", w.Code)
	}
}

func TestPromoteHandler_MissingFromParam(t *testing.T) {
	s := setupStore(t)
	h := NewPromoteHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/api/channels/stable/promote?version=1.0.0", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for missing from, got %d", w.Code)
	}
}

func TestPromoteHandler_MissingVersionParam(t *testing.T) {
	s := setupStore(t)
	h := NewPromoteHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/api/channels/stable/promote?from=beta", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for missing version, got %d", w.Code)
	}
}

func TestPromoteHandler_SameChannel(t *testing.T) {
	s := setupStore(t)
	h := NewPromoteHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/api/channels/beta/promote?from=beta&version=1.0.0", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for same channel, got %d", w.Code)
	}
}

func TestPromoteHandler_InvalidTarget(t *testing.T) {
	s := setupStore(t)
	h := NewPromoteHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/api/channels/dev/promote?from=beta&version=1.0.0", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for invalid target, got %d", w.Code)
	}
}

// --- List Handler Tests ---

func TestListHandler_ReturnsCorrectStructure(t *testing.T) {
	s := setupStore(t)
	addVersionAssets(t, s, "beta", []string{"1.0.0", "1.1.0"})

	h := NewListHandler(s)
	req := httptest.NewRequest(http.MethodGet, "/api/channels/beta/releases", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}

	var resp ListResponse
	json.NewDecoder(w.Body).Decode(&resp)
	if resp.Channel != "beta" {
		t.Errorf("expected channel=beta, got %s", resp.Channel)
	}
	if resp.TotalVersions != 2 {
		t.Errorf("expected 2 versions, got %d", resp.TotalVersions)
	}
	// Should be sorted descending (1.1.0 first)
	if len(resp.Versions) > 0 && resp.Versions[0].Version != "1.1.0" {
		t.Errorf("expected first version 1.1.0, got %s", resp.Versions[0].Version)
	}
}

func TestListHandler_InvalidChannel(t *testing.T) {
	s := setupStore(t)
	h := NewListHandler(s)

	req := httptest.NewRequest(http.MethodGet, "/api/channels/dev/releases", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusNotFound {
		t.Errorf("expected 404 for invalid channel, got %d", w.Code)
	}
}

func TestListHandler_MethodNotAllowed(t *testing.T) {
	s := setupStore(t)
	h := NewListHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/api/channels/beta/releases", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusMethodNotAllowed {
		t.Errorf("expected 405, got %d", w.Code)
	}
}

// --- Static Handler Tests ---

func TestStaticHandler_ServesReleaseFeed(t *testing.T) {
	s := setupStore(t)
	feed := &model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "App", Version: "1.0.0", FileName: "App-1.0.0-full.nupkg", Size: 512},
		},
	}
	s.WriteReleaseFeed("beta", feed)

	h := NewStaticHandler(s)
	req := httptest.NewRequest(http.MethodGet, "/beta/releases.win.json", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Code)
	}
	if ct := w.Header().Get("Content-Type"); ct != "application/json" {
		t.Errorf("expected application/json, got %s", ct)
	}

	var parsed model.ReleaseFeed
	json.NewDecoder(w.Body).Decode(&parsed)
	if len(parsed.Assets) != 1 {
		t.Errorf("expected 1 asset, got %d", len(parsed.Assets))
	}
}

func TestStaticHandler_ServesNupkg(t *testing.T) {
	s := setupStore(t)
	s.WriteFile("stable", "App-1.0.0-full.nupkg", []byte("binary-data"))

	h := NewStaticHandler(s)
	req := httptest.NewRequest(http.MethodGet, "/stable/App-1.0.0-full.nupkg", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Code)
	}
	if ct := w.Header().Get("Content-Type"); ct != "application/octet-stream" {
		t.Errorf("expected application/octet-stream, got %s", ct)
	}
	if w.Body.String() != "binary-data" {
		t.Errorf("body = %q, want %q", w.Body.String(), "binary-data")
	}
}

func TestStaticHandler_FileNotFound(t *testing.T) {
	s := setupStore(t)
	h := NewStaticHandler(s)

	req := httptest.NewRequest(http.MethodGet, "/beta/nonexistent.nupkg", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusNotFound {
		t.Errorf("expected 404, got %d", w.Code)
	}
}

func TestStaticHandler_InvalidChannel(t *testing.T) {
	s := setupStore(t)
	h := NewStaticHandler(s)

	req := httptest.NewRequest(http.MethodGet, "/dev/somefile.nupkg", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusNotFound {
		t.Errorf("expected 404 for invalid channel, got %d", w.Code)
	}
}

func TestStaticHandler_PathTraversal(t *testing.T) {
	s := setupStore(t)
	h := NewStaticHandler(s)

	req := httptest.NewRequest(http.MethodGet, "/beta/../etc/passwd", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusBadRequest {
		t.Errorf("expected 400 for path traversal, got %d", w.Code)
	}
}

func TestStaticHandler_MethodNotAllowed(t *testing.T) {
	s := setupStore(t)
	h := NewStaticHandler(s)

	req := httptest.NewRequest(http.MethodPost, "/beta/releases.win.json", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusMethodNotAllowed {
		t.Errorf("expected 405, got %d", w.Code)
	}
}

// --- APIHandler (multiplexer) Tests ---

func TestAPIHandler_DispatchesToPromote(t *testing.T) {
	s := setupStore(t)
	addVersionAssets(t, s, "beta", []string{"1.0.0"})

	h := NewAPIHandler(s)
	req := httptest.NewRequest(http.MethodPost, "/api/channels/stable/promote?from=beta&version=1.0.0", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}
}

func TestAPIHandler_DispatchesToListOnGet(t *testing.T) {
	s := setupStore(t)
	addVersionAssets(t, s, "stable", []string{"1.0.0"})

	h := NewAPIHandler(s)
	req := httptest.NewRequest(http.MethodGet, "/api/channels/stable/releases", nil)
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}
}

func TestAPIHandler_DispatchesToUploadOnPost(t *testing.T) {
	s := setupStore(t)
	h := NewAPIHandler(s)

	var buf bytes.Buffer
	writer := multipart.NewWriter(&buf)
	part, _ := writer.CreateFormFile("file", "App-1.0.0-full.nupkg")
	part.Write([]byte("data"))
	writer.Close()

	req := httptest.NewRequest(http.MethodPost, "/api/channels/beta/releases", &buf)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	w := httptest.NewRecorder()

	h.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", w.Code, w.Body.String())
	}
}

func TestAPIHandler_UnknownPath(t *testing.T) {
	s := setupStore(t)
	h := NewAPIHandler(s)

	req := httptest.NewRequest(http.MethodGet, "/api/channels/beta/unknown", nil)
	w := httptest.NewRecorder()
	h.ServeHTTP(w, req)

	if w.Code != http.StatusNotFound {
		t.Errorf("expected 404, got %d", w.Code)
	}
}

// --- Path extraction helpers test ---

func TestExtractChannelFromUploadPath(t *testing.T) {
	tests := []struct {
		path    string
		want    string
		wantErr bool
	}{
		{"/api/channels/beta/releases", "beta", false},
		{"/api/channels/stable/releases", "stable", false},
		{"/api/channels//releases", "", true},
		{"/api/channels/beta", "", true},
		{"/invalid/path", "", true},
	}
	for _, tt := range tests {
		got, err := extractChannelFromUploadPath(tt.path)
		if (err != nil) != tt.wantErr {
			t.Errorf("extractChannelFromUploadPath(%q) err=%v, wantErr=%v", tt.path, err, tt.wantErr)
		}
		if got != tt.want {
			t.Errorf("extractChannelFromUploadPath(%q) = %q, want %q", tt.path, got, tt.want)
		}
	}
}

func TestExtractVersionFromNupkg(t *testing.T) {
	tests := []struct {
		filename string
		want     string
	}{
		{"App-1.2.3-full.nupkg", "1.2.3"},
		{"App-1.0.0-delta.nupkg", "1.0.0"},
		{"MyApp-2.5.0-full.nupkg", "2.5.0"},
		{"notanupkg.txt", ""},
	}
	for _, tt := range tests {
		got := extractVersionFromNupkg(tt.filename)
		if got != tt.want {
			t.Errorf("extractVersionFromNupkg(%q) = %q, want %q", tt.filename, got, tt.want)
		}
	}
}
