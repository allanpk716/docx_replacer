# S02: 移除 JSON 数据源、转换器、KeywordEditorUrl、Tools

**Goal:** 移除 JSON 数据源（IDataParser/DataParserService）、JSON→Excel 转换器（ConverterWindow/ExcelToWordConverterService）、KeywordEditorUrl 配置和 UI 入口、以及 Tools 目录下全部 10 个诊断工具项目。清理后 Excel 为唯一数据源。
**Demo:** dotnet build 通过，Excel 为唯一数据源，主窗口无 JSON/转换器/外部编辑器入口，Tools 目录已删除

## Must-Haves

- dotnet build 通过（0 errors）
- grep 确认无 IDataParser、DataParserService、IExcelToWordConverter、ExcelToWordConverterService、ConverterWindow、KeywordEditorUrl 残留引用
- Tools 目录不存在
- MainWindow.xaml 无"工具"选项卡、无 JSON 拖拽提示文本、无转换器入口
- MainWindowViewModel 无 IDataParser 字段、无 DataFileType.Json、无 OpenConverterCommand
- DocumentProcessorService 无 IDataParser 依赖、无 JSON 处理分支
- appsettings.json 无 KeywordEditorUrl

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: S01's clean App.xaml.cs DI registrations (no update services), clean MainWindowViewModel (no update logic), clean MainWindow.xaml (no update UI)
- New wiring introduced in this slice: removes IDataParser from DI and DocumentProcessorService constructor, removes IExcelToWordConverter from DI, removes ConverterWindow from DI, removes KeywordEditorUrl from UISettings
- What remains before the milestone is truly usable end-to-end: S03 must fix broken tests (tests that reference IDataParser, MockDataParser) and sync documentation

## Verification

- Drag-and-drop for data files will only accept .xlsx (previously accepted .json and .xlsx)
- "工具" tab removed from MainWindow TabControl
- DataFileType enum simplified to Excel only
- DocumentProcessorService.ProcessDocumentsAsync no longer has JSON fallback branch

## Tasks

- [x] **T01: 移除 JSON 数据源管道（IDataParser、DataParserService、DocumentProcessorService JSON 分支、MainWindowViewModel JSON 逻辑）** `est:45m`
  删除 IDataParser 接口和 DataParserService 实现。从 DocumentProcessorService 构造函数移除 IDataParser 参数、移除 ProcessJsonDataAsync 方法、移除所有 JSON 处理分支（ProcessDocumentsAsync 和 ProcessFolderAsync 中的 else 分支）。从 MainWindowViewModel 移除 IDataParser 字段、构造函数参数、PreviewDataAsync 中的 JSON else 分支、DataFileType.Json 枚举值（保留 DataFileType.Excel），简化 DataFileTypeDisplay 属性。从 App.xaml.cs 移除 IDataParser/DataParserService DI 注册。更新 BrowseData 文件对话框过滤器移除 .json。从 MainWindow.xaml.cs 移除 JSON 文件相关的拖拽验证逻辑（IsJsonFile、IsValidJsonFile 方法，DataFileDropBorder_Drop 中的 JSON 验证分支），更新拖拽提示文本和 IsDataFile 方法仅支持 .xlsx。从 MainWindow.xaml 更新拖拽提示文本从“拖拽 JSON 或 Excel 数据文件到此处”改为仅 Excel。
  - Files: `Services/Interfaces/IDataParser.cs`, `Services/DataParserService.cs`, `Services/DocumentProcessorService.cs`, `ViewModels/MainWindowViewModel.cs`, `App.xaml.cs`, `MainWindow.xaml.cs`, `MainWindow.xaml`, `Exceptions/DataParsingException.cs`
  - Verify: cd DocuFiller && grep -c "IDataParser\|DataParserService\|_dataParser\|ProcessJsonData\|DataFileType\.Json" Services/DocumentProcessorService.cs ViewModels/MainWindowViewModel.cs App.xaml.cs && echo 'Should be 0'

- [x] **T02: 移除转换器窗口、KeywordEditorUrl 和工具选项卡** `est:30m`
  删除 ConverterWindow 视图文件（Views/ConverterWindow.xaml、Views/ConverterWindow.xaml.cs）、ConverterWindowViewModel（ViewModels/ConverterWindowViewModel.cs）、ExcelToWordConverterService（Services/ExcelToWordConverterService.cs）、IExcelToWordConverter 接口（Services/Interfaces/IExcelToWordConverter.cs）。从 App.xaml.cs 移除 IExcelToWordConverter、ConverterWindowViewModel、ConverterWindow 的 DI 注册。从 MainWindowViewModel 移除 OpenConverterCommand 声明、初始化、OpenConverter 方法。从 MainWindow.xaml 移除整个“工具”TabItem（包含 JSON转Excel 转换工具 UI）。从 MainWindow.xaml.cs 移除 ConverterHyperlink_Click 事件处理器。从 appsettings.json 移除 KeywordEditorUrl 配置项。从 Configuration/AppSettings.cs 中 UISettings 类移除 KeywordEditorUrl 属性。从 MainWindow.xaml.cs 移除 KeywordEditorHyperlink_Click 事件处理器和 _uiSettings 字段（如果仅用于 KeywordEditorUrl），同时移除构造函数中 IOptions<UISettings> 参数（如果仅用于 KeywordEditorUrl——需检查是否有其他用途）及其 using 语句。
  - Files: `Views/ConverterWindow.xaml`, `Views/ConverterWindow.xaml.cs`, `ViewModels/ConverterWindowViewModel.cs`, `Services/ExcelToWordConverterService.cs`, `Services/Interfaces/IExcelToWordConverter.cs`, `App.xaml.cs`, `ViewModels/MainWindowViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `appsettings.json`, `Configuration/AppSettings.cs`
  - Verify: cd DocuFiller && grep -c "IExcelToWordConverter\|ConverterWindow\|ConverterWindowViewModel\|OpenConverter\|KeywordEditor\|工具" App.xaml.cs ViewModels/MainWindowViewModel.cs MainWindow.xaml.cs MainWindow.xaml && echo 'Should be 0'

- [x] **T03: 删除 Tools 目录并验证构建通过** `est:15m`
  删除 Tools 目录（包含 10 个诊断工具子目录：CompareDocumentStructure、ControlRelationshipAnalyzer、DeepDiagnostic、DiagnoseTableStructure、E2ETest、ExcelFormattedTestGenerator、ExcelToWordVerifier、StepByStepSimulator、TableCellTest、TableStructureAnalyzer）。从 DocuFiller.csproj 移除 Tools 的 Compile Remove/EmbeddedResource Remove/None Remove 行（目录已不存在）。同时移除 ExcelToWordVerifier 的排除行（也已在 Tools 内或根目录）。运行 dotnet build 确认 0 errors。运行 grep 全面验证无残留引用。
  - Files: `DocuFiller.csproj`, `Tools/`
  - Verify: cd DocuFiller && dotnet build && test ! -d Tools

## Files Likely Touched

- Services/Interfaces/IDataParser.cs
- Services/DataParserService.cs
- Services/DocumentProcessorService.cs
- ViewModels/MainWindowViewModel.cs
- App.xaml.cs
- MainWindow.xaml.cs
- MainWindow.xaml
- Exceptions/DataParsingException.cs
- Views/ConverterWindow.xaml
- Views/ConverterWindow.xaml.cs
- ViewModels/ConverterWindowViewModel.cs
- Services/ExcelToWordConverterService.cs
- Services/Interfaces/IExcelToWordConverter.cs
- appsettings.json
- Configuration/AppSettings.cs
- DocuFiller.csproj
- Tools/
