---
id: T01
parent: S02
milestone: M010-hpylzg
key_files:
  - ViewModels/UpdateSettingsViewModel.cs
  - DocuFiller/Views/UpdateSettingsWindow.xaml
  - DocuFiller/Views/UpdateSettingsWindow.xaml.cs
  - App.xaml.cs
key_decisions:
  - Used internal CloseCallback Action<bool?> property on ViewModel injected by code-behind, avoiding event subscription complexity while keeping ViewModel testable
duration: 
verification_result: passed
completed_at: 2026-04-29T09:45:55.458Z
blocker_discovered: false
---

# T01: Create UpdateSettingsWindow + ViewModel for editing update source URL and channel with ReloadSource integration

**Create UpdateSettingsWindow + ViewModel for editing update source URL and channel with ReloadSource integration**

## What Happened

Created three files implementing the update settings dialog:

1. **ViewModels/UpdateSettingsViewModel.cs** — ViewModel reading current IUpdateService state on construction. Extracts the raw base URL from EffectiveUpdateUrl by stripping the `/{channel}/` suffix. For GitHub source type, displays empty URL. SaveCommand calls `IUpdateService.ReloadSource(UpdateUrl, Channel)` and logs the save action at Information level. CancelCommand closes with DialogResult=false. Uses INotifyPropertyChanged with SetProperty pattern matching project convention. CloseCallback (Action<bool?>) is injected by code-behind for window control.

2. **DocuFiller/Views/UpdateSettingsWindow.xaml** — Dialog with Title="更新源设置", SizeToContent=Height, Width=450, CenterOwner, NoResize. Contains read-only SourceTypeDisplay label, editable UpdateUrl TextBox with hint "留空则使用 GitHub Releases", Channel ComboBox bound to Channels collection (stable/beta), and Save/Cancel buttons bound to commands.

3. **DocuFiller/Views/UpdateSettingsWindow.xaml.cs** — Code-behind following CleanupWindow pattern: constructor takes ILogger, resolves UpdateSettingsViewModel from App.Current.ServiceProvider, sets DataContext, and injects CloseCallback lambda that sets DialogResult and closes the window.

Also registered UpdateSettingsViewModel (Transient) and UpdateSettingsWindow (Transient) in App.xaml.cs ConfigureServices.

## Verification

- `dotnet build` completed with 0 errors and 0 warnings
- All three output files exist and are non-empty (132, 50, 29 lines respectively)
- ViewModel correctly reads IUpdateService current state, handles URL stripping logic
- SaveCommand calls ReloadSource with user-edited values and logs structured Information message
- DI registration follows existing CleanupWindow/CleanupViewModel pattern

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 4940ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/UpdateSettingsViewModel.cs`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`
- `DocuFiller/Views/UpdateSettingsWindow.xaml.cs`
- `App.xaml.cs`
