# Requirements

This file is the explicit capability and coverage contract for the project.

## Validated

### R001 — Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Class: core-capability
- Status: validated
- Description: Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Why it matters: 允许用户在 Excel 中为每行添加人类可读的标签，方便维护大型数据表
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: 3-column Excel (ID|#keyword#|value) correctly parsed via DetectExcelFormat heuristic. 6 new xunit tests prove: 3-col parsing returns correct keyword-value pairs, ID column excluded from results, and format detection distinguishes 2-col vs 3-col. All 61 tests pass.
- Notes: 检测依据为第一行第一列内容是否匹配 #xxx# 格式

### R002 — 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Class: core-capability
- Status: validated
- Description: 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Why it matters: 防止数据表维护错误导致混淆
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: ID uniqueness validation implemented in ValidateExcelFileAsync using HashSet tracking. Duplicate IDs populate ExcelFileSummary.DuplicateRowIds and add errors to ExcelValidationResult.Errors with specific duplicate ID names. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms behavior.
- Notes: 两列模式不触发此校验

### R003 — 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Class: constraint
- Status: validated
- Description: 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Why it matters: 已有用户和数据模板不能因为新功能被破坏
- Source: inferred
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: M001-a60bo7/S02
- Validation: All 3 pre-existing ExcelDataParserServiceTests pass unchanged (ParseExcelFileAsync_ValidFile_ReturnsData, ValidateExcelFileAsync_ValidFile_PassesValidation, ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation). Full 61-test suite passes with zero regressions. 2-column parsing and validation behavior is completely unchanged.
- Notes: 硬性约束，零回归

### R004 — 所有现有单元测试和集成测试在改动后继续通过
- Class: quality-attribute
- Status: validated
- Description: 所有现有单元测试和集成测试在改动后继续通过
- Why it matters: 回归安全的底线
- Source: inferred
- Primary owning slice: M001-a60bo7/S02
- Supporting slices: none
- Validation: Full test suite passes with 71 tests (0 failures, 0 skipped). Includes 12 new edge case unit tests for 3-column format (empty file, blank first rows, empty ID, single row, ID trim, multi-duplicate) and 1 new end-to-end integration test proving 3-column Excel→Word pipeline works correctly. All pre-existing tests remain green with zero regressions.
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
- Validation: CLAUDE.md 包含 17 个唯一 I 前缀标识符（>=14）、16 个关键数据模型、DetectExcelFormat 处理路径说明、DI 生命周期配置。grep 验证所有关键接口和数据模型均存在。
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
| R001 | core-capability | validated | M001-a60bo7/S01 | none | 3-column Excel (ID|#keyword#|value) correctly parsed via DetectExcelFormat heuristic. 6 new xunit tests prove: 3-col parsing returns correct keyword-value pairs, ID column excluded from results, and format detection distinguishes 2-col vs 3-col. All 61 tests pass. |
| R002 | core-capability | validated | M001-a60bo7/S01 | none | ID uniqueness validation implemented in ValidateExcelFileAsync using HashSet tracking. Duplicate IDs populate ExcelFileSummary.DuplicateRowIds and add errors to ExcelValidationResult.Errors with specific duplicate ID names. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms behavior. |
| R003 | constraint | validated | M001-a60bo7/S01 | M001-a60bo7/S02 | All 3 pre-existing ExcelDataParserServiceTests pass unchanged (ParseExcelFileAsync_ValidFile_ReturnsData, ValidateExcelFileAsync_ValidFile_PassesValidation, ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation). Full 61-test suite passes with zero regressions. 2-column parsing and validation behavior is completely unchanged. |
| R004 | quality-attribute | validated | M001-a60bo7/S02 | none | Full test suite passes with 71 tests (0 failures, 0 skipped). Includes 12 new edge case unit tests for 3-column format (empty file, blank first rows, empty ID, single row, ID trim, multi-duplicate) and 1 new end-to-end integration test proving 3-column Excel→Word pipeline works correctly. All pre-existing tests remain green with zero regressions. |
| R005 | core-capability | validated | M003-g1w88x/S01 | none | docs/DocuFiller产品需求文档.md 已创建，覆盖所有 6 个功能模块（文件输入、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具），包含 Mermaid 流程图和用户界面设计 |
| R006 | core-capability | validated | M003-g1w88x/S01 | none | docs/DocuFiller技术架构文档.md 已创建，包含 14 个 public interface 定义、9 个二级章节、5 个 Mermaid 图（架构图、ER 图、3 个序列图），覆盖全部 15 个服务组件 |
| R007 | core-capability | validated | M003-g1w88x/S02 | none | README.md 包含 14 个服务接口架构表（grep 验证全部 14 个 I 前缀接口存在）、6 个功能模块完整覆盖、Excel 两列/三列格式说明、准确项目结构。验证命令全部通过。 |
| R008 | core-capability | validated | M003-g1w88x/S02 | none | CLAUDE.md 包含 17 个唯一 I 前缀标识符（>=14）、16 个关键数据模型、DetectExcelFormat 处理路径说明、DI 生命周期配置。grep 验证所有关键接口和数据模型均存在。 |
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

## Coverage Summary

- Active requirements: 0
- Mapped to slices: 0
- Validated: 20 (R001, R002, R003, R004, R005, R006, R007, R008, R009, R010, R011, R012, R014, R015, R016, R017, R018, R019, R020, R021)
- Unmapped active requirements: 0
