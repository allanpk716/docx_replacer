# S01: Go 更新服务器

**Goal:** Build a Go-based lightweight update server in update-server/ that serves Velopack release artifacts over HTTP with stable/beta dual-channel support. Provides static file serving, upload API with token auth, promote API, version listing API, and automatic version cleanup (keep last 10 per channel).
**Demo:** 启动服务器，curl 上传 .nupkg 到 beta 通道，从 /beta/releases.win.json 下载，promote 到 stable，查询版本列表，上传第 11 个版本触发自动清理

## Must-Haves

- `go build` compiles to single binary with zero errors
- Server starts, curl uploads .nupkg to beta channel, static file serves /beta/releases.win.json
- Promote API copies version from beta to stable, /stable/releases.win.json updated
- GET /api/channels/{channel}/releases returns version list JSON
- Uploading 11th version triggers auto-cleanup, oldest version removed
- All `go test` pass

## Proof Level

- This slice proves: contract — each API endpoint verified via curl and go test with assertions on HTTP status codes, response bodies, and file system state

## Integration Closure

Upstream surfaces consumed: none (leaf node)
New wiring introduced: Go HTTP server at update-server/main.go, serving on configurable port
What remains before milestone is usable: S02 (client channel support), S03 (build script), S04 (e2e verification)

## Verification

- Runtime signals: structured JSON logging for each HTTP request (method, path, status, duration)
- Inspection surfaces: GET /api/channels/{channel}/releases for version audit
- Failure visibility: error logs with channel name, filename, and operation context

## Tasks

- [x] **T01: Scaffold Go project with static file serving and channel directory structure** `est:1h`
  Create the Go project scaffold under update-server/ with go.mod, main.go entrypoint, and core HTTP server.

Steps:
1. Create update-server/go.mod with module name `docufiller-update-server`
2. Create update-server/main.go with CLI flag parsing (-port, -data-dir, -token) and HTTP server startup
3. Create update-server/handler/static.go implementing static file handler:
   - Serve GET /{channel}/releases.win.json from {data-dir}/{channel}/releases.win.json
   - Serve GET /{channel}/*.nupkg from {data-dir}/{channel}/ files
   - Return 404 for missing files
   - Set correct Content-Type headers (application/json for .json, application/octet-stream for .nupkg)
4. Create update-server/model/release.go defining the Velopack releases JSON model:
   ```go
   type ReleaseFeed struct { Assets []ReleaseAsset `json:"Assets"` }
   type ReleaseAsset struct {
     PackageId string `json:"PackageId"`
     Version   string `json:"Version"`
     Type      string `json:"Type"`    // Full or Delta
     FileName  string `json:"FileName"`
     SHA1      string `json:"SHA1"`
     Size      int64  `json:"Size"`
   }
   ```
5. Create update-server/storage/store.go with file system storage abstraction:
   - EnsureChannelDir(channel) — create {data-dir}/{channel}/ if not exists
   - ReadReleaseFeed(channel) — parse releases.win.json, return empty feed if not exists
   - WriteReleaseFeed(channel, feed) — write releases.win.json atomically
   - ListFiles(channel) — list .nupkg files in channel directory
   - DeleteVersion(channel, version) — remove all .nupkg files matching version
6. Verify: `go build -o update-server/bin/update-server.exe ./update-server/...` compiles, and running it with a temp data directory starts without error
  - Files: `update-server/go.mod`, `update-server/main.go`, `update-server/handler/static.go`, `update-server/model/release.go`, `update-server/storage/store.go`
  - Verify: cd update-server && go build -o bin/update-server.exe . && echo BUILD_OK

- [x] **T02: Implement upload API with token authentication** `est:1.5h`
  Add the upload API endpoint and Bearer token authentication middleware.

Steps:
1. Create update-server/middleware/auth.go:
   - Bearer token validation middleware
   - Extract token from Authorization header
   - Compare against configured token (from -token flag)
   - Return 401 if missing/invalid, pass through for valid tokens
   - Skip auth for GET requests to static file paths (/{channel}/*) and GET /api/channels/{channel}/releases
2. Create update-server/handler/upload.go:
   - POST /api/channels/{channel}/releases
   - Accept multipart form upload with files (releases.win.json, .nupkg, Setup.exe, Portable.zip, etc.)
   - Validate channel name (only alphanumeric + hyphen, e.g. stable, beta)
   - For each uploaded file, write to {data-dir}/{channel}/ directory
   - When releases.win.json is uploaded, parse it to extract version info, then merge with existing feed:
     a. Read existing releases.win.json from channel dir (if exists)
     b. Parse the uploaded releases.win.json
     c. Merge new assets into existing feed (append, avoiding duplicates by FileName)
     d. Write updated releases.win.json
   - Return 200 with JSON summary: {"channel": "beta", "files_received": 3, "versions_added": ["1.2.0"]}
   - Return 400 for invalid multipart or missing channel
   - Return 404 for non-existent channel path (should auto-create on first upload)
3. Wire routes in main.go:
   - Apply auth middleware to /api/* paths
   - Register upload handler
   - Static file handler continues to serve without auth
4. Verify: start server, use curl to upload a test .nupkg + releases.win.json, confirm files appear in channel dir
  - Files: `update-server/middleware/auth.go`, `update-server/handler/upload.go`, `update-server/main.go`
  - Verify: cd update-server && go build -o bin/update-server.exe . && echo BUILD_OK

- [x] **T03: Implement promote, version list, and auto-cleanup APIs** `est:1.5h`
  Add the remaining API endpoints: promote, version listing, and auto-cleanup logic.

Steps:
1. Create update-server/handler/promote.go:
   - POST /api/channels/stable/promote?from=beta&version={version}
   - Validate source channel exists and version exists in it
   - Copy all .nupkg files matching version from source channel to stable dir
   - Merge source channel's version assets into stable's releases.win.json
   - Trigger auto-cleanup on target channel after promote
   - Return 200 with {"promoted": "1.2.0", "from": "beta", "to": "stable", "files_copied": 3}
   - Return 404 if version not found in source channel
   - Return 400 if from/to params invalid
2. Create update-server/handler/list.go:
   - GET /api/channels/{channel}/releases
   - Read releases.win.json from channel dir
   - Extract unique versions with their file count, total size, and list of file names
   - Return JSON: {"channel": "stable", "versions": [{"version": "1.2.0", "files": [...], "total_size": 5000000, "file_count": 3}], "total_versions": 5}
   - Return 404 if channel directory doesn't exist
   - No auth required (read-only)
3. Add auto-cleanup logic to update-server/storage/cleanup.go:
   - Function CleanupOldVersions(channel, maxKeep int) error
   - Read releases.win.json, extract unique versions sorted by semver (descending)
   - If more than maxKeep versions, identify versions to remove
   - Delete .nupkg files for removed versions from filesystem
   - Remove corresponding entries from releases.win.json
   - Write updated releases.win.json
   - maxKeep = 10 (configurable via constant)
   - Call after every upload and promote
4. Wire new routes in main.go
5. Verify: `go build` compiles
  - Files: `update-server/handler/promote.go`, `update-server/handler/list.go`, `update-server/storage/cleanup.go`, `update-server/main.go`
  - Verify: cd update-server && go build -o bin/update-server.exe . && echo BUILD_OK

- [x] **T04: Write Go unit tests and curl-based integration test script** `est:1.5h`
  Add comprehensive tests covering all API endpoints, auth, edge cases, and cleanup.

Steps:
1. Create update-server/storage/store_test.go:
   - Test ReadReleaseFeed with existing/missing files
   - Test WriteReleaseFeed creates correct JSON
   - Test EnsureChannelDir creates directory structure
2. Create update-server/storage/cleanup_test.go:
   - Test CleanupOldVersions with < 10 versions (no cleanup)
   - Test CleanupOldVersions with 11 versions (oldest removed)
   - Test cleanup removes files from filesystem and updates releases.win.json
3. Create update-server/handler/handler_test.go:
   - Test upload: valid multipart → 200, missing auth → 401, bad channel → 400
   - Test promote: valid version → 200, missing version → 404
   - Test list: returns correct version structure
   - Test static: serves releases.win.json and .nupkg correctly
   - Use httptest.NewServer for all tests
4. Create update-server/handler/upload_test.go with integration-style tests:
   - Full upload workflow: create temp data dir, upload files via httptest, verify filesystem state
5. Create scripts/test-update-server.sh (bash script for curl-based verification):
   - Start server in background with temp data dir
   - Upload test .nupkg + releases.win.json to beta channel
   - GET /beta/releases.win.json and verify content
   - Promote to stable
   - GET /stable/releases.win.json and verify content
   - List versions for both channels
   - Upload 11 versions and verify cleanup
   - Test auth rejection (no token, bad token)
   - Print PASS/FAIL summary
   Note: The test data files (releases.win.json, .nupkg) should be generated inline in the script
6. Verify: `cd update-server && go test ./...` all pass
  - Files: `update-server/storage/store_test.go`, `update-server/storage/cleanup_test.go`, `update-server/handler/handler_test.go`, `update-server/handler/upload_test.go`, `scripts/test-update-server.sh`
  - Verify: cd update-server && go test ./... -v -count=1 2>&1 | tail -20

## Files Likely Touched

- update-server/go.mod
- update-server/main.go
- update-server/handler/static.go
- update-server/model/release.go
- update-server/storage/store.go
- update-server/middleware/auth.go
- update-server/handler/upload.go
- update-server/handler/promote.go
- update-server/handler/list.go
- update-server/storage/cleanup.go
- update-server/storage/store_test.go
- update-server/storage/cleanup_test.go
- update-server/handler/handler_test.go
- update-server/handler/upload_test.go
- scripts/test-update-server.sh
