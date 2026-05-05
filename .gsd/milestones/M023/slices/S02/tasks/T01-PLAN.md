---
estimated_steps: 1
estimated_files: 4
skills_used: []
---

# T01: Create database package with SQLite schema and CRUD

Create a self-contained `database` package with SQLite initialization, schema migration, and CRUD operations for apps and versions metadata. Use `modernc.org/sqlite` (pure Go, no CGO) to avoid GCC dependency on Windows. Schema has two tables: `apps` (id TEXT PK, created_at) and `versions` (id INTEGER PK AUTOINCREMENT, app_id, channel, version, notes, created_at, UNIQUE(app_id, channel, version)). CRUD methods: Init (open DB with WAL mode, run migrations), Close, UpsertApp, GetApps (with channel lists derived from versions), UpsertVersion, GetVersions(appId, channel), DeleteVersion(appId, channel, version). All methods use parameterized queries to prevent SQL injection.

## Inputs

- `update-hub/go.mod`
- `update-hub/model/release.go`

## Expected Output

- `update-hub/database/db.go`
- `update-hub/database/db_test.go`
- `update-hub/go.mod`
- `update-hub/go.sum`

## Verification

cd update-hub && go test ./database/ -v -count=1
