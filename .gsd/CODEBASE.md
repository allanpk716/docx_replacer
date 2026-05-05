# Codebase Map

Generated: 2026-05-05T01:08:38Z | Files: 221 | Described: 0/221
<!-- gsd:codebase-meta {"generatedAt":"2026-05-05T01:08:38Z","fingerprint":"f7d169ca1ba461f3d03d344534b93bd41007e932","fileCount":221,"truncated":false} -->

### (root)/
- `.env.example`
- `.gitignore`
- `5`
- `App.config`
- `App.xaml`
- `App.xaml.cs`
- `appsettings.Development.json`
- `appsettings.json`
- `CHANGELOG.md`
- `Directory.Build.props`
- `DocuFiller.csproj`
- `DocuFiller.sln`
- `generate_icon.py`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `Program.cs`
- `README.md`

### "docs/DocuFiller\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241/
- `"docs/DocuFiller\344\272\247\345\223\201\351\234\200\346\261\202\346\226\207\346\241\243.md"`

### "docs/DocuFiller\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241/
- `"docs/DocuFiller\346\212\200\346\234\257\346\236\266\346\236\204\346\226\207\346\241\243.md"`

### "docs/\346\211\271\346\263\250\345\212\237\350\203\275\350\257\264\346\230/
- `"docs/\346\211\271\346\263\250\345\212\237\350\203\275\350\257\264\346\230\216.md"`

### .github/workflows/
- `.github/workflows/build-release.yml`

### .trae/rules/
- `.trae/rules/project_rules.md`

### Behaviors/
- `Behaviors/FileDragDrop.cs`

### Cli/
- `Cli/CliRunner.cs`
- `Cli/ConsoleHelper.cs`
- `Cli/JsonlOutput.cs`

### Cli/Commands/
- `Cli/Commands/CleanupCommand.cs`
- `Cli/Commands/FillCommand.cs`
- `Cli/Commands/InspectCommand.cs`
- `Cli/Commands/UpdateCommand.cs`

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
- `DocuFiller/Views/DownloadProgressWindow.xaml`
- `DocuFiller/Views/DownloadProgressWindow.xaml.cs`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`
- `DocuFiller/Views/UpdateSettingsWindow.xaml.cs`

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

### Properties/
- `Properties/AssemblyInfo.cs`

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
- `Services/UpdateService.cs`

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
- `Services/Interfaces/IUpdateService.cs`

### Tests/
- `Tests/ContentControlProcessorTests.cs`
- `Tests/DocuFiller.Tests.csproj`
- `Tests/DownloadProgressViewModelTests.cs`
- `Tests/ExcelDataParserServiceTests.cs`
- `Tests/ExcelIntegrationTests.cs`
- `Tests/FormattedCellValueTests.cs`
- `Tests/HeaderFooterCommentTests.cs`
- `Tests/UpdateServiceTests.cs`
- `Tests/UpdateSettingsViewModelTests.cs`
- `Tests/verify-templates.bat`

### Tests/DocuFiller.Tests/
- `Tests/DocuFiller.Tests/HeaderFooterIntegrationTests.cs`
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`

### Tests/DocuFiller.Tests/Cli/
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
- `Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`

### Tests/DocuFiller.Tests/Services/
- `Tests/DocuFiller.Tests/Services/CancellationTests.cs`
- `Tests/DocuFiller.Tests/Services/ContentControlProcessorIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Services/FileServiceTests.cs`
- `Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs`
- `Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs`
- `Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs`

### Tests/DocuFiller.Tests/Stubs/
- `Tests/DocuFiller.Tests/Stubs/WindowStubs.cs`

### Tests/DocuFiller.Tests/Utils/
- `Tests/DocuFiller.Tests/Utils/OpenXmlTableCellHelperTests.cs`

### Tests/E2ERegression/
- `Tests/E2ERegression/E2ERegression.csproj`
- `Tests/E2ERegression/HeaderFooterCommentTests.cs`
- `Tests/E2ERegression/InfrastructureTests.cs`
- `Tests/E2ERegression/ReplacementCorrectnessTests.cs`
- `Tests/E2ERegression/RichTextFormatTests.cs`
- `Tests/E2ERegression/ServiceFactory.cs`
- `Tests/E2ERegression/TableStructureTests.cs`
- `Tests/E2ERegression/TestDataHelper.cs`
- `Tests/E2ERegression/TwoColumnFormatTests.cs`

### Tests/Integration/
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`

### Tests/Templates/
- `Tests/Templates/README.md`

### Utils/
- `Utils/GlobalExceptionHandler.cs`
- `Utils/LoggerConfiguration.cs`
- `Utils/OpenXmlHelper.cs`
- `Utils/ValidationHelper.cs`
- `Utils/VersionHelper.cs`

### ViewModels/
- `ViewModels/DownloadProgressViewModel.cs`
- `ViewModels/FillViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/ObservableObject.cs`
- `ViewModels/RelayCommand.cs`
- `ViewModels/UpdateSettingsViewModel.cs`
- `ViewModels/UpdateStatusViewModel.cs`

### docs/
- `docs/excel-data-user-guide.md`
- `docs/release-guide.md`
- `docs/ssh-offline-install.md`
- `docs/update-server-deployment.md`

### docs/cross-platform-research/
- `docs/cross-platform-research/avalonia-research.md`
- `docs/cross-platform-research/blazor-hybrid-research.md`
- `docs/cross-platform-research/comparison-and-recommendation.md`
- `docs/cross-platform-research/core-dependencies-compatibility.md`
- `docs/cross-platform-research/electron-net-research.md`
- `docs/cross-platform-research/maui-research.md`
- `docs/cross-platform-research/packaging-distribution.md`
- `docs/cross-platform-research/platform-differences.md`
- `docs/cross-platform-research/tauri-dotnet-research.md`
- `docs/cross-platform-research/velopack-cross-platform.md`
- `docs/cross-platform-research/web-app-research.md`

### docs/features/
- `docs/features/header-footer-support.md`

### docs/plans/
- `docs/plans/2025-01-23-build-scripts-design.md`
- `docs/plans/2025-01-23-cleanup-feature.md`
- `docs/plans/2025-01-23-cleanup-output-directory.md`
- `docs/plans/2025-01-23-mainwindow-layout-refactor-design.md`
- `docs/plans/2025-01-23-mainwindow-layout-refactor.md`
- `docs/plans/e2e-update-test-guide.md`

### poc/electron-net-docufiller/
- `poc/electron-net-docufiller/electron-net-docufiller.csproj`
- `poc/electron-net-docufiller/electron.manifest.json`
- `poc/electron-net-docufiller/global.json`
- `poc/electron-net-docufiller/Program.cs`

### poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/project.assets.json`

### poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/.nupkg.metadata`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/.signature.p7s`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/electronnet.cli.23.6.2.nupkg`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/electronnet.cli.23.6.2.nupkg.sha512`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/ElectronNET.CLI.nupkg`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/ElectronNET.CLI.nuspec`

### poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/tools/net6.0/any/
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/tools/net6.0/any/dotnet-electronize.deps.json`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/tools/net6.0/any/dotnet-electronize.runtimeconfig.json`
- `poc/electron-net-docufiller/.tools/.store/electronnet.cli/23.6.2/electronnet.cli/23.6.2/tools/net6.0/any/DotnetToolSettings.xml`

### poc/electron-net-docufiller/Controllers/
- `poc/electron-net-docufiller/Controllers/ProcessingController.cs`

### poc/electron-net-docufiller/Services/
- `poc/electron-net-docufiller/Services/SimulatedProcessor.cs`

### poc/electron-net-docufiller/wwwroot/
- `poc/electron-net-docufiller/wwwroot/index.html`

### poc/electron-net-docufiller/wwwroot/css/
- `poc/electron-net-docufiller/wwwroot/css/app.css`

### poc/electron-net-docufiller/wwwroot/js/
- `poc/electron-net-docufiller/wwwroot/js/app.js`

### poc/tauri-docufiller/
- `poc/tauri-docufiller/package.json`

### poc/tauri-docufiller/sidecar-dotnet/
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs`
- `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj`

### poc/tauri-docufiller/src/
- `poc/tauri-docufiller/src/app.js`
- `poc/tauri-docufiller/src/index.html`
- `poc/tauri-docufiller/src/styles.css`

### poc/tauri-docufiller/src-tauri/
- `poc/tauri-docufiller/src-tauri/build.rs`
- `poc/tauri-docufiller/src-tauri/Cargo.toml`
- `poc/tauri-docufiller/src-tauri/tauri.conf.json`

### poc/tauri-docufiller/src-tauri/capabilities/
- `poc/tauri-docufiller/src-tauri/capabilities/default.json`

### poc/tauri-docufiller/src-tauri/gen/schemas/
- `poc/tauri-docufiller/src-tauri/gen/schemas/acl-manifests.json`
- `poc/tauri-docufiller/src-tauri/gen/schemas/capabilities.json`
- `poc/tauri-docufiller/src-tauri/gen/schemas/desktop-schema.json`
- `poc/tauri-docufiller/src-tauri/gen/schemas/windows-schema.json`

### poc/tauri-docufiller/src-tauri/src/
- `poc/tauri-docufiller/src-tauri/src/lib.rs`
- `poc/tauri-docufiller/src-tauri/src/main.rs`

### scripts/
- `scripts/build-internal.bat`
- `scripts/build.bat`
- `scripts/cleanup-gsd-stash.bat`
- `scripts/e2e-dual-channel-test.sh`
- `scripts/e2e-portable-go-update-test.sh`
- `scripts/e2e-portable-update-test.bat`
- `scripts/e2e-serve.py`
- `scripts/e2e-update-test.bat`
- `scripts/install-ssh.bat`
- `scripts/post_reboot_test.py`
- `scripts/run_e2e_test.ps1`
- `scripts/sync-version.bat`
- `scripts/test-releases.win.json`
- `scripts/test-update-server.sh`

### update-server/
- `update-server/go.mod`
- `update-server/main.go`

### update-server/handler/
- `update-server/handler/api.go`
- `update-server/handler/handler_test.go`
- `update-server/handler/list.go`
- `update-server/handler/promote.go`
- `update-server/handler/static.go`
- `update-server/handler/upload_test.go`
- `update-server/handler/upload.go`

### update-server/middleware/
- `update-server/middleware/auth.go`

### update-server/model/
- `update-server/model/release.go`

### update-server/storage/
- `update-server/storage/cleanup_test.go`
- `update-server/storage/cleanup.go`
- `update-server/storage/store_test.go`
- `update-server/storage/store.go`

### update-server/testdata/beta/
- `update-server/testdata/beta/test-1.0.0-full.nupkg`
