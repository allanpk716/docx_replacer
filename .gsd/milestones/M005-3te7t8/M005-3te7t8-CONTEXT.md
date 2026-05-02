---
depends_on: [M006-rj9bue]
---

# M005-3te7t8: CLI 接口 — LLM Agent 集成

**Gathered:** 2026-04-23
**Status:** Queued — pending auto-mode execution

## Project Description

为 DocuFiller 新增命令行接口（CLI），让第三方 LLM agent 能无需 GUI 界面直接调用核心功能。CLI 全部使用 JSONL 格式输出（包括 `--help`），专为机器消费设计。三个子命令覆盖完整工作流：`fill`（Excel 数据批量填充模板）、`cleanup`（清理批注和内容控件）、`inspect`（查询模板控件信息）。

## Why This Milestone

LLM agent 无法操作 WPF GUI 界面，但 DocuFiller 的核心能力（文档填充、清理、控件查询）对 agent 自动化场景有明确价值。通过暴露 CLI 接口，agent 可以：
1. 先 `inspect` 了解模板有哪些需要填充的关键词
2. 准备 Excel 数据后 `fill` 批量生成文档
3. 用 `cleanup` 清理生成文档中的批注和控件痕迹

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在命令行执行 `DocuFiller.exe --help` 获取 JSONL 格式的完整参数说明和使用示例
- 执行 `DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output` 批量填充文档
- 执行 `DocuFiller.exe cleanup --input ./docs` 批量清理文档
- 执行 `DocuFiller.exe inspect --template report.docx` 查看模板中的内容控件列表
- LLM agent 通过调用这些命令实现文档处理的完整自动化

### Entry point / environment

- Entry point: `DocuFiller.exe <command> [options]`（命令行）
- Environment: Windows 桌面应用 + 命令行终端
- 无参数时正常启动 WPF GUI，有参数时走 CLI 路径不弹窗

## Completion Class

- Contract complete means: 三个子命令（fill/cleanup/inspect）均可用，`--help` 输出完整 JSONL 参数文档
- Integration complete means: 所有子命令通过已有的 DI 服务执行，与 GUI 共享同一处理逻辑
- Operational complete means: `dotnet build` 编译成功，所有现有测试通过，新增 CLI 测试通过

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- `DocuFiller.exe --help` 输出 JSONL 格式的完整帮助文档，包含三个子命令的参数和使用示例
- `DocuFiller.exe fill --template <path> --data <xlsx> --output <dir>` 成功生成填充后的文档
- `DocuFiller.exe inspect --template <path>` 输出模板控件列表 JSONL
- `DocuFiller.exe cleanup --input <path>` 成功清理文档
- `dotnet test` 全部通过
- 无参数启动时 WPF GUI 正常工作

## Architectural Decisions

### CLI 入口点：App.OnStartup 参数检测

**Decision:** 在 `App.xaml.cs` 的 `OnStartup` 中检测 `StartupEventArgs.Args`，有参数时走 CLI 路径直接 `Shutdown(exitCode)`，无参数时正常启动 GUI

**Rationale:** 最小化改动，不需要修改 csproj 的 `OutputType` 或引入自定义 `Main`。WPF 应用的 `OnStartup` 天然能拿到命令行参数。

**Alternatives Considered:**
- 新建 `Program.cs` 自定义 Main 入口 — 需要改 csproj 的 `BuildStartupObject` 或 `OutputType`，且 WPF 应用自定义 Main 有额外复杂度
- 独立可执行文件项目 — 增加构建和维护复杂度，且不能共享 DI 容器

### 全 JSONL 输出，面向 LLM agent

**Decision:** 所有 CLI 输出（包括 `--help`）均使用 JSONL 格式，每行一个 JSON 对象

**Rationale:** 目标用户是 LLM agent，不是人类。JSONL 格式易于 agent 解析，`--help` 输出 JSONL 让 agent 可以通过调用 `--help` 获取完整的参数说明和使用示例，无需查阅外部文档。

**Alternatives Considered:**
- 人类可读文本 + `--json` 参数切换 — 增加双模式维护成本，且人类不是主要受众
- 纯 JSON（非 JSONL）— 多条结果需要数组包裹，JSONL 更适合流式输出和逐行解析

### 统一 stdout 输出

**Decision:** 所有输出统一走 stdout，不分离 stderr

**Rationale:** 用户明确选择统一 stdout。简化实现，agent 只需读取一个流。

## Error Handling Strategy

- 命令行参数验证失败 → 输出错误 JSONL + exit code 1
- 文件不存在或格式错误 → 输出错误 JSONL（含详细错误信息）+ exit code 1
- 处理过程中的异常 → 每个失败的文件/记录输出错误 JSONL 行，最后输出汇总行
- exit code: 0 = 全部成功, 1 = 参数错误或部分/全部失败

## Risks and Unknowns

- **WinExe stdout 问题**: csproj 的 `OutputType` 是 `WinExe`，Windows 下 WinExe 默认没有控制台窗口，stdout 不会输出到调用终端。需要 `AllocConsole` + `AttachConsole(-1)` 或将 `OutputType` 改为 `Exe`。改为 `Exe` 会带来启动时的控制台闪屏问题（GUI 模式），需要权衡。 — 这是关键技术风险，需要验证
- **M004 后构造函数变更**: M004 会移除 `IDataParser` 依赖，`DocumentProcessorService` 构造函数将变更。CLI 代码必须在 M004 完成后的代码基础上开发
- **JSONL 格式设计**: 需要仔细设计每条 JSONL 的 schema，确保 agent 能可靠解析。不同子命令的输出结构需要保持一致性

## Existing Codebase / Prior Art

- `App.xaml.cs` — WPF 应用入口，DI 容器配置。CLI 入口点将在此文件中添加参数检测逻辑
- `Services/DocumentProcessorService.cs` — 核心处理管道，提供 `ProcessDocumentsAsync`/`ProcessFolderAsync`/`GetContentControlsAsync`/`ValidateTemplateAsync` 四个核心方法（verified against current codebase state）
- `Services/ExcelDataParserService.cs` — Excel 数据解析，提供 `ParseExcelFileAsync`/`ValidateExcelFileAsync`/`GetDataPreviewAsync`/`GetDataStatisticsAsync`（verified against IExcelDataParser interface）
- `DocuFiller/Services/DocumentCleanupService.cs` — 文档清理，提供 `CleanupAsync` 三种重载：单文件路径、CleanupFileItem、CleanupFileItem+输出目录（verified against current codebase state）
- `Models/ProcessRequest.cs` — 单文件处理请求模型，含 TemplateFilePath/DataFilePath/OutputDirectory/OutputFileNamePattern/OverwriteExisting
- `Models/FolderProcessRequest.cs` — 文件夹批量处理请求模型，含 TemplateFolderPath/DataFilePath/OutputDirectory/IncludeSubfolders/PreserveDirectoryStructure/CreateTimestampFolder/OutputFileNamePattern/OverwriteExistingFiles/SelectedFiles
- `Models/ProcessResult.cs` — 处理结果模型，含 IsSuccess/TotalRecords/SuccessfulRecords/GeneratedFiles/Errors/Warnings/Message
- `Models/ContentControlData.cs` — 内容控件模型，含 Tag/Title/Value/Type/Location/IsRequired/DefaultValue
- `Models/CleanupResult.cs` (在 IDocumentCleanupService.cs 中) — 清理结果，含 Success/Message/CommentsRemoved/ControlsUnwrapped/FilePath/OutputFilePath/OutputFolderPath
- `DocuFiller.csproj` — 当前 OutputType 为 WinExe + UseWPF

## Relevant Requirements

- 需在 M004 完成后创建新需求

## Scope

### In Scope

- `fill` 子命令：单模板+Excel 批量填充，文件夹批量填充（对齐 GUI 功能）
- `cleanup` 子命令：单文件清理，文件夹批量清理（对齐 GUI 功能）
- `inspect` 子命令：查询模板中的内容控件列表（tag、title、type、location）
- `--help` 输出 JSONL 格式的完整参数说明和使用示例
- JSONL 输出格式设计（统一的 envelope schema）
- CLI 入口点实现（App.OnStartup 参数检测）
- 解决 WinExe stdout 输出问题
- CLI 单元测试

### Out of Scope / Non-Goals

- 不修改 GUI 功能或界面
- 不修改核心服务逻辑（CLI 调用现有服务）
- 不做交互式 CLI（不需要进度条、确认提示等）
- 不做 daemon/server 模式
- 不做配置文件支持（所有参数通过命令行传递）

## Technical Constraints

- Windows 环境，.NET 8 + WPF
- 同一 exe 文件，CLI 和 GUI 共存
- 依赖 M004 完成后的代码库（Excel 为唯一数据源）
- `dotnet build` 编译成功
- `dotnet test` 全部通过
- JSONL 格式输出（每行一个 JSON 对象）

## Integration Points

- `IDocumentProcessor` — fill 和 inspect 子命令的主要依赖
- `IExcelDataParser` — fill 子命令的数据解析依赖
- `IDocumentCleanupService` — cleanup 子命令的依赖
- `App.xaml.cs` DI 容器 — CLI 需要从同一 DI 容器解析服务
- `ProcessRequest` / `FolderProcessRequest` — fill 子命令的请求模型
- `CleanupFileItem` — cleanup 子命令的文件模型

## Testing Requirements

- CLI 参数解析测试（有效/无效参数、必需参数缺失）
- fill 子命令端到端测试（使用测试模板和 Excel 数据）
- inspect 子命令测试（验证输出包含正确的控件信息）
- cleanup 子命令测试（验证批注和控件被正确清理）
- JSONL 输出格式验证（每行可解析为有效 JSON）
- `--help` 输出包含三个子命令的完整说明
- GUI 模式不受影响（无参数启动正常）

## Acceptance Criteria

- S01: CLI 框架搭建完成，`--help` 输出 JSONL 格式帮助，`fill`/`inspect` 子命令可用
- S02: `cleanup` 子命令可用，三个子命令均支持单文件和文件夹模式
- S03: 测试全部通过，文档更新完成

## Open Questions

- WinExe stdout 问题的具体解决方案需要技术验证（AllocConsole vs OutputType 改 Exe + 闪屏抑制）
