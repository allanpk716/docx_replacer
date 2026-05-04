---
id: T01
parent: S01
milestone: M021
key_files:
  - ViewModels/UpdateStatusViewModel.cs
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - App.xaml.cs
key_decisions:
  - Used IServiceProvider injection instead of ((App)Application.Current).ServiceProvider for window creation in UpdateStatusViewModel, following DI best practices
  - UpdateStatus enum moved into UpdateStatusViewModel.cs file rather than a separate file, keeping related types co-located
duration: 
verification_result: passed
completed_at: 2026-05-04T10:17:35.924Z
blocker_discovered: false
---

# T01: Create UpdateStatusViewModel with CT.Mvvm and extract all update-related properties/commands from MainWindowViewModel

**Create UpdateStatusViewModel with CT.Mvvm and extract all update-related properties/commands from MainWindowViewModel**

## What Happened

Created `ViewModels/UpdateStatusViewModel.cs` (397 lines) using CT.Mvvm `[ObservableProperty]` + `[RelayCommand]` pattern, following the established pattern from DownloadProgressViewModel and UpdateSettingsViewModel. The new ViewModel manages:

1. **UpdateStatus enum** — moved from MainWindowViewModel namespace scope into the new file
2. **Properties** — `IsCheckingUpdate`, `CurrentUpdateStatus` (ObservableProperty); `CanCheckUpdate`, `UpdateStatusMessage`, `UpdateStatusBrush`, `HasUpdateStatus`, `CurrentVersion` (derived/computed)
3. **Commands** — `CheckUpdateCommand`, `UpdateStatusClickCommand`, `OpenUpdateSettingsCommand` (all with [RelayCommand])
4. **Methods** — `CheckUpdateAsync`, `InitializeUpdateStatusAsync`, `OnUpdateStatusClickAsync`, `OpenUpdateSettings`, `ExtractHostFromUrl` (all migrated from MainWindowViewModel)
5. **Side effects** — `OnIsCheckingUpdateChanged` and `OnCurrentUpdateStatusChanged` partial methods for CanExecute notification

Key implementation decisions:
- Used `IServiceProvider` injection for creating DownloadProgressWindow and UpdateSettingsWindow (replacing `((App)Application.Current).ServiceProvider` pattern)
- Constructor takes `IUpdateService?` (nullable) to match the optional registration pattern
- `InitializeAsync()` public method called from MainWindowViewModel constructor for fire-and-forget startup check
- Fully-qualified `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` to avoid conflict with project's custom `ObservableObject.cs`

From MainWindowViewModel, removed: UpdateStatus enum, all update-related fields/properties/commands/methods, `System.Windows.Media` using. Added `UpdateStatusVM` property as sub-ViewModel reference and updated constructor to accept `UpdateStatusViewModel` parameter.

XAML bindings updated: all 10 update-related bindings changed from direct property paths (e.g., `{Binding UpdateStatusMessage}`) to sub-VM paths (e.g., `{Binding UpdateStatusVM.UpdateStatusMessage}`).

DI registration added in App.xaml.cs: `services.AddTransient<UpdateStatusViewModel>()` registered before MainWindowViewModel.

## Verification

Build verified: `dotnet build DocuFiller.csproj` succeeds with 0 errors, 0 warnings. All update-related bindings in MainWindow.xaml correctly reference `UpdateStatusVM.*` paths. No dangling references to removed code remain in MainWindowViewModel (grep confirms only 4 references to UpdateStatusViewModel, all for the sub-VM field/property/constructor).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj` | 0 | ✅ pass | 7860ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/UpdateStatusViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `App.xaml.cs`
