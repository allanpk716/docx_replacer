# DocuFiller

## What This Is

DocuFiller 是一个 .NET 8 + WPF 桌面应用，提供 Word 文档批量填充、富文本替换、批注追踪和审核清理功能。支持 GUI（WPF）和 CLI 双模式运行。

## Core Value

根据 Excel 数据自动填充 Word 模板中的内容控件，保留格式和表格结构。

## Project Shape

- **Complexity:** complex
- **Why:** MVVM 协调器+子 ViewModel 架构，11 个服务接口，CLI/GUI 双模式，Velopack 自动更新双源

## Current State

已发布至 v1.8.0，包含完整的文档填充、清理、CLI 和自动更新功能。M020 完成了第一轮代码质量清理。M021 进行第二轮重构：MainWindowViewModel 全面拆分为协调器+子 ViewModel，清理 Tab 复用 CleanupViewModel，拖放逻辑提取为 Behavior，CleanupViewModel 迁移 CT.Mvvm，R028 自动检查更新，CLAUDE.md 删除，文档同步。

## Architecture / Key Patterns

- **框架**：.NET 8 + WPF，MVVM 模式 + DI（Microsoft.Extensions.DependencyInjection）
- **ViewModel 架构**：协调器 + 子 ViewModel 模式；子 VM 使用 CommunityToolkit.Mvvm 8.4 源代码生成器
- **文档处理**：DocumentFormat.OpenXml SDK 操作 Word 文档
- **数据源**：EPPlus 解析 Excel（两列/三列自动检测）
- **CLI**：Program.cs 双模式入口，JSONL 输出
- **自动更新**：Velopack SDK，双源（内网 Go 服务器 + GitHub Releases）
- **拖放**：DragDropBehavior AttachedProperty 统一处理文件拖放

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001–M019: 已完成的历史里程碑
- [x] M020: 代码质量与技术债清理（5/5 slices 完成）
- [ ] M021: 第二轮重构 — ViewModel 拆分 + 拖放 Behavior + R028 自动更新 + 文档同步
