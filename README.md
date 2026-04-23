# DocuFiller - Word文档批量填充工具

## 项目简介

DocuFiller是一个基于C# + .NET 8 + WPF开发的桌面应用程序，用于通过JSON或Excel数据文件批量填充Word文档中的内容控件。支持富文本格式保留、批注追踪、审核清理和格式转换等功能。

## 主要功能

### 1. 文件输入与管理
- **单文件模式**：拖入单个 .docx 文件作为处理模板
- **文件夹模式**：拖入文件夹，系统自动递归扫描子目录中的所有 .docx 文件
- 支持 .docx 格式（不支持 .doc 旧格式和 .dotx）

### 2. JSON/Excel 双数据源
- **JSON格式**：数组格式，每个对象代表一组关键词-值对，字段名对应内容控件Tag值
- **Excel两列格式**：第一列为关键词（`#关键词#`格式），第二列为替换值
- **Excel三列格式**：第一列为行标识（ID），第二列为关键词，第三列为替换值
- 系统自动检测Excel列格式（两列/三列）
- 支持Excel富文本格式保留（上标、下标等），详见[Excel数据支持指南](docs/excel-data-user-guide.md)

### 3. 文档处理
- 通过Word内容控件（Content Control）的Tag属性精确匹配关键词
- 支持正文、页眉、页脚三个位置的内容控件替换
- 表格中内容控件安全替换（保留表格结构）
- 富文本格式保留（上标、下标）
- 文件夹批量处理保持目录结构
- 时间戳子文件夹输出
- 详见[页眉页脚支持说明](docs/features/header-footer-support.md)

### 4. 批注追踪
- 正文区域自动添加变更批注，记录旧值、新值和替换时间
- 批注格式：`此字段（正文）已于 2025年4月23日 14:30:00 更新。标签：#姓名#，旧值：[张三]，新值：李四`
- 页眉/页脚区域不添加批注（OpenXML API限制）

### 5. 审核清理
- 独立清理窗口，一键去除DocuFiller处理痕迹
- 将批注标记的文本颜色恢复为黑色
- 删除批注范围标记和批注内容
- 解包内容控件（移除SdtElement包装，保留内部内容）

### 6. 转换工具
- JSON → Excel格式转换
- 支持批量转换多个JSON文件
- 输出两列格式Excel（关键词 | 值）

## 技术架构

### 核心框架

| 技术 | 版本 | 用途 |
|------|------|------|
| **.NET** | 8.0 | 应用程序运行时框架 |
| **WPF** | .NET 8 内置 | 桌面UI框架 |
| **DocumentFormat.OpenXml** | 3.0.1 | Word文档读写操作 |
| **EPPlus** | 7.5.2 | Excel文件读写操作 |
| **Newtonsoft.Json** | 13.0.3 | JSON数据文件解析 |
| **Microsoft.Extensions.DependencyInjection** | — | 依赖注入容器 |
| **Microsoft.Extensions.Logging** | — | 日志记录框架 |
| **Microsoft.Extensions.Options** | — | 配置选项模式 |

### 架构设计

项目遵循MVVM（Model-View-ViewModel）模式 + 分层架构：
- **Views** (XAML)：通过 DataContext 绑定到 ViewModels
- **ViewModels**：处理业务逻辑和状态管理
- **Services**：核心业务逻辑实现，通过接口抽象
- **Models**：数据结构定义

### 服务层架构

核心服务接口和实现：

| 服务 | 接口 | 实现类 | 职责 |
|------|------|--------|------|
| 文档处理 | `IDocumentProcessor` | `DocumentProcessorService` | 批量文档处理主入口 |
| JSON数据解析 | `IDataParser` | `DataParserService` | JSON文件解析与验证 |
| Excel数据解析 | `IExcelDataParser` | `ExcelDataParserService` | Excel文件解析（两列/三列格式） |
| 文件操作 | `IFileService` | `FileService` | 文件读写、复制、验证 |
| 进度报告 | `IProgressReporter` | `ProgressReporterService` | 处理进度追踪与报告 |
| 文件扫描 | `IFileScanner` | `FileScannerService` | 文件夹中.docx文件发现 |
| 目录管理 | `IDirectoryManager` | `DirectoryManagerService` | 输出目录创建、时间戳文件夹 |
| JSON↔Excel转换 | `IExcelToWordConverter` | `ExcelToWordConverterService` | JSON与Excel格式互转 |
| 安全文本替换 | `ISafeTextReplacer` | `SafeTextReplacer` | 保留表格结构的文本替换 |
| 格式化内容替换 | `ISafeFormattedContentReplacer` | `SafeFormattedContentReplacer` | 保留富文本格式的内容替换 |
| 模板缓存 | `ITemplateCacheService` | `TemplateCacheService` | 模板验证结果与控件信息缓存 |
| 关键词验证 | `IKeywordValidationService` | — | 关键词格式、重复性验证 |
| 文档清理 | `IDocumentCleanupService` | `DocumentCleanupService` | 去除批注痕迹、内容控件正常化 |
| 更新服务 | `IUpdateService` | `UpdateClientService` | 应用自动更新检查与下载 |

此外还有两个非接口的核心处理器：
- **`ContentControlProcessor`**：内容控件处理协调（含批注逻辑）
- **`CommentManager`**：Word文档批注的创建与管理

### 依赖注入

- 服务在 `App.xaml.cs:ConfigureServices()` 中注册
- 使用 Microsoft.Extensions.DependencyInjection 容器
- 通过构造函数注入解析服务依赖
- 大部分服务为Singleton生命周期，清理服务和窗口为Transient

### 文档处理管道

```
1. 模板文件选择 (.docx)
         ↓
2. JSON/Excel 数据解析和验证
         ↓
3. 从模板提取内容控件
         ↓
4. 数据映射和验证
         ↓
5. 批量文档生成（带进度跟踪）
         ↓
6. 输出文件管理和组织
```

### OpenXML 集成 - 表格内容控件处理

文档使用 DocumentFormat.OpenXml SDK 操作 Word 文档，支持复杂的内容控件替换：

**表格中的内容控件替换需要特别处理**，`SafeTextReplacer` 服务实现了三种替换策略：

| 场景 | 结构示意 | 检测方式 | 处理方法 |
|------|----------|----------|----------|
| 控件在单元格内 | `TableCell → SdtCell` | `isInTableCell = true` | `ReplaceTextInTableCell` |
| 控件包装单元格 | `TableRow → SdtCell → TableCell` | `containsTableCell = true` | `ReplaceTextInWrappedTableCell` |
| 普通控件 | `SdtRun/SdtBlock` | 两者均为 false | `ReplaceTextStandard` |

**关键注意事项**：
1. **不要删除 TableCell 结构**：当 `containsTableCell = true` 时，控件包装了整个表格单元格，此时必须使用 `ReplaceTextInWrappedTableCell` 方法，该方法会找到被包装的 TableCell 并只替换其中的文本内容，而不会删除 TableCell 本身
2. **区分 SdtBlock 和 SdtRun**：块级控件（SdtBlock）包含完整的 Paragraph 结构，处理时需要确保容器内只有一个段落
3. **避免破坏其他控件**：在 SdtContentBlock 容器内可能存在多个段落，每个段落可能属于不同的控件，不能随意删除

**相关文件**：
- `Services/SafeTextReplacer.cs` - 核心替换逻辑实现
- `Utils/OpenXmlTableCellHelper.cs` - 表格单元格位置检测工具

### 核心数据模型

| 模型 | 用途 |
|------|------|
| `ProcessRequest` | 单文件处理请求参数 |
| `FolderProcessRequest` | 文件夹批量处理请求参数 |
| `ProcessResult` | 处理结果和统计信息 |
| `ProgressEventArgs` | 进度事件参数 |
| `ContentControlData` | 内容控件信息（Tag、标题、值、类型、位置） |
| `FormattedCellValue` | 带格式的单元格值（文本片段列表） |
| `TextFragment` | 单个文本片段（文本 + 上标/下标标记） |
| `JsonKeywordItem` | JSON关键词项（键名、值、来源文件） |
| `JsonProjectModel` | JSON项目数据 |
| `CleanupFileItem` | 清理文件项（文件路径、大小、状态） |
| `CleanupResult` | 清理结果（移除批注数、解包控件数） |
| `ExcelValidationResult` | Excel数据验证结果 |
| `ExcelFileSummary` | Excel文件摘要（行数、有效行数、重复项） |
| `FileInfo` | 文件信息 |
| `FolderStructure` | 文件夹结构信息 |

## 项目结构

```
docx_replacer/                         # 主项目根目录
├── App.xaml.cs                        # 应用入口，DI 注册配置
├── DocuFiller.csproj                  # 项目文件
├── Configuration/                     # 配置类（AppSettings, LoggingSettings 等）
├── Models/                            # 数据模型
│   └── Update/                        # 更新相关模型
├── ViewModels/                        # 视图模型
│   └── Update/                        # 更新相关 ViewModel
├── Views/                             # XAML 视图
│   └── Update/                        # 更新相关窗口
├── Services/                          # 业务服务
│   ├── Interfaces/                    # 14 个服务接口定义
│   └── Update/                        # 更新服务实现
├── External/                          # 外部工具配置（update-client）
├── Converters/                        # WPF 值转换器
├── Utils/                             # 工具类（OpenXmlTableCellHelper 等）
├── Exceptions/                        # 自定义异常
├── DocuFiller/                        # WPF 资源字典和样式
│   ├── Services/                      # 资源相关服务
│   │   └── Update/
│   ├── Utils/                         # 资源工具
│   ├── ViewModels/                    # 资源 ViewModel
│   └── Views/                         # 资源视图
├── Tools/                             # 诊断和测试工具
│   ├── E2ETest/                       # 端到端测试
│   ├── ExcelToWordVerifier/           # Excel 转 Word 验证工具
│   ├── ExcelFormattedTestGenerator/   # Excel 格式化测试生成器
│   ├── CompareDocumentStructure/      # 文档结构比较工具
│   ├── ControlRelationshipAnalyzer/   # 控件关系分析器
│   ├── DeepDiagnostic/                # 深度诊断工具
│   ├── DiagnoseTableStructure/        # 表格结构诊断
│   ├── StepByStepSimulator/           # 逐步模拟器
│   ├── TableCellTest/                 # 表格单元格测试
│   └── TableStructureAnalyzer/        # 表格结构分析器
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

### 相关项目

- **[update-server](https://github.com/LiteHomeLab/update-server)** - 应用自动更新服务器
  - 独立的 Go 语言项目
  - 支持多应用的发布和更新
  - 用于 DocuFiller 的自动更新功能

## 使用方法

### 1. 准备模板文件
- 在Word中创建文档模板
- 插入内容控件并设置标记（Tag值）
- 保存为 .docx 格式

### 2. 准备数据文件

#### JSON格式
创建JSON格式的数据文件，字段名对应模板中内容控件的Tag值：
```json
[
  {
    "姓名": "张三",
    "性别": "男",
    "年龄": "28"
  },
  {
    "姓名": "李四",
    "性别": "女",
    "年龄": "25"
  }
]
```

#### Excel格式
创建 Excel (.xlsx) 文件，系统自动检测两列或三列格式：

**两列模式**（第一列匹配 `#xxx#` 格式时自动识别）：

| 关键词 | 值 |
|--------|-----|
| #姓名# | 张三 |
| #性别# | 男 |

**三列模式**（第一列不匹配 `#xxx#` 格式时自动识别）：

| ID | 关键词 | 值 |
|----|--------|-----|
| 1 | #姓名# | 张三 |
| 2 | #性别# | 男 |

- Excel支持富文本格式保留（上标、下标等）
- 详见[Excel数据支持指南](docs/excel-data-user-guide.md)

### 3. 运行应用程序
- 选择Word模板文件
- 选择JSON或Excel数据文件
- 设置输出目录
- 点击开始处理

### 4. 审核清理（可选）
- 打开清理窗口
- 选择已处理的 .docx 文件
- 一键去除批注痕迹和内容控件包装

## 编译和运行

### 环境要求
- .NET 8 SDK
- Visual Studio 2022 或 VS Code
- Windows 10/11

### 编译步骤
```bash
# 克隆项目
git clone <repository-url>
cd docx_replacer

# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run
```

## 发布 Release

DocuFiller 使用自动化脚本发布到更新服务器。详见 [部署与发布指南](docs/deployment-guide.md)。

快速发布：
```bash
# 稳定版
git tag v1.0.0 && git push origin v1.0.0 && scripts\release.bat

# 测试版
git tag v1.0.1-beta && git push origin v1.0.1-beta && scripts\release.bat
```

## 配置说明

应用程序通过 `App.config` 文件进行配置：

- **日志配置**：日志级别、保留天数、文件路径
- **文件处理**：最大文件大小、支持的扩展名
- **性能配置**：并发处理数、超时时间
- **UI配置**：自动保存、进度显示等

## 日志和错误处理

- 所有操作都有详细的日志记录
- 日志文件保存在 `Logs` 目录
- 支持不同级别的日志输出
- 完善的异常处理机制（全局异常处理）

## 许可证

Copyright © Allan 2024

## 联系方式

如有问题或建议，请联系开发者。
