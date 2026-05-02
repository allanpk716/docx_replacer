---
estimated_steps: 30
estimated_files: 5
skills_used: []
---

# T04: Write Go unit tests and curl-based integration test script

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

## Inputs

- `update-server/storage/store.go`
- `update-server/storage/cleanup.go`
- `update-server/handler/upload.go`
- `update-server/handler/promote.go`
- `update-server/handler/list.go`
- `update-server/handler/static.go`
- `update-server/middleware/auth.go`
- `update-server/model/release.go`

## Expected Output

- `update-server/storage/store_test.go`
- `update-server/storage/cleanup_test.go`
- `update-server/handler/handler_test.go`
- `update-server/handler/upload_test.go`
- `scripts/test-update-server.sh`

## Verification

cd update-server && go test ./... -v -count=1 2>&1 | tail -20
