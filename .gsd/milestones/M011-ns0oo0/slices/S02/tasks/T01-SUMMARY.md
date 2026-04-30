---
id: T01
parent: S02
milestone: M011-ns0oo0
key_files:
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
  - ViewModels/DownloadProgressViewModel.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
key_decisions:
  - Used cumulative average speed (from first progress point) rather than incremental delta speed for stability
  - Injected Func<TimeSpan> timestamp provider and Action<Action> dispatcher wrapper for unit-testability without WPF Dispatcher
duration: 
verification_result: passed
completed_at: 2026-04-30T06:19:15.772Z
blocker_discovered: false
---

# T01: Add CancellationToken support to IUpdateService.DownloadUpdatesAsync and create DownloadProgressViewModel with speed/ETA tracking

**Add CancellationToken support to IUpdateService.DownloadUpdatesAsync and create DownloadProgressViewModel with speed/ETA tracking**

## What Happened

Added CancellationToken parameter (default=default) to IUpdateService.DownloadUpdatesAsync and UpdateService, forwarding to Velopack's UpdateManager.DownloadUpdatesAsync which already accepts CancellationToken. Updated both test mock implementations in CliRunnerTests and UpdateCommandTests to match the new signature.

Created DownloadProgressViewModel with full progress tracking:
- Constructor accepts (totalBytes, version, timestampProvider, dispatcherInvoke) for unit-testability
- UpdateProgress(int percent) called from Velopack callback, thread-safe via dispatcher wrapper
- Speed calculation: tracks (percent, timestamp) history pairs, computes cumulative average speed from first data point
- ETA calculation: remainingBytes / currentSpeed with human-readable formatting (秒/分钟/小时)
- CancellationTokenSource exposed for cancel button binding
- CancelCommand calls CTS.Cancel()
- MarkCompleted/MarkFailed/MarkCancelled for terminal state transitions
- IsDownloading, IsCompleted, ErrorMessage bindable properties
- CloseCallback action follows same pattern as UpdateSettingsViewModel
- IDisposable for CancellationTokenSource cleanup

No existing callers needed code changes since the new parameter has a default value.

## Verification

dotnet build --verbosity minimal returns 0 errors, 0 new warnings. Build succeeded with all projects compiling correctly.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --verbosity minimal` | 0 | ✅ pass | 2450ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `ViewModels/DownloadProgressViewModel.cs`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
