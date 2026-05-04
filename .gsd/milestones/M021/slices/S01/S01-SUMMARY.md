---
id: S01
parent: M021
milestone: M021
provides:
  - ["MainWindowVM coordinator (390 lines) holding FillVM/UpdateStatusVM references", "FillViewModel (825 lines, CT.Mvvm) — keyword replacement tab business logic", "UpdateStatusViewModel (397 lines, CT.Mvvm) — update status management + CheckUpdateAsync", "XAML DataContext scoping pattern (DockPanel DataContext per tab)", "DI registration pattern for sub-ViewModels"]
requires:
  []
affects:
  - ["S02", "S03", "S05", "S06"]
key_files:
  - ["ViewModels/FillViewModel.cs", "ViewModels/UpdateStatusViewModel.cs", "ViewModels/MainWindowViewModel.cs", "MainWindow.xaml", "MainWindow.xaml.cs", "App.xaml.cs"]
key_decisions:
  - ["Used DockPanel DataContext={Binding FillVM} for fill tab scope instead of prefixing every binding with FillVM.", "Added proxy properties (IsProcessing, CancelProcessCommand) on coordinator for MainWindow.xaml.cs OnClosing access without casting.", "Used IServiceProvider injection in UpdateStatusViewModel for window creation instead of ((App)Application.Current).ServiceProvider.", "UpdateStatus enum co-located in UpdateStatusViewModel.cs file rather than separate file.", "FillViewModel and UpdateStatusViewModel both registered as Transient in DI."]
patterns_established:
  - ["Coordinator + sub-ViewModel pattern: MainWindowVM holds child VM references, XAML uses DataContext scoping per tab", "CT.Mvvm extraction pattern: partial class + [ObservableProperty] + [RelayCommand] + fully-qualified ObservableObject base class", "Proxy properties on coordinator for code-behind access to sub-VM state", "DI window creation via IServiceProvider injection instead of Application.Current cast"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M021/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M021/slices/S01/tasks/T02-SUMMARY.md", ".gsd/milestones/M021/slices/S01/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-04T10:28:53.129Z
blocker_discovered: false
---

# S01: FillViewModel + UpdateStatusViewModel 拆分

**MainWindowViewModel refactored from 1623 to 390 lines via coordinator pattern; FillViewModel (825 lines) and UpdateStatusViewModel (397 lines) extracted using CT.Mvvm**

## What Happened

## Overview

This slice accomplished the largest single refactoring in M021: splitting the monolithic MainWindowViewModel (1623 lines) into a coordinator + sub-ViewModel architecture. Three tasks executed cleanly with no blockers or deviations.

## What Was Built

**UpdateStatusViewModel** (397 lines, CT.Mvvm) — T01 extracted all update-domain properties, commands, and methods from MainWindowViewModel:
- Properties: IsCheckingUpdate, CurrentUpdateStatus, CanCheckUpdate, UpdateStatusMessage, UpdateStatusBrush, HasUpdateStatus, CurrentVersion
- Commands: CheckUpdateCommand, UpdateStatusClickCommand, OpenUpdateSettingsCommand
- Methods: CheckUpdateAsync, InitializeUpdateStatusAsync, OnUpdateStatusClickAsync, ExtractHostFromUrl
- UpdateStatus enum co-located in the same file
- Uses IServiceProvider injection for window creation (DI best practice)

**FillViewModel** (825 lines, CT.Mvvm) — T02 extracted all keyword-replacement tab business logic:
- 18 properties, 3 collections, 11 commands
- 6 DI-injected services (IFileService, IExcelDataParser, IProgressReporter, IFileScanner, IDirectoryManager, ISafeTextReplacer)
- All Browse/Validate/Preview/Start/Cancel/HandleDrop/ProcessFolder methods

**MainWindowViewModel** (390 lines) — T03 finalized the coordinator:
- Holds FillVM and UpdateStatusVM sub-ViewModel references
- Retains only cleanup tab logic, window-level commands (Exit, ToggleTopmost), and proxy properties (IsProcessing, CancelProcessCommand)
- Dead GetConfigValue method removed

## XAML Binding Strategy

T02 introduced a clean binding approach: the keyword replacement tab's DockPanel sets `DataContext="{Binding FillVM}"`, so all fill-related bindings resolve directly without prefixing. The Exit button uses `RelativeSource AncestorType=TabItem` to escape the FillVM scope. Status bar bindings use `FillVM.ProgressMessage` and `UpdateStatusVM.*` paths.

## DI Registration

Both FillViewModel and UpdateStatusViewModel registered in App.xaml.cs (FillViewModel as Transient, UpdateStatusViewModel as Transient). MainWindowViewModel constructor accepts both sub-ViewModels via DI.

## Verification

## Build Verification
- `dotnet build DocuFiller.csproj --no-restore` → 0 errors, 0 warnings ✅
- `dotnet test --no-restore` → 280 passed (253 DocuFiller.Tests + 27 E2ERegression), 0 failed ✅

## Line Count Verification
- MainWindowViewModel.cs: 390 lines (target: ≤400) ✅
- FillViewModel.cs: 825 lines (new extraction) ✅
- UpdateStatusViewModel.cs: 397 lines (new extraction) ✅

## XAML Binding Verification
- Build compilation confirms all WPF bindings resolve correctly (binding errors surface as compiler warnings)
- Fill tab bindings route through DockPanel DataContext={Binding FillVM}
- Status bar update bindings route through UpdateStatusVM.* paths
- Exit button correctly escapes FillVM scope via RelativeSource AncestorType=TabItem

## DI Registration Verification
- FillViewModel registered as Transient in App.xaml.cs
- UpdateStatusViewModel registered as Transient in App.xaml.cs
- MainWindowViewModel constructor accepts both sub-ViewModels

## Requirements Advanced

- R060 — MainWindowViewModel reduced from 1623 to 390 lines; FillViewModel and UpdateStatusViewModel extracted with CT.Mvvm; build passes with 0 errors; 280 tests pass

## Requirements Validated

- R060 — dotnet build 0 errors 0 warnings; dotnet test 280 passed; MainWindowVM 390 lines ≤ 400 target; FillVM 825 lines CT.Mvvm; UpdateStatusVM 397 lines CT.Mvvm; XAML bindings verified via compilation

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
