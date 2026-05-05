---
estimated_steps: 6
estimated_files: 2
skills_used: []
---

# T01: Implement migration package with old-format detection, file move, and metadata sync

Create the migration package that handles the core data migration from old single-app format to new multi-app format.

The old DocuFiller server stored data as `data/{channel}/` (e.g. `data/stable/`, `data/beta/`). The new update-hub uses `data/{appId}/{channel}/` (e.g. `data/docufiller/stable/`). This task builds the migration logic with comprehensive tests.

Three main functions:
1. `Migrate(dataDir, appId string) error` — detects old-format directories and moves them under the app directory. Detection heuristic: scan dataDir for immediate subdirectories that contain release feed files (`releases.*.json`) — these are old-format channel dirs. Skip directories that contain only subdirectories (new-format app dirs) or special files (`.db`, `.json`). Move is atomic per channel: create target parent, rename source to target.
2. `SyncMetadata(dataDir string, db *database.DB) error` — scans ALL `data/{appId}/{channel}/` directories for feed files, parses them, and upserts version metadata into SQLite. This ensures SQLite is populated after migration (or for any pre-existing data without metadata).
3. Idempotency: if `data/{appId}/{channel}/` already exists, skip that channel. SyncMetadata uses INSERT OR IGNORE semantics via db.UpsertVersion.

## Inputs

- `update-hub/storage/store.go`
- `update-hub/model/release.go`
- `update-hub/database/db.go`

## Expected Output

- `update-hub/migration/migrate.go`
- `update-hub/migration/migrate_test.go`

## Verification

GOCACHE=/tmp/go-cache go test ./migration/... -v -count=1
