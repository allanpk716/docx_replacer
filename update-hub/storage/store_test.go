package storage

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"testing"

	"update-hub/model"
)

// ── Store path helpers ──

func TestAppDir(t *testing.T) {
	s := NewStore("/data")
	expected := filepath.Join("/data", "docufiller")
	if got := s.AppDir("docufiller"); got != expected {
		t.Errorf("AppDir = %q, want %q", got, expected)
	}
}

func TestChannelDir(t *testing.T) {
	s := NewStore("/data")
	expected := filepath.Join("/data", "docufiller", "stable")
	if got := s.ChannelDir("docufiller", "stable"); got != expected {
		t.Errorf("ChannelDir = %q, want %q", got, expected)
	}
}

func TestChannelDir_DifferentApps(t *testing.T) {
	s := NewStore("/data")
	app1 := s.ChannelDir("docufiller", "stable")
	app2 := s.ChannelDir("otherapp", "beta")
	if app1 == app2 {
		t.Error("different apps should have different channel directories")
	}
	if !filepath.HasPrefix(app1, filepath.Join("/data", "docufiller")) {
		t.Errorf("app1 channel dir %q should be under /data/docufiller", app1)
	}
	if !filepath.HasPrefix(app2, filepath.Join("/data", "otherapp")) {
		t.Errorf("app2 channel dir %q should be under /data/otherapp", app2)
	}
}

// ── EnsureDir ──

func TestEnsureDir_CreatesDirectoryStructure(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	if err := s.EnsureDir("docufiller", "stable"); err != nil {
		t.Fatalf("EnsureDir failed: %v", err)
	}

	info, err := os.Stat(filepath.Join(dir, "docufiller", "stable"))
	if err != nil {
		t.Fatalf("directory not created: %v", err)
	}
	if !info.IsDir() {
		t.Error("expected directory, got file")
	}

	// Idempotent
	if err := s.EnsureDir("docufiller", "stable"); err != nil {
		t.Fatalf("EnsureDir idempotent call failed: %v", err)
	}
}

// ── ReadReleaseFeed ──

func TestReadReleaseFeed_MissingFile(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	feed, err := s.ReadReleaseFeed("docufiller", "stable", "releases.win.json")
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
	channelDir := filepath.Join(dir, "docufiller", "stable")
	os.MkdirAll(channelDir, 0o755)

	existing := model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "DocuFiller", Version: "1.0.0", FileName: "DocuFiller-1.0.0-full.nupkg", Size: 1024},
		},
	}
	data, _ := json.MarshalIndent(existing, "", "  ")
	os.WriteFile(filepath.Join(channelDir, "releases.win.json"), data, 0o644)

	feed, err := s.ReadReleaseFeed("docufiller", "stable", "releases.win.json")
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

func TestReadReleaseFeed_LinuxFile(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	channelDir := filepath.Join(dir, "myapp", "stable")
	os.MkdirAll(channelDir, 0o755)

	existing := model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "MyApp", Version: "2.0.0", FileName: "MyApp-2.0.0-full.nupkg", Size: 2048},
		},
	}
	data, _ := json.MarshalIndent(existing, "", "  ")
	os.WriteFile(filepath.Join(channelDir, "releases.linux.json"), data, 0o644)

	feed, err := s.ReadReleaseFeed("myapp", "stable", "releases.linux.json")
	if err != nil {
		t.Fatalf("ReadReleaseFeed failed: %v", err)
	}
	if len(feed.Assets) != 1 || feed.Assets[0].Version != "2.0.0" {
		t.Errorf("unexpected feed content: %+v", feed)
	}
}

func TestReadReleaseFeed_MultiAppIsolation(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	// Write feed for app1
	channelDir1 := filepath.Join(dir, "app1", "stable")
	os.MkdirAll(channelDir1, 0o755)
	feed1 := model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "App1", Version: "1.0.0", FileName: "App1-1.0.0-full.nupkg"},
	}}
	data1, _ := json.MarshalIndent(feed1, "", "  ")
	os.WriteFile(filepath.Join(channelDir1, "releases.win.json"), data1, 0o644)

	// Write feed for app2
	channelDir2 := filepath.Join(dir, "app2", "stable")
	os.MkdirAll(channelDir2, 0o755)
	feed2 := model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "App2", Version: "3.0.0", FileName: "App2-3.0.0-full.nupkg"},
	}}
	data2, _ := json.MarshalIndent(feed2, "", "  ")
	os.WriteFile(filepath.Join(channelDir2, "releases.win.json"), data2, 0o644)

	// Verify isolation
	read1, _ := s.ReadReleaseFeed("app1", "stable", "releases.win.json")
	read2, _ := s.ReadReleaseFeed("app2", "stable", "releases.win.json")
	if read1.Assets[0].PackageId != "App1" {
		t.Errorf("app1 feed should contain App1, got %s", read1.Assets[0].PackageId)
	}
	if read2.Assets[0].PackageId != "App2" {
		t.Errorf("app2 feed should contain App2, got %s", read2.Assets[0].PackageId)
	}
}

// ── WriteReleaseFeed ──

func TestWriteReleaseFeed_CreatesCorrectJSON(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	feed := &model.ReleaseFeed{
		Assets: []model.ReleaseAsset{
			{PackageId: "App", Version: "2.0.0", FileName: "App-2.0.0-full.nupkg", Size: 2048},
		},
	}
	if err := s.WriteReleaseFeed("docufiller", "beta", "releases.win.json", feed); err != nil {
		t.Fatalf("WriteReleaseFeed failed: %v", err)
	}

	data, err := os.ReadFile(filepath.Join(dir, "docufiller", "beta", "releases.win.json"))
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

func TestWriteReleaseFeed_AtomicOverwrite(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	// Write initial feed
	feed1 := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "App", Version: "1.0.0"},
	}}
	s.WriteReleaseFeed("app1", "stable", "releases.win.json", feed1)

	// Overwrite with new version
	feed2 := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "App", Version: "2.0.0"},
	}}
	s.WriteReleaseFeed("app1", "stable", "releases.win.json", feed2)

	// Verify no temp file left behind
	tmpPath := filepath.Join(dir, "app1", "stable", "releases.win.json.tmp")
	if _, err := os.Stat(tmpPath); !os.IsNotExist(err) {
		t.Error("temp file should not exist after atomic write")
	}

	// Verify content is the new version
	read, _ := s.ReadReleaseFeed("app1", "stable", "releases.win.json")
	if len(read.Assets) != 1 || read.Assets[0].Version != "2.0.0" {
		t.Errorf("expected version 2.0.0 after overwrite, got %+v", read.Assets)
	}
}

func TestWriteReleaseFeed_MultiOS(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	feedWin := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "App", Version: "1.0.0", Type: "Full", FileName: "App-1.0.0-win-full.nupkg"},
	}}
	feedLinux := &model.ReleaseFeed{Assets: []model.ReleaseAsset{
		{PackageId: "App", Version: "1.0.0", Type: "Full", FileName: "App-1.0.0-linux-full.nupkg"},
	}}

	s.WriteReleaseFeed("app1", "stable", "releases.win.json", feedWin)
	s.WriteReleaseFeed("app1", "stable", "releases.linux.json", feedLinux)

	winFeed, _ := s.ReadReleaseFeed("app1", "stable", "releases.win.json")
	linuxFeed, _ := s.ReadReleaseFeed("app1", "stable", "releases.linux.json")

	if winFeed.Assets[0].FileName != "App-1.0.0-win-full.nupkg" {
		t.Errorf("win feed wrong: %s", winFeed.Assets[0].FileName)
	}
	if linuxFeed.Assets[0].FileName != "App-1.0.0-linux-full.nupkg" {
		t.Errorf("linux feed wrong: %s", linuxFeed.Assets[0].FileName)
	}
}

// ── ListFiles ──

func TestListFiles(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	channelDir := filepath.Join(dir, "docufiller", "beta")
	os.MkdirAll(channelDir, 0o755)

	os.WriteFile(filepath.Join(channelDir, "App-1.0.0-full.nupkg"), []byte("pkg1"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "App-1.0.1-full.nupkg"), []byte("pkg2"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "releases.win.json"), []byte("{}"), 0o644)

	files, err := s.ListFiles("docufiller", "beta")
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

	files, err := s.ListFiles("nonexistent", "channel")
	if err != nil {
		t.Fatalf("ListFiles on nonexistent dir should not error: %v", err)
	}
	if len(files) != 0 {
		t.Fatalf("expected 0 files, got %d", len(files))
	}
}

func TestListFiles_MultiAppIsolation(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	// Create files for app1
	ch1 := filepath.Join(dir, "app1", "stable")
	os.MkdirAll(ch1, 0o755)
	os.WriteFile(filepath.Join(ch1, "App1-1.0.0-full.nupkg"), []byte("a"), 0o644)

	// Create files for app2
	ch2 := filepath.Join(dir, "app2", "stable")
	os.MkdirAll(ch2, 0o755)
	os.WriteFile(filepath.Join(ch2, "App2-2.0.0-full.nupkg"), []byte("b"), 0o644)
	os.WriteFile(filepath.Join(ch2, "App2-3.0.0-full.nupkg"), []byte("c"), 0o644)

	files1, _ := s.ListFiles("app1", "stable")
	files2, _ := s.ListFiles("app2", "stable")

	if len(files1) != 1 {
		t.Errorf("app1 should have 1 file, got %d", len(files1))
	}
	if len(files2) != 2 {
		t.Errorf("app2 should have 2 files, got %d", len(files2))
	}
}

// ── WriteAndReadFile ──

func TestWriteAndReadFile(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	content := []byte("test content")
	if err := s.WriteFile("docufiller", "stable", "test.txt", content); err != nil {
		t.Fatalf("WriteFile failed: %v", err)
	}

	read, err := s.ReadFile("docufiller", "stable", "test.txt")
	if err != nil {
		t.Fatalf("ReadFile failed: %v", err)
	}
	if string(read) != string(content) {
		t.Errorf("ReadFile = %q, want %q", read, content)
	}
}

func TestReadFile_NotFound(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	_, err := s.ReadFile("nonexistent", "channel", "missing.txt")
	if err == nil {
		t.Error("expected error reading nonexistent file")
	}
}

// ── DeleteVersion ──

func TestDeleteVersion(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	channelDir := filepath.Join(dir, "docufiller", "beta")
	os.MkdirAll(channelDir, 0o755)

	os.WriteFile(filepath.Join(channelDir, "App-1.0.0-full.nupkg"), []byte("v1"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "App-1.0.0-delta.nupkg"), []byte("d1"), 0o644)
	os.WriteFile(filepath.Join(channelDir, "App-1.0.1-full.nupkg"), []byte("v2"), 0o644)

	if err := s.DeleteVersion("docufiller", "beta", "1.0.0"); err != nil {
		t.Fatalf("DeleteVersion failed: %v", err)
	}

	files, _ := s.ListFiles("docufiller", "beta")
	if len(files) != 1 || files[0] != "App-1.0.1-full.nupkg" {
		t.Errorf("expected only 1.0.1 file, got %v", files)
	}
}

func TestDeleteVersion_DoesNotAffectOtherApps(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	ch1 := filepath.Join(dir, "app1", "stable")
	os.MkdirAll(ch1, 0o755)
	os.WriteFile(filepath.Join(ch1, "App-1.0.0-full.nupkg"), []byte("a"), 0o644)

	ch2 := filepath.Join(dir, "app2", "stable")
	os.MkdirAll(ch2, 0o755)
	os.WriteFile(filepath.Join(ch2, "App-1.0.0-full.nupkg"), []byte("b"), 0o644)

	s.DeleteVersion("app1", "stable", "1.0.0")

	// app2 should be unaffected
	files2, _ := s.ListFiles("app2", "stable")
	if len(files2) != 1 {
		t.Errorf("app2 should still have 1 file after deleting from app1, got %d", len(files2))
	}
}

// ── Cleanup ──

func writeTestFeed(t *testing.T, s *Store, appId, channel, filename string, versions []string) {
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
		s.EnsureDir(appId, channel)
		os.WriteFile(filepath.Join(s.ChannelDir(appId, channel), "App-"+v+"-full.nupkg"), []byte("data-"+v), 0o644)
	}
	feed := &model.ReleaseFeed{Assets: assets}
	if err := s.WriteReleaseFeed(appId, channel, filename, feed); err != nil {
		t.Fatalf("writeTestFeed failed: %v", err)
	}
}

func TestCleanupOldVersions_FewerThan10(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	versions := []string{"1.0.0", "1.0.1", "1.0.2"}
	writeTestFeed(t, s, "docufiller", "beta", "releases.win.json", versions)

	removed, err := s.CleanupOldVersions("docufiller", "beta", 10, "releases.win.json")
	if err != nil {
		t.Fatalf("CleanupOldVersions failed: %v", err)
	}
	if len(removed) != 0 {
		t.Errorf("expected 0 removed, got %d: %v", len(removed), removed)
	}

	files, _ := s.ListFiles("docufiller", "beta")
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
	writeTestFeed(t, s, "docufiller", "beta", "releases.win.json", versions)

	removed, err := s.CleanupOldVersions("docufiller", "beta", 10, "releases.win.json")
	if err != nil {
		t.Fatalf("CleanupOldVersions failed: %v", err)
	}
	if len(removed) != 1 {
		t.Fatalf("expected 1 removed version, got %d: %v", len(removed), removed)
	}
	if removed[0] != "1.0.0" {
		t.Errorf("expected 1.0.0 removed, got %s", removed[0])
	}

	files, _ := s.ListFiles("docufiller", "beta")
	for _, f := range files {
		if f == "App-1.0.0-full.nupkg" {
			t.Error("1.0.0 file should have been deleted")
		}
	}
	if len(files) != 10 {
		t.Errorf("expected 10 remaining files, got %d", len(files))
	}

	feed, _ := s.ReadReleaseFeed("docufiller", "beta", "releases.win.json")
	for _, a := range feed.Assets {
		if a.Version == "1.0.0" {
			t.Error("feed should not contain version 1.0.0 after cleanup")
		}
	}
}

func TestCleanupOldVersions_EmptyFeed(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	s.EnsureDir("docufiller", "beta")

	removed, err := s.CleanupOldVersions("docufiller", "beta", 10, "releases.win.json")
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

	versions := make([]string, 15)
	for i := 0; i < 15; i++ {
		versions[i] = fmt.Sprintf("1.0.%d", i)
	}
	writeTestFeed(t, s, "myapp", "stable", "releases.win.json", versions)

	removed, err := s.CleanupOldVersions("myapp", "stable", 10, "releases.win.json")
	if err != nil {
		t.Fatalf("CleanupOldVersions failed: %v", err)
	}
	if len(removed) != 5 {
		t.Fatalf("expected 5 removed, got %d: %v", len(removed), removed)
	}

	feed, _ := s.ReadReleaseFeed("myapp", "stable", "releases.win.json")
	if len(feed.Assets) != 10 {
		t.Errorf("expected 10 assets in feed, got %d", len(feed.Assets))
	}

	files, _ := s.ListFiles("myapp", "stable")
	if len(files) != 10 {
		t.Errorf("expected 10 .nupkg files, got %d", len(files))
	}
}

func TestCleanupOldVersions_DoesNotAffectOtherApps(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	// App1: 15 versions → should clean 5
	versions15 := make([]string, 15)
	for i := range versions15 {
		versions15[i] = fmt.Sprintf("1.0.%d", i)
	}
	writeTestFeed(t, s, "app1", "stable", "releases.win.json", versions15)

	// App2: 3 versions → should not clean anything
	writeTestFeed(t, s, "app2", "stable", "releases.win.json", []string{"1.0.0", "1.0.1", "1.0.2"})

	s.CleanupOldVersions("app1", "stable", 10, "releases.win.json")

	// App2 should be untouched
	files2, _ := s.ListFiles("app2", "stable")
	if len(files2) != 3 {
		t.Errorf("app2 should still have 3 files, got %d", len(files2))
	}
	feed2, _ := s.ReadReleaseFeed("app2", "stable", "releases.win.json")
	if len(feed2.Assets) != 3 {
		t.Errorf("app2 feed should still have 3 assets, got %d", len(feed2.Assets))
	}
}

func TestCleanupOldVersions_LinuxFeed(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	versions := make([]string, 12)
	for i := range versions {
		versions[i] = fmt.Sprintf("2.0.%d", i)
	}
	writeTestFeed(t, s, "myapp", "stable", "releases.linux.json", versions)

	removed, err := s.CleanupOldVersions("myapp", "stable", 10, "releases.linux.json")
	if err != nil {
		t.Fatalf("CleanupOldVersions with linux feed failed: %v", err)
	}
	if len(removed) != 2 {
		t.Fatalf("expected 2 removed, got %d: %v", len(removed), removed)
	}

	feed, _ := s.ReadReleaseFeed("myapp", "stable", "releases.linux.json")
	if len(feed.Assets) != 10 {
		t.Errorf("expected 10 assets in linux feed, got %d", len(feed.Assets))
	}
}

// ── Semver comparison ──

func TestCompareSemver(t *testing.T) {
	tests := []struct {
		a, b string
		want int
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
