# S01: Go 服务器核心 API（多应用 Velopack 分发）

**Goal:** Build the core Go HTTP server for update-hub with multi-app Velopack feed distribution. The server supports POST upload of .nupkg + releases.*.json to /api/apps/{appId}/channels/{channel}/releases, serves Velopack-compatible feeds at /{appId}/{channel}/releases.{os}.json, auto-registers apps from PackageId, supports dynamic channel names, and protects write operations with Bearer token auth.

**Demo:** curl 上传 .nupkg 到新服务器 /api/apps/docufiller/channels/stable/releases，Velopack SimpleWebSource 客户端从 /docufiller/stable/releases.win.json 成功拉取更新

## Must-Haves

- POST /api/apps/{appId}/channels/{channel}/releases accepts multipart upload of releases.*.json + .nupkg files
- GET /{appId}/{channel}/releases.{os}.json serves Velopack-compatible feed (win/linux/darwin)
- GET /{appId}/{channel}/*.nupkg serves artifact files
- GET /api/apps/{appId}/channels/{channel}/releases returns version list
- POST /api/apps/{appId}/channels/{target}/promote?from={source}&version={version} promotes a version between channels
- DELETE /api/apps/{appId}/channels/{channel}/versions/{version} deletes a version
- App auto-registers on first upload (PackageId from feed matches appId in URL)
- Dynamic channels — any alphanumeric+hyphen name works, no hardcoded set
- Bearer token auth protects POST/DELETE, GET is public for Velopack client access
- End-to-end integration test proves upload → feed serve → list → promote → delete workflow

## Threat Surface

- **Abuse**: Unauthorized upload (forged Bearer token), path traversal in filename (../../../etc/passwd), oversized upload (disk exhaustion), malicious PackageId (mismatch to hijack appId namespace)
- **Data exposure**: Bearer token in transit (mitigate: internal network only), release feed visible to any GET request (by design — Velopack clients need unauthenticated access)
- **Input trust**: Multipart filenames reach filesystem (sanitize path separators), channel/appId from URL path reach filesystem (validate by regex), releases.*.json content parsed as JSON (handle malformed gracefully)

## Requirement Impact

- **Requirements touched**: R066 (multi-app feed), R067 (multi-OS), R068 (auto-registration), R069 (dynamic channels)
- **Re-verify**: All four requirements are new — no prior implementation to re-verify
- **Decisions revisited**: D052 (Velopack protocol — confirmed), D055 (auth scheme — Bearer only in S01, JWT deferred to S03), D056 (URL structure — confirmed), D058 (auto-registration — confirmed), D059 (dynamic channels — confirmed)

## Proof Level

- This slice proves: integration
- Real runtime required: yes (integration test starts real HTTP server with temp directory)
- Human/UAT required: no

## Verification

- `cd update-hub && go test ./... -count=1 -v` — all unit + integration tests pass
- Integration test `TestFullMultiAppWorkflow` proves: upload → feed serve at /{appId}/{channel}/releases.win.json → .nupkg download → version list → promote → delete → auth rejection → multi-app isolation
- `cd update-hub && go vet ./...` — no static analysis warnings

## Observability / Diagnostics

- Runtime signals: structured JSON logs per request (method, path, status, duration_ms), event-specific logs (upload_feed, upload_file, promote_success, cleanup_complete, auth_missing, auth_invalid_token)
- Inspection surfaces: stdout logs (captured by NSSM), data/ directory filesystem structure
- Failure visibility: per-event error logs with appId/channel/file context, HTTP status codes (400/401/404/500)
- Redaction constraints: Bearer token never logged (only "configured" or "(none)")

## Integration Closure

- Upstream surfaces consumed: existing update-server/ as reference architecture (handler/storage/middleware/model pattern, atomic file writes, auth middleware)
- New wiring introduced: Go 1.22 enhanced ServeMux with method+path patterns, multi-app directory namespace (data/{appId}/{channel}/)
- What remains before the milestone is truly usable end-to-end: S02 adds SQLite metadata layer (DB init, migrations, structured queries); S03 adds Vue 3 Web UI and JWT session auth; S04 adds data migration from old format and NSSM deployment

## Tasks

- [ ] **T01: Create Go module + data model + multi-app storage layer** `est:1h`
  - Why: Foundation for the entire server — Go module, Velopack data models, and the multi-app file-system storage abstraction that all handlers depend on
  - Files: `update-hub/go.mod`, `update-hub/model/release.go`, `update-hub/storage/store.go`, `update-hub/storage/cleanup.go`, `update-hub/storage/store_test.go`
  - Do: Create update-hub/ with go.mod (module update-hub, go 1.22). Port model/release.go unchanged. Create storage/store.go with multi-app methods (NewStore, AppDir, ChannelDir, ReadReleaseFeed, WriteReleaseFeed, ReadFile, WriteFile, ListFiles, DeleteVersion) using data/{appId}/{channel}/ paths. Feed methods accept any OS filename (releases.*.json) not just releases.win.json. Port cleanup.go with appId parameter. Write comprehensive storage tests.
  - Verify: `cd update-hub && go test ./storage/... -count=1 -v`
  - Done when: All storage tests pass, multi-app directory structure verified, multi-OS feed read/write works

- [ ] **T02: Implement upload handler with auto-registration + Bearer auth middleware** `est:1.5h`
  - Why: Upload is the primary write path and most complex handler — it handles multi-OS feed merging, auto-registration from PackageId, and artifact storage. Auth middleware protects all write operations.
  - Files: `update-hub/handler/upload.go`, `update-hub/middleware/auth.go`, `update-hub/handler/upload_test.go`, `update-hub/middleware/auth_test.go`
  - Do: Create handler/upload.go for POST /api/apps/{appId}/channels/{channel}/releases — multipart parsing, detect releases.*.json by pattern, merge with existing feed, auto-register by validating PackageId matches appId, write artifacts, auto-cleanup. Use Go 1.22 r.PathValue(). Create middleware/auth.go — BearerAuth(token) with subtle.ConstantTimeCompare, skip auth for GET non-API paths and GET /api/*/releases. Write tests for upload (valid, bad channel, auto-registration validation) and auth (skip static, require POST, wrong token, disabled).
  - Verify: `cd update-hub && go test ./handler/... ./middleware/... -run 'TestUpload|TestAuth' -count=1 -v`
  - Done when: Upload handler accepts multipart with any OS feed, auto-registration validates PackageId, auth middleware correctly protects/enforces

- [ ] **T03: Implement static serving + list + promote + delete handlers** `est:1.5h`
  - Why: Read path for Velopack clients (static serving) and management API (list/promote/delete). Each handler adapts existing single-app logic to multi-app by adding appId and removing hardcoded channel validation.
  - Files: `update-hub/handler/static.go`, `update-hub/handler/list.go`, `update-hub/handler/promote.go`, `update-hub/handler/delete.go`, `update-hub/handler/static_test.go`, `update-hub/handler/list_test.go`, `update-hub/handler/promote_test.go`, `update-hub/handler/delete_test.go`
  - Do: Create handler/static.go for /{appId}/{channel}/releases.{os}.json and *.nupkg (no channel validation, path traversal protection). Create handler/list.go for GET version list grouped by version sorted descending. Create handler/promote.go for cross-channel promotion (copy .nupkg, merge feeds). Create handler/delete.go for version deletion (remove files + update all OS feeds). All handlers use r.PathValue() for path extraction and regex-only channel validation. Write tests for each handler (normal flow + error cases).
  - Verify: `cd update-hub && go test ./handler/... -run 'TestStatic|TestList|TestPromote|TestDelete' -count=1 -v`
  - Done when: Static handler serves Velopack-compatible feeds, list returns versions, promote copies between channels, delete removes versions, all tests pass

- [ ] **T04: Wire main.go with Go 1.22 ServeMux routes + write end-to-end integration test** `est:1h`
  - Why: Final wiring and proof — main.go connects all handlers with proper routing, and the integration test proves the complete Velopack-compatible workflow works end-to-end including multi-app isolation.
  - Files: `update-hub/main.go`, `update-hub/handler/integration_test.go`
  - Do: Create main.go with CLI flags (port=30001, data-dir, token), Go 1.22 ServeMux patterns (POST/GET/DELETE with {appId}, {channel}, {version}), auth middleware, structured JSON logging. Write handler/integration_test.go with TestFullMultiAppWorkflow: upload to app1, upload to app2, verify feed served at /{appId}/{channel}/releases.win.json, verify .nupkg download, list versions, promote beta→stable, delete version, verify auth rejection, verify multi-app isolation (apps don't see each other). Verify curl-compatible multipart format.
  - Verify: `cd update-hub && go test ./... -count=1 -v`
  - Done when: Full test suite passes, integration test proves Velopack SimpleWebSource-compatible feed serving works, main.go compiles and runs

## Files Likely Touched

- `update-hub/go.mod`
- `update-hub/main.go`
- `update-hub/model/release.go`
- `update-hub/storage/store.go`
- `update-hub/storage/cleanup.go`
- `update-hub/storage/store_test.go`
- `update-hub/handler/upload.go`
- `update-hub/handler/upload_test.go`
- `update-hub/handler/static.go`
- `update-hub/handler/static_test.go`
- `update-hub/handler/list.go`
- `update-hub/handler/list_test.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/promote_test.go`
- `update-hub/handler/delete.go`
- `update-hub/handler/delete_test.go`
- `update-hub/middleware/auth.go`
- `update-hub/middleware/auth_test.go`
- `update-hub/handler/integration_test.go`
