---
id: S03
parent: M021
milestone: M021
provides:
  - ["FileDragDrop AttachedProperty Behavior (Behaviors/FileDragDrop.cs) — reusable drag-drop with file filtering and visual feedback", "MainWindow.xaml.cs reduced to 104 lines with only Window-level drag handler", "FillViewModel.TemplateDropCommand/DataDropCommand and CleanupViewModel.DropFilesCommand for drag-drop integration"]
requires:
  []
affects:
  - ["S04 (no impact — independent slice)", "S06 (no impact — documentation only)"]
key_files:
  - ["Behaviors/FileDragDrop.cs", "MainWindow.xaml", "MainWindow.xaml.cs", "ViewModels/FillViewModel.cs", "DocuFiller/ViewModels/CleanupViewModel.cs"]
key_decisions:
  - ["DocxFile filter uses loose DragEnter validation (accepts all FileDrop) with actual file filtering in ViewModel command, matching existing cleanup zone behavior", "CleanupDropZoneBorder uses bubbling events (not Preview) because Border has no built-in drag-drop interception", "AllowDrop removed from XAML — Behavior's OnIsEnabledChanged sets element.AllowDrop automatically"]
patterns_established:
  - ["AttachedProperty Behavior pattern for reusable UI event handling (FileDragDrop.cs): static class with IsEnabled/Filter/DropCommand attached properties", "Preview tunnel events for TextBox targets vs bubbling events for non-TextBox targets in drag-drop scenarios", "CommandManager.InvalidateRequerySuggested() workaround for WPF OLE drag-drop CanExecute stale-state"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M021/slices/S03/tasks/T01-SUMMARY.md", ".gsd/milestones/M021/slices/S03/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-04T11:17:34.836Z
blocker_discovered: false
---

# S03: DragDropBehavior 提取

**将 MainWindow.xaml.cs 中 13 个拖放事件处理器（~446 行）提取为 FileDragDrop AttachedProperty Behavior，代码从 ~550 行降至 104 行，拖放功能行为不变**

## What Happened

## 概述

S03 将 MainWindow.xaml.cs 中散布在 3 个 #region 块里的 13 个拖放事件处理器（~422 行）和 5 个辅助方法（~40 行）全部提取为可复用的 `FileDragDrop` AttachedProperty Behavior（280 行），MainWindow.xaml.cs 仅保留 `Window_PreviewDragOver`（窗口级激活处理器），总行数从 ~550 行降至 104 行。

## T01: 创建 FileDragDrop Behavior + ViewModel DropCommands

创建了 `Behaviors/FileDragDrop.cs` 静态类，包含三个 AttachedProperty：
- **IsEnabled** (bool)：注册/注销拖放事件。TextBox 自动使用 Preview 隧道事件绕过内置拦截，其他元素使用冒泡事件。自动设置 `AllowDrop=true`。
- **Filter** (FileFilter 枚举)：ExcelFile / DocxOrFolder / DocxFile 三种过滤器，在 DragEnter 时验证拖入内容。
- **DropCommand** (ICommand)：Drop 时将文件路径作为 `string[]` 传给 ViewModel 命令。

视觉效果与原有完全一致：TextBox 蓝色边框 #2196F3 (thickness 2) + 20% 蓝色背景；Border 蓝色边框 #2196F3 (thickness 3) + 20% 蓝色背景。

在 FillViewModel 添加 TemplateDropCommand / DataDropCommand，在 CleanupViewModel 添加 DropFilesCommand。所有命令调用 `CommandManager.InvalidateRequerySuggested()` 绕过 WPF OLE 拖放 CanExecute 失效问题。

## T02: XAML 绑定 + 删除 code-behind

在 MainWindow.xaml 添加 `xmlns:behaviors` 命名空间，将 TemplatePathTextBox（4 个 Preview 事件）、DataPathTextBox（4 个 Preview 事件）、CleanupDropZoneBorder（4 个冒泡事件）的 12 个拖放事件属性替换为 FileDragDrop AttachedProperty 绑定。删除了 AllowDrop 属性（由 Behavior 自动设置）。

从 MainWindow.xaml.cs 删除了 3 个 #region 块、13 个事件处理器、5 个辅助方法和 3 个 unused using。仅保留 Window_PreviewDragOver。

## 关键决策

1. **DocxFile 过滤器使用宽松验证**：DragEnter 接受所有 FileDrop，实际过滤在 ViewModel 命令中执行（匹配原有清理区域行为）
2. **CleanupDropZoneBorder 使用冒泡事件**：Border 无内置拖放拦截，不需要 Preview 隧道
3. **AllowDrop 由 Behavior 管理**：XAML 不再手动设置，避免遗忘

## 验证结果

- dotnet build：0 错误，0 警告
- dotnet test：253 单元测试 + 27 E2E 测试全部通过
- MainWindow.xaml.cs：104 行（目标 ≤130 行 ✅）
- 仅保留 1 个 Drag 相关处理器（Window_PreviewDragOver）（目标 ≤3 个 ✅）
- FileDragDrop.cs：280 行，28 个关键属性引用
- XAML 3 个目标元素均有 IsEnabled/Filter/DropCommand 绑定

## Verification

## 构建验证

| 检查项 | 结果 |
|--------|------|
| dotnet build DocuFiller.csproj | 0 错误, 0 警告 ✅ |
| dotnet test (253 单元 + 27 E2E) | 全部通过 ✅ |

## 行数验证

| 文件 | 行数 | 目标 | 结果 |
|------|------|------|------|
| MainWindow.xaml.cs | 104 | ≤130 | ✅ |
| Behaviors/FileDragDrop.cs | 280 | — | 新增 |

## Drag 处理器验证

| 检查项 | 结果 |
|--------|------|
| MainWindow.xaml.cs 中 Drag 相关方法数 | 1 (Window_PreviewDragOver) ✅ |
| XAML FileDragDrop.IsEnabled 绑定数 | 3 (TemplatePathTextBox, DataPathTextBox, CleanupDropZoneBorder) ✅ |
| XAML AllowDrop 属性数 | 1 (仅 Window 级) ✅ |

## 功能完整性验证

- FileFilter 枚举包含 ExcelFile、DocxOrFolder、DocxFile 三种过滤器 ✅
- FillViewModel 有 TemplateDropCommand 和 DataDropCommand ✅
- CleanupViewModel 有 DropFilesCommand ✅
- 视觉反馈代码与原有一致（蓝色边框 + 半透明背景）✅

## Requirements Advanced

None.

## Requirements Validated

None.

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
