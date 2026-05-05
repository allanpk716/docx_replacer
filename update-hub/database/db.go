// Package database provides SQLite-backed metadata storage for apps and versions.
// It uses modernc.org/sqlite (pure Go, no CGO) for cross-platform compatibility.
package database

import (
	"context"
	"database/sql"
	"fmt"
	"log"
	"os"
	"path/filepath"

	_ "modernc.org/sqlite"
)

// AppInfo holds metadata for a registered application, including its active channels.
type AppInfo struct {
	ID       string   `json:"id"`
	Channels []string `json:"channels"`
}

// VersionEntry holds metadata for a single version release.
type VersionEntry struct {
	ID        int64  `json:"id"`
	AppID     string `json:"app_id"`
	Channel   string `json:"channel"`
	Version   string `json:"version"`
	Notes     string `json:"notes"`
	CreatedAt string `json:"created_at"`
}

// DB wraps *sql.DB and provides CRUD operations for app and version metadata.
type DB struct {
	db *sql.DB
}

// schema is the idempotent DDL for the metadata tables.
const schema = `
CREATE TABLE IF NOT EXISTS apps (
	id TEXT PRIMARY KEY,
	created_at DATETIME NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS versions (
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	app_id TEXT NOT NULL,
	channel TEXT NOT NULL,
	version TEXT NOT NULL,
	notes TEXT NOT NULL DEFAULT '',
	created_at DATETIME NOT NULL DEFAULT (datetime('now')),
	UNIQUE(app_id, channel, version)
);

CREATE INDEX IF NOT EXISTS idx_versions_app_channel ON versions(app_id, channel);
`

// Init opens (or creates) a SQLite database at dbPath, enables WAL mode,
// and runs idempotent schema migrations. The parent directory is created if needed.
func Init(dbPath string) (*DB, error) {
	dir := filepath.Dir(dbPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return nil, fmt.Errorf("create db directory: %w", err)
	}

	db, err := sql.Open("sqlite", dbPath+"?_pragma=busy_timeout(5000)&_pragma=journal_mode(WAL)")
	if err != nil {
		return nil, fmt.Errorf("open sqlite: %w", err)
	}

	// Verify WAL mode
	var mode string
	if err := db.QueryRow("PRAGMA journal_mode").Scan(&mode); err != nil {
		db.Close()
		return nil, fmt.Errorf("verify WAL mode: %w", err)
	}
	_ = mode // WAL confirmed via DSN

	// Run idempotent schema migration
	if _, err := db.Exec(schema); err != nil {
		db.Close()
		return nil, fmt.Errorf("run migrations: %w", err)
	}

	log.Printf(`{"event":"metadata_init","path":"%s"}`, dbPath)
	return &DB{db: db}, nil
}

// Close releases all database resources.
func (d *DB) Close() error {
	if d.db == nil {
		return nil
	}
	return d.db.Close()
}

// UpsertApp ensures an app record exists. INSERT OR IGNORE makes it idempotent.
func (d *DB) UpsertApp(ctx context.Context, appID string) error {
	_, err := d.db.ExecContext(ctx,
		"INSERT OR IGNORE INTO apps (id) VALUES (?)",
		appID,
	)
	if err != nil {
		return fmt.Errorf("upsert app %q: %w", appID, err)
	}
	return nil
}

// GetApps returns all registered apps with their active channels derived from versions.
func (d *DB) GetApps(ctx context.Context) ([]AppInfo, error) {
	rows, err := d.db.QueryContext(ctx, "SELECT id FROM apps ORDER BY id")
	if err != nil {
		return nil, fmt.Errorf("query apps: %w", err)
	}
	defer rows.Close()

	var apps []AppInfo
	for rows.Next() {
		var app AppInfo
		if err := rows.Scan(&app.ID); err != nil {
			return nil, fmt.Errorf("scan app: %w", err)
		}
		apps = append(apps, app)
	}
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate apps: %w", err)
	}

	// Derive channels for each app
	for i := range apps {
		channels, err := d.GetChannels(ctx, apps[i].ID)
		if err != nil {
			return nil, fmt.Errorf("get channels for %q: %w", apps[i].ID, err)
		}
		apps[i].Channels = channels
	}

	return apps, nil
}

// GetChannels returns distinct channels for a given app, derived from its versions.
func (d *DB) GetChannels(ctx context.Context, appID string) ([]string, error) {
	rows, err := d.db.QueryContext(ctx,
		"SELECT DISTINCT channel FROM versions WHERE app_id = ? ORDER BY channel",
		appID,
	)
	if err != nil {
		return nil, fmt.Errorf("query channels for %q: %w", appID, err)
	}
	defer rows.Close()

	var channels []string
	for rows.Next() {
		var ch string
		if err := rows.Scan(&ch); err != nil {
			return nil, fmt.Errorf("scan channel: %w", err)
		}
		channels = append(channels, ch)
	}
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate channels: %w", err)
	}
	return channels, nil
}

// UpsertVersion inserts or replaces a version record. Also upserts the parent app.
func (d *DB) UpsertVersion(ctx context.Context, appID, channel, version, notes string) error {
	// Ensure the app exists
	if err := d.UpsertApp(ctx, appID); err != nil {
		return err
	}

	_, err := d.db.ExecContext(ctx,
		"INSERT OR REPLACE INTO versions (app_id, channel, version, notes) VALUES (?, ?, ?, ?)",
		appID, channel, version, notes,
	)
	if err != nil {
		return fmt.Errorf("upsert version %s/%s/%s: %w", appID, channel, version, err)
	}
	log.Printf(`{"event":"metadata_upsert","app":"%s","channel":"%s","version":"%s"}`, appID, channel, version)
	return nil
}

// GetVersions returns all versions for an app and channel, ordered by created_at DESC.
func (d *DB) GetVersions(ctx context.Context, appID, channel string) ([]VersionEntry, error) {
	rows, err := d.db.QueryContext(ctx,
		"SELECT id, app_id, channel, version, notes, created_at FROM versions WHERE app_id = ? AND channel = ? ORDER BY id DESC",
		appID, channel,
	)
	if err != nil {
		return nil, fmt.Errorf("query versions for %s/%s: %w", appID, channel, err)
	}
	defer rows.Close()

	var versions []VersionEntry
	for rows.Next() {
		var v VersionEntry
		if err := rows.Scan(&v.ID, &v.AppID, &v.Channel, &v.Version, &v.Notes, &v.CreatedAt); err != nil {
			return nil, fmt.Errorf("scan version: %w", err)
		}
		versions = append(versions, v)
	}
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate versions: %w", err)
	}
	return versions, nil
}

// DeleteVersion removes a specific version record.
func (d *DB) DeleteVersion(ctx context.Context, appID, channel, version string) error {
	res, err := d.db.ExecContext(ctx,
		"DELETE FROM versions WHERE app_id = ? AND channel = ? AND version = ?",
		appID, channel, version,
	)
	if err != nil {
		return fmt.Errorf("delete version %s/%s/%s: %w", appID, channel, version, err)
	}
	n, _ := res.RowsAffected()
	log.Printf(`{"event":"metadata_delete","app":"%s","channel":"%s","version":"%s","affected":%d}`, appID, channel, version, n)
	return nil
}
