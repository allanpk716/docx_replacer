---
id: S02
parent: M021
milestone: M021
provides:
  - ["CleanupViewModel (CT.Mvvm, 244 lines) — unified VM for both MainWindow Tab and CleanupWindow", "MainWindowViewModel reduced to 156 lines with zero cleanup code", "DockPanel DataContext scoping pattern validated for second sub-VM"]
requires:
  []
affects:
  []
key_files:
  - ["DocuFiller/ViewModels/CleanupViewModel.cs", "ViewModels/MainWindowViewModel.cs", "MainWindow.xaml", "MainWindow.xaml.cs"]
key_decisions:
  - ["Dual-mode cleanup dispatch on OutputDirectory emptiness", "DockPanel DataContext scoping for cleanup Tab (matching FillVM pattern)", "Removed dead CloseCleanupCommand/退出 button from cleanup Tab"]
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M021/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M021/slices/S02/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-04T10:52:17.933Z
blocker_discovered: false
---

# S02: CleanupViewModel 统一 + CT.Mvvm 迁移

**Migrated CleanupViewModel to CT.Mvvm, unified MainWindow cleanup Tab and CleanupWindow to share the same ViewModel, removed all cleanup code from MainWindowViewModel (390→156 lines)**

## What Happened

## What Was Done

**T01 — CleanupViewModel CT.Mvvm Rewrite:**
- Migrated CleanupViewModel from hand-written ObservableObject to CommunityToolkit.Mvvm source generators (partial class, [ObservableProperty], [RelayCommand])
- Added OutputDirectory property with default path (Documents\DocuFiller输出\清理) supporting dual-mode cleanup
- Implemented dual-mode StartCleanupAsync: dispatches on OutputDirectory emptiness — in-place mode uses 1-param CleanupAsync, output-dir mode creates directory and calls 3-param CleanupAsync
- Created 5 RelayCommand methods: StartCleanup, ClearList, RemoveSelectedFiles, BrowseOutputDirectory, OpenOutputFolder
- Fixed ambiguous FileInfo reference (DocuFiller.Models.FileInfo vs System.IO.FileInfo)

**T02 — Wire cleanup Tab + Remove cleanup from MainWindowVM:**
- Removed all cleanup-specific code from MainWindowViewModel: IDocumentCleanupService dependency, 4 cleanup fields, 5 cleanup properties, 7 cleanup commands, 8 cleanup methods, and unused imports
- MainWindowViewModel reduced from 390→156 lines (zero cleanup code remains)
- Changed MainWindow cleanup Tab DockPanel DataContext to `{Binding CleanupVM}` (DockPanel scoping pattern matching FillVM)
- Updated all XAML bindings to match CleanupViewModel property names
- Rewrote MainWindow.xaml.cs drag-drop handlers to delegate to CleanupVM.AddFiles/AddFolder
- Removed dead helper methods (AddCleanupFile, AddCleanupFolder) and unused "退出" button
- CleanupWindow continues to work unchanged (in-place mode, no output directory)

## Verification

## Build & Test Verification

- `dotnet build DocuFiller.csproj --no-restore` → 0 errors, 0 warnings ✅
- `dotnet test --no-restore --verbosity minimal` → 253 passed, 0 failed, 0 skipped ✅

## Slice Goal Verification

| Must-Have | Status |
|-----------|--------|
| CleanupViewModel uses CT.Mvvm (partial class + [ObservableProperty] + [RelayCommand]) | ✅ Verified: 3 CT.Mvvm imports, 4 ObservableProperty fields, 5 RelayCommand methods, partial class |
| CleanupViewModel has OutputDirectory property with default path | ✅ Verified: defaults to Documents\DocuFiller输出\清理 |
| MainWindow cleanup Tab DataContext binds to CleanupVM via DockPanel scoping | ✅ Verified: DataContext="{Binding CleanupVM}" on cleanup Tab DockPanel |
| MainWindowViewModel has zero cleanup-specific code | ✅ Verified: grep returns 0 matches for IDocumentCleanupService, 0 for CleanupFile |
| MainWindowViewModel no longer depends on IDocumentCleanupService | ✅ Verified: 0 grep matches |
| OpenCleanupCommand removed | ✅ Verified: removed as part of cleanup code deletion |
| MainWindow.xaml.cs cleanup drag-drop handlers call CleanupVM methods | ✅ Verified: cleanupVM.AddFiles/AddFolder calls in Drop handler |
| CleanupWindow continues to work unchanged | ✅ Verified: in-place mode (no output directory) |
| dotnet build 0 errors, dotnet test all passed | ✅ Verified: 0 errors, 253 passed |

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
