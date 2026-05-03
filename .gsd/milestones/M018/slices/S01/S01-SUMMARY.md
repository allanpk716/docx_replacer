---
id: S01
parent: M018
milestone: M018
provides:
  - ["IUpdateService.IsPortable property for downstream mode detection", "GUI status bar no longer blocks portable version", "CLI update --yes works for portable version", "All portable blocking logic removed from codebase"]
requires:
  []
affects:
  []
key_files:
  - (none)
key_decisions:
  - ["Portable version follows identical update code path as installed version — no branching on IsInstalled anywhere in GUI or CLI", "UpdateSettingsViewModel gets optional readPersistentConfig delegate for test hermeticity"]
patterns_established:
  - ["Optional constructor delegate for test isolation of filesystem-dependent static methods"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-03T09:21:14.927Z
blocker_discovered: false
---

# S01: S01: 便携版更新解锁

**移除所有便携版更新阻断逻辑，新增 IUpdateService.IsPortable 属性，便携版现在走与安装版完全相同的检查→下载→应用更新代码路径**

## What Happened

This slice removed all portable version update blocking from the DocuFiller application, enabling portable builds (extracted from Portable.zip) to use the full auto-update pipeline.

**T01 — IsPortable property**: Added `bool IsPortable { get; }` to `IUpdateService` interface (line 32) with implementation in `UpdateService` reading from Velopack's `UpdateManager.IsPortable`. Updated both test stub classes (`StubUpdateService` in UpdateCommandTests.cs and CliRunnerTests.cs) to satisfy the new interface member. `IsInstalled` XML comment updated from "便携版返回 false" to "信息属性，不用于流程阻断".

**T02 — Remove blocking logic**: Deleted `UpdateStatus.PortableVersion` enum value and all associated switch branches from `MainWindowViewModel.cs` (UpdateStatusMessage, UpdateStatusBrush, OnUpdateStatusClickAsync). Removed `!_updateService.IsInstalled` guard in `InitializeUpdateStatusAsync` and `if (!_updateService.IsInstalled)` guard in `UpdateCommand.cs`. Updated portable CLI test to verify normal update flow (exitCode 0, no PORTABLE_NOT_SUPPORTED error).

**Fix — UpdateSettingsViewModelTests**: Fixed 6 pre-existing test failures caused by `ReadPersistentConfig()` reading from the real `%USERPROFILE%\.docx_replacer\update-config.json` file (containing server URL http://172.18.200.47:30001). Added optional `Func<(string?, string?)>? readPersistentConfig` parameter to the constructor, allowing tests to inject a no-op delegate `() => (null, null)` that bypasses the filesystem. This is backward-compatible — the default null value falls back to the static `ReadPersistentConfig()` method.

## Verification

All verification checks pass:
- `dotnet build`: 0 errors, 72 pre-existing warnings
- `dotnet test`: 249/249 passed (222 DocuFiller.Tests + 27 E2ERegression)
- `grep -c "PortableVersion" ViewModels/MainWindowViewModel.cs`: 0 occurrences (no residual references)
- `grep -c "PORTABLE_NOT_SUPPORTED" Cli/Commands/UpdateCommand.cs`: 0 occurrences (no residual references)
- `grep -n "IsPortable" Services/Interfaces/IUpdateService.cs`: property at line 32

## Requirements Advanced

- R001 — IsInstalled guard removed from GUI and CLI, portable version follows identical update code path
- R002 — IUpdateService.IsPortable property added and implemented in UpdateService
- R003 — UpdateStatus.PortableVersion enum deleted, all associated UI blocking removed
- R004 — UpdateCommand IsInstalled guard removed, CLI update --yes works for portable
- R008 — Decision D045 recorded overturning D029

## Requirements Validated

- R001 — Code inspection: no IsInstalled guards in InitializeUpdateStatusAsync or UpdateCommand.ExecuteAsync; all 249 tests pass
- R002 — IUpdateService.cs line 32 defines IsPortable; UpdateService.cs reads from Velopack; all stubs compile
- R003 — grep -c PortableVersion ViewModels/MainWindowViewModel.cs = 0; enum and switch branches removed
- R004 — grep -c PORTABLE_NOT_SUPPORTED Cli/Commands/UpdateCommand.cs = 0; test Update_WithYes_Portable_ProceedsNormally passes
- R008 — Decision D045 saved via gsd_decision_save

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Fixed 6 pre-existing UpdateSettingsViewModelTests failures by adding an optional `readPersistentConfig` delegate parameter to the UpdateSettingsViewModel constructor. This was not in the original slice plan but was necessary to make `dotnet test` pass cleanly — the tests were reading from the real `%USERPROFILE%\.docx_replacer\update-config.json` file which contained a production server URL.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

- `Services/Interfaces/IUpdateService.cs` — Added bool IsPortable property to interface
- `Services/UpdateService.cs` — Implemented IsPortable from Velopack UpdateManager.IsPortable
- `ViewModels/MainWindowViewModel.cs` — Removed PortableVersion enum, IsInstalled guard, and all switch branches
- `Cli/Commands/UpdateCommand.cs` — Removed IsInstalled guard and PORTABLE_NOT_SUPPORTED error
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — Updated StubUpdateService with IsPortable; rewrote portable test
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs` — Updated StubUpdateService with IsPortable
- `Tests/UpdateSettingsViewModelTests.cs` — Added readPersistentConfig delegate for test isolation
- `ViewModels/UpdateSettingsViewModel.cs` — Added optional readPersistentConfig constructor parameter
