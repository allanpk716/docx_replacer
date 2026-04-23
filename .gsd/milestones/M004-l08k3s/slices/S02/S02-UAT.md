# S02: 移除 JSON 数据源、转换器、KeywordEditorUrl、Tools — UAT

**Milestone:** M004-l08k3s
**Written:** 2026-04-23T14:45:53.926Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a code removal operation with no runtime behavior changes. Verification is build/test based, not live runtime.

## Preconditions

- .NET 8 SDK installed
- Working directory is the project root (M004-l08k3s worktree)

## Smoke Test

1. Run `dotnet build` — expect 0 errors, 0 warnings
2. Run `dotnet test` — expect all tests pass
3. Confirm Tools/ directory does not exist

## Test Cases

### 1. Build succeeds without Tools directory

1. Confirm `Tools/` directory does not exist on disk
2. Run `dotnet build --no-restore`
3. **Expected:** 0 errors, 0 warnings

### 2. All tests pass after removals

1. Run `dotnet test --no-build --verbosity minimal`
2. **Expected:** 71 passed, 0 failed, 0 skipped

### 3. No IDataParser/DataParserService residual references

1. Run `grep -c "IDataParser\|DataParserService\|_dataParser\|ProcessJsonData\|DataFileType\.Json" Services/DocumentProcessorService.cs ViewModels/MainWindowViewModel.cs App.xaml.cs`
2. **Expected:** 0 matches in each file (ExcelDataParserService false positive is acceptable)

### 4. No converter/KeywordEditor residual references

1. Run `grep -c "IExcelToWordConverter\|ConverterWindow\|ConverterWindowViewModel\|OpenConverter\|KeywordEditor" App.xaml.cs ViewModels/MainWindowViewModel.cs MainWindow.xaml.cs MainWindow.xaml Configuration/AppSettings.cs appsettings.json`
2. **Expected:** 0 matches in all files

### 5. No Tools tab in MainWindow

1. Run `grep -c 'Header="工具"' MainWindow.xaml`
2. **Expected:** 0 matches

### 6. DataFileType enum is Excel-only

1. Search for DataFileType in ViewModels/MainWindowViewModel.cs
2. **Expected:** Only `DataFileType.Excel` exists; no `.Json` member

### 7. No JSON branch in DocumentProcessorService

1. Search for "json" (case-insensitive) in Services/DocumentProcessorService.cs
2. **Expected:** No JSON processing logic remains

### 8. appsettings.json has no KeywordEditorUrl

1. Run `grep "KeywordEditorUrl" appsettings.json`
2. **Expected:** 0 matches

## Edge Cases

### False positive substring match
- `ExcelDataParserService` contains "DataParser" as a substring — this is the active Excel parser and is NOT the removed `DataParserService`. Grep verification must account for this.

## Failure Signals

- Build errors referencing IDataParser, DataParserService, IExcelToWordConverter, or ConverterWindow
- Test failures
- Tools/ directory still present on disk
- "工具" tab still visible in MainWindow.xaml

## Not Proven By This UAT

- Application launch and runtime Excel data processing (covered by S01 and existing tests)
- Documentation sync with cleaned code (S03 responsibility)

## Notes for Tester

All verification commands use bash syntax. On pure Windows cmd, replace `grep` with `findstr` and `test ! -d` with `if not exist`.
