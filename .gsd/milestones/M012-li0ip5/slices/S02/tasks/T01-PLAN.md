---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T01: Add Window-level AllowDrop and PreviewDragOver activation handler

在 MainWindow.xaml 的 Window 元素添加 AllowDrop="True" 和 PreviewDragOver="Window_PreviewDragOver"。在 MainWindow.xaml.cs 添加 Window_PreviewDragOver 处理器：当窗口不处于活动状态时调用 Activate()，并记录日志。这使 Window 成为 OLE 拖放目标，在未聚焦时也能接收拖放事件并自动激活窗口。

## Inputs

- `MainWindow.xaml`
- `MainWindow.xaml.cs`

## Expected Output

- `MainWindow.xaml`
- `MainWindow.xaml.cs`

## Verification

cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M012-li0ip5" && dotnet build 2>&1 | tail -5 && echo '=== AllowDrop count ===' && grep -c 'AllowDrop="True"' MainWindow.xaml && echo '=== Window has AllowDrop ===' && head -10 MainWindow.xaml | grep 'AllowDrop' && echo '=== PreviewDragOver handler ===' && grep 'Window_PreviewDragOver' MainWindow.xaml.cs
