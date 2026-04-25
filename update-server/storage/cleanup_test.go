package storage

import (
	"fmt"
	"os"
	"path/filepath"
	"testing"

	"docufiller-update-server/model"
)

func writeTestFeed(t *testing.T, s *Store, channel string, versions []string) {
	t.Helper()
	var assets []model.ReleaseAsset
	for _, v := range versions {
		assets = append(assets, model.ReleaseAsset{
			PackageId: "App",
			Version:   v,
			Type:      "Full",
			FileName:  "App-" + v + "-full.nupkg",
			Size:      1024,
		})
		// Create the actual .nupkg file
		s.EnsureChannelDir(channel)
		os.WriteFile(filepath.Join(s.ChannelDir(channel), "App-"+v+"-full.nupkg"), []byte("data-"+v), 0o644)
	}
	feed := &model.ReleaseFeed{Assets: assets}
	if err := s.WriteReleaseFeed(channel, feed); err != nil {
		t.Fatalf("writeTestFeed failed: %v", err)
	}
}

func TestCleanupOldVersions_FewerThan10(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	versions := []string{"1.0.0", "1.0.1", "1.0.2"}
	writeTestFeed(t, s, "beta", versions)

	removed, err := s.CleanupOldVersions("beta", 10)
	if err != nil {
		t.Fatalf("CleanupOldVersions failed: %v", err)
	}
	if len(removed) != 0 {
		t.Errorf("expected 0 removed, got %d: %v", len(removed), removed)
	}

	// Verify all files still exist
	files, _ := s.ListFiles("beta")
	if len(files) != 3 {
		t.Errorf("expected 3 files, got %d: %v", len(files), files)
	}
}

func TestCleanupOldVersions_11Versions_OldestRemoved(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	versions := []string{
		"1.0.0", "1.0.1", "1.0.2", "1.0.3", "1.0.4",
		"1.0.5", "1.0.6", "1.0.7", "1.0.8", "1.0.9",
		"1.1.0",
	}
	writeTestFeed(t, s, "beta", versions)

	removed, err := s.CleanupOldVersions("beta", 10)
	if err != nil {
		t.Fatalf("CleanupOldVersions failed: %v", err)
	}
	if len(removed) != 1 {
		t.Fatalf("expected 1 removed version, got %d: %v", len(removed), removed)
	}
	if removed[0] != "1.0.0" {
		t.Errorf("expected 1.0.0 removed, got %s", removed[0])
	}

	// Verify the oldest file was deleted
	files, _ := s.ListFiles("beta")
	for _, f := range files {
		if f == "App-1.0.0-full.nupkg" {
			t.Error("1.0.0 file should have been deleted")
		}
	}
	if len(files) != 10 {
		t.Errorf("expected 10 remaining files, got %d", len(files))
	}

	// Verify releases.win.json was updated (no 1.0.0 entry)
	feed, _ := s.ReadReleaseFeed("beta")
	for _, a := range feed.Assets {
		if a.Version == "1.0.0" {
			t.Error("feed should not contain version 1.0.0 after cleanup")
		}
	}
}

func TestCleanupOldVersions_EmptyFeed(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	s.EnsureChannelDir("beta")

	removed, err := s.CleanupOldVersions("beta", 10)
	if err != nil {
		t.Fatalf("CleanupOldVersions on empty feed failed: %v", err)
	}
	if removed != nil {
		t.Errorf("expected nil removed for empty feed, got %v", removed)
	}
}

func TestCleanupOldVersions_RemovesFilesAndUpdateFeed(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	// Create 15 versions - should keep 10, remove 5 oldest
	versions := make([]string, 15)
	for i := 0; i < 15; i++ {
		versions[i] = fmt.Sprintf("1.0.%d", i)
	}
	writeTestFeed(t, s, "stable", versions)

	removed, err := s.CleanupOldVersions("stable", 10)
	if err != nil {
		t.Fatalf("CleanupOldVersions failed: %v", err)
	}
	if len(removed) != 5 {
		t.Fatalf("expected 5 removed, got %d: %v", len(removed), removed)
	}

	// Verify feed has exactly 10 assets
	feed, _ := s.ReadReleaseFeed("stable")
	if len(feed.Assets) != 10 {
		t.Errorf("expected 10 assets in feed, got %d", len(feed.Assets))
	}

	// Verify disk has exactly 10 .nupkg files
	files, _ := s.ListFiles("stable")
	if len(files) != 10 {
		t.Errorf("expected 10 .nupkg files, got %d", len(files))
	}
}

func TestCompareSemver(t *testing.T) {
	tests := []struct {
		a, b string
		want int // 1 if a>b, -1 if a<b, 0 if equal
	}{
		{"1.0.0", "1.0.0", 0},
		{"2.0.0", "1.0.0", 1},
		{"1.0.0", "2.0.0", -1},
		{"1.1.0", "1.0.9", 1},
		{"1.0.10", "1.0.9", 1},
		{"1.10.0", "1.9.0", 1},
	}

	for _, tt := range tests {
		got := compareSemver(tt.a, tt.b)
		if got != tt.want {
			t.Errorf("compareSemver(%s, %s) = %d, want %d", tt.a, tt.b, got, tt.want)
		}
	}
}
