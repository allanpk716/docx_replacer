# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

Project: DocuFiller — a WPF (.NET 8) desktop app for batch-filling Word content controls from JSON data.

Prerequisites
- Windows 10/11
- .NET 8 SDK

Common commands (PowerShell)
- Restore dependencies
  ```powershell
  dotnet restore
  ```
- Build
  ```powershell
  # Debug (default)
  dotnet build

  # Release
  dotnet build -c Release
  ```
- Run the main WPF app
  ```powershell
  dotnet run --project .\DocuFiller.csproj
  ```
- Optional console harnesses for manual verification
  ```powershell
  dotnet run --project .\TestConsole\TestConsole.csproj
  dotnet run --project .\TestScanner\TestScanner.csproj
  ```
- Clean
  ```powershell
  dotnet clean
  ```
- Format (if dotnet-format is installed)
  ```powershell
  dotnet format
  ```
- Tests
  - No test projects were found in this repo at the time of writing. Running `dotnet test` will not execute tests. Use the console harnesses above for functional checks.

High-level architecture
- UI (WPF, MVVM)
  - App.xaml/App.xaml.cs bootstraps DI, logging, and global exception handling, then shows MainWindow.
  - Views: MainWindow.xaml (primary UI), Views/JsonEditorWindow.xaml (JSON editor).
  - ViewModels: MainWindowViewModel orchestrates the workflow via ICommand bindings (browse template/data, preview, validate, start/cancel processing, folder mode processing). Updates progress via IProgressReporter.
  - Converters: Converters/StringToVisibilityConverter aids UI visibility bindings.
- Services (application/core logic)
  - Document processing: Services/DocumentProcessorService uses DocumentFormat.OpenXml to replace content control values by matching Word content control tags against JSON keys. It copies the template to an output file, opens it in write mode, replaces text (preserving paragraphs/line breaks), and adds Word comments summarizing changes (tag, old value, new value, timestamp). Reports progress via IProgressReporter.
  - OpenXML utilities: Services/OpenXmlDocumentHandler provides lower-level helpers for validating templates, enumerating content controls, and single-document processing.
  - Data parsing and file ops: Services/DataParserService reads JSON and builds preview/statistics; Services/FileService abstracts IO (exists/copy/ensure directories); Services/DirectoryManager and Services/FileScanner support folder-mode processing.
  - Validation and editing: Services/KeywordValidationService and Services/JsonEditorService support keyword validation and JSON editing UI flows.
- Models
  - ProcessRequest/ProcessResult encapsulate batch processing parameters and outcomes (success/fail counts, generated files, errors/warnings, timing).
  - ContentControlData describes discovered content controls (Tag/Title/Type/Value). DataStatistics summarizes JSON file characteristics (record count, field count, size).
- Utilities and cross-cutting concerns
  - Utils/LoggerConfiguration wires Microsoft.Extensions.Logging providers; GlobalExceptionHandler subscribes to AppDomain and Dispatcher unhandled exceptions. Logs are written to Logs/ with retention controlled by App.config.
- Configuration (App.config)
  - Keys: LogLevel, LogRetentionDays, LogFilePath, MaxFileSize, SupportedExtensions (.docx,.dotx), DefaultOutputDirectory, MaxConcurrentProcessing, ProcessingTimeout, and UI toggles.
- Default folders
  - Logs/, Output/, Backup/, Templates/, Examples/ are created and/or copied to output on build as defined in DocuFiller.csproj.

Primary user flow
1) User selects a Word template (.docx/.dotx) and a JSON data file. MainWindowViewModel validates template and previews parsed data/statistics.
2) On Start, DocumentProcessorService validates the template, parses JSON into a list of dictionaries, ensures Output exists, and iterates each record:
   - Copies template to a timestamped output filename.
   - For each content control (by Tag), finds the matching JSON key; clears existing text and inserts formatted runs/paragraphs (supports multi-line content).
   - Adds a Word comment with tag, old value, new value, and timestamp.
   - Saves document and reports progress.
3) Progress and completion/failure statistics are displayed in the UI; errors/warnings are logged.

Repository-specific rules and notes
- From .trae/rules/project_rules.md: prefer emitting debug/diagnostic logs while debugging to aid problem localization. The ViewModel and services already produce verbose logs; keep them enabled when investigating issues.
- Solution vs projects: DocuFiller.sln currently includes the DocuFiller WPF app; auxiliary console projects (TestConsole, TestScanner) are not part of the solution but can be run directly with `dotnet run --project` as shown above.

Key references
- README.md: Includes environment requirements, example JSON data format, and high-level usage steps in Chinese.
- DocuFiller.csproj: Targets net8.0-windows7.0, UseWPF=true; copies Templates/ and Examples/ to output and creates Logs/, Output/, Backup/ on build; references DocumentFormat.OpenXml and Newtonsoft.Json along with Microsoft.Extensions.* packages.
