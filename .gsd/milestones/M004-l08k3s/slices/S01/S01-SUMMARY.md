---
id: S01
parent: M004-l08k3s
milestone: M004-l08k3s
provides:
  - ["Clean csproj with no PreBuild update-client gate", "Clean App.xaml.cs with no update service DI registrations", "Clean MainWindowViewModel with no update logic or commands", "Clean MainWindow.xaml with no update UI elements or namespaces", "Clean MainWindow.xaml.cs with no update event handlers", "Deleted External/ directory", "Deleted all 19 update system files", "Deleted all 9 JSON editor orphan files"]
requires:
  []
affects:
  []
key_files:
  - ["DocuFiller.csproj", "App.xaml.cs", "ViewModels/MainWindowViewModel.cs", "MainWindow.xaml", "MainWindow.xaml.cs"]
key_decisions:
  - ["Extended T01 scope to include all T02 work (MainWindow cleanup) since update system was deeply integrated into constructor params, commands, properties, and event handlers — project would not compile without removing these references", "Fixed file corruption in MainWindowViewModel.cs where duplicate content with garbled bytes was appended after closing brace (likely from worktree git operations)"]
patterns_established:
  - ["Feature removal pattern: delete service files → clean DI registrations → clean consuming ViewModels → clean Views → clean code-behind → verify build. Tight coupling may require expanding scope into consuming files."]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M004-l08k3s/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M004-l08k3s/slices/S01/tasks/T02-SUMMARY.md", ".gsd/milestones/M004-l08k3s/slices/S01/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-23T14:18:36.485Z
blocker_discovered: false
---

# S01: 移除更新功能和 JSON 编辑器遗留

**Removed all online update infrastructure (19 files, csproj gates, External directory, DI registrations, MainWindow integration) and 9 orphaned JSON editor files. dotnet build passes with 0 errors.**

## What Happened

## What Happened

This slice removed all online update infrastructure and orphaned JSON editor files from DocuFiller.

### Update System Removal (T01 + T02)

**DocuFiller.csproj:** Removed three build targets — `ValidateUpdateClientFiles` (PreBuild gate that blocked building without External/update-client.exe), `ValidateReleaseFiles` (PostPublish validation), and the Update Client External Files ItemGroup.

**External/ directory:** Deleted entirely (contained .gitignore, .gitkeep, publish-client.usage.txt, update-client.config.yaml).

**Deleted 19 update files** across 4 layers:
- Services: `IUpdateService.cs`, `UpdateClientService.cs`, `UpdateService.cs`, `UpdateDownloader.cs`
- Models: `DaemonProgressInfo.cs`, `DownloadProgress.cs`, `DownloadStatus.cs`, `UpdateClientResponseModels.cs`, `UpdateConfig.cs`, `VersionInfo.cs`
- ViewModels: `UpdateBannerViewModel.cs`, `UpdateViewModel.cs`
- Views: `UpdateBannerView.xaml/.cs`, `UpdateWindow.xaml/.cs`, `UpdateBannerView.xaml/.cs` (root duplicate)

**App.xaml.cs:** Removed `using DocuFiller.Services.Update` and 5 DI registrations (IUpdateService, UpdateViewModel, UpdateBannerViewModel, UpdateWindow, UpdateBannerView).

**MainWindowViewModel.cs:** Removed IUpdateService field, 3 update state fields, 2 constructor parameters, 7 methods (OnInitializedAsync, CheckForUpdatesAsync, ShowUpdateBannerAsync, CheckForUpdateAsync, OnUpdateAvailable, ShowUpdateWindow, SubscribeToUpdateEvents), 3 properties, CheckForUpdateCommand, and the Task.Run auto-update call.

**MainWindow.xaml:** Removed `xmlns:updateViews` namespace and the "检查更新" Border UI block with bell icon and CheckForUpdateCommand binding.

**MainWindow.xaml.cs:** Removed CheckForUpdateHyperlink_Click event handler.

**File corruption fix:** MainWindowViewModel.cs had duplicate content with garbled bytes appended after the closing brace (likely from worktree git operations). Fixed as part of T01.

### JSON Editor Cleanup (T03)

Deleted 9 orphaned files with no DI registration or active references:
- Services: `JsonEditorService.cs`, `IJsonEditorService.cs`, `KeywordValidationService.cs`, `IKeywordValidationService.cs`
- Models: `JsonKeywordItem.cs`, `JsonProjectModel.cs`
- ViewModels: `JsonEditorViewModel.cs`
- Views: `JsonEditorWindow.xaml`, `JsonEditorWindow.xaml.cs`

### Scope Deviation

T01 extended beyond its planned scope to include all of T02's work (MainWindow cleanup). This was necessary because the update system was deeply integrated into MainWindowViewModel (constructor params, fields, commands, methods, properties) and the project would not compile without removing those references.

## Verification

All grep checks return 0 matches for update/JSON editor identifiers across all modified files. dotnet build succeeds with 0 errors (54 pre-existing warnings, none related to removed code). No update or JSON editor code files remain in the codebase.

## Verification

## Verification Results

| # | Check | Result |
|---|-------|--------|
| 1 | `grep -c "ValidateUpdateClientFiles\|ValidateReleaseFiles\|update-client" DocuFiller.csproj` → 0 | ✅ pass |
| 2 | `test ! -d External` → directory deleted | ✅ pass |
| 3 | `grep -c "IUpdateService\|UpdateViewModel\|UpdateBannerView\|UpdateWindow" App.xaml.cs` → 0 | ✅ pass |
| 4 | `grep -c "IUpdateService\|UpdateBanner\|CheckForUpdate\|ShowUpdate\|OnUpdateAvailable\|VersionInfo\|UpdateViewModel" MainWindowViewModel.cs` → 0 | ✅ pass |
| 5 | `grep -c "CheckForUpdate\|updateViews\|检查更新" MainWindow.xaml` → 0 | ✅ pass |
| 6 | `grep -c "CheckForUpdate" MainWindow.xaml.cs` → 0 | ✅ pass |
| 7 | `test ! -f Services/JsonEditorService.cs` → deleted | ✅ pass |
| 8 | `test ! -f Models/JsonKeywordItem.cs` → deleted | ✅ pass |
| 9 | `test ! -f ViewModels/JsonEditorViewModel.cs` → deleted | ✅ pass |
| 10 | `test ! -f Views/JsonEditorWindow.xaml` → deleted | ✅ pass |
| 11 | `find . -name "*.cs" -path "*/Update/*"` → no results | ✅ pass |
| 12 | `find . -name "JsonEditor*" -o -name "JsonKeywordItem*"` → no results | ✅ pass |
| 13 | `dotnet build` → 0 errors, 54 pre-existing warnings | ✅ pass |

## Requirements Advanced

- R016 — S01 removed update DI registrations and MainWindow update references, clearing the way for S02 to remove JSON data source code without conflicts
- R017 — S01 cleaned App.xaml.cs DI registrations and MainWindow.xaml structure, simplifying S02 converter window removal

## Requirements Validated

- R014 — All online update code removed: 19 files deleted, csproj gates removed, External/ deleted, DI registrations removed, MainWindow integration cleaned. grep confirms 0 matches. dotnet build passes.
- R015 — All 9 JSON editor orphaned files deleted. No remaining files found via find/grep. dotnet build passes.

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
