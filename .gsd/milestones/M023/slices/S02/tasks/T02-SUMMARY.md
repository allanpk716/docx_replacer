---
id: T02
parent: S02
milestone: M023
key_files:
  - update-hub/handler/app_list.go
  - update-hub/handler/version_list.go
  - update-hub/handler/upload.go
  - update-hub/handler/promote.go
  - update-hub/handler/delete.go
  - update-hub/handler/integration_test.go
  - update-hub/main.go
  - update-hub/middleware/auth.go
  - update-hub/middleware/auth_test.go
key_decisions:
  - All GET /api/* requests are public (no auth required) since they are read-only queries, consolidating the previous policy that only allowed GET paths ending in /releases
  - Metadata DB operations are best-effort: logged errors do not block file operations, and nil DB is handled gracefully with no-ops
duration: 
verification_result: passed
completed_at: 2026-05-05T06:47:47.103Z
blocker_discovered: false
---

# T02: Wire SQLite metadata into upload/promote/delete handlers, add GET /api/apps and GET /api/apps/{appId}/channels/{channel}/versions query endpoints

**Wire SQLite metadata into upload/promote/delete handlers, add GET /api/apps and GET /api/apps/{appId}/channels/{channel}/versions query endpoints**

## What Happened

Wired the database package from T01 into all existing handlers and created two new query API endpoints for the Web UI.

**Handler changes:**
- UploadHandler: Added `DB *database.DB` field, accepts optional `notes` multipart form field, calls `UpsertApp` + `UpsertVersion` for each version added after successful file operations. All DB calls are nil-safe (skip if DB is nil).
- PromoteHandler: Added `DB *database.DB` field, calls `UpsertVersion` for target channel after successful file copy. Looks up source version notes to carry them over during promote.
- DeleteHandler: Added `DB *database.DB` field, calls `DeleteVersion` after successful file cleanup.

**New endpoints:**
- `GET /api/apps` — Returns JSON array of all registered apps with derived channels from SQLite. Returns empty array (not null) when no apps exist or DB is nil.
- `GET /api/apps/{appId}/channels/{channel}/versions` — Returns JSON array of versions with notes for a specific app/channel. Returns empty array when no versions found.

**Auth policy update:** Changed middleware to allow all GET /api/* requests (not just paths ending in /releases) since all GET endpoints are read-only queries. Updated the corresponding test from `TestAuth_GETNonReleasesAPI_RequiresAuth` to `TestAuth_GETNonReleasesAPI_SkipsAuth`.

**Main.go:** Added database init at `data/update-hub.db`, passes DB to all handler constructors, registers new routes, defers DB.Close().

**Integration tests:** Extended `setupIntegrationServer` to create temp DB and pass to handlers (3-return-value signature). Added `TestMetadataFlow` with sub-tests proving upload-with-notes → list-apps → list-versions-with-notes → promote-metadata-sync → delete-metadata-cleanup lifecycle. Added `TestMetadataEndpoints_NilDB` proving nil-safe empty array returns. Added `TestMetadataVersionList_EmptyChannel` for unknown app/channel.

**Existing tests:** Updated all unit tests in upload_test.go, promote_test.go, delete_test.go to pass `nil` for the new DB constructor parameter. All existing S01 integration tests pass unchanged.

## Verification

All tests pass across all 4 packages (database: 13 tests, handler: 18 tests including 5 new metadata tests, middleware: 12 tests, storage: 22 tests). Go vet reports no warnings. Build compiles successfully. Verified: upload with notes persisted to SQLite, GET /api/apps returns app list with channels, GET versions returns version entries with notes, promote carries notes to target channel, delete removes version metadata, nil DB returns empty arrays, empty channel returns empty arrays.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && go test ./... -v -count=1` | 0 | ✅ pass (65/65 tests across 4 packages) | 3200ms |
| 2 | `cd update-hub && go vet ./...` | 0 | ✅ pass | 500ms |
| 3 | `cd update-hub && go build ./...` | 0 | ✅ pass | 400ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `update-hub/handler/app_list.go`
- `update-hub/handler/version_list.go`
- `update-hub/handler/upload.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/handler/integration_test.go`
- `update-hub/main.go`
- `update-hub/middleware/auth.go`
- `update-hub/middleware/auth_test.go`
