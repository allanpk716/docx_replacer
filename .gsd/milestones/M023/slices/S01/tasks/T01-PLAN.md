---
estimated_steps: 6
estimated_files: 5
skills_used:
  - api-design
  - verify-before-complete
---

# T01: Create Go module + data model + multi-app storage layer

**Slice:** S01 — Go 服务器核心 API（多应用 Velopack 分发）
**Milestone:** M023

## Description

Create the update-hub Go project from scratch with the module definition, Velopack feed data model, and the multi-app file-system storage layer. This is the foundation all handlers depend on.

The key difference from the existing `update-server/storage/` is multi-app namespacing: all paths become `data/{appId}/{channel}/` instead of `data/{channel}/`. Feed file handling is generalized from hardcoded `releases.win.json` to any `releases.{os}.json` filename.

Reference implementation: `update-server/storage/store.go` (~120 lines), `update-server/model/release.go` (~20 lines), `update-server/storage/cleanup.go` (~100 lines).

## Steps

1. Create `update-hub/` directory and `go.mod` with `module update-hub` and `go 1.22`
2. Create `model/release.go` — port the existing `ReleaseFeed` and `ReleaseAsset` structs unchanged (JSON tags match Velopack protocol). Add a helper `IsFeedFilename(name string) bool` that matches `releases.*.json` pattern
3. Create `storage/store.go` with the following methods (all using `data/{appId}/{channel}/` paths):
   - `NewStore(dataDir string) *Store`
   - `AppDir(appId string) string` → `data/{appId}/`
   - `ChannelDir(appId, channel string) string` → `data/{appId}/{channel}/`
   - `EnsureDir(appId, channel string) error` — create channel directory
   - `ReadReleaseFeed(appId, channel, filename string) (*model.ReleaseFeed, error)` — parse releases.*.json, return empty feed if not found
   - `WriteReleaseFeed(appId, channel, filename string, feed *model.ReleaseFeed) error` — atomic write (temp + rename)
   - `ReadFile(appId, channel, filename string) ([]byte, error)`
   - `WriteFile(appId, channel, filename string, data []byte) error` — atomic write
   - `ListFiles(appId, channel string) ([]string, error)` — list .nupkg files
   - `DeleteVersion(appId, channel, version string) error` — delete .nupkg files matching version
   - `ListFeedFiles(appId, channel string) ([]string, error)` — list releases.*.json files in channel dir
4. Create `storage/cleanup.go` — port `CleanupOldVersions` with `(appId, channel string, maxKeep int)` signature. Adapt to handle all feed files in a channel (iterate ListFeedFiles, update each)
5. Write `storage/store_test.go` — adapt existing tests for multi-app paths:
   - Test multi-app isolation (two apps with same channel name don't interfere)
   - Test multi-OS feed files (read/write releases.linux.json)
   - Test EnsureDir creates nested structure
   - Test atomic write (temp file cleaned up on success)
   - Test ListFiles only returns .nupkg
   - Test DeleteVersion removes correct files
6. Write `storage/cleanup_test.go` — test cleanup with appId parameter, verify oldest version removed

## Must-Haves

- [ ] go.mod with module name `update-hub` and go 1.22
- [ ] ReleaseFeed/ReleaseAsset structs with correct Velopack JSON tags
- [ ] IsFeedFilename helper matching `releases.*.json`
- [ ] Store methods with appId parameter in all paths
- [ ] Atomic writes (temp + rename) for both feeds and artifacts
- [ ] CleanupOldVersions works per-app per-channel
- [ ] All storage tests pass

## Verification

- `cd update-hub && go test ./storage/... -count=1 -v` — all tests pass
- `cd update-hub && go vet ./storage/... ./model/...` — no warnings

## Inputs

- `update-server/model/release.go` — existing Velopack data model to port
- `update-server/storage/store.go` — existing storage implementation to adapt
- `update-server/storage/cleanup.go` — existing cleanup logic to adapt
- `update-server/storage/store_test.go` — existing test patterns to adapt
- `update-server/storage/cleanup_test.go` — existing cleanup test patterns to adapt

## Expected Output

- `update-hub/go.mod` — Go module definition
- `update-hub/model/release.go` — Velopack feed data model + IsFeedFilename helper
- `update-hub/storage/store.go` — multi-app file-system storage layer
- `update-hub/storage/cleanup.go` — version cleanup logic with appId
- `update-hub/storage/store_test.go` — comprehensive storage tests
