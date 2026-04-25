package storage

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"docufiller-update-server/model"
)

// Store provides file-system-backed storage for release artifacts.
type Store struct {
	DataDir string
}

// NewStore creates a Store rooted at dataDir.
func NewStore(dataDir string) *Store {
	return &Store{DataDir: dataDir}
}

// ChannelDir returns the absolute path for a channel's directory.
func (s *Store) ChannelDir(channel string) string {
	return filepath.Join(s.DataDir, channel)
}

// feedPath returns the path to the releases JSON file for a channel.
func (s *Store) feedPath(channel string) string {
	return filepath.Join(s.ChannelDir(channel), "releases.win.json")
}

// EnsureChannelDir creates the channel directory if it does not exist.
func (s *Store) EnsureChannelDir(channel string) error {
	dir := s.ChannelDir(channel)
	return os.MkdirAll(dir, 0o755)
}

// ReadReleaseFeed parses releases.win.json for the given channel.
// Returns an empty feed (with empty Assets slice) if the file does not exist.
func (s *Store) ReadReleaseFeed(channel string) (*model.ReleaseFeed, error) {
	path := s.feedPath(channel)
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return &model.ReleaseFeed{Assets: []model.ReleaseAsset{}}, nil
		}
		return nil, fmt.Errorf("read release feed %s: %w", channel, err)
	}
	var feed model.ReleaseFeed
	if err := json.Unmarshal(data, &feed); err != nil {
		return nil, fmt.Errorf("parse release feed %s: %w", channel, err)
	}
	if feed.Assets == nil {
		feed.Assets = []model.ReleaseAsset{}
	}
	return &feed, nil
}

// WriteReleaseFeed writes releases.win.json atomically for the given channel.
// It writes to a temp file first, then renames to avoid partial reads.
func (s *Store) WriteReleaseFeed(channel string, feed *model.ReleaseFeed) error {
	if err := s.EnsureChannelDir(channel); err != nil {
		return fmt.Errorf("ensure channel dir for write: %w", err)
	}
	data, err := json.MarshalIndent(feed, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal release feed: %w", err)
	}
	path := s.feedPath(channel)
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return fmt.Errorf("write temp feed: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename temp feed: %w", err)
	}
	return nil
}

// ListFiles returns all .nupkg filenames in the channel directory.
func (s *Store) ListFiles(channel string) ([]string, error) {
	dir := s.ChannelDir(channel)
	entries, err := os.ReadDir(dir)
	if err != nil {
		if os.IsNotExist(err) {
			return []string{}, nil
		}
		return nil, fmt.Errorf("list files in %s: %w", channel, err)
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

// WriteFile writes data to a file in the channel directory.
// It writes to a temp file first, then renames for atomicity.
func (s *Store) WriteFile(channel, filename string, data []byte) error {
	if err := s.EnsureChannelDir(channel); err != nil {
		return fmt.Errorf("ensure channel dir for write: %w", err)
	}
	path := filepath.Join(s.ChannelDir(channel), filename)
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return fmt.Errorf("write temp file: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename temp file: %w", err)
	}
	return nil
}

// ReadFile reads a single file from a channel directory.
// Returns an error if the file does not exist.
func (s *Store) ReadFile(channel, filename string) ([]byte, error) {
	path := filepath.Join(s.ChannelDir(channel), filename)
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read file %s/%s: %w", channel, filename, err)
	}
	return data, nil
}

// DeleteVersion removes all .nupkg files matching the given version pattern.
// A file matches if its name contains "-{version}-" (Velopack naming convention).
func (s *Store) DeleteVersion(channel, version string) error {
	files, err := s.ListFiles(channel)
	if err != nil {
		return err
	}
	dir := s.ChannelDir(channel)
	suffix := "-" + version + "-"
	deleted := 0
	for _, f := range files {
		if strings.Contains(f, suffix) {
			if err := os.Remove(filepath.Join(dir, f)); err != nil {
				return fmt.Errorf("delete %s/%s: %w", channel, f, err)
			}
			deleted++
		}
	}
	_ = deleted // used for logging in caller
	return nil
}
