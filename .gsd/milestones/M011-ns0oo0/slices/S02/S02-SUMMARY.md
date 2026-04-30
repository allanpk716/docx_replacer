---
id: S02
parent: M011-ns0oo0
milestone: M011-ns0oo0
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["Services/Interfaces/IUpdateService.cs", "Services/UpdateService.cs", "ViewModels/DownloadProgressViewModel.cs", "DocuFiller/Views/DownloadProgressWindow.xaml", "DocuFiller/Views/DownloadProgressWindow.xaml.cs", "ViewModels/MainWindowViewModel.cs", "App.xaml.cs", "Tests/DocuFiller.Tests.csproj", "Tests/DownloadProgressViewModelTests.cs"]
key_decisions:
  - ["Cumulative average speed calculation for stability", "Injected timestamp provider and dispatcher wrapper for unit-testability", "Manual ViewModel creation in caller for runtime parameters (totalBytes, version)", "Task.Run + ShowDialog pattern for background download with modal UI", "OnClosing override routes through CancelCommand to prevent direct X-button close"]
patterns_established:
  - ["Modal dialog with runtime params: caller creates ViewModel manually, resolves Window from DI, injects via SetViewModel()", "Progress tracking: cumulative average from first data point, binary units (1024-based), ETA with human-readable formatting", "Background work + modal UI: Task.Run for download, ShowDialog blocks main window, dispatcher for thread-safe UI updates"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M011-ns0oo0/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M011-ns0oo0/slices/S02/tasks/T02-SUMMARY.md", ".gsd/milestones/M011-ns0oo0/slices/S02/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-30T06:33:16.938Z
blocker_discovered: false
---

# S02: 下载更新进度弹窗

**Modal download progress window with real-time progress bar, speed, ETA, and cancel support for Velopack updates**

## What Happened

This slice delivers a complete modal progress window for the Velopack update download flow. Three tasks were executed:

**T01** added CancellationToken support to IUpdateService.DownloadUpdatesAsync (default parameter, no caller changes needed) and created DownloadProgressViewModel with thread-safe progress tracking, cumulative average speed calculation, ETA estimation, and cancel/complete/error state transitions. The ViewModel is fully unit-testable via injected timestamp provider and dispatcher wrapper.

**T02** created DownloadProgressWindow (XAML modal dialog at 450px, CenterOwner, NoResize) with ProgressBar, speed/ETA TextBlocks, and a Cancel button. MainWindowViewModel.CheckUpdateAsync was modified to: create the ViewModel with VelopackAsset.Size and version, launch the download in Task.Run, show the progress window modally, and handle completion/cancel/error states. The window prevents direct X-button closing during download (routes through CancelCommand). DI registration added as Transient.

**T03** wrote 38 xunit tests covering initial state, progress updates, speed/ETA calculation, cancel/complete/error transitions, edge cases (zero totalBytes, percent clamping, fast downloads), FormatSpeed/FormatEta static methods, IUpdateService signature verification, dispose safety, and PropertyChanged events. All tests pass without WPF Dispatcher dependency.

Key architectural decisions: cumulative average speed for stability, manual ViewModel creation for runtime parameters, Task.Run + ShowDialog pattern for background download with modal UI.

## Verification

Build verification: dotnet build --verbosity minimal returns 0 errors, 0 warnings.
Unit tests: dotnet test --filter "FullyQualifiedName~DownloadProgressViewModelTests" passes 38/38 tests.
Full test suite: dotnet test passes all 203 tests (176 DocuFiller.Tests + 27 E2ERegression).
All three tasks individually verified with passing build/test gates before completion.

## Requirements Advanced

None.

## Requirements Validated

- R048 — DownloadProgressWindow with ProgressBar (0-100%), speed (MB/s), ETA TextBlocks created. DownloadProgressViewModel with cumulative average speed/ETA calculation. MainWindowViewModel wires download flow with Task.Run + ShowDialog. 38 unit tests pass.
- R049 — Cancel button triggers CancellationTokenSource.Cancel(). OperationCanceledException caught, app continues normally. OnClosing prevents X button close during download. Unit tests verify cancel state transitions.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

- `Services/Interfaces/IUpdateService.cs` — Added CancellationToken parameter with default value to DownloadUpdatesAsync
- `Services/UpdateService.cs` — Forwarded CancellationToken to Velopack UpdateManager.DownloadUpdatesAsync
- `ViewModels/DownloadProgressViewModel.cs` — New ViewModel with progress/speed/ETA tracking, cancel/complete/error states, testable via injected dependencies
- `DocuFiller/Views/DownloadProgressWindow.xaml` — New modal progress window with ProgressBar, speed/ETA TextBlocks, cancel button
- `DocuFiller/Views/DownloadProgressWindow.xaml.cs` — Code-behind with SetViewModel injection, OnClosing override for cancel routing
- `ViewModels/MainWindowViewModel.cs` — Modified CheckUpdateAsync to show DownloadProgressWindow with Task.Run download
- `App.xaml.cs` — Registered DownloadProgressWindow as Transient in DI
- `Tests/DocuFiller.Tests.csproj` — Added Compile include for DownloadProgressViewModel.cs
- `Tests/DownloadProgressViewModelTests.cs` — 38 unit tests covering all ViewModel logic and edge cases
