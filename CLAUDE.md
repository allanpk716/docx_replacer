# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 开发规范 (Development Guidelines)

- **语言**：使用中文回答问题和沟通
- **开发环境**：项目在 Windows 系统上开发和运行
- **BAT 脚本规范**：
  - BAT 脚本文件中不要包含中文字符
  - 修复脚本时优先在原脚本上修改，非必需情况不要创建新脚本
- **图片处理**：使用 MCP/Agent 截图或图片识别能力时，提交前需确保图片尺寸小于 1000x1000 像素
- **文档管理**：项目计划文件统一存放在 `docs\plans` 目录中

## Build and Development Commands

### Building the Project
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Build for release
dotnet build -c Release

# Publish the application
dotnet publish -c Release -r win-x64 --self-contained
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "TestName"
```

### Debugging
The project includes debug configurations in Visual Studio 2022 and supports hot reload during development.

## Architecture Overview

DocuFiller 是一个基于 .NET 8 + WPF 的桌面应用程序，遵循 MVVM 模式 + 依赖注入 + 分层架构，提供文档批量填充、富文本替换、批注追踪和审核清理 4 大功能模块。应用同时支持 GUI 和 CLI 双模式：无命令行参数时启动 WPF GUI，有参数时进入 CLI 模式（JSONL 输出，适合 LLM agent 集成）。

### Core Architecture Components

1. **MVVM Pattern**:
   - Views (XAML) bind to ViewModels via DataContext
   - ViewModels handle business logic and state management
   - Models represent data structures

2. **Dependency Injection**:
   - Services are registered in `App.xaml.cs:ConfigureServices()`
   - Uses Microsoft.Extensions.DependencyInjection
   - Services are resolved via constructor injection
   - 大部分服务为 Singleton，清理服务和窗口为 Transient

3. **Configuration System**:
   - 使用 `appsettings.json` 作为主配置文件
   - 通过 `IOptions<T>` 选项模式访问配置
   - 支持 `App.config` 向后兼容
   - 配置类：`AppSettings`, `LoggingSettings`, `FileProcessingSettings`, `PerformanceSettings`, `UISettings`

### Service Layer Architecture (10 个服务接口 + 2 个处理器)

| 服务 | 接口 | 实现类 | 职责 |
|------|------|--------|------|
| 文档处理 | `IDocumentProcessor` | `DocumentProcessorService` | 批量文档处理主入口（Excel 数据源） |
| Excel 数据解析 | `IExcelDataParser` | `ExcelDataParserService` | Excel 文件解析（两列/三列格式自动检测） |
| 文件操作 | `IFileService` | `FileService` | 文件读写、复制、验证 |
| 进度报告 | `IProgressReporter` | `ProgressReporterService` | 处理进度追踪与报告 |
| 文件扫描 | `IFileScanner` | `FileScannerService` | 文件夹中 .docx 文件递归发现 |
| 目录管理 | `IDirectoryManager` | `DirectoryManagerService` | 输出目录创建、时间戳文件夹、镜像目录结构 |
| 安全文本替换 | `ISafeTextReplacer` | `SafeTextReplacer` | 保留表格结构的文本替换（三种策略） |
| 格式化内容替换 | `ISafeFormattedContentReplacer` | `SafeFormattedContentReplacer` | 保留富文本格式的内容替换（上标/下标） |
| 模板缓存 | `ITemplateCacheService` | `TemplateCacheService` | 模板验证结果与控件信息缓存（含过期清理） |
| 文档清理 | `IDocumentCleanupService` | `DocumentCleanupService` | 去除批注痕迹、内容控件正常化（解包 SdtElement） |

CLI 组件（非服务接口，手写轻量实现）：

| 组件 | 类名 | 职责 |
|------|------|------|
| CLI 路由 | `CliRunner` | 参数解析、子命令分发、--help/--version 处理 |
| JSONL 输出 | `JsonlOutput` | 统一 envelope 格式的 JSONL 输出（type/status/timestamp/data） |
| 控制台附加 | `ConsoleHelper` | WinExe stdout P/Invoke（AttachConsole(-1)） |
| fill 命令 | `FillCommand` | Excel 数据填充模板，输出结果和进度 |
| cleanup 命令 | `CleanupCommand` | 批注和内容控件清理 |
| inspect 命令 | `InspectCommand` | 查询模板中的内容控件列表 |

非接口核心处理器：

| 处理器 | 类名 | 职责 |
|--------|------|------|
| 内容控件处理 | `ContentControlProcessor` | 协调内容控件处理流程（正文/页眉/页脚差异化 + 批注） |
| 批注管理 | `CommentManager` | Word 文档批注的创建与管理 |

### DI 生命周期选择

| 生命周期 | 适用场景 | 服务示例 |
|----------|----------|----------|
| **Singleton** | 无状态服务、全局缓存、线程安全服务 | IFileService, IExcelDataParser, ITemplateCacheService, ContentControlProcessor |
| **Transient** | 有状态服务、每次使用需新建实例 | IDocumentCleanupService, CleanupCommentProcessor, CleanupControlProcessor, ViewModels, Windows |

### Key Data Models

| 模型 | 用途 |
|------|------|
| `ProcessRequest` | 单文件处理请求（模板路径、数据路径、输出目录、命名模式） |
| `FolderProcessRequest` | 文件夹批量处理请求（含子文件夹扫描、目录结构保持、时间戳文件夹选项） |
| `ProcessResult` | 处理结果和统计信息（成功率、生成文件列表、错误/警告） |
| `ProgressEventArgs` | 进度事件参数（百分比、当前文件、完成/错误状态） |
| `ContentControlData` | 内容控件信息（Tag、标题、值、类型、位置：Body/Header/Footer） |
| `FormattedCellValue` | 带格式的单元格值（包含 TextFragment 列表） |
| `TextFragment` | 单个文本片段（文本 + IsSuperscript/IsSubscript 标记） |
| `DataStatistics` | 数据统计信息（总记录数、字段列表、文件大小） |
| `CleanupFileItem` | 清理文件项（文件路径、大小、状态：Pending/Processing/Success/Failure/Skipped） |
| `CleanupResult` | 清理结果（移除批注数、解包控件数） |
| `CleanupProgressEventArgs` | 清理进度事件（总数、已处理、成功/失败/跳过计数） |
| `ExcelValidationResult` | Excel 数据验证结果（错误、警告、摘要） |
| `ExcelFileSummary` | Excel 文件摘要（总行数、有效行数、重复关键词、无效格式关键词） |
| `InputSourceType` | 输入来源类型枚举（None/SingleFile/Folder） |
| `FolderStructure` | 文件夹结构信息（文件列表、子文件夹、.docx 总数） |

### CLI 接口

应用通过 `Program.cs` 入口点实现 CLI/GUI 双模式：`args.Length > 0` 时走 CLI 路径（绕过 WPF Application），否则启动 GUI。CLI 输出统一使用 JSONL 格式（每行一个 JSON 对象），适合 LLM agent 集成。

#### 子命令

| 子命令 | 用途 | 必需参数 | 可选参数 |
|--------|------|----------|----------|
| `fill` | 使用 Excel 数据批量填充 Word 模板 | `--template`, `--data`, `--output` | `--folder`, `--overwrite` |
| `cleanup` | 清理 Word 文档中的批注和内容控件 | `--input` | `--output`, `--folder` |
| `inspect` | 查询模板中的内容控件列表 | `--template` | — |

全局选项：`--help` / `-h`（帮助）、`--version` / `-v`（版本号）

#### JSONL 输出格式

每行一个 JSON 对象，统一 envelope schema：

```json
{"type":"...","status":"success|error","timestamp":"2025-04-23T14:30:00.0000000+08:00","data":{...}}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `type` | string | 输出类型（见下表） |
| `status` | string | `"success"` 或 `"error"` |
| `timestamp` | string | ISO 8601 UTC 时间戳 |
| `data` | object | 类型相关的数据负载 |

#### JSONL 输出类型

| type | status | 说明 | data 内容 |
|------|--------|------|-----------|
| `control` | success | inspect 输出的单个控件信息 | `{tag, title, contentType, location}` |
| `result` | success | 单个文件处理结果 | fill: `{file}`, cleanup: `{commentsRemoved, controlsUnwrapped, outputPath}` |
| `progress` | success | fill 命令进度更新 | `{current, total, percent, message}` |
| `summary` | success | 命令执行汇总 | fill: `{total, success, failed, duration}`, inspect: `{totalControls}` |
| `error` | error | 错误信息 | `{message, code}` |
| `help` | success | 帮助信息 | 命令/选项描述 |

#### 错误码

| 错误码 | 场景 |
|--------|------|
| `MISSING_ARGUMENT` | 缺少必需参数 |
| `FILE_NOT_FOUND` | 文件或目录不存在 |
| `FILL_ERROR` | 文档填充失败 |
| `CLEANUP_ERROR` | 文档清理失败 |
| `INSPECT_ERROR` | 模板检查失败 |
| `UNKNOWN_COMMAND` | 未识别的子命令 |

#### 使用示例

```bash
# 查看帮助
DocuFiller.exe --help
DocuFiller.exe fill --help

# 查询模板控件
DocuFiller.exe inspect --template report.docx

# 批量填充
DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output

# 填充并覆盖已有文件
DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output --overwrite

# 清理单个文件
DocuFiller.exe cleanup --input ./output/report_filled.docx

# 清理文件夹
DocuFiller.exe cleanup --input ./output --folder
```

**注意**：CLI 输出使用 `AttachConsole(-1)` P/Invoke 附加到父控制台。`dotnet run` 可能无法正确捕获 WinExe 进程的 stdout，应直接运行构建产物 `.exe` 文件验证输出。

### Document Processing Pipeline

1. Template file selection (.docx)
2. Excel 数据解析和验证
3. Content control extraction from template
4. Data mapping and validation
5. Batch document generation with progress tracking
6. Output file management and organization

## Important Implementation Details

### Excel 双格式处理路径

Excel 数据解析由 `ExcelDataParserService` 实现，通过内部的 `DetectExcelFormat` 方法自动检测格式：

- **两列格式**：第一列为关键词（`#xxx#` 格式），第二列为替换值
- **三列格式**：第一列为行标识 ID，第二列为关键词，第三列为替换值
- **检测机制**：读取第一个非空行的第一列值，如果匹配 `#xxx#` 模式则为两列格式，否则为三列格式
- **EPPlus 使用**：通过 Officeonerge.Excel.Epplus 包操作 Excel（非 EPPlus 官方包）
- **富文本支持**：解析单元格内的上标/下标格式，生成 `FormattedCellValue` 供格式化替换使用

**已知问题**：`ParseExcelFileAsync` 在空工作表（`worksheet.Dimension` 为 null）时会抛出 NullReferenceException。仅 `ValidateExcelFileAsync` 做了防护检查。

### OpenXML Integration
The application uses DocumentFormat.OpenXml SDK to manipulate Word documents:
- Content controls are identified by their tags/aliases
- Data is mapped to controls based on matching field names
- Supports text content replacement in structured documents
- 支持正文、页眉、页脚三个位置的内容控件替换

#### Table Content Control Handling (表格内容控件处理)
表格中的内容控件替换需要特别处理以保持表格结构不变。`SafeTextReplacer` 服务实现了三种替换策略：

| 场景 | 结构示意 | 检测方式 | 处理方法 |
|------|----------|----------|----------|
| 控件在单元格内 | `TableCell -> SdtCell` | `isInTableCell = true` | `ReplaceTextInTableCell` |
| 控件包装单元格 | `TableRow -> SdtCell -> TableCell` | `containsTableCell = true` | `ReplaceTextInWrappedTableCell` |
| 普通控件 | `SdtRun/SdtBlock` | 两者均为 false | `ReplaceTextStandard` |

**关键注意事项**：
1. **不要删除 TableCell 结构**：当 `containsTableCell = true` 时，控件包装了整个表格单元格，此时必须使用 `ReplaceTextInWrappedTableCell` 方法，该方法会找到被包装的 TableCell 并只替换其中的文本内容，而不会删除 TableCell 本身
2. **区分 SdtBlock 和 SdtRun**：块级控件（SdtBlock）包含完整的 Paragraph 结构，处理时需要确保容器内只有一个段落
3. **避免破坏其他控件**：在 SdtContentBlock 容器内可能存在多个段落，每个段落可能属于不同的控件，不能随意删除

**相关文件**：
- `Services/SafeTextReplacer.cs`: 核心替换逻辑实现
- `Utils/OpenXmlTableCellHelper.cs`: 表格单元格位置检测工具

#### 富文本格式替换
`ISafeFormattedContentReplacer` 处理带有上标/下标等格式的文本，将 `FormattedCellValue` 中的 `TextFragment` 列表转换为带格式的 OpenXML Run 元素。

#### 内容控件处理协调
`ContentControlProcessor` 协调整个内容控件处理流程：
- 遍历文档主体、所有页眉、所有页脚中的内容控件
- 对于嵌套控件，通过 `HasAncestorWithSameTag` 检测，只处理最外层控件
- 正文区域添加批注追踪，页眉/页脚区域跳过批注（OpenXML 不支持）

#### 批注追踪
`CommentManager` 管理 Word 文档批注：
- 批注格式：`此字段（正文）已于 YYYY年M月D日 HH:mm:ss 更新。标签：#关键词#，旧值：[旧值]，新值：新值`
- 仅正文区域添加批注，页眉/页脚不添加

### 审核清理功能
`IDocumentCleanupService` + `CleanupCommentProcessor` + `CleanupControlProcessor` 实现文档审核清理：
- 将批注标记的文本颜色恢复为黑色
- 删除批注范围标记和批注内容
- 解包内容控件（移除 SdtElement 包装，保留内部内容）
- 清理服务注册为 Transient（每次操作创建新实例）

### Error Handling and Logging
- Comprehensive exception handling with `GlobalExceptionHandler`
- Structured logging using Microsoft.Extensions.Logging
- Log files stored in `Logs` directory with configurable retention
- User-friendly error messages displayed in UI

### Thread Safety
- Long-running operations use async/await patterns
- Progress reporting is thread-safe via events
- UI updates are marshaled to the UI thread

## File Structure Notes

```
docx_replacer/                         # 主项目根目录
├── App.xaml.cs                        # 应用入口，DI 注册配置
├── DocuFiller.csproj                  # 项目文件
├── Configuration/                     # 配置类（AppSettings, LoggingSettings 等）
├── Models/                            # 数据模型
├── ViewModels/                        # 视图模型
├── Views/                             # XAML 视图
├── Cli/                               # CLI 模式（双模式入口、命令、JSONL 输出）
│   ├── CliRunner.cs                   # 参数解析和命令分发
│   ├── JsonlOutput.cs                 # JSONL 格式化输出
│   ├── ConsoleHelper.cs               # WinExe stdout P/Invoke
│   └── Commands/                      # 子命令处理器
│       ├── FillCommand.cs             # fill: Excel 数据填充
│       ├── CleanupCommand.cs          # cleanup: 批注/控件清理
│       └── InspectCommand.cs          # inspect: 控件查询
├── Program.cs                         # 应用入口（CLI/GUI 双模式分发）
├── Services/                          # 业务服务
│   └── Interfaces/                    # 10 个服务接口定义
├── Converters/                        # WPF 值转换器
├── Utils/                             # 工具类（OpenXmlTableCellHelper 等）
├── Exceptions/                        # 自定义异常
├── DocuFiller/                        # WPF 资源字典和样式
│   ├── Services/                      # 资源相关服务
│   ├── Utils/                         # 资源工具
│   ├── ViewModels/                    # 资源 ViewModel
│   └── Views/                         # 资源视图
├── Tests/                             # 单元测试和集成测试
│   ├── DocuFiller.Tests/              # 主测试项目
│   ├── Integration/                   # 集成测试
│   ├── Data/                          # 测试数据
│   └── Templates/                     # 测试模板
├── scripts/                           # 构建和发布脚本
│   └── config/                        # 脚本配置
├── docs/                              # 项目文档
│   ├── features/                      # 功能说明文档
│   ├── plans/                         # 开发计划文档
│   └── *.md                           # 其他文档
├── Examples/                          # 示例数据文件（输出目录）
├── Templates/                         # 模板文件（输出目录）
├── Logs/                              # 日志文件（输出目录）
└── Output/                            # 生成的文档（输出目录）
```
