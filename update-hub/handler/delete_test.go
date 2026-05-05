package handler

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"

	"update-hub/model"
)

// ── Delete handler tests ──

func TestDelete_Valid(t *testing.T) {
	store := newTestStore(t)

	// Set up channel with version 1.0.0
	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
		{PackageId: "MyApp", Version: "2.0.0", FileName: "MyApp-2.0.0-full.nupkg", Size: 2048},
	}}
	store.WriteReleaseFeed("myapp", "stable", "releases.win.json", feed)
	store.WriteFile("myapp", "stable", "MyApp-1.0.0-full.nupkg", []byte("old-pkg"))
	store.WriteFile("myapp", "stable", "MyApp-2.0.0-full.nupkg", []byte("new-pkg"))

	handler := NewDeleteHandler(store, nil)
	req := httptest.NewRequest(http.MethodDelete,
		"/api/apps/myapp/channels/stable/versions/1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	req.SetPathValue("version", "1.0.0")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp DeleteResponse
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if resp.Version != "1.0.0" {
		t.Errorf("expected version 1.0.0, got %s", resp.Version)
	}
	if resp.FilesDeleted != 1 {
		t.Errorf("expected 1 file deleted, got %d", resp.FilesDeleted)
	}

	// Verify .nupkg file was removed
	dir := store.ChannelDir("myapp", "stable")
	if _, err := os.Stat(filepath.Join(dir, "MyApp-1.0.0-full.nupkg")); !os.IsNotExist(err) {
		t.Error("expected 1.0.0 nupkg to be deleted")
	}

	// Verify 2.0.0 still exists
	if _, err := os.Stat(filepath.Join(dir, "MyApp-2.0.0-full.nupkg")); err != nil {
		t.Errorf("expected 2.0.0 nupkg to still exist: %v", err)
	}

	// Verify feed was updated
	updatedFeed, _ := store.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	if len(updatedFeed.Assets) != 1 {
		t.Fatalf("expected 1 asset after delete, got %d", len(updatedFeed.Assets))
	}
	if updatedFeed.Assets[0].Version != "2.0.0" {
		t.Errorf("expected remaining version 2.0.0, got %s", updatedFeed.Assets[0].Version)
	}
}

func TestDelete_NonExistentVersion(t *testing.T) {
	store := newTestStore(t)

	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
	}}
	store.WriteReleaseFeed("myapp", "stable", "releases.win.json", feed)
	store.WriteFile("myapp", "stable", "MyApp-1.0.0-full.nupkg", []byte("data"))

	handler := NewDeleteHandler(store, nil)
	req := httptest.NewRequest(http.MethodDelete,
		"/api/apps/myapp/channels/stable/versions/9.9.9", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	req.SetPathValue("version", "9.9.9")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	// Should succeed with 0 files deleted (idempotent)
	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200 for idempotent delete, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp DeleteResponse
	json.NewDecoder(rr.Body).Decode(&resp)
	if resp.FilesDeleted != 0 {
		t.Errorf("expected 0 files deleted, got %d", resp.FilesDeleted)
	}

	// Original file should still exist
	if _, err := os.Stat(filepath.Join(store.ChannelDir("myapp", "stable"), "MyApp-1.0.0-full.nupkg")); err != nil {
		t.Errorf("expected 1.0.0 nupkg to still exist: %v", err)
	}
}

func TestDelete_FeedUpdatedAcrossMultipleOSFeeds(t *testing.T) {
	store := newTestStore(t)

	winFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-win-full.nupkg", Size: 1024},
		{PackageId: "MyApp", Version: "2.0.0", FileName: "MyApp-2.0.0-win-full.nupkg", Size: 2048},
	}}
	linuxFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-linux-full.nupkg", Size: 1536},
		{PackageId: "MyApp", Version: "2.0.0", FileName: "MyApp-2.0.0-linux-full.nupkg", Size: 2560},
	}}
	store.WriteReleaseFeed("myapp", "stable", "releases.win.json", winFeed)
	store.WriteReleaseFeed("myapp", "stable", "releases.linux.json", linuxFeed)

	handler := NewDeleteHandler(store, nil)
	req := httptest.NewRequest(http.MethodDelete,
		"/api/apps/myapp/channels/stable/versions/1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	req.SetPathValue("version", "1.0.0")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	// Both feeds should have only 2.0.0 assets
	winResult, _ := store.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	linuxResult, _ := store.ReadReleaseFeed("myapp", "stable", "releases.linux.json")

	if len(winResult.Assets) != 1 || winResult.Assets[0].Version != "2.0.0" {
		t.Errorf("win feed should have only 2.0.0: %+v", winResult.Assets)
	}
	if len(linuxResult.Assets) != 1 || linuxResult.Assets[0].Version != "2.0.0" {
		t.Errorf("linux feed should have only 2.0.0: %+v", linuxResult.Assets)
	}
}

func TestDelete_OnlyVersionLeavesEmptyFeed(t *testing.T) {
	store := newTestStore(t)

	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
	}}
	store.WriteReleaseFeed("myapp", "stable", "releases.win.json", feed)
	store.WriteFile("myapp", "stable", "MyApp-1.0.0-full.nupkg", []byte("only-pkg"))

	handler := NewDeleteHandler(store, nil)
	req := httptest.NewRequest(http.MethodDelete,
		"/api/apps/myapp/channels/stable/versions/1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	req.SetPathValue("version", "1.0.0")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	// Feed should be empty
	result, _ := store.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	if len(result.Assets) != 0 {
		t.Errorf("expected empty feed, got %d assets", len(result.Assets))
	}
}

func TestDelete_MethodNotAllowed(t *testing.T) {
	store := newTestStore(t)
	handler := NewDeleteHandler(store, nil)

	req := httptest.NewRequest(http.MethodGet,
		"/api/apps/myapp/channels/stable/versions/1.0.0", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", rr.Code)
	}
}

func TestDelete_MissingVersion(t *testing.T) {
	store := newTestStore(t)
	handler := NewDeleteHandler(store, nil)

	req := httptest.NewRequest(http.MethodDelete,
		"/api/apps/myapp/channels/stable/versions/", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	req.SetPathValue("version", "")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d: %s", rr.Code, rr.Body.String())
	}
}
