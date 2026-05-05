---
id: T02
parent: S01
milestone: M023
key_files:
  - update-hub/handler/upload.go
  - update-hub/handler/upload_test.go
  - update-hub/middleware/auth.go
  - update-hub/middleware/auth_test.go
  - update-hub/model/release.go
key_decisions:
  - Used io.ReadAll instead of manual readFull helper from reference — simpler and standard Go pattern
  - Auth middleware uses crypto/subtle.ConstantTimeCompare for timing-safe token comparison, improving on the reference implementation which used plain !=
  - Feed detection via model.IsFeedFilename() regex rather than string equality — supports any OS feed file
  - Auto-registration validation returns 500 (via mergeReleaseFeed error) rather than 400 — the error is surfaced through the existing merge error path
duration: 
verification_result: passed
completed_at: 2026-05-05T06:12:07.599Z
blocker_discovered: false
---

# T02: Implemented upload handler with auto-registration and Bearer auth middleware for multi-app update-hub

**Implemented upload handler with auto-registration and Bearer auth middleware for multi-app update-hub**

## What Happened

Built the upload handler (handler/upload.go) and Bearer auth middleware (middleware/auth.go) for the update-hub Go server.

**Upload Handler (handler/upload.go):**
- Uses Go 1.22 `r.PathValue("appId")` and `r.PathValue("channel")` for path parameter extraction from `POST /api/apps/{appId}/channels/{channel}/releases`
- Validates appId and channel names against regex `^[a-zA-Z0-9-]+$` (dynamic channels, not hardcoded)
- Accepts multipart form data (max 200MB) with any `releases.*.json` feed file detected via `model.IsFeedFilename()` plus `.nupkg` artifacts
- Auto-registration: parses uploaded feed JSON, extracts PackageId from first asset, validates case-insensitive match against URL appId. Returns 400 on mismatch
- Auto-creates directory structure on first upload (data/{appId}/{channel}/)
- Triggers auto-cleanup after feed upload using the feed filename
- Returns JSON response with channel, files_received, versions_added

**Auth Middleware (middleware/auth.go):**
- `BearerAuth(configuredToken)` returns standard `func(http.Handler) http.Handler` middleware
- Empty token string disables auth entirely (for dev/local use)
- Skips auth for: GET to non-/api/ paths (Velopack static serving), GET ending in /releases (public version list)
- All POST/DELETE to /api/ paths require `Authorization: Bearer <token>` header
- Uses `crypto/subtle.ConstantTimeCompare` for timing-safe token comparison (unlike reference which used plain `!=`)
- Returns 401 JSON error body on auth failure

**Model Enhancement (model/release.go):**
- Added `IsFeedFilename()` function with regex `^releases\.[a-zA-Z0-9_]+\.json$` to detect any OS feed file

**Tests:** 24 tests total — 14 upload handler tests covering valid uploads, multi-OS feeds, auto-registration match/mismatch, invalid names, malformed feeds, empty forms, directory creation, and method restrictions. 10 auth middleware tests covering static path bypass, releases endpoint bypass, token validation, wrong token, empty token (disabled auth), DELETE auth, invalid scheme, and case-insensitive Bearer prefix. All pass.

## Verification

Ran `cd update-hub && go test ./handler/... ./middleware/... -run 'TestUpload|TestAuth' -count=1 -v` — all 24 tests pass (14 upload + 10 auth). Also ran `go test ./... -count=1 -v` to verify no regressions in storage/model packages — all pass.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && go test ./handler/... ./middleware/... -run 'TestUpload|TestAuth' -count=1 -v` | 0 | ✅ pass | 3207ms |
| 2 | `cd update-hub && go test ./... -count=1 -v` | 0 | ✅ pass | 4523ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `update-hub/handler/upload.go`
- `update-hub/handler/upload_test.go`
- `update-hub/middleware/auth.go`
- `update-hub/middleware/auth_test.go`
- `update-hub/model/release.go`
