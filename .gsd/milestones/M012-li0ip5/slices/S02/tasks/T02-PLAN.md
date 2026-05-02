---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T02: Build verification and drag handler integrity check

验证编译通过，确认所有拖放处理器完好。检查：Window 元素有 AllowDrop 和 PreviewDragOver；AllowDrop 总数 = 4（Window + TemplatePathTextBox + DataPathTextBox + CleanupDropZoneBorder）；三个子控件的 Drop/DragEnter/DragLeave/DragOver 事件处理器未丢失；dotnet build 0 error 0 warning。

## Inputs

- `MainWindow.xaml`
- `MainWindow.xaml.cs`

## Expected Output

- `MainWindow.xaml`
- `MainWindow.xaml.cs`

## Verification

cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M012-li0ip5" && dotnet build 2>&1 | tail -5 && echo '=== AllowDrop count (expect 4) ===' && grep -c 'AllowDrop="True"' MainWindow.xaml && echo '=== Window element attributes ===' && head -12 MainWindow.xaml | grep -E 'AllowDrop|PreviewDragOver' && echo '=== Child drag targets ===' && grep -c 'Drop="' MainWindow.xaml && echo '=== DragEnter handlers ===' && grep -c 'DragEnter="' MainWindow.xaml && echo '=== DragOver handlers ===' && grep -c 'DragOver="' MainWindow.xaml && echo '=== CS handler ===' && grep 'Window_PreviewDragOver' MainWindow.xaml.cs
