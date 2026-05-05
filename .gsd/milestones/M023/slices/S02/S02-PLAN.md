# S02: SQLite 元数据层 + Release notes

**Goal:** Add SQLite metadata layer to persist app metadata, release notes, and upload history. Extend upload handler to accept optional notes field. Add GET /api/apps and GET /api/apps/{appId}/channels/{channel}/versions query endpoints for the Web UI.
**Demo:** 上传时附带备注文本，通过 GET /api/apps 查询应用列表，通过 GET /api/apps/{appId}/channels/{channel}/versions 查询版本列表含备注

## Must-Haves

- Upload handler accepts optional `notes` multipart form field and persists it to SQLite
- GET /api/apps returns all registered apps with their channels (e.g., [{"id":"docufiller","channels":["stable","beta"],"created_at":"..."}])
- GET /api/apps/{appId}/channels/{channel}/versions returns version list with notes from SQLite
- Promote handler copies version metadata to target channel in SQLite
- Delete handler removes version metadata from SQLite
- All existing S01 tests continue to pass unchanged
- New integration test proves: upload with notes → query apps → query versions with notes → promote → delete metadata cleanup

## Proof Level

- This slice proves: integration — metadata CRUD and query APIs tested through httptest with real SQLite database and real handlers. Not mocked. File system + SQLite dual-write tested end-to-end.

## Integration Closure

Upstream surfaces consumed: handler/upload.go (UploadHandler), handler/promote.go (PromoteHandler), handler/delete.go (DeleteHandler), handler/list.go (ListHandler patterns), storage/store.go (Store for file operations), model/release.go (ReleaseFeed, ReleaseAsset).
New wiring introduced: database.DB initialized in main.go, passed to all handler constructors. New route handlers for GET /api/apps and GET /api/apps/{appId}/channels/{channel}/versions.
What remains before milestone is truly usable end-to-end: S03 (Vue 3 Web UI) consumes the new query APIs. S04 (data migration) needs to also seed SQLite metadata from migrated files.

## Verification

- Runtime signals: structured JSON logs for metadata events (metadata_upsert, metadata_query, metadata_delete) alongside existing file operation logs.
- Inspection surfaces: SQLite database file at data/update-hub.db — can be inspected with sqlite3 CLI for debugging metadata state.
- Failure visibility: metadata write failures logged but do not block file operations (metadata is best-effort, file storage is authoritative).

## Tasks

- [x] **T01: Create database package with SQLite schema and CRUD** `est:1h`
  Create a self-contained `database` package with SQLite initialization, schema migration, and CRUD operations for apps and versions metadata. Use `modernc.org/sqlite` (pure Go, no CGO) to avoid GCC dependency on Windows. Schema has two tables: `apps` (id TEXT PK, created_at) and `versions` (id INTEGER PK AUTOINCREMENT, app_id, channel, version, notes, created_at, UNIQUE(app_id, channel, version)). CRUD methods: Init (open DB with WAL mode, run migrations), Close, UpsertApp, GetApps (with channel lists derived from versions), UpsertVersion, GetVersions(appId, channel), DeleteVersion(appId, channel, version). All methods use parameterized queries to prevent SQL injection.
  - Files: `update-hub/database/db.go`, `update-hub/database/db_test.go`, `update-hub/go.mod`, `update-hub/go.sum`
  - Verify: cd update-hub && go test ./database/ -v -count=1

- [x] **T02: Wire metadata into handlers and add query API endpoints** `est:1.5h`
  Wire the database package into existing handlers and add new query API endpoints. (1) Add `*database.DB` field to UploadHandler, PromoteHandler, DeleteHandler — metadata operations run after successful file operations. (2) UploadHandler: accept optional `notes` multipart form field, call UpsertApp + UpsertVersion after file storage. (3) PromoteHandler: call UpsertVersion for target channel after file copy. (4) DeleteHandler: call DeleteVersion after file cleanup. (5) Create new AppListHandler for GET /api/apps returning all apps with channels. (6) Create new VersionListHandler for GET /api/apps/{appId}/channels/{channel}/versions returning versions with notes from SQLite. (7) Update main.go: init DB at data/update-hub.db, pass to handlers, register new routes, defer DB.Close(). (8) Update integration test setup to create DB and pass to handlers. (9) Add integration sub-tests: upload with notes → GET /api/apps → GET versions with notes → promote metadata sync → delete metadata cleanup.
  - Files: `update-hub/handler/upload.go`, `update-hub/handler/promote.go`, `update-hub/handler/delete.go`, `update-hub/handler/app_list.go`, `update-hub/handler/version_list.go`, `update-hub/main.go`, `update-hub/handler/integration_test.go`
  - Verify: cd update-hub && go test ./... -v -count=1

## Files Likely Touched

- update-hub/database/db.go
- update-hub/database/db_test.go
- update-hub/go.mod
- update-hub/go.sum
- update-hub/handler/upload.go
- update-hub/handler/promote.go
- update-hub/handler/delete.go
- update-hub/handler/app_list.go
- update-hub/handler/version_list.go
- update-hub/main.go
- update-hub/handler/integration_test.go
