---
estimated_steps: 28
estimated_files: 5
skills_used: []
---

# T01: Scaffold Go project with static file serving and channel directory structure

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

## Inputs

- None specified.

## Expected Output

- `update-server/go.mod`
- `update-server/main.go`
- `update-server/handler/static.go`
- `update-server/model/release.go`
- `update-server/storage/store.go`

## Verification

cd update-server && go build -o bin/update-server.exe . && echo BUILD_OK
