---
id: T01
parent: S02
milestone: M002-ahlnua
key_files:
  - ViewModels/MainWindowViewModel.cs
key_decisions:
  - Used OpenFolderDialog.FolderName property for folder path retrieval, matching ConverterWindowViewModel pattern
duration: 
verification_result: passed
completed_at: 2026-04-23T08:23:49.298Z
blocker_discovered: false
---

# T01: Replace three OpenFileDialog folder-selection hacks with OpenFolderDialog in MainWindowViewModel

**Replace three OpenFileDialog folder-selection hacks with OpenFolderDialog in MainWindowViewModel**

## What Happened

Replaced three methods in MainWindowViewModel.cs that used OpenFileDialog as a fake folder picker with the proper Microsoft.Win32.OpenFolderDialog, matching the pattern already established in ConverterWindowViewModel.BrowseOutput():

1. **BrowseOutput()** — Was using OpenFileDialog with `FileName = "选择文件夹"` hack and `Path.GetDirectoryName()` extraction. Replaced with OpenFolderDialog using `dialog.FolderName`.

2. **BrowseTemplateFolder()** — Was using OpenFileDialog requiring user to pick a file inside the target folder, then extracting the directory from `dialog.FileName`. Replaced with OpenFolderDialog that directly selects a folder.

3. **BrowseCleanupOutput()** — Same hack as BrowseOutput. Replaced identically.

All three methods now include:
- `_logger.LogInformation` for successful folder selection (consistent with S01 ILogger baseline)
- Null/empty path early-return with `_logger.LogDebug` for observability
- Clean `dialog.FolderName` property access instead of directory extraction tricks

The existing `BrowseTemplate()` and `BrowseData()` methods that legitimately select files remain unchanged with OpenFileDialog. Build succeeded (no CS errors; pre-existing update-client.exe pre-build check failure is unrelated). All 71 tests pass.

## Verification

- `dotnet test --no-restore`: 71 tests passed, 0 failed (841ms)
- `grep -c "OpenFileDialog" ViewModels/MainWindowViewModel.cs`: 2 remaining (legitimate file-selection uses in BrowseTemplate and BrowseData)
- `grep -c "OpenFolderDialog" ViewModels/MainWindowViewModel.cs`: 3 new uses (all three folder-selection methods)
- No C# compilation errors from the changes

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --no-restore` | 0 | ✅ pass | 5000ms |
| 2 | `grep -c "OpenFileDialog" ViewModels/MainWindowViewModel.cs` | 0 | ✅ pass (2 legitimate file uses) | 50ms |
| 3 | `grep -c "OpenFolderDialog" ViewModels/MainWindowViewModel.cs` | 0 | ✅ pass (3 folder dialog uses) | 50ms |

## Deviations

None.

## Known Issues

Pre-existing build error for missing External/update-client.exe is unrelated to this change.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
