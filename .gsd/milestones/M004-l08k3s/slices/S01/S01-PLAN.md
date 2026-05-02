# S01: 移除更新功能和 JSON 编辑器遗留

**Goal:** 移除所有在线更新相关代码（服务、模型、视图、ViewModel、DI 注册、csproj 构建门禁、External 目录）和 JSON 编辑器遗留文件（7 个文件），使 dotnet build 在无 External/ 目录文件的情况下编译成功。
**Demo:** dotnet build 通过（无 External 文件），grep 确认无更新/JSON编辑器相关代码文件

## Must-Haves

- Not provided.

## Proof Level

- This slice proves: Not provided.

## Integration Closure

Not provided.

## Verification

- Not provided.

## Tasks

- [x] **T01: Remove csproj build gates and External directory, delete all update service files, clean App.xaml.cs DI** `est:45m`
  Remove the PreBuild validation gate that blocks building without External/update-client.exe, delete the External/ directory, delete all update-related service/model/viewmodel/view files, and clean update DI registrations from App.xaml.cs.

## Steps

1. In `DocuFiller.csproj`, remove the entire `<Target Name="ValidateUpdateClientFiles">` block (PreBuild validation)
2. In `DocuFiller.csproj`, remove the entire `<Target Name="ValidateReleaseFiles">` block (PostPublish validation)
3. In `DocuFiller.csproj`, remove the `<!-- Update Client External Files -->` ItemGroup containing update-client.exe and update-client.config.yaml references
4. Delete the `External/` directory and all its contents
5. Delete all update service files:
   - `Services/Update/IUpdateService.cs` (includes UpdateAvailableEventArgs class)
   - `Services/Update/UpdateClientService.cs`
   - `Services/Update/UpdateService.cs`
   - `DocuFiller/Services/Update/UpdateDownloader.cs`
6. Delete all update model files:
   - `Models/Update/DaemonProgressInfo.cs`
   - `Models/Update/DownloadProgress.cs`
   - `Models/Update/DownloadStatus.cs`
   - `Models/Update/UpdateClientResponseModels.cs`
   - `Models/Update/UpdateConfig.cs`
   - `Models/Update/VersionInfo.cs`
7. Delete all update viewmodel files:
   - `ViewModels/Update/UpdateBannerViewModel.cs`
   - `ViewModels/Update/UpdateViewModel.cs`
8. Delete all update view files:
   - `Views/Update/UpdateBannerView.xaml` + `.xaml.cs`
   - `Views/Update/UpdateWindow.xaml` + `.xaml.cs`
   - `Views/UpdateBannerView.xaml` + `.xaml.cs` (duplicate at root Views/ level)
9. In `App.xaml.cs`, remove these DI registrations from `ConfigureServices()`:
   - `services.AddSingleton<IUpdateService, UpdateClientService>();`
   - `services.AddTransient<ViewModels.Update.UpdateViewModel>();`
   - `services.AddTransient<ViewModels.Update.UpdateBannerViewModel>();`
   - `services.AddTransient<Views.Update.UpdateWindow>();`
   - `services.AddTransient<Views.Update.UpdateBannerView>();`
10. In `App.xaml.cs`, remove the `using DocuFiller.Services.Update;` import

## Must-Haves

- [ ] DocuFiller.csproj has no ValidateUpdateClientFiles, ValidateReleaseFiles targets, and no External file references
- [ ] External/ directory deleted
- [ ] All update files (Services/Update/*, Models/Update/*, ViewModels/Update/*, Views/Update/*, Views/UpdateBannerView.*, DocuFiller/Services/Update/*) deleted
- [ ] App.xaml.cs has no update service DI registrations
  - Files: `DocuFiller.csproj`, `App.xaml.cs`, `Services/Update/IUpdateService.cs`, `Services/Update/UpdateClientService.cs`, `Services/Update/UpdateService.cs`, `DocuFiller/Services/Update/UpdateDownloader.cs`, `Models/Update/DaemonProgressInfo.cs`, `Models/Update/DownloadProgress.cs`, `Models/Update/DownloadStatus.cs`, `Models/Update/UpdateClientResponseModels.cs`, `Models/Update/UpdateConfig.cs`, `Models/Update/VersionInfo.cs`, `ViewModels/Update/UpdateBannerViewModel.cs`, `ViewModels/Update/UpdateViewModel.cs`, `Views/Update/UpdateBannerView.xaml`, `Views/Update/UpdateWindow.xaml`, `Views/UpdateBannerView.xaml`
  - Verify: grep -c "ValidateUpdateClientFiles\|ValidateReleaseFiles\|update-client" DocuFiller.csproj returns 0; test ! -d External; grep -c "IUpdateService\|UpdateViewModel\|UpdateBannerView\|UpdateWindow" App.xaml.cs returns 0

- [x] **T02: Remove all update code from MainWindowViewModel, MainWindow.xaml, and MainWindow.xaml.cs** `est:45m`
  Clean all update-related code from the three main UI files: MainWindowViewModel (the bulk of update logic), MainWindow.xaml (update button in Tools tab), and MainWindow.xaml.cs (event handler).

## Steps

1. In `ViewModels/MainWindowViewModel.cs`, remove these using statements:
   - `using DocuFiller.Models.Update;`
   - `using DocuFiller.Services.Update;`
   - `using DocuFiller.Views.Update;`
   - `using DocuFiller.ViewModels.Update;`

2. In `ViewModels/MainWindowViewModel.cs`, remove the `IUpdateService _updateService` field

3. In `ViewModels/MainWindowViewModel.cs`, remove these fields:
   - `private bool _isUpdateAvailable;`
   - `private VersionInfo? _latestVersionInfo;`
   - `private UpdateBannerViewModel? _updateBannerViewModel;`

4. In `ViewModels/MainWindowViewModel.cs` constructor, remove:
   - `IUpdateService updateService` parameter
   - `UpdateBannerViewModel? updateBannerViewModel = null` parameter
   - `_updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));`
   - `SubscribeToUpdateEvents();` call
   - UpdateBanner initialization block
   - The entire `Task.Run(async () => await OnInitializedAsync());` call (OnInitializedAsync only checks for updates)

5. In `ViewModels/MainWindowViewModel.cs`, delete these entire methods:
   - `OnInitializedAsync()` (startup update check)
   - `CheckForUpdatesAsync()` (update check with banner)
   - `ShowUpdateBannerAsync()` (show update banner)
   - `CheckForUpdateAsync()` (manual/auto update check)
   - `OnUpdateAvailable()` (event handler)
   - `ShowUpdateWindow()` (open update window)
   - `SubscribeToUpdateEvents()` (event subscription)

6. In `ViewModels/MainWindowViewModel.cs`, remove these properties:
   - `IsUpdateAvailable`
   - `LatestVersionInfo`
   - `UpdateBanner`

7. In `ViewModels/MainWindowViewModel.cs`, remove the `CheckForUpdateCommand` declaration and its initialization in `InitializeCommands()`

8. In `MainWindow.xaml`, remove the `xmlns:updateViews="clr-namespace:DocuFiller.Views.Update"` namespace import

9. In `MainWindow.xaml`, in the Tools TabItem, remove the entire "检查更新" Border element (Grid.Row="1" with CheckForUpdateCommand binding, the bell icon, "检查更新" title, "检查并下载最新版本" description)

10. In `MainWindow.xaml.cs`, remove the `CheckForUpdateHyperlink_Click` event handler method entirely

## Must-Haves

- [ ] MainWindowViewModel has no IUpdateService dependency, no update fields/methods/properties/commands
- [ ] MainWindow.xaml has no update UI elements or update namespace
- [ ] MainWindow.xaml.cs has no update event handlers
- [ ] Project compiles without errors after these changes
  - Files: `ViewModels/MainWindowViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`
  - Verify: grep -c "IUpdateService\|UpdateBanner\|CheckForUpdate\|ShowUpdate\|OnUpdateAvailable\|VersionInfo\|UpdateViewModel" ViewModels/MainWindowViewModel.cs returns 0; grep -c "CheckForUpdate\|updateViews\|检查更新" MainWindow.xaml returns 0; grep -c "CheckForUpdate" MainWindow.xaml.cs returns 0; dotnet build succeeds with 0 errors

- [x] **T03: Delete JSON editor leftover files and verify final build** `est:20m`
  Delete the 8 orphaned JSON editor files that have no DI registration and no active references, then run a final build verification to confirm the entire slice is complete.

## Steps

1. Delete the JSON editor service files:
   - `Services/JsonEditorService.cs`
   - `Services/Interfaces/IJsonEditorService.cs`
   - `Services/KeywordValidationService.cs`
   - `Services/Interfaces/IKeywordValidationService.cs`

2. Delete the JSON editor model files:
   - `Models/JsonKeywordItem.cs`
   - `Models/JsonProjectModel.cs`

3. Delete the JSON editor ViewModel and View:
   - `ViewModels/JsonEditorViewModel.cs`
   - `Views/JsonEditorWindow.xaml`
   - `Views/JsonEditorWindow.xaml.cs`

4. Run `dotnet build` and confirm 0 errors

5. Run grep verification to confirm no update or JSON editor code files remain in the project

## Must-Haves

- [ ] All 8+ JSON editor files deleted
- [ ] dotnet build succeeds with 0 errors
- [ ] No remaining update or JSON editor code files in the codebase
  - Files: `Services/JsonEditorService.cs`, `Services/Interfaces/IJsonEditorService.cs`, `Services/KeywordValidationService.cs`, `Services/Interfaces/IKeywordValidationService.cs`, `Models/JsonKeywordItem.cs`, `Models/JsonProjectModel.cs`, `ViewModels/JsonEditorViewModel.cs`, `Views/JsonEditorWindow.xaml`, `Views/JsonEditorWindow.xaml.cs`
  - Verify: test ! -f Services/JsonEditorService.cs; test ! -f Services/Interfaces/IJsonEditorService.cs; test ! -f Models/JsonKeywordItem.cs; test ! -f ViewModels/JsonEditorViewModel.cs; test ! -f Views/JsonEditorWindow.xaml; dotnet build exits with code 0

## Files Likely Touched

- DocuFiller.csproj
- App.xaml.cs
- Services/Update/IUpdateService.cs
- Services/Update/UpdateClientService.cs
- Services/Update/UpdateService.cs
- DocuFiller/Services/Update/UpdateDownloader.cs
- Models/Update/DaemonProgressInfo.cs
- Models/Update/DownloadProgress.cs
- Models/Update/DownloadStatus.cs
- Models/Update/UpdateClientResponseModels.cs
- Models/Update/UpdateConfig.cs
- Models/Update/VersionInfo.cs
- ViewModels/Update/UpdateBannerViewModel.cs
- ViewModels/Update/UpdateViewModel.cs
- Views/Update/UpdateBannerView.xaml
- Views/Update/UpdateWindow.xaml
- Views/UpdateBannerView.xaml
- ViewModels/MainWindowViewModel.cs
- MainWindow.xaml
- MainWindow.xaml.cs
- Services/JsonEditorService.cs
- Services/Interfaces/IJsonEditorService.cs
- Services/KeywordValidationService.cs
- Services/Interfaces/IKeywordValidationService.cs
- Models/JsonKeywordItem.cs
- Models/JsonProjectModel.cs
- ViewModels/JsonEditorViewModel.cs
- Views/JsonEditorWindow.xaml
- Views/JsonEditorWindow.xaml.cs
