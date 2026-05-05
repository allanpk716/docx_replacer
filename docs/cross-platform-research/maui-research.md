# .NET MAUI 跨平台方案技术调研报告

> **调研日期**: 2026-05  
> **调研范围**: .NET MAUI 作为 DocuFiller 跨平台桌面应用的可行性评估  
> **基于**: .NET MAUI 9/.NET 10 官方文档、社区项目、GitHub Issues 及技术文献调研  
> **版本**: 1.0

---

## 目录

1. [技术概述](#1-技术概述)
2. [与 DocuFiller 的适配性分析](#2-与-docufiller-的适配性分析)
3. [跨平台支持状态](#3-跨平台支持状态)
4. [NuGet 生态与依赖](#4-nuget-生态与依赖)
5. [.NET 8/9/10 兼容性](#5-net-8910-兼容性)
6. [打包与分发](#6-打包与分发)
7. [社区活跃度与维护状态](#7-社区活跃度与维护状态)
8. [性能特征](#8-性能特征)
9. [优缺点总结](#9-优缺点总结)
10. [成熟度评估](#10-成熟度评估)
11. [调研日期与信息来源](#11-调研日期与信息来源)

---

## 1. 技术概述

.NET MAUI（Multi-platform App UI）是 Microsoft 于 2022 年正式发布的跨平台 UI 框架，是 Xamarin.Forms 的继任者。MAUI 允许开发者使用单一的 C#/XAML 代码库构建面向 Android、iOS、macOS 和 Windows 的原生应用。

### 1.1 架构

.NET MAUI 的核心架构采用**平台抽象层 + Handler 模式**设计：

- **控制层 (MAUI Controls)**: 提供跨平台统一的 UI 控件 API（Button、Entry、CollectionView 等），开发者直接调用此层
- **Handler 层**: 替代 Xamarin.Forms 的 Renderer 模式，为每个平台提供轻量级的控件映射。Handler 比 Renderer 更轻量、更易定制，同时支持部分平台覆盖
- **平台层**: 各平台的原生 API 实现
  - **Windows**: WinUI 3（通过 Windows App SDK）
  - **macOS**: Mac Catalyst（iOS API 映射到 macOS）
  - **iOS**: 原生 UIKit（通过 .NET for iOS）
  - **Android**: 原生 Android API（通过 .NET for Android）
- **基础层**: .NET Base Class Library (BCL)，跨平台共享

运行时机制：
- **Android**: Mono 运行时 + JIT 编译
- **iOS**: Full AOT（Ahead-of-Time）编译为原生 ARM 二进制
- **macOS**: Mac Catalyst（iOS API 到 macOS 的桥接层），Mono 运行时
- **Windows**: .NET CLR + WinUI 3（原生 Windows 渲染）

### 1.2 单项目多目标

MAUI 引入了**单项目结构**（Single Project），取代了 Xamarin.Forms 的多项目结构：

```
MyMauiApp/
├── Platforms/
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   └── Windows/
├── Resources/
│   ├── Fonts/
│   ├── Images/
│   └── AppIcon/
├── App.xaml
├── App.xaml.cs
├── MainPage.xaml
└── MainPage.xaml.cs
```

共享代码和资源放在项目根目录，平台特定代码放在 `Platforms/` 子目录。通过编译时的多目标（multi-targeting）机制，MSBuild 根据目标平台选择性地编译对应文件夹中的代码。

### 1.3 Handler 模式

Handler 是 MAUI 相对于 Xamarin.Forms 的核心架构改进：

| 特性 | Xamarin.Forms Renderer | MAUI Handler |
|------|----------------------|--------------|
| 耦合度 | 紧耦合，需重写整个控件 | 松耦合，可单独覆盖属性映射 |
| 性能 | 较重，涉及完整视图创建 | 更轻量，按需映射属性 |
| 定制方式 | 继承 + 完整重写 | 属性映射 + 部分覆盖 |
| 跨平台一致性 | 依赖 Renderer 实现 | Handler 提供更一致的基线行为 |

```csharp
// Handler 示例：自定义 Entry 控件
public class CustomEntryHandler : EntryHandler
{
    protected override void ConnectHandler(EditText platformView)
    {
        base.ConnectHandler(platformView);
        // Android 平台特定定制
    }
}
```

### 1.4 XAML 支持

MAUI 使用 XAML 作为 UI 声明语言，但与 WPF 的 XAML 存在显著差异（详见第 2 节）。MAUI 支持：
- 声明式 UI 定义
- 数据绑定（`{Binding}` 表达式）
- Compiled Bindings（编译时绑定检查）
- XAML Hot Reload（实时预览修改）
- ResourceDictionary 和样式系统

### 1.5 Blazor Hybrid 集成

MAUI 支持 `BlazorWebView` 控件，允许在 MAUI 应用中嵌入 Blazor Web UI 组件。这意味着可以：
- 在原生 MAUI 页面中混合使用 Web 组件
- 利用 Blazor 生态的组件库
- 通过 HTML/CSS/JS 实现部分 UI，降低纯 XAML 编写量

> 来源: [Microsoft .NET MAUI 官方文档](https://learn.microsoft.com/en-us/dotnet/maui/), [.NET MAUI 支持的平台](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms)

---

## 2. 与 DocuFiller 的适配性分析

DocuFiller 当前架构为 WPF + MVVM（CommunityToolkit.Mvvm），运行在 net8.0-windows 上。以下逐层分析迁移到 .NET MAUI 的影响。

### 2.1 UI 层：XAML 迁移路径

WPF 和 MAUI 都使用 XAML，但两者**不兼容**，需要重写而非简单迁移。

| 维度 | WPF（当前） | .NET MAUI |
|------|-------------|-----------|
| XAML 方言 | WPF XAML（丰富控件模板、Trigger 系统） | MAUI XAML（精简控件集、Visual States） |
| 布局系统 | StackPanel、Grid、WrapPanel、DockPanel、Canvas | StackLayout、Grid、FlexLayout、AbsoluteLayout |
| 数据绑定 | `{Binding}` + DataContext | `{Binding}` + BindingContext + Compiled Bindings |
| 样式系统 | Style + Trigger + DataTrigger + EventTrigger | Style + VisualState + Setter |
| 控件模板 | ControlTemplate + Trigger | ControlTemplate（简化版，无 Trigger） |
| 资源字典 | ResourceDictionary（合并、主题） | ResourceDictionary（类似但更简化） |
| 窗口管理 | Window + NavigationWindow | Window（MAUI 9+） / Shell 导航 |

**关键差异详述**：

1. **布局控件不同**: WPF 的 `StackPanel` 对应 MAUI 的 `StackLayout`（有 `Horizontal`/`Vertical` 方向），`WrapPanel` 对应 `FlexLayout` 的 `Wrap` 属性
2. **无 Trigger 系统**: MAUI 不支持 WPF 的 `DataTrigger`、`EventTrigger`、`MultiTrigger`。需要改用 `VisualState` 或在 ViewModel 中用代码实现条件样式逻辑
3. **控件集精简**: MAUI 的控件集比 WPF 小得多。许多 WPF 内置控件（如 `TreeView`、`DataGrid`、`Expander`）在 MAUI 中不存在或需要第三方库
4. **窗口管理差异**: WPF 的多窗口模型（`new Window().Show()`）在 MAUI 中直到 .NET 9 才获得完整支持。之前版本主要通过 Shell 导航管理页面
5. **Attached Property 机制相同**: 两者都支持 Attached Property，但命名空间不同

**评估**: UI 层需要完全重写。虽然 XAML 语法相似，但控件名称、布局模型、样式机制差异巨大。无法进行自动化的 XAML 转换。工作量估计：DocuFiller 的 MainWindow 和 CleanupWindow 的所有 XAML 都需要重写。

### 2.2 MVVM 支持与迁移

| 维度 | 评估 |
|------|------|
| MVVM 模式 | ✅ MAUI 原生支持 MVVM，与 WPF 模式一致 |
| CommunityToolkit.Mvvm | ✅ 完全兼容，`ObservableObject`、`[RelayCommand]`、`[ObservableProperty]` 均可使用 |
| INotifyPropertyChanged | ✅ 标准接口，无差异 |
| ICommand | ✅ 标准接口，无差异 |
| DI 容器 | ✅ `Microsoft.Extensions.DependencyInjection` 完全兼容 |
| Messaging | ✅ `WeakReferenceMessenger` 可用 |

**结论**: ViewModel 层代码可高度复用，这是 MAUI 方案的主要优势之一。

### 2.3 服务层复用性

DocuFiller 的服务层设计良好，多数服务零 WPF 依赖：

| 服务 | WPF 依赖 | 迁移难度 |
|------|----------|----------|
| `IDocumentProcessor` / `DocumentProcessor` | ❌ 无 | ✅ 直接复用 |
| `IExcelDataParser` / `ExcelDataParser` | ❌ 无 | ✅ 直接复用 |
| `IFileService` / `FileService` | ❌ 无 | ✅ 直接复用 |
| `IFileScanner` / `FileScanner` | ❌ 无 | ✅ 直接复用 |
| `ITemplateCacheService` | ❌ 无 | ✅ 直接复用 |
| `ISafeTextReplacer` / `ISafeFormattedContentReplacer` | ❌ 无 | ✅ 直接复用 |
| `IDocumentCleanupService` | ❌ 无 | ✅ 直接复用 |
| `IProgressReporter` | ⚠️ 依赖 Dispatcher | 🔄 需适配（MAUI 有 `MainThread.BeginInvokeOnMainThread`） |
| `IUpdateService` (Velopack) | ⚠️ 平台相关 | 🔄 需重写（详见第 6 节） |

**关键发现**: DocuFiller 的 DI 架构和日志框架与 MAUI 完全一致（均为 `Microsoft.Extensions.*`），服务注册可直接迁移。核心文档处理逻辑（OpenXml + EPPlus）跨平台兼容，零修改即可复用。

### 2.4 文件对话框

| 功能 | WPF (Microsoft.Win32) | .NET MAUI |
|------|----------------------|-----------|
| 打开文件对话框 | `OpenFileDialog` | `FilePicker.PickAsync()` |
| 保存文件对话框 | `SaveFileDialog` | `FilePicker.PickAsync()`（仅选取，保存需自定义） |
| 文件过滤器 | `Filter = "Word\|*.docx"` | `FilePickerFileType` 自定义 |
| 多选 | `Multiselect = true` | `FilePicker.PickMultipleAsync()` |
| 跨平台 | ❌ Windows Only | ✅ 四平台 |

**注意**: MAUI 的 `FilePicker` API 比 WPF 的 `OpenFileDialog` 功能更受限，特别是保存对话框的支持不够完善。社区通常需要通过平台特定代码实现完整的保存功能。

### 2.5 CLI 层

DocuFiller 通过自定义 `Program.cs` 的 `[STAThread] Main` 实现 GUI/CLI 双模式。MAUI 架构下，CLI 模式可以保留——将服务层提取为独立的类库项目，CLI 入口直接引用该类库而不启动 MAUI UI。这与当前的架构设计一致。

---

## 3. 跨平台支持状态

### 3.1 官方支持平台

| 平台 | 支持状态 | 底层技术 | 说明 |
|------|---------|---------|------|
| **Windows** | ✅ 完全支持（一等公民） | WinUI 3 / Windows App SDK | 原生 Windows 渲染，体验最佳 |
| **Android** | ✅ 完全支持 | .NET for Android + 原生 API | 主要移动目标平台 |
| **iOS** | ✅ 完全支持 | .NET for iOS + UIKit | 主要移动目标平台 |
| **macOS** | ⚠️ 基本支持 | Mac Catalyst（iOS→macOS 桥接） | 非原生 macOS API，存在 UI 偏差 |
| **Tizen** | ⚠️ 社区支持 | Tizen .NET | 三星设备专用 |
| **Linux 桌面** | ❌ **官方不支持** | 无 | 核心阻碍，详见下文 |

> 来源: [Microsoft 官方支持平台列表](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms)

### 3.2 macOS 桌面支持分析

MAUI 的 macOS 支持通过 **Mac Catalyst** 实现——将 iOS API 桥接到 macOS 桌面。这意味着：

**优势**:
- 可运行在 Apple Silicon (M1/M2/M3/M4) 和 Intel Mac 上
- 共享 iOS 平台的代码和控件实现
- Visual Studio 2022 和 CLI 工具链支持 macOS 构建

**限制**:
- UI 外观偏向 iOS 风格，不是原生 macOS AppKit 外观
- 部分 macOS 原生特性（如菜单栏定制、Dock 集成）支持不完整
- Mac Catalyst 本身有已知的性能和渲染问题
- macOS 桌面应用需要通过 `dotnet publish` 生成 `.app` bundle，需在 macOS 上构建或使用 CI/CD

**稳定性评估**: macOS 支持在 .NET 8/9 中基本稳定，适合内部工具和企业应用。但对于追求原生 macOS 体验的消费级应用，Mac Catalyst 方案存在 UI 一致性问题。

### 3.3 Linux 桌面支持——关键阻碍

**这是 .NET MAUI 作为 DocuFiller 跨平台方案的最大阻碍。**

Microsoft 官方立场：**Linux 桌面支持不在路线图上（"not planned"）**。

MAUI 维护者 David Ortinau 在 2021 年明确表示 Linux "is on our radar but we're open to collaboration"，翻译为：Microsoft 不会投入资源，但欢迎社区自行实现。截至 2026 年 5 月，官方立场未变。

#### 3.3.1 社区 Linux 方案 A：open-maui/maui-linux

| 维度 | 详情 |
|------|------|
| **项目地址** | https://github.com/open-maui/maui-linux |
| **原始项目** | https://github.com/jsuarezruiz/maui-linux（由 MAUI 团队成员 Javier Suárez 开发） |
| **许可证** | MIT |
| **渲染方式** | SkiaSharp 硬件加速渲染 |
| **显示服务器** | X11 + Wayland 支持 |
| **控件数量** | 47+ 个 MAUI 控件（Button、Label、Entry、CollectionView、CarouselView、RefreshView 等） |
| **平台服务** | 剪贴板、文件选择器、通知、拖放、全局快捷键、系统托盘 |
| **无障碍** | AT-SPI2 屏幕阅读器支持 |
| **文本输入** | IBus + XIM 国际化输入支持 |
| **提交数** | 157+ commits |
| **成熟度** | 声称生产就绪 |

**使用方式**:
```bash
dotnet new install Open.Maui.Templates
dotnet new mauilinux -n MyLinuxApp
cd MyLinuxApp
dotnet run
```

**系统依赖**: Ubuntu/Debian 需要 libx11-dev、libxrandr-dev、libxcursor-dev、libxi-dev、libgl1-mesa-dev、libfontconfig1-dev。

**风险评估**:
- 由 MAUI 团队成员开发，技术可靠性较高
- 但仍为社区项目，无 Microsoft 官方支持承诺
- 需要 .NET 10 SDK，版本绑定较新
- 长期维护依赖于社区贡献者的持续投入

#### 3.3.2 社区 Linux 方案 B：Avalonia MAUI Backend

| 维度 | 详情 |
|------|------|
| **项目** | Avalonia 团队为 MAUI 提供的 Linux/WebAssembly 后端 |
| **发布公告** | 2025 年 11 月 |
| **渲染方式** | Avalonia 自绘引擎（composition-based pixel drawing） |
| **目标平台** | Linux、WebAssembly、macOS、Windows |
| **状态** | Preview（基于 .NET 11 Preview） |
| **技术方式** | 通过 MAUI Handler 架构将 MAUI 控件映射到 Avalonia 渲染器 |

**特点**:
- 不是 fork MAUI，而是提供替代渲染层
- 应用在所有平台上渲染一致（Avalonia 自绘风格，非平台原生外观）
- 基于 .NET 11 Preview，预计 2026 年 11 月 GA
- 尚不支持 WinUI 内嵌 Avalonia 控件
- 目前不支持原生 Wayland（依赖 X11/XWayland）

**风险评估**:
- 技术路径优雅（通过 Handler 架构扩展）
- 但处于早期 Preview 阶段，API 可能大幅变动
- 依赖 .NET 11 的发布节奏
- 与 MAUI 官方控件可能存在行为差异

#### 3.3.3 Linux 支持结论

对于 DocuFiller，Linux 桌面支持是跨平台迁移的关键需求之一。MAUI 的现状是：
- 官方不支持 Linux，短期内不会改变
- 社区有两个可行方案（open-maui 和 Avalonia backend），但都存在风险
- 如果 DocuFiller 必须支持 Linux 桌面，MAUI 方案需要额外依赖社区项目，增加维护复杂度

### 3.4 DocuFiller 核心功能的跨平台可行性

| 功能 | Windows | macOS | Linux |
|------|---------|-------|-------|
| Word 文档处理 (OpenXml) | ✅ | ✅ | ✅ |
| Excel 数据读取 (EPPlus) | ✅ | ✅ | ✅ |
| 原生文件对话框 | ✅ | ✅ | ⚠️ 社区方案 |
| 原生通知 | ✅ | ⚠️ 有限 | ⚠️ 社区方案 |
| 系统托盘 | ✅ | ⚠️ 有限 | ⚠️ 社区方案 |
| 自动更新 (Velopack) | ✅ | ⚠️ 需适配 | ❌ 需替换 |
| 全局快捷键 | ✅ | ✅ | ⚠️ 社区方案 |
| 多窗口 | ✅ (.NET 9+) | ⚠️ 有限 | ⚠️ 未验证 |

---

## 4. NuGet 生态与依赖

### 4.1 DocuFiller 核心依赖跨平台兼容性

| NuGet 包 | 当前版本 | MAUI 兼容性 | 说明 |
|----------|---------|------------|------|
| `DocumentFormat.OpenXml` | 3.0.1 | ✅ 完全兼容 | 纯 .NET 实现，无平台依赖 |
| `EPPlus` | 7.5.2 | ✅ 完全兼容 | v5+ 支持 .NET Core，跨平台可用 |
| `CommunityToolkit.Mvvm` | 8.4.0 | ✅ 完全兼容 | 平台无关的 MVVM 工具包 |
| `Microsoft.Extensions.*` | 8.0.x | ✅ 完全兼容 | 通用 DI/日志/配置框架 |
| `Velopack` | 0.0.1298 | ⚠️ 需评估 | Windows 主要支持，macOS 有限，Linux 不支持 |

> 来源: 各 NuGet 包官方文档及兼容性矩阵

### 4.2 MAUI 特有依赖

迁移到 MAUI 引入的关键依赖：
- `Microsoft.Maui.Controls` — MAUI 核心控件库
- `Microsoft.Maui.Essentials` — 跨平台 API（文件选择、偏好设置、网络状态等）
- `Microsoft.WindowsAppSDK` — Windows 平台的 WinUI 3 运行时
- 各平台 SDK（.NET for Android、.NET for iOS 等）

### 4.3 第三方控件生态

MAUI 的第三方控件生态相比 WPF 仍然较小：

| 供应商 | MAUI 支持 | 说明 |
|--------|----------|------|
| Telerik UI for MAUI | ✅ 完整 | 商业控件库，DataGrid、Chart 等 |
| Syncfusion MAUI | ✅ 完整 | 商业控件库，社区许可可用 |
| DevExpress MAUI | ✅ 完整 | 商业控件库 |
| .NET MAUI Community Toolkit | ✅ 开源 | 社区维护的辅助工具和控件 |
| Gorilla Player | ⚠️ 已停止 | XAML 预览工具 |

**对比 WPF**: WPF 拥有近 20 年的控件生态积累，MAUI 的控件库（包括第三方）在深度和广度上仍有差距。特别是 `DataGrid`、`TreeView`、`TabControl` 等企业级常用控件在 MAUI 标准库中缺失，需要依赖商业库。

---

## 5. .NET 8/9/10 兼容性

### 5.1 版本绑定策略

MAUI 与 .NET 版本强绑定，每个 MAUI 版本对应一个 .NET 版本：

| MAUI 版本 | .NET 版本 | 发布日期 | 支持截止 | 状态 |
|-----------|----------|---------|---------|------|
| MAUI 8 | .NET 8 LTS | 2023-11 | 2025-05-14 | ⚠️ 即将过期 |
| MAUI 9 | .NET 9 STS | 2024-11 | 2026-05-12 | 当前推荐 |
| MAUI 10 | .NET 10 LTS | 2025-11 | 2028-11 | 最新版本 |

> 来源: [.NET MAUI 支持策略](https://dotnet.microsoft.com/en-us/platform/support/policy/maui)

### 5.2 版本迁移挑战

开发者社区报告从 .NET 9 迁移到 .NET 10 时遇到了问题：
- 部分 Android 和 iOS 功能在 .NET 10 中行为不符合预期
- Q1 2026 报告了持续的回归和 bug，生产可用性受影响
- 有开发者被迫回退到 .NET 9

**评估**: MAUI 的快速版本节奏（每年一个主要版本）意味着 DocuFiller 需要频繁跟进 .NET 版本升级。与 WPF 的稳定性（.NET 10 LTS 支持到 2028 年）相比，MAUI 的维护负担更高。

### 5.3 框架目标

DocuFiller 当前目标是 `net8.0-windows`。迁移到 MAUI 后，项目目标框架将变为 `net9.0`（或 `net10.0`），并通过 MAUI 的多目标机制自动包含各平台。这意味着失去 Windows 特有的 API 访问能力（如 WPF 专属 API），需要通过条件编译 `#if WINDOWS` 访问 WinUI 3 特定功能。

---

## 6. 打包与分发

### 6.1 Windows：MSIX 打包

MAUI Windows 应用通过 WinUI 3 / Windows App SDK 打包：

| 格式 | 说明 |
|------|------|
| MSIX | 标准 Windows 应用包格式，支持 Microsoft Store 分发 |
| MSIX Bundle | 多架构包（x86 + x64 + ARM64），Store 要求 |
| 便携版（未打包） | 通过 `WindowsPackageType=None` 可生成独立 exe |

**MSIX 打包命令**:
```bash
dotnet publish -c Release -f:net9.0-windows10.0.22621.0
```

**与 Velopack 的兼容性**:
DocuFiller 当前使用 Velopack 实现自动更新。在 MAUI 方案中：
- MSIX 打包模式下，Velopack 与 Windows App SDK 的自动更新机制冲突
- 便携版模式下，Velopack 理论上可以工作，但需要额外适配
- **建议**: 如果选择 MAUI 方案，Windows 平台可考虑使用 MSIX 的自动更新，或切换到 Velopack 的便携版模式

### 6.2 macOS：App Bundle

MAUI macOS 应用通过 Mac Catalyst 打包为 `.app` bundle：

```bash
dotnet publish -c Release -f:net9.0-maccatalyst
```

- 输出为 macOS `.app` 目录结构
- 需要 macOS 构建环境（或在 macOS CI/CD agent 上构建）
- 支持 Apple 代码签名和公证（notarization）
- 可通过 `dotnet bundle` 或第三方工具生成 DMG/PKG 安装包

### 6.3 Linux：社区方案打包

Linux 打包依赖社区方案：
- **open-maui/maui-linux**: 支持 AppImage、deb、rpm 等格式（通过各工具链）
- **Avalonia backend**: 取决于 Avalonia 的打包支持

### 6.4 Velopack 集成可能性

| 平台 | Velopack 支持 | MAUI 兼容性 |
|------|-------------|------------|
| Windows | ✅ 主要支持平台 | ⚠️ 与 MSIX 冲突，便携版可能可行 |
| macOS | ⚠️ 有限支持 | ⚠️ 需验证 |
| Linux | ❌ 不支持 | ❌ 不可用 |

**结论**: 如果 DocuFiller 选择 MAUI 跨平台，需要为不同平台采用不同的更新策略——Windows 用 MSIX 更新或 Velopack 便携版，macOS 需要评估 Velopack 或替代方案，Linux 完全需要替代方案。

---

## 7. 社区活跃度与维护状态

### 7.1 GitHub 数据（截至 2026-05）

| 指标 | 数值 |
|------|------|
| GitHub Stars | ~22,000+ |
| 开放 Issues | ~1,200+ |
| 开放 Pull Requests | ~100+ |
| 主要维护者 | Microsoft .NET MAUI 团队（David Ortinau 等） |
| License | MIT |
| 仓库 | https://github.com/dotnet/maui |

> 来源: [GitHub - dotnet/maui](https://github.com/dotnet/maui)

### 7.2 维护状态评估

**积极方面**:
- Microsoft 官方团队全职维护，每年随 .NET 版本发布更新
- 开源社区活跃，Community Toolkit 持续更新
- 与 Visual Studio 2022 深度集成（Hot Reload、调试器、XAML 预览）
- 有明确的支持策略和生命周期（但偏短）

**担忧方面**:
- **Issue 积压严重**: 超过 1,200 个开放 Issue，响应时间不稳定
- **回归问题频发**: 社区报告 .NET 9→10 迁移出现持续回归
- **Microsoft 内部使用有限**: Microsoft 自身的跨平台产品（如 Teams）使用 Electron/React Native，而非 MAUI，传递负面信号
- **控件成熟度不足**: 核心控件（如 CollectionView、CarouselView）在 Android 上性能问题反复出现
- **Linux 支持缺失**: 官方明确不计划支持，社区诉求（GitHub Discussion #339，500+ upvote，700+ 👍）被搁置

### 7.3 开发工具链

| 工具 | MAUI 支持情况 |
|------|-------------|
| Visual Studio 2022 | ✅ 完整支持（Windows 上最佳体验） |
| Visual Studio Code | ⚠️ 通过 .NET MAUI 扩展，功能有限 |
| JetBrains Rider | ⚠️ 支持但社区反馈有工具链问题 |
| Visual Studio for Mac | ❌ 已停止维护（2024-08） |
| CLI (dotnet) | ✅ 完整支持构建、发布 |

**注意**: 最佳 MAUI 开发体验依赖 Visual Studio 2022 on Windows。macOS/Linux 上的开发体验明显受限。

---

## 8. 性能特征

### 8.1 启动时间

| 平台 | 冷启动 | 热启动 |
|------|--------|--------|
| WPF (当前 DocuFiller) | ~1-2 秒 | ~0.5 秒 |
| MAUI Windows (WinUI 3) | ~2-4 秒 | ~1-2 秒 |
| MAUI macOS (Mac Catalyst) | ~3-5 秒 | ~1-3 秒 |
| MAUI Android | ~3-6 秒 | ~1-3 秒 |

MAUI 的启动开销来源：
- .NET 运行时初始化
- 平台层（WinUI 3 / Mac Catalyst）初始化
- Handler 控件创建和属性映射
- XAML 解析（除非使用 Compiled XAML）

> 来源: 社区 benchmark 报告, Microsoft .NET MAUI 性能指南

### 8.2 内存占用

| 方案 | 空闲内存 | 工作内存 |
|------|---------|---------|
| WPF (当前 DocuFiller) | ~50-80 MB | ~100-200 MB |
| MAUI Windows (WinUI 3) | ~80-150 MB | ~150-300 MB |
| MAUI macOS (Mac Catalyst) | ~100-200 MB | ~200-350 MB |

MAUI 的内存开销高于 WPF，主要因为：
- WinUI 3 运行时额外开销
- Mac Catalyst 的 iOS→macOS 桥接层
- Handler 映射层

### 8.3 渲染性能

| 维度 | WPF | MAUI Windows (WinUI 3) |
|------|-----|----------------------|
| 渲染引擎 | DirectX（硬件加速） | WinUI 3（DirectX，硬件加速） |
| 大数据列表 | VirtualizingStackPanel 优秀 | CollectionView 有性能问题（Android 尤明显） |
| 复杂布局 | 高度优化 | WinUI 3 优化，Mac Catalyst 偏慢 |
| 动画 | Storyboard + DoubleAnimation | 简化动画 API |

**注意**: WinUI 3 在 Windows 上的渲染性能理论上与 WPF 相当（都使用 DirectX）。但 MAUI 的 Handler 层增加了一层抽象开销。社区报告 CollectionView 在大数据量场景下的性能不如 WPF 的 DataGrid + VirtualizingStackPanel。

---

## 9. 优缺点总结

### SWOT 分析

#### 优势 (Strengths)

1. **C#/XAML 技术栈延续**: 团队现有的 C# 和 XAML 技能可直接复用，学习曲线相对平缓
2. **MVVM 架构完美匹配**: CommunityToolkit.Mvvm、DI 容器、日志框架全部兼容
3. **服务层高复用**: DocuFiller 的核心服务层（文档处理、Excel 解析等）零修改即可复用
4. **Microsoft 官方支持**: 有明确的支持策略和生命周期，Visual Studio 深度集成
5. **Windows 原生体验**: WinUI 3 提供原生 Windows 11 外观和 API 访问
6. **单项目结构**: 简化了多平台代码管理

#### 劣势 (Weaknesses)

1. **Linux 不支持（关键阻碍）**: 官方不计划支持 Linux 桌面，社区方案风险高
2. **UI 完全重写**: WPF XAML 与 MAUI XAML 不兼容，所有窗口需要重写
3. **控件生态较小**: 第三方控件库远少于 WPF，部分核心控件（DataGrid、TreeView）缺失
4. **macOS 体验非原生**: Mac Catalyst 方案产生 iOS 风格 UI，不是原生 macOS 体验
5. **频繁版本升级**: 每年一个主要版本，支持窗口短（STS 仅 18 个月），维护负担高
6. **回归问题**: 社区频繁报告版本升级后的回归 bug
7. **工具链依赖 Windows**: 最佳开发体验需要 Visual Studio 2022 on Windows

#### 机会 (Opportunities)

1. **移动端扩展**: 如果未来需要 iOS/Android 版本，MAUI 原生支持
2. **Blazor Hybrid**: 可在 MAUI 中嵌入 Web 组件，渐进式迁移 UI
3. **社区 Linux 方案成熟**: open-maui/maui-linux 和 Avalonia backend 可能在未来稳定
4. **Microsoft 持续投入**: 作为 Xamarin.Forms 的官方继任者，MAUI 有长期支持承诺

#### 威胁 (Threats)

1. **Linux 社区方案不可靠**: 依赖社区项目承担核心平台支持，存在维护中断风险
2. **Microsoft 内部不用 MAUI**: Teams、Office 等产品用 Electron/React Native，传递负面信号
3. **竞争框架更成熟**: Avalonia 原生支持 Linux，Flutter 支持全平台，Electron 方案更成熟
4. **版本绑定风险**: MAUI 与 .NET 版本强绑定，.NET 版本问题直接影响 MAUI
5. **框架采用率不高**: 相比 Flutter/React Native，MAUI 的市场采用率偏低

---

## 10. 成熟度评估

### 技术成熟度等级 (TRL)

| 维度 | 等级 | 说明 |
|------|------|------|
| Windows 桌面 (WinUI 3) | TRL 8 (系统完成) | 官方一等公民，工具链完善 |
| macOS 桌面 (Mac Catalyst) | TRL 6 (技术验证) | 基本可用，但非原生体验 |
| Linux 桌面 (社区方案) | TRL 4-5 (实验室/原型) | open-maui 可用但无长期保障 |
| DocuFiller 服务迁移 | TRL 7 (系统原型) | 核心服务可直接复用，需验证边界情况 |
| 打包与分发 | TRL 6 (技术验证) | Windows MSIX 成熟，macOS 需验证，Linux 需社区方案 |
| 自动更新方案 | TRL 4 (实验室验证) | 需多平台分别处理，方案复杂 |
| **整体方案** | **TRL 5** | **Windows 可行，macOS 基本可行，Linux 是关键短板** |

### 适用场景判断

| 场景 | 推荐度 | 说明 |
|------|--------|------|
| 仅 Windows + 移动端 | ⭐⭐⭐⭐ | MAUI 的原生定位，Windows + iOS + Android 全覆盖 |
| Windows + macOS 桌面 | ⭐⭐⭐ | macOS 通过 Mac Catalyst 可用，但体验非原生 |
| Windows + macOS + Linux 桌面 | ⭐⭐ | Linux 依赖社区方案，维护风险高 |
| 仅 Windows 桌面 | ⭐ | 如果不需要跨平台，WPF 方案更成熟、更稳定 |

### 评分（1-5 分）

| 维度 | 评分 | 说明 |
|------|------|------|
| 技术架构设计 | 4 | Handler 模式优雅，单项目结构清晰 |
| 与 DocuFiller 适配性 | 3 | 服务层高复用，但 UI 需完全重写 |
| 跨平台覆盖 | 2 | 缺失 Linux 桌面是致命短板 |
| 控件与 UI 能力 | 3 | 核心控件可用，但企业级控件不足 |
| 打包分发 | 3 | Windows MSIX 成熟，其他平台需额外工作 |
| 社区与生态 | 3 | Microsoft 官方维护但社区反馈偏负面 |
| 性能 | 3 | Windows 可接受，移动端有优化空间 |
| 长期维护成本 | 2 | 频繁版本升级，回归风险，Linux 社区维护 |
| **综合评分** | **2.8 / 5** | **技术架构合理，但 Linux 缺失和生态不成熟是显著阻碍** |

---

## 11. 调研日期与信息来源

### 调研时间

2026 年 5 月

### 信息来源

| 来源 | URL |
|------|-----|
| .NET MAUI GitHub 仓库 | https://github.com/dotnet/maui |
| .NET MAUI 官方文档 | https://learn.microsoft.com/en-us/dotnet/maui/ |
| .NET MAUI 支持的平台 | https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms |
| .NET MAUI 支持策略 | https://dotnet.microsoft.com/en-us/platform/support/policy/maui |
| Linux 支持讨论 (#339) | https://github.com/dotnet/maui/discussions/339 |
| Linux 支持社区贡献讨论 (#31649) | https://github.com/dotnet/maui/discussions/31649 |
| open-maui/maui-linux 项目 | https://github.com/open-maui/maui-linux |
| jsuarezruiz/maui-linux（原始项目） | https://github.com/jsuarezruiz/maui-linux |
| Avalonia MAUI Backend 公告 | https://avaloniaui.net/blog/net-maui-is-coming-to-linux-and-the-browser-powered-by-avalonia |
| MAUI Linux 支持分析 (byteiota) | https://byteiota.com/net-maui-linux-support-two-paths-forward-in-2026/ |
| Avalonia 增强 MAUI (devclass) | https://www.devclass.com/development/2026/03/24/avaloniaui-enhances-net-maui-with-linux-and-webassembly-support/5209515 |
| MAUI 2026 现状讨论 (#27185/#34171) | https://github.com/dotnet/maui/discussions/27185 |
| WPF 到 MAUI 迁移考虑 (Telerik) | https://www.telerik.com/blogs/considerations-when-porting-wpf-app-net-maui |
| WPF 或 MAUI 如何选择 (Telerik) | https://www.telerik.com/blogs/wpf-net-maui-how-choose |
| WPF 现代化迁移分析 (Uno Platform) | https://platform.uno/articles/wpf-modernization-vs-migration-which-one-are-you-actually-doing/ |
| WPF vs WinForms vs MAUI 比较 (SciChart) | https://www.scichart.com/blog/wpf-vs-winforms-vs-maui/ |
| DocumentFormat.OpenXml NuGet | https://www.nuget.org/packages/DocumentFormat.OpenXml |
| EPPlus NuGet | https://www.nuget.org/packages/EPPlus |
| Velopack 文档 | https://docs.velopack.io/ |
| .NET MAUI MVVM 模式 (Microsoft Learn) | https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm |

### 相关调研文档

| 文档 | 说明 |
|------|------|
| `docs/cross-platform-research/avalonia-research.md` | Avalonia UI 方案调研 |
| `docs/cross-platform-research/blazor-hybrid-research.md` | Blazor Hybrid 方案调研 |
| `docs/cross-platform-research/web-app-research.md` | 纯 Web 应用方案调研 |
| `docs/cross-platform-research/electron-net-research.md` | Electron.NET 方案调研 |
