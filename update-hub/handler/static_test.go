package handler

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"

	"update-hub/model"
	"update-hub/storage"
)

// ── Static handler tests ──

func TestStatic_ServeFeed(t *testing.T) {
	store := newTestStore(t)

	// Write a feed to disk
	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
	}}
	if err := store.WriteReleaseFeed("myapp", "stable", "releases.win.json", feed); err != nil {
		t.Fatalf("write feed: %v", err)
	}

	handler := NewStaticHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/myapp/stable/releases.win.json", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	ct := rr.Header().Get("Content-Type")
	if ct != "application/json" {
		t.Errorf("expected application/json, got %s", ct)
	}

	var result model.ReleaseFeed
	if err := json.NewDecoder(rr.Body).Decode(&result); err != nil {
		t.Fatalf("decode feed: %v", err)
	}
	if len(result.Assets) != 1 || result.Assets[0].Version != "1.0.0" {
		t.Errorf("unexpected feed: %+v", result)
	}
}

func TestStatic_ServeNupkg(t *testing.T) {
	store := newTestStore(t)
	if err := store.WriteFile("myapp", "stable", "MyApp-1.0.0-full.nupkg", []byte("nupkg-bytes")); err != nil {
		t.Fatalf("write nupkg: %v", err)
	}

	handler := NewStaticHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/myapp/stable/MyApp-1.0.0-full.nupkg", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	ct := rr.Header().Get("Content-Type")
	if ct != "application/octet-stream" {
		t.Errorf("expected application/octet-stream, got %s", ct)
	}
	if rr.Body.String() != "nupkg-bytes" {
		t.Errorf("unexpected body: %s", rr.Body.String())
	}
}

func TestStatic_NotFound(t *testing.T) {
	store := newTestStore(t)
	handler := NewStaticHandler(store)

	req := httptest.NewRequest(http.MethodGet, "/myapp/stable/nonexistent.nupkg", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", rr.Code)
	}
}

func TestStatic_PathTraversal(t *testing.T) {
	store := newTestStore(t)
	// Create a file outside the channel dir
	store.WriteFile("secret", "data", "secret.txt", []byte("secret"))

	handler := NewStaticHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/myapp/stable/../../secret/data/secret.txt", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	// Should reject due to ".." in path segments
	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400 for path traversal, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestStatic_MethodNotAllowed(t *testing.T) {
	store := newTestStore(t)
	handler := NewStaticHandler(store)

	req := httptest.NewRequest(http.MethodPost, "/myapp/stable/releases.win.json", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", rr.Code)
	}
}

func TestStatic_DynamicChannel(t *testing.T) {
	store := newTestStore(t)

	// Create a custom channel name
	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "2.0.0", FileName: "MyApp-2.0.0-full.nupkg", Size: 2048},
	}}
	if err := store.WriteReleaseFeed("myapp", "custom-channel", "releases.linux.json", feed); err != nil {
		t.Fatalf("write feed: %v", err)
	}

	handler := NewStaticHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/myapp/custom-channel/releases.linux.json", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200 for dynamic channel, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestStatic_MissingPathSegments(t *testing.T) {
	store := newTestStore(t)
	handler := NewStaticHandler(store)

	// Only two segments (missing filename)
	req := httptest.NewRequest(http.MethodGet, "/myapp/stable", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404 for missing segments, got %d", rr.Code)
	}
}

// Verify static handler does NOT need storage import for the test
var _ = (*storage.Store)(nil)
