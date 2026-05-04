---
id: T01
parent: S03
milestone: M021
key_files:
  - Behaviors/FileDragDrop.cs
  - ViewModels/FillViewModel.cs
  - DocuFiller/ViewModels/CleanupViewModel.cs
key_decisions:
  - DocxFile filter uses loose DragEnter validation (accepts all FileDrop) with actual file filtering in the ViewModel command, matching existing cleanup zone behavior
  - DropCommand passes string[] to ViewModel commands — each ViewModel extracts what it needs (Template/Data use paths[0], Cleanup iterates all)
duration: 
verification_result: passed
completed_at: 2026-05-04T11:13:27.462Z
blocker_discovered: false
---

# T01: Create FileDragDrop AttachedProperty Behavior with FileFilter enum, visual feedback, and add TemplateDrop/DataDrop/DropFiles commands to ViewModels

**Create FileDragDrop AttachedProperty Behavior with FileFilter enum, visual feedback, and add TemplateDrop/DataDrop/DropFiles commands to ViewModels**

## What Happened

Created `Behaviors/FileDragDrop.cs` — a static class with three attached properties:

1. **IsEnabled** (bool) — registers/unregisters drag-drop events on the target element. TextBox targets get Preview tunnel events (PreviewDragEnter/Leave/Over/Drop) to bypass built-in drag-drop interception; all other targets get bubbling events (DragEnter/Leave/Over/Drop).

2. **Filter** (FileFilter enum) — controls file validation during drag. `ExcelFile` requires a single .xlsx; `DocxOrFolder` requires .docx/.dotx file or directory; `DocxFile` accepts any FileDrop (loose validation, command does filtering).

3. **DropCommand** (ICommand) — invoked on drop with the dropped file paths as `string[]`.

Visual feedback exactly matches existing MainWindow.xaml.cs: TextBox gets blue border (#2196F3, thickness 2) + 20% blue background; Border gets blue border (#2196F3, thickness 3) + 20% blue background. Restore resets to original neutral colors.

Added to **FillViewModel**:
- `TemplateDrop(string[] paths)` — validates first path as .docx file or folder, delegates to existing `HandleSingleFileDropAsync`/`HandleFolderDropAsync`
- `DataDrop(string[] paths)` — validates first path as .xlsx, sets `DataPath`, executes `PreviewDataCommand`
- `IsExcelFile(string)` — private static helper

Added to **CleanupViewModel**:
- `DropFiles(string[] paths)` — iterates paths, calls existing `AddFiles` for .docx files and `AddFolder` for directories

All commands call `CommandManager.InvalidateRequerySuggested()` to work around the WPF OLE drag-drop CanExecute stale-state issue (MEM195).

## Verification

Build succeeded with 0 errors, 0 warnings via `dotnet build DocuFiller.csproj`. Grep confirms: FileDragDrop.cs has 22 matches for Filter/DropCommand/IsEnabled attached properties; FillViewModel.cs has TemplateDrop and DataDrop methods; CleanupViewModel.cs has DropFiles method.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj` | 0 | ✅ pass | 5430ms |
| 2 | `grep -c 'TemplateDrop|DataDrop' ViewModels/FillViewModel.cs` | 0 | ✅ pass (2 matches) | 100ms |
| 3 | `grep -c 'DropFiles' DocuFiller/ViewModels/CleanupViewModel.cs` | 0 | ✅ pass (1 match) | 100ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Behaviors/FileDragDrop.cs`
- `ViewModels/FillViewModel.cs`
- `DocuFiller/ViewModels/CleanupViewModel.cs`
