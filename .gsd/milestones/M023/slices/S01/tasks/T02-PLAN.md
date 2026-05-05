---
estimated_steps: 5
estimated_files: 4
skills_used:
  - api-design
  - verify-before-complete
---

# T02: Implement upload handler with auto-registration + Bearer auth middleware

**Slice:** S01 — Go 服务器核心 API（多应用 Velopack 分发）
**Milestone:** M023

## Description

Build the upload handler for `POST /api/apps/{appId}/channels/{channel}/releases` and the Bearer token authentication middleware. The upload handler is the most complex handler — it handles multipart form data with any OS feed file, auto-registers apps from PackageId, and writes artifacts. The auth middleware protects write operations while allowing unauthenticated Velopack client reads.

Key design decisions:
- Use Go 1.22's `r.PathValue("appId")` and `r.PathValue("channel")` for path parameter extraction
- Detect feed files by `releases.*.json` pattern (not just releases.win.json)
- Auto-registration: extract PackageId from first asset in uploaded feed, validate it matches the appId in the URL path
- Channel validation: regex `^[a-zA-Z0-9-]+$` only (no hardcoded set, per D059)
- AppId validation: regex `^[a-zA-Z0-9-]+$`

## Failure Modes

| Dependency | On error | On timeout | On malformed response |
|------------|----------|-----------|----------------------|
| Filesystem (write) | Return 500, log error | N/A (no network) | N/A |
| Multipart parse | Return 400 with parse error message | N/A (30s read timeout in main) | Return 400 |
| Feed JSON parse | Return 500 "merge releases: parse error" | N/A | Return 500 |

## Negative Tests

- **Malformed inputs**: Empty multipart form, non-JSON file named releases.win.json, missing feed file (only .nupkg), appId with special chars
- **Error paths**: PackageId mismatch (feed says "OtherApp" but URL says "docufiller"), write permission denied (temp dir)
- **Boundary conditions**: Empty channel name, max-length channel name, upload with only .nupkg and no feed

## Steps

1. Create `handler/upload.go`:
   - `UploadHandler` struct with `Store *storage.Store`
   - `ServeHTTP` method:
     - Parse appId and channel from path via `r.PathValue("appId")` and `r.PathValue("channel")`
     - Validate appId and channel by regex `^[a-zA-Z0-9-]+$`
     - Parse multipart form (max 200MB)
     - Ensure channel directory exists
     - For each uploaded file:
       - If filename matches `releases.*.json` (via `model.IsFeedFilename`): parse as ReleaseFeed, validate PackageId matches appId (case-insensitive), merge with existing feed, write atomically
       - Otherwise: write as artifact file
     - Trigger auto-cleanup after upload
     - Return JSON response with channel, files_received, versions_added
   - UploadResponse struct: `{channel, files_received, versions_added}`
2. Implement auto-registration logic:
   - Extract PackageId from first non-empty asset in the uploaded feed
   - Compare with appId from URL (case-insensitive)
   - If mismatch, return 400: `{"error":"package ID mismatch: feed has X, URL has Y"}`
   - If no feed file uploaded, skip registration validation (just store artifacts)
3. Create `middleware/auth.go`:
   - `BearerAuth(configuredToken string) func(http.Handler) http.Handler`
   - If token is empty string, allow all requests (auth disabled)
   - Skip auth for GET requests to non-`/api/` paths (Velopack static serving)
   - Skip auth for GET requests ending in `/releases` (public version list)
   - All POST/DELETE to `/api/` paths require `Authorization: Bearer <token>` header
   - Use `subtle.ConstantTimeCompare` for timing-safe comparison
   - Return 401 with JSON error body on auth failure
4. Write `handler/upload_test.go`:
   - Test valid multipart upload with feed + .nupkg
   - Test upload with only .nupkg (no feed)
   - Test upload with releases.linux.json (multi-OS)
   - Test invalid channel name (special chars)
   - Test auto-registration: PackageId matches appId → success
   - Test auto-registration: PackageId mismatch → 400
   - Test method not allowed (GET)
5. Write `middleware/auth_test.go`:
   - Test GET to static path skips auth
   - Test GET to /api/.../releases skips auth
   - Test POST without token → 401
   - Test POST with wrong token → 401
   - Test POST with correct token → passes through
   - Test empty token config disables auth entirely

## Must-Haves

- [ ] Upload handler accepts multipart with any releases.*.json + .nupkg files
- [ ] Auto-registration validates PackageId matches appId (case-insensitive)
- [ ] Channel names validated by regex only (dynamic channels)
- [ ] Bearer auth middleware with timing-safe comparison
- [ ] Auth skipped for Velopack client GET paths
- [ ] All upload and auth tests pass

## Verification

- `cd update-hub && go test ./handler/... ./middleware/... -run 'TestUpload|TestAuth' -count=1 -v`

## Inputs

- `update-hub/go.mod` — Go module definition (from T01)
- `update-hub/model/release.go` — ReleaseFeed/ReleaseAsset + IsFeedFilename (from T01)
- `update-hub/storage/store.go` — multi-app Store (from T01)
- `update-server/handler/upload.go` — reference implementation for upload pattern
- `update-server/middleware/auth.go` — reference implementation for auth middleware

## Expected Output

- `update-hub/handler/upload.go` — upload handler with auto-registration and multi-OS feed handling
- `update-hub/middleware/auth.go` — Bearer token auth middleware
- `update-hub/handler/upload_test.go` — upload handler unit tests
- `update-hub/middleware/auth_test.go` — auth middleware unit tests
