---
id: T01
parent: S01
milestone: M002-ahlnua
key_files:
  - ViewModels/MainWindowViewModel.cs
  - ViewModels/JsonEditorViewModel.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T08:03:00.695Z
blocker_discovered: false
---

# T01: Remove all Console.WriteLine debug logging from MainWindowViewModel and JsonEditorViewModel, replacing with structured ILogger calls

**Remove all Console.WriteLine debug logging from MainWindowViewModel and JsonEditorViewModel, replacing with structured ILogger calls**

## What Happened

Removed 17 Console.WriteLine calls from MainWindowViewModel.cs and 1 from JsonEditorViewModel.cs. 

In MainWindowViewModel.cs:
- StartProcessAsync: Removed 5 duplicate Console.WriteLine lines that echoed the exact same debug info already logged via _logger.LogInformation. Also removed the "// 同时输出到控制台" comment block. Converted the _logger calls from string interpolation ($"") to structured logging with template parameters.
- HandleFolderDropAsync: Removed 2 Console.WriteLine calls (entry log + invalid folder path). Replaced the invalid-folder-path one with _logger.LogWarning since it's a warning condition.
- ProcessFolderAsync: Removed 6 Console.WriteLine calls (template file list debug output, FolderProcessRequest debug output). Replaced with _logger.LogDebug using structured parameters.

In JsonEditorViewModel.cs:
- Initialize(): Replaced Console.WriteLine("[DEBUG] JsonEditorViewModel 初始化完成") with _logger.LogDebug("JsonEditorViewModel 初始化完成").

Build verification: No C# compilation errors (the only build error is a pre-existing missing update-client.exe binary unrelated to this change). Grep confirms zero Console.WriteLine remaining in both files.

## Verification

grep -rn "Console\.WriteLine" ViewModels/MainWindowViewModel.cs ViewModels/JsonEditorViewModel.cs returns exit code 1 (no matches found), confirming zero Console.WriteLine residue. dotnet build shows no C# compilation errors — only a pre-existing missing update-client.exe error unrelated to this task.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -rn "Console\.WriteLine" ViewModels/MainWindowViewModel.cs ViewModels/JsonEditorViewModel.cs` | 1 | ✅ pass | 75ms |

## Deviations

Minor deviation: the plan said JsonEditorViewModel's Console.WriteLine should be changed to _logger.LogDebug, which was done. For MainWindowViewModel, the plan said to simply delete the duplicate Console.WriteLine lines, but I also upgraded the _logger.LogInformation calls from string interpolation ($"") to structured logging template parameters for consistency. Additionally, the invalid-folder-path log was upgraded to _logger.LogWarning (more appropriate severity) instead of just deleting the Console.WriteLine.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/JsonEditorViewModel.cs`
