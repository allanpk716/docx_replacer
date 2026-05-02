---
id: S02
parent: M010-hpylzg
milestone: M010-hpylzg
provides:
  - ["UpdateSettingsWindow.xaml(.cs) — independent dialog for editing UpdateUrl/Channel", "UpdateSettingsViewModel — reads IUpdateService state, SaveCommand calls ReloadSource", "MainWindowViewModel.OpenUpdateSettingsCommand — opens settings dialog and refreshes status bar on save", "UpdateStatusMessage source type suffix — '(GitHub)' or '(内网: host)'"]
requires:
  - slice: S01
    provides: IUpdateService.ReloadSource, UpdateService.UpdateSourceType, UpdateService.EffectiveUpdateUrl, UpdateService.Channel
affects:
  - []
key_files:
  - ["ViewModels/UpdateSettingsViewModel.cs", "DocuFiller/Views/UpdateSettingsWindow.xaml", "DocuFiller/Views/UpdateSettingsWindow.xaml.cs", "ViewModels/MainWindowViewModel.cs", "MainWindow.xaml", "App.xaml.cs"]
key_decisions:
  - ["Used CloseCallback Action<bool?> injected by code-behind for window control instead of event subscriptions", "ExtractHostFromUrl helper parses host from URL for display in status bar suffix", "Gear button placed at Grid.Column=3, shifting '检查更新' to Column=4"]
patterns_established:
  - ["Window+ViewModel DI pattern: Transient registration, code-behind resolves from ServiceProvider, CloseCallback for dialog control", "Status bar source type suffix: append '(GitHub)' or '(内网: host)' based on UpdateSourceType, refresh via OnPropertyChanged after dialog save"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M010-hpylzg/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M010-hpylzg/slices/S02/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-29T09:51:47.581Z
blocker_discovered: false
---

# S02: 更新设置弹窗 + 状态栏源类型显示

**状态栏齿轮按钮弹出 UpdateSettingsWindow 编辑更新源，保存后热重载并即时刷新源类型显示**

## What Happened

S02 在两个任务中完成了更新设置 GUI 的全部功能：

T01 创建了 UpdateSettingsWindow（XAML + code-behind）和 UpdateSettingsViewModel。窗口显示当前源类型（只读标签）、可编辑的 UpdateUrl TextBox 和 Channel ComboBox（stable/beta）。ViewModel 在构造时从 IUpdateService 读取当前状态，SaveCommand 调用 ReloadSource 并记录 Information 日志。通过 CloseCallback Action<bool?> 模式实现窗口控制，保持 ViewModel 可测试。DI 注册为 Transient。

T02 在 MainWindow.xaml 状态栏添加了 ⚙ 齿轮按钮（Grid.Column=3），绑定到 MainWindowViewModel.OpenUpdateSettingsCommand。UpdateStatusMessage getter 根据 UpdateSourceType 追加源类型后缀（"(GitHub)" 或 "(内网: host)"），通过 ExtractHostFromUrl 辅助方法解析主机地址。对话框保存后触发 OnPropertyChanged 刷新状态栏。"检查更新"按钮移至 Column=4。

最终验证：dotnet build 0 错误，192/192 测试全部通过，无回归。

## Verification

dotnet build: 0 errors, 0 CS/MC errors. dotnet test: 192/192 pass (165 unit + 27 E2E). Key files verified: UpdateSettingsWindow.xaml (50 lines), UpdateSettingsWindow.xaml.cs (29 lines), UpdateSettingsViewModel.cs (132 lines). DI registration confirmed in App.xaml.cs (both Transient). Gear button at Grid.Column=3 in MainWindow.xaml, OpenUpdateSettingsCommand binding confirmed. UpdateStatusMessage getter references _updateService.UpdateSourceType and ExtractHostFromUrl. "检查更新" button at Grid.Column=4.

## Requirements Advanced

- R029 — UpdateSettingsWindow provides GUI for editing UpdateUrl/Channel with ReloadSource integration
- R045 — UpdateStatusMessage getter appends source type suffix using UpdateSourceType and EffectiveUpdateUrl
- R046 — All 192 tests pass, no modifications to existing update check/download/restart flows

## Requirements Validated

- R029 — Build 0 errors, 192/192 tests pass, UpdateSettingsWindow calls ReloadSource on save
- R045 — UpdateStatusMessage getter code verified — appends '(GitHub)' or '(内网: host)' suffix
- R046 — 192/192 tests pass, no changes to CheckUpdateAsync/DownloadUpdatesAsync/ApplyUpdatesAndRestart

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
