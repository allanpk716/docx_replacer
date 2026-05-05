---
id: T03
parent: S01
milestone: M023
key_files:
  - update-hub/handler/static.go
  - update-hub/handler/list.go
  - update-hub/handler/promote.go
  - update-hub/handler/delete.go
  - update-hub/handler/static_test.go
  - update-hub/handler/list_test.go
  - update-hub/handler/promote_test.go
  - update-hub/handler/delete_test.go
  - update-hub/storage/store.go
key_decisions:
  - Static handler parses URL path manually (3 segments) rather than using PathValue since Velopack URLs don't follow API path patterns
  - Promote handler creates feed files in target using source feed filenames when target has no existing feeds
  - Delete handler is idempotent — returns success with files_deleted=0 for non-existent versions
  - Added ListFeedFiles to Store to discover all releases.*.json files without scanning for all .json files
duration: 
verification_result: passed
completed_at: 2026-05-05T06:21:37.054Z
blocker_discovered: false
---

# T03: Implemented static serving, list, promote, and delete handlers for multi-app Velopack update-hub

**Implemented static serving, list, promote, and delete handlers for multi-app Velopack update-hub**

## What Happened

Implemented the four remaining HTTP handlers for update-hub's management API:

1. **StaticHandler** (`handler/static.go`): Serves `/{appId}/{channel}/{filename}` for Velopack SimpleWebSource compatibility. No auth required. Supports both `.json` feeds and `.nupkg` package files. Path traversal protection rejects `..` in any segment. Content-type detection based on extension. Dynamic channel support — no channel validation, just serve files.

2. **ListHandler** (`handler/list.go`): Handles `GET /api/apps/{appId}/channels/{channel}/releases`. Discovers all `releases.*.json` files in the channel directory using a new `Store.ListFeedFiles()` method, merges assets from all OS feeds, groups by version, and returns them sorted descending by semver. Returns empty list (not error) for channels with no feeds.

3. **PromoteHandler** (`handler/promote.go`): Handles `POST /api/apps/{appId}/channels/{target}/promote?from={source}&version={version}`. Copies `.nupkg` files from source to target channel, merges feed entries into all target feed files (creating feeds if target is new), deduplicates by filename, and triggers auto-cleanup on target.

4. **DeleteHandler** (`handler/delete.go`): Handles `DELETE /api/apps/{appId}/channels/{channel}/versions/{version}`. Removes matching `.nupkg` files via `Store.DeleteVersion()`, then updates all `releases.*.json` feeds to remove matching assets. Idempotent — deleting a non-existent version returns success with `files_deleted: 0`.

Also added `Store.ListFeedFiles()` method to `storage/store.go` for discovering all OS feed files in a channel directory. All handlers use structured JSON logging and the `writeJSONError` helper from upload.go. No hardcoded channel validation — dynamic channels work throughout.

22 new tests across four test files cover normal flows, error cases, boundary conditions (empty channels, non-existent versions, same-channel promote), and negative cases (path traversal, missing params).

## Verification

All 22 handler tests pass:
- TestStatic (7 tests): feed serving, .nupkg serving, not found, path traversal, method not allowed, dynamic channel, missing path segments
- TestList (6 tests): multiple versions, empty channel, multi-OS feed merging, semver sort order, method not allowed, missing appId
- TestPromote (8 tests): valid promote with file copy verification, version not found, missing params, same channel rejection, no feeds in source, dedup in target, physical file copy, method not allowed
- TestDelete (6 tests): valid delete with feed+file verification, non-existent version (idempotent), multi-OS feed cleanup, only-version-deleted leaves empty feed, method not allowed, missing version

Full test suite (handler + storage packages) passes with no regressions.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && go test ./handler/... -run 'TestStatic|TestList|TestPromote|TestDelete' -count=1 -v` | 0 | ✅ pass | 1332ms |
| 2 | `cd update-hub && go test ./... -count=1 -v` | 0 | ✅ pass | 2384ms |

## Deviations

None — all handlers follow the task plan exactly.

## Known Issues

None.

## Files Created/Modified

- `update-hub/handler/static.go`
- `update-hub/handler/list.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/handler/static_test.go`
- `update-hub/handler/list_test.go`
- `update-hub/handler/promote_test.go`
- `update-hub/handler/delete_test.go`
- `update-hub/storage/store.go`
