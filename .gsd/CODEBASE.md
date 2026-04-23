# Codebase Map

Generated: 2026-04-23T16:35:47Z | Files: 149 | Described: 0/149
<!-- gsd:codebase-meta {"generatedAt":"2026-04-23T16:35:47Z","fingerprint":"4c5f4e475dcfc8035ca4630f4f12260fb738b916","fileCount":149,"truncated":false} -->

### (root)/
- `.gitignore`
- `App.config`
- `App.xaml`
- `App.xaml.cs`
- `appsettings.Development.json`
- `appsettings.json`
- `CLAUDE.md`
- `Directory.Build.props`
- `DocuFiller.csproj`
- `DocuFiller.sln`
- `err.txt`
- `help_output.txt`
- `help_output2.txt`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `out.txt`
- `Program.cs`
- `README.md`
- `run_e2e_test.ps1`
- `test_data.xlsx`

### "docs/DocuFiller\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241/
- `"docs/DocuFiller\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241\243.md"`

### "docs/DocuFiller\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241/
- `"docs/DocuFiller\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241\243.md"`

### "docs/\346\211\271\346\263\250\345\212\237\350\203\275\350\257\264\346\230/
- `"docs/\346\211\271\346\263\250\345\212\237\350\203\275\350\257\264\346\230\216.md"`

### .trae/rules/
- `.trae/rules/project_rules.md`

### Cli/
- `Cli/CliRunner.cs`
- `Cli/ConsoleHelper.cs`
- `Cli/JsonlOutput.cs`

### Cli/Commands/
- `Cli/Commands/CleanupCommand.cs`
- `Cli/Commands/FillCommand.cs`
- `Cli/Commands/InspectCommand.cs`

### Configuration/
- `Configuration/AppSettings.cs`

### Converters/
- `Converters/BooleanToVisibilityConverter.cs`
- `Converters/StringToVisibilityConverter.cs`

### DocuFiller/Services/
- `DocuFiller/Services/CleanupCommentProcessor.cs`
- `DocuFiller/Services/CleanupControlProcessor.cs`
- `DocuFiller/Services/DocumentCleanupService.cs`

### DocuFiller/Utils/
- `DocuFiller/Utils/OpenXmlTableCellHelper.cs`

### DocuFiller/ViewModels/
- `DocuFiller/ViewModels/CleanupViewModel.cs`

### DocuFiller/Views/
- `DocuFiller/Views/CleanupWindow.xaml`
- `DocuFiller/Views/CleanupWindow.xaml.cs`

### Exceptions/
- `Exceptions/DataParsingException.cs`
- `Exceptions/DocumentProcessingException.cs`
- `Exceptions/TemplateValidationException.cs`

### Models/
- `Models/CleanupFileItem.cs`
- `Models/CleanupProgressEventArgs.cs`
- `Models/ContentControlData.cs`
- `Models/DataStatistics.cs`
- `Models/ExcelFileSummary.cs`
- `Models/ExcelValidationResult.cs`
- `Models/FileInfo.cs`
- `Models/FolderProcessRequest.cs`
- `Models/FolderStructure.cs`
- `Models/FormattedCellValue.cs`
- `Models/InputSourceType.cs`
- `Models/ProcessRequest.cs`
- `Models/ProcessResult.cs`
- `Models/ProgressEventArgs.cs`
- `Models/TextFragment.cs`

### Services/
- `Services/CommentManager.cs`
- `Services/ContentControlProcessor.cs`
- `Services/DirectoryManagerService.cs`
- `Services/DocumentProcessorService.cs`
- `Services/ExcelDataParserService.cs`
- `Services/FileScannerService.cs`
- `Services/FileService.cs`
- `Services/ProgressReporterService.cs`
- `Services/SafeFormattedContentReplacer.cs`
- `Services/SafeTextReplacer.cs`
- `Services/TemplateCacheService.cs`

### Services/Interfaces/
- `Services/Interfaces/IDirectoryManager.cs`
- `Services/Interfaces/IDocumentCleanupService.cs`
- `Services/Interfaces/IDocumentProcessor.cs`
- `Services/Interfaces/IExcelDataParser.cs`
- `Services/Interfaces/IFileScanner.cs`
- `Services/Interfaces/IFileService.cs`
- `Services/Interfaces/IProgressReporter.cs`
- `Services/Interfaces/ISafeFormattedContentReplacer.cs`
- `Services/Interfaces/ISafeTextReplacer.cs`
- `Services/Interfaces/ITemplateCacheService.cs`

### Tests/
- `Tests/ContentControlProcessorTests.cs`
- `Tests/DocuFiller.Tests.csproj`
- `Tests/ExcelDataParserServiceTests.cs`
- `Tests/ExcelIntegrationTests.cs`
- `Tests/FormattedCellValueTests.cs`
- `Tests/HeaderFooterCommentTests.cs`
- `Tests/verify-templates.bat`

### Tests/DocuFiller.Tests/
- `Tests/DocuFiller.Tests/HeaderFooterIntegrationTests.cs`

### Tests/DocuFiller.Tests/Cli/
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
- `Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs`

### Tests/DocuFiller.Tests/Services/
- `Tests/DocuFiller.Tests/Services/ContentControlProcessorIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs`
- `Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs`

### Tests/DocuFiller.Tests/Utils/
- `Tests/DocuFiller.Tests/Utils/OpenXmlTableCellHelperTests.cs`

### Tests/Integration/
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`

### Tests/Templates/
- `Tests/Templates/README.md`

### Tests/TestResults/
- `Tests/TestResults/cli-test-results.trx`
- `Tests/TestResults/test-results.trx`

### Utils/
- `Utils/GlobalExceptionHandler.cs`
- `Utils/LoggerConfiguration.cs`
- `Utils/ValidationHelper.cs`
- `Utils/VersionHelper.cs`

### ViewModels/
- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/ObservableObject.cs`
- `ViewModels/RelayCommand.cs`

### docs/
- `docs/excel-data-user-guide.md`

### docs/features/
- `docs/features/header-footer-support.md`

### docs/plans/
- `docs/plans/2025-01-23-build-scripts-design.md`
- `docs/plans/2025-01-23-cleanup-feature.md`
- `docs/plans/2025-01-23-cleanup-output-directory.md`
- `docs/plans/2025-01-23-mainwindow-layout-refactor-design.md`
- `docs/plans/2025-01-23-mainwindow-layout-refactor.md`

### publish/
- `publish/App.config`
- `publish/appsettings.Development.json`
- `publish/appsettings.json`
- `publish/coverlet.collector.deps.json`
- `publish/coverlet.collector.targets`
- `publish/DocuFiller.deps.json`
- `publish/DocuFiller.dll.config`
- `publish/DocuFiller.runtimeconfig.json`
- `publish/DocuFiller.Tests.deps.json`
- `publish/DocuFiller.Tests.runtimeconfig.json`
- `publish/DocuFiller.xml`
- `publish/help.txt`
- `publish/Microsoft.CodeCoverage.props`
- `publish/Microsoft.CodeCoverage.targets`
- `publish/ThirdPartyNotices.txt`

### publish/CodeCoverage/
- `publish/CodeCoverage/CodeCoverage.config`
- `publish/CodeCoverage/VanguardInstrumentationProfiler_x86.config`

### publish/CodeCoverage/amd64/
- `publish/CodeCoverage/amd64/VanguardInstrumentationProfiler_x64.config`

### publish/CodeCoverage/arm64/
- `publish/CodeCoverage/arm64/VanguardInstrumentationProfiler_arm64.config`

### publish/InstrumentationEngine/alpine/x64/
- `publish/InstrumentationEngine/alpine/x64/libCoverageInstrumentationMethod.so`
- `publish/InstrumentationEngine/alpine/x64/libInstrumentationEngine.so`
- `publish/InstrumentationEngine/alpine/x64/VanguardInstrumentationProfiler_x64.config`

### publish/InstrumentationEngine/macos/x64/
- `publish/InstrumentationEngine/macos/x64/libCoverageInstrumentationMethod.dylib`
- `publish/InstrumentationEngine/macos/x64/libInstrumentationEngine.dylib`
- `publish/InstrumentationEngine/macos/x64/VanguardInstrumentationProfiler_x64.config`

### publish/InstrumentationEngine/ubuntu/x64/
- `publish/InstrumentationEngine/ubuntu/x64/libCoverageInstrumentationMethod.so`
- `publish/InstrumentationEngine/ubuntu/x64/libInstrumentationEngine.so`
- `publish/InstrumentationEngine/ubuntu/x64/VanguardInstrumentationProfiler_x64.config`

### scripts/
- `scripts/build-and-publish.bat`
- `scripts/build-internal.bat`
- `scripts/build.bat`
- `scripts/publish.bat`
- `scripts/release.bat`
- `scripts/sync-version.bat`

### scripts/config/
- `scripts/config/publish-config.bat`
- `scripts/config/release-config.bat.example`
