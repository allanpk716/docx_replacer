---
id: T01
parent: S01
milestone: M023
key_files:
  - update-hub/go.mod
  - update-hub/model/release.go
  - update-hub/storage/store.go
  - update-hub/storage/cleanup.go
  - update-hub/storage/store_test.go
key_decisions:
  - Feed filename parameterized instead of hardcoded — supports any OS feed (releases.win.json, releases.linux.json, releases.osx.json) via caller-supplied filename
  - CleanupOldVersions takes feedFilename parameter rather than scanning for all releases.*.json files — caller decides which feed to clean
duration: 
verification_result: passed
completed_at: 2026-05-05T06:05:33.908Z
blocker_discovered: false
---

# T01: Created update-hub Go module with multi-app storage layer using data/{appId}/{channel}/ paths and multi-OS feed support

**Created update-hub Go module with multi-app storage layer using data/{appId}/{channel}/ paths and multi-OS feed support**

## What Happened

Ported the storage layer from the single-app update-server to the new multi-app update-hub project. Key changes from the original:

1. **Directory layout** changed from `data/{channel}/` to `data/{appId}/{channel}/`, with `AppDir(appId)` and `ChannelDir(appId, channel)` helpers.
2. **Feed filename** is no longer hardcoded to `releases.win.json` — all methods now accept a `filename` parameter, enabling `releases.linux.json`, `releases.osx.json`, etc.
3. **CleanupOldVersions** takes `appId` and `feedFilename` parameters and logs structured JSON with `appId` context.
4. **Atomic write** (temp+rename) pattern preserved from original, per MEM083.
5. All methods (ReadReleaseFeed, WriteReleaseFeed, ReadFile, WriteFile, ListFiles, DeleteVersion) updated with appId parameter.

Created 26 unit tests covering: path helpers, multi-app isolation, multi-OS feed files, atomic overwrite, cleanup with semver sorting, and cross-app isolation during cleanup and deletion. All tests pass.

## Verification

Ran `cd update-hub && go test ./storage/... -count=1 -v` — all 26 tests passed in 1.008s. Tests cover multi-app path isolation (different apps get different directories), multi-OS feed files (win/linux/osx), atomic writes (no temp files left behind), cleanup semver sorting, and cross-app isolation during both cleanup and deletion.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && go test ./storage/... -count=1 -v` | 0 | ✅ pass | 10080ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `update-hub/go.mod`
- `update-hub/model/release.go`
- `update-hub/storage/store.go`
- `update-hub/storage/cleanup.go`
- `update-hub/storage/store_test.go`
