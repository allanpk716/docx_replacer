# DocuFiller

## What This Is

DocuFiller 是一个 .NET 8 + WPF 桌面应用，提供 Word 文档批量填充和审核清理功能。支持 GUI（WPF）和 CLI 双模式运行。

## Core Value

用 Excel 数据批量填充 Word 模板中的内容控件，保留富文本格式和表格结构，并在小屏幕上也能完整操作。

## Current State

已完成的核心功能：Excel 两列/三列格式自动检测、关键词替换（正文/页眉/页脚）、富文本格式保留、批注追踪、审核清理（批注清除+内容控件解包）、CLI 模式（fill/cleanup/inspect）、在线更新（Velopack + 内网 Go 服务器 + GitHub Releases 双通道）。主界面紧凑化至 900x550，在 1366x768 下无需滚动即可完整操作。

## Architecture / Key Patterns

- MVVM 模式 + 依赖注入 + 分层架构
- MainWindow.xaml 承载两个 Tab（关键词替换、审核清理），窗口 900x550
- TabItem 内使用 DockPanel 包裹结构（Grid DockPanel.Dock=Top + 底部按钮）
- 拖放通过 TextBox AllowDrop + Window 级 PreviewDragOver Activate() 实现
- 10 个服务接口 + 2 个处理器，大部分 Singleton
- CLI 通过 Program.cs 入口分流，JSONL 输出格式
- 配置使用 appsettings.json + IOptions<T>

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M012-li0ip5: 主界面布局紧凑化与拖放修复 — 窗口从 1400x900 降至 900x550，GroupBox 全移除，拖放迁移到 TextBox AllowDrop，窗口未聚焦拖放通过 Window 级激活修复
