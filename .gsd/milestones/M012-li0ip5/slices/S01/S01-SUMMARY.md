---
id: S01
parent: M012-li0ip5
milestone: M012-li0ip5
provides:
  - ["MainWindow.xaml 紧凑化布局（900x550，无 GroupBox，12-14px 字号）", "MainWindow.xaml.cs TextBox 拖放事件处理器（TemplatePathTextBox/DataPathTextBox 的 DragEnter/DragLeave/DragOver/Drop）", "App.xaml 调整后的全局样式（缩小的 Padding 和 FontSize）"]
requires:
  []
affects:
  - ["S02"]
key_files:
  - ["MainWindow.xaml", "MainWindow.xaml.cs", "App.xaml"]
key_decisions:
  - ["DockPanel 包裹 TabItem 内容：Grid DockPanel.Dock=Top + 底部操作按钮，解决 TabItem 单子元素约束", "TextBox 拖放视觉反馈直接修改 BorderBrush/BorderThickness/Background，恢复色 #BDC3C7/White", "RestoreBorderStyle/UpdateBorderStyle 改为 Tab 2 清理区域专用配色 #CCC/#F9F9F9"]
patterns_established:
  - ["DockPanel + Grid(DockPanel.Dock=Top) + bottom Grid 用于 TabItem 内容+按钮布局", "TextBox AllowDrop + 事件处理器用于替代独立拖放 Border", "字号分层：TabControl=14, labels=13, body/buttons=12"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M012-li0ip5/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M012-li0ip5/slices/S01/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-02T00:53:39.442Z
blocker_discovered: false
---

# S01: 主界面布局紧凑化

**主界面从 1400x900 紧凑化为 900x550，移除全部 GroupBox，拖放区域迁移到 TextBox AllowDrop，两个 Tab 字号降至 12-14px**

## What Happened

重写了 DocuFiller 主界面的两个 Tab 布局，实现紧凑化目标：

**T01（关键词替换 Tab）**：移除三个 GroupBox（模板文件、数据文件、输出设置），改为 Grid 行布局 + Separator 分隔线。移除两个拖放 Border（TemplateDropBorder、DataFileDropBorder），改为路径 TextBox 直接支持 AllowDrop=True，绑定 DragEnter/DragLeave/DragOver/Drop 事件。迁移 8 个拖放事件处理器到 MainWindow.xaml.cs，视觉反馈改为修改 TextBox 的 BorderBrush/BorderThickness/Background。添加浏览按钮。窗口尺寸从 1400x900 缩小为 900x550（MinWidth=800 MinHeight=500）。

**T02（审核清理 Tab + 全局样式）**：移除"输出设置" GroupBox，改为内联布局。拖放区域 CleanupDropZoneBorder Padding 从 30 降为 12。文件列表 ListView 列宽和按钮缩小。进度区 ProgressBar 高度从 25 降为 16。App.xaml 全局样式调整：ModernTextBoxStyle Padding 12,8→8,4，按钮 Padding 16,8→12,6，HeaderLabelStyle FontSize 16→13，GroupBoxStyle Padding 12→8。

两个 Tab 均使用 DockPanel 包裹结构（Grid DockPanel.Dock=Top + 底部操作按钮），布局一致。编译通过 0 error 0 warning。

## Verification

dotnet build 通过（0 error, 0 warning）。MainWindow.xaml 中 GroupBox 数量为 0，AllowDrop 出现 3 次，FontSize 全部在 11-14px 范围内，CleanupDropZoneBorder 拖放事件处理器完整保留，TemplateDropBorder/DataFileDropBorder 引用数为 0。

## Requirements Advanced

None.

## Requirements Validated

- R050 — Window 900x550, both Tabs compacted, no ScrollViewer, 12-14px fonts, 0 GroupBox, dotnet build 0 errors
- R051 — TemplateDropBorder/DataFileDropBorder removed, TextBox AllowDrop with 4 event handlers each, grep confirms 3 AllowDrop occurrences
- R052 — TabControl=14, labels=13, body=12, App.xaml styles adjusted (Padding, FontSize), all values in 11-14px range
- R053 — All 3 GroupBox removed, replaced with TextBlock + Separator, grep confirms 0 GroupBox in MainWindow.xaml
- R055 — Tab 2 uses same DockPanel structure, same font sizes (12-14px), same label width (65px), same button heights (26-32px), output GroupBox removed

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

计划要求操作按钮和内容用同一容器，实际使用 DockPanel 包裹（Grid DockPanel.Dock=Top + 操作按钮 Grid），因为 TabItem 只能有一个直接子元素。这是 XAML 结构约束，不影响功能和视觉效果。

## Known Limitations

窗口未聚焦时拖放仍可能不工作（R054），这是 S02 要修复的已知 bug。

## Follow-ups

None.

## Files Created/Modified

None.
