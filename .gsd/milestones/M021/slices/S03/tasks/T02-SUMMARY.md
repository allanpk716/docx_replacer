---
id: T02
parent: S03
milestone: M021
key_files:
  - MainWindow.xaml
  - MainWindow.xaml.cs
key_decisions:
  - CleanupDropZoneBorder uses bubbling events (not Preview) because Border has no built-in drag-drop interception, matching MEM190/MEM191 pattern
  - AllowDrop removed from XAML — Behavior's OnIsEnabledChanged sets element.AllowDrop automatically
duration: 
verification_result: passed
completed_at: 2026-05-04T11:16:16.823Z
blocker_discovered: false
---

# T02: Wire FileDragDrop Behavior in XAML, delete 13 drag-drop event handlers and 5 helper methods (~446 lines) from MainWindow.xaml.cs

**Wire FileDragDrop Behavior in XAML, delete 13 drag-drop event handlers and 5 helper methods (~446 lines) from MainWindow.xaml.cs**

## What Happened

Updated MainWindow.xaml with `xmlns:behaviors="clr-namespace:DocuFiller.Behaviors"` namespace. Replaced all 12 drag-drop event attributes on TemplatePathTextBox (4 Preview events), DataPathTextBox (4 Preview events), and CleanupDropZoneBorder (4 bubbling events) with FileDragDrop attached property bindings:

- TemplatePathTextBox: `IsEnabled="True" Filter="DocxOrFolder" DropCommand="{Binding TemplateDropCommand}"`
- DataPathTextBox: `IsEnabled="True" Filter="ExcelFile" DropCommand="{Binding DataDropCommand}"`
- CleanupDropZoneBorder: `IsEnabled="True" Filter="DocxFile" DropCommand="{Binding DropFilesCommand}"`

Removed all `AllowDrop` attributes (now set by Behavior's OnIsEnabledChanged).

Deleted from MainWindow.xaml.cs: 3 #region blocks (数据文件拖拽事件处理, 模板文件拖拽事件处理, 清理功能拖拽事件处理), 13 event handler methods, 5 helper methods (IsExcelFile, IsDataFile, IsDocxFile, RestoreBorderStyle, UpdateBorderStyle), and 3 unused using directives (System.Windows.Input, System.Windows.Media, System.IO). Only Window_PreviewDragOver retained for window activation.

File reduced from ~550 lines to 104 lines. Build: 0 errors, 0 warnings. Tests: 253 unit + 27 E2E all pass.

## Verification

Build verified: `dotnet build DocuFiller.csproj` → 0 errors, 0 warnings. Tests verified: `dotnet test` in main project → 253 unit tests pass, 27 E2E tests pass. Grep confirms only Window_PreviewDragOver remains in code-behind — all 13 drag-drop handlers and 5 helpers removed. XAML correctly references behaviors namespace and attached properties.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj` | 0 | ✅ pass | 3580ms |
| 2 | `dotnet test --verbosity minimal (from main project)` | 0 | ✅ pass (253 unit + 27 E2E) | 15000ms |
| 3 | `grep -c handlers/helpers MainWindow.xaml.cs` | 0 | ✅ pass (only Window_PreviewDragOver remains) | 100ms |

## Deviations

Used `dotnet build DocuFiller.csproj` (with restore) instead of `--no-restore` because the worktree lacked a valid project.assets.json cache. Tests ran from main project directory for the same reason.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
