---
id: T02
parent: S02
milestone: M004-l08k3s
key_files:
  - Views/ConverterWindow.xaml
  - Views/ConverterWindow.xaml.cs
  - ViewModels/ConverterWindowViewModel.cs
  - Services/ExcelToWordConverterService.cs
  - Services/Interfaces/IExcelToWordConverter.cs
  - App.xaml.cs
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
  - appsettings.json
  - Configuration/AppSettings.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T14:40:41.232Z
blocker_discovered: false
---

# T02: Remove ConverterWindow, ExcelToWordConverterService, KeywordEditorUrl config, and Tools tab from MainWindow

**Remove ConverterWindow, ExcelToWordConverterService, KeywordEditorUrl config, and Tools tab from MainWindow**

## What Happened

Deleted 5 files: Views/ConverterWindow.xaml, Views/ConverterWindow.xaml.cs, ViewModels/ConverterWindowViewModel.cs, Services/ExcelToWordConverterService.cs, Services/Interfaces/IExcelToWordConverter.cs. From App.xaml.cs removed IExcelToWordConverter, ConverterWindowViewModel, and ConverterWindow DI registrations. From MainWindowViewModel removed OpenConverterCommand declaration, initialization in InitializeCommands, and the OpenConverter method. From MainWindow.xaml removed the entire "工具" TabItem (JSON→Excel converter UI). From MainWindow.xaml.cs removed IOptions&lt;UISettings&gt; constructor parameter, _uiSettings field, KeywordEditorHyperlink_Click handler, ConverterHyperlink_Click handler, and related usings (Microsoft.Extensions.Options, DocuFiller.Configuration, System.Diagnostics). From appsettings.json removed KeywordEditorUrl config entry. From Configuration/AppSettings.cs removed KeywordEditorUrl property from UISettings class. Fixed a pre-existing malformed XML comment (missing &lt;summary&gt; tag) exposed by the handler removal. Build succeeds with 0 errors in main project and tests. All 71 tests pass. Tools/E2ETest has expected build errors referencing deleted IExcelToWordConverter — this project gets removed entirely in the Tools directory cleanup task.

## Verification

1. dotnet build — 0 errors in DocuFiller project (Tools/E2ETest failure expected, removed in later task)
2. dotnet test — 71 passed, 0 failed
3. grep for IExcelToWordConverter|ConverterWindowViewModel|OpenConverterCommand|OpenConverter|KeywordEditor in App.xaml.cs, MainWindowViewModel.cs, MainWindow.xaml.cs, AppSettings.cs, appsettings.json — 0 matches
4. grep for Header="工具" in MainWindow.xaml — 0 matches (tools tab removed)
5. All 5 converter files confirmed deleted

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --no-restore` | 0 | ✅ pass (DocuFiller project) | 2300ms |
| 2 | `dotnet test --no-build --verbosity minimal` | 0 | ✅ pass (71/71 tests) | 822ms |
| 3 | `grep -c 'IExcelToWordConverter|ConverterWindowViewModel|OpenConverterCommand|OpenConverter|KeywordEditor' [core files]` | 1 | ✅ pass (0 matches, exit 1 = no match) | 50ms |
| 4 | `grep -c 'Header="工具"' MainWindow.xaml` | 1 | ✅ pass (0 matches — tools tab removed) | 30ms |

## Deviations

Fixed a pre-existing malformed XML comment (missing &lt;summary&gt; opening tag on the OnClosing method) that was exposed when the event handlers above it were removed. This was not in the task plan but was necessary to eliminate a CS1570 warning.

## Known Issues

Tools/E2ETest/Program.cs still references IExcelToWordConverter and ExcelToWordConverterService — this will be resolved when the entire Tools directory is removed in the next task.

## Files Created/Modified

- `Views/ConverterWindow.xaml`
- `Views/ConverterWindow.xaml.cs`
- `ViewModels/ConverterWindowViewModel.cs`
- `Services/ExcelToWordConverterService.cs`
- `Services/Interfaces/IExcelToWordConverter.cs`
- `App.xaml.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `appsettings.json`
- `Configuration/AppSettings.cs`
