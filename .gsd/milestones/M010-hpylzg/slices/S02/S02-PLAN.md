# S02: 更新设置弹窗 + 状态栏源类型显示

**Goal:** 在状态栏添加齿轮图标按钮，点击弹出 UpdateSettingsWindow 独立窗口，显示当前更新源类型、UpdateUrl 和 Channel，用户可修改并保存。保存时调用 IUpdateService.ReloadSource 实现热重载。UpdateStatusMessage 追加源类型标识（"(GitHub)" 或 "(内网: 地址)"），帮助用户一眼看出当前更新走哪个源。
**Demo:** 用户点击状态栏齿轮图标→弹出设置窗口显示当前源类型和配置→修改 URL 保存后状态栏立即显示新源类型，检查更新走新源

## Must-Haves

- UpdateSettingsWindow displays current UpdateSourceType as read-only label\n- User can edit UpdateUrl and Channel (stable/beta) in UpdateSettingsWindow\n- Save button calls IUpdateService.ReloadSource and persists to appsettings.json\n- Status bar shows source type suffix in UpdateStatusMessage (e.g. '(GitHub)' or '(内网: 192.168.1.100:8080)')\n- Gear button (⚙) in status bar opens UpdateSettingsWindow as dialog\n- Existing update check flow (startup auto-check, manual button, download, restart) unaffected\n- dotnet build returns 0 CS/MC errors

## Proof Level

- This slice proves: integration — the slice wires a new Window through DI into the existing MainWindow status bar, consuming S01's ReloadSource contract. Runtime required to verify dialog opens and closes correctly, but automated build verification proves all wiring compiles.

## Integration Closure

- Upstream surfaces consumed: IUpdateService.ReloadSource (from S01), IUpdateService.UpdateSourceType, IUpdateService.EffectiveUpdateUrl, IUpdateService.Channel\n- New wiring introduced: UpdateSettingsWindow registered as Transient in App.xaml.cs DI, MainWindowViewModel.OpenUpdateSettingsCommand opens it via ServiceProvider, UpdateStatusMessage reads IUpdateService properties\n- What remains before milestone is truly usable end-to-end: nothing — this is the final slice

## Verification

- UpdateSettingsViewModel logs save action with structured Information log\n- UpdateStatusMessage visible in status bar immediately reflects source type after ReloadSource\n- Existing UpdateService ReloadSource logs (from S01) confirm hot-reload succeeded

## Tasks

- [x] **T01: Create UpdateSettingsWindow with ViewModel for editing UpdateUrl/Channel** `est:45m`
  Create the UpdateSettingsWindow (XAML + code-behind) and UpdateSettingsViewModel. The window displays current UpdateSourceType (read-only label), UpdateUrl TextBox, Channel ComboBox (stable/beta), and Save/Cancel buttons. On Save, calls IUpdateService.ReloadSource(updateUrl, channel), shows success MessageBox, then closes. On Cancel, just closes. The ViewModel reads current values from IUpdateService on construction.
  - Files: `Views/UpdateSettingsWindow.xaml`, `Views/UpdateSettingsWindow.xaml.cs`, `ViewModels/UpdateSettingsViewModel.cs`
  - Verify: dotnet build 2>&1 | Select-String -Pattern 'error CS|error MC' | Measure-Object | Select-Object -ExpandProperty Count

- [x] **T02: Wire gear button into status bar, display source type, register window in DI** `est:30m`
  Add a gear icon button (⚙) to MainWindow.xaml status bar (between UpdateStatusText and '检查更新' button). Add OpenUpdateSettingsCommand to MainWindowViewModel that opens UpdateSettingsWindow as dialog. Modify UpdateStatusMessage getter to append source type suffix: '(GitHub)' or '(内网: host)' using IUpdateService.UpdateSourceType and EffectiveUpdateUrl. Register UpdateSettingsWindow and UpdateSettingsViewModel as Transient in App.xaml.cs DI. After settings change, trigger OnPropertyChanged for UpdateStatusMessage so the status bar refreshes.
  - Files: `MainWindow.xaml`, `ViewModels/MainWindowViewModel.cs`, `App.xaml.cs`
  - Verify: dotnet build 2>&1 | Select-String -Pattern 'error CS|error MC' | Measure-Object | Select-Object -ExpandProperty Count

## Files Likely Touched

- Views/UpdateSettingsWindow.xaml
- Views/UpdateSettingsWindow.xaml.cs
- ViewModels/UpdateSettingsViewModel.cs
- MainWindow.xaml
- ViewModels/MainWindowViewModel.cs
- App.xaml.cs
