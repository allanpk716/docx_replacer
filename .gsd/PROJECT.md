# DocuFiller

## What This Is

DocuFiller 是一款基于 .NET 8 + WPF 的 Windows 桌面应用，通过 JSON 或 Excel 数据文件批量填充 Word 文档模板中的内容控件。支持富文本格式保留（上标、下标）、页眉页脚替换、批注追踪、审核清理（去除控件和批注）、JSON↔Excel 转换等功能。

## Core Value

用户选择模板 + 数据源 → 一键批量生成格式正确的 Word 文档。内容控件精确定位替换，不破坏文档结构。

## Current State

已完成三个里程碑：
- **M001-a60bo7**: Excel 三列格式支持（ID|关键词|值），自动格式检测，ID 唯一性校验
- **M002-ahlnua**: 代码质量清理（ILogger 注入、OpenFolderDialog 替换、Console.WriteLine 清除）
- **M003-g1w88x**: 文档全面更新（7 份文档对齐代码库、.trae/documents/ 迁移清理）

功能完整可用：单文件/文件夹批量处理、JSON/Excel 双数据源、两列/三列 Excel 格式、富文本格式保留、页眉页脚内容控件、批注变更追踪、审核清理、JSON↔Excel 转换工具。71 项测试全部通过。文档体系完整（产品需求、技术架构、README、CLAUDE.md、Excel 指南、页眉页脚、批注说明）。

## Architecture / Key Patterns

- **框架**: .NET 8 + WPF，MVVM 模式
- **文档处理**: DocumentFormat.OpenXml（SafeTextReplacer 三种替换策略处理表格控件）
- **Excel 处理**: EPPlus 7.5.2（ExcelDataParserService 自动检测两列/三列格式）
- **JSON 处理**: Newtonsoft.Json
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **服务层**: 15 个服务接口，覆盖文档处理、数据解析、Excel 转换、关键词验证、格式化替换、模板缓存、文档清理等
- **主界面**: 三个 Tab（关键词替换、审核清理、工具）

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001-a60bo7: Excel 行 ID 列支持 — 三列 Excel 格式自动检测与 ID 唯一性校验
- [x] M002-ahlnua: 代码质量清理 — ILogger 注入、调试日志清除、OpenFolderDialog 替换
- [x] M003-g1w88x: 文档全面更新 — 7 份文档对齐代码库当前状态
  - [x] S01: 产品需求 + 技术架构文档重写与迁移（R005/R006/R011 已验证）
  - [x] S02: README.md + CLAUDE.md 更新（R007/R008 已验证）
  - [x] S03: 功能级文档更新（R009/R010 已验证）
