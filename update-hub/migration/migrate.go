// Package migration handles data migration from the old single-app directory layout
// (data/{channel}/) to the new multi-app layout (data/{appId}/{channel}/),
// and syncs file-system release data into SQLite metadata.
package migration

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"

	"update-hub/database"
	"update-hub/model"
)

// Migrate detects old-format channel directories under dataDir and moves them
// under the app-specific subdirectory identified by appId.
//
// Old format: data/{channel}/            (e.g. data/stable/)
// New format: data/{appId}/{channel}/    (e.g. data/docufiller/stable/)
//
// Detection heuristic: an immediate subdirectory of dataDir is considered an
// old-format channel dir if it contains at least one release feed file
// matching the pattern releases.*.json.
//
// Directories that contain only subdirectories (new-format app dirs) or
// special files (.db, root-level .json) are not treated as channel dirs.
//
// Migration is atomic per channel: os.Rename is used after ensuring the
// target parent directory exists. If the target data/{appId}/{channel}/
// already exists, that channel is skipped (idempotent).
func Migrate(dataDir, appId string) error {
	entries, err := os.ReadDir(dataDir)
	if err != nil {
		if os.IsNotExist(err) {
			log.Printf(`{"event":"migration_skip","reason":"data_dir_not_found","dir":"%s"}`, dataDir)
			return nil
		}
		return fmt.Errorf("read data dir %s: %w", dataDir, err)
	}

	migrated := 0
	skipped := 0

	for _, entry := range entries {
		if !entry.IsDir() {
			continue
		}
		channelDir := filepath.Join(dataDir, entry.Name())

		if !isOldFormatChannelDir(channelDir) {
			continue
		}

		channel := entry.Name()
		targetDir := filepath.Join(dataDir, appId, channel)

		// Idempotent: skip if target already exists
		if _, err := os.Stat(targetDir); err == nil {
			log.Printf(`{"event":"migration_skip","channel":"%s","reason":"target_exists","target":"%s"}`, channel, targetDir)
			skipped++
			continue
		}

		// Ensure parent of target exists
		if err := os.MkdirAll(filepath.Dir(targetDir), 0o755); err != nil {
			return fmt.Errorf("create app dir for migration: %w", err)
		}

		log.Printf(`{"event":"migration_move","channel":"%s","src":"%s","dst":"%s"}`, channel, channelDir, targetDir)
		if err := os.Rename(channelDir, targetDir); err != nil {
			log.Printf(`{"event":"migration_move_error","channel":"%s","error":"%s"}`, channel, err)
			return fmt.Errorf("migrate channel %s: %w", channel, err)
		}
		migrated++
	}

	log.Printf(`{"event":"migration_complete","appId":"%s","migrated":%d,"skipped":%d}`, appId, migrated, skipped)
	return nil
}

// isOldFormatChannelDir returns true if dir contains at least one release
// feed file (releases.*.json). This distinguishes old-format channel dirs
// from new-format app dirs (which contain only subdirectories).
func isOldFormatChannelDir(dir string) bool {
	entries, err := os.ReadDir(dir)
	if err != nil {
		return false
	}
	for _, e := range entries {
		if !e.IsDir() && model.IsFeedFilename(e.Name()) {
			return true
		}
	}
	return false
}

// SyncMetadata scans all data/{appId}/{channel}/ directories under dataDir,
// parses every release feed file, and upserts version metadata into the
// SQLite database. This is used after migration (or for any pre-existing
// data that lacks metadata) to ensure the database reflects the file system.
//
// Uses INSERT OR REPLACE via db.UpsertVersion for idempotency.
func SyncMetadata(dataDir string, db *database.DB) error {
	log.Printf(`{"event":"sync_metadata_start","dir":"%s"}`, dataDir)

	appEntries, err := os.ReadDir(dataDir)
	if err != nil {
		if os.IsNotExist(err) {
			log.Printf(`{"event":"sync_metadata_skip","reason":"data_dir_not_found"}`)
			return nil
		}
		return fmt.Errorf("read data dir %s: %w", dataDir, err)
	}

	totalVersions := 0

	for _, appEntry := range appEntries {
		if !appEntry.IsDir() {
			continue
		}
		appId := appEntry.Name()
		appDir := filepath.Join(dataDir, appId)

		channelEntries, err := os.ReadDir(appDir)
		if err != nil {
			log.Printf(`{"event":"sync_metadata_error","appId":"%s","error":"%s"}`, appId, err)
			return fmt.Errorf("read app dir %s: %w", appId, err)
		}

		log.Printf(`{"event":"sync_metadata_app","appId":"%s","channels":%d}`, appId, len(channelEntries))

		for _, chEntry := range channelEntries {
			if !chEntry.IsDir() {
				continue
			}
			channel := chEntry.Name()
			channelDir := filepath.Join(appDir, channel)

			count, err := syncChannelDir(channelDir, appId, channel, db)
			if err != nil {
				return err
			}
			totalVersions += count
		}
	}

	log.Printf(`{"event":"sync_metadata_complete","totalVersions":%d}`, totalVersions)
	return nil
}

// syncChannelDir processes all feed files in a single channel directory,
// extracting version metadata and upserting into the database.
func syncChannelDir(channelDir, appId, channel string, db *database.DB) (int, error) {
	entries, err := os.ReadDir(channelDir)
	if err != nil {
		if os.IsNotExist(err) {
			return 0, nil
		}
		return 0, fmt.Errorf("read channel dir %s/%s: %w", appId, channel, err)
	}

	count := 0
	ctx := context.Background()

	for _, e := range entries {
		if e.IsDir() || !model.IsFeedFilename(e.Name()) {
			continue
		}

		feedPath := filepath.Join(channelDir, e.Name())
		data, err := os.ReadFile(feedPath)
		if err != nil {
			log.Printf(`{"event":"sync_metadata_error","file":"%s","error":"%s"}`, feedPath, err)
			return 0, fmt.Errorf("read feed %s: %w", feedPath, err)
		}

		var feed model.ReleaseFeed
		if err := json.Unmarshal(data, &feed); err != nil {
			log.Printf(`{"event":"sync_metadata_error","file":"%s","error":"parse failed: %s"}`, feedPath, err)
			return 0, fmt.Errorf("parse feed %s: %w", feedPath, err)
		}

		for _, asset := range feed.Assets {
			if asset.Version == "" {
				continue
			}
			if err := db.UpsertVersion(ctx, appId, channel, asset.Version, buildNotes(&asset)); err != nil {
				return 0, fmt.Errorf("upsert version %s/%s/%s: %w", appId, channel, asset.Version, err)
			}
			count++
		}
	}

	return count, nil
}

// buildNotes constructs a notes string from asset metadata for database storage.
func buildNotes(asset *model.ReleaseAsset) string {
	parts := []string{}
	if asset.Type != "" {
		parts = append(parts, fmt.Sprintf("type=%s", asset.Type))
	}
	if asset.FileName != "" {
		parts = append(parts, fmt.Sprintf("file=%s", asset.FileName))
	}
	if asset.Size > 0 {
		parts = append(parts, fmt.Sprintf("size=%d", asset.Size))
	}
	return strings.Join(parts, "; ")
}
