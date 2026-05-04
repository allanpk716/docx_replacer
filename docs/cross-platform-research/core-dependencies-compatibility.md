# DocuFiller 核心依赖库跨平台兼容性调研报告

> **调研日期**: 2026-05  
> **调研范围**: DocuFiller 全部 NuGet 依赖在 net8.0（无 Windows 后缀）目标框架下的兼容性评估  
> **基于**: 各 NuGet 包官方文档、NuGet Gallery 元数据、GitHub 仓库、社区实践  
> **版本**: 1.0

---

## 目录

1. [调研概述](#1-调研概述)
2. [DocumentFormat.OpenXml 跨平台兼容性](#2-documentformatopenxml-跨平台兼容性)
3. [EPPlus 跨平台兼容性](#3-epplus-跨平台兼容性)
4. [Microsoft.Extensions.* 系列跨平台兼容性](#4-microsoftextensions-系列跨平台兼容性)
5. [CommunityToolkit.Mvvm 跨平台兼容性](#5-communitytoolkitmvvm-跨平台兼容性)
6. [Services 层服务接口分析](#6-services-层服务接口分析)
7. [System.Configuration.ConfigurationManager 兼容性](#7-systemconfigurationconfigurationmanager-兼容性)
8. [潜在问题与风险](#8-潜在问题与风险)
9. [替代方案](#9-替代方案)
10. [对 DocuFiller 的建议](#10-对-docufiller-的建议)
11. [优缺点总结](#11-优缺点总结)
12. [调研日期与信息来源](#12-调研日期与信息来源)

---

## 1. 调研概述

### 1.1 调研背景

DocuFiller 当前目标框架为 `net8.0-windows`，依赖 WPF 作为 UI 框架。迁移到跨平台 UI 框架（如 Avalonia）后，需要将目标框架改为 `net8.0`（无 Windows 后缀），这意味着所有 NuGet 依赖必须在纯 .NET 8 运行时上正常工作，不能依赖 Windows 特定 API。

### 1.2 依赖全景图

DocuFiller 的 NuGet 依赖按功能分为以下五类：

| 分类 | 包名 | 版本 | 用途 |
|------|------|------|------|
| **核心业务** | `DocumentFormat.OpenXml` | 3.0.1 | Word 文档操作（OOXML 解析/生成/修改） |
| **核心业务** | `EPPlus` | 7.5.2 | Excel 文件解析（数据源读取） |
| **MVVM 框架** | `CommunityToolkit.Mvvm` | 8.4.0 | ViewModel 源代码生成（ObservableObject、RelayCommand 等） |
| **基础设施** | `Microsoft.Extensions.DependencyInjection` | 8.0.0 | 依赖注入容器 |
| **基础设施** | `Microsoft.Extensions.Logging` | 8.0.0 | 日志抽象层 |
| **基础设施** | `Microsoft.Extensions.Logging.Console` | 8.0.0 | 控制台日志提供程序 |
| **基础设施** | `Microsoft.Extensions.Logging.Debug` | 8.0.0 | 调试日志提供程序 |
| **基础设施** | `Microsoft.Extensions.Configuration` | 8.0.0 | 配置抽象层 |
| **基础设施** | `Microsoft.Extensions.Configuration.Json` | 8.0.1 | JSON 配置文件支持 |
| **基础设施** | `Microsoft.Extensions.Configuration.Binder` | 8.0.0 | 配置绑定到 POCO |
| **基础设施** | `Microsoft.Extensions.Configuration.EnvironmentVariables` | 8.0.0 | 环境变量配置源 |
| **基础设施** | `Microsoft.Extensions.Configuration.Xml` | 8.0.0 | XML 配置文件支持 |
| **基础设施** | `Microsoft.Extensions.Http` | 8.0.0 | IHttpClientFactory 集成 |
| **基础设施** | `Microsoft.Extensions.Options.ConfigurationExtensions` | 8.0.0 | Options 模式与配置绑定 |
| **遗留兼容** | `System.Configuration.ConfigurationManager` | 8.0.0 | 旧版 App.config 读取（仅 1 处使用） |
| **自动更新** | `Velopack` | 0.0.1298 | 自动更新（T01 已单独调研） |

### 1.3 关键结论预览

**DocuFiller 的所有核心依赖均支持 net8.0（无 Windows 后缀）跨平台运行。** 迁移到跨平台 UI 框架后，核心业务逻辑（文档处理 + 数据解析）无需替换任何依赖库。唯一需要处理的 Windows 特定代码是 `Cli/ConsoleHelper.cs` 中的 kernel32.dll P/Invoke 调用，以及 `App.xaml.cs` 中的一处 `System.Configuration.ConfigurationManager.AppSettings` 读取。

---

## 2. DocumentFormat.OpenXml 跨平台兼容性

### 2.1 概述

DocumentFormat.OpenXml 是微软官方的 Open XML SDK，用于操作 Office Open XML 格式文档（Word .docx、Excel .xlsx、PowerPoint .pptx）。它是 DocuFiller **最核心的业务依赖**——负责 Word 模板的解析、内容控件的读取和替换、文档的生成与保存。

### 2.2 目标框架支持

DocumentFormat.OpenXml 3.0.1 的 NuGet 包同时支持：

| 目标框架 | 支持状态 |
|----------|---------|
| .NET Standard 2.0 | ✅ 直接兼容 |
| .NET 8.0 | ✅ 直接兼容 |
| .NET 8.0-android | ✅ 兼容 |
| .NET 8.0-ios | ✅ 兼容 |
| .NET 8.0-maccatalyst | ✅ 兼容 |
| .NET 8.0-macos | ✅ 兼容 |
| .NET 8.0-windows | ✅ 兼容 |
| .NET 8.0-browser | ✅ 兼容 |
| .NET Framework 3.5+ | ✅ 兼容 |

**关键事实**: 该包的最低兼容目标是 .NET Standard 2.0，这意味着它可以在**所有**支持 .NET Standard 2.0 的运行时上运行，包括 Windows、macOS 和 Linux。它**不依赖** Windows 特定 API、COM 互操作或任何原生库。

### 2.3 纯托管实现

DocumentFormat.OpenXml 3.0 是**纯 C# 托管实现**，具有以下特征：

- **无 COM 互操作**: 不依赖 `Microsoft.Office.Interop.Word` 或任何 Office COM 组件，不需要安装 Microsoft Office
- **无 Windows API**: 不调用 Win32 API、不使用 P/Invoke、不依赖 Windows 特定的 `System.IO.Packaging`（v3.0 使用自带的 `System.IO.Packaging` 实现）
- **无原生库**: 不包含任何平台特定的原生二进制文件
- **AOT 友好**: v3.0 进行了架构重构以改善 AOT 编译和 trimming 支持

### 2.4 性能特征

| 维度 | 说明 |
|------|------|
| **内存占用** | 流式处理，内存占用与文档大小成正比，无需加载整个文档到内存 |
| **I/O 模型** | 基于 `System.IO.Packaging`，使用 ZIP 流式读写 |
| **CPU 占用** | 纯托管 XML 解析，无原生代码调用开销 |
| **跨平台性能差异** | 无明显差异——纯 C# 代码在各平台表现一致 |

### 2.5 已知限制

1. **不渲染文档**: Open XML SDK 只操作文档的 XML 结构，不渲染文档预览。这是设计上的选择，不影响跨平台兼容性
2. **部分高级功能**: 某些与 Office 应用紧密耦合的高级功能（如 VBA 宏项目操作）可能有限制，但 DocuFiller 不使用这些功能
3. **图片处理**: 文档中的嵌入图片以二进制形式存储在 OOXML 包中，SDK 仅负责读写二进制数据，不涉及平台特定的图像编解码 API

### 2.6 兼容性评估

| 维度 | 评分 | 说明 |
|------|------|------|
| net8.0 支持 | ✅ 完全支持 | .NET Standard 2.0 目标，全平台兼容 |
| Windows 特定依赖 | ❌ 无 | 纯托管实现 |
| 性能 | ✅ 优秀 | 流式处理，无原生调用 |
| 维护状态 | ✅ 活跃 | 微软官方维护，持续更新 |

> 来源: [NuGet Gallery - DocumentFormat.OpenXml 3.0.1](https://www.nuget.org/packages/DocumentFormat.OpenXml/3.0.1)、[Microsoft Learn - Open XML SDK](https://learn.microsoft.com/en-us/office/open-xml/what-s-new-in-the-open-xml-sdk)、[GitHub - OfficeDev/Open-XML-SDK](https://github.com/OfficeDev/Open-XML-SDK)

---

## 3. EPPlus 跨平台兼容性

### 3.1 概述

EPPlus 是 DocuFiller 的**第二核心业务依赖**，负责 Excel 数据源文件的解析。DocuFiller 使用 EPPlus 读取 .xlsx 文件中的数据，将其作为填充 Word 模板的数据源。

### 3.2 目标框架支持

EPPlus 7.5.2（继承自 EPPlus 7.x）的 NuGet 包支持：

| 目标框架 | 支持状态 |
|----------|---------|
| .NET 6.0+ | ✅ 直接兼容 |
| .NET 8.0 | ✅ 直接兼容 |
| .NET 8.0-android | ✅ 兼容（计算） |
| .NET 8.0-ios | ✅ 兼容（计算） |
| .NET 8.0-maccatalyst | ✅ 兼容 |
| .NET 8.0-macos | ✅ 兼容 |
| .NET 8.0-windows | ✅ 兼容 |
| .NET Framework 4.6.2+ | ✅ 兼容 |

**关键事实**: EPPlus 7.x 从 v5 开始就完全基于 .NET Standard/.NET Core，不再依赖 Windows 特定 API。它在 Linux 和 macOS Docker 容器中被广泛使用（官方提供了 [EPPlus.DockerSample](https://github.com/EPPlusSoftware/EPPlus.DockerSample) 示例项目），证明其跨平台能力已经过生产验证。

### 3.3 纯托管实现

EPPlus 7.x 是**纯 C# 托管实现**：

- **无 Office 依赖**: 不需要安装 Microsoft Office 或任何 COM 组件
- **无 Windows API**: 不调用 Win32 API，不使用 P/Invoke
- **无原生库**: 纯 .NET 代码，包含自己的 ZIP 解压缩实现
- **Docker 验证**: 官方提供四个不同 Docker 镜像的示例项目，在 Linux 容器中运行良好

### 3.4 许可证影响

| 维度 | EPPlus 4.x（旧版） | EPPlus 5+（当前） |
|------|-------------------|-------------------|
| 许可证 | LGPL | **Polyform Noncommercial 1.0.0** |
| 商业使用 | 免费需开源 | **需要购买商业许可证** |
| 非商业使用 | 免费 | 免费（需设置 LicenseContext） |
| 价格 | N/A | $569/开发者/年（1-4 许可订阅） |

**对 DocuFiller 的影响**: 如果 DocuFiller 是商业产品，使用 EPPlus 7.x 需要购买商业许可证。许可证要求通过 `ExcelPackage.LicenseContext = LicenseContext.Commercial` 设置。这个许可证限制与跨平台迁移无关——在 Windows 和其他平台上使用 EPPlus 的许可证要求是相同的。

### 3.5 非商业替代方案

如果许可证成本是考虑因素，DocuFiller 在跨平台迁移时可以考虑以下替代方案（详见[第 9 节](#9-替代方案)）：

- **MiniExcel**: 开源免费，性能优异，但功能集不如 EPPlus 完整
- **ClosedXML**: 开源免费（MIT），API 与 EPPlus 类似
- **NPOI**: 开源免费（Apache 2.0），功能完整但 API 较老旧

### 3.6 兼容性评估

| 维度 | 评分 | 说明 |
|------|------|------|
| net8.0 支持 | ✅ 完全支持 | .NET 6.0+ 目标，全平台兼容 |
| Windows 特定依赖 | ❌ 无 | 纯托管实现 |
| Docker/Linux 验证 | ✅ 生产验证 | 官方提供 Docker 示例 |
| 许可证风险 | ⚠️ 商业需付费 | Polyform Noncommercial 许可证 |
| 维护状态 | ✅ 活跃 | EPPlus Software AB 商业公司维护 |

> 来源: [NuGet Gallery - EPPlus 7.0.8](https://www.nuget.org/packages/EPPlus/7.0.8)、[EPPlus License Overview](https://www.epplussoftware.com/en/LicenseOverview)、[GitHub - EPPlusSoftware/EPPlus](https://github.com/EPPlusSoftware/EPPlus)

---

## 4. Microsoft.Extensions.* 系列跨平台兼容性

### 4.1 概述

DocuFiller 使用了大量的 `Microsoft.Extensions.*` 包作为基础设施层。这些包是 .NET 生态系统中最基础、最广泛使用的库，其跨平台支持是设计目标之一。

### 4.2 逐包分析

| 包名 | 版本 | 目标框架 | 跨平台 | 说明 |
|------|------|---------|--------|------|
| `Microsoft.Extensions.DependencyInjection` | 8.0.0 | net8.0 | ✅ | 纯托管 DI 容器 |
| `Microsoft.Extensions.Logging` | 8.0.0 | net8.0 | ✅ | 日志抽象层 |
| `Microsoft.Extensions.Logging.Console` | 8.0.0 | net8.0 | ✅ | 控制台日志输出 |
| `Microsoft.Extensions.Logging.Debug` | 8.0.0 | net8.0 | ✅ | 调试输出日志 |
| `Microsoft.Extensions.Configuration` | 8.0.0 | net8.0 | ✅ | 配置抽象层 |
| `Microsoft.Extensions.Configuration.Json` | 8.0.1 | net8.0 | ✅ | JSON 配置文件 |
| `Microsoft.Extensions.Configuration.Binder` | 8.0.0 | net8.0 | ✅ | POCO 配置绑定 |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | 8.0.0 | net8.0 | ✅ | 环境变量配置 |
| `Microsoft.Extensions.Configuration.Xml` | 8.0.0 | net8.0 | ✅ | XML 配置文件 |
| `Microsoft.Extensions.Http` | 8.0.0 | net8.0 | ✅ | IHttpClientFactory |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 8.0.0 | net8.0 | ✅ | Options 模式 |

### 4.3 关键特性

1. **纯托管实现**: 所有 `Microsoft.Extensions.*` 包均为纯 C# 托管代码，无平台特定依赖
2. **设计目标**: 跨平台支持是这些包的核心设计目标。它们被 ASP.NET Core（运行在 Windows、Linux、macOS 和 Docker 中）广泛使用
3. **版本一致性**: DocuFiller 使用的所有 `Microsoft.Extensions.*` 包版本均为 8.0.0/8.0.1，与 .NET 8 SDK 内置版本匹配，确保最佳兼容性
4. **DI 容器**: `Microsoft.Extensions.DependencyInjection` 是 .NET 生态的标准 DI 容器，被 Avalonia、MAUI 等跨平台框架官方推荐使用
5. **HttpClientFactory**: `Microsoft.Extensions.Http` 封装了 `System.Net.Http.HttpClient`，后者在 .NET 8 中是完全跨平台的

### 4.4 兼容性评估

| 维度 | 评分 | 说明 |
|------|------|------|
| net8.0 支持 | ✅ 全部支持 | 所有包均以 net8.0 为目标框架 |
| Windows 特定依赖 | ❌ 全部无 | 纯托管实现 |
| 生产验证 | ✅ 大规模验证 | ASP.NET Core 核心依赖，全球生产环境广泛使用 |
| 维护状态 | ✅ 微软官方维护 | .NET 团队直接维护 |

> 来源: [NuGet Gallery](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)、[Microsoft Learn - .NET Extensions](https://learn.microsoft.com/en-us/dotnet/core/extensions/)

---

## 5. CommunityToolkit.Mvvm 跨平台兼容性

### 5.1 概述

CommunityToolkit.Mvvm（原名 Microsoft.Toolkit.Mvvm）是微软维护的 MVVM 工具包，提供源代码生成器来简化 ViewModel 的编写。DocuFiller 使用它来生成 `ObservableObject`、`RelayCommand`、`ObservableProperty` 等 MVVM 基础设施。

### 5.2 目标框架支持

CommunityToolkit.Mvvm 8.4.0 的 NuGet 包支持：

| 目标框架 | 支持状态 |
|----------|---------|
| .NET Standard 2.0 | ✅ 直接兼容 |
| .NET 8.0 | ✅ 直接兼容 |

**关键事实**: 该包同时支持 .NET Standard 2.0 和 .NET 8.0。这意味着它可以在**所有** .NET 平台上运行，包括 WPF、Avalonia、MAUI、Uno Platform、WinForms 以及任何自定义 UI 框架。

### 5.3 源代码生成器兼容性

CommunityToolkit.Mvvm 的核心功能基于 Roslyn 源代码生成器（Source Generator）。源代码生成器在编译时运行，生成平台无关的 C# 代码：

| 特性 | 平台依赖 | 说明 |
|------|---------|------|
| `[ObservableProperty]` | ❌ 无 | 生成标准 `INotifyPropertyChanged` 实现 |
| `[RelayCommand]` | ❌ 无 | 生成 `ICommand` / `AsyncRelayCommand` 实现 |
| `[ObservableObject]` | ❌ 无 | 生成 `ObservableObject` 基类 |
| `[NotifyPropertyChangedFor]` | ❌ 无 | 编译时属性通知链 |
| `[NotifyCanExecuteChangedFor]` | ❌ 无 | 编译时命令状态联动 |
| 生成代码 | ❌ 无 | 纯 C# 代码，无平台 API 调用 |

**源代码生成器的运行环境**: Roslyn 编译器在所有平台（Windows、macOS、Linux）上均可用，源代码生成器在各平台上的行为完全一致。

### 5.4 与跨平台框架的兼容性

| 框架 | 兼容性 | 说明 |
|------|--------|------|
| **Avalonia** | ✅ 完全兼容 | Avalonia 官方推荐使用 CommunityToolkit.Mvvm |
| **MAUI** | ✅ 完全兼容 | 微软官方示例使用该工具包 |
| **Uno Platform** | ✅ 完全兼容 | Uno 文档推荐使用 |
| **WinUI 3** | ✅ 完全兼容 | Windows 原生 UI 框架 |
| **WPF** | ✅ 完全兼容 | DocuFiller 当前使用 |

### 5.5 兼容性评估

| 维度 | 评分 | 说明 |
|------|------|------|
| net8.0 支持 | ✅ 完全支持 | .NET Standard 2.0 + .NET 8.0 双目标 |
| Windows 特定依赖 | ❌ 无 | 纯 C# 代码生成 |
| 源代码生成器跨平台 | ✅ 完全支持 | Roslyn 编译器全平台可用 |
| 框架兼容性 | ✅ 广泛 | Avalonia/MAUI/Uno/WPF/WinUI 全支持 |
| 维护状态 | ✅ 活跃 | 微软 .NET 团队维护，.NET Foundation 项目 |

> 来源: [NuGet Gallery - CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)、[Avalonia Docs - Cross-platform architecture](https://docs.avaloniaui.net/docs/fundamentals/cross-platform-architecture)、[Community Toolkit GitHub](https://github.com/CommunityToolkit/dotnet)

---

## 6. Services 层服务接口分析

### 6.1 分析方法

对 DocuFiller 的 `Services/`、`DocuFiller/Services/` 和 `Cli/` 目录下的所有服务文件进行了 Windows 特定 API 扫描，检查以下模式：

- `using System.Windows.*`（WPF 命名空间）
- `using Microsoft.Win32.*`（Windows 注册表/对话框）
- `[DllImport]` / `System.Runtime.InteropServices`（平台调用）
- `OperatingSystem.IsWindows()`（运行时平台检测）
- `Registry`（Windows 注册表访问）

### 6.2 扫描结果

| 服务 | Windows 特定依赖 | 跨平台状态 | 说明 |
|------|-----------------|-----------|------|
| `DocumentProcessorService` | ❌ 无 | ✅ 直接复用 | 纯 OpenXml 操作 |
| `ContentControlProcessor` | ❌ 无 | ✅ 直接复用 | OOXML 内容控件处理 |
| `ExcelDataParserService` | ❌ 无 | ✅ 直接复用 | 纯 EPPlus 操作 |
| `FileService` | ❌ 无 | ✅ 直接复用 | 使用 `System.IO` 跨平台 API |
| `FileScannerService` | ❌ 无 | ✅ 直接复用 | 文件系统扫描 |
| `DirectoryManagerService` | ❌ 无 | ✅ 直接复用 | 目录管理 |
| `TemplateCacheService` | ❌ 无 | ✅ 直接复用 | 内存缓存 |
| `SafeTextReplacer` | ❌ 无 | ✅ 直接复用 | 文本替换算法 |
| `SafeFormattedContentReplacer` | ❌ 无 | ✅ 直接复用 | 格式化内容替换 |
| `CommentManager` | ❌ 无 | ✅ 直接复用 | OOXML 评论管理 |
| `ProgressReporterService` | ⚠️ Dispatcher | 🔄 需适配 | 使用 `Dispatcher.Invoke` 回调 UI 线程 |
| `UpdateService` | ⚠️ 平台相关 | 🔄 需适配 | Velopack 集成（T01 已调研） |
| `DocumentCleanupService` | ❌ 无 | ✅ 直接复用 | 文档清理处理 |
| `CleanupCommentProcessor` | ❌ 无 | ✅ 直接复用 | 评论清理 |
| `CleanupControlProcessor` | ❌ 无 | ✅ 直接复用 | 内容控件清理 |
| `OpenXmlTableCellHelper` | ❌ 无 | ✅ 直接复用 | 表格单元格辅助 |
| `Cli/ConsoleHelper` | ⚠️ kernel32.dll | 🔄 需适配 | Windows P/Invoke 控制台附加 |

### 6.3 需要适配的服务

#### 6.3.1 ProgressReporterService（Dispatcher 适配）

`ProgressReporterService` 使用 `System.Windows.Threading.Dispatcher.Invoke` 将进度更新回调到 UI 线程。迁移到 Avalonia 后需要替换为 `Avalonia.Threading.Dispatcher.UIThread.Invoke`。

**适配难度**: 🟢 低。Avalonia 的 Dispatcher API 概念与 WPF 完全一致，只需替换命名空间。

**建议**: 将 Dispatcher 调用抽象为接口，通过 DI 注入，使服务层完全平台无关：

```csharp
public interface IUiThreadInvoker
{
    void Invoke(Action action);
}

// WPF 实现
public class WpfUiThreadInvoker : IUiThreadInvoker { ... }

// Avalonia 实现
public class AvaloniaUiThreadInvoker : IUiThreadInvoker { ... }
```

#### 6.3.2 Cli/ConsoleHelper（kernel32.dll P/Invoke）

`ConsoleHelper` 使用 `kernel32.dll` 的 `AttachConsole`/`AllocConsole`/`FreeConsole` 来实现 WinExe 应用的控制台附加。这些是 Windows 特定 API，在 macOS 和 Linux 上不可用。

**适配难度**: 🟡 中等。需要为非 Windows 平台提供替代实现。

**建议**: 使用条件编译或运行时平台检测：

```csharp
public static class ConsoleHelper
{
    public static void Initialize()
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsConsoleHelper.Initialize();
        }
        // macOS/Linux: 控制台默认可用，无需额外处理
    }
}
```

#### 6.3.3 UpdateService（Velopack 集成）

Velopack 本身支持跨平台（T01 已确认），但 `UpdateService` 中可能有平台特定的逻辑（如安装路径、权限处理）。这部分在 Velopack 集成时统一处理。

### 6.4 ViewModel 层

| ViewModel | WPF 依赖 | 跨平台状态 |
|-----------|----------|-----------|
| `MainWindowViewModel` | ❌ 无 | ✅ 直接复用 |
| `FillViewModel` | ❌ 无 | ✅ 直接复用 |
| `CleanupViewModel` | ❌ 无 | ✅ 直接复用 |
| `DownloadProgressViewModel` | ❌ 无 | ✅ 直接复用 |
| `UpdateSettingsViewModel` | ❌ 无 | ✅ 直接复用 |
| `UpdateStatusViewModel` | ❌ 无 | ✅ 直接复用 |
| `ObservableObject` | ❌ 无 | ✅ 直接复用 |
| `RelayCommand` | ❌ 无 | ✅ 直接复用 |

**结论**: 所有 ViewModel 均零 WPF 依赖，可直接在跨平台项目中复用。

---

## 7. System.Configuration.ConfigurationManager 兼容性

### 7.1 概述

`System.Configuration.ConfigurationManager` 是 .NET Framework 遗留的配置系统，用于读取 `App.config` / `Web.config` 文件中的 `AppSettings` 和 `ConnectionStrings`。DocuFiller 通过 NuGet 引入了该包的 8.0.0 版本。

### 7.2 跨平台支持

| 目标框架 | 支持状态 |
|----------|---------|
| .NET 6.0 | ✅ 兼容 |
| .NET Standard 2.0 | ✅ 兼容 |
| .NET Framework 4.6.2+ | ✅ 兼容 |

该包在 NuGet 上标注了明确的警告：

> *"This package exists only to support migrating existing .NET Framework code that already uses System.Configuration. When writing new code, use another configuration system."*

### 7.3 实际使用情况

在 DocuFiller 代码库中，`System.Configuration.ConfigurationManager` 仅在 `App.xaml.cs` 中被使用了一次：

```csharp
// App.xaml.cs 第 256 行
var value = System.Configuration.ConfigurationManager.AppSettings[key];
```

这是一个简单的 `AppSettings` 键值读取，用于获取少量配置值。DocuFiller 的主要配置系统已经是 `Microsoft.Extensions.Configuration`（Options 模式）。

### 7.4 迁移建议

**强烈建议移除** `System.Configuration.ConfigurationManager` 依赖。将唯一的 `AppSettings` 读取迁移到 `Microsoft.Extensions.Configuration` 的 Options 模式：

```csharp
// 迁移前
var value = System.Configuration.ConfigurationManager.AppSettings["SomeKey"];

// 迁移后
var value = _configuration["SomeKey"];
// 或使用 Options 模式
services.Configure<LegacySettings>(_configuration.GetSection("Legacy"));
```

**好处**:
1. 移除一个遗留依赖，减少包体积
2. 统一配置系统（全部使用 `Microsoft.Extensions.Configuration`）
3. 消除 `App.config` 文件的需要，统一使用 `appsettings.json`

---

## 8. 潜在问题与风险

### 8.1 已识别的风险

| 风险 | 严重性 | 概率 | 影响 | 缓解措施 |
|------|--------|------|------|---------|
| OpenXml 在 Linux 上的文件系统大小写敏感 | 🟡 低 | 中等 | 文件名大小写不匹配可能导致找不到资源文件 | 确保所有文件名在代码中使用正确的大小写 |
| EPPlus 商业许可证成本 | 🟡 低 | 确定 | 跨平台迁移后许可证费用不变 | 评估开源替代方案（MiniExcel/ClosedXML） |
| ConsoleHelper 的 kernel32.dll P/Invoke | 🟢 低 | 确定 | macOS/Linux 上控制台附加功能不可用 | 使用 `OperatingSystem.IsWindows()` 条件编译 |
| ProgressReporter 的 Dispatcher 依赖 | 🟢 低 | 确定 | 需要替换为 Avalonia Dispatcher | 抽象为接口，通过 DI 注入 |
| 反射/Trimming 兼容性 | 🟡 低 | 低 | 启用 trimming 时可能丢失反射使用的类型 | 充分测试，使用 `DynamicallyAccessedMembers` 注解 |
| System.Configuration.ConfigurationManager 遗留 | 🟢 低 | 确定 | 跨平台可用但属于遗留 API | 迁移到 Microsoft.Extensions.Configuration |
| 文件路径分隔符 | 🟢 低 | 中等 | 代码中硬编码 `\` 路径分隔符 | 使用 `Path.Combine()` 和 `Path.DirectorySeparatorChar` |

### 8.2 反射依赖分析

DocuFiller 使用以下涉及反射的库：

| 库 | 反射用途 | 跨平台风险 |
|-----|---------|-----------|
| `Microsoft.Extensions.DependencyInjection` | 构造函数注入 | 🟢 低 — DI 容器在设计上支持跨平台 |
| `CommunityToolkit.Mvvm` 源代码生成器 | 编译时代码生成 | 🟢 无 — 编译时运行，生成标准 C# |
| `Microsoft.Extensions.Options` | 属性绑定 | 🟢 低 — Options 模式在跨平台环境中广泛使用 |
| `DocumentFormat.OpenXml` | XML 类型解析 | 🟢 低 — 使用自己的元数据系统，不依赖运行时反射 |

### 8.3 原生库分析

| 库 | 原生依赖 | 跨平台风险 |
|-----|---------|-----------|
| `DocumentFormat.OpenXml` | ❌ 无 | 无风险 |
| `EPPlus` | ❌ 无 | 无风险 |
| `CommunityToolkit.Mvvm` | ❌ 无 | 无风险 |
| `Microsoft.Extensions.*` | ❌ 无 | 无风险 |
| `System.Configuration.ConfigurationManager` | ❌ 无 | 无风险 |
| `Velopack` | ⚠️ 平台特定二进制 | Velopack 为每个平台提供独立的原生库 |

**结论**: DocuFiller 的核心依赖链中没有任何原生库依赖（除 Velopack 外）。这意味着在切换目标框架时，不会遇到"缺少 .so/.dylib"之类的原生库加载失败问题。

---

## 9. 替代方案

### 9.1 EPPlus 替代方案（如需规避许可证成本）

| 替代库 | 许可证 | net8.0 支持 | 功能完整度 | API 相似度 | 性能 |
|--------|--------|-----------|-----------|-----------|------|
| **MiniExcel** | Apache 2.0 | ✅ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐（流式，低内存） |
| **ClosedXML** | MIT | ✅ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐（与 EPPlus 类似） | ⭐⭐⭐ |
| **NPOI** | Apache 2.0 | ✅ | ⭐⭐⭐⭐⭐ | ⭐⭐（Java POI 移植） | ⭐⭐⭐ |
| **EPPlus 7.x** | Polyform NC + 商业 | ✅ | ⭐⭐⭐⭐⭐ | N/A（当前使用） | ⭐⭐⭐⭐ |

**建议**: 如果 DocuFiller 已经购买了 EPPlus 商业许可证，继续使用 EPPlus 7.x 是最佳选择——迁移成本为零。如果许可证成本是问题，**ClosedXML** 是最接近的替代方案（MIT 许可证、API 与 EPPlus 高度相似），而 **MiniExcel** 则在纯读取场景下性能最优。

### 9.2 DocumentFormat.OpenXml 替代方案

DocumentFormat.OpenXml 是微软官方的 OOXML SDK，功能最完整且完全免费（MIT 许可证）。在 .NET 生态中，没有功能完全对等的替代方案：

| 替代方案 | 说明 | 适用性 |
|----------|------|--------|
| **NPOI** | Apache POI 的 .NET 移植，支持 OOXML | 功能较旧，API 不如 OpenXml SDK 现代 |
| **Open XML SDK**（当前） | 微软官方，MIT 许可证 | ✅ **继续使用**，无需替代 |

**建议**: 继续使用 DocumentFormat.OpenXml，无需替代。

### 9.3 System.Configuration.ConfigurationManager 替代

| 方案 | 说明 | 建议 |
|------|------|------|
| **Microsoft.Extensions.Configuration** | DocuFiller 已在使用 | ✅ **推荐** — 统一配置系统 |

---

## 10. 对 DocuFiller 的建议

### 10.1 迁移策略

基于本次调研，DocuFiller 的跨平台迁移在依赖层面**没有不可逾越的障碍**。所有核心依赖均支持 net8.0。建议按以下优先级处理：

#### 优先级 1：零成本直接复用（迁移当天完成）

以下依赖和代码无需任何修改即可在 net8.0 上运行：

- `DocumentFormat.OpenXml` 3.0.1 — 直接复用
- `EPPlus` 7.5.2 — 直接复用（确保 `LicenseContext` 已设置）
- `CommunityToolkit.Mvvm` 8.4.0 — 直接复用
- 所有 `Microsoft.Extensions.*` 8.0.x — 直接复用
- 所有 ViewModel 类 — 直接复用
- 所有 Service 类（除 ProgressReporterService）— 直接复用
- 所有 Model 类 — 直接复用

#### 优先级 2：简单适配（迁移首周完成）

| 项目 | 工作量 | 说明 |
|------|--------|------|
| ProgressReporterService Dispatcher 替换 | 0.5 天 | 替换为 Avalonia Dispatcher 或抽象为接口 |
| ConsoleHelper 跨平台适配 | 0.5 天 | 添加 `OperatingSystem.IsWindows()` 条件编译 |
| System.Configuration.ConfigurationManager 移除 | 0.5 天 | 将 1 处 AppSettings 迁移到 Options 模式 |

#### 优先级 3：Velopack 集成（已由 T01 调研覆盖）

Velopack 支持跨平台，集成方式与 WPF 版本基本一致。详见 T01 调研报告。

### 10.2 目标框架变更

```xml
<!-- 迁移前 -->
<TargetFramework>net8.0-windows</TargetFramework>
<UseWPF>true</UseWPF>

<!-- 迁移后（Avalonia） -->
<TargetFramework>net8.0</TargetFramework>
```

### 10.3 依赖清理建议

| 操作 | 原因 |
|------|------|
| 移除 `System.Configuration.ConfigurationManager` | 遗留 API，仅 1 处使用，已有替代方案 |
| 移除 `App.config` 文件引用 | 配置已迁移到 `appsettings.json` |
| 保留 `UseWPF` 条件编译（如需渐进迁移） | 可使用多目标框架同时支持 WPF 和 Avalonia |

---

## 11. 优缺点总结

### 优势

1. **零替代成本**: 两个核心业务依赖（DocumentFormat.OpenXml + EPPlus）均为纯托管实现，直接支持 net8.0，无需寻找替代方案
2. **基础设施完全兼容**: Microsoft.Extensions.* 系列和 CommunityToolkit.Mvvm 是 .NET 生态中跨平台支持最好的库
3. **服务层高复用率**: 15/17 个服务文件零 Windows 依赖，可直接复用
4. **ViewModel 零修改**: 所有 ViewModel 可直接在跨平台项目中使用
5. **无原生库风险**: 核心依赖链中无原生库，不存在平台特定的 .so/.dylib 缺失问题
6. **成熟且稳定**: 所有依赖均为微软官方或大型商业公司维护，长期维护有保障

### 劣势

1. **EPPlus 许可证**: Polyform Noncommercial 许可证要求商业使用购买许可证，但这是与平台无关的现有成本
2. **ConsoleHelper 需适配**: CLI 模式的控制台附加功能使用了 Windows P/Invoke，需要为非 Windows 平台提供替代实现
3. **Dispatcher 耦合**: ProgressReporterService 直接依赖 WPF Dispatcher，需要抽象化
4. **遗留配置 API**: 存在一处 System.Configuration.ConfigurationManager 使用，虽可跨平台但不推荐

### 风险评级

| 维度 | 评级 | 说明 |
|------|------|------|
| 整体迁移可行性 | ✅ 高 | 所有核心依赖均支持跨平台 |
| 核心业务逻辑风险 | ✅ 极低 | OpenXml + EPPlus 纯托管，零 Windows 依赖 |
| 适配工作量 | 🟢 低 | 仅 3 个小项需要适配，总工作量约 1-2 天 |
| 许可证风险 | ⚠️ 低 | EPPlus 商业许可证是现有成本，非新增风险 |

### 结论

**DocuFiller 的核心依赖库完全支持跨平台迁移。** 迁移到 Avalonia 等跨平台 UI 框架后，只需将目标框架从 `net8.0-windows` 改为 `net8.0`，核心业务逻辑即可在 Windows、macOS 和 Linux 上运行。需要适配的代码量极少（约 3 处，1-2 天工作量），且均为简单的 API 替换，不涉及架构变更。

---

## 12. 调研日期与信息来源

### 调研时间

2026 年 5 月

### 信息来源

| 来源 | URL |
|------|-----|
| NuGet Gallery - DocumentFormat.OpenXml 3.0.1 | https://www.nuget.org/packages/DocumentFormat.OpenXml/3.0.1 |
| Microsoft Learn - Open XML SDK What's New | https://learn.microsoft.com/en-us/office/open-xml/what-s-new-in-the-open-xml-sdk |
| Microsoft Learn - Migrate v2 to v3 | https://learn.microsoft.com/en-us/office/open-xml/migration/migrate-v2-to-v3 |
| NuGet Gallery - EPPlus 7.0.8 | https://www.nuget.org/packages/EPPlus/7.0.8 |
| EPPlus License Overview | https://www.epplussoftware.com/en/LicenseOverview |
| GitHub - EPPlusSoftware/EPPlus | https://github.com/EPPlusSoftware/EPPlus |
| NuGet Gallery - CommunityToolkit.Mvvm | https://www.nuget.org/packages/CommunityToolkit.Mvvm |
| Avalonia Docs - Cross-platform Architecture | https://docs.avaloniaui.net/docs/fundamentals/cross-platform-architecture |
| GitHub - CommunityToolkit/dotnet | https://github.com/CommunityToolkit/dotnet |
| NuGet Gallery - System.Configuration.ConfigurationManager | https://www.nuget.org/packages/System.Configuration.ConfigurationManager/8.0.0 |
| Microsoft Learn - Cross-platform Targeting | https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting |
| Microsoft Learn - Platform Compatibility Analyzer | https://learn.microsoft.com/en-us/dotnet/standard/analyzers/platform-compat-analyzer |
| NuGet Gallery - Microsoft.Extensions.DependencyInjection | https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/ |

### 参考文档（项目内部）

| 文件 | 说明 |
|------|------|
| `DocuFiller.csproj` | NuGet 依赖完整列表和版本号 |
| `Services/` | 服务层源代码（Windows API 扫描对象） |
| `Cli/ConsoleHelper.cs` | Windows P/Invoke 控制台附加代码 |
| `App.xaml.cs` | System.Configuration.ConfigurationManager 使用点 |
| `docs/cross-platform-research/avalonia-research.md` | Avalonia 调研报告（格式参考和依赖兼容性对照） |
| `docs/cross-platform-research/velopack-research.md` | Velopack 调研报告（T01 产出） |
