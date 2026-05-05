---
id: T01
parent: S04
milestone: M023
key_files:
  - update-hub/migration/migrate.go
  - update-hub/migration/migrate_test.go
key_decisions:
  - Used os.Rename for atomic per-channel migration rather than copy+delete
  - Used context.Background() in SyncMetadata since it has no request-scoped lifecycle; callers can wrap with timeout if needed
duration: 
verification_result: passed
completed_at: 2026-05-05T07:39:48.390Z
blocker_discovered: false
---

# T01: Implement migration package with old-format directory detection, atomic file move, and SQLite metadata sync

**Implement migration package with old-format directory detection, atomic file move, and SQLite metadata sync**

## What Happened

Created the `update-hub/migration` package with two main functions:

**`Migrate(dataDir, appId string) error`**: Scans `dataDir` for immediate subdirectories containing release feed files (`releases.*.json`), which indicate old-format channel directories (e.g. `data/stable/`). Each detected channel is atomically moved via `os.Rename` to the new layout `data/{appId}/{channel}/`. Idempotent — skips channels where the target directory already exists. Emits structured JSON logs: `migration_skip`, `migration_move`, `migration_complete`, `migration_move_error`.

**`SyncMetadata(dataDir string, db *database.DB) error`**: Scans all `data/{appId}/{channel}/` directories for feed files, parses each `releases.*.json`, and upserts version metadata into SQLite via `db.UpsertVersion` (INSERT OR REPLACE for idempotency). Emits structured JSON logs: `sync_metadata_start`, `sync_metadata_app`, `sync_metadata_complete`, `sync_metadata_error`.

Also includes `buildNotes` helper for constructing version notes from asset metadata and `isOldFormatChannelDir` for the detection heuristic.

Wrote 17 tests covering: old-format detection and migration, idempotency (skip existing targets), empty/nonexistent data dirs, new-format dirs not being migrated, special-file-only dirs not being migrated, .nupkg file preservation during move, basic metadata upsert, multiple apps, idempotent sync, empty feeds, empty-version asset skipping, multiple feed files (dedup), full Migrate+SyncMetadata integration, and buildNotes unit tests.

One adaptation: Go 1.22 doesn't have `t.Context()` (Go 1.24+), so used `context.Background()` in tests instead.

## Verification

Ran `GOCACHE=/tmp/go-cache go test ./migration/... -v -count=1` — all 17 tests (including sub-tests) pass. Verified:
- Old-format directories correctly detected and moved to new layout
- Idempotency: second migration skips channels with existing targets
- New-format app dirs (containing only subdirectories) are not migrated
- Directories with only .db/.json special files are not treated as channel dirs
- .nupkg files preserved during atomic rename
- SyncMetadata correctly upserts versions into SQLite for multiple apps/channels
- Empty feeds and empty-version assets handled gracefully
- Multiple feed files per channel deduplicated via INSERT OR REPLACE
- Full integration: Migrate then SyncMetadata produces correct directory structure and DB records
- All structured log events match the slice verification spec

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && GOCACHE=/tmp/go-cache go test ./migration/... -v -count=1` | 0 | ✅ pass | 30235ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `update-hub/migration/migrate.go`
- `update-hub/migration/migrate_test.go`
