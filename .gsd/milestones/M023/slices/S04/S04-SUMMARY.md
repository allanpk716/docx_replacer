---
id: S04
parent: M023
milestone: M023
provides:
  - ["migration package (Migrate + SyncMetadata functions)", "main.go migration integration with -migrate-app-id flag", "NSSM deployment scripts (install/uninstall/start/stop)", "Deployment README with complete Windows Server 2019 instructions"]
requires:
  - slice: S01
    provides: storage/store.go Store (filesystem operations), model/release.go (feed data model)
  - slice: S02
    provides: database/db.go DB (SQLite CRUD with UpsertVersion)
affects:
  []
key_files:
  - ["update-hub/migration/migrate.go", "update-hub/migration/migrate_test.go", "update-hub/main.go", "update-hub/deploy/install-service.bat", "update-hub/deploy/uninstall-service.bat", "update-hub/deploy/start-service.bat", "update-hub/deploy/stop-service.bat", "update-hub/deploy/README.md"]
key_decisions:
  - ["os.Rename for atomic per-channel migration (not copy+delete)", "SyncMetadata runs unconditionally to ensure DB/filesystem consistency", "Migration errors are fatal (log.Fatalf) to prevent serving inconsistent state", "-migrate-app-id flag defaults to 'docufiller', empty string skips migration", "Go 1.22 compatibility: used context.Background() instead of t.Context() in tests"]
patterns_established:
  - ["Structured JSON logging for all migration lifecycle events (migration_start/move/skip/complete, sync_metadata_start/app/complete)", "Idempotent operations with skip-not-error pattern", "Unconditional post-migration metadata sync for consistency"]
observability_surfaces:
  - ["Structured JSON logs: migration_start, migration_skip, migration_move, migration_complete, migration_move_error, sync_metadata_start, sync_metadata_app, sync_metadata_complete, sync_metadata_error", "NSSM log rotation: 10MB per file, 5 copies retained in logs/ directory"]
drill_down_paths:
  - [".gsd/milestones/M023/slices/S04/tasks/T01-SUMMARY.md", ".gsd/milestones/M023/slices/S04/tasks/T02-SUMMARY.md", ".gsd/milestones/M023/slices/S04/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-05T07:44:42.388Z
blocker_discovered: false
---

# S04: 数据迁移 + Windows Server 部署

**Atomic old-format data migration with SQLite metadata sync, plus NSSM deployment scripts and README for Windows Server 2019**

## What Happened

## What Happened

This slice delivered the data migration pipeline and Windows Server deployment tooling for update-hub.

**Migration package** (`migration/migrate.go`): Two core functions — `Migrate(dataDir, appId)` detects old single-app directories (e.g. `data/stable/`) by scanning for release feed files, atomically moves them to the new multi-app layout (`data/{appId}/{channel}/`) via `os.Rename`, and skips existing targets for idempotency. `SyncMetadata(dataDir, db)` scans all app/channel directories, parses feed files, and upserts version metadata into SQLite via `INSERT OR REPLACE`. Both emit structured JSON logs for observability (migration_start, migration_move, migration_skip, migration_complete, sync_metadata_start, sync_metadata_app, sync_metadata_complete).

**main.go integration**: Added `-migrate-app-id` flag (default "docufiller", empty = skip). Startup order: parse flags → create Store → init SQLite → Migrate (if flag non-empty) → SyncMetadata (always) → create handlers → start server. Migration errors are fatal to prevent serving with inconsistent data.

**NSSM deployment scripts**: 4 batch scripts (install, uninstall, start, stop) that register update-hub as a Windows service with auto-start, 5s restart delay, and log rotation. install-service.bat checks for admin privileges, prompts for token/password, and configures all NSSM parameters.

**Deployment README**: Complete guide covering prerequisites, CLI arguments, install steps, verification, automatic migration, update procedure, and log management.

17 migration tests cover all critical paths including idempotency, empty dirs, special-file filtering, .nupkg preservation, metadata upsert, and full Migrate+SyncMetadata integration. All 5 test packages pass (database, handler, middleware, migration, storage).

## Verification

## Verification Across All Tasks

| # | Check | Result |
|---|-------|--------|
| 1 | Deploy files exist (4 .bat + README.md) | ✅ pass |
| 2 | Migration source files exist (migrate.go, migrate_test.go) | ✅ pass |
| 3 | Full test suite: `go test ./... -count=1` — 5 packages, 0 failures | ✅ pass |
| 4 | Build: `go build -o update-hub.exe .` | ✅ pass |
| 5 | Migration tests: 17 tests covering detection, move, idempotency, sync | ✅ pass |

Note: Original slice plan verification used `test -f deploy/README.md` which fails on Windows CMD. Adapted to `ls deploy/README.md` for Git Bash compatibility.

## Requirements Advanced

- R074 — Migrate() + SyncMetadata() implemented with 17 tests proving old-format detection, atomic move, idempotency, and SQLite metadata sync
- R075 — NSSM deploy scripts (install/uninstall/start/stop) + comprehensive README with prerequisites, CLI args, install steps, and log rotation config

## Requirements Validated

- R074 — 17 migration tests pass; Migrate detects old-format dirs via feed file presence, atomic os.Rename move, idempotent skip; SyncMetadata upserts parsed feed metadata via INSERT OR REPLACE; build + full test suite pass
- R075 — 4 NSSM batch scripts exist with correct syntax; install-service.bat configures auto-start, 5s restart delay, 10MB×5 log rotation; README documents full deployment procedure for Windows Server 2019 on port 30001

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Adapted verification commands from Linux-style (test -f, GOCACHE=/tmp/go-cache) to Windows Git Bash compatible syntax (ls, GOCACHE="$TEMP/go-cache"). Go 1.22 doesn't have t.Context() — used context.Background() in tests.

## Known Limitations

["NSSM deployment scripts require actual Windows Server 2019 for live validation", "Real DocuFiller data migration (~1.3GB) not tested in CI — only synthetic test data used", "Cross-filesystem migration (rename across drives) not supported — os.Rename fails across volumes"]

## Follow-ups

["Live deployment on Windows Server 2019 to validate NSSM scripts end-to-end", "Real data migration test with production DocuFiller stable/beta data"]

## Files Created/Modified

None.
