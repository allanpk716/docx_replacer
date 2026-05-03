---
id: S01
parent: M017
milestone: M017
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["MainWindow.xaml", "MainWindow.xaml.cs"]
key_decisions:
  - (none)
patterns_established:
  - ["WPF TextBox 拖放使用 Preview 隧道事件模式：将 Drop/DragOver/DragEnter/DragLeave 替换为 Preview 版本，e.Handled=true 阻止内置处理。无内置拦截的控件（如 Border）保持冒泡事件即可。"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M017/slices/S01/tasks/T01-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-03T04:20:08.095Z
blocker_discovered: false
---

# S01: TextBox 拖放事件改为 Preview 隧道

**将模板和数据 TextBox 的 8 个冒泡拖放事件改为 Preview 隧道路由，绕过 TextBox 内置拖放拦截，清理区域事件保持不变**

## What Happened

将 MainWindow.xaml 中 TemplatePathTextBox 和 DataPathTextBox 的 4 个冒泡拖放事件属性（Drop/DragOver/DragEnter/DragLeave）改为 Preview 隧道版本（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave），并在 MainWindow.xaml.cs 中将对应的 8 个事件处理方法重命名添加 Preview 前缀。清理区域 (CleanupDropZoneBorder) 的冒泡事件保持不变。方法签名和 e.Handled=true 逻辑未改动。构建验证通过。

## Verification

dotnet build 0 错误 0 警告。XAML 验证确认 TemplatePathTextBox 和 DataPathTextBox 各使用 4 个 Preview* 事件属性，CleanupDropZoneBorder 保留 4 个冒泡事件。code-behind 确认 8 个 Preview 方法名正确，4 个 Cleanup 方法名未变。

## Requirements Advanced

None.

## Requirements Validated

- R059 — 8 个冒泡拖放事件已改为 Preview 隧道版本，清理区域不变，构建通过

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
