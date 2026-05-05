---
estimated_steps: 7
estimated_files: 2
skills_used: []
---

# T04: Wire main.go with Go 1.22 ServeMux routes + write end-to-end integration test

Create main.go that wires all handlers using Go 1.22's enhanced ServeMux with method+path patterns (e.g. mux.HandleFunc("POST /api/apps/{appId}/channels/{channel}/releases", uploadHandler)). Add CLI flags for port (default 30001), data-dir, and token. Apply auth middleware and structured JSON logging. Write a comprehensive integration test that proves the full Velopack-compatible workflow: upload files via multipart POST → verify feed served at /{appId}/{channel}/releases.win.json → verify .nupkg served → list versions → promote → delete → verify auth rejection. This test uses httptest.NewServer with real handlers and temp directory storage.

Steps:
1. Create main.go: parse flags (port, data-dir, token), create Store, create handlers, register routes using Go 1.22 ServeMux patterns (POST/GET/DELETE with {appId}, {channel}, {version} path params), apply BearerAuth middleware, wrap with loggingMiddleware, start server
2. Use Go 1.22 patterns: "POST /api/apps/{appId}/channels/{channel}/releases", "GET /api/apps/{appId}/channels/{channel}/releases", "POST /api/apps/{appId}/channels/{target}/promote", "DELETE /api/apps/{appId}/channels/{channel}/versions/{version}", "/{appId}/{channel}/" for static
3. Write handler/integration_test.go: TestFullMultiAppWorkflow with subtests for upload-to-app1, upload-to-app2, static-serve-feed, static-serve-nupkg, list, promote, delete, auth-rejection, multi-app-isolation (different apps don't see each other's data)
4. Verify curl-compatible upload format in integration test (multipart with file field names matching build-internal.bat pattern)
5. Run full test suite and verify all tests pass

## Inputs

- `update-hub/go.mod`
- `update-hub/model/release.go`
- `update-hub/storage/store.go`
- `update-hub/storage/cleanup.go`
- `update-hub/handler/upload.go`
- `update-hub/handler/static.go`
- `update-hub/handler/list.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/middleware/auth.go`
- `update-server/main.go`
- `update-server/handler/upload_test.go`

## Expected Output

- `update-hub/main.go`
- `update-hub/handler/integration_test.go`

## Verification

cd update-hub && go test ./... -count=1 -v
