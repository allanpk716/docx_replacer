---
estimated_steps: 5
estimated_files: 3
skills_used: []
---

# T01: Create UpdateSettingsWindow with ViewModel for editing UpdateUrl/Channel

**Slice:** S02 — 更新设置弹窗 + 状态栏源类型显示
**Milestone:** M010-hpylzg

## Description

Create the UpdateSettingsWindow (XAML + code-behind) and UpdateSettingsViewModel. The window displays current UpdateSourceType (read-only label), UpdateUrl TextBox, Channel ComboBox (stable/beta), and Save/Cancel buttons. On Save, calls IUpdateService.ReloadSource(updateUrl, channel), shows success MessageBox, then closes. On Cancel, just closes. The ViewModel reads current values from IUpdateService on construction.

## Steps

1. Create `ViewModels/UpdateSettingsViewModel.cs` (namespace `DocuFiller.ViewModels`, matching project convention — ViewModels live in root `ViewModels/` directory):
   - Constructor takes `IUpdateService updateService` and `ILogger<UpdateSettingsViewModel> logger` (both required, non-nullable)
   - Initialize `UpdateUrl` from `updateService.EffectiveUpdateUrl` — but since EffectiveUpdateUrl includes the channel path suffix (e.g. "http://host/stable/"), strip the trailing `/{channel}/` to recover the raw base URL for display. If UpdateSourceType is "GitHub", set UpdateUrl to empty string.
   - Initialize `Channel` from `updateService.Channel` (should be "stable" or "beta")
   - Initialize `SourceTypeDisplay` from `updateService.UpdateSourceType` (read-only display string)
   - Create `SaveCommand` (RelayCommand) that:
     - Calls `_updateService.ReloadSource(UpdateUrl, Channel)`
     - Logs Information: "更新设置已保存：UpdateUrl={UpdateUrl}, Channel={Channel}"
     - Sets `DialogResult = true` (via event or callback — see code-behind)
     - Catches exceptions and shows MessageBox with error message
   - Create `CancelCommand` (RelayCommand) that sets `DialogResult = false`
   - Properties: `UpdateUrl` (string, settable), `Channel` (string, settable), `SourceTypeDisplay` (string, read-only), `Channels` (ObservableCollection<string> initialized with "stable" and "beta")
   - Use `INotifyPropertyChanged` with `SetProperty` pattern matching the project convention (see MainWindowViewModel)

2. Create `DocuFiller/Views/UpdateSettingsWindow.xaml` (views are inside `DocuFiller/Views/` directory, matching CleanupWindow location):
   - Window: Title="更新源设置", SizeToContent="Height", Width="450", WindowStartupLocation="CenterOwner", ResizeMode="NoResize"
   - x:Class="DocuFiller.Views.UpdateSettingsWindow"
   - Grid with Margin="20" and RowDefinitions for 4 rows (Auto each):
     - Row 0: Label "当前源类型：" + TextBlock bound to SourceTypeDisplay (read-only, bold)
     - Row 1: Label "更新源 URL：" + TextBox bound to UpdateUrl (UpdateSourceTrigger=PropertyChanged), with a hint TextBlock below saying "留空则使用 GitHub Releases"
     - Row 2: Label "更新通道：" + ComboBox bound to Channel, ItemsSource bound to Channels
     - Row 3: StackPanel (horizontal, right-aligned) with "保存" Button (Command=SaveCommand) and "取消" Button (Command=CancelCommand)
   - Style buttons to match the project's existing simple WPF style (no custom template needed)

3. Create `DocuFiller/Views/UpdateSettingsWindow.xaml.cs`:
   - Namespace `DocuFiller.Views`, class `UpdateSettingsWindow`
   - Constructor takes `ILogger<UpdateSettingsWindow> logger` and resolves `UpdateSettingsViewModel` from `App.Current.ServiceProvider`
   - Sets `DataContext = _viewModel`
   - Subscribe to ViewModel's close-request (either via event or by having the Save/Cancel commands call `DialogResult = true/false` on the Window directly)
   - Simplest approach: SaveCommand and CancelCommand in ViewModel accept an `Action<bool?>` closeCallback, injected in code-behind after construction

4. Verify the window XAML namespace is correct: `x:Class="DocuFiller.Views.UpdateSettingsWindow"` (matching CleanupWindow pattern)

5. Run `dotnet build` and confirm 0 CS/MC errors

## Must-Haves

- [ ] UpdateSettingsViewModel reads IUpdateService current state on construction
- [ ] SaveCommand calls IUpdateService.ReloadSource with user-edited values
- [ ] UpdateSourceType displayed as read-only label in the window
- [ ] Channel ComboBox with stable/beta options

## Verification

- `dotnet build` returns 0 CS/MC errors
- `Views/UpdateSettingsWindow.xaml`, `Views/UpdateSettingsWindow.xaml.cs`, and `ViewModels/UpdateSettingsViewModel.cs` exist and are non-empty

## Inputs

- `Services/Interfaces/IUpdateService.cs` — ReloadSource method signature and property contracts
- `Services/UpdateService.cs` — EffectiveUpdateUrl includes channel path, UpdateSourceType returns "HTTP" or "GitHub"
- `DocuFiller/Views/CleanupWindow.xaml` — XAML style reference (views are in `DocuFiller/Views/`)
- `DocuFiller/Views/CleanupWindow.xaml.cs` — code-behind pattern reference (DI resolution from App.Current.ServiceProvider)

## Expected Output

- `DocuFiller/Views/UpdateSettingsWindow.xaml` — new settings dialog XAML
- `DocuFiller/Views/UpdateSettingsWindow.xaml.cs` — new settings dialog code-behind
- `ViewModels/UpdateSettingsViewModel.cs` — ViewModel with Save/Cancel logic calling IUpdateService.ReloadSource
