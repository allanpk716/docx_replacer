package migration

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
	"testing"

	"update-hub/database"
	"update-hub/model"
)

// helper: create a temp directory with the old single-app layout
func createOldFormatLayout(t *testing.T, channels map[string][]model.ReleaseAsset) string {
	t.Helper()
	tmpDir := t.TempDir()

	for channel, assets := range channels {
		dir := filepath.Join(tmpDir, channel)
		if err := os.MkdirAll(dir, 0o755); err != nil {
			t.Fatalf("create channel dir %s: %v", channel, err)
		}
		feed := model.ReleaseFeed{Assets: assets}
		data, _ := json.MarshalIndent(feed, "", "  ")
		if err := os.WriteFile(filepath.Join(dir, "releases.win.json"), data, 0o644); err != nil {
			t.Fatalf("write feed for %s: %v", channel, err)
		}
	}

	return tmpDir
}

// helper: create a temp directory with the new multi-app layout
func createNewFormatLayout(t *testing.T, appId string, channels map[string][]model.ReleaseAsset) string {
	t.Helper()
	tmpDir := t.TempDir()

	for channel, assets := range channels {
		dir := filepath.Join(tmpDir, appId, channel)
		if err := os.MkdirAll(dir, 0o755); err != nil {
			t.Fatalf("create channel dir %s/%s: %v", appId, channel, err)
		}
		feed := model.ReleaseFeed{Assets: assets}
		data, _ := json.MarshalIndent(feed, "", "  ")
		if err := os.WriteFile(filepath.Join(dir, "releases.win.json"), data, 0o644); err != nil {
			t.Fatalf("write feed for %s/%s: %v", appId, channel, err)
		}
	}

	return tmpDir
}

func sampleAssets() []model.ReleaseAsset {
	return []model.ReleaseAsset{
		{
			PackageId: "DocuFiller",
			Version:   "1.0.0",
			Type:      "Full",
			FileName:  "DocuFiller-1.0.0-full.nupkg",
			SHA1:      "abc123",
			Size:      1024,
		},
		{
			PackageId: "DocuFiller",
			Version:   "1.1.0",
			Type:      "Full",
			FileName:  "DocuFiller-1.1.0-full.nupkg",
			SHA1:      "def456",
			Size:      2048,
		},
	}
}

// --- Migrate tests ---

func TestMigrate_OldFormatDetected(t *testing.T) {
	dataDir := createOldFormatLayout(t, map[string][]model.ReleaseAsset{
		"stable": sampleAssets(),
		"beta":   sampleAssets(),
	})

	err := Migrate(dataDir, "docufiller")
	if err != nil {
		t.Fatalf("Migrate returned error: %v", err)
	}

	// Old dirs should be gone
	if _, err := os.Stat(filepath.Join(dataDir, "stable")); !os.IsNotExist(err) {
		t.Error("old stable/ dir should not exist after migration")
	}
	if _, err := os.Stat(filepath.Join(dataDir, "beta")); !os.IsNotExist(err) {
		t.Error("old beta/ dir should not exist after migration")
	}

	// New dirs should exist with feed files
	for _, channel := range []string{"stable", "beta"} {
		feedPath := filepath.Join(dataDir, "docufiller", channel, "releases.win.json")
		if _, err := os.Stat(feedPath); err != nil {
			t.Errorf("expected %s to exist: %v", feedPath, err)
		}
	}
}

func TestMigrate_Idempotent(t *testing.T) {
	dataDir := createOldFormatLayout(t, map[string][]model.ReleaseAsset{
		"stable": sampleAssets(),
	})

	// First migration
	if err := Migrate(dataDir, "docufiller"); err != nil {
		t.Fatalf("first Migrate: %v", err)
	}

	// Create a new "stable" dir to simulate re-running on partially-migrated data
	newStable := filepath.Join(dataDir, "stable")
	if err := os.MkdirAll(newStable, 0o755); err != nil {
		t.Fatalf("create stable dir: %v", err)
	}
	feed := model.ReleaseFeed{Assets: sampleAssets()}
	data, _ := json.MarshalIndent(feed, "", "  ")
	os.WriteFile(filepath.Join(newStable, "releases.win.json"), data, 0o644)

	// Second migration should skip (target already exists)
	if err := Migrate(dataDir, "docufiller"); err != nil {
		t.Fatalf("second Migrate: %v", err)
	}

	// The old stable should still exist (was NOT moved because target existed)
	if _, err := os.Stat(newStable); os.IsNotExist(err) {
		t.Error("old stable/ should still exist when target is present")
	}
}

func TestMigrate_EmptyDataDir(t *testing.T) {
	dataDir := t.TempDir()

	err := Migrate(dataDir, "docufiller")
	if err != nil {
		t.Fatalf("Migrate on empty dir should not error: %v", err)
	}
}

func TestMigrate_NonexistentDataDir(t *testing.T) {
	err := Migrate("/nonexistent/path/data", "docufiller")
	if err != nil {
		t.Fatalf("Migrate on nonexistent dir should return nil: %v", err)
	}
}

func TestMigrate_NewFormatDirsNotMigrated(t *testing.T) {
	// Create a mix: old-format dirs and new-format app dirs
	dataDir := t.TempDir()

	// Old format: stable/ with feed file
	oldDir := filepath.Join(dataDir, "stable")
	os.MkdirAll(oldDir, 0o755)
	feed := model.ReleaseFeed{Assets: sampleAssets()}
	data, _ := json.MarshalIndent(feed, "", "  ")
	os.WriteFile(filepath.Join(oldDir, "releases.win.json"), data, 0o644)

	// New format: anotherapp/ containing only subdirs (no feed files)
	newAppDir := filepath.Join(dataDir, "anotherapp", "stable")
	os.MkdirAll(newAppDir, 0o755)

	err := Migrate(dataDir, "docufiller")
	if err != nil {
		t.Fatalf("Migrate: %v", err)
	}

	// anotherapp should NOT be moved (it's a new-format app dir)
	if _, err := os.Stat(filepath.Join(dataDir, "anotherapp")); err != nil {
		t.Error("anotherapp/ should still exist (not migrated)")
	}

	// stable should be moved
	if _, err := os.Stat(filepath.Join(dataDir, "stable")); !os.IsNotExist(err) {
		t.Error("old stable/ should be gone")
	}
}

func TestMigrate_DirWithOnlySpecialFiles(t *testing.T) {
	// A dir containing only .db or root-level .json should not be treated as a channel dir
	dataDir := t.TempDir()
	specialDir := filepath.Join(dataDir, "metadata")
	os.MkdirAll(specialDir, 0o755)
	os.WriteFile(filepath.Join(specialDir, "config.json"), []byte("{}"), 0o644)
	os.WriteFile(filepath.Join(specialDir, "update-hub.db"), []byte("sqlite"), 0o644)

	err := Migrate(dataDir, "docufiller")
	if err != nil {
		t.Fatalf("Migrate: %v", err)
	}

	// metadata dir should NOT be moved
	if _, err := os.Stat(specialDir); err != nil {
		t.Error("metadata/ should still exist (not a channel dir)")
	}
}

func TestMigrate_PreservesNupkgFiles(t *testing.T) {
	dataDir := t.TempDir()
	stableDir := filepath.Join(dataDir, "stable")
	os.MkdirAll(stableDir, 0o755)

	// Write feed
	feed := model.ReleaseFeed{Assets: sampleAssets()}
	data, _ := json.MarshalIndent(feed, "", "  ")
	os.WriteFile(filepath.Join(stableDir, "releases.win.json"), data, 0o644)

	// Write a .nupkg file
	nupkgData := []byte("fake package content")
	os.WriteFile(filepath.Join(stableDir, "DocuFiller-1.0.0-full.nupkg"), nupkgData, 0o644)

	err := Migrate(dataDir, "docufiller")
	if err != nil {
		t.Fatalf("Migrate: %v", err)
	}

	// .nupkg should be in new location
	nupkgPath := filepath.Join(dataDir, "docufiller", "stable", "DocuFiller-1.0.0-full.nupkg")
	got, err := os.ReadFile(nupkgPath)
	if err != nil {
		t.Fatalf("read nupkg: %v", err)
	}
	if string(got) != string(nupkgData) {
		t.Errorf("nupkg content mismatch: got %q, want %q", got, nupkgData)
	}
}

// --- SyncMetadata tests ---

func TestSyncMetadata_BasicUpsert(t *testing.T) {
	dataDir := createNewFormatLayout(t, "docufiller", map[string][]model.ReleaseAsset{
		"stable": sampleAssets(),
	})

	// Create in-memory SQLite DB
	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	err = SyncMetadata(dataDir, db)
	if err != nil {
		t.Fatalf("SyncMetadata: %v", err)
	}

	// Verify versions in DB
	ctx := context.Background()
	versions, err := db.GetVersions(ctx, "docufiller", "stable")
	if err != nil {
		t.Fatalf("GetVersions: %v", err)
	}
	if len(versions) != 2 {
		t.Fatalf("expected 2 versions, got %d", len(versions))
	}

	gotVersions := map[string]bool{}
	for _, v := range versions {
		gotVersions[v.Version] = true
	}
	if !gotVersions["1.0.0"] {
		t.Error("expected version 1.0.0")
	}
	if !gotVersions["1.1.0"] {
		t.Error("expected version 1.1.0")
	}
}

func TestSyncMetadata_MultipleApps(t *testing.T) {
	dataDir := t.TempDir()

	// Create two apps
	for _, appId := range []string{"docufiller", "otherapp"} {
		dir := filepath.Join(dataDir, appId, "stable")
		os.MkdirAll(dir, 0o755)
		feed := model.ReleaseFeed{Assets: []model.ReleaseAsset{
			{Version: "2.0.0", Type: "Full", FileName: "app-2.0.0.nupkg", Size: 512},
		}}
		data, _ := json.MarshalIndent(feed, "", "  ")
		os.WriteFile(filepath.Join(dir, "releases.win.json"), data, 0o644)
	}

	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	err = SyncMetadata(dataDir, db)
	if err != nil {
		t.Fatalf("SyncMetadata: %v", err)
	}

	ctx := context.Background()
	for _, appId := range []string{"docufiller", "otherapp"} {
		versions, err := db.GetVersions(ctx, appId, "stable")
		if err != nil {
			t.Fatalf("GetVersions for %s: %v", appId, err)
		}
		if len(versions) != 1 {
			t.Errorf("expected 1 version for %s, got %d", appId, len(versions))
		}
		if versions[0].Version != "2.0.0" {
			t.Errorf("expected version 2.0.0 for %s, got %s", appId, versions[0].Version)
		}
	}
}

func TestSyncMetadata_Idempotent(t *testing.T) {
	dataDir := createNewFormatLayout(t, "docufiller", map[string][]model.ReleaseAsset{
		"stable": sampleAssets(),
	})

	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	// Run twice
	if err := SyncMetadata(dataDir, db); err != nil {
		t.Fatalf("first SyncMetadata: %v", err)
	}
	if err := SyncMetadata(dataDir, db); err != nil {
		t.Fatalf("second SyncMetadata: %v", err)
	}

	// Should still have exactly 2 versions (INSERT OR REPLACE)
	ctx := context.Background()
	versions, err := db.GetVersions(ctx, "docufiller", "stable")
	if err != nil {
		t.Fatalf("GetVersions: %v", err)
	}
	if len(versions) != 2 {
		t.Errorf("expected 2 versions after double-sync, got %d", len(versions))
	}
}

func TestSyncMetadata_EmptyFeed(t *testing.T) {
	dataDir := t.TempDir()
	dir := filepath.Join(dataDir, "myapp", "stable")
	os.MkdirAll(dir, 0o755)

	// Empty feed (no assets)
	feed := model.ReleaseFeed{Assets: []model.ReleaseAsset{}}
	data, _ := json.MarshalIndent(feed, "", "  ")
	os.WriteFile(filepath.Join(dir, "releases.win.json"), data, 0o644)

	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	err = SyncMetadata(dataDir, db)
	if err != nil {
		t.Fatalf("SyncMetadata with empty feed: %v", err)
	}

	ctx := context.Background()
	versions, err := db.GetVersions(ctx, "myapp", "stable")
	if err != nil {
		t.Fatalf("GetVersions: %v", err)
	}
	if len(versions) != 0 {
		t.Errorf("expected 0 versions for empty feed, got %d", len(versions))
	}
}

func TestSyncMetadata_NonexistentDir(t *testing.T) {
	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	err = SyncMetadata("/nonexistent/path", db)
	if err != nil {
		t.Fatalf("SyncMetadata on nonexistent dir should return nil: %v", err)
	}
}

func TestSyncMetadata_SkipsEmptyVersionAssets(t *testing.T) {
	dataDir := t.TempDir()
	dir := filepath.Join(dataDir, "myapp", "stable")
	os.MkdirAll(dir, 0o755)

	// Feed with one valid asset and one with empty version
	feed := model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{Version: "1.0.0", Type: "Full"},
		{Version: "", Type: "Full"}, // should be skipped
	}}
	data, _ := json.MarshalIndent(feed, "", "  ")
	os.WriteFile(filepath.Join(dir, "releases.win.json"), data, 0o644)

	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	err = SyncMetadata(dataDir, db)
	if err != nil {
		t.Fatalf("SyncMetadata: %v", err)
	}

	ctx := context.Background()
	versions, err := db.GetVersions(ctx, "myapp", "stable")
	if err != nil {
		t.Fatalf("GetVersions: %v", err)
	}
	if len(versions) != 1 {
		t.Errorf("expected 1 version (empty version skipped), got %d", len(versions))
	}
}

func TestSyncMetadata_MultipleFeedFiles(t *testing.T) {
	dataDir := t.TempDir()
	dir := filepath.Join(dataDir, "myapp", "stable")
	os.MkdirAll(dir, 0o755)

	// Write two feed files for different OSes
	for _, filename := range []string{"releases.win.json", "releases.linux.json"} {
		feed := model.ReleaseFeed{Assets: []model.ReleaseAsset{
			{Version: "1.0.0", Type: "Full"},
		}}
		data, _ := json.MarshalIndent(feed, "", "  ")
		os.WriteFile(filepath.Join(dir, filename), data, 0o644)
	}

	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	err = SyncMetadata(dataDir, db)
	if err != nil {
		t.Fatalf("SyncMetadata: %v", err)
	}

	// Same version from two feeds should result in 1 entry (INSERT OR REPLACE)
	ctx := context.Background()
	versions, err := db.GetVersions(ctx, "myapp", "stable")
	if err != nil {
		t.Fatalf("GetVersions: %v", err)
	}
	if len(versions) != 1 {
		t.Errorf("expected 1 version (deduplicated), got %d", len(versions))
	}
}

// --- Integration: Migrate + SyncMetadata ---

func TestMigrateAndSyncMetadata_Integration(t *testing.T) {
	// Start with old format
	dataDir := createOldFormatLayout(t, map[string][]model.ReleaseAsset{
		"stable": {model.ReleaseAsset{Version: "1.0.0", Type: "Full", FileName: "DocuFiller-1.0.0-full.nupkg", Size: 1024}},
		"beta":   {model.ReleaseAsset{Version: "2.0.0-beta1", Type: "Full", FileName: "DocuFiller-2.0.0-beta1-full.nupkg", Size: 2048}},
	})

	// Migrate
	if err := Migrate(dataDir, "docufiller"); err != nil {
		t.Fatalf("Migrate: %v", err)
	}

	// Verify new directory structure
	for _, channel := range []string{"stable", "beta"} {
		fp := filepath.Join(dataDir, "docufiller", channel, "releases.win.json")
		if _, err := os.Stat(fp); err != nil {
			t.Errorf("expected %s to exist: %v", fp, err)
		}
	}

	// Sync metadata
	dbPath := filepath.Join(t.TempDir(), "test.db")
	db, err := database.Init(dbPath)
	if err != nil {
		t.Fatalf("init db: %v", err)
	}
	defer db.Close()

	if err := SyncMetadata(dataDir, db); err != nil {
		t.Fatalf("SyncMetadata: %v", err)
	}

	// Verify DB has both channels
	ctx := context.Background()
	for _, channel := range []string{"stable", "beta"} {
		versions, err := db.GetVersions(ctx, "docufiller", channel)
		if err != nil {
			t.Fatalf("GetVersions for %s: %v", channel, err)
		}
		if len(versions) != 1 {
			t.Errorf("expected 1 version in %s, got %d", channel, len(versions))
		}
	}
}

func TestBuildNotes(t *testing.T) {
	tests := []struct {
		name   string
		asset  model.ReleaseAsset
		expect string
	}{
		{
			name:   "full asset",
			asset:  model.ReleaseAsset{Type: "Full", FileName: "app.nupkg", Size: 100},
			expect: "type=Full; file=app.nupkg; size=100",
		},
		{
			name:   "empty asset",
			asset:  model.ReleaseAsset{},
			expect: "",
		},
		{
			name:   "partial asset",
			asset:  model.ReleaseAsset{Type: "Delta"},
			expect: "type=Delta",
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			got := buildNotes(&tt.asset)
			if got != tt.expect {
				t.Errorf("buildNotes() = %q, want %q", got, tt.expect)
			}
		})
	}
}
