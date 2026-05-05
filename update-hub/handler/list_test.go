package handler

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"

	"update-hub/model"
)

// ── List handler tests ──

func TestList_MultipleVersions(t *testing.T) {
	store := newTestStore(t)

	// Create feed with multiple versions
	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
		{PackageId: "MyApp", Version: "2.0.0", FileName: "MyApp-2.0.0-full.nupkg", Size: 2048},
		{PackageId: "MyApp", Version: "1.5.0", FileName: "MyApp-1.5.0-full.nupkg", Size: 1536},
	}}
	if err := store.WriteReleaseFeed("myapp", "stable", "releases.win.json", feed); err != nil {
		t.Fatalf("write feed: %v", err)
	}

	handler := NewListHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/api/apps/myapp/channels/stable/releases", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp ListResponse
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	if resp.Channel != "stable" {
		t.Errorf("expected channel stable, got %s", resp.Channel)
	}
	if resp.TotalVersions != 3 {
		t.Errorf("expected 3 versions, got %d", resp.TotalVersions)
	}

	// Verify semver descending sort: 2.0.0, 1.5.0, 1.0.0
	expectedOrder := []string{"2.0.0", "1.5.0", "1.0.0"}
	for i, v := range resp.Versions {
		if v.Version != expectedOrder[i] {
			t.Errorf("version[%d]: expected %s, got %s", i, expectedOrder[i], v.Version)
		}
	}
}

func TestList_EmptyChannel(t *testing.T) {
	store := newTestStore(t)

	handler := NewListHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/api/apps/myapp/channels/stable/releases", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp ListResponse
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if resp.TotalVersions != 0 {
		t.Errorf("expected 0 versions, got %d", resp.TotalVersions)
	}
	if len(resp.Versions) != 0 {
		t.Errorf("expected empty versions, got %d", len(resp.Versions))
	}
}

func TestList_MultiOSFeeds(t *testing.T) {
	store := newTestStore(t)

	// Create both win and linux feeds with different assets for the same version
	winFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-win-full.nupkg", Size: 1024},
	}}
	linuxFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-linux-full.nupkg", Size: 2048},
	}}
	if err := store.WriteReleaseFeed("myapp", "stable", "releases.win.json", winFeed); err != nil {
		t.Fatalf("write win feed: %v", err)
	}
	if err := store.WriteReleaseFeed("myapp", "stable", "releases.linux.json", linuxFeed); err != nil {
		t.Fatalf("write linux feed: %v", err)
	}

	handler := NewListHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/api/apps/myapp/channels/stable/releases", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp ListResponse
	json.NewDecoder(rr.Body).Decode(&resp)

	// Both OS assets should be merged under version 1.0.0
	if resp.TotalVersions != 1 {
		t.Errorf("expected 1 version (merged), got %d", resp.TotalVersions)
	}
	if resp.Versions[0].FileCount != 2 {
		t.Errorf("expected 2 files, got %d", resp.Versions[0].FileCount)
	}
	if resp.Versions[0].TotalSize != 3072 {
		t.Errorf("expected total size 3072, got %d", resp.Versions[0].TotalSize)
	}
}

func TestList_SemverSort(t *testing.T) {
	store := newTestStore(t)

	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{Version: "1.10.0", FileName: "a.nupkg"},
		{Version: "1.2.0", FileName: "b.nupkg"},
		{Version: "1.9.0", FileName: "c.nupkg"},
		{Version: "2.0.1", FileName: "d.nupkg"},
		{Version: "2.0.0", FileName: "e.nupkg"},
	}}
	if err := store.WriteReleaseFeed("myapp", "stable", "releases.win.json", feed); err != nil {
		t.Fatalf("write feed: %v", err)
	}

	handler := NewListHandler(store)
	req := httptest.NewRequest(http.MethodGet, "/api/apps/myapp/channels/stable/releases", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	var resp ListResponse
	json.NewDecoder(rr.Body).Decode(&resp)

	expected := []string{"2.0.1", "2.0.0", "1.10.0", "1.9.0", "1.2.0"}
	for i, v := range resp.Versions {
		if v.Version != expected[i] {
			t.Errorf("position %d: expected %s, got %s", i, expected[i], v.Version)
		}
	}
}

func TestList_MethodNotAllowed(t *testing.T) {
	store := newTestStore(t)
	handler := NewListHandler(store)

	req := httptest.NewRequest(http.MethodPost, "/api/apps/myapp/channels/stable/releases", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", rr.Code)
	}
}

func TestList_MissingAppId(t *testing.T) {
	store := newTestStore(t)
	handler := NewListHandler(store)

	req := httptest.NewRequest(http.MethodGet, "/api/apps//channels/stable/releases", nil)
	req.SetPathValue("appId", "")
	req.SetPathValue("channel", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rr.Code)
	}
}
