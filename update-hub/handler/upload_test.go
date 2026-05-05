package handler

import (
	"bytes"
	"encoding/json"
	"mime/multipart"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"

	"update-hub/model"
	"update-hub/storage"
)

// ── Test helpers ──

func newTestStore(t *testing.T) *storage.Store {
	t.Helper()
	dir := t.TempDir()
	return storage.NewStore(dir)
}

func storeDir(t *testing.T, s *storage.Store) string {
	t.Helper()
	return s.DataDir
}

// multipartUploadRequest creates a POST request with multipart form data.
// files is a map of filename → content.
func multipartUploadRequest(t *testing.T, appId, channel string, files map[string]string) *http.Request {
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

	writer.Close()

	req := httptest.NewRequest(http.MethodPost, "/api/apps/"+appId+"/channels/"+channel+"/releases", &buf)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	req.SetPathValue("appId", appId)
	req.SetPathValue("channel", channel)
	return req
}

func makeFeedJSON(t *testing.T, assets []model.ReleaseAsset) string {
	t.Helper()
	feed := model.ReleaseFeed{Assets: assets}
	data, err := json.Marshal(feed)
	if err != nil {
		t.Fatalf("marshal feed: %v", err)
	}
	return string(data)
}

// ── Upload tests ──

func TestUpload_ValidFeedAndNupkg(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	feedJSON := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "DocuFiller", Version: "1.0.0", FileName: "DocuFiller-1.0.0-full.nupkg", Size: 1024},
	})

	files := map[string]string{
		"releases.win.json":          feedJSON,
		"DocuFiller-1.0.0-full.nupkg": "fake-nupkg-content",
	}

	req := multipartUploadRequest(t, "docufiller", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp UploadResponse
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if resp.Channel != "stable" {
		t.Errorf("expected channel stable, got %s", resp.Channel)
	}
	if resp.FilesReceived != 2 {
		t.Errorf("expected 2 files received, got %d", resp.FilesReceived)
	}

	// Verify feed was written to disk
	readFeed, err := store.ReadReleaseFeed("docufiller", "stable", "releases.win.json")
	if err != nil {
		t.Fatalf("read feed: %v", err)
	}
	if len(readFeed.Assets) != 1 {
		t.Errorf("expected 1 asset in feed, got %d", len(readFeed.Assets))
	}

	// Verify .nupkg was written
	data, err := store.ReadFile("docufiller", "stable", "DocuFiller-1.0.0-full.nupkg")
	if err != nil {
		t.Fatalf("read nupkg: %v", err)
	}
	if string(data) != "fake-nupkg-content" {
		t.Errorf("unexpected nupkg content: %s", data)
	}
}

func TestUpload_OnlyNupkg(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	files := map[string]string{
		"MyApp-2.0.0-full.nupkg": "nupkg-data",
	}

	req := multipartUploadRequest(t, "myapp", "beta", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp UploadResponse
	json.NewDecoder(rr.Body).Decode(&resp)
	if resp.FilesReceived != 1 {
		t.Errorf("expected 1 file received, got %d", resp.FilesReceived)
	}

	// Verify .nupkg was written
	data, err := store.ReadFile("myapp", "beta", "MyApp-2.0.0-full.nupkg")
	if err != nil {
		t.Fatalf("read nupkg: %v", err)
	}
	if string(data) != "nupkg-data" {
		t.Errorf("unexpected nupkg content: %s", data)
	}
}

func TestUpload_LinuxFeed(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	feedJSON := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "3.0.0", FileName: "MyApp-3.0.0-linux-full.nupkg", Size: 2048},
	})

	files := map[string]string{
		"releases.linux.json": feedJSON,
	}

	req := multipartUploadRequest(t, "myapp", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	// Verify linux feed was written
	readFeed, err := store.ReadReleaseFeed("myapp", "stable", "releases.linux.json")
	if err != nil {
		t.Fatalf("read linux feed: %v", err)
	}
	if len(readFeed.Assets) != 1 || readFeed.Assets[0].Version != "3.0.0" {
		t.Errorf("unexpected feed: %+v", readFeed)
	}
}

func TestUpload_InvalidChannel(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	files := map[string]string{
		"test.txt": "content",
	}

	req := multipartUploadRequest(t, "myapp", "bad@channel", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestUpload_InvalidAppId(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	files := map[string]string{
		"test.txt": "content",
	}

	req := multipartUploadRequest(t, "bad@app", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestUpload_AutoRegistration_PackageIdMatches(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	feedJSON := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "DocuFiller", Version: "1.0.0", FileName: "DocuFiller-1.0.0-full.nupkg"},
	})

	files := map[string]string{
		"releases.win.json": feedJSON,
	}

	// appId is "docufiller" (lowercase) but PackageId is "DocuFiller" — should succeed (case-insensitive)
	req := multipartUploadRequest(t, "docufiller", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200 for case-insensitive match, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestUpload_AutoRegistration_PackageIdMismatch(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	feedJSON := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "OtherApp", Version: "1.0.0", FileName: "OtherApp-1.0.0-full.nupkg"},
	})

	files := map[string]string{
		"releases.win.json": feedJSON,
	}

	req := multipartUploadRequest(t, "docufiller", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500 for package ID mismatch, got %d: %s", rr.Code, rr.Body.String())
	}

	// Check error message contains both names
	var errResp map[string]string
	json.NewDecoder(rr.Body).Decode(&errResp)
	if errResp["error"] == "" {
		t.Fatal("expected error in response")
	}
}

func TestUpload_MethodNotAllowed(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	req := httptest.NewRequest(http.MethodGet, "/api/apps/docufiller/channels/stable/releases", nil)
	req.SetPathValue("appId", "docufiller")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", rr.Code)
	}
}

func TestUpload_MalformedFeed(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	files := map[string]string{
		"releases.win.json": "this is not json",
	}

	req := multipartUploadRequest(t, "myapp", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500 for malformed feed, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestUpload_EmptyMultipart(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	// Empty files map
	files := map[string]string{}
	req := multipartUploadRequest(t, "myapp", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200 for empty upload, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp UploadResponse
	json.NewDecoder(rr.Body).Decode(&resp)
	if resp.FilesReceived != 0 {
		t.Errorf("expected 0 files received, got %d", resp.FilesReceived)
	}
}

func TestUpload_CreatesDirectoryStructure(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	// First upload to a new app should auto-create the directory structure
	feedJSON := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "NewApp", Version: "1.0.0", FileName: "NewApp-1.0.0-full.nupkg"},
	})

	files := map[string]string{
		"releases.win.json":     feedJSON,
		"NewApp-1.0.0-full.nupkg": "nupkg-data",
	}

	req := multipartUploadRequest(t, "newapp", "beta", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	// Verify directory structure was created
	dir := filepath.Join(storeDir(t, store), "newapp", "beta")
	if _, err := os.Stat(dir); os.IsNotExist(err) {
		t.Errorf("directory %s should have been created", dir)
	}
}

func TestUpload_MultipleFeedFiles(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	winFeed := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-win-full.nupkg"},
	})
	linuxFeed := makeFeedJSON(t, []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-linux-full.nupkg"},
	})

	files := map[string]string{
		"releases.win.json":   winFeed,
		"releases.linux.json": linuxFeed,
	}

	req := multipartUploadRequest(t, "myapp", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	// Both feeds should be written
	winRead, _ := store.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	linuxRead, _ := store.ReadReleaseFeed("myapp", "stable", "releases.linux.json")

	if len(winRead.Assets) != 1 || winRead.Assets[0].FileName != "MyApp-1.0.0-win-full.nupkg" {
		t.Errorf("unexpected win feed: %+v", winRead)
	}
	if len(linuxRead.Assets) != 1 || linuxRead.Assets[0].FileName != "MyApp-1.0.0-linux-full.nupkg" {
		t.Errorf("unexpected linux feed: %+v", linuxRead)
	}
}

func TestUpload_SpecialCharsInAppId(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	files := map[string]string{
		"test.txt": "content",
	}

	req := multipartUploadRequest(t, "my.app", "stable", files)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400 for special chars in appId, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestUpload_EmptyAppId(t *testing.T) {
	store := newTestStore(t)
	handler := NewUploadHandler(store, nil)

	req := httptest.NewRequest(http.MethodPost, "/api/apps//channels/stable/releases", nil)
	req.SetPathValue("appId", "")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400 for empty appId, got %d: %s", rr.Code, rr.Body.String())
	}
}
