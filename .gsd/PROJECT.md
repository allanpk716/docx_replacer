# DocuFiller

## What This Is

DocuFiller 是一个 .NET 8 + WPF 桌面应用，提供 Word 文档批量填充和审核清理功能。支持 GUI（WPF）和 CLI 双模式运行。

## Core Value

用 Excel 数据批量填充 Word 模板中的内容控件，保留富文本格式和表格结构，并在小屏幕上也能完整操作。

## Current State

已完成的核心功能：Excel 两列/三列格式自动检测、关键词替换（正文+页眉+页脚）、富文本格式保留、批注追踪、审核清理（批注清除+内容控件解包）、CLI 模式（fill/cleanup/inspect）、在线更新（Velopack + 内网 Go 服务器 + GitHub CDN 直连双通道）、主界面紧凑化至 900x550、窗口置顶开关（WindowChrome 自定义标题栏）、拖放提示文字、TextBox 拖放修复（Preview 隧道事件绕过内置拦截）。配置持久化已修复（~/.docx_replacer/update-config.json）。更新检测已修复：移除 ExplicitChannel 使 Velopack 正确查找 releases.win.json。GitHub 更新模式已从 GithubSource（API，60次/小时 rate limit）切换为 SimpleWebSource（CDN 直连，无 rate limit）。

## Architecture / Key Patterns

- MVVM 模式 + 依赖注入 + 分层架构
- MainWindow.xaml 使用 WindowChrome 自定义标题栏（WindowStyle=None），含图钉/最小化/关闭按钮
- 图钉按钮通过 ViewModel IsTopmost 属性 + ToggleTopmostCommand 切换 Window.Topmost
- TabItem 内使用 DockPanel 包裹结构（Grid DockPanel.Dock=Top + 底部按钮）
- 拖放通过 TextBox Preview 隧道事件（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave）+ Window 级 PreviewDragOver Activate() 实现；清理区域 Border 使用冒泡事件
- 10 个服务接口 + 2 个处理器，大部分 Singleton
- CLI 通过 Program.cs 入口分流，JSONL 输出格式
- 配置使用 appsettings.json + IOptions<T>
- 更新源配置持久化到 ~/.docx_replacer/update-config.json（独立于安装目录）

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M012-li0ip5: 主界面布局紧凑化与拖放修复 — 窗口从 1400x900 降至 900x550，GroupBox 全移除，拖放迁移到 TextBox AllowDrop，窗口未聚焦拖放通过 Window 级激活修复
- [x] M013-ueix00: 更新配置持久化路径修复 — 将 update-config.json 从 Velopack 安装目录迁移到 ~/.docx_replacer/，彻底隔离安装/更新生命周期
- [x] M014-jkpyu6: ExplicitChannel 导致 GitHub 更新检测失败 — 去掉 ExplicitChannel 使客户端查找 releases.win.json，修正内网 HTTP 模式 beta→stable 回退逻辑
- [x] M015: GitHub 更新源从 API 切换到 CDN 直连 — 将 GithubSource（GitHub API，60次/小时 rate limit）替换为 SimpleWebSource（CDN 直连 /releases/latest/download/，无 rate limit），统一两种源的底层实现
- [x] M016: 窗口置顶开关 + 拖放提示 — WindowChrome 自定义标题栏含图钉按钮切换 Topmost，关键词替换 tab TextBox 下方拖放提示文字
- [x] M017: 修复 TextBox 拖放被拦截 — 模板和数据 TextBox 的冒泡拖放事件改为 Preview 隧道版本，绕过 TextBox 内置拖放拦截，清理区域保持不变
