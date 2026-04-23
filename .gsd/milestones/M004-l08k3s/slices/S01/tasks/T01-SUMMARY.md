---
id: T01
parent: S01
milestone: M004-l08k3s
key_files:
  - DocuFiller.csproj
  - App.xaml.cs
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
key_decisions:
  - Extended cleanup beyond plan scope to remove all update references from MainWindowViewModel.cs, MainWindow.xaml, and MainWindow.xaml.cs — necessary for successful compilation since these files had deep integration with the update service
duration: 
verification_result: passed
completed_at: 2026-04-23T14:14:11.376Z
blocker_discovered: false
---

# T01: Remove all online update infrastructure: build gates, External directory, update service/model/viewmodel/view files, and DI registrations

**Remove all online update infrastructure: build gates, External directory, update service/model/viewmodel/view files, and DI registrations**

## What Happened

Removed the complete online update system from DocuFiller:

**DocuFiller.csproj changes:**
- Removed `<Target Name="ValidateUpdateClientFiles">` (PreBuild gate that blocked building without External/update-client.exe)
- Removed `<Target Name="ValidateReleaseFiles">` (PostPublish validation)
- Removed the `<!-- Update Client External Files -->` ItemGroup referencing update-client.exe and update-client.config.yaml

**External/ directory:** Deleted entirely (contained .gitignore, .gitkeep, publish-client.usage.txt, update-client.config.yaml)

**Deleted 19 update-related files:**
- Services: IUpdateService.cs, UpdateClientService.cs, UpdateService.cs, UpdateDownloader.cs
- Models: DaemonProgressInfo.cs, DownloadProgress.cs, DownloadStatus.cs, UpdateClientResponseModels.cs, UpdateConfig.cs, VersionInfo.cs
- ViewModels: UpdateBannerViewModel.cs, UpdateViewModel.cs
- Views: UpdateBannerView.xaml/.cs, UpdateWindow.xaml/.cs, UpdateBannerView.xaml/.cs (root Views/)

**App.xaml.cs changes:**
- Removed `using DocuFiller.Services.Update;`
- Removed `services.AddSingleton<IUpdateService, UpdateClientService>();`
- Removed `services.AddTransient<ViewModels.Update.UpdateViewModel>();`
- Removed `services.AddTransient<ViewModels.Update.UpdateBannerViewModel>();`
- Removed `services.AddTransient<Views.Update.UpdateWindow>();`
- Removed `services.AddTransient<Views.Update.UpdateBannerView>();`

**Additional cleanup beyond the plan (necessary for build success):**
- MainWindowViewModel.cs: Removed 4 update-related using statements, IUpdateService field, update fields (_isUpdateAvailable, _latestVersionInfo, _updateBannerViewModel), constructor parameters (IUpdateService, UpdateBannerViewModel), OnInitializedAsync(), CheckForUpdatesAsync(), ShowUpdateBannerAsync(), CheckForUpdateAsync(), OnUpdateAvailable(), ShowUpdateWindow(), SubscribeToUpdateEvents(), IsUpdateAvailable/LatestVersionInfo/UpdateBanner properties, CheckForUpdateCommand, and Task.Run call for auto-update check
- MainWindow.xaml: Removed xmlns:updateViews namespace and the entire "检查更新" Border UI block with CheckForUpdateCommand binding
- MainWindow.xaml.cs: Removed CheckForUpdateHyperlink_Click event handler
- Fixed file corruption in MainWindowViewModel.cs (duplicate content appended after closing brace with garbled bytes)

## Verification

All verification checks from the task plan pass:
1. `grep -c "ValidateUpdateClientFiles|ValidateReleaseFiles|update-client" DocuFiller.csproj` returns 0 ✅
2. `test ! -d External` passes — External/ directory deleted ✅
3. `grep -c "IUpdateService|UpdateViewModel|UpdateBannerView|UpdateWindow" App.xaml.cs` returns 0 ✅
4. No update-related .cs/.xaml files remain in the project ✅
5. `dotnet build` succeeds with 0 errors, 0 warnings ✅

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -c "ValidateUpdateClientFiles|ValidateReleaseFiles|update-client" DocuFiller.csproj` | 0 | ✅ pass (0 matches) | 500ms |
| 2 | `test ! -d External` | 0 | ✅ pass (directory deleted) | 200ms |
| 3 | `grep -c "IUpdateService|UpdateViewModel|UpdateBannerView|UpdateWindow" App.xaml.cs` | 0 | ✅ pass (0 matches) | 300ms |
| 4 | `dotnet build` | 0 | ✅ pass (0 errors, 0 warnings) | 1580ms |

## Deviations

Extended scope to clean update references from MainWindowViewModel.cs, MainWindow.xaml, and MainWindow.xaml.cs — the task plan only specified csproj, External/, update service/model/viewmodel/view files, and App.xaml.cs, but MainWindowViewModel had deep update integration (field, constructor params, commands, properties, methods) that caused build failures. Also fixed file corruption in MainWindowViewModel.cs where duplicate content with garbled bytes was appended after the closing brace.

## Known Issues

None.

## Files Created/Modified

- `DocuFiller.csproj`
- `App.xaml.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
