# S02: 下载更新进度弹窗

**Goal:** 用户确认下载更新后弹出独立模态进度窗口，显示进度条（0-100%）、下载速度（MB/s）、预估剩余时间，点击取消可中断下载（通过 CancellationToken），应用继续正常运行。
**Demo:** 确认下载更新后弹出独立模态进度窗口，显示进度条（0-100%）、下载速度（MB/s）、预估剩余时间，点击取消可中断下载

## Must-Haves

- IUpdateService.DownloadUpdatesAsync 接受 CancellationToken 参数，传递给 Velopack
- DownloadProgressViewModel 实现线程安全的进度/速度/ETA 计算（基于 Velopack Action<int> 回调 + VelopackAsset.Size）
- DownloadProgressWindow 为模态窗口，包含 ProgressBar、速度/ETA TextBlock、取消按钮
- MainWindowViewModel.CheckUpdateAsync 弹出 DownloadProgressWindow 替代原来的静默下载
- 取消按钮通过 CancellationTokenSource.Cancel() 中断下载，异常不崩溃应用
- 单元测试覆盖 ViewModel 逻辑（进度/速度/ETA 计算、取消）

## Proof Level

- This slice proves: integration — 需要证明 Velopack 回调正确驱动 WPF UI、CancellationToken 正确传递

## Integration Closure

Upstream: IUpdateService.DownloadUpdatesAsync (已存在，需添加 CancellationToken)
New wiring: MainWindowViewModel.CheckUpdateAsync → DownloadProgressWindow(ShowDialog) → DownloadProgressViewModel → IUpdateService.DownloadUpdatesAsync(progressCallback, cancelToken)
DI: DownloadProgressViewModel 注册为 Transient
Remaining after this slice: nothing — 这是里程碑的最后一个 slice

## Verification

- Structured logging: MainWindowViewModel 记录下载开始/取消/完成/失败事件及耗时
- Velopack progress callback 通过 ILogger.LogDebug 输出百分比
- Failure: 下载失败/取消时 MessageBox 显示错误信息，日志记录异常详情

## Tasks

- [x] **T01: Add CancellationToken to DownloadUpdatesAsync and create DownloadProgressViewModel** `est:1h`
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
  - Files: `Services/Interfaces/IUpdateService.cs`, `Services/UpdateService.cs`, `ViewModels/DownloadProgressViewModel.cs`
  - Verify: dotnet build --verbosity minimal returns 0 errors

- [x] **T02: Create DownloadProgressWindow XAML and wire into MainWindowViewModel + DI** `est:1h`
  Create the modal progress window and integrate it into the existing update flow.

1. Create `DocuFiller/Views/DownloadProgressWindow.xaml` — modal window, ~450px wide, SizeToContent=Height, WindowStartupLocation=CenterOwner, ResizeMode=NoResize:
   - Title: "下载更新"
   - ProgressBar bound to ProgressPercent (0-100), height ~25px, using ModernProgressBarStyle from App.xaml
   - TextBlock showing progress percentage ("50%")
   - TextBlock showing download speed ("2.5 MB/s")
   - TextBlock showing remaining time ("预计剩余 2 分钟")
   - Cancel button bound to CancelCommand, disabled when !IsDownloading
   - Use App.xaml global styles (PrimaryButtonStyle for cancel, etc.)

2. Create `DocuFiller/Views/DownloadProgressWindow.xaml.cs` code-behind:
   - Resolve DownloadProgressViewModel from DI (same pattern as UpdateSettingsWindow)
   - Set Owner to Application.Current.MainWindow
   - Set up CloseCallback to set DialogResult and close
   - Handle window closing via CancelCommand (prevent direct X button closing during download)

3. Register DownloadProgressViewModel in App.xaml.cs ConfigureServices as Transient

4. Modify MainWindowViewModel.CheckUpdateAsync:
   - After user confirms download (MessageBoxResult.Yes), create CancellationTokenSource and DownloadProgressWindow
   - Show DownloadProgressWindow as modal (ShowDialog) — this blocks the main window
   - Pass VelopackAsset.Size (from updateInfo.TargetFullRelease.Size) to ViewModel
   - In the progress callback, call ViewModel.UpdateProgress(percent)
   - On completion: ViewModel.IsCompleted = true, auto-close window after brief delay
   - On cancel: catch OperationCanceledException, show info message, return normally
   - On error: catch other exceptions, show error in window, log
   - After successful download: call ApplyUpdatesAndRestart() (existing behavior)

5. Add DownloadProgressViewModel.cs to test project's Compile includes in DocuFiller.Tests.csproj

Key constraint: The progress callback from Velopack runs on a background thread. The ViewModel.UpdateProgress must dispatch PropertyChanged to the UI thread.
  - Files: `DocuFiller/Views/DownloadProgressWindow.xaml`, `DocuFiller/Views/DownloadProgressWindow.xaml.cs`, `ViewModels/MainWindowViewModel.cs`, `App.xaml.cs`, `Tests/DocuFiller.Tests.csproj`
  - Verify: dotnet build --verbosity minimal returns 0 errors

- [x] **T03: Write unit tests for DownloadProgressViewModel and updated DownloadUpdatesAsync** `est:45m`
  Write xunit tests covering the core logic:

1. DownloadProgressViewModel tests:
   - Initial state: ProgressPercent=0, IsDownloading=true, IsCompleted=false, speed/ETA empty
   - UpdateProgress: single update sets percent, speed/ETA remain empty (need 2+ points for speed)
   - Speed calculation: two progress updates with known time delta → correct MB/s
   - ETA calculation: remaining bytes / speed → correct time string
   - Cancel: CancelCommand triggers cancellation, IsDownloading=false
   - Complete: reaches 100%, IsCompleted=true
   - Error handling: set ErrorMessage, IsCompleted=true
   - Edge cases: 0 totalBytes (avoid divide-by-zero), very fast download (speed clamped)

2. UpdateService.DownloadUpdatesAsync CancellationToken test:
   - Verify the method signature accepts CancellationToken (compile-time check is sufficient)
   - Verify CancellationToken is forwarded: mock/verify pattern or just ensure it compiles and passes default(CancellationToken)

All tests must run in CI without WPF — ensure ViewModel tests don't require Dispatcher (the injected dispatcher wrapper handles this).

Add DownloadProgressViewModel.cs to test csproj Compile includes if not already done in T02.
  - Files: `Tests/DownloadProgressViewModelTests.cs`
  - Verify: dotnet test --filter "FullyQualifiedName~DownloadProgressViewModelTests" --verbosity minimal

## Files Likely Touched

- Services/Interfaces/IUpdateService.cs
- Services/UpdateService.cs
- ViewModels/DownloadProgressViewModel.cs
- DocuFiller/Views/DownloadProgressWindow.xaml
- DocuFiller/Views/DownloadProgressWindow.xaml.cs
- ViewModels/MainWindowViewModel.cs
- App.xaml.cs
- Tests/DocuFiller.Tests.csproj
- Tests/DownloadProgressViewModelTests.cs
