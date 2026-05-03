---
phase: close
phase_name: Milestone Completion
project: DocuFiller
generated: "2026-05-03T04:25:00Z"
counts:
  decisions: 1
  lessons: 1
  patterns: 1
  surprises: 0
missing_artifacts: []
---

### Decisions

- **选择 Preview 隧道事件而非 Border 覆盖方案修复 TextBox 拖放拦截。** Preview 事件是 WPF 标准机制，改动最小（仅重命名事件属性和方法名），不破坏现有 XAML 布局。替代方案（Border 覆盖 TextBox、自定义 TextBox 子类重写 OnDragOver）过度工程。
  Source: M017-CONTEXT.md/Architectural Decisions

### Lessons

- **WPF TextBox 内置文本拖放处理在冒泡阶段拦截外部文件拖放，即使 IsReadOnly="True" + AllowDrop="True" 也无法绕过。** 根因是 TextBox 内部有文本选择拖拽的默认处理逻辑，会在 DragOver 冒泡阶段将 e.Effects 设为 None。修复方式是使用 Preview 隧道事件（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave），在隧道阶段设置 e.Handled=true 阻止事件到达 TextBox 内部处理。
  Source: M017-CONTEXT.md/Root Cause

### Patterns

- **WPF TextBox 拖放使用 Preview 隧道事件模式。** 将 Drop/DragOver/DragEnter/DragLeave 替换为 Preview 版本，配合 e.Handled=true 阻止内置处理。对于无内置拦截的控件（如 Border），保持冒泡事件即可。这是一个通用模式，适用于所有需要外部文件拖放但控件有内置拖放行为的场景。
  Source: S01-SUMMARY.md/Patterns Established

### Surprises

（无）
