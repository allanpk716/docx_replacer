---
id: T03
parent: S01
milestone: M008-4uyz6m
key_files:
  - update-server/handler/promote.go
  - update-server/handler/list.go
  - update-server/handler/api.go
  - update-server/storage/cleanup.go
  - update-server/handler/upload.go
  - update-server/storage/store.go
  - update-server/main.go
key_decisions:
  - Created APIHandler multiplexer (handler/api.go) to route /api/channels/ by path suffix and HTTP method, avoiding conflicts between upload/list/promote on the same prefix
  - Exported Store.ChannelDir() so external handlers can check directory existence
duration: 
verification_result: passed
completed_at: 2026-04-24T23:28:09.362Z
blocker_discovered: false
---

# T03: Implement promote, version list, and auto-cleanup APIs for update-server

**Implement promote, version list, and auto-cleanup APIs for update-server**

## What Happened

Implemented the three remaining API endpoints for the Go update-server:

1. **Promote API** (`handler/promote.go`): `POST /api/channels/{target}/promote?from={source}&version={version}` validates source/target channels, copies matching .nupkg files, merges release feed entries, triggers auto-cleanup, and returns a structured JSON response. Returns 404 if version not found in source, 400 on invalid params.

2. **Version List API** (`handler/list.go`): `GET /api/channels/{channel}/releases` reads the release feed, groups assets by version with file names/counts/sizes, sorts by semver descending, and returns structured JSON. No auth required (read-only). Returns 404 for unknown channels.

3. **Auto-cleanup logic** (`storage/cleanup.go`): `CleanupOldVersions(channel, maxKeep)` reads the release feed, identifies versions exceeding maxKeep (default 10), deletes associated .nupkg files from disk, prunes feed entries, and writes the updated feed atomically. Called after every upload and promote.

4. **API routing** (`handler/api.go`): Created a multiplexer that routes `/api/channels/` requests to upload (POST /releases), list (GET /releases), or promote (POST /promote) based on path suffix and HTTP method. Updated `main.go` to use `NewAPIHandler` instead of `NewUploadHandler`.

5. **Minor refactoring**: Exported `Store.ChannelDir()` method (was unexported `channelDir`) so the list handler can check directory existence. Also added post-upload cleanup call in `upload.go`.

All endpoints include structured JSON logging for observability. Build compiles cleanly, `go vet` passes.

## Verification

Verified with `go build -o bin/update-server.exe .` (exit code 0) and `go vet ./...` (no issues). All three new files compile correctly and wire into the existing routing via the API multiplexer.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-server && go build -o bin/update-server.exe .` | 0 | ✅ pass | 5000ms |
| 2 | `cd update-server && go vet ./...` | 0 | ✅ pass | 2000ms |

## Deviations

Plan called for wiring routes directly in main.go, but the existing upload handler already captured all /api/channels/ paths. Created handler/api.go as an API multiplexer instead — cleaner separation of concerns.

## Known Issues

None.

## Files Created/Modified

- `update-server/handler/promote.go`
- `update-server/handler/list.go`
- `update-server/handler/api.go`
- `update-server/storage/cleanup.go`
- `update-server/handler/upload.go`
- `update-server/storage/store.go`
- `update-server/main.go`
