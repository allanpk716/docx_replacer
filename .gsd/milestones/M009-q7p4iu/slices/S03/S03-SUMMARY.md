---
id: S03
parent: M009-q7p4iu
milestone: M009-q7p4iu
provides:
  - ["GUI 状态栏常驻更新提示（三种状态：便携版/有新版本/检查失败），ViewModel UpdateStatus 枚举 + 属性联动模式，XAML InputBindings 声明式点击交互"]
requires:
  - slice: S02
    provides: IUpdateService.IsInstalled, IUpdateService.CheckForUpdatesAsync, IUpdateService.IsUpdateUrlConfigured
affects:
  - ["S04"]
key_files:
  - ["ViewModels/MainWindowViewModel.cs", "MainWindow.xaml"]
key_decisions:
  - ["CurrentUpdateStatus 单一 setter 统一触发三个派生属性的 PropertyChanged", "TextBlock.InputBindings + MouseBinding 实现点击交互，保持 XAML 纯声明式", "UpdateStatusClickCommand Error 状态重试 InitializeUpdateStatusAsync 而非直接调用 CheckUpdateAsync", "IUpdateService? 可选注入模式：null 时安全跳过整个更新检查流程"]
patterns_established:
  - ["ViewModel 枚举驱动属性联动：单一枚举 setter 触发所有派生属性通知", "WPF TextBlock 点击交互：InputBindings+MouseBinding 声明式模式替代 code-behind", "构造函数 fire-and-forget 异步初始化：_ = MethodAsync() + try-catch 兜底"]
observability_surfaces:
  - ["InitializeUpdateStatusAsync 每条状态判定分支输出 Information 级别日志（便携版/未配置/有更新/最新/异常）", "ViewModel 属性 UpdateStatusMessage 可通过 WPF 绑定观察", "检查更新失败时日志记录异常详情"]
drill_down_paths:
  - [".gsd/milestones/M009-q7p4iu/slices/S03/tasks/T01-SUMMARY.md", ".gsd/milestones/M009-q7p4iu/slices/S03/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-26T11:19:52.922Z
blocker_discovered: false
---

# S03: GUI 状态栏常驻更新提示

**ViewModel 属性驱动状态栏三种更新提示（便携版/有更新/检查失败），XAML 声明式绑定，fire-and-forget 启动检查**

## What Happened

S03 实现了 GUI 状态栏常驻更新提示功能，分两个任务完成：

**T01（MainWindowViewModel）**：定义 UpdateStatus 枚举（None/PortableVersion/UpdateAvailable/UpToDate/Checking/Error），新增 CurrentUpdateStatus 属性，其 setter 统一触发 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus 三个派生属性的 PropertyChanged。构造函数末尾 fire-and-forget 调用 InitializeUpdateStatusAsync，按优先级判定状态（便携版→更新源未配置→调用 CheckForUpdatesAsync→结果映射→异常兜底），每步输出 Information 级别日志。UpdateStatusClickCommand 封装点击逻辑，Error 状态重试检查。

**T02（MainWindow.xaml）**：状态栏 Grid 从 3 列扩展为 4 列，在版本号和中间消息之后新增 TextBlock（Column 2），绑定 Text/Foreground/Visibility 到 ViewModel 属性。使用 InputBindings+MouseBinding 实现点击交互，带下划线 TextDecoration 暗示可点击。现有"检查更新"按钮移至 Column 3，功能不变。

集成验证：dotnet build -c Release 0 错误，dotnet test 全部 172 测试通过（27 E2E + 145 单元测试）。

## Verification

dotnet build -c Release: 0 errors (exit code 0). dotnet test --no-build -c Release: 172/172 tests passed (27 E2E + 145 unit). XAML 绑定审查：UpdateStatusText.Text→UpdateStatusMessage, .Foreground→UpdateStatusBrush, .Visibility→HasUpdateStatus+BooleanToVisibilityConverter, MouseBinding LeftClick→UpdateStatusClickCommand. ViewModel 逻辑审查：CurrentUpdateStatus setter 正确触发派生属性通知，InitializeUpdateStatusAsync 覆盖便携版/未配置/有更新/最新/异常五条路径，每条输出日志。

## Requirements Advanced

- R040 — ViewModel 新增 UpdateStatus 枚举 + 5 个属性 + 自动检查逻辑 + 点击命令；XAML 状态栏新增 TextBlock 绑定，实现三种状态的常驻提示

## Requirements Validated

- R040 — T01+T02: UpdateStatus 枚举6种状态，属性联动正确，XAML 绑定完整，构建通过172测试
- R043 — 现有检查更新按钮保留（Column 3），新增提示使用独立列和 InputBindings，172测试零回归

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
