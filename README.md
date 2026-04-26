# DocuFiller - Word文档批量填充工具

## 项目简介

DocuFiller是一个基于C# + .NET 8 + WPF开发的桌面应用程序，用于通过Excel数据文件批量填充Word文档中的内容控件。支持富文本格式保留、批注追踪和审核清理等功能。

## 主要功能

### 1. 文件输入与管理
- **单文件模式**：拖入单个 .docx 文件作为处理模板
- **文件夹模式**：拖入文件夹，系统自动递归扫描子目录中的所有 .docx 文件
- 支持 .docx 格式（不支持 .doc 旧格式和 .dotx）

### 2. Excel 数据源
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

### CLI 使用方法

DocuFiller 支持命令行操作，输出为 JSONL 格式（每行一个 JSON 对象），适合 LLM agent 和脚本集成。

#### inspect — 查询模板控件

```bash
DocuFiller.exe inspect --template <模板文件路径>
```

输出每行一个内容控件信息，最后输出汇总行：

```
{"type":"control","status":"success","timestamp":"...","data":{"tag":"#姓名#","title":"姓名","contentType":"Text","location":"Body"}}
{"type":"summary","status":"success","timestamp":"...","data":{"totalControls":1}}
```

#### fill — 批量填充文档

```bash
DocuFiller.exe fill --template <模板文件路径> --data <Excel数据文件> --output <输出目录> [--overwrite]
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--template` | 是 | Word 模板文件路径（.docx） |
| `--data` | 是 | Excel 数据文件路径（.xlsx） |
| `--output` | 是 | 输出目录 |
| `--overwrite` | 否 | 覆盖已存在的文件 |

#### cleanup — 清理文档

```bash
DocuFiller.exe cleanup --input <文件或目录路径> [--output <输出目录>] [--folder]
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--input` | 是 | 文件或文件夹路径 |
| `--output` | 否 | 输出目录（不指定则原地清理） |
| `--folder` | 否 | 指定输入为文件夹模式 |

#### JSONL 输出格式

所有 CLI 输出统一使用 JSONL envelope 格式：

```json
{"type":"result|control|progress|summary|error","status":"success|error","timestamp":"ISO8601","data":{...}}
```

**错误输出示例**：
```json
{"type":"error","status":"error","timestamp":"...","data":{"message":"模板文件不存在: report.docx","code":"FILE_NOT_FOUND"}}
```

### 4. 批注追踪
- 正文区域自动添加变更批注，记录旧值、新值和替换时间
- 批注格式：`此字段（正文）已于 2025年4月23日 14:30:00 更新。标签：#姓名#，旧值：[张三]，新值：李四`
- 页眉/页脚区域不添加批注（OpenXML API限制）

### 5. 审核清理
- 独立清理窗口，一键去除DocuFiller处理痕迹
- 将批注标记的文本颜色恢复为黑色
- 删除批注范围标记和批注内容
- 解包内容控件（移除SdtElement包装，保留内部内容）

### 6. CLI 接口（LLM Agent 集成）
- 支持 `fill`、`cleanup`、`inspect` 三个子命令，适合脚本化和自动化
- JSONL 格式输出（每行一个 JSON 对象），方便 LLM agent 解析
- 支持进度、结果、错误等结构化 JSON 输出
- 无命令行参数时自动进入 GUI 模式

## 技术架构

### 核心框架

| 技术 | 版本 | 用途 |
|------|------|------|
| **.NET** | 8.0 | 应用程序运行时框架 |
| **WPF** | .NET 8 内置 | 桌面UI框架 |
| **DocumentFormat.OpenXml** | 3.0.1 | Word文档读写操作 |
| **EPPlus** | 7.5.2 | Excel文件读写操作 |
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
| Excel数据解析 | `IExcelDataParser` | `ExcelDataParserService` | Excel文件解析（两列/三列格式） |
| 文件操作 | `IFileService` | `FileService` | 文件读写、复制、验证 |
| 进度报告 | `IProgressReporter` | `ProgressReporterService` | 处理进度追踪与报告 |
| 文件扫描 | `IFileScanner` | `FileScannerService` | 文件夹中.docx文件发现 |
| 目录管理 | `IDirectoryManager` | `DirectoryManagerService` | 输出目录创建、时间戳文件夹 |
| 安全文本替换 | `ISafeTextReplacer` | `SafeTextReplacer` | 保留表格结构的文本替换 |
| 格式化内容替换 | `ISafeFormattedContentReplacer` | `SafeFormattedContentReplacer` | 保留富文本格式的内容替换 |
| 模板缓存 | `ITemplateCacheService` | `TemplateCacheService` | 模板验证结果与控件信息缓存 |
| 关键词验证 | `IKeywordValidationService` | — | 关键词格式、重复性验证 |
| 文档清理 | `IDocumentCleanupService` | `DocumentCleanupService` | 去除批注痕迹、内容控件正常化 |

此外还有两个非接口的核心处理器：
- **`ContentControlProcessor`**：内容控件处理协调（含批注逻辑）
- **`CommentManager`**：Word文档批注的创建与管理

### 依赖注入

- 服务在 `App.xaml.cs:ConfigureServices()` 中注册
- 使用 Microsoft.Extensions.DependencyInjection 容器
- 通过构造函数注入解析服务依赖
- 大部分服务为Singleton生命周期，清理服务和窗口为Transient

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
├── .env                               # 环境变量配置（gitignore，包含服务器连接信息）
├── Configuration/                     # 配置类（AppSettings, LoggingSettings 等）
├── Models/                            # 数据模型
├── ViewModels/                        # 视图模型
├── Views/                             # XAML 视图
├── Services/                          # 业务服务
│   ├── Interfaces/                    # 服务接口定义
│   └── ...                            # 服务实现
├── Cli/                               # CLI 模式组件
│   ├── CliRunner.cs                   # 参数解析和命令分发
│   ├── JsonlOutput.cs                 # JSONL 格式化输出
│   ├── ConsoleHelper.cs               # WinExe stdout P/Invoke
│   └── Commands/                      # 子命令处理器（Fill/Cleanup/Inspect）
├── Program.cs                         # 应用入口（CLI/GUI 双模式分发）
├── Converters/                        # WPF 值转换器
├── Utils/                             # 工具类（OpenXmlTableCellHelper 等）
├── Exceptions/                        # 自定义异常
├── DocuFiller/                        # WPF 资源字典和样式
│   ├── Services/                      # 资源相关服务
│   ├── Utils/                         # 资源工具
│   ├── ViewModels/                    # 资源 ViewModel
│   └── Views/                         # 资源视图
├── update-server/                     # Go 更新服务器（Velopack release hosting）
│   ├── main.go                        # 入口
│   ├── handler/                       # HTTP 处理器（upload/list/promote/static）
│   ├── middleware/                     # Bearer Token 鉴权
│   ├── storage/                       # 文件系统存储 + 旧版本清理
│   └── model/                         # Velopack ReleaseFeed 数据模型
├── Tests/                             # 单元测试和集成测试
│   ├── DocuFiller.Tests/              # 主测试项目
│   ├── Integration/                   # 集成测试
│   ├── Data/                          # 测试数据
│   └── Templates/                     # 测试模板
├── scripts/                           # 构建和部署脚本
│   ├── build.bat                      # 构建入口（可选上传到 beta/stable）
│   ├── build-internal.bat             # 构建核心逻辑（Velopack 打包 + 上传）
│   ├── install-ssh.bat                # OpenSSH 离线安装脚本（Windows Server）
│   ├── post_reboot_test.py            # 更新服务器健康检查脚本
│   └── config/                        # 脚本配置
├── docs/                              # 项目文档
│   ├── update-server-deployment.md    # 🔑 更新服务器部署指南
│   ├── ssh-offline-install.md         # 🔑 Windows Server SSH 离线安装指南
│   ├── features/                      # 功能说明文档
│   ├── plans/                         # 开发计划文档
│   └── *.md                           # 其他文档
├── Examples/                          # 示例数据文件（输出目录）
├── Templates/                         # 模板文件（输出目录）
├── Logs/                              # 日志文件（输出目录）
└── Output/                            # 生成的文档（输出目录）
```

## 使用方法

### 1. 准备模板文件
- 在Word中创建文档模板
- 插入内容控件并设置标记（Tag值）
- 保存为 .docx 格式

### 2. 准备数据文件

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
- 选择Excel数据文件
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
# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 运行 GUI 模式
dotnet run

# CLI 模式（直接运行构建产物）
dotnet build -c Release
.\bin\Release\net8.0-windows\DocuFiller.exe inspect --template report.docx
.\bin\Release\net8.0-windows\DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output
.\bin\Release\net8.0-windows\DocuFiller.exe cleanup --input ./output
```

## 发布 Release

详见项目 scripts/ 目录下的构建和发布脚本。

快速发布：
```bash
# 稳定版
git tag v1.0.0 && git push origin v1.0.0 && scripts\release.bat

# 测试版
git tag v1.0.1-beta && git push origin v1.0.1-beta && scripts\release.bat
```

## 配置说明

应用程序通过 `appsettings.json` 和 `App.config` 文件进行配置：

- **日志配置**：日志级别、保留天数、文件路径
- **文件处理**：最大文件大小、支持的扩展名
- **性能配置**：并发处理数、超时时间
- **UI配置**：自动保存、进度显示等

### 更新服务配置

在 `appsettings.json` 中配置自动更新：

```json
{
  "Update": {
    "UpdateUrl": "http://<服务器IP>:<端口>",
    "Channel": "stable"
  }
}
```

- `UpdateUrl`：更新服务器地址（不含通道路径）
- `Channel`：`stable` 或 `beta`

## 部署文档

### 更新服务器部署

将 Go 更新服务器部署到 Windows Server，提供 Velopack 自动更新的 release 托管服务。

> **完整指南**：[docs/update-server-deployment.md](docs/update-server-deployment.md)
>
> 涵盖：编译、上传、NSSM 服务注册、防火墙配置、API 测试、客户端配置、发布流程。

**环境变量**（配置在 `.env` 文件中，已 gitignore）：

| 变量 | 说明 |
|------|------|
| `UPDATE_SERVER_HOST` | 更新服务器 IP |
| `UPDATE_SERVER_USER` | SSH 用户名 |
| `UPDATE_SERVER_PASSWORD` | SSH 密码 |
| `UPDATE_SERVER_SSH_PORT` | SSH 端口 |
| `UPDATE_SERVER_PORT` | 更新服务 HTTP 端口 |
| `UPDATE_SERVER_API_TOKEN` | API 鉴权 Token |

### Windows Server SSH 配置

为无法联网的 Windows Server 离线安装 OpenSSH Server。

> **完整指南**：[docs/ssh-offline-install.md](docs/ssh-offline-install.md)
>
> 涵盖：离线安装步骤、一键安装脚本、端口修改、防火墙配置、常见问题排查。

**快速使用**：

1. 从 [Win32-OpenSSH Releases](https://github.com/PowerShell/Win32-OpenSSH/releases/latest) 下载 `OpenSSH-Win64.zip`
2. 将 zip 和 `scripts/install-ssh.bat` 拷贝到服务器
3. 以管理员身份运行：`install-ssh.bat 30000`

## 日志和错误处理

- 所有操作都有详细的日志记录
- 日志文件保存在 `Logs` 目录
- 支持不同级别的日志输出
- 完善的异常处理机制（全局异常处理）

## 许可证

Copyright © Allan 2024

## 联系方式

如有问题或建议，请联系开发者。
