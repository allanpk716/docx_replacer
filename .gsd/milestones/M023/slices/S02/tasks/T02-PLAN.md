---
estimated_steps: 7
estimated_files: 7
skills_used:
  - test
  - verify-before-complete
---

# T02: Wire metadata into handlers and add query API endpoints

**Slice:** S02 — SQLite 元数据层 + Release notes
**Milestone:** M023

## Description

Wire the database package from T01 into existing handlers and add new query API endpoints. Upload handler gains `notes` form field support. Promote and delete handlers sync SQLite metadata. Two new endpoints expose metadata for the Web UI: GET /api/apps (list all apps) and GET /api/apps/{appId}/channels/{channel}/versions (list versions with notes). Update main.go to init DB and register new routes. Extend integration test to prove end-to-end metadata flow.

## Failure Modes

| Dependency | On error | On timeout | On malformed response |
|------------|----------|-----------|----------------------|
| SQLite DB | Log error, continue file operation (metadata is best-effort) | N/A (local) | Log error, file ops succeed |

## Load Profile

- **Shared resources**: SQLite DB passed to all handlers — WAL mode handles concurrent reads
- **Per-operation cost**: 1-2 extra DB queries per upload/promote/delete (trivial)
- **10x breakpoint**: Not applicable for internal tool scale

## Negative Tests

- **Malformed inputs**: Notes field >10KB, empty appId in query, non-existent appId/channel for GET
- **Error paths**: DB nil (backward-compat), DB closed mid-request
- **Boundary conditions**: App with no versions, channel with no feeds, promote when source metadata missing

## Steps

1. Create `update-hub/handler/app_list.go`:
   - Define `AppListHandler` struct with `DB *database.DB` field
   - Handle `GET /api/apps` — return JSON array of `{"id": "...", "channels": ["stable","beta"], "created_at": "..."}`
   - Return empty array (not null) when no apps exist

2. Create `update-hub/handler/version_list.go`:
   - Define `VersionListHandler` struct with `DB *database.DB` field
   - Handle `GET /api/apps/{appId}/channels/{channel}/versions` — return JSON array of `{"version": "...", "notes": "...", "created_at": "..."}`
   - Return empty array when no versions found

3. Modify `update-hub/handler/upload.go`:
   - Add `DB *database.DB` field to UploadHandler struct
   - Update NewUploadHandler to accept `*database.DB`
   - After successful file operations: read `notes` from `r.MultipartForm.Value["notes"]` (first value, default empty)
   - Call `h.DB.UpsertApp(ctx, appId)` and `h.DB.UpsertVersion(ctx, appId, channel, version, notes)` for each version added
   - Guard all DB calls: if `h.DB == nil`, skip (backward-compat with existing tests)

4. Modify `update-hub/handler/promote.go`:
   - Add `DB *database.DB` field to PromoteHandler struct
   - Update NewPromoteHandler to accept `*database.DB`
   - After successful file copy: call `h.DB.UpsertVersion(ctx, appId, targetChannel, version, "")` — carry over notes from source if available
   - Guard: if `h.DB == nil`, skip

5. Modify `update-hub/handler/delete.go`:
   - Add `DB *database.DB` field to DeleteHandler struct
   - Update NewDeleteHandler to accept `*database.DB`
   - After successful file cleanup: call `h.DB.DeleteVersion(ctx, appId, channel, version)`
   - Guard: if `h.DB == nil`, skip

6. Modify `update-hub/main.go`:
   - Import `update-hub/database`
   - Init DB: `db, err := database.Init(filepath.Join(*dataDir, "update-hub.db"))`
   - Pass `db` to all handler constructors: `handler.NewUploadHandler(store, db)`, etc.
   - Register new routes:
     - `mux.HandleFunc("GET /api/apps", appListHandler.ServeHTTP)`
     - `mux.HandleFunc("GET /api/apps/{appId}/channels/{channel}/versions", versionListHandler.ServeHTTP)`
   - Defer `db.Close()` before server start

7. Update `update-hub/handler/integration_test.go`:
   - In `setupIntegrationServer`: create temp DB file, call `database.Init`, pass to all handler constructors, defer cleanup
   - Add test sub-tests in `TestFullMultiAppWorkflow`:
     - `UploadWithNotes`: upload with `notes` form field, verify response
     - `ListApps`: GET /api/apps returns both uploaded apps
     - `ListVersionsWithNotes`: GET /api/apps/docufiller/channels/beta/versions returns version with notes
     - `PromoteMetadataSync`: after promote, GET /api/apps/docufiller/channels/stable/versions includes promoted version
     - `DeleteMetadataCleanup`: after delete, GET versions no longer includes deleted version
   - Verify existing S01 tests still pass unchanged

## Must-Haves

- [ ] UploadHandler accepts optional `notes` multipart form field and persists to SQLite
- [ ] GET /api/apps returns all registered apps with their channels
- [ ] GET /api/apps/{appId}/channels/{channel}/versions returns versions with notes from SQLite
- [ ] PromoteHandler syncs metadata to target channel in SQLite
- [ ] DeleteHandler removes version metadata from SQLite
- [ ] All handler constructors accept `*database.DB` (nil-safe)
- [ ] main.go initializes DB and registers new routes
- [ ] Existing S01 integration tests pass without modification (except setupIntegrationServer)
- [ ] New integration sub-tests prove metadata flow end-to-end

## Verification

- `cd update-hub && go test ./... -v -count=1` — all tests pass (database + handler + middleware)
- `cd update-hub && go vet ./...` — no warnings
- `cd update-hub && go build` — compiles successfully

## Inputs

- `update-hub/database/db.go` — DB struct and CRUD methods from T01
- `update-hub/handler/upload.go` — existing upload handler to modify
- `update-hub/handler/promote.go` — existing promote handler to modify
- `update-hub/handler/delete.go` — existing delete handler to modify
- `update-hub/handler/list.go` — patterns for handler structure (read for reference)
- `update-hub/main.go` — server wiring to update
- `update-hub/handler/integration_test.go` — existing integration test to extend

## Expected Output

- `update-hub/handler/app_list.go` — new AppListHandler for GET /api/apps
- `update-hub/handler/version_list.go` — new VersionListHandler for GET versions with notes
- `update-hub/handler/upload.go` — modified to accept notes and persist metadata
- `update-hub/handler/promote.go` — modified to sync metadata on promote
- `update-hub/handler/delete.go` — modified to clean up metadata on delete
- `update-hub/main.go` — updated with DB init and new routes
- `update-hub/handler/integration_test.go` — extended with metadata flow tests
