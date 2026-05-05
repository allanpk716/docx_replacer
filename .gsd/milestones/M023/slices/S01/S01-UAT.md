# S01: Go 服务器核心 API（多应用 Velopack 分发） — UAT

**Milestone:** M023
**Written:** 2026-05-05T06:28:17.605Z

# S01: Go 服务器核心 API（多应用 Velopack 分发） — UAT

**Milestone:** M023
**Written:** 2026-05-05

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: All functionality is proven by the integration test (TestFullMultiAppWorkflow) which exercises the full HTTP API through httptest.NewServer with real handlers and temp directory storage. No live runtime or human experience testing is required for this backend API slice.

## Preconditions

- Go 1.22+ installed with GOCACHE environment variable set
- update-hub directory with go.mod and all source files
- No external services or network required (all tests use httptest)

## Smoke Test

Run `cd update-hub && go test ./handler/... -run TestFullMultiAppWorkflow -count=1 -v` — the integration test should PASS with all 15 subtests.

## Test Cases

### 1. Upload Windows feed + .nupkg to docufiller/beta

1. POST multipart form to /api/apps/docufiller/channels/beta/releases with releases.win.json + .nupkg
2. Response should be 200 with channel "beta" and files_received count
3. GET /docufiller/beta/releases.win.json should return valid Velopack feed JSON
4. GET /docufiller/beta/{filename}.nupkg should return the uploaded binary

### 2. Upload Linux feed to different app (multi-app isolation)

1. POST multipart to /api/apps/go-tool/channels/stable/releases with releases.linux.json + .nupkg
2. GET /go-tool/stable/releases.linux.json should return feed with correct PackageId
3. GET /docufiller/stable/releases.linux.json should return 404 (no cross-app leak)

### 3. Version listing

1. GET /api/apps/docufiller/channels/beta/releases (with Bearer token)
2. Should return JSON array with versions sorted descending by semver

### 4. Promote beta → stable

1. POST /api/apps/docufiller/channels/stable/promote?from=beta&version={version} (with Bearer token)
2. GET /docufiller/stable/releases.win.json should now include the promoted version
3. .nupkg file should be physically copied to stable directory

### 5. Delete version

1. DELETE /api/apps/docufiller/channels/beta/versions/{version} (with Bearer token)
2. GET /docufiller/beta/releases.win.json should no longer include the deleted version
3. .nupkg file should be removed from beta directory

### 6. Auth enforcement

1. POST without Authorization header → 401
2. POST with wrong token → 401
3. GET static paths without token → 200 (public Velopack access)

### 7. Dynamic channel

1. Upload to /api/apps/docufiller/channels/nightly/releases
2. GET /docufiller/nightly/releases.win.json should serve the feed
3. No server configuration needed — any channel name works

## Edge Cases

### Auto-registration mismatch

1. Upload feed with PackageId "com.other.app" to /api/apps/docufiller/channels/stable/releases
2. Should return 400 with error about PackageId mismatch

### Non-existent resources

1. GET /nonexistent/stable/releases.win.json → 404
2. GET /docufiller/nonexistent/releases.win.json → 404

### Path traversal protection

1. GET /docufiller/stable/../../../etc/passwd → 400 or 404 (rejected)

### Idempotent delete

1. DELETE a version that doesn't exist → 200 with files_deleted: 0

## Failure Signals

- Any test FAIL in `go test ./... -count=1 -v`
- go vet reporting warnings
- go build failing to compile

## Not Proven By This UAT

- Live server startup with `-port`, `-data-dir`, `-token` CLI flags (only tested via integration test's programmatic setup)
- Actual Velopack SimpleWebSource client connecting over network (only feed format compatibility verified)
- Performance under concurrent load
- NSSM service deployment on Windows Server 2019
- Web UI (S03 scope)
- SQLite metadata layer (S02 scope)
- Data migration from old format (S04 scope)

## Notes for Tester

- The initial verification failure was an environment issue (missing GOCACHE/LOCALAPPDATA), not a code bug. Ensure Go build cache is configured.
- Run `export GOCACHE=$HOME/.cache/go-build` if GOCACHE is not set.
- Integration test creates temp directories that are cleaned up automatically.
- Auth is disabled when `-token` is empty (empty string).
