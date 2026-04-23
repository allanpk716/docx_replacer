---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T01: 移除 MainWindowViewModel 和 JsonEditorViewModel 中的 Console.WriteLine 调试日志

MainWindowViewModel.cs 中约 17 处 Console.WriteLine 与已存在的 _logger.LogInformation 调用完全重复，直接删除即可。JsonEditorViewModel.cs 中有 1 处 Console.WriteLine 应改为 _logger.LogDebug。清理后这两个 ViewModel 文件中不再有 Console.WriteLine 调用。

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/JsonEditorViewModel.cs`

## Expected Output

- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/JsonEditorViewModel.cs`

## Verification

cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M002-ahlnua" && grep -rn "Console\.WriteLine" ViewModels/MainWindowViewModel.cs ViewModels/JsonEditorViewModel.cs; echo "Exit: $?"
