# DocuFiller

## What This Is

DocuFiller 是一款基于 .NET 8 + WPF 的 Windows 桌面应用，通过 Excel 数据文件批量填充 Word 文档模板中的内容控件。支持富文本格式保留（上标、下标）、页眉页脚替换、批注追踪、审核清理（去除控件和批注）。提供 CLI 接口（JSONL 输出）供 LLM Agent 集成调用。

## Core Value

用户选择模板 + Excel 数据源 → 一键批量生成格式正确的 Word 文档。内容控件精确定位替换，不破坏文档结构。CLI 模式支持第三方 LLM agent 无需 GUI 直接调用核心功能。

## Current State

已完成五个里程碑：
- **M001-a60bo7**: Excel 三列格式支持（ID|关键词|值），自动格式检测，ID 唯一性校验
- **M002-ahlnua**: 代码质量清理（ILogger 注入、OpenFolderDialog 替换、Console.WriteLine 清除）
- **M003-g1w88x**: 文档全面更新（7 份文档对齐代码库、.trae/documents/ 迁移清理）
- **M004-l08k3s**: 功能瘦身 — 移除在线更新（19文件）、JSON编辑器遗留（9文件）、JSON数据源（IDataParser）、转换器窗口（5文件）、KeywordEditorUrl、Tools目录（10项目）、Newtonsoft.Json依赖，Excel成为唯一数据源，文档全部同步
- **M005-3te7t8**: CLI 接口 — 新增命令行接口（CliRunner + 3 子命令），JSONL 格式输出，37 个 CLI 单元测试，文档更新

## Architecture / Key Patterns

- **框架**: .NET 8 + WPF，MVVM 模式
- **文档处理**: DocumentFormat.OpenXml（SafeTextReplacer 三种替换策略处理表格控件）
- **Excel 处理**: EPPlus 7.5.2（ExcelDataParserService 自动检测两列/三列格式）
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **CLI 接口**: CliRunner 参数解析 + fill/cleanup/inspect 子命令，JsonlOutput 统一 envelope 输出，ConsoleHelper P/Invoke 控制台管理
- **服务层**: 10+ 个服务接口覆盖文档处理、Excel 数据解析、格式化替换、文档清理等核心功能
- **主界面**: 两个 Tab（关键词替换、审核清理）
- **数据源**: 仅 Excel（.xlsx）
- **双模式启动**: 无参数→WPF GUI，有参数→CLI（JSONL 输出）

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001-a60bo7: Excel 行 ID 列支持 — 三列 Excel 格式自动检测与 ID 唯一性校验
- [x] M002-ahlnua: 代码质量清理 — ILogger 注入、调试日志清除、OpenFolderDialog 替换
- [x] M003-g1w88x: 文档全面更新 — 7 份文档对齐代码库当前状态
- [x] M004-l08k3s: 功能瘦身 — 移除在线更新、JSON 相关、转换器、Tools 等不活跃模块
- [x] M005-3te7t8: CLI 接口 — LLM Agent 集成（fill/cleanup/inspect 子命令，JSONL 输出）
  - [x] S01: CliRunner 框架 + inspect 子命令
  - [x] S02: fill/cleanup 子命令 + 帮助系统
  - [x] S03: CLI 单元测试 + 文档更新
