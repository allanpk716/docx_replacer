---
estimated_steps: 6
estimated_files: 4
skills_used:
  - test
  - tdd
---

# T01: Create database package with SQLite schema and CRUD

**Slice:** S02 — SQLite 元数据层 + Release notes
**Milestone:** M023

## Description

Create a self-contained `database` package with SQLite initialization, schema migration, and CRUD operations for apps and versions metadata. Use `modernc.org/sqlite` (pure Go, no CGO) to avoid GCC dependency on Windows Server 2019. This package is the foundation that T02 will wire into existing handlers.

## Failure Modes

| Dependency | On error | On timeout | On malformed response |
|------------|----------|-----------|----------------------|
| SQLite file | Return error from Init, server won't start | N/A (local file) | Schema migration handles malformed JSON gracefully |

## Load Profile

- **Shared resources**: SQLite database file — WAL mode enables concurrent reads, serialized writes
- **Per-operation cost**: 1 DB query per metadata call (trivial)
- **10x breakpoint**: SQLite handles ~100K writes/sec in WAL mode; internal app server will never hit this

## Negative Tests

- **Malformed inputs**: Empty appId, very long notes (10KB+), version strings with special characters, concurrent UpsertVersion on same (appId, channel, version)
- **Error paths**: Init with non-existent directory, Close on already-closed DB, operations after Close
- **Boundary conditions**: Empty notes field, duplicate UpsertVersion (should update not error), GetVersions on app with no versions

## Steps

1. Add `modernc.org/sqlite` dependency: `cd update-hub && go get modernc.org/sqlite`
2. Create `update-hub/database/db.go` with package `database`
3. Define DB struct wrapping `*sql.DB`, with `Init(dbPath string) (*DB, error)` that opens SQLite with WAL mode (`PRAGMA journal_mode=WAL`), runs schema migrations
4. Schema migration (idempotent CREATE TABLE IF NOT EXISTS):
   - `apps` table: `id TEXT PRIMARY KEY`, `created_at DATETIME NOT NULL DEFAULT (datetime('now'))`
   - `versions` table: `id INTEGER PRIMARY KEY AUTOINCREMENT`, `app_id TEXT NOT NULL`, `channel TEXT NOT NULL`, `version TEXT NOT NULL`, `notes TEXT NOT NULL DEFAULT ''`, `created_at DATETIME NOT NULL DEFAULT (datetime('now'))`, `UNIQUE(app_id, channel, version)`
   - Index: `CREATE INDEX IF NOT EXISTS idx_versions_app_channel ON versions(app_id, channel)`
5. Implement CRUD methods:
   - `UpsertApp(ctx, appId)` — INSERT OR IGNORE into apps
   - `GetApps(ctx) ([]AppInfo, error)` — returns apps with channels derived via subquery
   - `GetChannels(ctx, appId) ([]string, error)` — SELECT DISTINCT channel
   - `UpsertVersion(ctx, appId, channel, version, notes)` — INSERT OR REPLACE into versions, also UpsertApp
   - `GetVersions(ctx, appId, channel) ([]VersionEntry, error)` — ordered by created_at DESC
   - `DeleteVersion(ctx, appId, channel, version) error`
   - `Close() error`
6. Create `update-hub/database/db_test.go` with comprehensive tests: TestInit, TestUpsertApp, TestGetApps, TestUpsertVersion, TestGetVersions, TestDeleteVersion, TestGetChannels, TestIdempotentMigration, TestConcurrentAccess

## Must-Haves

- [ ] SQLite database opens with WAL mode for concurrent access
- [ ] Schema migration is idempotent (safe to run multiple times)
- [ ] All CRUD methods use parameterized queries (no SQL injection)
- [ ] `modernc.org/sqlite` dependency added to go.mod (pure Go, no CGO)
- [ ] AppInfo type includes derived channels list
- [ ] VersionEntry type includes notes field

## Verification

- `cd update-hub && go test ./database/ -v -count=1` — all tests pass
- `cd update-hub && go vet ./database/` — no warnings

## Inputs

- `update-hub/go.mod` — existing module definition to add SQLite dependency
- `update-hub/model/release.go` — ReleaseAsset type for reference (version extraction patterns)

## Expected Output

- `update-hub/database/db.go` — DB struct, Init, Close, all CRUD methods
- `update-hub/database/db_test.go` — comprehensive test suite
- `update-hub/go.mod` — updated with modernc.org/sqlite dependency
- `update-hub/go.sum` — updated checksums
