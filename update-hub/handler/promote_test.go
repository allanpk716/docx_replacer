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

// ── Promote handler tests ──

func TestPromote_Valid(t *testing.T) {
	store := newTestStore(t)

	// Set up source channel (beta) with a version
	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024, Type: "Full"},
	}}
	if err := store.WriteReleaseFeed("myapp", "beta", "releases.win.json", feed); err != nil {
		t.Fatalf("write source feed: %v", err)
	}
	// Write the actual .nupkg file
	if err := store.WriteFile("myapp", "beta", "MyApp-1.0.0-full.nupkg", []byte("nupkg-data")); err != nil {
		t.Fatalf("write nupkg: %v", err)
	}

	handler := NewPromoteHandler(store, nil)
	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=beta&version=1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	var resp PromoteResponse
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if resp.Promoted != "1.0.0" {
		t.Errorf("expected promoted 1.0.0, got %s", resp.Promoted)
	}
	if resp.From != "beta" {
		t.Errorf("expected from beta, got %s", resp.From)
	}
	if resp.To != "stable" {
		t.Errorf("expected to stable, got %s", resp.To)
	}
	if resp.FilesCopied != 1 {
		t.Errorf("expected 1 file copied, got %d", resp.FilesCopied)
	}

	// Verify .nupkg was physically copied to target
	data, err := store.ReadFile("myapp", "stable", "MyApp-1.0.0-full.nupkg")
	if err != nil {
		t.Fatalf("read promoted nupkg: %v", err)
	}
	if string(data) != "nupkg-data" {
		t.Errorf("unexpected nupkg content: %s", data)
	}

	// Verify target feed has the promoted asset
	targetFeed, err := store.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	if err != nil {
		t.Fatalf("read target feed: %v", err)
	}
	if len(targetFeed.Assets) != 1 || targetFeed.Assets[0].Version != "1.0.0" {
		t.Errorf("unexpected target feed: %+v", targetFeed)
	}
}

func TestPromote_VersionNotFound(t *testing.T) {
	store := newTestStore(t)

	feed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
	}}
	store.WriteReleaseFeed("myapp", "beta", "releases.win.json", feed)

	handler := NewPromoteHandler(store, nil)
	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=beta&version=9.9.9", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestPromote_MissingFromParam(t *testing.T) {
	store := newTestStore(t)
	handler := NewPromoteHandler(store, nil)

	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?version=1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestPromote_MissingVersionParam(t *testing.T) {
	store := newTestStore(t)
	handler := NewPromoteHandler(store, nil)

	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=beta", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestPromote_SameChannel(t *testing.T) {
	store := newTestStore(t)
	handler := NewPromoteHandler(store, nil)

	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=stable&version=1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400 for same channel, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestPromote_NoFeedsInSource(t *testing.T) {
	store := newTestStore(t)
	handler := NewPromoteHandler(store, nil)

	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=beta&version=1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404 for no source feeds, got %d: %s", rr.Code, rr.Body.String())
	}
}

func TestPromote_DedupInTarget(t *testing.T) {
	store := newTestStore(t)

	// Source has version 1.0.0
	srcFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
	}}
	store.WriteReleaseFeed("myapp", "beta", "releases.win.json", srcFeed)
	store.WriteFile("myapp", "beta", "MyApp-1.0.0-full.nupkg", []byte("data"))

	// Target already has the same version
	targetFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
	}}
	store.WriteReleaseFeed("myapp", "stable", "releases.win.json", targetFeed)

	handler := NewPromoteHandler(store, nil)
	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=beta&version=1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d: %s", rr.Code, rr.Body.String())
	}

	// Target feed should have the asset only once (dedup)
	resultFeed, _ := store.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	count := 0
	for _, a := range resultFeed.Assets {
		if a.FileName == "MyApp-1.0.0-full.nupkg" {
			count++
		}
	}
	if count != 1 {
		t.Errorf("expected 1 asset after dedup, got %d", count)
	}
}

func TestPromote_FilesPhysicallyCopied(t *testing.T) {
	store := newTestStore(t)

	srcFeed := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 100},
		{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-delta.nupkg", Size: 50},
	}}
	store.WriteReleaseFeed("myapp", "beta", "releases.win.json", srcFeed)
	store.WriteFile("myapp", "beta", "MyApp-1.0.0-full.nupkg", []byte("full-pkg"))
	store.WriteFile("myapp", "beta", "MyApp-1.0.0-delta.nupkg", []byte("delta-pkg"))

	handler := NewPromoteHandler(store, nil)
	req := httptest.NewRequest(http.MethodPost,
		"/api/apps/myapp/channels/stable/promote?from=beta&version=1.0.0", nil)
	req.SetPathValue("appId", "myapp")
	req.SetPathValue("target", "stable")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	var resp PromoteResponse
	json.NewDecoder(rr.Body).Decode(&resp)
	if resp.FilesCopied != 2 {
		t.Errorf("expected 2 files copied, got %d", resp.FilesCopied)
	}

	// Verify both files exist in target directory
	targetDir := store.ChannelDir("myapp", "stable")
	for _, fname := range []string{"MyApp-1.0.0-full.nupkg", "MyApp-1.0.0-delta.nupkg"} {
		if _, err := os.Stat(filepath.Join(targetDir, fname)); err != nil {
			t.Errorf("expected file %s in target: %v", fname, err)
		}
	}
}

func TestPromote_MethodNotAllowed(t *testing.T) {
	store := newTestStore(t)
	handler := NewPromoteHandler(store, nil)

	req := httptest.NewRequest(http.MethodGet, "/api/apps/myapp/channels/stable/promote", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", rr.Code)
	}
}
