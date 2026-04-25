package storage

import (
	"encoding/json"
	"os"
	"path/filepath"
	"testing"

	"docufiller-update-server/model"
)

func TestReadReleaseFeed_MissingFile(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	feed, err := s.ReadReleaseFeed("stable")
	if err != nil {
		t.Fatalf("ReadReleaseFeed on missing file should not error: %v", err)
	}
	if feed == nil {
		t.Fatal("feed should not be nil")
	}
	if len(feed.Assets) != 0 {
		t.Fatalf("expected empty assets, got %d", len(feed.Assets))
	}
}

func TestReadReleaseFeed_ExistingFile(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	channelDir := filepath.Join(dir, "stable")
	os.MkdirAll(channelDir, 0o755)

	existing := model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "MyApp", Version: "1.0.0", FileName: "MyApp-1.0.0-full.nupkg", Size: 1024},
		},
	}
	data, _ := json.MarshalIndent(existing, "", "  ")
	os.WriteFile(filepath.Join(channelDir, "releases.win.json"), data, 0o644)

	feed, err := s.ReadReleaseFeed("stable")
	if err != nil {
		t.Fatalf("ReadReleaseFeed failed: %v", err)
	}
	if len(feed.Assets) != 1 {
		t.Fatalf("expected 1 asset, got %d", len(feed.Assets))
	}
	if feed.Assets[0].Version != "1.0.0" {
		t.Errorf("expected version 1.0.0, got %s", feed.Assets[0].Version)
	}
}

func TestWriteReleaseFeed_CreatesCorrectJSON(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	feed := &model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "App", Version: "2.0.0", FileName: "App-2.0.0-full.nupkg", Size: 2048},
		},
	}
	if err := s.WriteReleaseFeed("beta", feed); err != nil {
		t.Fatalf("WriteReleaseFeed failed: %v", err)
	}

	// Verify file exists and is valid JSON
	data, err := os.ReadFile(filepath.Join(dir, "beta", "releases.win.json"))
	if err != nil {
		t.Fatalf("failed to read written file: %v", err)
	}

	var parsed model.ReleaseFeed
	if err := json.Unmarshal(data, &parsed); err != nil {
		t.Fatalf("failed to parse written JSON: %v", err)
	}
	if len(parsed.Assets) != 1 || parsed.Assets[0].Version != "2.0.0" {
		t.Errorf("unexpected content: %+v", parsed)
	}
}

func TestEnsureChannelDir_CreatesDirectoryStructure(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	if err := s.EnsureChannelDir("stable"); err != nil {
		t.Fatalf("EnsureChannelDir failed: %v", err)
	}

	info, err := os.Stat(filepath.Join(dir, "stable"))
	if err != nil {
		t.Fatalf("directory not created: %v", err)
	}
	if !info.IsDir() {
		t.Error("expected directory, got file")
	}

	// Calling again should not fail (idempotent)
	if err := s.EnsureChannelDir("stable"); err != nil {
		t.Fatalf("EnsureChannelDir idempotent call failed: %v", err)
	}
}

func TestChannelDir(t *testing.T) {
	s := NewStore("/data")
	expected := filepath.Join("/data", "beta")
	if got := s.ChannelDir("beta"); got != expected {
		t.Errorf("ChannelDir = %q, want %q", got, expected)
	}
}

func TestListFiles(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	channelDir := filepath.Join(dir, "beta")
	os.MkdirAll(channelDir, 0o755)

	// Create test files
	os.WriteFile(filepath.Join(channelDir, "App-1.0.0-full.nupkg"), []byte("pkg1"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "App-1.0.1-full.nupkg"), []byte("pkg2"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "releases.win.json"), []byte("{}"), 0o644)

	files, err := s.ListFiles("beta")
	if err != nil {
		t.Fatalf("ListFiles failed: %v", err)
	}
	if len(files) != 2 {
		t.Fatalf("expected 2 .nupkg files, got %d: %v", len(files), files)
	}
}

func TestListFiles_EmptyDir(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	files, err := s.ListFiles("nonexistent")
	if err != nil {
		t.Fatalf("ListFiles on nonexistent dir should not error: %v", err)
	}
	if len(files) != 0 {
		t.Fatalf("expected 0 files, got %d", len(files))
	}
}

func TestWriteAndReadFile(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	content := []byte("test content")
	if err := s.WriteFile("stable", "test.txt", content); err != nil {
		t.Fatalf("WriteFile failed: %v", err)
	}

	read, err := s.ReadFile("stable", "test.txt")
	if err != nil {
		t.Fatalf("ReadFile failed: %v", err)
	}
	if string(read) != string(content) {
		t.Errorf("ReadFile = %q, want %q", read, content)
	}
}

func TestDeleteVersion(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	channelDir := filepath.Join(dir, "beta")
	os.MkdirAll(channelDir, 0o755)

	// Create files for version 1.0.0 and 1.0.1
	os.WriteFile(filepath.Join(channelDir, "App-1.0.0-full.nupkg"), []byte("v1"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "App-1.0.0-delta.nupkg"), []byte("d1"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "App-1.0.1-full.nupkg"), []byte("v2"), 0o644)

	if err := s.DeleteVersion("beta", "1.0.0"); err != nil {
		t.Fatalf("DeleteVersion failed: %v", err)
	}

	files, _ := s.ListFiles("beta")
	if len(files) != 1 || files[0] != "App-1.0.1-full.nupkg" {
		t.Errorf("expected only 1.0.1 file, got %v", files)
	}
}
