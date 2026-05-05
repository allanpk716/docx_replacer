package database

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"testing"
)

// tempDB creates a temporary database, runs f, and cleans up.
func tempDB(t *testing.T, f func(db *DB)) {
	t.Helper()
	dir := t.TempDir()
	dbPath := filepath.Join(dir, "test.db")

	d, err := Init(dbPath)
	if err != nil {
		t.Fatalf("Init: %v", err)
	}
	defer d.Close()

	f(d)
}

func TestInit(t *testing.T) {
	dir := t.TempDir()
	dbPath := filepath.Join(dir, "sub", "dir", "test.db")

	d, err := Init(dbPath)
	if err != nil {
		t.Fatalf("Init with nested directory: %v", err)
	}
	defer d.Close()

	// Verify the file was created
	if _, err := os.Stat(dbPath); os.IsNotExist(err) {
		t.Fatal("database file was not created")
	}

	// Verify WAL mode
	var mode string
	err = d.db.QueryRow("PRAGMA journal_mode").Scan(&mode)
	if err != nil {
		t.Fatalf("query journal_mode: %v", err)
	}
	if mode != "wal" {
		t.Errorf("journal_mode = %q, want %q", mode, "wal")
	}
}

func TestIdempotentMigration(t *testing.T) {
	dir := t.TempDir()
	dbPath := filepath.Join(dir, "test.db")

	// Open twice — schema should not error on second init
	d1, err := Init(dbPath)
	if err != nil {
		t.Fatalf("first Init: %v", err)
	}
	d1.Close()

	d2, err := Init(dbPath)
	if err != nil {
		t.Fatalf("second Init (idempotent): %v", err)
	}
	d2.Close()
}

func TestUpsertApp(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		// Insert new app
		if err := d.UpsertApp(ctx, "myapp"); err != nil {
			t.Fatalf("UpsertApp: %v", err)
		}

		// Idempotent — same app again
		if err := d.UpsertApp(ctx, "myapp"); err != nil {
			t.Fatalf("UpsertApp (duplicate): %v", err)
		}

		// Verify it exists
		apps, err := d.GetApps(ctx)
		if err != nil {
			t.Fatalf("GetApps: %v", err)
		}
		if len(apps) != 1 || apps[0].ID != "myapp" {
			t.Errorf("apps = %v, want [{id:myapp}]", apps)
		}
	})
}

func TestGetApps(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		// Empty case
		apps, err := d.GetApps(ctx)
		if err != nil {
			t.Fatalf("GetApps empty: %v", err)
		}
		if len(apps) != 0 {
			t.Errorf("expected 0 apps, got %d", len(apps))
		}

		// Add apps with versions to get channels
		_ = d.UpsertVersion(ctx, "app1", "stable", "1.0.0", "release")
		_ = d.UpsertVersion(ctx, "app1", "beta", "2.0.0", "beta release")
		_ = d.UpsertVersion(ctx, "app2", "stable", "1.0.0", "")

		apps, err = d.GetApps(ctx)
		if err != nil {
			t.Fatalf("GetApps: %v", err)
		}
		if len(apps) != 2 {
			t.Fatalf("expected 2 apps, got %d", len(apps))
		}

		// app1 should have 2 channels
		if apps[0].ID != "app1" {
			t.Fatalf("first app = %q, want %q", apps[0].ID, "app1")
		}
		if len(apps[0].Channels) != 2 {
			t.Errorf("app1 channels = %v, want 2", apps[0].Channels)
		}

		// app2 should have 1 channel
		if apps[1].ID != "app2" {
			t.Fatalf("second app = %q, want %q", apps[1].ID, "app2")
		}
		if len(apps[1].Channels) != 1 || apps[1].Channels[0] != "stable" {
			t.Errorf("app2 channels = %v, want [stable]", apps[1].Channels)
		}
	})
}

func TestGetChannels(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		// No channels initially
		ch, err := d.GetChannels(ctx, "noapp")
		if err != nil {
			t.Fatalf("GetChannels empty: %v", err)
		}
		if len(ch) != 0 {
			t.Errorf("expected 0 channels, got %d", len(ch))
		}

		_ = d.UpsertVersion(ctx, "app1", "stable", "1.0.0", "")
		_ = d.UpsertVersion(ctx, "app1", "beta", "2.0.0", "")
		_ = d.UpsertVersion(ctx, "app1", "stable", "1.1.0", "")

		ch, err = d.GetChannels(ctx, "app1")
		if err != nil {
			t.Fatalf("GetChannels: %v", err)
		}
		// stable appears twice in versions but should be distinct
		if len(ch) != 2 {
			t.Errorf("channels = %v, want 2 distinct", ch)
		}
	})
}

func TestUpsertVersion(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		if err := d.UpsertVersion(ctx, "myapp", "stable", "1.0.0", "initial release"); err != nil {
			t.Fatalf("UpsertVersion: %v", err)
		}

		// Verify app was auto-created
		apps, _ := d.GetApps(ctx)
		if len(apps) != 1 || apps[0].ID != "myapp" {
			t.Errorf("auto-created app not found: %v", apps)
		}

		// Duplicate should update notes, not error
		if err := d.UpsertVersion(ctx, "myapp", "stable", "1.0.0", "updated notes"); err != nil {
			t.Fatalf("UpsertVersion (update): %v", err)
		}

		versions, _ := d.GetVersions(ctx, "myapp", "stable")
		if len(versions) != 1 {
			t.Fatalf("expected 1 version, got %d", len(versions))
		}
		if versions[0].Notes != "updated notes" {
			t.Errorf("notes = %q, want %q", versions[0].Notes, "updated notes")
		}
	})
}

func TestGetVersions(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		// Empty case
		v, err := d.GetVersions(ctx, "noapp", "stable")
		if err != nil {
			t.Fatalf("GetVersions empty: %v", err)
		}
		if len(v) != 0 {
			t.Errorf("expected 0 versions, got %d", len(v))
		}

		// Add versions
		_ = d.UpsertVersion(ctx, "myapp", "stable", "1.0.0", "first")
		_ = d.UpsertVersion(ctx, "myapp", "stable", "1.1.0", "second")
		_ = d.UpsertVersion(ctx, "myapp", "beta", "2.0.0", "beta")
		_ = d.UpsertVersion(ctx, "otherapp", "stable", "1.0.0", "other")

		// Get stable versions for myapp
		v, err = d.GetVersions(ctx, "myapp", "stable")
		if err != nil {
			t.Fatalf("GetVersions: %v", err)
		}
		if len(v) != 2 {
			t.Fatalf("expected 2 versions, got %d", len(v))
		}
		// Should be DESC by created_at
		if v[0].Version != "1.1.0" {
			t.Errorf("first version = %q, want %q (DESC order)", v[0].Version, "1.1.0")
		}
		if v[1].Version != "1.0.0" {
			t.Errorf("second version = %q, want %q (DESC order)", v[1].Version, "1.0.0")
		}

		// Beta channel should only have one
		vBeta, _ := d.GetVersions(ctx, "myapp", "beta")
		if len(vBeta) != 1 || vBeta[0].Version != "2.0.0" {
			t.Errorf("beta versions = %v, want 1 entry with 2.0.0", vBeta)
		}
	})
}

func TestDeleteVersion(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		_ = d.UpsertVersion(ctx, "myapp", "stable", "1.0.0", "release")
		_ = d.UpsertVersion(ctx, "myapp", "stable", "1.1.0", "patch")

		// Delete one version
		if err := d.DeleteVersion(ctx, "myapp", "stable", "1.0.0"); err != nil {
			t.Fatalf("DeleteVersion: %v", err)
		}

		v, _ := d.GetVersions(ctx, "myapp", "stable")
		if len(v) != 1 || v[0].Version != "1.1.0" {
			t.Errorf("after delete, versions = %v, want only 1.1.0", v)
		}

		// Delete non-existent version should succeed without error
		if err := d.DeleteVersion(ctx, "myapp", "stable", "9.9.9"); err != nil {
			t.Fatalf("DeleteVersion non-existent: %v", err)
		}
	})
}

func TestEmptyNotes(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		_ = d.UpsertVersion(ctx, "myapp", "stable", "1.0.0", "")
		v, _ := d.GetVersions(ctx, "myapp", "stable")
		if len(v) != 1 {
			t.Fatalf("expected 1 version")
		}
		if v[0].Notes != "" {
			t.Errorf("notes = %q, want empty", v[0].Notes)
		}
	})
}

func TestLongNotes(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		// 10KB+ notes string
		longNotes := make([]byte, 12*1024)
		for i := range longNotes {
			longNotes[i] = 'A' + byte(i%26)
		}

		_ = d.UpsertVersion(ctx, "myapp", "stable", "1.0.0", string(longNotes))
		v, _ := d.GetVersions(ctx, "myapp", "stable")
		if len(v) != 1 {
			t.Fatalf("expected 1 version")
		}
		if len(v[0].Notes) != len(longNotes) {
			t.Errorf("notes length = %d, want %d", len(v[0].Notes), len(longNotes))
		}
	})
}

func TestSpecialCharacters(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()

		specialVersion := "1.0.0-beta+build.123"
		specialNotes := "Contains 'quotes' and \"double quotes\" and ; DROP TABLE versions;--"
		_ = d.UpsertVersion(ctx, "my-app/v2", "stable", specialVersion, specialNotes)

		v, _ := d.GetVersions(ctx, "my-app/v2", "stable")
		if len(v) != 1 {
			t.Fatalf("expected 1 version")
		}
		if v[0].Version != specialVersion {
			t.Errorf("version = %q, want %q", v[0].Version, specialVersion)
		}
		if v[0].Notes != specialNotes {
			t.Errorf("notes = %q, want %q", v[0].Notes, specialNotes)
		}
	})
}

func TestOperationsAfterClose(t *testing.T) {
	dir := t.TempDir()
	dbPath := filepath.Join(dir, "test.db")
	d, _ := Init(dbPath)
	d.Close()

	ctx := context.Background()
	if err := d.UpsertApp(ctx, "app"); err == nil {
		t.Error("expected error after Close")
	}
}

func TestConcurrentAccess(t *testing.T) {
	tempDB(t, func(d *DB) {
		ctx := context.Background()
		var wg sync.WaitGroup
		errCh := make(chan error, 20)

		// 10 concurrent writers
		for i := 0; i < 10; i++ {
			wg.Add(1)
			go func(n int) {
				defer wg.Done()
				appID := "concurrent-app"
				channel := "stable"
				version := fmt.Sprintf("1.%d.0", n)
				if err := d.UpsertVersion(ctx, appID, channel, version, ""); err != nil {
					errCh <- err
				}
			}(i)
		}

		// 10 concurrent readers
		for i := 0; i < 10; i++ {
			wg.Add(1)
			go func() {
				defer wg.Done()
				_, _ = d.GetVersions(ctx, "concurrent-app", "stable")
			}()
		}

		wg.Wait()
		close(errCh)
		for err := range errCh {
			t.Errorf("concurrent access error: %v", err)
		}

		// Verify all writes succeeded
		v, _ := d.GetVersions(ctx, "concurrent-app", "stable")
		if len(v) != 10 {
			t.Errorf("expected 10 versions, got %d", len(v))
		}
	})
}
