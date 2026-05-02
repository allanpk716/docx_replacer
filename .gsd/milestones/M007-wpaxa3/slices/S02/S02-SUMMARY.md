---
id: S02
parent: M007-wpaxa3
milestone: M007-wpaxa3
provides:
  - ["Services/UpdateService.cs — IUpdateService 完整实现（4 个成员）", "MainWindow.xaml — 底部状态栏（版本号 + 检查更新按钮）", "ViewModels/MainWindowViewModel.cs — 更新检查命令、状态属性、交互流程", "IUpdateService DI 注册（Singleton）"]
requires:
  - slice: S01
    provides: IUpdateService 接口定义, Program.cs VelopackApp 初始化, appsettings.json Update:UpdateUrl 配置节点
affects:
  - ["S04"]
key_files:
  - ["Services/UpdateService.cs", "App.xaml.cs", "MainWindow.xaml", "ViewModels/MainWindowViewModel.cs"]
key_decisions:
  - ["每个 API 方法创建独立 UpdateManager 实例避免状态管理问题", "ApplyUpdatesAndRestart 通过 UpdatePendingRestart 获取 VelopackAsset 参数（Velopack 0.0.1298 API 要求）", "IUpdateService 作为可选构造函数参数注入（IUpdateService? updateService = null）保证向后兼容"]
patterns_established:
  - ["更新服务使用每个方法独立 UpdateManager 实例模式", "ViewModel 可选注入模式（nullable + 默认值）用于向后兼容"]
observability_surfaces:
  - ["ILogger<UpdateService> 记录更新检查/下载/应用生命周期（Information 级别）", "ILogger<MainWindowViewModel> 记录命令执行和状态变更", "Error 级别日志记录网络失败和 Velopack 异常"]
drill_down_paths:
  - [".gsd/milestones/M007-wpaxa3/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M007-wpaxa3/slices/S02/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-24T06:02:45.259Z
blocker_discovered: false
---

# S02: 更新服务 + 状态栏 UI

**实现 UpdateService 封装 Velopack UpdateManager 并注册 DI，MainWindow 底部状态栏显示版本号和检查更新按钮，完整更新检查交互流程（检测/下载/确认/重启）**

## What Happened

T01 创建了 `Services/UpdateService.cs`，实现 IUpdateService 接口的全部 4 个成员：CheckForUpdatesAsync（创建 UpdateManager 检测远程最新版本）、DownloadUpdatesAsync（下载更新包并支持进度回调）、ApplyUpdatesAndRestart（应用已下载更新并重启应用）、IsUpdateUrlConfigured（检查 Update:UpdateUrl 配置是否为空）。每个方法创建独立 UpdateManager 实例避免状态管理问题，ApplyUpdatesAndRestart 通过 UpdatePendingRestart 属性获取已下载资产传入。服务注册为 Singleton。添加了 ILogger 日志记录生命周期事件。

T02 在 MainWindow.xaml 底部添加 StatusBar（DockPanel.Dock="Bottom"），左侧显示 VersionHelper.GetCurrentVersion() 版本号，右侧放置"检查更新"按钮。MainWindowViewModel 注入 IUpdateService（可选构造函数参数 IUpdateService? updateService = null，保证向后兼容），实现 CheckUpdateCommand 命令和 CanCheckUpdate 属性。完整交互流程：有新版本弹确认对话框（显示新旧版本号），用户确认后下载并重启；无新版本显示"已是最新版本"；异常显示错误信息。IsUpdateUrlConfigured 为 false 时按钮自动灰显。

所有构建通过（0 错误 0 警告），全部 162 个测试通过（135 单元 + 27 E2E）。

## Verification

全部 6 项切片验证检查通过：
1. `dotnet build` — 0 错误 0 警告 ✅
2. `dotnet test` — 162 tests pass (135 + 27), 0 failures ✅
3. `grep -c "CheckUpdateCommand" ViewModels/MainWindowViewModel.cs` — 2 (≥1) ✅
4. `grep -c "IUpdateService" App.xaml.cs` — 1 (≥1, DI registration) ✅
5. `grep -c "StatusBar" MainWindow.xaml` — 1 (≥1) ✅
6. `test -f Services/UpdateService.cs` — EXISTS ✅

## Requirements Advanced

None.

## Requirements Validated

- R023 — UpdateService.cs 实现全部 4 个 IUpdateService 成员，App.xaml.cs 注册为 Singleton，从 IConfiguration 读取 Update:UpdateUrl，dotnet build 0 errors + dotnet test 162 pass
- R024 — MainWindow.xaml StatusBar 显示版本号和检查更新按钮，ViewModel 实现 CheckUpdateCommand 和 CanCheckUpdate 绑定，完整交互流程（检测/确认/下载/重启），dotnet build 0 errors + dotnet test 162 pass

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

["Velopack UpdateManager 在非 Velopack 安装环境下（dotnet run）无法真正连接更新源进行完整的更新流程，运行时验证推迟到 S04", "检查更新确认对话框使用 WPF MessageBox 而非自定义样式对话框（功能足够，后续可美化）"]

## Follow-ups

None.

## Files Created/Modified

None.
