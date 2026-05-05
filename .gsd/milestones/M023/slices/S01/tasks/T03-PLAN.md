---
estimated_steps: 6
estimated_files: 8
skills_used:
  - api-design
  - verify-before-complete
---

# T03: Implement static serving + list + promote + delete handlers

**Slice:** S01 — Go 服务器核心 API（多应用 Velopack 分发）
**Milestone:** M023

## Description

Build the remaining four handlers: static file serving (Velopack client), version list, promote between channels, and delete version. Each adapts the existing single-app logic to multi-app by adding appId to all storage calls and removing hardcoded channel validation.

The static handler is the Velopack SimpleWebSource compatibility layer — it must serve `/{appId}/{channel}/releases.{os}.json` and `/{appId}/{channel}/*.nupkg` without authentication. The list, promote, and delete handlers form the management API.

Reference implementation: `update-server/handler/static.go`, `update-server/handler/list.go`, `update-server/handler/promote.go` (no existing delete handler).

## Failure Modes

| Dependency | On error | On timeout | On malformed response |
|------------|----------|-----------|----------------------|
| Filesystem (read) | Return 404 for missing file | N/A | N/A |
| Filesystem (write/copy) | Return 500, log error | N/A | N/A |
| Feed parse | Return 500 | N/A | Return 500 |

## Negative Tests

- **Malformed inputs**: Path traversal attempts in filename (`../`), non-existent appId/channel, delete non-existent version
- **Error paths**: Promote version that doesn't exist in source, promote from same channel to same channel, delete when feed file is malformed
- **Boundary conditions**: Empty channel (no uploads yet), promote when target already has the version (dedup), delete only remaining version

## Steps

1. Create `handler/static.go`:
   - `StaticHandler` struct with `Store *storage.Store`
   - `ServeHTTP` method:
     - Only handle GET requests
     - Parse path as `/{appId}/{channel}/{filename}` (3 segments after leading /)
     - No channel/appId validation (just serve files, 404 if not found)
     - Path traversal protection: reject filenames containing `..`
     - Content-Type: `.json` → `application/json`, `.nupkg` → `application/octet-stream`
     - Read file via `store.ReadFile(appId, channel, filename)`
2. Create `handler/list.go`:
   - `ListHandler` struct with `Store *storage.Store`
   - `ServeHTTP` method:
     - Only handle GET requests
     - Parse appId and channel from `r.PathValue()`
     - Read all feed files in channel (`store.ListFeedFiles`)
     - Merge assets from all OS feeds into one list
     - Group by version, sort descending by semver
     - Return `ListResponse{channel, versions[{version, files, total_size, file_count}], total_versions}`
3. Create `handler/promote.go`:
   - `PromoteHandler` struct with `Store *storage.Store`
   - `ServeHTTP` method:
     - Only handle POST requests
     - Parse appId, targetChannel from `r.PathValue()`
     - Parse `from` and `version` from query params
     - Validate source != target
     - Read source feed, find matching assets by version
     - Copy .nupkg files from source to target channel
     - Merge promoted assets into ALL target channel feed files (each OS feed)
     - Auto-cleanup target channel
     - Return `PromoteResponse{promoted, from, to, files_copied}`
4. Create `handler/delete.go`:
   - `DeleteHandler` struct with `Store *storage.Store`
   - `ServeHTTP` method:
     - Only handle DELETE requests
     - Parse appId, channel, version from `r.PathValue()`
     - Delete matching .nupkg files via `store.DeleteVersion`
     - Read all feed files, remove matching assets, rewrite each
     - Return `DeleteResponse{channel, version, files_deleted}`
5. Write test files for each handler:
   - `handler/static_test.go`: test feed serving, .nupkg serving, file not found, path traversal, method not allowed
   - `handler/list_test.go`: test version listing with multiple versions, empty channel, semver sort order
   - `handler/promote_test.go`: test valid promote, missing version, missing source param, same channel, files physically copied
   - `handler/delete_test.go`: test valid delete, non-existent version, feed updated after delete
6. Remove all hardcoded `validChannels` map usage — validate channel names by regex `^[a-zA-Z0-9-]+$` only in upload handler (static handler doesn't validate at all, just serves files)

## Must-Haves

- [ ] Static handler serves /{appId}/{channel}/releases.{os}.json and *.nupkg without auth
- [ ] List handler returns version list from all OS feeds merged
- [ ] Promote handler copies files and merges feeds across channels within same app
- [ ] Delete handler removes .nupkg files and updates all feed files
- [ ] No hardcoded channel validation — dynamic channels work
- [ ] All handler tests pass

## Verification

- `cd update-hub && go test ./handler/... -run 'TestStatic|TestList|TestPromote|TestDelete' -count=1 -v`

## Inputs

- `update-hub/go.mod` — Go module (from T01)
- `update-hub/model/release.go` — data model (from T01)
- `update-hub/storage/store.go` — multi-app Store (from T01)
- `update-hub/storage/cleanup.go` — cleanup logic (from T01)
- `update-hub/handler/upload.go` — upload handler for reference pattern (from T02)
- `update-hub/middleware/auth.go` — auth middleware (from T02)
- `update-server/handler/static.go` — reference static handler implementation
- `update-server/handler/list.go` — reference list handler implementation
- `update-server/handler/promote.go` — reference promote handler implementation

## Expected Output

- `update-hub/handler/static.go` — Velopack-compatible static file serving
- `update-hub/handler/list.go` — version list API handler
- `update-hub/handler/promote.go` — channel promotion handler
- `update-hub/handler/delete.go` — version deletion handler
- `update-hub/handler/static_test.go` — static handler tests
- `update-hub/handler/list_test.go` — list handler tests
- `update-hub/handler/promote_test.go` — promote handler tests
- `update-hub/handler/delete_test.go` — delete handler tests
