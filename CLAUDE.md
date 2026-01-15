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

DocuFiller is a WPF desktop application built with .NET 8 that follows the MVVM (Model-View-ViewModel) pattern with dependency injection and layered architecture.

### Core Architecture Components

1. **MVVM Pattern**:
   - Views (XAML) bind to ViewModels via DataContext
   - ViewModels handle business logic and state management
   - Models represent data structures

2. **Dependency Injection**:
   - Services are registered in `App.xaml.cs:ConfigureServices()`
   - Uses Microsoft.Extensions.DependencyInjection
   - Services are resolved via constructor injection

3. **Service Layer Architecture**:
   - **Document Processing**: `IDocumentProcessor` handles Word document manipulation using OpenXML
   - **Data Parsing**: `IDataParser` processes JSON data files
   - **File Operations**: `IFileService` manages file I/O operations
   - **Progress Reporting**: `IProgressReporter` tracks and reports processing progress
   - **File Scanning**: `IFileScanner` discovers template files in directories
   - **Directory Management**: `IDirectoryManager` handles folder operations

4. **Configuration System**:
   - Uses `App.config` for application settings
   - Configuration values accessed via `ConfigurationManager.AppSettings`
   - Supports runtime configuration updates

### Key Models and Data Flow

1. **ProcessRequest**: Encapsulates all data needed for document processing
2. **ProcessResult**: Contains results and statistics from processing operations
3. **ProgressEventArgs**: Carries progress information during long-running operations
4. **ContentControlData**: Represents Word document content controls that can be filled with data

### Document Processing Pipeline

1. Template file selection (.docx/.dotx)
2. JSON data file parsing and validation
3. Content control extraction from template
4. Data mapping and validation
5. Batch document generation with progress tracking
6. Output file management and organization

## Important Implementation Details

### OpenXML Integration
The application uses DocumentFormat.OpenXml SDK to manipulate Word documents:
- Content controls are identified by their tags/aliases
- Data is mapped to controls based on matching field names
- Supports text content replacement in structured documents

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

### JSON Data Structure
Expected JSON format is an array of objects where each object represents one document:
```json
[
  {
    "FieldName": "Value",
    "AnotherField": "AnotherValue"
  }
]
```

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

- `Examples/`: Contains sample JSON data files
- `Templates/`: Word template files (.docx/.dotx)
- `Output/`: Generated documents are saved here
- `Logs/`: Application log files
- `Services/`: Core business logic implementations
- `ViewModels/`: MVVM view models
- `Models/`: Data model classes
- `Utils/`: Helper utilities and common functionality