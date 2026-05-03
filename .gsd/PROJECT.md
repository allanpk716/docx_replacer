# DocuFiller

## What This Is

DocuFiller 是一个 .NET 8 + WPF 桌面应用，提供 Word 文档批量填充、富文本替换、批注追踪和审核清理功能。支持 GUI（WPF）和 CLI 双模式运行。

## Core Value

根据 Excel 数据自动填充 Word 模板中的内容控件，保留格式和表格结构。

## Current State

已发布至 v1.8.0，包含完整的文档填充、清理、CLI 和自动更新（Velopack + 内网 Go 服务器 + GitHub Releases）功能。

## Architecture / Key Patterns

- **框架**：.NET 8 + WPF，MVVM 模式 + DI（Microsoft.Extensions.DependencyInjection）
- **文档处理**：DocumentFormat.OpenXml SDK 操作 Word 文档
- **数据源**：EPPlus 解析 Excel（两列/三列自动检测）
- **CLI**：Program.cs 双模式入口，JSONL 输出
- **自动更新**：Velopack SDK，双源（内网 Go 服务器 + GitHub Releases）
- **窗口样式**：WindowChrome 自定义标题栏，无系统边框
- **进度条**：ModernProgressBarStyle 模板包含 PART_Track + PART_Indicator 双命名元素，符合 WPF 标准
- **应用图标**：所有窗口和 exe 使用统一的 DocuFiller 专属图标（pack URI 引用），csproj ApplicationIcon 嵌入 exe 资源

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001–M019: 已完成的历史里程碑
