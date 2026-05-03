---
id: T01
parent: S01
milestone: M017
key_files:
  - MainWindow.xaml
  - MainWindow.xaml.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-03T04:19:36.701Z
blocker_discovered: false
---

# T01: 将 TemplatePathTextBox 和 DataPathTextBox 的 4 个冒泡拖放事件改为 Preview 隧道路由版本，绕过 TextBox 内置拖放拦截

**将 TemplatePathTextBox 和 DataPathTextBox 的 4 个冒泡拖放事件改为 Preview 隧道路由版本，绕过 TextBox 内置拖放拦截**

## What Happened

将 MainWindow.xaml 中 TemplatePathTextBox 和 DataPathTextBox 的 Drop/DragOver/DragEnter/DragLeave 冒泡事件属性改为 PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave 隧道版本，并在 MainWindow.xaml.cs 中将对应的 8 个事件处理方法重命名（添加 Preview 前缀）。清理区域 (CleanupDropZoneBorder) 的冒泡事件保持不变。方法签名和 e.Handled=true 逻辑未改动。构建通过，0 错误 0 警告。

## Verification

运行 `dotnet build` 成功编译，0 错误 0 警告。确认 XAML 中两个 TextBox 使用 Preview* 事件属性，CleanupDropZoneBorder 保持冒泡事件不变。确认 code-behind 中 8 个方法全部重命名为 Preview 版本，4 个 Cleanup 方法名未变。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2530ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
