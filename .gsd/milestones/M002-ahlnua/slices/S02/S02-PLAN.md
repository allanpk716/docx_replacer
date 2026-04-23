# S02: 文件夹选择对话框替换和验证

**Goal:** 将 MainWindowViewModel 中的 BrowseOutput、BrowseTemplateFolder、BrowseCleanupOutput 三个方法从 OpenFileDialog 伪装文件夹选择器替换为 Microsoft.Win32.OpenFolderDialog，与 ConverterWindowViewModel 中已有的正确实现保持一致。
**Demo:** BrowseOutput、BrowseTemplateFolder、BrowseCleanupOutput 三个按钮打开真正的系统文件夹选择对话框，dotnet test 全部通过

## Must-Haves

- BrowseOutput 使用 OpenFolderDialog 并设置 FolderName 属性到 OutputDirectory
- BrowseTemplateFolder 使用 OpenFolderDialog 选择文件夹后调用 HandleFolderDropAsync
- BrowseCleanupOutput 使用 OpenFolderDialog 并设置 FolderName 属性到 CleanupOutputDirectory
- dotnet test 全部通过，零回归
- grep 扫描确认 MainWindowViewModel.cs 中不再有 OpenFileDialog 用于文件夹选择

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: S01 清理后的 MainWindowViewModel.cs（干净的 ILogger 基线）
- New wiring: 无新依赖注入，OpenFolderDialog 是 Microsoft.Win32 内置类型
- What remains: nothing — this is the final slice of the milestone

## Verification

- Runtime signals: _logger.LogInformation calls added for folder selection results (consistent with S01 ILogger baseline)
- Inspection surfaces: structured log output shows selected folder path
- Failure visibility: null/empty path handled with early return, logged at debug level

## Tasks

- [x] **T01: Replace three OpenFileDialog folder hacks with OpenFolderDialog** `est:30m`
  Replace BrowseOutput, BrowseTemplateFolder, and BrowseCleanupOutput methods in MainWindowViewModel.cs with proper OpenFolderDialog usage, matching the pattern already used in ConverterWindowViewModel.BrowseOutput(). Then run full test suite to confirm zero regressions and grep-scan to verify no OpenFileDialog remains for folder selection.
  - Files: `ViewModels/MainWindowViewModel.cs`
  - Verify: dotnet test --no-restore 2>&1 | tail -5
# AND
grep -c "OpenFileDialog" ViewModels/MainWindowViewModel.cs
# AND
grep -c "OpenFolderDialog" ViewModels/MainWindowViewModel.cs

## Files Likely Touched

- ViewModels/MainWindowViewModel.cs
