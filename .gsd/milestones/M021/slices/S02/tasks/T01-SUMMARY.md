---
id: T01
parent: S02
milestone: M021
key_files:
  - DocuFiller/ViewModels/CleanupViewModel.cs
key_decisions:
  - Dual-mode cleanup dispatches on string.IsNullOrWhiteSpace(OutputDirectory) — in-place mode uses 1-param CleanupAsync, output-dir mode uses 3-param CleanupAsync
  - RemoveSelectedFiles is a stub logging command, not yet wired to UI selection
  - OpenFolderDialog from Microsoft.Win32 used for BrowseOutputDirectory (WPF-compatible)
duration: 
verification_result: mixed
completed_at: 2026-05-04T10:44:19.909Z
blocker_discovered: false
---

# T01: Rewrite CleanupViewModel with CT.Mvvm source generators, dual-mode cleanup (in-place vs output-dir), and 5 RelayCommand methods

**Rewrite CleanupViewModel with CT.Mvvm source generators, dual-mode cleanup (in-place vs output-dir), and 5 RelayCommand methods**

## What Happened

Rewrote CleanupViewModel from hand-written ObservableObject to CommunityToolkit.Mvvm source generators. Key changes:

1. **Base class migration**: Changed from `ObservableObject` to fully-qualified `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`, added `partial` modifier.

2. **[ObservableProperty] fields** (4): `_isProcessing`, `_progressStatus`, `_progressPercent`, `_outputDirectory` — all use underscore prefix convention for auto-generated PascalCase properties.

3. **New OutputDirectory property**: Defaults to `Documents\DocuFiller输出\清理`, supports dual-mode cleanup.

4. **Dual-mode StartCleanupAsync**: If `OutputDirectory` is set (non-whitespace), creates directory and calls 3-param `CleanupAsync(fileItem, OutputDirectory)`. Otherwise calls 1-param `CleanupAsync(fileItem)` for in-place mode. Success messages include output path in output-dir mode.

5. **Commands via [RelayCommand]**: `StartCleanupCommand` (CanExecute=CanStartCleanup), `ClearListCommand` (CanExecute=CanClearList), `RemoveSelectedFilesCommand` (stub), `BrowseOutputDirectoryCommand` (OpenFolderDialog), `OpenOutputFolderCommand` (Process.Start shell execute).

6. **OnIsProcessingChanged partial method**: Notifies CanStartCleanup and CanClearList computed properties when IsProcessing changes.

7. **Preserved**: AddFiles, AddFolder, RemoveFile public methods for code-behind drag-drop handlers. ProgressChanged event subscription in constructor.

Build verification was blocked by a system-wide NuGet restore failure ("Value cannot be null. (Parameter 'path1')") affecting all projects on this machine, including fresh `dotnet new console` projects. This is an environment infrastructure issue unrelated to the code changes.

## Verification

Manual code review against all 7 must-have checklist items — all pass. Build verification attempted but blocked by system-wide NuGet environment issue (affects all projects, all SDK versions, including fresh projects). Code follows established CT.Mvvm patterns verified via memory store (MEM216, MEM219, MEM232).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj` | 1 | ❌ blocked by environment | 1200ms |

## Deviations

None. All steps from the task plan were implemented as specified.

## Known Issues

System-wide NuGet restore failure prevents build verification — environment issue, not code issue.

## Files Created/Modified

- `DocuFiller/ViewModels/CleanupViewModel.cs`
