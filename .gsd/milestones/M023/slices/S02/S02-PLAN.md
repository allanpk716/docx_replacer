# S02: SQLite 元数据层 + Release notes

**Goal:** Add SQLite metadata layer to persist app metadata, release notes, and upload history. Extend upload handler to accept optional notes field. Add GET /api/apps and GET /api/apps/{appId}/channels/{channel}/versions query endpoints for the Web UI.
**Demo:** 上传时附带备注文本，通过 GET /api/apps 查询应用列表，通过 GET /api/apps/{appId}/channels/{channel}/versions 查询版本列表含备注

## Must-Haves

- SQLite database package with schema migration (apps + versions tables), WAL mode
- Upload handler accepts optional `notes` multipart form field, persists to SQLite
- GET /api/apps returns all registered apps with their channels
- GET /api/apps/{appId}/channels/{channel}/versions returns versions with notes
- Promote and delete handlers keep SQLite in sync with file system
- All existing S01 tests continue to pass

## Threat Surface

- **Abuse**: Notes field accepts arbitrary text — limit to reasonable length (10KB max) to prevent abuse. AppId/channel/version parameters in SQL queries use parameterized queries (no injection risk).
- **Data exposure**: Release notes may contain internal information — protected by same Bearer token auth as upload. GET /api/apps and GET versions are public (same as existing feed serving).
- **Input trust**: Notes text is user input stored in SQLite — parameterized queries prevent SQL injection. No HTML rendering server-side.

## Requirement Impact

- **Requirements touched**: R070 (SQLite metadata), R071 (release notes)
- **Re-verify**: S01 integration test must still pass (existing upload/list/promote/delete flow unchanged except metadata additions)
- **Decisions revisited**: D053 (SQLite confirmed as correct choice — WAL mode for concurrent access)

## Proof Level

- This slice proves: integration — metadata CRUD and query APIs tested through httptest with real SQLite database and real handlers. Not mocked. File system + SQLite dual-write tested end-to-end.
- Real runtime required: yes (SQLite + HTTP handlers in httptest)
- Human/UAT required: no

## Verification

- `cd update-hub && go test ./database/ -v -count=1` — database CRUD tests pass
- `cd update-hub && go test ./... -v -count=1` — all tests pass including new integration tests
- Integration test proves: upload with notes → GET /api/apps → GET versions with notes → promote → delete metadata cleanup

## Observability / Diagnostics

- Runtime signals: structured JSON logs for metadata events (metadata_upsert, metadata_query, metadata_delete) alongside existing file operation logs
- Inspection surfaces: SQLite database file at data/update-hub.db — inspectable with sqlite3 CLI for debugging
- Failure visibility: metadata write failures logged but do not block file operations (metadata is best-effort, file storage is authoritative)
- Redaction constraints: none (release notes are operational, not secrets)

## Integration Closure

- Upstream surfaces consumed: handler/upload.go (UploadHandler), handler/promote.go (PromoteHandler), handler/delete.go (DeleteHandler), handler/list.go (ListHandler patterns), storage/store.go (Store), model/release.go (ReleaseFeed, ReleaseAsset)
- New wiring introduced: database.DB initialized in main.go, passed to all handler constructors. New route handlers for GET /api/apps and GET /api/apps/{appId}/channels/{channel}/versions.
- What remains before the milestone is truly usable end-to-end: S03 (Vue 3 Web UI) consumes the new query APIs. S04 (data migration) needs to also seed SQLite metadata from migrated files.

## Tasks

- [ ] **T01: Create database package with SQLite schema and CRUD** `est:1h`
  - Why: Need a self-contained database layer that all handlers will use for metadata storage
  - Files: `update-hub/database/db.go`, `update-hub/database/db_test.go`, `update-hub/go.mod`, `update-hub/go.sum`
  - Do: Add modernc.org/sqlite dependency. Create DB struct with Init (WAL mode, migrations), Close, UpsertApp, GetApps, UpsertVersion, GetVersions, DeleteVersion, GetChannels. Schema: apps (id TEXT PK, created_at) and versions (id INTEGER PK AUTOINCREMENT, app_id, channel, version, notes, created_at, UNIQUE(app_id,channel,version)). Parameterized queries throughout. Comprehensive tests.
  - Verify: `cd update-hub && go test ./database/ -v -count=1`
  - Done when: All CRUD tests pass, schema migration is idempotent (run twice without error)

- [ ] **T02: Wire metadata into handlers and add query API endpoints** `est:1.5h`
  - Why: Connect the database layer to existing handlers and expose new API endpoints for the Web UI
  - Files: `update-hub/handler/app_list.go`, `update-hub/handler/version_list.go`, `update-hub/handler/upload.go`, `update-hub/handler/promote.go`, `update-hub/handler/delete.go`, `update-hub/main.go`, `update-hub/handler/integration_test.go`
  - Do: Add *database.DB to handler structs. UploadHandler accepts `notes` form field + calls UpsertApp/UpsertVersion. PromoteHandler calls UpsertVersion on target. DeleteHandler calls DeleteVersion. New AppListHandler for GET /api/apps. New VersionListHandler for GET /api/apps/{appId}/channels/{channel}/versions. Update main.go: init DB, pass to handlers, new routes, defer Close. Update integration test with metadata flow tests.
  - Verify: `cd update-hub && go test ./... -v -count=1`
  - Done when: Integration test proves upload with notes → GET /api/apps returns app → GET versions returns notes → promote copies metadata → delete cleans metadata

## Files Likely Touched

- `update-hub/database/db.go`
- `update-hub/database/db_test.go`
- `update-hub/handler/app_list.go`
- `update-hub/handler/version_list.go`
- `update-hub/handler/upload.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/main.go`
- `update-hub/handler/integration_test.go`
- `update-hub/go.mod`
- `update-hub/go.sum`
