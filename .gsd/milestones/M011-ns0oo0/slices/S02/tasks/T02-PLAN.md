---
estimated_steps: 26
estimated_files: 5
skills_used: []
---

# T02: Create DownloadProgressWindow XAML and wire into MainWindowViewModel + DI

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

## Inputs

- `ViewModels/DownloadProgressViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`

## Expected Output

- `DocuFiller/Views/DownloadProgressWindow.xaml`
- `DocuFiller/Views/DownloadProgressWindow.xaml.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`
- `Tests/DocuFiller.Tests.csproj`

## Verification

dotnet build --verbosity minimal returns 0 errors
