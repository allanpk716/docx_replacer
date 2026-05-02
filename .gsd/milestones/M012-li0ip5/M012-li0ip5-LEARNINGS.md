---
phase: complete
phase_name: milestone-completion
project: DocuFiller
generated: "2026-05-02T01:45:00Z"
counts:
  decisions: 4
  lessons: 3
  patterns: 3
  surprises: 1
missing_artifacts: []
---

### Decisions

- DockPanel 包裹 TabItem 内容（Grid DockPanel.Dock=Top + 底部按钮）解决 TabItem 单子元素 XAML 约束。TabItem 只允许一个直接子元素，多个内容区域（表单+按钮）需要容器包裹。DockPanel 让 Grid 占据顶部剩余空间，按钮自然停靠底部。
  Source: S01-SUMMARY.md/Key Decisions

- 拖放区域从独立 Border（70-80px 高）迁移到路径 TextBox AllowDrop。两个拖放 Border 共占约 150px 垂直空间，改为单行 TextBox 可大幅节省。TextBox AllowDrop 是 WPF 标准做法。
  Source: M012-li0ip5-CONTEXT.md/Architectural Decisions

- 窗口尺寸从 1400x900 降至 900x550（MinWidth=800 MinHeight=500）。紧凑布局后内容不需要大窗口，900x550 在 1366x768（减任务栏约 728px）下充裕显示。
  Source: M012-li0ip5-CONTEXT.md/Architectural Decisions

- Window 级 AllowDrop + PreviewDragOver Activate() 修复未聚焦拖放。WPF 拖放依赖 COM OLE 注册，窗口非前台时子控件 AllowDrop 不触发。Window 级处理确保 OLE 管道始终激活。
  Source: M012-li0ip5-CONTEXT.md/Architectural Decisions

### Lessons

- WPF TabItem 只允许一个直接子元素，布局多个区域时必须用 DockPanel/StackPanel/Grid 等容器包裹。计划时假设"操作按钮和内容用同一容器"忽略了此 XAML 约束，实际需 DockPanel 解决。
  Source: S01-SUMMARY.md/Deviations

- WPF 拖放在窗口未聚焦时需要 Window 级 AllowDrop + Activate() 注册 OLE 拖放目标。PreviewDragOver 隧道事件需设 e.Handled=false 才能让子控件也接收到事件。
  Source: S02-SUMMARY.md/What Happened

- 紧凑化 UI 时全局样式调整不可遗漏。App.xaml 中的 ModernTextBoxStyle Padding、按钮 Padding、HeaderLabelStyle FontSize 等全局样式都会影响实际空间占用，必须与控件级调整同步进行。
  Source: S01-SUMMARY.md/What Happened

### Patterns

- TabItem 内容布局模式：DockPanel → Grid(DockPanel.Dock=Top) + bottom Grid。Grid 承载表单内容占满剩余空间，底部 Grid 承载操作按钮。这是解决 TabItem 单子元素约束的标准模式。
  Source: S01-SUMMARY.md/Patterns Established

- 字号分层规范：TabControl 标题 14px、标签 13px、正文和按钮 12px。适用于紧凑化 WPF 界面，在保持可读性的同时最大化内容密度。
  Source: S01-SUMMARY.md/Patterns Established

- TextBox 拖放模式：设置 AllowDrop=True，绑定 DragEnter/DragLeave/DragOver/Drop 四个事件处理器，视觉反馈通过直接修改 BorderBrush/BorderThickness/Background 实现。
  Source: S01-SUMMARY.md/Patterns Established

### Surprises

- 原计划将操作按钮和内容放在同一 Grid 容器内，但发现 TabItem 只接受一个直接子元素。这个 XAML 结构约束在规划阶段未预见，需要 DockPanel 包裹才能实现内容+按钮的并排布局。
  Source: S01-SUMMARY.md/Deviations
