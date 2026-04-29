---
id: M010-hpylzg
title: "GUI 更新源配置"
status: complete
completed_at: 2026-04-29T09:57:46.689Z
key_decisions:
  - D030: IUpdateService 新增 ReloadSource 方法，运行时重建 IUpdateSource，Singleton 生命周期不变
  - D031: 独立 WPF Window (UpdateSettingsWindow)，状态栏齿轮按钮触发
  - D032: UpdateStatusMessage 追加源类型后缀，复用现有 TextBlock
key_files:
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
  - ViewModels/UpdateSettingsViewModel.cs
  - DocuFiller/Views/UpdateSettingsWindow.xaml
  - DocuFiller/Views/UpdateSettingsWindow.xaml.cs
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - App.xaml.cs
  - Tests/UpdateServiceTests.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
lessons_learned:
  - Singleton 服务可通过方法级重载实现运行时行为切换，无需改为 Transient
  - System.Text.Json.Nodes 适合配置文件局部修改（读→改节点→写回），比全序列化更安全
  - 状态栏信息追加后缀是最小改动方式，避免新增 UI 元素增加复杂度
---

# M010-hpylzg: GUI 更新源配置

**状态栏齿轮按钮弹出 UpdateSettingsWindow 编辑更新源，ReloadSource 热重载 + appsettings.json 写回，状态栏显示源类型标识 (GitHub/内网)**

## What Happened

M010 分两个切片完成 GUI 更新源配置功能。

S01 在 UpdateService 层实现热重载核心能力：新增 IUpdateService.ReloadSource(string updateUrl, string channel) 方法，运行时重建 IUpdateSource（SimpleWebSource 或 GithubSource），后续 CheckForUpdatesAsync 自动使用新源。同时实现 PersistToAppSettings 方法，通过 System.Text.Json.Nodes 修改 appsettings.json 的 Update:UpdateUrl 和 Update:Channel 节点并写回文件。EffectiveUpdateUrl 和 UpdateSourceType 属性提升为公开接口成员供 S02 GUI 消费。21 个单元测试覆盖内存热重载、GitHub 回退、通道更新、边界值处理和文件持久化。

S02 实现 GUI 层：UpdateSettingsWindow 独立弹窗（ViewModel + DI），状态栏齿轮图标按钮（MainWindowViewModel.OpenUpdateSettingsCommand），保存后调用 ReloadSource 并即时刷新 UpdateStatusMessage 的源类型后缀——"(GitHub)" 或 "(内网: host)"。弹窗使用 CloseCallback 模式控制窗口关闭，ExtractHostFromUrl 辅助方法解析 URL 主机名用于显示。

两个切片通过 IUpdateService.ReloadSource 接口衔接，S02 消费 S01 提供的热重载方法和属性。全部 192 个测试通过，编译 0 错误，现有更新检查流程无回归。

## Success Criteria Results

- ✅ 状态栏显示更新源类型标识 — UpdateStatusMessage getter 追加 "(GitHub)" 或 "(内网: host)" 后缀，通过 IUpdateService.UpdateSourceType 和 EffectiveUpdateUrl 判断。S02 验证通过。
- ✅ 齿轮图标按钮点击弹出设置窗口，可编辑 UpdateUrl 和 Channel — MainWindow.xaml 状态栏 Grid.Column=3 齿轮按钮，绑定 OpenUpdateSettingsCommand，弹出 UpdateSettingsWindow。S02 验证通过。
- ✅ 保存配置后立即生效（热重载），不需要重启应用 — SaveCommand 调用 IUpdateService.ReloadSource，运行时重建 IUpdateSource。S01 21 个测试 + S02 集成验证通过。
- ✅ 配置同时持久化到 appsettings.json — PersistToAppSettings 使用 System.Text.Json.Nodes 读写 JSON，保留其他配置节。S01 4 个持久化测试通过。
- ✅ 现有更新检查流程无回归 — 全部 192 个测试通过，新增代码仅追加 OpenUpdateSettingsCommand 和 UpdateStatusMessage 后缀，未修改 CheckUpdateAsync/DownloadUpdatesAsync/ApplyUpdatesAndRestart。

## Definition of Done Results

- ✅ 所有切片完成（S01: 2/2 tasks, S02: 2/2 tasks）
- ✅ 所有切片摘要存在（S01-SUMMARY.md, S02-SUMMARY.md）
- ✅ 跨切片集成点正常：S02 通过 IUpdateService.ReloadSource 消费 S01 的热重载能力
- ✅ dotnet build 通过（0 errors, 0 warnings）
- ✅ 全部测试通过（192/192）

## Requirement Outcomes

- R029: deferred → validated — UpdateSettingsWindow 提供 GUI 编辑 UpdateUrl/Channel，Save 调用 IUpdateService.ReloadSource，配置持久化到 appsettings.json，状态栏显示源类型后缀。dotnet build 0 errors, 192/192 tests pass。
- R044: active → validated — ReloadSource 方法通过 21 个单元测试验证（内存热重载 + 文件持久化），覆盖 HTTP/GitHub 切换、通道更新、边界值、appsettings.json 写回。
- R045: active → validated — UpdateStatusMessage 追加 "(GitHub)" 或 "(内网: host)" 后缀，通过 IUpdateService.UpdateSourceType 和 EffectiveUpdateUrl。保存后刷新。
- R046: active → validated — 全部 192 个测试通过，新增代码不修改现有更新流程（CheckUpdateAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart）。

## Deviations

None.

## Follow-ups

None.
