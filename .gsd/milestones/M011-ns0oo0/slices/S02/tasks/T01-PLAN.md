---
estimated_steps: 15
estimated_files: 3
skills_used: []
---

# T01: Add CancellationToken to DownloadUpdatesAsync and create DownloadProgressViewModel

Two changes in one task because they're tightly coupled:

1. Add CancellationToken parameter to IUpdateService.DownloadUpdatesAsync and UpdateService implementation, forwarding to Velopack's UpdateManager.DownloadUpdatesAsync which already accepts CancellationToken.

2. Create DownloadProgressViewModel with:
   - Constructor accepting (long totalBytes, string version) — totalBytes from VelopackAsset.Size
   - UpdateProgress(int percent) method called from Velopack callback — must be thread-safe (use Application.Current.Dispatcher.Invoke or store values and raise PropertyChanged via dispatcher)
   - CancellationTokenSource exposed for cancel button binding
   - Calculated properties: ProgressPercent (int 0-100), DownloadSpeed (string like "2.5 MB/s"), RemainingTime (string like "约 2 分钟"), StatusText (combines info)
   - Speed calculation: track (percent, timestamp) pairs; when progress increases, compute bytesDownloaded = totalBytes * newPercent / 100, speed = bytesDownloaded / elapsedSeconds
   - ETA calculation: remainingBytes / speed
   - IsDownloading bool (false when complete/cancelled/error)
   - CancelCommand that calls CancellationTokenSource.Cancel()
   - Completion handling: IsCompleted, ErrorMessage properties
   - CloseCallback action (same pattern as UpdateSettingsViewModel)

Important: ViewModel must be unit-testable without WPF Dispatcher. Use a Func<TimeSpan> timestamp provider (default: () => DateTime.UtcNow.TimeOfDay) and an Action<Action> dispatcher wrapper (default: Application.Current.Dispatcher.Invoke) so tests can inject substitutes.

DO NOT create the Window XAML in this task — that's T02.

## Inputs

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`

## Expected Output

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `ViewModels/DownloadProgressViewModel.cs`

## Verification

dotnet build --verbosity minimal returns 0 errors
