package storage

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"update-hub/model"
)

// Store provides file-system-backed storage for release artifacts.
// Directory layout: data/{appId}/{channel}/
type Store struct {
	DataDir string
}

// NewStore creates a Store rooted at dataDir.
func NewStore(dataDir string) *Store {
	return &Store{DataDir: dataDir}
}

// AppDir returns the absolute path for an app's root directory.
func (s *Store) AppDir(appId string) string {
	return filepath.Join(s.DataDir, appId)
}

// ChannelDir returns the absolute path for a channel's directory under an app.
func (s *Store) ChannelDir(appId, channel string) string {
	return filepath.Join(s.DataDir, appId, channel)
}

// feedPath returns the path to a releases JSON file for a given app/channel/filename.
func (s *Store) feedPath(appId, channel, filename string) string {
	return filepath.Join(s.ChannelDir(appId, channel), filename)
}

// EnsureDir creates the channel directory (and any parents) if it does not exist.
func (s *Store) EnsureDir(appId, channel string) error {
	dir := s.ChannelDir(appId, channel)
	return os.MkdirAll(dir, 0o755)
}

// ReadReleaseFeed parses the given releases.{os}.json for the app/channel.
// Returns an empty feed (with empty Assets slice) if the file does not exist.
func (s *Store) ReadReleaseFeed(appId, channel, filename string) (*model.ReleaseFeed, error) {
	path := s.feedPath(appId, channel, filename)
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return &model.ReleaseFeed{Assets: []model.ReleaseAsset{}}, nil
		}
		return nil, fmt.Errorf("read release feed %s/%s/%s: %w", appId, channel, filename, err)
	}
	var feed model.ReleaseFeed
	if err := json.Unmarshal(data, &feed); err != nil {
		return nil, fmt.Errorf("parse release feed %s/%s/%s: %w", appId, channel, filename, err)
	}
	if feed.Assets == nil {
		feed.Assets = []model.ReleaseAsset{}
	}
	return &feed, nil
}

// WriteReleaseFeed writes the releases JSON file atomically for the given app/channel/filename.
// It writes to a temp file first, then renames to avoid partial reads.
func (s *Store) WriteReleaseFeed(appId, channel, filename string, feed *model.ReleaseFeed) error {
	if err := s.EnsureDir(appId, channel); err != nil {
		return fmt.Errorf("ensure channel dir for write: %w", err)
	}
	data, err := json.MarshalIndent(feed, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal release feed: %w", err)
	}
	path := s.feedPath(appId, channel, filename)
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return fmt.Errorf("write temp feed: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename temp feed: %w", err)
	}
	return nil
}

// ListFiles returns all .nupkg filenames in the app/channel directory.
func (s *Store) ListFiles(appId, channel string) ([]string, error) {
	dir := s.ChannelDir(appId, channel)
	entries, err := os.ReadDir(dir)
	if err != nil {
		if os.IsNotExist(err) {
			return []string{}, nil
		}
		return nil, fmt.Errorf("list files in %s/%s: %w", appId, channel, err)
	}
	var files []string
	for _, e := range entries {
		if e.IsDir() {
			continue
		}
		if strings.HasSuffix(e.Name(), ".nupkg") {
			files = append(files, e.Name())
		}
	}
	return files, nil
}

// WriteFile writes data to a file in the app/channel directory.
// It writes to a temp file first, then renames for atomicity.
func (s *Store) WriteFile(appId, channel, filename string, data []byte) error {
	if err := s.EnsureDir(appId, channel); err != nil {
		return fmt.Errorf("ensure channel dir for write: %w", err)
	}
	path := filepath.Join(s.ChannelDir(appId, channel), filename)
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return fmt.Errorf("write temp file: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename temp file: %w", err)
	}
	return nil
}

// ReadFile reads a single file from an app/channel directory.
// Returns an error if the file does not exist.
func (s *Store) ReadFile(appId, channel, filename string) ([]byte, error) {
	path := filepath.Join(s.ChannelDir(appId, channel), filename)
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read file %s/%s/%s: %w", appId, channel, filename, err)
	}
	return data, nil
}

// ListFeedFiles returns all releases.*.json filenames in the app/channel directory.
func (s *Store) ListFeedFiles(appId, channel string) ([]string, error) {
	dir := s.ChannelDir(appId, channel)
	entries, err := os.ReadDir(dir)
	if err != nil {
		if os.IsNotExist(err) {
			return []string{}, nil
		}
		return nil, fmt.Errorf("list feed files in %s/%s: %w", appId, channel, err)
	}
	var feeds []string
	for _, e := range entries {
		if !e.IsDir() && model.IsFeedFilename(e.Name()) {
			feeds = append(feeds, e.Name())
		}
	}
	return feeds, nil
}

// DeleteVersion removes all .nupkg files matching the given version pattern
// for a specific app/channel. A file matches if its name contains "-{version}-"
// (Velopack naming convention).
func (s *Store) DeleteVersion(appId, channel, version string) error {
	files, err := s.ListFiles(appId, channel)
	if err != nil {
		return err
	}
	dir := s.ChannelDir(appId, channel)
	suffix := "-" + version + "-"
	deleted := 0
	for _, f := range files {
		if strings.Contains(f, suffix) {
			if err := os.Remove(filepath.Join(dir, f)); err != nil {
				return fmt.Errorf("delete %s/%s/%s: %w", appId, channel, f, err)
			}
			deleted++
		}
	}
	_ = deleted // used for logging in caller
	return nil
}
