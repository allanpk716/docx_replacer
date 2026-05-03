---
id: T02
parent: S01
milestone: M018
key_files:
  - ViewModels/MainWindowViewModel.cs
  - Cli/Commands/UpdateCommand.cs
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
key_decisions:
  - Portable version now follows identical update code path as installed version — no branching on IsInstalled anywhere in GUI or CLI
duration: 
verification_result: passed
completed_at: 2026-05-03T09:17:29.762Z
blocker_discovered: false
---

# T02: Remove all portable version update blocking logic from GUI status bar, CLI update command, and tests

**Remove all portable version update blocking logic from GUI status bar, CLI update command, and tests**

## What Happened

Removed all portable version update blocking in three files:

1. **MainWindowViewModel.cs**: Deleted `UpdateStatus.PortableVersion` enum value, removed its switch branches in `UpdateStatusMessage` and `UpdateStatusBrush`, deleted the `!_updateService.IsInstalled` guard in `InitializeUpdateStatusAsync`, and removed the `case UpdateStatus.PortableVersion:` handler in `OnUpdateStatusClickAsync`.

2. **UpdateCommand.cs**: Removed the `if (!_updateService.IsInstalled)` guard block that returned `PORTABLE_NOT_SUPPORTED` error, allowing portable mode to follow the same download+apply update path as installed mode.

3. **UpdateCommandTests.cs**: Renamed `Update_WithYes_Portable_OutputsError` to `Update_WithYes_Portable_ProceedsNormally`, changed assertions to verify exitCode 0 (success), output contains "update" type, and output does NOT contain "PORTABLE_NOT_SUPPORTED".

All 6 UpdateCommand tests pass. Build succeeds with 0 errors (only pre-existing warnings). Full test suite: 216 pass + 6 pre-existing UpdateSettingsViewModelTests failures (unrelated to this change, caused by appsettings.json containing real server URL).

## Verification

dotnet build: 0 errors, build succeeded. grep -c "PortableVersion" ViewModels/MainWindowViewModel.cs = 0. grep -c "PORTABLE_NOT_SUPPORTED" Cli/Commands/UpdateCommand.cs = 0. dotnet test UpdateCommandTests: 6/6 passed. dotnet test (all update-related): 35/35 passed.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 3000ms |
| 2 | `grep -c PortableVersion ViewModels/MainWindowViewModel.cs` | 1 | ✅ pass (0 occurrences) | 100ms |
| 3 | `grep -c PORTABLE_NOT_SUPPORTED Cli/Commands/UpdateCommand.cs` | 1 | ✅ pass (0 occurrences) | 100ms |
| 4 | `dotnet test --filter UpdateCommandTests` | 0 | ✅ pass (6/6) | 2400ms |

## Deviations

None.

## Known Issues

Pre-existing UpdateSettingsViewModelTests failures (6 tests) unrelated to this change — they expect null/empty URLs but test appsettings.json has a real server URL.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
- `Cli/Commands/UpdateCommand.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
