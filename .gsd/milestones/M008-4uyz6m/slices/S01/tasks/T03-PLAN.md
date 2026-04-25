---
estimated_steps: 29
estimated_files: 4
skills_used: []
---

# T03: Implement promote, version list, and auto-cleanup APIs

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

## Inputs

- `update-server/main.go`
- `update-server/storage/store.go`
- `update-server/model/release.go`
- `update-server/handler/upload.go`

## Expected Output

- `update-server/handler/promote.go`
- `update-server/handler/list.go`
- `update-server/storage/cleanup.go`
- `update-server/main.go`

## Verification

cd update-server && go build -o bin/update-server.exe . && echo BUILD_OK

## Observability Impact

Signals added: promote logs (source channel, target channel, version, files copied), cleanup logs (channel, versions removed, count)
How a future agent inspects: GET /api/channels/{channel}/releases shows current versions
Failure state exposed: 404 on missing version, cleanup errors logged with channel name and version list
