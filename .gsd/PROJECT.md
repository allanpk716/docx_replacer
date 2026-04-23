# DocuFiller

## What This Is

DocuFiller 是一个 WPF 桌面应用，基于 .NET 8，使用 OpenXML SDK 批量替换 Word 文档中的内容控件。支持 JSON 和 Excel 两种数据源格式，提供文件夹批量处理、内容控件扫描、文档清理等功能。

## Core Value

准确、可靠地将 Excel/JSON 数据映射到 Word 模板内容控件，保持文档格式完整。

## Current State

已发布，核心功能完整：Excel 和 JSON 数据解析、Word 模板内容控件替换、文件夹批量处理、文档清理、自动更新。Excel 数据源支持两列格式（关键词 | 值）和三列格式（ID | 关键词 | 值），自动检测格式并校验 ID 唯一性。

## Architecture / Key Patterns

- MVVM 模式，DI 注入服务
- 服务层：`ExcelDataParserService`（Excel 解析）、`DataParserService`（JSON 解析）、`DocumentProcessorService`（文档处理）
- EPPlus 库读写 Excel，OpenXML SDK 处理 Word
- Excel 解析返回 `Dictionary<string, FormattedCellValue>`，关键词格式 `#xxx#`
- 格式自动检测：`DetectExcelFormat` 私有方法，通过首行首列匹配 `#xxx#` 判断两列/三列模式

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001-a60bo7: Excel 行 ID 列支持 ✅ — 三列格式（ID | 关键词 | 值）自动检测、ID 唯一性校验、完全向后兼容、71 tests pass
  - [x] S01: 三列格式解析与 ID 唯一性校验 (R001, R002, R003 validated)
  - [x] S02: 测试覆盖验证 (R004 validated, 71 tests pass)
