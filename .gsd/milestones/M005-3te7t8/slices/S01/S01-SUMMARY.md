---
id: S01
parent: M005-3te7t8
milestone: M005-3te7t8
provides:
  - ["ConsoleHelper — WinExe stdout P/Invoke 基础设施", "JsonlOutput — JSONL 格式化输出工具", "CliRunner — 参数解析和命令分发框架", "ICliCommand — 子命令处理器接口", "Program.cs — CLI/GUI 双模式入口点模式"]
requires:
  []
affects:
  []
key_files:
  - ["Program.cs", "App.xaml.cs", "Cli/ConsoleHelper.cs", "Cli/JsonlOutput.cs", "Cli/CliRunner.cs", "Cli/Commands/InspectCommand.cs", "DocuFiller.csproj", "Utils/LoggerConfiguration.cs"]
key_decisions:
  - ["Program.cs 自定义入口点绕过 WPF InitializeComponent()", "StartupObject=DocuFiller.Program 在 csproj 中设置", "CLI 模式禁用 console logger 避免污染 JSONL 输出", "App.CreateCliServices() 静态方法为 CLI 提供 ServiceProvider"]
patterns_established:
  - ["CLI 入口通过 Program.Main 分叉，不依赖 WPF Application 生命周期", "JSONL 统一 envelope: {type, status, timestamp, data}", "ICliCommand 接口供子命令处理器实现，通过 DI 注册", "子命令通过 ServiceProvider.GetServices<ICliCommand>() 查找处理器"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-23T16:00:39.520Z
blocker_discovered: false
---

# S01: CLI 框架 + inspect 子命令

**建立 CLI 基础框架（ConsoleHelper + JsonlOutput + CliRunner + Program.cs 入口），实现 inspect 子命令端到端可用，所有 JSONL 输出和错误路径验证通过**

## What Happened

## 概要

S01 搭建了 DocuFiller 的 CLI 基础框架并实现了 inspect 子命令，验证了最关键的技术风险已解决。

## 任务执行

- **T01**: 创建三个核心 CLI 基础设施类：ConsoleHelper（WinExe P/Invoke）、JsonlOutput（JSONL 统一 envelope）、CliRunner（参数解析 + ICliCommand 命令分发）
- **T02**: 修改 App.xaml.cs 实现 CLI/GUI 双模式分叉（初始方案在 OnStartup 中分叉，后被 T05 验证阶段发现的问题覆盖）
- **T03**: 实现 InspectCommand（通过 DI 调用 IDocumentProcessor.GetContentControlsAsync，输出控件列表 JSONL）
- **T04**: 实现 --help/-h JSONL 输出（全局 + 子命令级别）+ --version，修复参数解析顺序 bug
- **T05**: 修复控制台日志污染 JSONL 输出问题（LoggerConfiguration.CreateLoggerFactory 添加 enableConsole 参数）

## 关键修复（验证阶段发现）

在端到端验证阶段发现 **WPF Application.InitializeComponent() 在 OnStartup 之前执行**，导致从非交互终端启动时 BAML 资源加载失败（FileNotFoundException）。T02 原方案在 App.OnStartup 中分叉 CLI/GUI 路径，但 InitializeComponent() 在 OnStartup 之前就被 WPF 自动生成的 Main 调用了。

**修复方案**：创建 `Program.cs` 自定义入口点，设置 `<StartupObject>DocuFiller.Program</StartupObject>` 在 csproj 中。Program.Main 检查 args.Length > 0，有参数时直接通过 `App.CreateCliServices()` 构建 ServiceProvider 并执行 CLI，完全绕过 WPF Application 初始化。无参数时才走标准 WPF 启动路径。

## 公共组件（供 S02 复用）

| 组件 | 文件 | 说明 |
|------|------|------|
| ConsoleHelper | Cli/ConsoleHelper.cs | AttachConsole(-1) P/Invoke，CLI stdout 输出基础 |
| JsonlOutput | Cli/JsonlOutput.cs | JSONL 统一 envelope（type/status/timestamp/data） |
| CliRunner | Cli/CliRunner.cs | 参数解析 + ICliCommand 分发 + 帮助输出 |
| ICliCommand | Cli/CliRunner.cs | 子命令处理器接口（CommandName + ExecuteAsync） |
| Program.cs | Program.cs | CLI/GUI 双模式入口点 |

## 验证结果

所有 9 项端到端验证通过：
1. ✅ dotnet build — 0 错误
2. ✅ dotnet test — 71/71 通过
3. ✅ --help 输出 5 行 JSONL（help + 3 子命令 + examples）
4. ✅ inspect --template 输出控件列表 JSONL + 汇总行
5. ✅ inspect（无 --template）错误 JSONL + exit code 1
6. ✅ --unknown-cmd 错误 JSONL + exit code 1
7. ✅ 所有 timestamp 为有效 ISO 8601 格式
8. ✅ 无参数启动走 GUI 路径（Program.Main 分叉）
9. ✅ 控制台日志不污染 JSONL 输出

## Verification

## 验证结果

| # | 验证项 | 命令/方法 | 结果 |
|---|--------|-----------|------|
| 1 | 编译成功 | `dotnet build` — 0 errors | ✅ pass |
| 2 | 测试回归 | `dotnet test` — 71/71 passed | ✅ pass |
| 3 | --help JSONL | `DocuFiller.exe --help` → 5 行 JSONL（help + fill + cleanup + inspect + examples） | ✅ pass |
| 4 | inspect 正常 | `DocuFiller.exe inspect --template <docx>` → 控件 JSONL + 汇总行 | ✅ pass |
| 5 | inspect 错误 | `DocuFiller.exe inspect` → 错误 JSONL + exit code 1 | ✅ pass |
| 6 | 未知命令 | `DocuFiller.exe --unknown-cmd` → 错误 JSONL + exit code 1 | ✅ pass |
| 7 | timestamp 格式 | Python 脚本验证所有 JSONL 行的 timestamp 为有效 ISO 8601 | ✅ pass |
| 8 | CLI/GUI 分叉 | Program.Main 检查 args.Length > 0 路由 CLI | ✅ pass |
| 9 | 无日志污染 | CLI 模式下 console logger 被禁用 | ✅ pass |

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

T02 原方案在 App.OnStartup 中实现 CLI/GUI 分叉，验证阶段发现 WPF InitializeComponent() 在 OnStartup 之前执行导致非交互终端中 BAML 加载失败。修复为创建 Program.cs 自定义入口点，通过 StartupObject 配置替换 WPF 自动生成的 Main，完全绕过 WPF Application 初始化进行 CLI 调度。T05 初始 verify 字段为中文描述而非可执行命令，已手动执行 9 项验证。

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

- `Program.cs` — 新建 — 自定义 Main 入口点，CLI/GUI 双模式分叉
- `App.xaml.cs` — 修改 — 移除 CLI 分叉逻辑（改由 Program.cs 处理），添加 CreateCliServices() 和 BuildServiceProvider()
- `DocuFiller.csproj` — 修改 — 添加 StartupObject=DocuFiller.Program
- `Utils/LoggerConfiguration.cs` — 修改 — CreateLoggerFactory 添加 enableConsole 参数
- `Cli/ConsoleHelper.cs` — 新建 — WinExe P/Invoke 附加父控制台
- `Cli/JsonlOutput.cs` — 新建 — JSONL 统一 envelope 序列化
- `Cli/CliRunner.cs` — 新建 — 参数解析、命令分发、ICliCommand 接口定义
- `Cli/Commands/InspectCommand.cs` — 新建 — inspect 子命令处理器
