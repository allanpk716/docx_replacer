---
estimated_steps: 5
estimated_files: 2
skills_used:
  - api-design
  - verify-before-complete
---

# T04: Wire main.go with Go 1.22 ServeMux routes + write end-to-end integration test

**Slice:** S01 — Go 服务器核心 API（多应用 Velopack 分发）
**Milestone:** M023

## Description

Create main.go that wires all handlers using Go 1.22's enhanced ServeMux with method+path patterns, and write a comprehensive integration test that proves the complete Velopack-compatible workflow. This is the proof task — it demonstrates that all pieces work together to satisfy the slice demo: "curl 上传 .nupkg 到新服务器 /api/apps/docufiller/channels/stable/releases，Velopack SimpleWebSource 客户端从 /docufiller/stable/releases.win.json 成功拉取更新".

Go 1.22 ServeMux supports patterns like `"POST /api/apps/{appId}/channels/{channel}/releases"` with method matching and path parameter extraction via `r.PathValue("appId")`.

## Steps

1. Create `main.go`:
   - CLI flags: `-port` (default 30001), `-data-dir` (default `./data`), `-token` (default empty, disables auth)
   - Create `storage.NewStore(dataDir)`
   - Create handlers: `NewUploadHandler(store)`, `NewListHandler(store)`, `NewPromoteHandler(store)`, `NewDeleteHandler(store)`, `NewStaticHandler(store)`
   - Register routes with Go 1.22 ServeMux:
     ```
     "POST /api/apps/{appId}/channels/{channel}/releases"     → uploadHandler
     "GET /api/apps/{appId}/channels/{channel}/releases"       → listHandler
     "POST /api/apps/{appId}/channels/{target}/promote"        → promoteHandler
     "DELETE /api/apps/{appId}/channels/{channel}/versions/{version}" → deleteHandler
     "/{appId}/"                                                → staticHandler (catch-all for /appId/channel/file)
     ```
   - Apply `middleware.BearerAuth(token)` wrapper
   - Wrap with loggingMiddleware (structured JSON: method, path, status, duration_ms)
   - Configure timeouts: ReadTimeout=30s, WriteTimeout=60s, IdleTimeout=120s
   - Log startup: port, data-dir, token status
2. Write `handler/integration_test.go` with `TestFullMultiAppWorkflow`:
   - Setup: temp dir, Store, all handlers, httptest.NewServer with auth middleware (token="test-secret")
   - Subtests:
     - "UploadToDocuFillerBeta": POST /api/apps/docufiller/channels/beta/releases with releases.win.json + .nupkg, verify 200
     - "UploadToGoAppStable": POST /api/apps/go-tool/channels/stable/releases with releases.linux.json + .nupkg, verify 200 (second app, different OS)
     - "ServeDocuFillerFeed": GET /docufiller/beta/releases.win.json → verify correct Velopack feed JSON (no auth needed)
     - "ServeDocuFillerNupkg": GET /docufiller/beta/{filename}.nupkg → verify binary content (no auth needed)
     - "ServeGoAppFeed": GET /go-tool/stable/releases.linux.json → verify Linux feed (multi-OS proof)
     - "ListDocuFillerVersions": GET /api/apps/docufiller/channels/beta/releases → verify version list (public GET)
     - "PromoteToStable": POST /api/apps/docufiller/channels/stable/promote?from=beta&version={ver} → verify 200, files copied
     - "DeleteFromBeta": DELETE /api/apps/docufiller/channels/beta/versions/{ver} → verify 200
     - "AuthRejection": POST without Bearer token → verify 401
     - "MultiAppIsolation": Verify docufiller/beta doesn't contain go-tool files
     - "AutoRegistration": Verify docufiller feed's PackageId was validated against URL appId
     - "DynamicChannel": Upload to /api/apps/docufiller/channels/nightly/releases → verify works (no hardcoded channels)
3. Verify the integration test format matches curl upload pattern from build-internal.bat (multipart form with `file` field name)
4. Run full test suite: `cd update-hub && go test ./... -count=1 -v`
5. Run static analysis: `cd update-hub && go vet ./...`

## Must-Haves

- [ ] main.go compiles and runs with proper CLI flags
- [ ] Go 1.22 ServeMux patterns correctly route to handlers
- [ ] Integration test proves: upload → feed serve → .nupkg serve → list → promote → delete
- [ ] Integration test proves: multi-app isolation (two apps don't interfere)
- [ ] Integration test proves: multi-OS feeds (releases.win.json + releases.linux.json)
- [ ] Integration test proves: auth rejection on POST without token
- [ ] Integration test proves: dynamic channels (non-standard channel name works)
- [ ] Full test suite passes with zero failures

## Verification

- `cd update-hub && go test ./... -count=1 -v` — all tests pass
- `cd update-hub && go vet ./...` — no warnings
- `cd update-hub && go build -o update-hub.exe .` — compiles successfully

## Observability Impact

- Signals added/changed: structured JSON request logging (method, path, status, duration_ms), startup log with port/data-dir/token-status
- How a future agent inspects this: check stdout logs, inspect data/ directory structure, curl /{appId}/{channel}/releases.win.json
- Failure state exposed: per-request status codes in logs, 401/400/404/500 with error details in response body

## Inputs

- `update-hub/go.mod` — Go module (from T01)
- `update-hub/model/release.go` — data model (from T01)
- `update-hub/storage/store.go` — multi-app Store (from T01)
- `update-hub/storage/cleanup.go` — cleanup logic (from T01)
- `update-hub/handler/upload.go` — upload handler (from T02)
- `update-hub/handler/static.go` — static handler (from T03)
- `update-hub/handler/list.go` — list handler (from T03)
- `update-hub/handler/promote.go` — promote handler (from T03)
- `update-hub/handler/delete.go` — delete handler (from T03)
- `update-hub/middleware/auth.go` — auth middleware (from T02)
- `update-server/main.go` — reference main.go implementation
- `update-server/handler/upload_test.go` — reference integration test (TestFullUploadWorkflow)

## Expected Output

- `update-hub/main.go` — server entry point with Go 1.22 routing
- `update-hub/handler/integration_test.go` — end-to-end integration test proving Velopack compatibility
