---
id: T02
parent: S02
milestone: M011-ns0oo0
key_files:
  - DocuFiller/Views/DownloadProgressWindow.xaml
  - DocuFiller/Views/DownloadProgressWindow.xaml.cs
  - ViewModels/MainWindowViewModel.cs
  - App.xaml.cs
  - Tests/DocuFiller.Tests.csproj
key_decisions:
  - ViewModel manually created in MainWindowViewModel rather than DI-resolved because it requires runtime parameters (totalBytes, version) not available at DI registration time
  - Download runs in Task.Run background thread while progress window shows modally via ShowDialog — allows UI thread to remain responsive
  - Window closing prevention via OnClosing override routes through CancelCommand instead of directly closing
duration: 
verification_result: passed
completed_at: 2026-04-30T06:24:53.009Z
blocker_discovered: false
---

# T02: Create DownloadProgressWindow XAML modal dialog and wire into MainWindowViewModel download flow with progress bar, speed, ETA, and cancel support

**Create DownloadProgressWindow XAML modal dialog and wire into MainWindowViewModel download flow with progress bar, speed, ETA, and cancel support**

## What Happened

Created the modal download progress window (DownloadProgressWindow.xaml + .xaml.cs) following the same pattern as UpdateSettingsWindow — window resolved from DI, ViewModel manually created by MainWindowViewModel with required parameters (totalBytes, version), then injected via SetViewModel() with CloseCallback.

**DownloadProgressWindow.xaml**: 450px wide, SizeToContent=Height, CenterOwner, NoResize modal window containing:
- StatusText TextBlock for version/progress summary
- ProgressBar (0-100) using ModernProgressBarStyle from App.xaml
- Progress info row with percentage, download speed (MB/s), and ETA display
- Cancel button bound to CancelCommand, disabled when !IsDownloading

**DownloadProgressWindow.xaml.cs**: Code-behind with:
- SetViewModel() method for ViewModel injection (follows UpdateSettingsWindow pattern)
- OnClosing override prevents X button close during download — routes through CancelCommand
- Owner set to Application.Current.MainWindow

**MainWindowViewModel.CheckUpdateAsync modification**: After user confirms download (MessageBoxResult.Yes):
1. Creates DownloadProgressViewModel with VelopackAsset.Size and version
2. Resolves DownloadProgressWindow from DI and sets ViewModel
3. Launches background Task.Run for download with progress callback
4. Shows progress window modally (ShowDialog) — blocks main window
5. On completion: marks ViewModel completed, delays 800ms for visual feedback, closes window, applies update
6. On cancel: marks ViewModel cancelled (OperationCanceledException caught)
7. On error: marks ViewModel failed with error message

**DI registration**: Added DownloadProgressWindow as Transient in App.xaml.cs

**Test project**: Added Compile include for DownloadProgressViewModel.cs in DocuFiller.Tests.csproj

## Verification

dotnet build --verbosity minimal returns 0 errors. All 203 existing tests pass (176 DocuFiller.Tests + 27 E2ERegression). Build succeeded for all 3 projects (DocuFiller, DocuFiller.Tests, E2ERegression).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --verbosity minimal` | 0 | ✅ pass | 2240ms |
| 2 | `dotnet test --no-build --verbosity minimal` | 0 | ✅ pass (203 tests) | 15000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `DocuFiller/Views/DownloadProgressWindow.xaml`
- `DocuFiller/Views/DownloadProgressWindow.xaml.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`
- `Tests/DocuFiller.Tests.csproj`
