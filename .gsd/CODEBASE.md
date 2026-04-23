# Codebase Map

Generated: 2026-04-23T08:34:14Z | Files: 186 | Described: 0/186
<!-- gsd:codebase-meta {"generatedAt":"2026-04-23T08:34:14Z","fingerprint":"9e6fecd4165ec753206019c5e0b93a17e02b8607","fileCount":186,"truncated":false} -->

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
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `README.md`
- `run_e2e_test.ps1`

### ".trae/documents/DocuFiller\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241/
- `".trae/documents/DocuFiller\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241\243.md"`

### ".trae/documents/DocuFiller\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241/
- `".trae/documents/DocuFiller\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241\243.md"`

### ".trae/documents/JSON\345\205\263\351\224\256\350\257\215\347\274\226\350\276\221\345\231\250\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241/
- `".trae/documents/JSON\345\205\263\351\224\256\350\257\215\347\274\226\350\276\221\345\231\250\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241\243.md"`

### ".trae/documents/JSON\345\205\263\351\224\256\350\257\215\347\274\226\350\276\221\345\231\250\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241/
- `".trae/documents/JSON\345\205\263\351\224\256\350\257\215\347\274\226\350\276\221\345\231\250\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241\243.md"`

### "docs/\346\211\271\346\263\250\345\212\237\350\203\275\350\257\264\346\230/
- `"docs/\346\211\271\346\263\250\345\212\237\350\203\275\350\257\264\346\230\216.md"`

### .trae/rules/
- `.trae/rules/project_rules.md`

### Configuration/
- `Configuration/AppSettings.cs`

### Converters/
- `Converters/BooleanToVisibilityConverter.cs`
- `Converters/StringToVisibilityConverter.cs`

### DocuFiller/Services/
- `DocuFiller/Services/CleanupCommentProcessor.cs`
- `DocuFiller/Services/CleanupControlProcessor.cs`
- `DocuFiller/Services/DocumentCleanupService.cs`

### DocuFiller/Services/Update/
- `DocuFiller/Services/Update/UpdateDownloader.cs`

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

### External/
- `External/.gitignore`
- `External/.gitkeep`
- `External/publish-client.usage.txt`
- `External/update-client.config.yaml`

### Models/
- `Models/CleanupFileItem.cs`
- `Models/CleanupProgressEventArgs.cs`
- `Models/ContentControlData.cs`
- `Models/ExcelFileSummary.cs`
- `Models/ExcelValidationResult.cs`
- `Models/FileInfo.cs`
- `Models/FolderProcessRequest.cs`
- `Models/FolderStructure.cs`
- `Models/FormattedCellValue.cs`
- `Models/InputSourceType.cs`
- `Models/JsonKeywordItem.cs`
- `Models/JsonProjectModel.cs`
- `Models/ProcessRequest.cs`
- `Models/ProcessResult.cs`
- `Models/ProgressEventArgs.cs`
- `Models/TextFragment.cs`

### Models/Update/
- `Models/Update/DaemonProgressInfo.cs`
- `Models/Update/DownloadProgress.cs`
- `Models/Update/DownloadStatus.cs`
- `Models/Update/UpdateClientResponseModels.cs`
- `Models/Update/UpdateConfig.cs`
- `Models/Update/VersionInfo.cs`

### Services/
- `Services/CommentManager.cs`
- `Services/ContentControlProcessor.cs`
- `Services/DataParserService.cs`
- `Services/DirectoryManagerService.cs`
- `Services/DocumentProcessorService.cs`
- `Services/ExcelDataParserService.cs`
- `Services/ExcelToWordConverterService.cs`
- `Services/FileScannerService.cs`
- `Services/FileService.cs`
- `Services/JsonEditorService.cs`
- `Services/KeywordValidationService.cs`
- `Services/ProgressReporterService.cs`
- `Services/SafeFormattedContentReplacer.cs`
- `Services/SafeTextReplacer.cs`
- `Services/TemplateCacheService.cs`

### Services/Interfaces/
- `Services/Interfaces/IDataParser.cs`
- `Services/Interfaces/IDirectoryManager.cs`
- `Services/Interfaces/IDocumentCleanupService.cs`
- `Services/Interfaces/IDocumentProcessor.cs`
- `Services/Interfaces/IExcelDataParser.cs`
- `Services/Interfaces/IExcelToWordConverter.cs`
- `Services/Interfaces/IFileScanner.cs`
- `Services/Interfaces/IFileService.cs`
- `Services/Interfaces/IJsonEditorService.cs`
- `Services/Interfaces/IKeywordValidationService.cs`
- `Services/Interfaces/IProgressReporter.cs`
- `Services/Interfaces/ISafeFormattedContentReplacer.cs`
- `Services/Interfaces/ISafeTextReplacer.cs`
- `Services/Interfaces/ITemplateCacheService.cs`

### Services/Update/
- `Services/Update/IUpdateService.cs`
- `Services/Update/UpdateClientService.cs`
- `Services/Update/UpdateService.cs`

### Tests/
- `Tests/ContentControlProcessorTests.cs`
- `Tests/DocuFiller.Tests.csproj`
- `Tests/ExcelDataParserServiceTests.cs`
- `Tests/ExcelIntegrationTests.cs`
- `Tests/FormattedCellValueTests.cs`
- `Tests/HeaderFooterCommentTests.cs`
- `Tests/verify-templates.bat`

### Tests/Data/
- `Tests/Data/test-data.json`

### Tests/DocuFiller.Tests/
- `Tests/DocuFiller.Tests/HeaderFooterIntegrationTests.cs`

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

### Tools/CompareDocumentStructure/
- `Tools/CompareDocumentStructure/CompareDocumentStructure.csproj`
- `Tools/CompareDocumentStructure/Program.cs`

### Tools/ControlRelationshipAnalyzer/
- `Tools/ControlRelationshipAnalyzer/ControlRelationshipAnalyzer.csproj`
- `Tools/ControlRelationshipAnalyzer/Program.cs`

### Tools/DeepDiagnostic/
- `Tools/DeepDiagnostic/DeepDiagnostic.csproj`
- `Tools/DeepDiagnostic/Program.cs`

### Tools/DiagnoseTableStructure/
- `Tools/DiagnoseTableStructure/DiagnoseTableStructure.csproj`
- `Tools/DiagnoseTableStructure/Program.cs`

### Tools/E2ETest/
- `Tools/E2ETest/appsettings.json`
- `Tools/E2ETest/E2ETest.csproj`
- `Tools/E2ETest/Program.cs`

### Tools/ExcelFormattedTestGenerator/
- `Tools/ExcelFormattedTestGenerator/ExcelFormattedTestGenerator.csproj`
- `Tools/ExcelFormattedTestGenerator/Program.cs`

### Tools/ExcelFormattedTestGenerator/TestFiles/
- `Tools/ExcelFormattedTestGenerator/TestFiles/FormattedSuperscriptTest.xlsx`

### Tools/ExcelToWordVerifier/
- `Tools/ExcelToWordVerifier/create_formatted_excel.bat`
- `Tools/ExcelToWordVerifier/create_test_excel.py`
- `Tools/ExcelToWordVerifier/CreateFormattedExcel.csproj`
- `Tools/ExcelToWordVerifier/ExcelToWordVerifier.csproj`
- `Tools/ExcelToWordVerifier/Program.cs`
- `Tools/ExcelToWordVerifier/TestFormattedSuperscript.cs`
- `Tools/ExcelToWordVerifier/TestFormattedSuperscript.csproj`

### Tools/ExcelToWordVerifier/Models/
- `Tools/ExcelToWordVerifier/Models/FormattedText.cs`
- `Tools/ExcelToWordVerifier/Models/TextRun.cs`

### Tools/ExcelToWordVerifier/Services/
- `Tools/ExcelToWordVerifier/Services/ExcelReaderService.cs`
- `Tools/ExcelToWordVerifier/Services/IExcelReader.cs`
- `Tools/ExcelToWordVerifier/Services/IWordWriter.cs`
- `Tools/ExcelToWordVerifier/Services/WordWriterService.cs`

### Tools/ExcelToWordVerifier/TestFiles/
- `Tools/ExcelToWordVerifier/TestFiles/FormattedSuperscriptTest.xlsx`
- `Tools/ExcelToWordVerifier/TestFiles/FormattedTextTest.xlsx`

### Tools/StepByStepSimulator/
- `Tools/StepByStepSimulator/Program.cs`
- `Tools/StepByStepSimulator/StepByStepSimulator.csproj`

### Tools/TableCellTest/
- `Tools/TableCellTest/Program.cs`
- `Tools/TableCellTest/TableCellTest.csproj`

### Tools/TableStructureAnalyzer/
- `Tools/TableStructureAnalyzer/Program.cs`
- `Tools/TableStructureAnalyzer/TableStructureAnalyzer.csproj`

### Utils/
- `Utils/GlobalExceptionHandler.cs`
- `Utils/LoggerConfiguration.cs`
- `Utils/ValidationHelper.cs`
- `Utils/VersionHelper.cs`

### ViewModels/
- `ViewModels/ConverterWindowViewModel.cs`
- `ViewModels/JsonEditorViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/ObservableObject.cs`
- `ViewModels/RelayCommand.cs`

### ViewModels/Update/
- `ViewModels/Update/UpdateBannerViewModel.cs`
- `ViewModels/Update/UpdateViewModel.cs`

### Views/
- `Views/ConverterWindow.xaml`
- `Views/ConverterWindow.xaml.cs`
- `Views/JsonEditorWindow.xaml`
- `Views/JsonEditorWindow.xaml.cs`
- `Views/UpdateBannerView.xaml`
- `Views/UpdateBannerView.xaml.cs`

### Views/Update/
- `Views/Update/UpdateBannerView.xaml`
- `Views/Update/UpdateBannerView.xaml.cs`
- `Views/Update/UpdateWindow.xaml`
- `Views/Update/UpdateWindow.xaml.cs`

### docs/
- `docs/deployment-guide.md`
- `docs/excel-data-user-guide.md`
- `docs/EXTERNAL_SETUP.md`
- `docs/VERSION_MANAGEMENT.md`

### docs/features/
- `docs/features/header-footer-support.md`

### docs/plans/
- `docs/plans/2025-01-20-update-client-design-notes.md`
- `docs/plans/2025-01-20-update-client-integration.md`
- `docs/plans/2025-01-21-version-management-design.md`
- `docs/plans/2025-01-21-version-management-implementation.md`
- `docs/plans/2025-01-23-build-scripts-design.md`
- `docs/plans/2025-01-23-cleanup-feature.md`
- `docs/plans/2025-01-23-cleanup-output-directory.md`
- `docs/plans/2025-01-23-mainwindow-layout-refactor-design.md`
- `docs/plans/2025-01-23-mainwindow-layout-refactor.md`

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
