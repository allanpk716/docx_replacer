---
id: T01
parent: S02
milestone: M004-l08k3s
key_files:
  - Services/Interfaces/IDataParser.cs
  - Services/DataParserService.cs
  - Services/DocumentProcessorService.cs
  - ViewModels/MainWindowViewModel.cs
  - App.xaml.cs
  - MainWindow.xaml.cs
  - MainWindow.xaml
  - Models/DataStatistics.cs
  - Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs
  - Tests/ExcelIntegrationTests.cs
  - Tests/Integration/HeaderFooterCommentIntegrationTests.cs
  - Tests/DocuFiller.Tests.csproj
  - Tools/E2ETest/Program.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T14:36:46.245Z
blocker_discovered: false
---

# T01: Remove JSON data source pipeline (IDataParser, DataParserService, JSON branches) — Excel is now the sole data source

**Remove JSON data source pipeline (IDataParser, DataParserService, JSON branches) — Excel is now the sole data source**

## What Happened

Deleted `IDataParser.cs` and `DataParserService.cs` entirely. Removed IDataParser from `DocumentProcessorService` constructor, removed `ProcessJsonDataAsync`, `ProcessDocumentsInParallelAsync`, and the JSON else-branch in both `ProcessDocumentsAsync` and `ProcessFolderAsync`. Removed IDataParser from `MainWindowViewModel` constructor and field, simplified `PreviewDataAsync` to Excel-only, removed JSON detection from `DataPath` setter, simplified `DataFileType` enum to `Excel` only, simplified `DataFileTypeDisplay` to a constant, updated `BrowseData` filter to .xlsx only. Removed DI registration from `App.xaml.cs`. In `MainWindow.xaml.cs`, removed `IsJsonFile`, `IsValidJsonFile`, JSON validation branch in `DataFileDropBorder_Drop`, updated error message to Excel-only, simplified `IsDataFile` to call only `IsExcelFile`. Updated `MainWindow.xaml` drop hint from "JSON or Excel" to "Excel (.xlsx)". Updated three test files: removed MockDataParser, removed DataParserService from DI containers, replaced JSON data-file creation with inline Dictionary data for tests. Created `Models/DataStatistics.cs` to replace the class previously defined in `IDataParser.cs`. Also removed IDataParser reference from `Tools/E2ETest/Program.cs` (Tools directory removal is a separate task). Build succeeded with 0 errors, all 71 tests pass.

## Verification

1. `dotnet build` — 0 errors, 0 warnings
2. `dotnet test` — 71 passed, 0 failed
3. grep for `IDataParser|DataParserService|_dataParser|ProcessJsonData|DataFileType.Json` in core files — 0 real matches (one false positive on ExcelDataParserService substring)
4. No .json references in MainWindow.xaml drop hint, MainWindow.xaml.cs drag-drop logic, or ViewModel BrowseData filter

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --no-restore` | 0 | ✅ pass | 1000ms |
| 2 | `dotnet test --no-build --verbosity minimal` | 0 | ✅ pass (71/71 tests) | 787ms |
| 3 | `grep -c 'IDataParser|DataParserService|_dataParser|ProcessJsonData|DataFileType.Json' Services/DocumentProcessorService.cs ViewModels/MainWindowViewModel.cs App.xaml.cs` | 0 | ✅ pass (0 real JSON refs in core files) | 100ms |

## Deviations

Tools/E2ETest/Program.cs had a IDataParser registration that was not listed in the task plan inputs. Removed the line to keep the build passing — Tools directory is removed in a later task anyway.

## Known Issues

None.

## Files Created/Modified

- `Services/Interfaces/IDataParser.cs`
- `Services/DataParserService.cs`
- `Services/DocumentProcessorService.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`
- `MainWindow.xaml.cs`
- `MainWindow.xaml`
- `Models/DataStatistics.cs`
- `Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs`
- `Tests/ExcelIntegrationTests.cs`
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
- `Tests/DocuFiller.Tests.csproj`
- `Tools/E2ETest/Program.cs`
