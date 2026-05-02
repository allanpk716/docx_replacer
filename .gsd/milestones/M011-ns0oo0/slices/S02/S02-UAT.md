# S02: 下载更新进度弹窗 — UAT

**Milestone:** M011-ns0oo0
**Written:** 2026-04-30T06:33:16.939Z

# S02: 下载更新进度弹窗 — UAT

**Milestone:** M011-ns0oo0
**Written:** 2026-04-30

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: Download progress UI requires a real Velopack update server to trigger the full flow. Unit tests (38 tests) cover all ViewModel logic, build verification confirms XAML compiles, and code review confirms the wiring pattern matches the established UpdateSettingsWindow pattern. Live runtime testing requires network access to the configured UpdateUrl.

## Preconditions

- Application builds successfully (0 errors)
- All 38 DownloadProgressViewModelTests pass
- An UpdateUrl is configured in appsettings.json (required for live testing)
- A newer version exists on the update server (required for live testing)

## Smoke Test

1. Launch DocuFiller GUI
2. Navigate to update settings (Settings → Update tab)
3. Click "Check for Updates"
4. If an update is available and user clicks "Yes" to download:
5. **Expected:** A modal window titled "下载更新" appears with a progress bar at 0%, speed "计算中...", and cancel button enabled

## Test Cases

### 1. Progress window appears on download start

1. Check for updates, confirm download when prompted
2. **Expected:** DownloadProgressWindow opens as modal (main window blocked), shows version info in StatusText, ProgressBar at 0%

### 2. Progress updates during download

1. Start a download to a reachable update server
2. Wait for Velopack progress callbacks to fire
3. **Expected:** ProgressBar advances, percentage TextBlock updates, speed TextBlock shows value like "2.5 MB/s", ETA TextBlock shows value like "预计剩余 2 分钟"

### 3. Cancel button interrupts download

1. Start a download
2. Click the cancel button before download completes
3. **Expected:** Download is interrupted, IsDownloading becomes false, cancel button becomes disabled, application continues running normally with no crash

### 4. X button routes through cancel during download

1. Start a download
2. Attempt to close the window via the X button in title bar
3. **Expected:** Window does not close directly; instead CancelCommand is triggered, cancelling the download (same behavior as cancel button)

### 5. Successful download auto-closes and applies update

1. Start a download that completes successfully
2. **Expected:** Progress reaches 100%, window shows completed state, auto-closes after ~800ms, ApplyUpdatesAndRestart() is called

### 6. Download failure shows error

1. Start a download that fails (e.g., network error)
2. **Expected:** Error message displayed in the window, IsDownloading becomes false, application continues running

## Edge Cases

### Zero-byte update package

1. If VelopackAsset.Size is 0, speed and ETA calculations should handle gracefully
2. **Expected:** No divide-by-zero crash; speed shows "计算中...", ETA shows empty

### Very fast download (< 1 second)

1. If download completes before second progress callback
2. **Expected:** Speed may show "计算中..." (only 1 data point), window closes normally after completion

### Progress callback with out-of-order percentages

1. If Velopack sends progress values that don't strictly increase
2. **Expected:** Percent clamped to 0-100 range, no crash

## Failure Signals

- Build errors: XAML compilation errors in DownloadProgressWindow.xaml
- Runtime crash: Unhandled OperationCanceledException or NullReferenceException
- UI stuck: Modal window never closes after download completes
- Missing UI elements: ProgressBar, speed/ETA TextBlocks, or cancel button not visible

## Not Proven By This UAT

- Actual Velopack download progress callback behavior (requires live update server)
- Real network speed accuracy of speed/ETA calculations
- ApplyUpdatesAndRestart behavior after successful download (pre-existing, not modified in this slice)
- Multi-monitor or high-DPI display rendering of the progress window

## Notes for Tester

- The progress window follows the same pattern as UpdateSettingsWindow (DI-resolved window, manual ViewModel injection via SetViewModel)
- Speed uses binary units (1024-based), so displayed values may differ slightly from OS-level network monitors
- ETA shows "即将完成" when remaining time < 1 second
- CancellationTokenSource is disposed via IDisposable pattern in the ViewModel
