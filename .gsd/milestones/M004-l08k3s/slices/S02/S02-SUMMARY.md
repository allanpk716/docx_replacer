---
id: S02
parent: M004-l08k3s
milestone: M004-l08k3s
provides:
  - ["Clean DocumentProcessorService (Excel-only, no IDataParser dependency)", "Clean MainWindowViewModel (Excel-only preview/stats, no JSON logic, no OpenConverterCommand)", "Clean MainWindow.xaml (no tools tab, no JSON drop hint, Excel-only)", "Clean App.xaml.cs (no IDataParser, no IExcelToWordConverter, no ConverterWindow DI)", "Tools/ directory deleted from disk and solution", "appsettings.json without KeywordEditorUrl"]
requires:
  - slice: S01
    provides: Clean App.xaml.cs DI registrations (no update services), clean MainWindowViewModel (no update logic), clean MainWindow.xaml (no update UI)
affects:
  - ["S03"]
key_files:
  - ["Services/Interfaces/IDataParser.cs (deleted)", "Services/DataParserService.cs (deleted)", "Services/DocumentProcessorService.cs", "ViewModels/MainWindowViewModel.cs", "App.xaml.cs", "MainWindow.xaml.cs", "MainWindow.xaml", "Models/DataStatistics.cs (created)", "Views/ConverterWindow.xaml (deleted)", "Views/ConverterWindow.xaml.cs (deleted)", "ViewModels/ConverterWindowViewModel.cs (deleted)", "Services/ExcelToWordConverterService.cs (deleted)", "Services/Interfaces/IExcelToWordConverter.cs (deleted)", "appsettings.json", "Configuration/AppSettings.cs", "DocuFiller.csproj", "DocuFiller.sln", "Tools/ (deleted)"]
key_decisions:
  - (none)
patterns_established:
  - ["When removing a data source type, the pattern is: delete interface + implementation, remove from DI, remove processing branches, simplify enums, update file dialogs, and update drop hints in XAML.", "Solution file (.sln) must be cleaned when removing projects, not just .csproj exclusion entries."]
observability_surfaces:
  - ["none"]
drill_down_paths:
  - [".gsd/milestones/M004-l08k3s/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M004-l08k3s/slices/S02/tasks/T02-SUMMARY.md", ".gsd/milestones/M004-l08k3s/slices/S02/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-23T14:45:53.925Z
blocker_discovered: false
---

# S02: 移除 JSON 数据源、转换器、KeywordEditorUrl、Tools

**Removed all JSON data source infrastructure (IDataParser, DataParserService), JSON→Excel converter (ConverterWindow, ExcelToWordConverterService), KeywordEditorUrl config, and the entire Tools/ directory (10 diagnostic projects) — Excel is now the sole data source**

## What Happened

## Task Execution

**T01 — Remove JSON data source pipeline:** Deleted `IDataParser.cs` and `DataParserService.cs`. Removed IDataParser from DocumentProcessorService constructor, eliminated `ProcessJsonDataAsync`, `ProcessDocumentsInParallelAsync`, and the JSON else-branches in `ProcessDocumentsAsync` and `ProcessFolderAsync`. Simplified MainWindowViewModel to Excel-only (DataFileType enum now has only `Excel`, BrowseData filter is .xlsx only, PreviewDataAsync has no JSON branch). Removed DI registration. Created `Models/DataStatistics.cs` to replace the class previously nested in IDataParser. Updated three test files to remove MockDataParser and JSON data-file references.

**T02 — Remove converter, KeywordEditorUrl, and tools tab:** Deleted 5 files (ConverterWindow.xaml/.cs, ConverterWindowViewModel.cs, ExcelToWordConverterService.cs, IExcelToWordConverter.cs). Removed all DI registrations. Removed OpenConverterCommand from ViewModel. Removed the entire "工具" TabItem from MainWindow.xaml. Removed IOptions<UISettings> constructor parameter, _uiSettings field, and both KeywordEditorHyperlink_Click and ConverterHyperlink_Click handlers from MainWindow.xaml.cs. Removed KeywordEditorUrl from appsettings.json and AppSettings.cs UISettings class. Fixed a pre-existing malformed XML comment.

**T03 — Delete Tools directory:** Deleted the entire Tools/ directory (10 diagnostic tool subdirectories). Removed all Compile/EmbeddedResource/None Remove exclusion entries from DocuFiller.csproj. Cleaned DocuFiller.sln by removing all 10 Tools project entries, the Tools solution folder, and all related configuration sections.

All 71 tests pass. Build has 0 errors and 0 warnings.

## Verification

1. `dotnet build --no-restore` — 0 errors, 0 warnings ✅
2. `dotnet test --no-build --verbosity minimal` — 71 passed, 0 failed ✅
3. grep for IDataParser|DataParserService|_dataParser|ProcessJsonData|DataFileType.Json in core files — 0 real matches (one false positive on ExcelDataParserService substring) ✅
4. grep for IExcelToWordConverter|ConverterWindow|ConverterWindowViewModel|OpenConverter|KeywordEditor in source files — 0 matches ✅
5. grep for Header="工具" in MainWindow.xaml — 0 matches ✅
6. Tools/ directory confirmed deleted on disk ✅
7. grep for all 10 tool project names — 0 residual references ✅

## Requirements Advanced

- R020 — S02 removed all JSON/converter/Tools code. Tests updated in T01 now pass (71/71). S03 must do final documentation sync and verify test quality.

## Requirements Validated

- R016 — grep confirms 0 real IDataParser/DataParserService references; DataFileType enum is Excel-only; DocumentProcessorService has no JSON branch; build and 71 tests pass
- R017 — All 5 converter files deleted; 0 references to converter types in source; DI registrations removed; build and tests pass
- R018 — KeywordEditorUrl removed from appsettings.json and AppSettings.cs; handlers removed from MainWindow.xaml.cs; build and tests pass
- R019 — Tools/ directory confirmed deleted; all 10 tool entries removed from .sln; exclusion entries removed from .csproj; 0 residual references; build and tests pass

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

T01: Tools/E2ETest/Program.cs had an IDataParser registration not listed in the task plan — removed to keep build passing (Tools directory removed in T03 anyway). T02: Fixed a pre-existing malformed XML comment (missing <summary> tag) exposed by handler removal. T03: DocuFiller.sln was not in the task plan but required cleanup (MSB3202 errors from referencing deleted Tools projects).

## Known Limitations

["Tests that previously exercised JSON data paths now use inline Dictionary data instead — test coverage of the Excel path is maintained but JSON-specific edge case coverage is lost (acceptable since JSON is no longer supported)."]

## Follow-ups

None.

## Files Created/Modified

- `Services/Interfaces/IDataParser.cs` — Deleted — JSON data parser interface
- `Services/DataParserService.cs` — Deleted — JSON data parser implementation
- `Services/DocumentProcessorService.cs` — Removed IDataParser constructor param, ProcessJsonDataAsync, JSON branches
- `ViewModels/MainWindowViewModel.cs` — Removed IDataParser, JSON preview logic, DataFileType.Json, OpenConverterCommand
- `App.xaml.cs` — Removed IDataParser, IExcelToWordConverter, ConverterWindow DI registrations
- `MainWindow.xaml.cs` — Removed JSON drag-drop validation, KeywordEditor/Converter handlers, IOptions<UISettings>
- `MainWindow.xaml` — Removed tools tab, updated drop hint to Excel-only
- `Models/DataStatistics.cs` — Created — extracted DataStatistics class from deleted IDataParser.cs
- `Views/ConverterWindow.xaml` — Deleted
- `Views/ConverterWindow.xaml.cs` — Deleted
- `ViewModels/ConverterWindowViewModel.cs` — Deleted
- `Services/ExcelToWordConverterService.cs` — Deleted
- `Services/Interfaces/IExcelToWordConverter.cs` — Deleted
- `appsettings.json` — Removed KeywordEditorUrl
- `Configuration/AppSettings.cs` — Removed KeywordEditorUrl from UISettings
- `DocuFiller.csproj` — Removed Tools/ exclusion entries
- `DocuFiller.sln` — Removed all 10 Tools project entries and solution folder
