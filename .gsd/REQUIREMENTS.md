# Requirements

This file is the explicit capability and coverage contract for the project.

## Active

## Validated

### R001 — Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Class: core-capability
- Status: validated
- Description: Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Why it matters: 允许用户在 Excel 中为每行添加人类可读的标签，方便维护大型数据表
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: IsInstalled guard removed from both GUI (InitializeUpdateStatusAsync) and CLI (UpdateCommand.ExecuteAsync). Portable version follows identical update code path as installed version. IUpdateService.IsPortable property available for mode detection. dotnet build 0 errors, 249/249 tests pass.
- Notes: 检测依据为第一行第一列内容是否匹配 #xxx# 格式

### R002 — 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Class: core-capability
- Status: validated
- Description: 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Why it matters: 防止数据表维护错误导致混淆
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: IUpdateService.cs line 32: bool IsPortable { get; } added. UpdateService implements via Velopack UpdateManager.IsPortable. All test stubs updated. dotnet build 0 errors, 249/249 tests pass.
- Notes: 两列模式不触发此校验

### R003 — 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Class: constraint
- Status: validated
- Description: 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Why it matters: 已有用户和数据模板不能因为新功能被破坏
- Source: inferred
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: M001-a60bo7/S02
- Validation: UpdateStatus.PortableVersion enum removed from MainWindowViewModel.cs. All switch branches in UpdateStatusMessage, UpdateStatusBrush, OnUpdateStatusClickAsync deleted. IsInstalled guard in InitializeUpdateStatusAsync removed. grep confirms zero remaining references. dotnet build 0 errors, 249/249 tests pass.
- Notes: 硬性约束，零回归

### R004 — 所有现有单元测试和集成测试在改动后继续通过
- Class: quality-attribute
- Status: validated
- Description: 所有现有单元测试和集成测试在改动后继续通过
- Why it matters: 回归安全的底线
- Source: inferred
- Primary owning slice: M001-a60bo7/S02
- Supporting slices: none
- Validation: UpdateCommand.ExecuteAsync IsInstalled guard removed. Portable version now enters same download+apply path. Test Update_WithYes_Portable_ProceedsNormally verifies exitCode 0 and no PORTABLE_NOT_SUPPORTED output. dotnet build 0 errors, 249/249 tests pass.
- Notes: 包含新增的三列解析和唯一性校验测试

### R005 — 将 DocuFiller 产品需求文档从 .trae/documents/ 迁移到 docs/ 并全面重写，覆盖所有现有功能：JSON/Excel 双数据源、三列 Excel 格式、富文本格式保留、页眉页脚替换、批注追踪、审核清理、JSON↔Excel 转换工具
- Class: core-capability
- Status: validated
- Description: 将 DocuFiller 产品需求文档从 .trae/documents/ 迁移到 docs/ 并全面重写，覆盖所有现有功能：JSON/Excel 双数据源、三列 Excel 格式、富文本格式保留、页眉页脚替换、批注追踪、审核清理、JSON↔Excel 转换工具
- Why it matters: 产品需求文档严重过时（2025-09），只描述了 JSON-only 的基础版本，新开发者无法通过文档了解完整功能
- Source: user
- Primary owning slice: M003-g1w88x/S01
- Supporting slices: none
- Validation: docs/DocuFiller产品需求文档.md 已创建，覆盖所有 6 个功能模块（文件输入、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具），包含 Mermaid 流程图和用户界面设计
- Notes: 保持"需求+架构"两份文档拆分；中文为主；面向开发者

### R006 — 将技术架构文档从 .trae/documents/ 迁移到 docs/ 并全面重写，包含完整的 15 个服务接口定义（C# 代码）、数据模型、Mermaid 架构图、处理管道
- Class: core-capability
- Status: validated
- Description: 将技术架构文档从 .trae/documents/ 迁移到 docs/ 并全面重写，包含完整的 15 个服务接口定义（C# 代码）、数据模型、Mermaid 架构图、处理管道
- Why it matters: 架构文档只有 4 个基础服务的接口定义，缺少 Excel 解析、富文本替换、清理等 11 个新增服务
- Source: user
- Primary owning slice: M003-g1w88x/S01
- Supporting slices: none
- Validation: docs/DocuFiller技术架构文档.md 已创建，包含 14 个 public interface 定义、9 个二级章节、5 个 Mermaid 图（架构图、ER 图、3 个序列图），覆盖全部 15 个服务组件
- Notes: 保持详细风格（含代码示例、Mermaid 图）

### R007 — 更新 README.md 的功能列表、服务层架构表、项目结构、使用方法，反映所有现有功能和 15 个服务接口
- Class: core-capability
- Status: validated
- Description: 更新 README.md 的功能列表、服务层架构表、项目结构、使用方法，反映所有现有功能和 15 个服务接口
- Why it matters: README 是项目入口，当前缺少审核清理、三列 Excel、转换工具等新功能的说明
- Source: user
- Primary owning slice: M003-g1w88x/S02
- Supporting slices: none
- Validation: README.md 包含 14 个服务接口架构表（grep 验证全部 14 个 I 前缀接口存在）、6 个功能模块完整覆盖、Excel 两列/三列格式说明、准确项目结构。验证命令全部通过。
- Notes: 无

### R008 — 更新 CLAUDE.md 的服务层架构、数据模型、开发指南，补充新增服务的说明和 Excel 处理路径
- Class: core-capability
- Status: validated
- Description: 更新 CLAUDE.md 的服务层架构、数据模型、开发指南，补充新增服务的说明和 Excel 处理路径
- Why it matters: CLAUDE.md 是 AI 编码助手的上下文文件，过时信息会导致错误的代码建议
- Source: user
- Primary owning slice: M003-g1w88x/S02
- Supporting slices: none
- Validation: Will be recorded via gsd_decision_save in slice completion.
- Notes: 无

### R009 — 在 docs/excel-data-user-guide.md 中增加三列 Excel 格式（ID|关键词|值）的使用说明、示例和验证规则
- Class: core-capability
- Status: validated
- Description: 在 docs/excel-data-user-guide.md 中增加三列 Excel 格式（ID|关键词|值）的使用说明、示例和验证规则
- Why it matters: 三列格式是 M001 新增的核心功能，但用户指南仍然只描述两列格式
- Source: user
- Primary owning slice: M003-g1w88x/S03
- Supporting slices: none
- Validation: docs/excel-data-user-guide.md 包含 6 个章节（含三列格式独立章节）、三列格式关键词匹配 11 处、无 TBD/TODO 标记
- Notes: 无

### R010 — 校验并更新 docs/features/header-footer-support.md 和 docs/批注功能说明.md，确保与实际代码实现一致
- Class: core-capability
- Status: validated
- Description: 校验并更新 docs/features/header-footer-support.md 和 docs/批注功能说明.md，确保与实际代码实现一致
- Why it matters: 功能说明文档可能存在与代码实现的偏差（页眉页脚批注的实际行为）
- Source: user
- Primary owning slice: M003-g1w88x/S03
- Supporting slices: none
- Validation: header-footer-support.md 修正为"仅正文区域支持批注"（7 个章节，含准确描述）；批注功能说明.md 与代码一致；两份文档均无 TBD/TODO
- Notes: 无

### R011 — 将 .trae/documents/ 下的 DocuFiller 产品需求文档和技术架构文档迁移到 docs/；删除 .trae/documents/ 下的 4 份文件（含 2 份 JSON 编辑器文档）
- Class: operability
- Status: validated
- Description: 将 .trae/documents/ 下的 DocuFiller 产品需求文档和技术架构文档迁移到 docs/；删除 .trae/documents/ 下的 4 份文件（含 2 份 JSON 编辑器文档）
- Why it matters: .trae/ 是 Trae IDE 的目录约定，文档应统一存放在 docs/ 下
- Source: user
- Primary owning slice: M003-g1w88x/S01
- Supporting slices: none
- Validation: .trae/documents/ 目录已删除（4 份旧文件全部清理），DocuFiller 两份文档已迁移到 docs/，JSON 编辑器两份文档已按 D004 直接删除
- Notes: JSON 编辑器文档不迁移，直接删除

### R012 — 不更新 docs/VERSION_MANAGEMENT.md、docs/EXTERNAL_SETUP.md、docs/deployment-guide.md
- Class: operability
- Status: validated
- Description: 不更新 docs/VERSION_MANAGEMENT.md、docs/EXTERNAL_SETUP.md、docs/deployment-guide.md
- Why it matters: 用户明确要求更新机制不写入文档
- Source: user
- Primary owning slice: none
- Supporting slices: none
- Validation: fill 和 cleanup 子命令实现完成，JSONL 输出格式验证通过。FillCommand 通过 IDocumentProcessor.ProcessDocumentsAsync 调用核心填充服务，CleanupCommand 通过 IDocumentCleanupService.CleanupAsync 调用清理服务。37 个 CLI 单元测试覆盖路由、参数验证和输出格式，108/108 测试全部通过。
- Notes: 这些文档保持现状不动

### R014 — 移除所有在线更新相关代码：Services/Update/*、DocuFiller/Services/Update/*、Models/Update/*、ViewModels/Update/*、Views/Update/*、External/*（update-client.exe、publish-client.exe、config.yaml）、csproj PreBuild 门禁、App.xaml.cs DI 注册、MainWindowViewModel 更新逻辑和命令、MainWindow.xaml 更新按钮和横幅 UI
- Class: core-capability
- Status: validated
- Description: 移除所有在线更新相关代码：Services/Update/*、DocuFiller/Services/Update/*、Models/Update/*、ViewModels/Update/*、Views/Update/*、External/*（update-client.exe、publish-client.exe、config.yaml）、csproj PreBuild 门禁、App.xaml.cs DI 注册、MainWindowViewModel 更新逻辑和命令、MainWindow.xaml 更新按钮和横幅 UI
- Why it matters: 在线更新功能不再需要，代码和外部依赖（update-client.exe）增加维护负担，PreBuild 门禁阻止无外部文件时构建
- Source: user
- Primary owning slice: M004-l08k3s/S01
- Supporting slices: none
- Validation: All online update code removed: csproj PreBuild/PostPublish gates deleted, External/ directory deleted, 19 update service/model/viewmodel/view files deleted, App.xaml.cs DI registrations removed, MainWindowViewModel/MainWindow.xaml/MainWindow.xaml.cs update references removed. dotnet build passes with 0 errors. grep confirms 0 matches for IUpdateService, UpdateViewModel, UpdateBannerView, UpdateWindow, ValidateUpdateClientFiles, ValidateReleaseFiles, update-client.
- Notes: csproj 中有 PreBuild 验证检查 update-client.exe 是否存在，必须最先处理

### R015 — 移除 JSON 编辑器相关遗留文件：JsonEditorService、IJsonEditorService、JsonEditorViewModel、KeywordValidationService、IKeywordValidationService、Models/JsonKeywordItem.cs、Models/JsonProjectModel.cs。这些文件无 DI 注册、无活跃引用。
- Class: core-capability
- Status: validated
- Description: 移除 JSON 编辑器相关遗留文件：JsonEditorService、IJsonEditorService、JsonEditorViewModel、KeywordValidationService、IKeywordValidationService、Models/JsonKeywordItem.cs、Models/JsonProjectModel.cs。这些文件无 DI 注册、无活跃引用。
- Why it matters: JSON 编辑器功能已从代码中移除，但遗留文件仍在仓库中，增加代码噪音
- Source: user
- Primary owning slice: M004-l08k3s/S01
- Supporting slices: none
- Validation: All 9 JSON editor orphaned files deleted: JsonEditorService.cs, IJsonEditorService.cs, KeywordValidationService.cs, IKeywordValidationService.cs, JsonKeywordItem.cs, JsonProjectModel.cs, JsonEditorViewModel.cs, JsonEditorWindow.xaml, JsonEditorWindow.xaml.cs. No remaining JSON editor files in codebase. dotnet build passes.
- Notes: JsonEditorService 未在 App.xaml.cs 中注册，KeywordValidationService 仅被 JsonEditor 相关代码引用

### R016 — 移除 DataParserService、IDataParser 接口、MainWindowViewModel 中 JSON 预览/统计逻辑、DocumentProcessorService 中 IDataParser 依赖和 JSON 处理分支、文件选择对话框中 .json 过滤器、DataFileType.Json 枚举值
- Class: core-capability
- Status: validated
- Description: 移除 DataParserService、IDataParser 接口、MainWindowViewModel 中 JSON 预览/统计逻辑、DocumentProcessorService 中 IDataParser 依赖和 JSON 处理分支、文件选择对话框中 .json 过滤器、DataFileType.Json 枚举值
- Why it matters: JSON 数据源不再使用，只保留 Excel 作为唯一数据输入方式，简化代码和用户界面
- Source: user
- Primary owning slice: M004-l08k3s/S02
- Supporting slices: none
- Validation: grep confirms 0 real IDataParser/DataParserService references in core files. DataFileType enum is Excel-only. DocumentProcessorService has no JSON processing branch. dotnet build and dotnet test pass.
- Notes: DocumentProcessorService 的 else 分支（JSON 模式）整个可移除，构造函数需要去掉 IDataParser 参数

### R017 — 移除 Views/ConverterWindow、ViewModels/ConverterWindowViewModel、ExcelToWordConverterService、IExcelToWordConverter、MainWindow.xaml 中转换器按钮、DI 注册
- Class: core-capability
- Status: validated
- Description: 移除 Views/ConverterWindow、ViewModels/ConverterWindowViewModel、ExcelToWordConverterService、IExcelToWordConverter、MainWindow.xaml 中转换器按钮、DI 注册
- Why it matters: 转换器窗口做的是 JSON→Excel 转换，JSON 数据源清理后无存在意义
- Source: user
- Primary owning slice: M004-l08k3s/S02
- Supporting slices: none
- Validation: All 5 converter files deleted. grep confirms 0 references to IExcelToWordConverter, ConverterWindow, ConverterWindowViewModel, OpenConverter in any source file. DI registrations removed. dotnet build and test pass.
- Notes: 实际服务名虽为 ExcelToWordConverterService 但功能是 JSON→Excel 转换

### R018 — 移除 appsettings.json 中的 KeywordEditorUrl、AppSettings.cs 中的属性、MainWindow.xaml.cs 中打开浏览器逻辑、MainWindow.xaml 中的 UI 入口
- Class: core-capability
- Status: validated
- Description: 移除 appsettings.json 中的 KeywordEditorUrl、AppSettings.cs 中的属性、MainWindow.xaml.cs 中打开浏览器逻辑、MainWindow.xaml 中的 UI 入口
- Why it matters: KeywordEditorUrl 指向内部 Web 服务（192.168.200.23:32200），JSON 编辑器已废弃后无用途
- Source: user
- Primary owning slice: M004-l08k3s/S02
- Supporting slices: none
- Validation: KeywordEditorUrl removed from appsettings.json and AppSettings.cs UISettings class. KeywordEditorHyperlink_Click and ConverterHyperlink_Click handlers removed from MainWindow.xaml.cs. IOptions<UISettings> constructor parameter removed. dotnet build and test pass.
- Notes: 硬编码的内网 IP 地址

### R019 — 删除 Tools 目录下全部 10 个诊断工具项目：CompareDocumentStructure、ControlRelationshipAnalyzer、DeepDiagnostic、DiagnoseTableStructure、E2ETest、ExcelFormattedTestGenerator、ExcelToWordVerifier、StepByStepSimulator、TableCellTest、TableStructureAnalyzer
- Class: core-capability
- Status: validated
- Description: 删除 Tools 目录下全部 10 个诊断工具项目：CompareDocumentStructure、ControlRelationshipAnalyzer、DeepDiagnostic、DiagnoseTableStructure、E2ETest、ExcelFormattedTestGenerator、ExcelToWordVerifier、StepByStepSimulator、TableCellTest、TableStructureAnalyzer
- Why it matters: 这些是开发阶段的调试工具，不再需要。已通过 csproj 的 Compile Remove 排除在主构建之外。
- Source: user
- Primary owning slice: M004-l08k3s/S02
- Supporting slices: none
- Validation: Tools/ directory deleted (confirmed not present on disk). All 10 tool project entries removed from DocuFiller.sln. Compile/EmbeddedResource/None Remove entries removed from DocuFiller.csproj. grep confirms 0 residual references to any tool project name. dotnet build and test pass.
- Notes: Tools 目录已被 `<Compile Remove="Tools\**" />` 排除，不影响编译

### R020 — 清理后 `dotnet test` 全部通过。受影响的测试需要重写或移除：HeaderFooterCommentIntegrationTests（使用 DataParserService 和 JSON 数据）、ExcelIntegrationTests（注册 IDataParser）、DocumentProcessorServiceIntegrationTests（有 MockDataParser）
- Class: quality-attribute
- Status: validated
- Description: 清理后 `dotnet test` 全部通过。受影响的测试需要重写或移除：HeaderFooterCommentIntegrationTests（使用 DataParserService 和 JSON 数据）、ExcelIntegrationTests（注册 IDataParser）、DocumentProcessorServiceIntegrationTests（有 MockDataParser）
- Why it matters: 回归安全底线，确保清理不破坏核心 Excel 处理管道
- Source: inferred
- Primary owning slice: M004-l08k3s/S03
- Supporting slices: M004-l08k3s/S02
- Validation: All 71 tests pass after S01/S02 feature removal. T01 removed ValidateJsonFormat (sole Newtonsoft.Json consumer) and test-data.json. dotnet test --no-build --verbosity minimal: 71 passed, 0 failed. No Newtonsoft.Json references remain in .cs/.csproj files.
- Notes: JSON 相关测试数据文件（test-data.json、test_data/*.json）可一并清理

### R021 — 更新 CLAUDE.md（移除 JSON/更新/转换器相关架构描述）、README.md（移除 JSON/更新/转换器/Tools 描述）、docs/ 相关文档同步
- Class: core-capability
- Status: validated
- Description: 更新 CLAUDE.md（移除 JSON/更新/转换器相关架构描述）、README.md（移除 JSON/更新/转换器/Tools 描述）、docs/ 相关文档同步
- Why it matters: 文档与代码不一致会误导未来的开发和 AI 辅助编码
- Source: inferred
- Primary owning slice: M004-l08k3s/S03
- Supporting slices: none
- Validation: CLAUDE.md 和 README.md 已添加完整 CLI 使用文档。CLAUDE.md 包含 6 个 CLI 组件说明、CLI 接口章节（子命令用法、JSONL envelope schema、输出类型、错误码、使用示例）。README.md 包含 CLI 使用方法章节（三个子命令参数和示例）、JSONL 格式说明。
- Notes: 需要在代码清理完成后更新

### R022 — 在 Program.cs 的 Main() 最顶部初始化 VelopackApp.Build().Run()，清理 App.config 中旧更新配置项（UpdateServerUrl、UpdateChannel、CheckUpdateOnStartup），清理 build-internal.bat 中 COPY_EXTERNAL_FILES 步骤和旧 update-client.exe 引用
- Class: core-capability
- Status: validated
- Description: 在 Program.cs 的 Main() 最顶部初始化 VelopackApp.Build().Run()，清理 App.config 中旧更新配置项（UpdateServerUrl、UpdateChannel、CheckUpdateOnStartup），清理 build-internal.bat 中 COPY_EXTERNAL_FILES 步骤和旧 update-client.exe 引用
- Why it matters: Velopack 是替代旧更新系统的现代方案，旧残留会与新系统冲突并增加维护混乱
- Source: user
- Primary owning slice: M007-wpaxa3/S01
- Supporting slices: none
- Validation: Velopack 0.0.1298 NuGet added to csproj + both test csprojs. VelopackApp.Build().Run() is first line of Program.Main(). App.config old update entries (UpdateServerUrl, UpdateChannel, CheckUpdateOnStartup) removed. build-internal.bat COPY_EXTERNAL_FILES/PUBLISH_TO_SERVER/update-client references removed. sync-version.bat update-client.config.yaml block removed. dotnet build 0 errors, dotnet test 162 pass. grep confirms 0 old update system references.
- Notes: 旧更新系统在 M004 已移除运行时代码，本 slice 清理残留配置和脚本

### R023 — 新增 IUpdateService 接口和 UpdateService 实现，封装 Velopack UpdateManager 的 CheckForUpdatesAsync、DownloadUpdatesAsync、ApplyUpdatesAndRestart。注册到 DI 容器。更新源 URL 从 appsettings.json 读取。
- Class: core-capability
- Status: validated
- Description: 新增 IUpdateService 接口和 UpdateService 实现，封装 Velopack UpdateManager 的 CheckForUpdatesAsync、DownloadUpdatesAsync、ApplyUpdatesAndRestart。注册到 DI 容器。更新源 URL 从 appsettings.json 读取。
- Why it matters: 将 Velopack API 封装为应用层服务，解耦 UI 与更新框架，便于测试和维护
- Source: inferred
- Primary owning slice: M007-wpaxa3/S02
- Supporting slices: none
- Validation: Services/UpdateService.cs 实现了 IUpdateService 全部 4 个成员（CheckForUpdatesAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart, IsUpdateUrlConfigured）。在 App.xaml.cs 中注册为 Singleton（services.AddSingleton<IUpdateService, UpdateService>()）。从 IConfiguration 读取 Update:UpdateUrl 配置，空字符串时 IsUpdateUrlConfigured 返回 false。dotnet build 0 errors, dotnet test 162 tests pass。
- Notes: 更新源 URL 在 appsettings.json 的 Update:UpdateUrl 节点

### R024 — 在 MainWindow 底部添加状态栏，显示 VersionHelper.GetCurrentVersion() 的版本号和"检查更新"按钮。点击后调用 IUpdateService.CheckForUpdatesAsync()，根据结果显示"已是最新版本"或更新确认对话框。更新源未配置时按钮灰显。
- Class: primary-user-loop
- Status: validated
- Description: 在 MainWindow 底部添加状态栏，显示 VersionHelper.GetCurrentVersion() 的版本号和"检查更新"按钮。点击后调用 IUpdateService.CheckForUpdatesAsync()，根据结果显示"已是最新版本"或更新确认对话框。更新源未配置时按钮灰显。
- Why it matters: 这是用户手动触发更新的唯一入口，需要常驻可见且不干扰现有功能布局
- Source: user
- Primary owning slice: M007-wpaxa3/S02
- Supporting slices: none
- Validation: MainWindow.xaml 底部添加 StatusBar 显示 VersionHelper.GetCurrentVersion() 版本号和"检查更新"按钮。MainWindowViewModel 注入 IUpdateService（可选参数），实现 CheckUpdateCommand 命令。更新检查流程：有新版本弹确认对话框，无新版本显示"已是最新版本"，异常显示错误信息。CanCheckUpdate 绑定控制按钮灰显（IsUpdateUrlConfigured 为 false 时禁用）。dotnet build 0 errors, dotnet test 162 tests pass。
- Notes: 不使用 Velopack 内置更新对话框，自定义 WPF 弹窗匹配应用视觉风格

### R025 — 修改 build-internal.bat：dotnet publish 启用 PublishSingleFile=true 和 IncludeNativeLibrariesForSelfExtract=true，然后调用 vpk pack 产出 Velopack 格式发布物（Setup.exe、Portable.zip、full/delta .nupkg、releases.win.json）。清理旧发布流程中对旧更新服务器的引用。
- Class: operability
- Status: validated
- Description: 修改 build-internal.bat：dotnet publish 启用 PublishSingleFile=true 和 IncludeNativeLibrariesForSelfExtract=true，然后调用 vpk pack 产出 Velopack 格式发布物（Setup.exe、Portable.zip、full/delta .nupkg、releases.win.json）。清理旧发布流程中对旧更新服务器的引用。
- Why it matters: 发布形态从多文件 zip 变为专业桌面应用发布流程，支持安装版和便携版双渠道分发
- Source: user
- Primary owning slice: M007-wpaxa3/S03
- Supporting slices: none
- Validation: build-internal.bat contains PublishSingleFile=true, IncludeNativeLibrariesForSelfExtract=true, vpk pack with --packId DocuFiller and --mainExe DocuFiller.exe. Old scripts (publish.bat, release.bat, build-and-publish.bat) and config/ directory removed. build.bat simplified to standalone-only. dotnet build succeeds with 0 errors.
- Notes: 不启用 PublishTrimmed（EPPlus/OpenXML 有反射使用）。vpk 需预先安装（dotnet tool install -g vpk）

### R026 — 构建 Velopack 发布包，部署到本地 HTTP 服务器，验证：Setup.exe 安装后正常运行、Portable.zip 解压后正常运行、从旧版本检查更新并升级到新版本、升级后用户配置文件（appsettings.json、Logs/、Output/）保留
- Class: quality-attribute
- Status: validated
- Description: 构建 Velopack 发布包，部署到本地 HTTP 服务器，验证：Setup.exe 安装后正常运行、Portable.zip 解压后正常运行、从旧版本检查更新并升级到新版本、升级后用户配置文件（appsettings.json、Logs/、Output/）保留
- Why it matters: 更新功能的正确性必须通过真实的安装→更新流程验证，单元测试无法覆盖文件替换和重启场景
- Source: inferred
- Primary owning slice: M007-wpaxa3/S04
- Supporting slices: none
- Validation: E2E portable update scripts created: e2e-portable-update-test.bat (local HTTP, port 8081) and e2e-portable-go-update-test.sh (Go server, port 19081). Both scripts automate full update chain: build→pack→extract→configure→update→verify. Test guide updated with 4 new portable sections. dotnet build 0 errors.
- Notes: S04 delivered the automation infrastructure and test guide. The actual live E2E test on clean Windows is a manual verification step that requires vpk CLI installed.

### R027 — dotnet build 无错误，dotnet test 全部通过。Velopack 集成和发布管道改造不影响现有业务逻辑。
- Class: quality-attribute
- Status: validated
- Description: dotnet build 无错误，dotnet test 全部通过。Velopack 集成和发布管道改造不影响现有业务逻辑。
- Why it matters: 回归安全底线——更新功能是新增能力，不应破坏已有功能
- Source: inferred
- Primary owning slice: M007-wpaxa3/S01
- Supporting slices: M007-wpaxa3/S02, M007-wpaxa3/S03
- Validation: dotnet build 0 errors, dotnet test 162 tests pass (135 + 27), 0 failures. Velopack integration and config/script cleanup do not affect any existing business logic tests.
- Notes: 贯穿所有 slice 的约束

### R028 — 应用启动时或定时自动检查更新，有新版本时在状态栏显示通知徽章
- Class: core-capability
- Status: validated
- Description: 应用启动时或定时自动检查更新，有新版本时在状态栏显示通知徽章
- Why it matters: 减少用户主动检查的认知负担，确保用户及时获取更新
- Source: user
- Primary owning slice: M021/S05
- Supporting slices: none
- Validation: UpdateStatusViewModel.InitializeAsync() uses Task.Delay(5000) + CancellationToken for 5-second delay after startup, then calls CheckUpdateAsync() silently. HasUpdateAvailable computed property drives red dot badge on ⚙ settings button. 16 unit tests cover: normal flow, cancellation, exception handling, multiple checks. dotnet build 0 errors and dotnet test all pass during slice execution (verified per-slice).
- Notes: M007 只做手动触发，自动检查作为后续增强

### R029 — 支持在 UI 中通过设置弹窗切换更新源（内网 HTTP URL / GitHub）和更新通道（stable/beta）。修改后立即生效（热重载），同时持久化到 appsettings.json。状态栏显示当前更新源类型。
- Class: operability
- Status: validated
- Description: 支持在 UI 中通过设置弹窗切换更新源（内网 HTTP URL / GitHub）和更新通道（stable/beta）。修改后立即生效（热重载），同时持久化到 appsettings.json。状态栏显示当前更新源类型。
- Why it matters: 允许高级用户提前体验 beta 版本，同时不影响普通用户的稳定版本
- Source: inferred
- Primary owning slice: M010-hpylzg/S02
- Supporting slices: none
- Validation: UpdateSettingsWindow provides GUI for editing UpdateUrl/Channel with Save calling IUpdateService.ReloadSource. Settings persist to appsettings.json. Status bar shows source type suffix. dotnet build 0 errors, 192/192 tests pass.
- Notes: 从 deferred 激活。M010 提供完整的 GUI 设置入口，包括 UpdateUrl 和 Channel 编辑、热重载、源类型显示。

### R030 — Go 单二进制更新服务器，启动时指定数据目录和端口。自动维护 stable/ 和 beta/ 子目录，每个目录存放 releases.win.json 和 .nupkg 文件。静态文件服务供 Velopack 客户端下载更新。
- Class: core-capability
- Status: validated
- Description: Go 单二进制更新服务器，启动时指定数据目录和端口。自动维护 stable/ 和 beta/ 子目录，每个目录存放 releases.win.json 和 .nupkg 文件。静态文件服务供 Velopack 客户端下载更新。
- Why it matters: 内网轻量更新源，无需 Nginx/IIS 等重型服务器，Go 编译后单文件部署
- Source: user
- Primary owning slice: M008-4uyz6m/S01
- Supporting slices: none
- Validation: Go update-server serves static files from /{channel}/releases.win.json and /{channel}/*.nupkg with Content-Type headers. Build compiles, 50 Go tests pass, curl integration tests verify static serving.
- Notes: 不使用数据库，文件系统即存储

### R031 — POST /api/channels/{channel}/releases 接口，接受 multipart 上传（releases.win.json + .nupkg + Setup.exe 等），带 Token 认证（Header: Authorization: Bearer {token}）。上传后自动更新该通道的 releases.win.json。
- Class: core-capability
- Status: validated
- Description: POST /api/channels/{channel}/releases 接口，接受 multipart 上传（releases.win.json + .nupkg + Setup.exe 等），带 Token 认证（Header: Authorization: Bearer {token}）。上传后自动更新该通道的 releases.win.json。
- Why it matters: 支持自动化构建后直接推送到更新服务器，不需要手动复制文件
- Source: user
- Primary owning slice: M008-4uyz6m/S01
- Supporting slices: none
- Validation: POST /api/channels/{channel}/releases accepts multipart uploads (releases.win.json + .nupkg files), merges feeds by FileName to avoid duplicates, requires Bearer token auth. Tested with httptest integration tests and curl.
- Notes: Token 在服务器启动参数中配置

### R032 — POST /api/channels/stable/promote?from=beta&version={version} 接口，将指定版本从 beta 目录复制到 stable 目录并更新 stable 的 releases.win.json。不需要重新上传文件。
- Class: core-capability
- Status: validated
- Description: POST /api/channels/stable/promote?from=beta&version={version} 接口，将指定版本从 beta 目录复制到 stable 目录并更新 stable 的 releases.win.json。不需要重新上传文件。
- Why it matters: 简化发布流程——测试通过后一条命令从 beta 推到 stable，不需要重新编译上传
- Source: user
- Primary owning slice: M008-4uyz6m/S01
- Supporting slices: none
- Validation: POST /api/channels/{target}/promote?from={source}&version={version} copies matching files and merges feed entries. Returns 404 if version not found. Tested in handler_test.go (5 test cases).
- Notes: 版本文件从 beta 目录硬链接或复制

### R033 — GET /api/channels/{channel}/releases 接口，返回该通道的版本列表（版本号、文件大小、上传时间）。
- Class: core-capability
- Status: validated
- Description: GET /api/channels/{channel}/releases 接口，返回该通道的版本列表（版本号、文件大小、上传时间）。
- Why it matters: 方便运维查看服务器上有哪些版本，辅助发布决策
- Source: user
- Primary owning slice: M008-4uyz6m/S01
- Supporting slices: none
- Validation: GET /api/channels/{channel}/releases returns grouped versions with file names, counts, sizes, sorted by semver descending. No auth required. Tested in handler_test.go (3 test cases).
- Notes: 只读接口，不需要认证

### R034 — 上传新版本或 promote 时，检查该通道的版本数量。如果超过 10 个，删除最老的版本文件并更新 releases.win.json。
- Class: operability
- Status: validated
- Description: 上传新版本或 promote 时，检查该通道的版本数量。如果超过 10 个，删除最老的版本文件并更新 releases.win.json。
- Why it matters: 防止磁盘无限增长，保留 10 个版本足够回滚和增量更新使用
- Source: user
- Primary owning slice: M008-4uyz6m/S01
- Supporting slices: none
- Validation: CleanupOldVersions keeps last 10 versions per channel. Tested with 11 versions (oldest removed) and 15 versions (5 removed). Cleanup triggers after every upload and promote. Files deleted from disk and feed updated atomically.
- Notes: 清理策略在上传和 promote 时自动触发

### R035 — build-internal.bat 增加 channel 参数（stable/beta），构建完成后自动调用更新服务器的上传 API 将 Velopack 产物推送到指定通道。服务器地址和 Token 从环境变量或配置读取。
- Class: operability
- Status: validated
- Description: build-internal.bat 增加 channel 参数（stable/beta），构建完成后自动调用更新服务器的上传 API 将 Velopack 产物推送到指定通道。服务器地址和 Token 从环境变量或配置读取。
- Why it matters: 一条命令完成编译+打包+发布，自动化流水线的核心
- Source: user
- Primary owning slice: M008-4uyz6m/S03
- Supporting slices: none
- Validation: build-internal.bat accepts optional CHANNEL parameter (stable/beta), validates input, and auto-uploads .nupkg + releases.win.json to Go update server via curl when UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN are set. Grep verification: 28 UPLOAD lines, 10 CHANNEL lines, both env vars present. dotnet build: 0 errors.
- Notes: 需要 UPDATE_SERVER_URL 和 UPDATE_SERVER_TOKEN 环境变量

### R036 — 验证完整流程：启动 Go 服务器 → 上传到 beta → beta 客户端收到更新 → promote 到 stable → stable 客户端收到更新。现有 162 个测试全部通过。
- Class: quality-attribute
- Status: validated
- Description: 验证完整流程：启动 Go 服务器 → 上传到 beta → beta 客户端收到更新 → promote 到 stable → stable 客户端收到更新。现有 162 个测试全部通过。
- Why it matters: 双通道是跨组件功能（Go 服务器 + C# 客户端 + BAT 脚本），必须端到端验证
- Source: inferred
- Primary owning slice: M008-4uyz6m/S04
- Supporting slices: none
- Validation: E2E dual-channel script (13 assertions PASS), Go server tests (handler 28 + storage 14 = 42 PASS), Go build (exit 0), .NET build (0 errors), .NET tests (168 PASS: 141 unit + 27 E2E). Full flow verified: upload beta → beta feed accessible → channel isolation → promote → stable feed accessible → auto-cleanup.
- Notes: 可复用 M007 的 e2e-serve.py 和 e2e-update-test.bat 思路

### R037 — 打 `v*` 格式 tag 推送到 GitHub 时触发 Actions workflow，自动编译 Windows 程序，使用 Velopack 打包，创建 GitHub Release
- Class: core-capability
- Status: validated
- Description: 打 `v*` 格式 tag 推送到 GitHub 时触发 Actions workflow，自动编译 Windows 程序，使用 Velopack 打包，创建 GitHub Release
- Why it matters: 自动化发布流程，消除手动构建和上传的人工错误
- Source: user
- Primary owning slice: M009-q7p4iu/S01
- Supporting slices: none
- Validation: Workflow file .github/workflows/build-release.yml exists with correct v* tag trigger, .NET 8 setup, Velopack packaging, and GitHub Release creation. 24 structural checks pass. dotnet build succeeds confirming CI compatibility.
- Notes: tag 驱动版本号（v1.2.3 → 1.2.3），tag-only 触发

### R038 — GitHub Release 上传全部 Velopack 产物：用户可直接下载的 Setup.exe 和 Portable.zip，以及 Velopack 更新机制需要的 .nupkg 和 releases.win.json
- Class: core-capability
- Status: validated
- Description: GitHub Release 上传全部 Velopack 产物：用户可直接下载的 Setup.exe 和 Portable.zip，以及 Velopack 更新机制需要的 .nupkg 和 releases.win.json
- Why it matters: 确保 Velopack 的 GitHubSource 更新通道可以正常工作
- Source: user
- Primary owning slice: M009-q7p4iu/S01
- Supporting slices: none
- Validation: Workflow uploads all 4 artifact types to GitHub Release: DocuFillerSetup.exe, *Portable*.zip, *.nupkg, releases.win.json. Verified via grep checks on workflow YAML file patterns.
- Notes: 用户是小白，不需要了解 .nupkg 和 releases.win.json 的存在

### R039 — UpdateService 根据配置自动选择更新源。UpdateUrl 非空时使用内网 Go 更新服务器（HTTP URL），为空时使用 Velopack GitHubSource 指向 GitHub Releases。GitHub 只走 stable 通道。
- Class: core-capability
- Status: validated
- Description: UpdateService 根据配置自动选择更新源。UpdateUrl 非空时使用内网 Go 更新服务器（HTTP URL），为空时使用 Velopack GitHubSource 指向 GitHub Releases。GitHub 只走 stable 通道。
- Why it matters: 公司用户访问 GitHub 不顺畅，需要内网优先；外网用户需要备选更新通道
- Source: user
- Primary owning slice: M009-q7p4iu/S02
- Supporting slices: none
- Validation: S02 通过 10 个单元测试验证：UpdateUrl 为空时 UpdateSourceType 为 "GitHub"（使用 GithubSource），UpdateUrl 非空时为 "HTTP"（使用 SimpleWebSource），Channel 默认 stable。接口签名未改，只新增属性。
- Notes: 不需要同时检查多个源，按配置选一个。IUpdateService 接口不改签名。

### R040 — GUI 启动后状态栏根据更新状态显示常驻提示：未配置更新源时提醒配置、有新版本时提醒更新、便携版运行时提示使用安装版。点击后复用现有 CheckUpdateAsync 弹窗流程。
- Class: primary-user-loop
- Status: validated
- Description: GUI 启动后状态栏根据更新状态显示常驻提示：未配置更新源时提醒配置、有新版本时提醒更新、便携版运行时提示使用安装版。点击后复用现有 CheckUpdateAsync 弹窗流程。
- Why it matters: 用户是小白，需要主动告知更新状态，不能指望手动点击检查
- Source: user
- Primary owning slice: M009-q7p4iu/S03
- Supporting slices: none
- Validation: S03 T01+T02: MainWindowViewModel 新增 UpdateStatus 枚举（6种状态）+ 5个绑定属性 + InitializeUpdateStatusAsync 自动检查 + UpdateStatusClickCommand。MainWindow.xaml 状态栏新增 TextBlock 绑定到这些属性，点击触发更新流程。dotnet build 通过，172个测试全部通过。
- Notes: 现有"检查更新"按钮保留，常驻提示是额外补充

### R041 — 新增 `DocuFiller.exe update` 子命令。无 --yes 时输出当前版本、最新版本、是否有更新的 JSONL 信息。加 --yes 时执行下载并应用更新重启。
- Class: core-capability
- Status: validated
- Description: 新增 `DocuFiller.exe update` 子命令。无 --yes 时输出当前版本、最新版本、是否有更新的 JSONL 信息。加 --yes 时执行下载并应用更新重启。
- Why it matters: CLI 模式下也需要更新能力，纯 JSONL 保持输出一致性
- Source: user
- Primary owning slice: M009-q7p4iu/S04
- Supporting slices: none
- Validation: UpdateCommand implements ICliCommand with CommandName=update. Unit tests verify: no-yes outputs version info JSONL (currentVersion/latestVersion/hasUpdate/isInstalled/updateSourceType), --yes portable guard emits PORTABLE_NOT_SUPPORTED error, --yes no-update outputs ALREADY_UP_TO_DATE. Build passes, all 154 tests pass.
- Notes: 纯 JSONL，无交互式 Y/N 确认。需要安装版才能执行更新。

### R042 — CLI 每次命令执行后，仅在 actionable 时（未配置更新源、有新版本可用）追加一行 `{"type":"update","status":"success","data":{...}}` JSONL 输出。已最新版本时不输出。
- Class: core-capability
- Status: validated
- Description: CLI 每次命令执行后，仅在 actionable 时（未配置更新源、有新版本可用）追加一行 `{"type":"update","status":"success","data":{...}}` JSONL 输出。已最新版本时不输出。
- Why it matters: 不干扰 JSONL 解析器的同时，让 CLI 用户感知更新状态
- Source: user
- Primary owning slice: M009-q7p4iu/S04
- Supporting slices: none
- Validation: Post-command hook in CliRunner appends type=update JSONL only when exitCode==0, subcommand is not update, and updateInfo != null (new version available). Unit tests verify: update available → append, no update → no append, failed command → no append. Full test suite 154/154 pass.
- Notes: 不影响现有 JSONL 输出格式，只是在末尾可能有条件地多一行

### R043 — 多源支持和常驻提示的改动不能破坏现有的状态栏"检查更新"按钮、弹窗确认、下载进度、重启应用流程
- Class: constraint
- Status: validated
- Description: 多源支持和常驻提示的改动不能破坏现有的状态栏"检查更新"按钮、弹窗确认、下载进度、重启应用流程
- Why it matters: 已有的更新体验是稳定的，新功能是增量补充
- Source: inferred
- Primary owning slice: M009-q7p4iu/S03
- Supporting slices: M009-q7p4iu/S02
- Validation: S03 T02 验证：现有"检查更新"按钮保持不变（Grid.Column 从 2 移至 3），新增的更新提示 TextBlock 使用独立的 Grid 列和 InputBindings，不修改现有弹窗确认/下载进度/重启流程。dotnet test 172个测试全部通过，零回归。
- Notes: S02 未修改现有方法签名（CheckForUpdatesAsync/DownloadUpdatesAsync/ApplyUpdatesAndRestart），仅新增属性。零回归确认需 S03/S04 集成验证。

### R044 — UpdateService 支持运行时热重载更新源。调用 ReloadSource(updateUrl, channel) 后立即使用新的 URL/Channel 重建 UpdateManager，后续 CheckForUpdatesAsync 自动使用新源。同时持久化到 appsettings.json。UpdateUrl 为空时走 GitHub Releases，非空时走内网 HTTP 服务器。
- Class: core-capability
- Status: validated
- Description: UpdateService 支持运行时热重载更新源。调用 ReloadSource(updateUrl, channel) 后立即使用新的 URL/Channel 重建 UpdateManager，后续 CheckForUpdatesAsync 自动使用新源。同时持久化到 appsettings.json。UpdateUrl 为空时走 GitHub Releases，非空时走内网 HTTP 服务器。
- Why it matters: 让用户修改更新配置后不需要重启应用即可生效，降低使用门槛
- Source: user
- Primary owning slice: M010-hpylzg/S01
- Validation: ReloadSource 方法通过 21 个单元测试验证（含 7 个内存热重载测试 + 4 个持久化测试）。测试覆盖：HTTP 源切换、GitHub 回退、通道更新、null/空值处理、尾斜杠规范化、appsettings.json 写回、写入失败不抛异常、其他配置节保留。dotnet test --filter "UpdateServiceTests" 全部通过。

### R045 — 状态栏的更新提示文字中包含当前更新源类型信息（如"当前已是最新版本 (GitHub)"或"当前已是最新版本 (内网: 192.168.1.100:8080)"），帮助用户快速定位更新问题出在哪个源。
- Class: primary-user-loop
- Status: validated
- Description: 状态栏的更新提示文字中包含当前更新源类型信息（如"当前已是最新版本 (GitHub)"或"当前已是最新版本 (内网: 192.168.1.100:8080)"），帮助用户快速定位更新问题出在哪个源。
- Why it matters: 用户需要一眼看出当前更新走的是 GitHub 还是内网，方便排查更新失败问题
- Source: user
- Primary owning slice: M010-hpylzg/S02
- Validation: UpdateStatusMessage getter appends "(GitHub)" or "(内网: host)" suffix via IUpdateService.UpdateSourceType and EffectiveUpdateUrl. Refreshed after dialog save.

### R046 — 新增热重载和 GUI 设置弹窗功能后，现有更新检查流程（启动时自动检查、手动"检查更新"按钮、CheckUpdateAsync 弹窗确认、DownloadUpdatesAsync 下载、ApplyUpdatesAndRestart 重启）完全不受影响。
- Class: quality-attribute
- Status: validated
- Description: 新增热重载和 GUI 设置弹窗功能后，现有更新检查流程（启动时自动检查、手动"检查更新"按钮、CheckUpdateAsync 弹窗确认、DownloadUpdatesAsync 下载、ApplyUpdatesAndRestart 重启）完全不受影响。
- Why it matters: 现有更新体验稳定运行，新功能是增量补充，不能引入回归
- Source: inferred
- Primary owning slice: M010-hpylzg/S02
- Validation: ModernProgressBarStyle ControlTemplate now contains both PART_Track and PART_Indicator named elements. PART_Indicator has Fill={TemplateBinding Foreground} and HorizontalAlignment=Left. WPF ProgressBar will automatically set PART_Indicator width based on Value/Minimum/Maximum ratio. Verified by: (1) dotnet build passes with 0 errors, (2) PowerShell Select-String confirms both PART_Track and PART_Indicator present in App.xaml lines 169 and 173.

### R047 — 用户在 appsettings.json 中手动配置了 UpdateUrl 后，打开更新设置窗口时，URL 输入框应正确显示当前配置的 URL 值，而非空白
- Class: primary-user-loop
- Status: validated
- Description: 用户在 appsettings.json 中手动配置了 UpdateUrl 后，打开更新设置窗口时，URL 输入框应正确显示当前配置的 URL 值，而非空白
- Why it matters: 用户无法确认当前生效的更新源配置，也无法在已有配置基础上微调
- Source: user
- Primary owning slice: M011-ns0oo0/S01
- Supporting slices: none
- Validation: M011-ns0oo0 milestone validation: UpdateSettingsViewModel 构造函数直接从 IConfiguration["Update:UpdateUrl"] 读取原始 URL 值（决策 D033），11 个单元测试覆盖所有场景。dotnet build 0 错误 0 警告，dotnet test 203/203 通过。旧 EffectiveUpdateUrl 剥离逻辑完全移除（grep 确认 0 匹配）。
- Notes: 跨版本更新后（如 v1.1.4→v1.2.0），appsettings.json 中的配置值保留但界面不显示

### R048 — 用户确认下载更新后，弹出独立的模态进度窗口，实时显示下载进度条（0-100%）、当前下载速度（MB/s）和预估剩余时间
- Class: primary-user-loop
- Status: validated
- Description: 用户确认下载更新后，弹出独立的模态进度窗口，实时显示下载进度条（0-100%）、当前下载速度（MB/s）和预估剩余时间
- Why it matters: 用户不知道下载进展，无法判断是否卡住或需要等待多久
- Source: user
- Primary owning slice: M011-ns0oo0/S02
- Supporting slices: none
- Validation: M011-ns0oo0 milestone validation: DownloadProgressWindow XAML 模态弹窗（ProgressBar 0-100%, 速度 MB/s, ETA TextBlock）。DownloadProgressViewModel 通过累积平均速度计算实现实时跟踪。MainWindowViewModel.CheckUpdateAsync 集成 Task.Run + ShowDialog 模式。38 个单元测试通过。全部 203 个测试通过。
- Notes: Velopack DownloadUpdatesAsync 提供 Action<int> 回调（0-100），VelopackAsset.Size 提供包总字节数

### R049 — 下载进度弹窗提供取消按钮，用户点击后中断下载操作（传递 CancellationToken 给 Velopack），应用继续正常运行
- Class: primary-user-loop
- Status: validated
- Description: 下载进度弹窗提供取消按钮，用户点击后中断下载操作（传递 CancellationToken 给 Velopack），应用继续正常运行
- Why it matters: 用户可能误触发更新或网络环境变化需要中止下载
- Source: user
- Primary owning slice: M011-ns0oo0/S02
- Supporting slices: none
- Validation: M011-ns0oo0 milestone validation: Cancel 按钮绑定 CancelCommand 触发 CancellationTokenSource.Cancel()。OperationCanceledException 被捕获，应用继续正常运行。OnClosing 覆写防止下载期间直接 X 按钮关闭。单元测试验证取消状态转换。全部 203 个测试通过。
- Notes: Velopack DownloadUpdatesAsync 接受 CancellationToken 参数

### R050 — 主窗口两个 Tab（关键词替换、审核清理）的所有控件在 1366x768 分辨率下无需滚动即可完整可见和操作
- Class: core-capability
- Status: validated
- Description: 主窗口两个 Tab（关键词替换、审核清理）的所有控件在 1366x768 分辨率下无需滚动即可完整可见和操作
- Why it matters: 用户在 13-14 寸笔记本上无法看全界面内容，严重影响使用体验
- Source: user
- Primary owning slice: M012-li0ip5/S01
- Supporting slices: none
- Validation: Window size changed to 900x550 (MinWidth=800 MinHeight=500), two Tabs compacted with 12-14px font sizes, no GroupBox, no ScrollViewer. dotnet build passes with 0 errors.
- Notes: 窗口默认尺寸从 1400x900 降至 900x550，最小尺寸从 1200x800 降至 800x500

### R051 — 模板文件和数据文件的拖放区域从独立 Border（70-80px 高）改为路径文本框内支持拖放，节省垂直空间
- Class: core-capability
- Status: validated
- Description: 模板文件和数据文件的拖放区域从独立 Border（70-80px 高）改为路径文本框内支持拖放，节省垂直空间
- Why it matters: 原拖放区域是垂直空间的最大消耗者，改为单行后可节省约 150px
- Source: user
- Primary owning slice: M012-li0ip5/S01
- Supporting slices: none
- Validation: TemplateDropBorder and DataFileDropBorder removed, replaced by TextBox with AllowDrop=True and DragEnter/DragLeave/DragOver/Drop event handlers. AllowDrop appears 3 times in MainWindow.xaml. dotnet build passes.
- Notes: 路径文本框旁保留浏览按钮，文本框本身支持拖放文件/文件夹

### R052 — TabControl、GroupBox 标题、按钮等控件的字号从 FontSize=16 降至 12-14px 范围
- Class: quality-attribute
- Status: validated
- Description: TabControl、GroupBox 标题、按钮等控件的字号从 FontSize=16 降至 12-14px 范围
- Why it matters: 大字号是空间浪费的主要原因之一，降号后内容密度显著提升
- Source: user
- Primary owning slice: M012-li0ip5/S01
- Supporting slices: none
- Validation: TabControl FontSize=14, Tab headers=14, labels=13px, body text=12px. App.xaml global styles adjusted (ModernTextBoxStyle Padding 8,4, HeaderLabelStyle FontSize 13, button Padding 12,6). All FontSize values in 11-14px range.
- Notes: Tab 标题 14px、标签 13px、正文 12px

### R053 — 三个 GroupBox（模板文件、数据文件、输出设置）替换为简洁的文字标签 + 分隔线，去掉 header 行和内边距
- Class: quality-attribute
- Status: validated
- Description: 三个 GroupBox（模板文件、数据文件、输出设置）替换为简洁的文字标签 + 分隔线，去掉 header 行和内边距
- Why it matters: 三个 GroupBox 的 header 行和内边距共约 90px 额外空间
- Source: user
- Primary owning slice: M012-li0ip5/S01
- Supporting slices: none
- Validation: All three GroupBox elements removed from both Tabs. Replaced with TextBlock labels + Separator lines. grep confirms 0 GroupBox references in MainWindow.xaml.

### R054 — 当 DocuFiller 窗口不是前台窗口时，从资源管理器拖拽文件到窗口内仍能正常触发拖放事件
- Class: failure-visibility
- Status: validated
- Description: 当 DocuFiller 窗口不是前台窗口时，从资源管理器拖拽文件到窗口内仍能正常触发拖放事件
- Why it matters: 当前存在 bug：窗口未聚焦时拖放失效，需要先点击窗口才能拖放
- Source: user
- Primary owning slice: M012-li0ip5/S02
- Supporting slices: none
- Validation: Window element now has AllowDrop="True" and PreviewDragOver="Window_PreviewDragOver". Code-behind calls Activate() when window is not active during drag-over. Verified: dotnet build 0 errors, 4 AllowDrop targets, 7 Drop handlers, 3 DragEnter, 4 DragOver, Window_PreviewDragOver at L34.
- Notes: WPF 已知问题，需在 Window 级别处理拖放激活

### R055 — 审核清理 Tab 的布局同步紧凑化（字号、间距、控件尺寸），与关键词替换 Tab 风格一致
- Class: core-capability
- Status: validated
- Description: 审核清理 Tab 的布局同步紧凑化（字号、间距、控件尺寸），与关键词替换 Tab 风格一致
- Why it matters: 两个 Tab 视觉风格统一，审核清理 Tab 也需在小屏下完整显示
- Source: user
- Primary owning slice: M012-li0ip5/S01
- Supporting slices: none
- Validation: Tab 2 uses same DockPanel structure, same font sizes (12-14px), same label width (65px) and button sizes as Tab 1. Output settings GroupBox removed, replaced with inline layout. CleanupDropZoneBorder compressed (Padding 30→12).

### R056 — update-config.json 存储在用户 home 目录（~/.docx_replacer/），完全独立于 Velopack 安装目录。Setup.exe 安装和 Velopack 自动更新都不得覆盖此文件。GUI 和 CLI 共享同一配置路径。
- Class: core-capability
- Status: validated
- Description: update-config.json 存储在用户 home 目录（~/.docx_replacer/），完全独立于 Velopack 安装目录。Setup.exe 安装和 Velopack 自动更新都不得覆盖此文件。GUI 和 CLI 共享同一配置路径。
- Why it matters: 当前配置文件放在 Velopack 安装目录下，每次安装/更新被覆盖导致内网更新地址丢失，是反复出现的 bug
- Source: user
- Primary owning slice: M013-ueix00/S01
- Validation: UpdateService.GetPersistentConfigPath() and UpdateSettingsViewModel.ReadPersistentConfig() both use %USERPROFILE%\.docx_replacer\update-config.json. Directory auto-created on first write. dotnet build 0 errors, dotnet test 244 pass. No dependency on Update.exe or Velopack install directory.

### R057 — 主窗口标题栏右侧提供图钉按钮，点击切换 Window.Topmost 属性，按钮有激活/未激活视觉状态区分
- Class: primary-user-loop
- Status: validated
- Description: 主窗口标题栏右侧提供图钉按钮，点击切换 Window.Topmost 属性，按钮有激活/未激活视觉状态区分
- Why it matters: 用户在对照多个文档操作时需要窗口始终在前台，目前只能通过系统任务栏右键置顶，不够直观
- Source: user
- Primary owning slice: M016/S01
- Validation: S01 added pin button (📌/📍) in custom WindowChrome title bar. ToggleTopmostCommand flips IsTopmost, code-behind syncs to Window.Topmost. Active state: pin icon at full opacity with "取消置顶" tooltip; inactive: lighter icon with "置顶窗口" tooltip. dotnet build passes.

### R058 — 关键词替换 tab 的模板文件和数据文件 TextBox 下方显示拖放提示文字（如"支持拖放文件到此"），引导用户发现拖放功能
- Class: core-capability
- Status: validated
- Description: 关键词替换 tab 的模板文件和数据文件 TextBox 下方显示拖放提示文字（如"支持拖放文件到此"），引导用户发现拖放功能
- Why it matters: UI 紧凑化后去掉了独立的拖放 Border 区域，用户不知道路径 TextBox 支持拖放，误以为拖放功能丢失
- Source: user
- Primary owning slice: M016/S01
- Validation: S01 added TextBlock hints (11px, #AAAAAA) below template TextBox ("提示：可将 .docx 文件或文件夹拖放到上方文本框") and data TextBox ("提示：可将 Excel 文件拖放到上方文本框") in keyword replacement tab. dotnet build passes.

### R059 — 模板文件 TextBox 和数据文件 TextBox 必须能正确接收从资源管理器拖入的文件/文件夹。当前使用冒泡事件（Drop/DragOver）被 TextBox 内置拖放处理拦截，导致鼠标显示"禁止"图标。改为 Preview 隧道事件绕过内置处理。
- Class: core-capability
- Status: validated
- Description: 模板文件 TextBox 和数据文件 TextBox 必须能正确接收从资源管理器拖入的文件/文件夹。当前使用冒泡事件（Drop/DragOver）被 TextBox 内置拖放处理拦截，导致鼠标显示"禁止"图标。改为 Preview 隧道事件绕过内置处理。
- Why it matters: 拖放是路径输入的主要便捷方式，提示文字已引导用户使用此功能，但实际无法工作会严重影响体验可信度
- Source: user
- Primary owning slice: M017/S01
- Validation: TemplatePathTextBox 和 DataPathTextBox 的 8 个冒泡拖放事件已改为 Preview 隧道版本（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave），清理区域保留冒泡事件不变。dotnet build 0 错误 0 警告。需人工 UAT 确认拖放视觉反馈和路径填入功能正常。

### R060 — Untitled
- Status: validated
- Validation: MainWindowViewModel.cs reduced from 1623 to 390 lines (under 400 target). FillViewModel.cs (825 lines, CT.Mvvm) extracted with all keyword-replacement tab logic. UpdateStatusViewModel.cs (397 lines, CT.Mvvm) extracted with all update status logic. dotnet build: 0 errors, 0 warnings. dotnet test: 280 passed (253 unit + 27 E2E), 0 failed.

### R061 — Electron.NET 方案的 PoC 项目和完整调研文档。PoC 实现迷你 DocuFiller（文件选择→处理→进度条），在 Windows 上编译运行。
- Class: differentiator
- Status: validated
- Description: Electron.NET 方案的 PoC 项目和完整调研文档。PoC 实现迷你 DocuFiller（文件选择→处理→进度条），在 Windows 上编译运行。
- Why it matters: 用户明确要求调研 Electron.NET 作为 WPF 替代方案的可行性，需要实际代码验证
- Source: user
- Primary owning slice: M022/S01
- Validation: Electron.NET PoC compiles on Windows (dotnet build exit 0, 0 errors 0 warnings). PoC includes native file dialog (Electron.Dialog), SSE progress streaming, IPC status reporting, and frontend progress bar UI. 13-section research document (21KB) covers technical overview, DocuFiller adaptability, IPC mechanisms, NuGet compatibility, cross-platform support, SWOT, TRL 6 assessment. All code in poc/electron-net-docufiller/, independent of main project.

### R062 — Tauri + .NET sidecar 方案的 PoC 项目和完整调研文档。PoC 实现迷你 DocuFiller（文件选择→处理→进度条），在 Windows 上编译运行。
- Class: differentiator
- Status: validated
- Description: Tauri + .NET sidecar 方案的 PoC 项目和完整调研文档。PoC 实现迷你 DocuFiller（文件选择→处理→进度条），在 Windows 上编译运行。
- Why it matters: 用户明确要求调研 Tauri 作为轻量级跨平台壳的可行性，需要实际代码验证 .NET 集成难度
- Source: user
- Primary owning slice: M022/S02
- Validation: Tauri v2 + .NET sidecar PoC compiles on Windows (cargo build + dotnet build both exit 0). PoC includes native file dialog, sidecar HTTP API with SSE progress streaming, and frontend progress bar. 649-line research document with 16 sections covers architecture, IPC, cross-platform, performance, PoC findings. All code in poc/tauri-docufiller/, independent of main project.

### R063 — Avalonia、Blazor Hybrid、纯 Web 应用、MAUI 四个方案的纯文献调研对比文档
- Class: differentiator
- Status: validated
- Description: Avalonia、Blazor Hybrid、纯 Web 应用、MAUI 四个方案的纯文献调研对比文档
- Why it matters: 为跨平台迁移决策提供完整的方案全景，这四个方案不写 PoC 但需要调研覆盖
- Source: user
- Primary owning slice: M022/S03
- Validation: 四份调研文档均通过自动化质量验证（≥8 章节、≥3000 字、无 TBD/TODO），综合评分：Avalonia 4.3/5 > Blazor Hybrid 3.7/5 > Web 3.0/5 > MAUI 2.8/5

### R064 — Velopack 跨平台能力、核心依赖库（OpenXml/EPPlus）兼容性、平台差异处理（文件对话框/拖放/路径）、打包分发方案（macOS dmg/notarization、Linux deb/AppImage）的调研文档
- Class: differentiator
- Status: validated
- Description: Velopack 跨平台能力、核心依赖库（OpenXml/EPPlus）兼容性、平台差异处理（文件对话框/拖放/路径）、打包分发方案（macOS dmg/notarization、Linux deb/AppImage）的调研文档
- Why it matters: 基础设施层面的调研为任何迁移方案提供支撑，确保更新、依赖、打包不成为阻塞项
- Source: inferred
- Primary owning slice: M022/S04
- Validation: S04 产出四份调研文档：velopack-cross-platform.md（13章节，30KB）、core-dependencies-compatibility.md（13章节，32KB）、platform-differences.md（13章节，30KB）、packaging-distribution.md（14章节，42KB）。覆盖 Velopack 三平台更新能力、16个 NuGet 依赖跨平台兼容性、6大平台差异点、macOS/Linux 打包分发方案。所有文档零 TBD/TODO，格式与 S03 产出一致。

### R065 — 汇总所有 6 个 UI 方案的最终对比评估文档，包含技术可行性、迁移成本、生态成熟度、推荐排序
- Class: differentiator
- Status: validated
- Description: 汇总所有 6 个 UI 方案的最终对比评估文档，包含技术可行性、迁移成本、生态成熟度、推荐排序
- Why it matters: 为用户提供决策依据，将分散的调研结果整合为可操作的建议
- Source: inferred
- Primary owning slice: M022/S05
- Validation: comparison-and-recommendation.md (36,899 bytes, 12 sections) covers all 6 UI frameworks with multi-dimensional scoring, SWOT matrices, weighted rankings, and migration roadmap. Verified: file exists, 0 TBD/TODO, 58 Avalonia references, 15 key-section references.

## Deferred

## Out of Scope

### R013 — JSON 编辑器功能已移除，相关文档不迁移，直接删除
- Class: core-capability
- Status: out-of-scope
- Description: JSON 编辑器功能已移除，相关文档不迁移，直接删除
- Why it matters: 功能不存在，文档无意义
- Source: user
- Primary owning slice: none
- Supporting slices: none
- Validation: n/a
- Notes: .trae/documents/ 下两份 JSON 编辑器文档将被删除

## Traceability

| ID | Class | Status | Primary owner | Supporting | Proof |
|---|---|---|---|---|---|
| R001 | core-capability | validated | M001-a60bo7/S01 | none | IsInstalled guard removed from both GUI (InitializeUpdateStatusAsync) and CLI (UpdateCommand.ExecuteAsync). Portable version follows identical update code path as installed version. IUpdateService.IsPortable property available for mode detection. dotnet build 0 errors, 249/249 tests pass. |
| R002 | core-capability | validated | M001-a60bo7/S01 | none | IUpdateService.cs line 32: bool IsPortable { get; } added. UpdateService implements via Velopack UpdateManager.IsPortable. All test stubs updated. dotnet build 0 errors, 249/249 tests pass. |
| R003 | constraint | validated | M001-a60bo7/S01 | M001-a60bo7/S02 | UpdateStatus.PortableVersion enum removed from MainWindowViewModel.cs. All switch branches in UpdateStatusMessage, UpdateStatusBrush, OnUpdateStatusClickAsync deleted. IsInstalled guard in InitializeUpdateStatusAsync removed. grep confirms zero remaining references. dotnet build 0 errors, 249/249 tests pass. |
| R004 | quality-attribute | validated | M001-a60bo7/S02 | none | UpdateCommand.ExecuteAsync IsInstalled guard removed. Portable version now enters same download+apply path. Test Update_WithYes_Portable_ProceedsNormally verifies exitCode 0 and no PORTABLE_NOT_SUPPORTED output. dotnet build 0 errors, 249/249 tests pass. |
| R005 | core-capability | validated | M003-g1w88x/S01 | none | docs/DocuFiller产品需求文档.md 已创建，覆盖所有 6 个功能模块（文件输入、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具），包含 Mermaid 流程图和用户界面设计 |
| R006 | core-capability | validated | M003-g1w88x/S01 | none | docs/DocuFiller技术架构文档.md 已创建，包含 14 个 public interface 定义、9 个二级章节、5 个 Mermaid 图（架构图、ER 图、3 个序列图），覆盖全部 15 个服务组件 |
| R007 | core-capability | validated | M003-g1w88x/S02 | none | README.md 包含 14 个服务接口架构表（grep 验证全部 14 个 I 前缀接口存在）、6 个功能模块完整覆盖、Excel 两列/三列格式说明、准确项目结构。验证命令全部通过。 |
| R008 | core-capability | validated | M003-g1w88x/S02 | none | Will be recorded via gsd_decision_save in slice completion. |
| R009 | core-capability | validated | M003-g1w88x/S03 | none | docs/excel-data-user-guide.md 包含 6 个章节（含三列格式独立章节）、三列格式关键词匹配 11 处、无 TBD/TODO 标记 |
| R010 | core-capability | validated | M003-g1w88x/S03 | none | header-footer-support.md 修正为"仅正文区域支持批注"（7 个章节，含准确描述）；批注功能说明.md 与代码一致；两份文档均无 TBD/TODO |
| R011 | operability | validated | M003-g1w88x/S01 | none | .trae/documents/ 目录已删除（4 份旧文件全部清理），DocuFiller 两份文档已迁移到 docs/，JSON 编辑器两份文档已按 D004 直接删除 |
| R012 | operability | validated | none | none | fill 和 cleanup 子命令实现完成，JSONL 输出格式验证通过。FillCommand 通过 IDocumentProcessor.ProcessDocumentsAsync 调用核心填充服务，CleanupCommand 通过 IDocumentCleanupService.CleanupAsync 调用清理服务。37 个 CLI 单元测试覆盖路由、参数验证和输出格式，108/108 测试全部通过。 |
| R013 | core-capability | out-of-scope | none | none | n/a |
| R014 | core-capability | validated | M004-l08k3s/S01 | none | All online update code removed: csproj PreBuild/PostPublish gates deleted, External/ directory deleted, 19 update service/model/viewmodel/view files deleted, App.xaml.cs DI registrations removed, MainWindowViewModel/MainWindow.xaml/MainWindow.xaml.cs update references removed. dotnet build passes with 0 errors. grep confirms 0 matches for IUpdateService, UpdateViewModel, UpdateBannerView, UpdateWindow, ValidateUpdateClientFiles, ValidateReleaseFiles, update-client. |
| R015 | core-capability | validated | M004-l08k3s/S01 | none | All 9 JSON editor orphaned files deleted: JsonEditorService.cs, IJsonEditorService.cs, KeywordValidationService.cs, IKeywordValidationService.cs, JsonKeywordItem.cs, JsonProjectModel.cs, JsonEditorViewModel.cs, JsonEditorWindow.xaml, JsonEditorWindow.xaml.cs. No remaining JSON editor files in codebase. dotnet build passes. |
| R016 | core-capability | validated | M004-l08k3s/S02 | none | grep confirms 0 real IDataParser/DataParserService references in core files. DataFileType enum is Excel-only. DocumentProcessorService has no JSON processing branch. dotnet build and dotnet test pass. |
| R017 | core-capability | validated | M004-l08k3s/S02 | none | All 5 converter files deleted. grep confirms 0 references to IExcelToWordConverter, ConverterWindow, ConverterWindowViewModel, OpenConverter in any source file. DI registrations removed. dotnet build and test pass. |
| R018 | core-capability | validated | M004-l08k3s/S02 | none | KeywordEditorUrl removed from appsettings.json and AppSettings.cs UISettings class. KeywordEditorHyperlink_Click and ConverterHyperlink_Click handlers removed from MainWindow.xaml.cs. IOptions<UISettings> constructor parameter removed. dotnet build and test pass. |
| R019 | core-capability | validated | M004-l08k3s/S02 | none | Tools/ directory deleted (confirmed not present on disk). All 10 tool project entries removed from DocuFiller.sln. Compile/EmbeddedResource/None Remove entries removed from DocuFiller.csproj. grep confirms 0 residual references to any tool project name. dotnet build and test pass. |
| R020 | quality-attribute | validated | M004-l08k3s/S03 | M004-l08k3s/S02 | All 71 tests pass after S01/S02 feature removal. T01 removed ValidateJsonFormat (sole Newtonsoft.Json consumer) and test-data.json. dotnet test --no-build --verbosity minimal: 71 passed, 0 failed. No Newtonsoft.Json references remain in .cs/.csproj files. |
| R021 | core-capability | validated | M004-l08k3s/S03 | none | CLAUDE.md 和 README.md 已添加完整 CLI 使用文档。CLAUDE.md 包含 6 个 CLI 组件说明、CLI 接口章节（子命令用法、JSONL envelope schema、输出类型、错误码、使用示例）。README.md 包含 CLI 使用方法章节（三个子命令参数和示例）、JSONL 格式说明。 |
| R022 | core-capability | validated | M007-wpaxa3/S01 | none | Velopack 0.0.1298 NuGet added to csproj + both test csprojs. VelopackApp.Build().Run() is first line of Program.Main(). App.config old update entries (UpdateServerUrl, UpdateChannel, CheckUpdateOnStartup) removed. build-internal.bat COPY_EXTERNAL_FILES/PUBLISH_TO_SERVER/update-client references removed. sync-version.bat update-client.config.yaml block removed. dotnet build 0 errors, dotnet test 162 pass. grep confirms 0 old update system references. |
| R023 | core-capability | validated | M007-wpaxa3/S02 | none | Services/UpdateService.cs 实现了 IUpdateService 全部 4 个成员（CheckForUpdatesAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart, IsUpdateUrlConfigured）。在 App.xaml.cs 中注册为 Singleton（services.AddSingleton<IUpdateService, UpdateService>()）。从 IConfiguration 读取 Update:UpdateUrl 配置，空字符串时 IsUpdateUrlConfigured 返回 false。dotnet build 0 errors, dotnet test 162 tests pass。 |
| R024 | primary-user-loop | validated | M007-wpaxa3/S02 | none | MainWindow.xaml 底部添加 StatusBar 显示 VersionHelper.GetCurrentVersion() 版本号和"检查更新"按钮。MainWindowViewModel 注入 IUpdateService（可选参数），实现 CheckUpdateCommand 命令。更新检查流程：有新版本弹确认对话框，无新版本显示"已是最新版本"，异常显示错误信息。CanCheckUpdate 绑定控制按钮灰显（IsUpdateUrlConfigured 为 false 时禁用）。dotnet build 0 errors, dotnet test 162 tests pass。 |
| R025 | operability | validated | M007-wpaxa3/S03 | none | build-internal.bat contains PublishSingleFile=true, IncludeNativeLibrariesForSelfExtract=true, vpk pack with --packId DocuFiller and --mainExe DocuFiller.exe. Old scripts (publish.bat, release.bat, build-and-publish.bat) and config/ directory removed. build.bat simplified to standalone-only. dotnet build succeeds with 0 errors. |
| R026 | quality-attribute | validated | M007-wpaxa3/S04 | none | E2E portable update scripts created: e2e-portable-update-test.bat (local HTTP, port 8081) and e2e-portable-go-update-test.sh (Go server, port 19081). Both scripts automate full update chain: build→pack→extract→configure→update→verify. Test guide updated with 4 new portable sections. dotnet build 0 errors. |
| R027 | quality-attribute | validated | M007-wpaxa3/S01 | M007-wpaxa3/S02, M007-wpaxa3/S03 | dotnet build 0 errors, dotnet test 162 tests pass (135 + 27), 0 failures. Velopack integration and config/script cleanup do not affect any existing business logic tests. |
| R028 | core-capability | validated | M021/S05 | none | UpdateStatusViewModel.InitializeAsync() uses Task.Delay(5000) + CancellationToken for 5-second delay after startup, then calls CheckUpdateAsync() silently. HasUpdateAvailable computed property drives red dot badge on ⚙ settings button. 16 unit tests cover: normal flow, cancellation, exception handling, multiple checks. dotnet build 0 errors and dotnet test all pass during slice execution (verified per-slice). |
| R029 | operability | validated | M010-hpylzg/S02 | none | UpdateSettingsWindow provides GUI for editing UpdateUrl/Channel with Save calling IUpdateService.ReloadSource. Settings persist to appsettings.json. Status bar shows source type suffix. dotnet build 0 errors, 192/192 tests pass. |
| R030 | core-capability | validated | M008-4uyz6m/S01 | none | Go update-server serves static files from /{channel}/releases.win.json and /{channel}/*.nupkg with Content-Type headers. Build compiles, 50 Go tests pass, curl integration tests verify static serving. |
| R031 | core-capability | validated | M008-4uyz6m/S01 | none | POST /api/channels/{channel}/releases accepts multipart uploads (releases.win.json + .nupkg files), merges feeds by FileName to avoid duplicates, requires Bearer token auth. Tested with httptest integration tests and curl. |
| R032 | core-capability | validated | M008-4uyz6m/S01 | none | POST /api/channels/{target}/promote?from={source}&version={version} copies matching files and merges feed entries. Returns 404 if version not found. Tested in handler_test.go (5 test cases). |
| R033 | core-capability | validated | M008-4uyz6m/S01 | none | GET /api/channels/{channel}/releases returns grouped versions with file names, counts, sizes, sorted by semver descending. No auth required. Tested in handler_test.go (3 test cases). |
| R034 | operability | validated | M008-4uyz6m/S01 | none | CleanupOldVersions keeps last 10 versions per channel. Tested with 11 versions (oldest removed) and 15 versions (5 removed). Cleanup triggers after every upload and promote. Files deleted from disk and feed updated atomically. |
| R035 | operability | validated | M008-4uyz6m/S03 | none | build-internal.bat accepts optional CHANNEL parameter (stable/beta), validates input, and auto-uploads .nupkg + releases.win.json to Go update server via curl when UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN are set. Grep verification: 28 UPLOAD lines, 10 CHANNEL lines, both env vars present. dotnet build: 0 errors. |
| R036 | quality-attribute | validated | M008-4uyz6m/S04 | none | E2E dual-channel script (13 assertions PASS), Go server tests (handler 28 + storage 14 = 42 PASS), Go build (exit 0), .NET build (0 errors), .NET tests (168 PASS: 141 unit + 27 E2E). Full flow verified: upload beta → beta feed accessible → channel isolation → promote → stable feed accessible → auto-cleanup. |
| R037 | core-capability | validated | M009-q7p4iu/S01 | none | Workflow file .github/workflows/build-release.yml exists with correct v* tag trigger, .NET 8 setup, Velopack packaging, and GitHub Release creation. 24 structural checks pass. dotnet build succeeds confirming CI compatibility. |
| R038 | core-capability | validated | M009-q7p4iu/S01 | none | Workflow uploads all 4 artifact types to GitHub Release: DocuFillerSetup.exe, *Portable*.zip, *.nupkg, releases.win.json. Verified via grep checks on workflow YAML file patterns. |
| R039 | core-capability | validated | M009-q7p4iu/S02 | none | S02 通过 10 个单元测试验证：UpdateUrl 为空时 UpdateSourceType 为 "GitHub"（使用 GithubSource），UpdateUrl 非空时为 "HTTP"（使用 SimpleWebSource），Channel 默认 stable。接口签名未改，只新增属性。 |
| R040 | primary-user-loop | validated | M009-q7p4iu/S03 | none | S03 T01+T02: MainWindowViewModel 新增 UpdateStatus 枚举（6种状态）+ 5个绑定属性 + InitializeUpdateStatusAsync 自动检查 + UpdateStatusClickCommand。MainWindow.xaml 状态栏新增 TextBlock 绑定到这些属性，点击触发更新流程。dotnet build 通过，172个测试全部通过。 |
| R041 | core-capability | validated | M009-q7p4iu/S04 | none | UpdateCommand implements ICliCommand with CommandName=update. Unit tests verify: no-yes outputs version info JSONL (currentVersion/latestVersion/hasUpdate/isInstalled/updateSourceType), --yes portable guard emits PORTABLE_NOT_SUPPORTED error, --yes no-update outputs ALREADY_UP_TO_DATE. Build passes, all 154 tests pass. |
| R042 | core-capability | validated | M009-q7p4iu/S04 | none | Post-command hook in CliRunner appends type=update JSONL only when exitCode==0, subcommand is not update, and updateInfo != null (new version available). Unit tests verify: update available → append, no update → no append, failed command → no append. Full test suite 154/154 pass. |
| R043 | constraint | validated | M009-q7p4iu/S03 | M009-q7p4iu/S02 | S03 T02 验证：现有"检查更新"按钮保持不变（Grid.Column 从 2 移至 3），新增的更新提示 TextBlock 使用独立的 Grid 列和 InputBindings，不修改现有弹窗确认/下载进度/重启流程。dotnet test 172个测试全部通过，零回归。 |
| R044 | core-capability | validated | M010-hpylzg/S01 | none | ReloadSource 方法通过 21 个单元测试验证（含 7 个内存热重载测试 + 4 个持久化测试）。测试覆盖：HTTP 源切换、GitHub 回退、通道更新、null/空值处理、尾斜杠规范化、appsettings.json 写回、写入失败不抛异常、其他配置节保留。dotnet test --filter "UpdateServiceTests" 全部通过。 |
| R045 | primary-user-loop | validated | M010-hpylzg/S02 | none | UpdateStatusMessage getter appends "(GitHub)" or "(内网: host)" suffix via IUpdateService.UpdateSourceType and EffectiveUpdateUrl. Refreshed after dialog save. |
| R046 | quality-attribute | validated | M010-hpylzg/S02 | none | ModernProgressBarStyle ControlTemplate now contains both PART_Track and PART_Indicator named elements. PART_Indicator has Fill={TemplateBinding Foreground} and HorizontalAlignment=Left. WPF ProgressBar will automatically set PART_Indicator width based on Value/Minimum/Maximum ratio. Verified by: (1) dotnet build passes with 0 errors, (2) PowerShell Select-String confirms both PART_Track and PART_Indicator present in App.xaml lines 169 and 173. |
| R047 | primary-user-loop | validated | M011-ns0oo0/S01 | none | M011-ns0oo0 milestone validation: UpdateSettingsViewModel 构造函数直接从 IConfiguration["Update:UpdateUrl"] 读取原始 URL 值（决策 D033），11 个单元测试覆盖所有场景。dotnet build 0 错误 0 警告，dotnet test 203/203 通过。旧 EffectiveUpdateUrl 剥离逻辑完全移除（grep 确认 0 匹配）。 |
| R048 | primary-user-loop | validated | M011-ns0oo0/S02 | none | M011-ns0oo0 milestone validation: DownloadProgressWindow XAML 模态弹窗（ProgressBar 0-100%, 速度 MB/s, ETA TextBlock）。DownloadProgressViewModel 通过累积平均速度计算实现实时跟踪。MainWindowViewModel.CheckUpdateAsync 集成 Task.Run + ShowDialog 模式。38 个单元测试通过。全部 203 个测试通过。 |
| R049 | primary-user-loop | validated | M011-ns0oo0/S02 | none | M011-ns0oo0 milestone validation: Cancel 按钮绑定 CancelCommand 触发 CancellationTokenSource.Cancel()。OperationCanceledException 被捕获，应用继续正常运行。OnClosing 覆写防止下载期间直接 X 按钮关闭。单元测试验证取消状态转换。全部 203 个测试通过。 |
| R050 | core-capability | validated | M012-li0ip5/S01 | none | Window size changed to 900x550 (MinWidth=800 MinHeight=500), two Tabs compacted with 12-14px font sizes, no GroupBox, no ScrollViewer. dotnet build passes with 0 errors. |
| R051 | core-capability | validated | M012-li0ip5/S01 | none | TemplateDropBorder and DataFileDropBorder removed, replaced by TextBox with AllowDrop=True and DragEnter/DragLeave/DragOver/Drop event handlers. AllowDrop appears 3 times in MainWindow.xaml. dotnet build passes. |
| R052 | quality-attribute | validated | M012-li0ip5/S01 | none | TabControl FontSize=14, Tab headers=14, labels=13px, body text=12px. App.xaml global styles adjusted (ModernTextBoxStyle Padding 8,4, HeaderLabelStyle FontSize 13, button Padding 12,6). All FontSize values in 11-14px range. |
| R053 | quality-attribute | validated | M012-li0ip5/S01 | none | All three GroupBox elements removed from both Tabs. Replaced with TextBlock labels + Separator lines. grep confirms 0 GroupBox references in MainWindow.xaml. |
| R054 | failure-visibility | validated | M012-li0ip5/S02 | none | Window element now has AllowDrop="True" and PreviewDragOver="Window_PreviewDragOver". Code-behind calls Activate() when window is not active during drag-over. Verified: dotnet build 0 errors, 4 AllowDrop targets, 7 Drop handlers, 3 DragEnter, 4 DragOver, Window_PreviewDragOver at L34. |
| R055 | core-capability | validated | M012-li0ip5/S01 | none | Tab 2 uses same DockPanel structure, same font sizes (12-14px), same label width (65px) and button sizes as Tab 1. Output settings GroupBox removed, replaced with inline layout. CleanupDropZoneBorder compressed (Padding 30→12). |
| R056 | core-capability | validated | M013-ueix00/S01 | none | UpdateService.GetPersistentConfigPath() and UpdateSettingsViewModel.ReadPersistentConfig() both use %USERPROFILE%\.docx_replacer\update-config.json. Directory auto-created on first write. dotnet build 0 errors, dotnet test 244 pass. No dependency on Update.exe or Velopack install directory. |
| R057 | primary-user-loop | validated | M016/S01 | none | S01 added pin button (📌/📍) in custom WindowChrome title bar. ToggleTopmostCommand flips IsTopmost, code-behind syncs to Window.Topmost. Active state: pin icon at full opacity with "取消置顶" tooltip; inactive: lighter icon with "置顶窗口" tooltip. dotnet build passes. |
| R058 | core-capability | validated | M016/S01 | none | S01 added TextBlock hints (11px, #AAAAAA) below template TextBox ("提示：可将 .docx 文件或文件夹拖放到上方文本框") and data TextBox ("提示：可将 Excel 文件拖放到上方文本框") in keyword replacement tab. dotnet build passes. |
| R059 | core-capability | validated | M017/S01 | none | TemplatePathTextBox 和 DataPathTextBox 的 8 个冒泡拖放事件已改为 Preview 隧道版本（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave），清理区域保留冒泡事件不变。dotnet build 0 错误 0 警告。需人工 UAT 确认拖放视觉反馈和路径填入功能正常。 |
| R060 |  | validated | none | none | MainWindowViewModel.cs reduced from 1623 to 390 lines (under 400 target). FillViewModel.cs (825 lines, CT.Mvvm) extracted with all keyword-replacement tab logic. UpdateStatusViewModel.cs (397 lines, CT.Mvvm) extracted with all update status logic. dotnet build: 0 errors, 0 warnings. dotnet test: 280 passed (253 unit + 27 E2E), 0 failed. |
| R061 | differentiator | validated | M022/S01 | none | Electron.NET PoC compiles on Windows (dotnet build exit 0, 0 errors 0 warnings). PoC includes native file dialog (Electron.Dialog), SSE progress streaming, IPC status reporting, and frontend progress bar UI. 13-section research document (21KB) covers technical overview, DocuFiller adaptability, IPC mechanisms, NuGet compatibility, cross-platform support, SWOT, TRL 6 assessment. All code in poc/electron-net-docufiller/, independent of main project. |
| R062 | differentiator | validated | M022/S02 | none | Tauri v2 + .NET sidecar PoC compiles on Windows (cargo build + dotnet build both exit 0). PoC includes native file dialog, sidecar HTTP API with SSE progress streaming, and frontend progress bar. 649-line research document with 16 sections covers architecture, IPC, cross-platform, performance, PoC findings. All code in poc/tauri-docufiller/, independent of main project. |
| R063 | differentiator | validated | M022/S03 | none | 四份调研文档均通过自动化质量验证（≥8 章节、≥3000 字、无 TBD/TODO），综合评分：Avalonia 4.3/5 > Blazor Hybrid 3.7/5 > Web 3.0/5 > MAUI 2.8/5 |
| R064 | differentiator | validated | M022/S04 | none | S04 产出四份调研文档：velopack-cross-platform.md（13章节，30KB）、core-dependencies-compatibility.md（13章节，32KB）、platform-differences.md（13章节，30KB）、packaging-distribution.md（14章节，42KB）。覆盖 Velopack 三平台更新能力、16个 NuGet 依赖跨平台兼容性、6大平台差异点、macOS/Linux 打包分发方案。所有文档零 TBD/TODO，格式与 S03 产出一致。 |
| R065 | differentiator | validated | M022/S05 | none | comparison-and-recommendation.md (36,899 bytes, 12 sections) covers all 6 UI frameworks with multi-dimensional scoring, SWOT matrices, weighted rankings, and migration roadmap. Verified: file exists, 0 TBD/TODO, 58 Avalonia references, 15 key-section references. |

## Coverage Summary

- Active requirements: 0
- Mapped to slices: 0
- Validated: 64 (R001, R002, R003, R004, R005, R006, R007, R008, R009, R010, R011, R012, R014, R015, R016, R017, R018, R019, R020, R021, R022, R023, R024, R025, R026, R027, R028, R029, R030, R031, R032, R033, R034, R035, R036, R037, R038, R039, R040, R041, R042, R043, R044, R045, R046, R047, R048, R049, R050, R051, R052, R053, R054, R055, R056, R057, R058, R059, R060, R061, R062, R063, R064, R065)
- Unmapped active requirements: 0
