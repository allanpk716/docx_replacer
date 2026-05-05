# S01: Go 服务器核心 API（多应用 Velopack 分发）

**Goal:** Build the core Go HTTP server for update-hub with multi-app Velopack feed distribution. The server supports POST upload of .nupkg + releases.*.json to /api/apps/{appId}/channels/{channel}/releases, serves Velopack-compatible feeds at /{appId}/{channel}/releases.{os}.json, auto-registers apps from PackageId, supports dynamic channel names, and protects write operations with Bearer token auth.
**Demo:** curl 上传 .nupkg 到新服务器 /api/apps/docufiller/channels/stable/releases，Velopack SimpleWebSource 客户端从 /docufiller/stable/releases.win.json 成功拉取更新

## Must-Haves

- POST /api/apps/docufiller/channels/stable/releases accepts multipart upload of releases.win.json + .nupkg files
- GET /docufiller/stable/releases.win.json returns the correct Velopack feed JSON
- GET /docufiller/stable/*.nupkg serves artifact files
- GET /api/apps/docufiller/channels/stable/releases returns version list
- POST /api/apps/docufiller/channels/stable/promote?from=beta&version=X promotes a version
- DELETE /api/apps/docufiller/channels/beta/versions/X deletes a version
- App auto-registers on first upload (PackageId from feed matches appId in URL)
- Channels are dynamic — any alphanumeric+hyphen name works
- Bearer token auth protects POST/DELETE, GET is public for Velopack client access
- Integration test proves complete upload → feed serve → list → promote → delete workflow

## Proof Level

- This slice proves: integration — integration test starts real HTTP server, uploads files via multipart POST, and verifies Velopack SimpleWebSource-compatible feed URLs return correct JSON. File system storage is exercised end-to-end (temp dir, no mocks).

## Integration Closure

Upstream surfaces consumed: existing update-server/ as reference architecture (handler/storage/middleware/model pattern, atomic file writes, auth middleware)
New wiring introduced: Go 1.22 enhanced ServeMux with method+path patterns, multi-app directory namespace (data/{appId}/{channel}/)
What remains: S02 adds SQLite metadata layer (DB init, migrations, structured queries) alongside this file-system storage; S03 adds Vue 3 Web UI and JWT session auth; S04 adds data migration from old format and NSSM deployment

## Verification

- Runtime signals: structured JSON logging for every request (method, path, status, duration_ms), plus event-specific logs (upload_feed, upload_file, promote_success, cleanup_complete, auth_missing, auth_invalid_token)
- Inspection surfaces: log output to stdout (captured by NSSM), data/ directory structure inspectable via filesystem
- Failure visibility: per-event error logs with channel/appId/file context, HTTP status codes matching error semantics (400/401/404/500)
- Redaction constraints: Bearer token never logged (only "configured" or "(none)")

## Tasks

- [x] **T01: Create Go module + data model + multi-app storage layer** `est:1h`
  Create the update-hub Go project with go.mod, the Velopack feed data model (ReleaseFeed/ReleaseAsset), and a multi-app file-system storage layer. The storage layer uses data/{appId}/{channel}/ directory structure (vs the old data/{channel}/), supports any OS feed filename (releases.win.json, releases.linux.json, etc.) instead of hardcoded releases.win.json, and preserves atomic write (temp+rename). Port cleanup logic with appId parameter. Write comprehensive unit tests.
  - Files: `update-hub/go.mod`, `update-hub/model/release.go`, `update-hub/storage/store.go`, `update-hub/storage/cleanup.go`, `update-hub/storage/store_test.go`
  - Verify: cd update-hub && go test ./storage/... -count=1 -v

- [x] **T02: Implement upload handler with auto-registration + Bearer auth middleware** `est:1.5h`
  Build the upload handler for POST /api/apps/{appId}/channels/{channel}/releases and the Bearer token auth middleware. The upload handler accepts multipart form data with any releases.*.json feed file (not just releases.win.json) plus .nupkg artifacts. Auto-registration validates that the feed's PackageId matches the appId in the URL path — first upload for an appId automatically creates the app directory structure. Channel names are validated by regex (alphanumeric+hyphen) instead of a hardcoded set. The auth middleware protects POST/DELETE API paths, skips auth for GET to non-API paths (Velopack client static serving) and for GET /api/apps/{appId}/channels/{channel}/releases (public version list). Use Go 1.22 r.PathValue() for path parameter extraction.
  - Files: `update-hub/handler/upload.go`, `update-hub/middleware/auth.go`, `update-hub/handler/upload_test.go`, `update-hub/middleware/auth_test.go`
  - Verify: cd update-hub && go test ./handler/... ./middleware/... -run 'TestUpload|TestAuth' -count=1 -v

- [x] **T03: Implement static serving + list + promote + delete handlers** `est:1.5h`
  Build the remaining handlers: static file serving for Velopack clients, version list, promote between channels, and delete version. Each handler adapts the existing single-app pattern to multi-app by adding appId to all storage operations and removing the hardcoded channel validation.
  - Files: `update-hub/handler/static.go`, `update-hub/handler/list.go`, `update-hub/handler/promote.go`, `update-hub/handler/delete.go`, `update-hub/handler/static_test.go`, `update-hub/handler/list_test.go`, `update-hub/handler/promote_test.go`, `update-hub/handler/delete_test.go`
  - Verify: cd update-hub && go test ./handler/... -run 'TestStatic|TestList|TestPromote|TestDelete' -count=1 -v

- [x] **T04: Wire main.go with Go 1.22 ServeMux routes + write end-to-end integration test** `est:1h`
  Create main.go that wires all handlers using Go 1.22's enhanced ServeMux with method+path patterns (e.g. mux.HandleFunc("POST /api/apps/{appId}/channels/{channel}/releases", uploadHandler)). Add CLI flags for port (default 30001), data-dir, and token. Apply auth middleware and structured JSON logging. Write a comprehensive integration test that proves the full Velopack-compatible workflow: upload files via multipart POST → verify feed served at /{appId}/{channel}/releases.win.json → verify .nupkg served → list versions → promote → delete → verify auth rejection. This test uses httptest.NewServer with real handlers and temp directory storage.
  - Files: `update-hub/main.go`, `update-hub/handler/integration_test.go`
  - Verify: cd update-hub && go test ./... -count=1 -v

## Files Likely Touched

- update-hub/go.mod
- update-hub/model/release.go
- update-hub/storage/store.go
- update-hub/storage/cleanup.go
- update-hub/storage/store_test.go
- update-hub/handler/upload.go
- update-hub/middleware/auth.go
- update-hub/handler/upload_test.go
- update-hub/middleware/auth_test.go
- update-hub/handler/static.go
- update-hub/handler/list.go
- update-hub/handler/promote.go
- update-hub/handler/delete.go
- update-hub/handler/static_test.go
- update-hub/handler/list_test.go
- update-hub/handler/promote_test.go
- update-hub/handler/delete_test.go
- update-hub/main.go
- update-hub/handler/integration_test.go
