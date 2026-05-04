---
id: T02
parent: S02
milestone: M021
key_files:
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
  - DocuFiller/ViewModels/CleanupViewModel.cs
key_decisions:
  - Removed '退出' button from cleanup Tab footer — CloseCleanupCommand was a no-op stub, not meaningful functionality
  - CleanupVM property on MainWindowViewModel follows same coordinator pattern as FillVM and UpdateStatusVM
duration: 
verification_result: passed
completed_at: 2026-05-04T10:50:08.591Z
blocker_discovered: false
---

# T02: Wire cleanup Tab to shared CleanupViewModel, remove all cleanup code from MainWindowViewModel (390→156 lines)

**Wire cleanup Tab to shared CleanupViewModel, remove all cleanup code from MainWindowViewModel (390→156 lines)**

## What Happened

Executed T02 plan to wire the MainWindow cleanup Tab to the shared CleanupViewModel and remove all cleanup code from MainWindowViewModel.

**Step 1 — MainWindowViewModel cleanup:** Removed IDocumentCleanupService field, all cleanup-related fields (_isCleanupProcessing, _cleanupProgressStatus, _cleanupProgressPercent, _cleanupOutputDirectory), CleanupFileItems collection, cleanup properties (IsCleanupProcessing, CleanupProgressStatus, CleanupProgressPercent, CanStartCleanup, CleanupOutputDirectory), cleanup commands (7 commands), and all cleanup methods (OpenCleanup, RemoveSelectedCleanup, ClearCleanupList, StartCleanupAsync, CloseCleanup, BrowseCleanupOutput, OpenCleanupOutputFolder). Added CleanupViewModel constructor parameter and CleanupVM property. Also removed unused imports (ObservableCollection, IO, LINQ, DocuFiller.Models, DocuFiller.Utils). File went from 390→156 lines.

**Step 2 — MainWindow.xaml:** Changed cleanup Tab DockPanel to use `DataContext="{Binding CleanupVM}"`. Updated all bindings: CleanupOutputDirectory→OutputDirectory, CleanupFileItems→FileItems, CleanupProgressStatus→ProgressStatus, CleanupProgressPercent→ProgressPercent, BrowseCleanupOutputCommand→BrowseOutputDirectoryCommand, OpenCleanupOutputFolderCommand→OpenOutputFolderCommand, RemoveSelectedCleanupCommand→RemoveSelectedFilesCommand, ClearCleanupListCommand→ClearListCommand. Removed the "退出" button (CloseCleanupCommand stub was a no-op).

**Step 3 — MainWindow.xaml.cs:** Rewrote CleanupDropZoneBorder_Drop to call cleanupVM.AddFiles/AddFolder instead of the removed helper methods. Removed AddCleanupFile and AddCleanupFolder helper methods entirely. Removed unused imports (DocuFiller.Models, System.Linq).

**Step 4 — App.xaml.cs:** No changes needed — CleanupViewModel was already registered as Transient before MainWindowViewModel.

**Pre-fix:** Also fixed a T01 regression — CleanupViewModel.cs line 206 had ambiguous `FileInfo` reference between DocuFiller.Models.FileInfo and System.IO.FileInfo. Fixed by fully qualifying to `System.IO.FileInfo`.

## Verification

Build verification: `dotnet build DocuFiller.csproj --no-restore` → 0 errors, 0 warnings. Test verification: `dotnet test Tests/DocuFiller.Tests.csproj --no-restore --verbosity minimal` → 253 passed, 0 failed. Manual verification: grep confirmed no cleanup fields/properties/commands/methods remain in MainWindowViewModel, CleanupVM property exposed, XAML DataContext bound to CleanupVM, all bindings updated, helper methods removed from code-behind.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj --no-restore` | 0 | ✅ pass | 2690ms |
| 2 | `dotnet test Tests/DocuFiller.Tests.csproj --no-restore --verbosity minimal` | 0 | ✅ pass (253 passed, 0 failed) | 10000ms |

## Deviations

Fixed T01 regression in CleanupViewModel.cs (ambiguous FileInfo reference) — not part of T02 plan but required for build success.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `DocuFiller/ViewModels/CleanupViewModel.cs`
