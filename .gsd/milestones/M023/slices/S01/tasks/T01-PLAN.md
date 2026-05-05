---
estimated_steps: 7
estimated_files: 5
skills_used: []
---

# T01: Create Go module + data model + multi-app storage layer

Create the update-hub Go project with go.mod, the Velopack feed data model (ReleaseFeed/ReleaseAsset), and a multi-app file-system storage layer. The storage layer uses data/{appId}/{channel}/ directory structure (vs the old data/{channel}/), supports any OS feed filename (releases.win.json, releases.linux.json, etc.) instead of hardcoded releases.win.json, and preserves atomic write (temp+rename). Port cleanup logic with appId parameter. Write comprehensive unit tests.

Steps:
1. Create update-hub/ directory with go.mod (module update-hub, go 1.22)
2. Port model/release.go unchanged (ReleaseFeed, ReleaseAsset structs with JSON tags matching Velopack format)
3. Create storage/store.go with multi-app methods: NewStore, AppDir(appId), ChannelDir(appId,channel), EnsureDir, ReadReleaseFeed(appId,channel,filename), WriteReleaseFeed(appId,channel,filename,feed), ReadFile, WriteFile, ListFiles, DeleteVersion — all using data/{appId}/{channel}/ paths
4. Port storage/cleanup.go with CleanupOldVersions(appId,channel,maxKeep)
5. Write storage/store_test.go adapting existing tests for multi-app paths, multi-OS feed files, cleanup

## Inputs

- `update-server/model/release.go`
- `update-server/storage/store.go`
- `update-server/storage/cleanup.go`
- `update-server/storage/store_test.go`
- `update-server/storage/cleanup_test.go`

## Expected Output

- `update-hub/go.mod`
- `update-hub/model/release.go`
- `update-hub/storage/store.go`
- `update-hub/storage/cleanup.go`
- `update-hub/storage/store_test.go`

## Verification

cd update-hub && go test ./storage/... -count=1 -v
