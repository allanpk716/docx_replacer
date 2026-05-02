---
estimated_steps: 40
estimated_files: 3
skills_used: []
---

# T02: Remove all update code from MainWindowViewModel, MainWindow.xaml, and MainWindow.xaml.cs

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

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`

## Expected Output

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`

## Verification

grep -c "IUpdateService\|UpdateBanner\|CheckForUpdate\|ShowUpdate\|OnUpdateAvailable\|VersionInfo\|UpdateViewModel" ViewModels/MainWindowViewModel.cs returns 0; grep -c "CheckForUpdate\|updateViews\|检查更新" MainWindow.xaml returns 0; grep -c "CheckForUpdate" MainWindow.xaml.cs returns 0; dotnet build succeeds with 0 errors
