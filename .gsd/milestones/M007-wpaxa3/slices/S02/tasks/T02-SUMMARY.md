---
id: T02
parent: S02
milestone: M007-wpaxa3
key_files:
  - MainWindow.xaml
  - ViewModels/MainWindowViewModel.cs
key_decisions:
  - IUpdateService 作为可选构造函数参数注入（IUpdateService? updateService = null），保证不注册时 ViewModel 仍可正常工作
duration: 
verification_result: passed
completed_at: 2026-04-24T06:00:19.748Z
blocker_discovered: false
---

# T02: 在 MainWindow 底部状态栏添加版本号显示和检查更新按钮，实现完整的更新检查交互流程

**在 MainWindow 底部状态栏添加版本号显示和检查更新按钮，实现完整的更新检查交互流程**

## What Happened

在 MainWindow.xaml 底部添加了状态栏，使用 DockPanel 布局将状态栏固定在窗口底部。状态栏左侧显示当前版本号（通过 VersionHelper.GetCurrentVersion()），中间显示进度消息，右侧放置"检查更新"按钮。按钮通过 CanCheckUpdate 绑定控制灰显状态——更新源未配置时按钮自动灰显。

在 MainWindowViewModel 中注入 IUpdateService（可选参数，保证向后兼容），添加 CurrentVersion 属性、IsCheckingUpdate/CanCheckUpdate 属性、CheckUpdateCommand 命令。CheckUpdateAsync 方法实现完整的更新检查流程：调用 CheckForUpdatesAsync() 检测版本，有新版本时弹出确认对话框（显示新旧版本号），用户确认后下载更新并调用 ApplyUpdatesAndRestart() 重启应用；无新版本显示"已是最新版本"；异常时显示错误信息。所有状态变更都有 ILogger 日志记录。

构建通过 0 错误 0 警告，全部 162 个测试通过（135 单元测试 + 27 E2E 测试）。

## Verification

运行 dotnet build 构建成功（0 错误 0 警告），dotnet test 全部 162 个测试通过（135 单元测试 + 27 E2E 测试）。XAML 状态栏通过 DockPanel.Dock="Bottom" 固定在窗口底部，版本号和按钮绑定到 ViewModel 属性。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2130ms |
| 2 | `dotnet test` | 0 | ✅ pass | 90000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `ViewModels/MainWindowViewModel.cs`
