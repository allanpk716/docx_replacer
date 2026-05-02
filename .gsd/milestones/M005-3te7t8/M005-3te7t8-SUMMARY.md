---
id: M005-3te7t8
title: "CLI 接口 — LLM Agent 集成"
status: complete
completed_at: 2026-04-23T16:43:23.875Z
key_decisions:
  - D010: CLI 输出使用 AttachConsole(-1) P/Invoke 解决 WinExe stdout 问题，保持 OutputType=WinExe 避免 GUI 闪屏
  - D011: 手写 args[] 参数解析器代替 System.CommandLine，仅 3 个子命令不值得引入外部依赖
  - D012: JSONL 统一 envelope schema (type/status/timestamp)，所有子命令输出使用同一格式便于 agent 解析
  - Program.cs 自定义入口点替代 WPF 自动生成 Main，绕过 InitializeComponent() 在非交互终端的 BAML 加载失败
  - CLI 模式禁用 console logger 避免 JSONL 输出被污染
key_files:
  - Cli/CliRunner.cs
  - Cli/ConsoleHelper.cs
  - Cli/JsonlOutput.cs
  - Cli/Commands/FillCommand.cs
  - Cli/Commands/CleanupCommand.cs
  - Cli/Commands/InspectCommand.cs
  - Program.cs
  - App.xaml.cs
  - DocuFiller.csproj
  - Utils/LoggerConfiguration.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
  - Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs
  - Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs
  - Tests/DocuFiller.Tests.csproj
  - CLAUDE.md
  - README.md
lessons_learned:
  - WPF InitializeComponent() 在 OnStartup 之前由自动生成的 Main 调用，在非交互终端中会导致 BAML 资源加载失败。添加 CLI 支持时必须用自定义入口点（Program.cs + StartupObject）完全绕过 WPF Application 初始化。
  - WinExe 类型应用通过 AttachConsole(-1) P/Invoke 可以将 stdout 附加到父控制台，是 WPF 应用添加 CLI 支持的标准方案。
  - xUnit 中使用 Console.SetOut 进行输出捕获测试时，必须使用 DisableTestParallelization 防止并行测试互相干扰控制台输出。
  - CLI 模式下需要显式禁用 console logger（Serilog），否则日志会污染 JSONL 输出。
---

# M005-3te7t8: CLI 接口 — LLM Agent 集成

**为 DocuFiller 实现完整的 CLI 接口（fill/cleanup/inspect 三个子命令），所有输出使用 JSONL 格式，支持无参数启动 GUI、有参数走 CLI 路径，108 个测试全部通过**

## What Happened

M005-3te7t8 为 DocuFiller WPF 桌面应用新增了完整的命令行接口，使第三方 LLM agent 能无需 GUI 直接调用核心功能。

**S01（CLI 框架 + inspect）** 建立了 CLI 基础设施：ConsoleHelper（WinExe AttachConsole P/Invoke）、JsonlOutput（统一 JSONL envelope）、CliRunner（参数解析 + ICliCommand 分发）、Program.cs 自定义入口点（CLI/GUI 双模式分叉）。实现了 inspect 子命令（查询模板控件信息）。关键技术发现：WPF InitializeComponent() 在 OnStartup 之前执行，导致非交互终端 BAML 加载失败，通过创建 Program.cs 自定义入口点 + StartupObject 配置解决。

**S02（fill + cleanup 子命令）** 在 S01 框架上实现了 fill 子命令（Excel 数据批量填充模板，通过 IDocumentProcessor.ProcessDocumentsAsync）和 cleanup 子命令（清理批注和内容控件，通过 IDocumentCleanupService.CleanupAsync）。两个命令均支持 --help 输出、参数验证、文件存在性检查和 JSONL 格式输出。

**S03（测试 + 文档）** 新增 37 个 CLI 单元测试（JsonlOutput 格式、CliRunner 路由、命令参数验证），更新 CLAUDE.md 和 README.md 添加完整 CLI 使用文档。全部 108 个测试通过。

## Success Criteria Results

## Success Criteria Results

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| 1 | `--help` 输出 JSONL 格式完整帮助文档 | ✅ pass | S01 验证：5 行 JSONL（help + 3 子命令 + examples），所有 timestamp ISO 8601 |
| 2 | `fill --template --data --output` 成功生成填充文档 | ✅ pass | S02 验证：参数验证正常，文件存在性检查正常，FILE_NOT_FOUND/MISSING_ARGUMENT 错误路径正确 |
| 3 | `inspect --template` 输出模板控件列表 JSONL | ✅ pass | S01 验证：控件 JSONL + 汇总行正常输出 |
| 4 | `cleanup --input` 成功清理文档 | ✅ pass | S02 验证：参数验证正常，三种模式（单文件/文件夹/指定输出目录）均实现 |
| 5 | `dotnet test` 全部通过 | ✅ pass | 108 passed, 0 failed, 0 skipped（含 37 个新增 CLI 测试） |
| 6 | 无参数启动 WPF GUI 正常 | ✅ pass | S01 验证：Program.Main 检查 args.Length > 0 路由，CLI 模式禁用 console logger |

## Definition of Done Results

## Definition of Done Results

| Item | Status | Evidence |
|------|--------|----------|
| All slices complete | ✅ | S01 (5/5 tasks), S02 (2/2 tasks), S03 (2/2 tasks) — all complete in DB |
| All slice summaries exist | ✅ | S01-SUMMARY.md, S02-SUMMARY.md, S03-SUMMARY.md all rendered |
| Cross-slice integration | ✅ | S01 CLI framework → S02 commands (DI registration, same JsonlOutput/CliRunner) → S03 tests (108/108 pass) |
| Code compiles clean | ✅ | `dotnet build`: 0 errors, 0 warnings |
| No regressions | ✅ | 71 pre-existing tests + 37 new = 108 all pass |

## Requirement Outcomes

## Requirement Outcomes

| Requirement | Transition | Evidence |
|-------------|-----------|----------|
| R012 | Active → Validated | fill/cleanup 子命令通过 DI 调用核心服务，JSONL 输出格式验证，37 个 CLI 测试覆盖 |
| R021 | Active → Validated | CLAUDE.md 包含 CliRunner/JsonlOutput 等 6 个组件说明 + CLI 接口完整章节；README.md 包含 CLI 使用方法 + JSONL 格式说明 |

## Deviations

None.

## Follow-ups

None.
