# Electron.NET 跨平台方案技术调研报告

> **调研日期**: 2026-05  
> **调研范围**: Electron.NET 作为 DocuFiller 跨平台桌面应用的可行性评估  
> **基于**: ElectronNET.API 23.6.2 + ASP.NET Core 8 实际 PoC 开发  
> **版本**: 1.0

---

## 目录

1. [技术概述](#1-技术概述)
2. [与 DocuFiller 的适配性分析](#2-与-docufiller-的适配性分析)
3. [IPC 通信机制](#3-ipc-通信机制)
4. [NuGet 生态与依赖](#4-nuget-生态与依赖)
5. [.NET 8 兼容性](#5-net-8-兼容性)
6. [跨平台支持](#6-跨平台支持)
7. [打包与分发](#7-打包与分发)
8. [社区活跃度与维护状态](#8-社区活跃度与维护状态)
9. [性能特征](#9-性能特征)
10. [优缺点总结](#10-优缺点总结)
11. [成熟度评估](#11-成熟度评估)
12. [PoC 发现总结](#12-poc-发现总结)
13. [调研日期与信息来源](#13-调研日期与信息来源)

---

## 1. 技术概述

Electron.NET 是一个将 [Electron](https://www.electronjs.org/)（Chromium + Node.js）与 ASP.NET Core 结合的开源框架，允许 C# 开发者使用 Web 技术（HTML/CSS/JavaScript）构建跨平台桌面应用程序。

### 1.1 架构

Electron.NET 的核心架构由两层组成：

- **Electron Shell 层**：基于 Electron，提供 Chromium 渲染引擎和原生操作系统 API 访问（文件对话框、系统托盘、通知等）
- **ASP.NET Core Host 层**：.NET 后端进程作为中间件运行在 Electron 的 Node.js 主进程中，通过 IPC（进程间通信）桥接前后端

工作原理：
1. Electron 启动时创建主进程（Node.js）
2. 主进程启动 ASP.NET Core Kestrel 服务器（默认端口随机分配）
3. BrowserWindow 加载 `localhost:PORT` 作为前端页面
4. 前端通过 HTTP API 或 Electron IPC 与 .NET 后端通信

这种架构意味着 ASP.NET Core 应用本质上是一个标准的 Web 项目，只是被 Electron 窗口包裹而非浏览器。**HybridSupport.IsElectronActive** 属性可用于检测运行环境。

### 1.2 版本信息

- **当前最新版本**: ElectronNET.API 23.6.2（对应 Electron 23.x）
- **NuGet 包**: `ElectronNET.API`（运行时 API）、`ElectronNET.CLI`（构建工具 `electronize`）
- **目标框架**: net6.0（向前兼容 net7.0/net8.0）

> 来源: [NuGet Gallery - ElectronNET.API](https://www.nuget.org/packages/ElectronNET.API/), [GitHub Releases](https://github.com/ElectronNET/Electron.NET/releases)

---

## 2. 与 DocuFiller 的适配性分析

DocuFiller 当前架构为 WPF + MVVM（CommunityToolkit.Mvvm），运行在 net8.0-windows 上。以下逐层分析迁移到 Electron.NET 的影响。

### 2.1 UI 层：HTML/CSS/JS 替代 WPF

| 维度 | WPF（当前） | Electron.NET（迁移后） |
|------|-------------|----------------------|
| 标记语言 | XAML | HTML/CSS |
| 数据绑定 | `{Binding}` + ObservableObject | JavaScript 框架或原生 DOM |
| 样式系统 | ResourceDictionary + Styles | CSS（Tailwind/SASS 等） |
| UI 组件库 | WPF 内置控件 | Web 组件库（React/Vue/原生） |

**评估**: UI 层需要完全重写。WPF 的 XAML 声明式绑定在 Web 端没有直接对应物。优势是 Web UI 的设计自由度更高、CSS 生态更丰富。缺点是 DocuFiller 的所有窗口（MainWindow、CleanupWindow）均需用 HTML 重建，工作量较大。

### 2.2 后端层：Services 复用性

DocuFiller 的服务层（`Services/` 命名空间）设计良好，多数服务零 WPF 依赖：

| 服务 | WPF 依赖 | 迁移难度 |
|------|----------|----------|
| `IDocumentProcessor` / `DocumentProcessor` | ❌ 无 | ✅ 直接复用 |
| `IExcelDataParser` / `ExcelDataParser` | ❌ 无 | ✅ 直接复用 |
| `IFileService` / `FileService` | ❌ 无 | ✅ 直接复用 |
| `IFileScanner` / `FileScanner` | ❌ 无 | ✅ 直接复用 |
| `ITemplateCacheService` | ❌ 无 | ✅ 直接复用 |
| `ISafeTextReplacer` / `ISafeFormattedContentReplacer` | ❌ 无 | ✅ 直接复用 |
| `IDocumentCleanupService` | ❌ 无 | ✅ 直接复用 |
| `IProgressReporter` | ⚠️ 依赖 Dispatcher | 🔄 需适配 |
| `IUpdateService` (Velopack) | ⚠️ 平台相关 | 🔄 需重写 |

**关键发现**: DocuFiller 的 DI 架构（Microsoft.Extensions.DependencyInjection）和日志框架（Microsoft.Extensions.Logging）与 ASP.NET Core 完全一致，服务注册可直接迁移。**这是 Electron.NET 方案的最大优势——后端代码几乎零修改即可复用。**

### 2.3 CLI 层：双模式入口

DocuFiller 通过 `Program.cs` 的自定义 `[STAThread] Main` 实现 GUI/CLI 双模式。Electron.NET 架构下，此入口点需要重新设计：

- **GUI 模式**: `electronize start` 启动，ASP.NET Core 自动运行在 Electron 窗口内
- **CLI 模式**: 直接 `dotnet run -- args` 运行（`HybridSupport.IsElectronActive == false`），可保留命令行处理能力

PoC 中的 `Program.cs` 已验证此模式：通过 `HybridSupport.IsElectronActive` 检测运行环境，条件性地执行 Electron 初始化逻辑。

### 2.4 文件对话框

| 功能 | WPF (Microsoft.Win32) | Electron.NET |
|------|----------------------|--------------|
| 打开文件对话框 | `OpenFileDialog` | `Electron.Dialog.ShowOpenDialogAsync` |
| 保存文件对话框 | `SaveFileDialog` | `Electron.Dialog.ShowSaveDialogAsync` |
| 文件过滤器 | `Filter = "Word|*.docx"` | `FileFilter { Extensions = new[] { "docx" } }` |
| 多选 | `Multiselect = true` | `OpenDialogProperty.multiSelections` |
| 跨平台 | ❌ Windows Only | ✅ 三平台 |

PoC 验证：`Electron.Dialog.ShowOpenDialogAsync` 接受 `BrowserWindow` 和 `OpenDialogOptions` 参数，API 设计清晰。注意：`BrowserWindow` 参数不可为 null，必须传入有效窗口实例。

### 2.5 进度汇报

DocuFiller WPF 使用 `Dispatcher.Invoke` / `IProgress<T>` 更新 UI。Electron.NET 下有两种方案：

| 方案 | 优点 | 缺点 |
|------|------|------|
| **SSE (Server-Sent Events)** | 标准化、浏览器可测试、无需 Electron 运行 | 单向通信 |
| **Electron IPC** | 双向实时通信 | 依赖 Electron 运行环境 |

**PoC 选择**: 采用 SSE 方案，因为：更可移植、可在浏览器中独立测试、是 ASP.NET Core 标准模式。IPC 通道作为补充用于原生对话框等必须双向通信的场景。

---

## 3. IPC 通信机制

Electron.NET 提供了 Electron IPC 的 .NET 封装，支持主进程（.NET 后端）与渲染进程（Chromium 前端）之间的双向通信。

### 3.1 API 概览

```csharp
// 后端注册 IPC 监听器
Electron.IpcMain.On("channel-name", (args) => {
    // 处理来自渲染进程的消息
    var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
    Electron.IpcMain.Send(window, "reply-channel", responseData);
});

// 后端主动推送消息到前端
Electron.IpcMain.Send(browserWindow, "progress-update", progressData);
```

```javascript
// 前端（渲染进程）发送 IPC 消息
const { ipcRenderer } = require('electron');
ipcRenderer.send('channel-name', data);
ipcRenderer.on('reply-channel', (event, data) => { /* 处理回复 */ });
```

### 3.2 PoC 实际验证

在 PoC 中注册了两个 IPC 通道：

1. **`select-file-ipc`**: 渲染进程请求后端打开原生文件对话框，后端通过 `ShowOpenDialogAsync` 打开对话框并通过 `select-file-ipc-reply` 返回文件路径
2. **`ping/pong`**: 基础连接测试，验证 IPC 双向通信正常

**发现**: Electron.NET 23.6.2 使用 `Electron.WindowManager.BrowserWindows`（非 `.Windows`）获取窗口列表。IPC 消息传递为字符串格式，复杂对象需手动 JSON 序列化/反序列化。

### 3.3 与 DocuFiller 需求匹配度

| DocuFiller 需求 | IPC 匹配度 | 说明 |
|----------------|-----------|------|
| 文件选择 | ✅ 高 | 原生对话框通过 IPC 触发，完美匹配 |
| 处理进度 | ✅ 高 | SSE 或 IPC 均可满足 |
| 配置修改通知 | ✅ 中 | IPC 可推送，但 HTTP API 更简洁 |
| 错误报告 | ✅ 高 | 双向通信无障碍 |

**结论**: Electron.NET 的 IPC 机制完全可以承载 DocuFiller 的前后端通信需求。对于进度汇报等单向推送场景，SSE 是更优选择；对于原生 API 调用，IPC 是必要路径。

---

## 4. NuGet 生态与依赖

### 4.1 DocuFiller 核心依赖跨平台兼容性

| NuGet 包 | 当前版本 | 跨平台支持 | 说明 |
|----------|---------|-----------|------|
| `DocumentFormat.OpenXml` | 3.0.1 | ✅ 完全支持 | 纯 .NET 实现，不依赖 COM/Office，三平台可用 |
| `EPPlus` | 7.5.2 | ✅ 完全支持 | v5+ 支持 .NET Core，Linux/macOS 均可运行 |
| `CommunityToolkit.Mvvm` | 8.4.0 | ✅ 完全支持 | 平台无关的 MVVM 工具包 |
| `Microsoft.Extensions.*` | 8.0.x | ✅ 完全支持 | ASP.NET Core 基础设施，天然跨平台 |
| `Velopack` | 0.0.1298 | ⚠️ 部分支持 | 主要面向 Windows，macOS 支持有限 |

> 来源: 各 NuGet 包官方文档及 .NET Foundation 兼容性矩阵

### 4.2 Electron.NET 依赖链

Electron.NET 引入的关键依赖：
- `ElectronNET.API` (23.6.2) — .NET API 封装
- `ElectronNET.CLI` (23.6.2) — `electronize` 构建工具
- Electron 运行时（约 150MB+）— Chromium + Node.js

### 4.3 潜在风险

1. **Velopack 替换**: Velopack 在 macOS/Linux 上支持有限，Electron.NET 方案需切换到 `electron-builder` 的自动更新机制
2. **System.Configuration**: DocuFiller 使用 `System.Configuration.ConfigurationManager` 读取 App.config，需迁移到 `appsettings.json`（ASP.NET Core 标准配置系统）

---

## 5. .NET 8 兼容性

### 5.1 现状

ElectronNET.API 23.6.2 的 NuGet 包目标框架为 **net6.0**，但 .NET 的向后兼容性保证了在 net8.0 项目中可以正常使用。

**PoC 验证**: 我们的 PoC 项目 `poc/electron-net-docufiller` 明确使用 `<TargetFramework>net8.0</TargetFramework>`，编译和运行均无兼容性问题。`dotnet build` 输出 0 错误、0 警告。

### 5.2 已知问题

1. **版本追踪**: Electron.NET 的版本号（23.6.2）对应 Electron 23.x，而 Electron 最新已到 30+ 版本。Electron.NET 的 Electron 内核版本滞后于原生 Electron
2. **macOS ARM64**: 社区反馈在 Mac M1/M2 上存在兼容性问题（[Issue #831](https://github.com/ElectronNET/Electron.NET/issues/831)），需要特定的 Electron 版本支持
3. **构建环境**: 在某些自动化环境（如 CI/CD）中，NuGet restore 可能因环境变量缺失而失败，需设置 `NUGET_PACKAGES` 或通过 PowerShell 运行构建

> 来源: PoC 实际构建验证, [GitHub Issue #831](https://github.com/ElectronNET/Electron.NET/issues/831)

---

## 6. 跨平台支持

### 6.1 三平台支持情况

| 平台 | 支持状态 | 说明 |
|------|---------|------|
| **Windows** | ✅ 完全支持 | 一等公民，所有 API 可用 |
| **macOS** | ⚠️ 基本支持 | 大部分功能可用，ARM64 (M1/M2) 存在已知问题 |
| **Linux** | ⚠️ 基本支持 | 依赖 electron-builder 打包，部分原生 API 有差异 |

### 6.2 DocuFiller 核心功能的跨平台可行性

| 功能 | Windows | macOS | Linux |
|------|---------|-------|-------|
| Word 文档处理 (OpenXml) | ✅ | ✅ | ✅ |
| Excel 数据读取 (EPPlus) | ✅ | ✅ | ✅ |
| 原生文件对话框 | ✅ | ✅ | ✅ |
| 原生通知 | ✅ | ✅ | ⚠️ 取决于桌面环境 |
| 系统托盘 | ✅ | ✅ | ✅ |
| 自动更新 | ✅ | ⚠️ | ⚠️ |
| 全局快捷键 | ✅ | ✅ | ✅ |

**结论**: 文档处理核心逻辑（OpenXml + EPPlus）完全跨平台。原生 OS 集成功能在三平台上基本可用，但自动更新机制需要特别处理。

---

## 7. 打包与分发

### 7.1 electron-builder

Electron.NET 使用 [electron-builder](https://www.electron.build/) 作为打包工具，通过 `electronize build` 命令触发。

支持的分发格式：

| 平台 | 格式 | 说明 |
|------|------|------|
| Windows | NSIS 安装包 (.exe) | 支持 WiX、NSIS、便携版 |
| macOS | DMG / PKG | 支持 Apple 代码签名和公证 |
| Linux | AppImage / deb / rpm | 多种 Linux 包格式 |

### 7.2 与 Velopack 的兼容性

DocuFiller 当前使用 Velopack（0.0.1298）实现自动更新。在 Electron.NET 方案中：

- **冲突点**: Velopack 和 electron-builder 都试图管理应用更新流程，不能同时使用
- **解决方案**: 迁移到 electron-builder 内置的自动更新机制（基于 GitHub Releases / S3）
- **迁移成本**: 需要重写更新检查和安装逻辑，但 electron-builder 的 `autoUpdater` API 功能完备

### 7.3 包体积

| 方案 | 预估安装包大小 |
|------|--------------|
| WPF + Velopack (当前) | ~30-50 MB |
| Electron.NET (预估) | ~120-180 MB |

Electron 内核（Chromium + Node.js）贡献了约 100MB+ 的基础体积。这是 Electron 类方案的固有开销。

---

## 8. 社区活跃度与维护状态

### 8.1 GitHub 数据（截至 2026-05）

| 指标 | 数值 |
|------|------|
| Stars | ~7,600 |
| Forks | ~747 |
| Open Issues | ~26 |
| Open Pull Requests | ~5 |
| 最新版本 | 23.6.2 |
| 主要维护者 | Gregor Biswanger, Florian Rappl |
| License | MIT |

> 来源: [GitHub - ElectronNET/Electron.NET](https://github.com/ElectronNET/Electron.NET)

### 8.2 维护状态评估

**积极方面**:
- 项目仍在活跃维护，持续接受 PR
- 有商业赞助支持（GitHub Sponsors）
- 社区讨论区有持续活动

**担忧方面**:
- 版本更新频率较慢（Electron 内核版本滞后于原生 Electron）
- Issue 响应时间不稳定，部分 Issue 长期未关闭
- 核心维护者人数较少（2人），存在 bus factor 风险
- Electron 版本号（23.x）已显著落后于当前 Electron 最新版（30+）

**综合评估**: 社区活跃度中等偏低。对于实验性项目或内部工具可以接受，但对于 DocuFiller 这种面向终端用户的产品，需评估社区支持是否足够应对长期维护需求。

---

## 9. 性能特征

### 9.1 内存占用

| 方案 | 空闲内存 | 工作内存 |
|------|---------|---------|
| WPF (当前) | ~50-80 MB | ~100-200 MB |
| Electron.NET (预估) | ~150-250 MB | ~200-400 MB |

Electron.NET 的额外内存开销主要来自 Chromium 渲染引擎。即使窗口最小化，Chromium 进程仍占用相当数量的内存。

> 来源: Electron 官方性能文档, 社区 benchmark 报告

### 9.2 启动速度

| 方案 | 冷启动 | 热启动 |
|------|--------|--------|
| WPF (当前) | ~1-2 秒 | ~0.5 秒 |
| Electron.NET (预估) | ~3-5 秒 | ~1-2 秒 |

Electron.NET 需要依次启动 Node.js 主进程 → ASP.NET Core Kestrel → BrowserWindow → 页面加载，启动链路较长。

### 9.3 包体积

已在 [第 7 节](#7-打包与分发) 讨论。Electron.NET 的安装包体积约为 WPF 方案的 3-4 倍。

---

## 10. 优缺点总结

### SWOT 分析

#### 优势 (Strengths)

1. **后端代码高复用**: DocuFiller 的服务层（Services/）零 WPF 依赖，可几乎直接复用，这是最大的技术优势
2. **真正的跨平台**: 一套代码覆盖 Windows/macOS/Linux 三平台
3. **Web UI 生态**: 前端可利用成熟的 CSS/JS 生态，设计自由度高于 WPF
4. **ASP.NET Core 天然适配**: DI、日志、配置等基础设施与 DocuFiller 现有架构一致
5. **热重载**: 前端修改可实时预览，加速 UI 开发迭代

#### 劣势 (Weaknesses)

1. **UI 完全重写**: 所有 WPF 窗口需用 HTML/CSS/JS 重建，工作量巨大
2. **性能开销**: 内存占用高、启动慢、包体积大
3. **Electron 内核滞后**: Electron.NET 捆绑的 Electron 版本远落后于最新版
4. **调试复杂度**: 需同时调试 .NET 后端 + Chromium 前端 + IPC 桥接
5. **自动更新需重构**: Velopack 不兼容，需切换到 electron-builder 更新机制

#### 机会 (Opportunities)

1. **市场扩展**: 跨平台能力可覆盖 macOS/Linux 用户群
2. **团队技术栈**: 如果团队有 Web 前端经验，Electron.NET 学习曲线较低
3. **Blazor 集成**: 未来可考虑 Blazor Server/WebView 作为 UI 层，减少 JavaScript 编写

#### 威胁 (Threats)

1. **社区衰退风险**: 维护者少、更新慢，项目可能逐渐停滞
2. **Electron 负面认知**: "内存大户"标签可能影响用户接受度
3. **替代方案竞争**: MAUI、Avalonia、Photino 等方案可能在某些维度更优
4. **macOS ARM64 问题**: Apple Silicon 兼容性问题可能持续存在

---

## 11. 成熟度评估

### 技术成熟度等级 (TRL)

| 维度 | 等级 | 说明 |
|------|------|------|
| Electron.NET 框架 | TRL 7 (系统原型演示) | 可用于实际开发，但生产级案例较少 |
| DocuFiller 服务迁移 | TRL 6 (技术验证) | PoC 验证了核心模式，需更多边界测试 |
| 跨平台打包 | TRL 6 (技术验证) | Windows 打包成熟，macOS/Linux 需实际验证 |
| 自动更新方案 | TRL 4 (实验室验证) | 需替换 Velopack，方案待细化 |
| 整体方案 | **TRL 6** | **技术可行性已验证，距生产就绪尚有差距** |

### 适用场景判断

| 场景 | 推荐度 | 说明 |
|------|--------|------|
| 内部工具/企业应用 | ⭐⭐⭐⭐ | 性能开销可接受，跨平台收益大 |
| 面向终端用户的消费级应用 | ⭐⭐⭐ | 需仔细评估包体积和内存占用的用户接受度 |
| 仅 Windows 平台 | ⭐⭐ | 如果不需要跨平台，WPF 方案更优 |
| 需要高性能/低延迟 | ⭐ | Electron 方案不合适 |

---

## 12. PoC 发现总结

基于 T01（项目脚手架）和 T02（核心功能实现）的实际开发经验：

### 12.1 成功验证的能力

1. **项目搭建**: ASP.NET Core 8 + ElectronNET.API 23.6.2 集成顺利，`dotnet build` 零错误
2. **原生文件对话框**: `Electron.Dialog.ShowOpenDialogAsync` 工作正常，支持文件过滤和多选
3. **SSE 进度汇报**: ASP.NET Core 原生支持 SSE，前端通过 `EventSource` API 无缝接收进度更新
4. **IPC 双向通信**: `Electron.IpcMain.On/Send` 注册和处理正常，ping/pong 测试通过
5. **DI 服务注册**: Microsoft.Extensions.DependencyInjection 与 Electron.NET 完全兼容
6. **浏览器模式降级**: `HybridSupport.IsElectronActive` 检测机制可靠，无 Electron 环境可降级为浏览器应用

### 12.2 遇到的问题及解决方案

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| NuGet restore 失败 ("Value cannot be null (Parameter 'path1')") | 自动化环境中 `ProgramData`/`APPDATA`/`LOCALAPPDATA` 环境变量为空，NuGet 内部调用 `Environment.GetFolderPath` 返回 null | 通过 PowerShell 运行构建（PowerShell 正确设置 Windows Shell 环境变量），或设置 `NUGET_PACKAGES` 环境变量 |
| `Electron.WindowManager.Windows` 编译错误 | API 名称在 23.6.2 中为 `BrowserWindows` 而非 `Windows` | 使用 `Electron.WindowManager.BrowserWindows` |
| IPC 消息无法传递复杂对象 | IPC 仅支持字符串消息 | 手动 JSON 序列化/反序列化 |

### 12.3 开发体验评价

- **构建工具链**: `electronize` CLI 功能完整但文档偏少，需要阅读源码理解部分行为
- **调试体验**: 后端可用标准 .NET 调试器，前端使用 Chrome DevTools，但 IPC 调试需要额外的日志中间层
- **开发效率**: ASP.NET Core 部分开发体验优秀（标准 Web 开发流程），Electron 特有 API 学习成本较低
- **文档质量**: 官方文档存在过时内容（如 API 名称），建议结合 GitHub Issues 和源码验证

---

## 13. 调研日期与信息来源

### 调研时间

2026 年 5 月

### 信息来源

| 来源 | URL |
|------|-----|
| Electron.NET GitHub 仓库 | https://github.com/ElectronNET/Electron.NET |
| ElectronNET.API NuGet | https://www.nuget.org/packages/ElectronNET.API/ |
| ElectronNET.CLI NuGet | https://www.nuget.org/packages/ElectronNET.CLI |
| Electron 官方文档 | https://www.electronjs.org/docs |
| electron-builder 文档 | https://www.electron.build/ |
| DocuFiller 技术架构文档 | `docs/DocuFiller技术架构文档.md` |
| DocuFiller PoC 源码 | `poc/electron-net-docufiller/` |
| macOS ARM64 兼容性 Issue | https://github.com/ElectronNET/Electron.NET/issues/831 |
| DocumentFormat.OpenXml NuGet | https://www.nuget.org/packages/DocumentFormat.OpenXml |
| EPPlus NuGet | https://www.nuget.org/packages/EPPlus |
| Velopack 文档 | https://docs.velopack.io/ |

### PoC 代码参考

| 文件 | 说明 |
|------|------|
| `poc/electron-net-docufiller/electron-net-docufiller.csproj` | 项目配置（net8.0 + ElectronNET.API 23.6.2） |
| `poc/electron-net-docufiller/Program.cs` | Electron 启动、IPC 注册、窗口创建 |
| `poc/electron-net-docufiller/Controllers/ProcessingController.cs` | 原生文件对话框 + SSE 进度汇报 |
| `poc/electron-net-docufiller/Services/SimulatedProcessor.cs` | 模拟文档处理管道（5 步） |
| `poc/electron-net-docufiller/wwwroot/js/app.js` | 前端 SSE 连接 + IPC 桥接 |
| `poc/electron-net-docufiller/electron.manifest.json` | Electron 打包配置 |

---

## 附录：方案决策建议

**推荐**: 如果 DocuFiller 确定需要跨平台（macOS/Linux），Electron.NET 是可行方案，但建议：

1. **优先评估其他方案**: 在最终决策前，与 MAUI、Avalonia 等方案进行横向比较（见本系列其他调研报告）
2. **分阶段迁移**: 先迁移后端服务层（零修改），再逐步重建 UI
3. **验证 macOS ARM64**: 在 Apple Silicon 设备上进行完整测试
4. **评估 Electron 版本风险**: 确认 23.x 是否满足安全更新需求，考虑 fork 的维护成本
