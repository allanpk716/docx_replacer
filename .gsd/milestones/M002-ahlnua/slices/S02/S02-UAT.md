# S02: 文件夹选择对话框替换和验证 — UAT

**Milestone:** M002-ahlnua
**Written:** 2026-04-23T08:26:58.841Z

# S02: 文件夹选择对话框替换和验证 — UAT

**Milestone:** M002-ahlnua
**Written:** 2026-04-23

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: Changes are purely UI dialog replacement (OpenFileDialog → OpenFolderDialog) with no new runtime services. Verification via grep scanning and test suite is sufficient to confirm correctness. Live runtime testing requires manual desktop interaction which is beyond automated UAT scope.

## Preconditions

- .NET 8 SDK installed
- Project builds successfully (dotnet build)

## Smoke Test

Run `dotnet test --no-restore` — all 71 tests must pass with zero failures.

## Test Cases

### 1. No OpenFileDialog used for folder selection

1. Run `grep -c "OpenFileDialog" ViewModels/MainWindowViewModel.cs`
2. **Expected:** Output is `2` (only BrowseTemplate and BrowseData, which legitimately select files)

### 2. OpenFolderDialog used for all three folder-selection methods

1. Run `grep -c "OpenFolderDialog" ViewModels/MainWindowViewModel.cs`
2. **Expected:** Output is `3` (BrowseOutput, BrowseTemplateFolder, BrowseCleanupOutput)

### 3. ILogger usage in folder-selection methods

1. Run `grep -A2 "OpenFolderDialog" ViewModels/MainWindowViewModel.cs | grep -c "LogInformation\|LogDebug"`
2. **Expected:** Output is `3` (each method logs the result)

### 4. Full test suite passes

1. Run `dotnet test --no-restore`
2. **Expected:** All 71 tests pass, 0 failures, 0 skipped

## Edge Cases

### Null/empty folder path handling

1. Inspect each of the three methods in ViewModels/MainWindowViewModel.cs
2. **Expected:** Each method has an early return after null/empty check on dialog.FolderName, with a LogDebug call

## Failure Signals

- `dotnet test` shows any test failures
- grep shows OpenFileDialog count > 2 in MainWindowViewModel.cs
- Build errors related to OpenFolderDialog API usage

## Not Proven By This UAT

- Actual folder dialog appearance on Windows desktop (requires manual UI testing)
- Dialog initial folder path correctness (requires runtime verification with actual folder structure)
- Cross-platform behavior (WPF/OpenFolderDialog is Windows-only by design)

## Notes for Tester

- Pre-existing build warning for missing External/update-client.exe is unrelated to this change
- ConverterWindowViewModel already used OpenFolderDialog correctly — S02 brings MainWindowViewModel to the same pattern
