# S02: 拖放焦点修复与最终验证

**Goal:** 修复窗口未聚焦时拖放失效的 bug（R054）。在 Window 元素设置 AllowDrop=True 并添加 PreviewDragOver 处理器，当窗口不处于活动状态时调用 Activate()，使子控件的拖放事件处理器能正常接收 OLE 拖放消息。
**Demo:** 窗口未聚焦时从资源管理器拖拽文件到路径文本框，拖放正常触发

## Must-Haves

- dotnet build 编译通过 0 error
- MainWindow.xaml 中 Window 元素包含 AllowDrop="True" 和 PreviewDragOver 事件
- MainWindow.xaml.cs 包含 Window_PreviewDragOver 处理器，非活动时调用 Activate()
- 三个现有拖放目标（TemplatePathTextBox、DataPathTextBox、CleanupDropZoneBorder）的 AllowDrop 和事件处理器完好无损

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: MainWindow.xaml（S01 产出的紧凑化布局）, MainWindow.xaml.cs（S01 产出的 TextBox 拖放事件处理器）
- New wiring introduced in this slice: Window.AllowDrop + PreviewDragOver 激活机制
- What remains before the milestone is truly usable end-to-end: nothing — this is the final slice

## Verification

- Signals added/changed: _logger.LogInformation 在 Window_PreviewDragOver 激活时记录日志
- How a future agent inspects this: 搜索日志 "Window activated for drag-drop" 确认修复生效
- Failure state exposed: 无（仅影响拖放行为，不涉及运行时状态）

## Tasks

- [x] **T01: Add Window-level AllowDrop and PreviewDragOver activation handler** `est:15m`
  在 MainWindow.xaml 的 Window 元素添加 AllowDrop="True" 和 PreviewDragOver="Window_PreviewDragOver"。在 MainWindow.xaml.cs 添加 Window_PreviewDragOver 处理器：当窗口不处于活动状态时调用 Activate()，并记录日志。这使 Window 成为 OLE 拖放目标，在未聚焦时也能接收拖放事件并自动激活窗口。
  - Files: `MainWindow.xaml`, `MainWindow.xaml.cs`
  - Verify: cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M012-li0ip5" && dotnet build 2>&1 | tail -5 && echo '=== AllowDrop count ===' && grep -c 'AllowDrop="True"' MainWindow.xaml && echo '=== Window has AllowDrop ===' && head -10 MainWindow.xaml | grep 'AllowDrop' && echo '=== PreviewDragOver handler ===' && grep 'Window_PreviewDragOver' MainWindow.xaml.cs

- [x] **T02: Build verification and drag handler integrity check** `est:15m`
  验证编译通过，确认所有拖放处理器完好。检查：Window 元素有 AllowDrop 和 PreviewDragOver；AllowDrop 总数 = 4（Window + TemplatePathTextBox + DataPathTextBox + CleanupDropZoneBorder）；三个子控件的 Drop/DragEnter/DragLeave/DragOver 事件处理器未丢失；dotnet build 0 error 0 warning。
  - Files: `MainWindow.xaml`, `MainWindow.xaml.cs`
  - Verify: cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M012-li0ip5" && dotnet build 2>&1 | tail -5 && echo '=== AllowDrop count (expect 4) ===' && grep -c 'AllowDrop="True"' MainWindow.xaml && echo '=== Window element attributes ===' && head -12 MainWindow.xaml | grep -E 'AllowDrop|PreviewDragOver' && echo '=== Child drag targets ===' && grep -c 'Drop="' MainWindow.xaml && echo '=== DragEnter handlers ===' && grep -c 'DragEnter="' MainWindow.xaml && echo '=== DragOver handlers ===' && grep -c 'DragOver="' MainWindow.xaml && echo '=== CS handler ===' && grep 'Window_PreviewDragOver' MainWindow.xaml.cs

## Files Likely Touched

- MainWindow.xaml
- MainWindow.xaml.cs
