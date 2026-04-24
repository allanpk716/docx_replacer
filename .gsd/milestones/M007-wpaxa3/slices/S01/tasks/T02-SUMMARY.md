---
id: T02
parent: S01
milestone: M007-wpaxa3
key_files:
  - App.config
  - scripts/build-internal.bat
  - scripts/sync-version.bat
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-24T05:43:01.890Z
blocker_discovered: false
---

# T02: Remove old update system residuals from App.config, build-internal.bat, and sync-version.bat

**Remove old update system residuals from App.config, build-internal.bat, and sync-version.bat**

## What Happened

Cleaned all old update system residuals from three files:

**App.config**: Removed the XML comment `<!-- 更新配置 -->` and three old update config entries (UpdateServerUrl, UpdateChannel, CheckUpdateOnStartup). All other appSettings entries (log, file processing, performance, UI) remain intact.

**build-internal.bat**: Major cleanup — removed the `call :COPY_EXTERNAL_FILES` invocation, the entire `:COPY_EXTERNAL_FILES` function block, the `if "!MODE!"=="publish"` block calling `:PUBLISH_TO_SERVER`, and the entire `:PUBLISH_TO_SERVER` and `:GET_RELEASE_NOTES` function blocks. Simplified mode validation to only accept `standalone` (publish mode removed). Removed all `CHANNEL` variable references (detection logic in `:GET_VERSION` and echo lines) since they were only used by the publish flow.

**sync-version.bat**: Removed the entire `update-client.config.yaml` sync block (the `if exist` check and its PowerShell command). The DocuFiller.csproj version sync remains.

## Verification

dotnet build: 0 errors, 92 warnings (all pre-existing). dotnet test: 162 tests pass (135 + 27), 0 failures. grep confirms 0 matches for UpdateServerUrl, UpdateChannel, CheckUpdateOnStartup, COPY_EXTERNAL_FILES, update-client, publish-client across App.config and all .bat scripts. grep confirms 0 CHANNEL references remain in build-internal.bat.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --verbosity quiet` | 0 | ✅ pass | 1700ms |
| 2 | `dotnet test --no-build --verbosity minimal` | 0 | ✅ pass | 15000ms |
| 3 | `grep -r "UpdateServerUrl|UpdateChannel|CheckUpdateOnStartup|COPY_EXTERNAL_FILES|update-client|publish-client" App.config scripts/ --include="*.config" --include="*.bat"` | 1 | ✅ pass (0 matches = exit code 1 from grep) | 100ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `App.config`
- `scripts/build-internal.bat`
- `scripts/sync-version.bat`
