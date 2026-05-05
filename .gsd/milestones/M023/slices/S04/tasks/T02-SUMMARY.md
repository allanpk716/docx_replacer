---
id: T02
parent: S04
milestone: M023
key_files:
  - update-hub/main.go
key_decisions:
  - SyncMetadata runs unconditionally (not gated by -migrate-app-id) to ensure SQLite always reflects filesystem state, even when migration is skipped
  - Migration errors are fatal (log.Fatalf) to prevent serving with inconsistent data layout
duration: 
verification_result: passed
completed_at: 2026-05-05T07:41:22.335Z
blocker_discovered: false
---

# T02: Wire migration package into main.go startup with -migrate-app-id CLI flag

**Wire migration package into main.go startup with -migrate-app-id CLI flag**

## What Happened

Integrated the migration package into the server startup sequence in main.go. Added a `-migrate-app-id` CLI flag (default: "docufiller", empty string = skip migration). The startup order is now: parse flags → create Store → init SQLite → run migration.Migrate if flag non-empty → run migration.SyncMetadata → create handlers → start server.

Three structured JSON log events were added at the migration integration points: `migration_start` (before Migrate), `migration_skip` (when flag is empty), and `migration_error`/`sync_metadata_error` (on failure, via log.Fatalf). The SyncMetadata call runs unconditionally after migration to ensure SQLite is always consistent with the file system, even when no migration is needed.

Both build and full test suite pass with zero regressions. The previous verification failure was due to Linux-style `GOCACHE=/tmp/go-cache` syntax on Windows — resolved by using `$TEMP/go-cache`.

## Verification

Ran full build (`go build -o update-hub.exe .`) — compiles successfully with migration import and flag. Ran full test suite (`go test ./... -count=1`) — all 5 test packages pass (database, handler, middleware, migration, storage). No regressions. The main package correctly wires migration.Migrate and migration.SyncMetadata into the startup sequence between database init and handler creation.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && GOCACHE="$TEMP/go-cache" go build -o update-hub.exe .` | 0 | ✅ pass | 3146ms |
| 2 | `cd update-hub && GOCACHE="$TEMP/go-cache" go test ./... -count=1` | 0 | ✅ pass | 2936ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `update-hub/main.go`
