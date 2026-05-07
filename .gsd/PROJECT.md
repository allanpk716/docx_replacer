# DocuFiller

## What This Is

DocuFiller 是一个 .NET 8 + WPF 桌面应用，提供 Word 文档批量填充、富文本替换、批注追踪和审核清理功能。支持 GUI（WPF）和 CLI 双模式运行。

## Core Value

根据 Excel 数据自动填充 Word 模板中的内容控件，保留格式和表格结构。

## Project Shape

- **Complexity:** complex
- **Why:** MVVM 协调器+子 ViewModel 架构，11 个服务接口，CLI/GUI 双模式，Velopack 自动更新双源

## Current State

已发布至 v1.10.1，包含完整的文档填充、清理、CLI 和自动更新功能。M020 完成第一轮代码质量清理。M021 完成第二轮重构（ViewModel 拆分 + 拖放 Behavior + R028 自动更新 + 文档同步）。M022 完成跨平台技术调研：6 个 UI 方案调研（含 Electron.NET 和 Tauri 两个可编译 PoC）、4 份基础设施调研、最终对比评估推荐 Avalonia UI 为迁移首选方案。M023 完成 update-hub 独立项目：将嵌入的 Go 更新服务器抽取为通用内网自更新平台（Go + Vue 3 + SQLite），支持多应用、多平台、动态通道，带 Web 管理界面和 NSSM 部署。M024 完成启动时更新检查进度动画：状态栏启动瞬间显示旋转 spinner，覆盖 5 秒延迟和整个更新检查过程。

## Architecture / Key Patterns

- **框架**：.NET 8 + WPF，MVVM 模式 + DI（Microsoft.Extensions.DependencyInjection）
- **ViewModel 架构**：协调器 + 子 ViewModel 模式；子 VM 使用 CommunityToolkit.Mvvm 8.4 源代码生成器
- **文档处理**：DocumentFormat.OpenXml SDK 操作 Word 文档
- **数据源**：EPPlus 解析 Excel（两列/三列自动检测）
- **CLI**：Program.cs 双模式入口，JSONL 输出
- **自动更新**：Velopack SDK，双源（update-hub 内网服务器 + GitHub Releases）
- **拖放**：DragDropBehavior AttachedProperty 统一处理文件拖放
- **更新服务器**：update-hub（独立 Go 项目），Go 1.22 + Vue 3 + SQLite，多应用 Velopack 分发
- **启动动画**：Canvas + Ellipse 旋转 spinner，DataTrigger + Storyboard 控制，计算属性聚合状态标志

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001–M019: 已完成的历史里程碑
- [x] M020: 代码质量与技术债清理（5/5 slices 完成）
- [x] M021: 第二轮重构 — ViewModel 拆分 + 拖放 Behavior + R028 自动更新 + 文档同步
- [x] M022: 跨平台技术调研 — Electron.NET/Tauri PoC + 6 方案文献调研 + 基础设施评估
- [x] M023: update-hub 独立项目 — Go + Vue 3 + SQLite 多应用 Velopack 更新平台，Web 管理界面，数据迁移，NSSM 部署
- [x] M024: 启动时更新检查进度动画 — 状态栏旋转 spinner，消除 5 秒无反馈等待期
