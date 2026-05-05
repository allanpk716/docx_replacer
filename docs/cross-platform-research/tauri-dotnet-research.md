# Tauri v2 + .NET Sidecar 跨平台方案技术调研报告

> **调研日期**: 2026-05  
> **调研范围**: Tauri v2 + .NET sidecar 作为 DocuFiller 跨平台桌面应用的可行性评估  
> **基于**: Tauri v2.11 + .NET 8 sidecar 实际 PoC 开发  
> **版本**: 1.0

---

## 目录

1. [技术概述](#1-技术概述)
2. [与 DocuFiller 的适配性分析](#2-与-docufiller-的适配性分析)
3. [IPC 通信机制](#3-ipc-通信机制)
4. [.NET Sidecar 模式](#4-net-sidecar-模式)
5. [NuGet 生态与依赖](#5-nuget-生态与依赖)
6. [Tauri 生态与插件](#6-tauri-生态与插件)
7. [跨平台支持](#7-跨平台支持)
8. [打包与分发](#8-打包与分发)
9. [社区活跃度与维护状态](#9-社区活跃度与维护状态)
10. [性能特征](#10-性能特征)
11. [优缺点总结](#11-优缺点总结)
12. [成熟度评估](#12-成熟度评估)
13. [PoC 发现总结](#13-poc-发现总结)
14. [调研日期与信息来源](#14-调研日期与信息来源)

---

## 1. 技术概述

[Tauri](https://v2.tauri.app/) 是一个使用 Rust 构建跨平台桌面应用的框架，其核心设计理念是"用系统自带的 WebView 替代捆绑 Chromium"。

### 1.1 架构

Tauri v2 的架构由三层组成：

- **Rust 后端层**：编译为原生二进制，提供系统级 API 访问（文件系统、进程管理、原生对话框等）。通过 `#[tauri::command]` 宏将 Rust 函数暴露为前端可调用的命令。
- **WebView 前端层**：使用操作系统自带的 WebView 引擎渲染 UI——Windows 上是 WebView2（Chromium 内核），macOS 上是 WKWebView（WebKit），Linux 上是 WebKitGTK。
- **IPC 桥接层**：前端通过 `window.__TAURI__.core.invoke()` 调用 Rust 命令，使用 JSON 序列化参数和返回值。

与 Electron 的本质区别：

| 维度 | Electron | Tauri v2 |
|------|----------|----------|
| 渲染引擎 | 捆绑完整 Chromium (~100MB+) | 使用系统 WebView (~0MB 额外) |
| 后端语言 | Node.js | Rust |
| IPC 机制 | Electron IPC (异步消息) | Tauri commands (Rust↔JS, JSON) |
| 打包体积 | ~120-180 MB | ~3-10 MB (不含 .NET sidecar) |
| 内存占用 | ~150-300 MB 空闲 | ~30-50 MB 空闲 |

> 来源: [Tauri v2 官方文档](https://v2.tauri.app/start/), [Tauri vs Electron 对比](https://raftlabs.medium.com/tauri-vs-electron-a-practical-guide-to-picking-the-right-framework-5df80e360f26)

### 1.2 版本信息

- **当前最新稳定版**: Tauri v2.x（v2 于 2024 年 10 月发布稳定版，截至 2025 年 5 月持续迭代中）
- **PoC 使用版本**: tauri 2.x (Cargo.toml), tauri-plugin-dialog 2, tauri-plugin-shell 2
- **许可证**: Apache-2.0 / MIT 双许可

> 来源: [Tauri GitHub Releases](https://github.com/tauri-apps/tauri/releases)

---

## 2. 与 DocuFiller 的适配性分析

DocuFiller 当前架构为 WPF + MVVM（CommunityToolkit.Mvvm），运行在 net8.0-windows 上。Tauri + .NET sidecar 方案的迁移影响逐层分析如下。

### 2.1 UI 层：HTML/CSS/JS 替代 WPF

| 维度 | WPF（当前） | Tauri + sidecar（迁移后） |
|------|-------------|--------------------------|
| 标记语言 | XAML | HTML/CSS |
| 数据绑定 | `{Binding}` + ObservableObject | JavaScript 原生 DOM 或前端框架 |
| 样式系统 | ResourceDictionary + Styles | CSS |
| UI 组件库 | WPF 内置控件 | Web 组件库或原生 HTML |

**评估**: 与 Electron.NET 方案相同，UI 层需要完全重写。但 Tauri 的前端就是标准的 Web 页面，无需学习任何 Electron 特有 API。PoC 中使用纯 HTML + 原生 JavaScript 实现，代码量极低（app.js ~150 行，index.html ~80 行）。

### 2.2 后端层：服务复用性分析

DocuFiller 的核心服务层在 Tauri 方案中有两种复用路径：

**路径 A — Rust 重写后端逻辑**：放弃 .NET 依赖，将文档处理逻辑用 Rust 重写。此路径放弃了 DocumentFormat.OpenXml 和 EPPlus 等成熟的 .NET 库，工作量极大且不现实。

**路径 B — .NET sidecar 模式**（PoC 选择）：保留 .NET 后端作为独立进程运行，通过 HTTP API 与 Tauri 前端通信。此路径的核心优势是 DocuFiller 的服务层（`Services/` 命名空间）可几乎零修改地复用到 sidecar 中。

| 服务 | WPF 依赖 | Sidecar 复用难度 |
|------|----------|-----------------|
| `IDocumentProcessor` | ❌ 无 | ✅ 直接复用 |
| `IExcelDataParser` | ❌ 无 | ✅ 直接复用 |
| `IFileService` | ❌ 无 | ✅ 直接复用 |
| `IFileScanner` | ❌ 无 | ✅ 直接复用 |
| `ITemplateCacheService` | ❌ 无 | ✅ 直接复用 |
| `ISafeTextReplacer` | ❌ 无 | ✅ 直接复用 |
| `IDocumentCleanupService` | ❌ 无 | ✅ 直接复用 |
| `IProgressReporter` | ⚠️ 依赖 Dispatcher | 🔄 改为 SSE 推送 |
| `IUpdateService` (Velopack) | ⚠️ 平台相关 | 🔄 需重写 |

### 2.3 CLI 层：双模式入口

Tauri 架构下，CLI 模式需要重新考虑：
- **GUI 模式**: Tauri 启动 WebView 窗口，前端通过 IPC 调用 Rust 命令，Rust 启动 .NET sidecar
- **CLI 模式**: 直接运行 .NET sidecar 的 Headless 模式（不启动 Tauri 窗口），或者设计独立的 .NET CLI 入口

### 2.4 文件对话框

| 功能 | WPF (Microsoft.Win32) | Tauri (tauri-plugin-dialog) |
|------|----------------------|---------------------------|
| 打开文件对话框 | `OpenFileDialog` | `app.dialog().file().blocking_pick_file()` |
| 文件过滤器 | `Filter = "Word|*.docx"` | `.add_filter("Documents", &["docx", "xlsx"])` |
| 多选 | `Multiselect = true` | `.blocking_pick_files()` (返回 Vec) |
| 跨平台 | ❌ Windows Only | ✅ 三平台 |

PoC 验证：`tauri_plugin_dialog::DialogExt` 提供的文件对话框 API 简洁直观，通过 `#[tauri::command]` 包装后，前端一行 `window.__TAURI__.core.invoke('open_file_dialog')` 即可调用。

### 2.5 进度汇报

DocuFiller WPF 使用 `Dispatcher.Invoke` / `IProgress<T>` 更新 UI。Tauri + sidecar 方案下：

| 方案 | 优点 | 缺点 |
|------|------|------|
| **SSE (Server-Sent Events)** | 标准化、浏览器可测试、无需 Tauri 特有 API | 单向通信 |
| **Tauri Events** | 双向实时通信，原生支持 | 需要 Rust 中转 |
| **HTTP Polling** | 简单可靠 | 延迟高、轮询开销 |

**PoC 选择**: 采用 SSE 方案，通过 .NET sidecar 的 ASP.NET Core SSE 端点直接推送到前端 JavaScript（`ReadableStream` reader 解析）。前端 fetch() 请求受 CSP `connect-src` 控制，安全且标准。

---

## 3. IPC 通信机制

Tauri v2 + .NET sidecar 架构中存在三级通信：

### 3.1 Tauri Commands（Rust ↔ 前端）

Tauri 的核心 IPC 机制是 **commands**——Rust 侧用 `#[tauri::command]` 标注的函数自动暴露为前端可调用的异步命令。

```rust
// Rust 侧
#[tauri::command]
fn open_file_dialog(app: tauri::AppHandle) -> Result<Option<String>, String> {
    let file_path = app.dialog().file()
        .add_filter("Documents", &["docx", "xlsx"])
        .set_title("Select a document to process")
        .blocking_pick_file();
    Ok(file_path.map(|p| p.to_string()))
}
```

```javascript
// 前端侧
const result = await window.__TAURI__.core.invoke('open_file_dialog');
```

**特点**: 类型安全（Rust 类型自动序列化为 JSON）、同步/异步均支持、错误通过 `Result<T, E>` 的 `E` 传递。

### 3.2 Sidecar HTTP API（.NET ↔ 前端）

.NET sidecar 作为独立 Kestrel HTTP 服务器运行在 `localhost:5000`，前端直接通过 `fetch()` 调用。

```csharp
// .NET sidecar
app.MapGet("/api/process/stream", async (HttpContext context, string? filePath) => {
    context.Response.ContentType = "text/event-stream";
    // SSE 流式推送处理进度...
});
```

```javascript
// 前端直接调用 sidecar（通过 CSP connect-src 授权）
const resp = await fetch('http://localhost:5000/api/process/stream?filePath=' + encodedPath);
const reader = resp.body.getReader();
// 解析 SSE 事件...
```

**特点**: 标准 HTTP 协议，无需 Tauri 中转，浏览器 DevTools 可直接调试。

### 3.3 Sidecar stdin/stdout（Rust ↔ .NET 进程级通信）

Tauri 可通过 `std::process::Command` 或 `tauri-plugin-shell` 启动 sidecar 进程，通过 stdin/stdout 管道进行结构化通信。

```rust
let child = std::process::Command::new("dotnet")
    .args(["run", "--project", "../sidecar-dotnet"])
    .spawn()
    .map_err(|e| format!("Failed to start sidecar: {}", e))?;
```

**PoC 选择**: 使用 `std::process::Command` 启动 sidecar，但实际的业务数据通信走 HTTP API 而非 stdin/stdout。原因是 HTTP API 更标准、更易调试、支持 SSE 等流式场景。

### 3.4 三种模式对比

| 维度 | Tauri Commands | Sidecar HTTP API | Sidecar stdin/stdout |
|------|---------------|-----------------|---------------------|
| 延迟 | ~1ms（进程内） | ~5-10ms（localhost HTTP） | ~1ms（管道） |
| 协议 | Tauri 自定义 JSON | 标准 HTTP/SSE | 自定义二进制/文本 |
| 调试 | Rust 日志 | 浏览器 DevTools | 进程级日志 |
| 适用场景 | 原生 API 调用 | 数据传输、进度推送 | 进程控制、轻量信令 |
| 复杂度 | 低 | 低 | 中（需自定义协议） |

**结论**: DocuFiller 场景推荐 **Tauri Commands + Sidecar HTTP API 组合**——原生功能（文件对话框、窗口控制）走 Tauri commands，业务数据（文档处理、进度推送）走 sidecar HTTP/SSE。

---

## 4. .NET Sidecar 模式

### 4.1 架构设计

在 Tauri + .NET sidecar 模式下，应用由两个进程组成：

```
┌─────────────────────────────────────┐
│  Tauri 主进程                        │
│  ┌───────────────┐  ┌────────────┐  │
│  │  Rust 后端     │  │  WebView   │  │
│  │  (commands)   │←→│  (前端 UI)  │  │
│  └───────────────┘  └─────┬──────┘  │
│                           │ fetch()  │
└───────────────────────────┼─────────┘
                            │ HTTP/SSE
                    ┌───────▼────────┐
                    │  .NET Sidecar  │
                    │  (Kestrel)     │
                    │  localhost:5000│
                    │  ┌────────────┐│
                    │  │ DocuFiller ││
                    │  │ Services   ││
                    │  └────────────┘│
                    └────────────────┘
```

### 4.2 优势

1. **服务层完整复用**: DocuFiller 的所有 .NET 服务（OpenXml、EPPlus、DI 容器等）直接运行在 sidecar 中，无需重写
2. **独立可测试**: Sidecar 是标准 ASP.NET Core 应用，可独立启动、用 curl/Postman 测试
3. **技术栈解耦**: Tauri 前端和 .NET 后端独立演进、独立部署、独立更新
4. **.NET 生态完整**: 可直接使用所有 NuGet 包，不受 Tauri/Rust 限制

### 4.3 劣势

1. **双进程复杂度**: 需要管理 sidecar 生命周期（启动、健康检查、崩溃重启、端口冲突）
2. **安装包体积增大**: 需要捆绑 .NET 运行时或 sidecar 自包含发布（+30-70 MB）
3. **通信延迟**: 相比进程内调用，HTTP localhost 有额外延迟（通常 <10ms，可忽略）
4. **端口冲突风险**: localhost:5000 可能被其他进程占用，需要动态端口分配逻辑

### 4.4 生命周期管理

PoC 中的 sidecar 生命周期：

1. **启动**: Tauri Rust 后端通过 `std::process::Command::new("dotnet")` 启动 sidecar
2. **健康检查**: 前端 JavaScript 定期 `fetch('/api/health')` 检查 sidecar 状态
3. **通信**: 前端通过 HTTP/SSE 与 sidecar 交互
4. **关闭**: Tauri 主进程退出时，sidecar 子进程随之终止

> 来源: PoC 实际实现 (`poc/tauri-docufiller/src-tauri/src/lib.rs`, `poc/tauri-docufiller/sidecar-dotnet/Program.cs`)

---

## 5. NuGet 生态与依赖

### 5.1 DocuFiller 核心依赖在 Sidecar 中的可用性

| NuGet 包 | 当前版本 | Sidecar 中可用 | 说明 |
|----------|---------|---------------|------|
| `DocumentFormat.OpenXml` | 3.0.1 | ✅ 完全支持 | 纯 .NET 实现，不依赖 COM/Office |
| `EPPlus` | 7.5.2 | ✅ 完全支持 | v5+ 支持 .NET Core，跨平台可用 |
| `CommunityToolkit.Mvvm` | 8.4.0 | ⚠️ 不需要 | Sidecar 无 UI，MVVM 不适用 |
| `Microsoft.Extensions.*` | 8.0.x | ✅ 完全支持 | ASP.NET Core 基础设施，天然适配 |
| `Velopack` | 0.0.1298 | ⚠️ 需评估 | 自动更新逻辑需适配 Tauri 的打包方式 |

> 来源: DocuFiller.csproj (`DocuFiller.csproj`), PoC sidecar 项目 (`poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj`)

### 5.2 Sidecar 依赖链

PoC 中 sidecar 项目的依赖极简：

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

仅依赖 ASP.NET Core（SDK 内置），无需额外的 NuGet 包即可实现 HTTP API 和 SSE。正式迁移时需要添加 DocumentFormat.OpenXml、EPPlus 等处理库。

### 5.3 自包含发布

为避免要求用户预装 .NET 运行时，sidecar 可使用 .NET 的自包含发布（self-contained deploy）：

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

单文件自包含发布后 sidecar 体积约 30-50 MB（含 .NET 运行时）。这会加入 Tauri 安装包的总体积中。

---

## 6. Tauri 生态与插件

### 6.1 核心插件

| 插件 | 版本 | 用途 | PoC 使用 |
|------|------|------|---------|
| `tauri-plugin-dialog` | 2.x | 原生文件对话框（打开/保存/消息） | ✅ 文件选择 |
| `tauri-plugin-shell` | 2.x | Shell 命令执行、sidecar 管理 | ✅ 已注册，但用 std::process::Command 替代 |
| `tauri-plugin-fs` | 2.x | 文件系统读写 | ❌ 未使用 |
| `tauri-plugin-updater` | 2.x | 应用自动更新 | ❌ 未使用 |
| `tauri-plugin-process` | 2.x | 进程管理 | ❌ 未使用 |

### 6.2 权限模型

Tauri v2 引入了基于 **capabilities** 的权限系统，取代了 v1 的 `allowlist` 机制：

```json
// capabilities/default.json
{
  "identifier": "default",
  "description": "Default capabilities",
  "windows": ["main"],
  "permissions": [
    "core:default",
    "dialog:default",
    "shell:allow-open"
  ]
}
```

PoC 中需授权：`dialog:default`（文件对话框）、`shell:allow-open`（sidecar 启动）。

### 6.3 CSP 配置

Tauri v2 强制执行 Content Security Policy（CSP），这是与 Electron 的重要区别。PoC 中的配置：

```json
{
  "security": {
    "csp": "default-src 'self'; script-src 'self'; connect-src 'self' http://localhost:5000; style-src 'self' 'unsafe-inline'"
  }
}
```

关键点：
- `connect-src http://localhost:5000` 允许前端 fetch() 访问 sidecar
- `script-src 'self'` 禁止内联脚本执行（安全性高，但要求 JS 放在文件中）
- CSP 违规会在 WebView 控制台中显示，便于调试

> 来源: PoC 配置 (`poc/tauri-docufiller/src-tauri/tauri.conf.json`), [Tauri v2 CSP 文档](https://v2.tauri.app/concept/security/)

### 6.4 与 Velopack 的兼容性

Velopack 是 DocuFiller 当前的自动更新方案。在 Tauri 架构中：

- **可行性**: Velopack 可作为 Tauri 应用的外部更新管理器使用。Tauri 本身的 `tauri-plugin-updater` 也是可选方案。
- **集成方式**: Velopack 管理 Tauri 安装包（MSI/NSIS）的更新，sidecar 可作为 Tauri bundle 的资源一并分发。
- **替代方案**: Tauri 内置的 `tauri-plugin-updater` 支持从自建服务器检查和下载更新，功能足够但生态不如 Velopack 成熟。

---

## 7. 跨平台支持

### 7.1 三平台 WebView 情况

| 平台 | WebView 引擎 | 预装状态 | 兼容性 |
|------|-------------|---------|--------|
| **Windows** | WebView2 (Chromium Edge) | Win10/11 预装；Win7 需安装 | ✅ 优秀 |
| **macOS** | WKWebView (Safari/WebKit) | 系统自带 | ✅ 优秀 |
| **Linux** | WebKitGTK | 需用户安装 `libwebkit2gtk-4.1-dev` | ⚠️ 依赖系统包 |

### 7.2 DocuFiller 核心功能的跨平台可行性

| 功能 | Windows | macOS | Linux |
|------|---------|-------|-------|
| Word 文档处理 (OpenXml via .NET) | ✅ | ✅ | ✅ |
| Excel 数据读取 (EPPlus via .NET) | ✅ | ✅ | ✅ |
| 原生文件对话框 | ✅ | ✅ | ✅ |
| 原生通知 | ✅ | ✅ | ⚠️ 取决于桌面环境 |
| 系统托盘 | ✅ | ✅ | ✅ |
| 自动更新 | ✅ | ⚠️ 需适配 | ⚠️ 需适配 |
| .NET sidecar | ✅ | ✅ | ✅ |

**关键发现**: Tauri 的原生 API（对话框、通知、托盘）在 Windows 上最成熟，macOS 表现良好，Linux 因桌面环境多样性存在一定差异。但由于 DocuFiller 的核心处理逻辑在 .NET sidecar 中运行（完全跨平台），Tauri 层仅负责 UI 和原生对话框，跨平台风险可控。

> 来源: [Tauri v2 跨平台文档](https://v2.tauri.app/concept/), PoC 实际验证

---

## 8. 打包与分发

### 8.1 Tauri Bundler 支持

Tauri 内置 bundler 支持多平台打包：

| 平台 | 格式 | 说明 |
|------|------|------|
| **Windows** | MSI (WiX v3) / NSIS setup.exe | 支持自定义安装界面、i18n |
| **macOS** | DMG / App Bundle | 支持 Apple 代码签名和公证 |
| **Linux** | AppImage / deb / rpm | 多种包格式 |

### 8.2 安装包体积对比

| 方案 | 预估安装包大小 |
|------|--------------|
| WPF + Velopack (当前) | ~30-50 MB |
| Tauri + .NET sidecar 自包含 | ~40-70 MB |
| Tauri (不含 sidecar) | ~3-10 MB |
| Electron.NET (参考) | ~120-180 MB |

Tauri + .NET sidecar 的体积介于 WPF 和 Electron.NET 之间。其中 .NET sidecar 自包含发布约 30-50 MB，Tauri 本体约 3-10 MB。

> 来源: [Tauri App Size 文档](https://v2.tauri.app/concept/size/), [Tauri Windows Installer](https://v2.tauri.app/distribute/windows-installer/), [Tauri DMG](https://v2.tauri.app/distribute/dmg/)

### 8.3 Sidecar 分发策略

将 .NET sidecar 捆绑到 Tauri 安装包中有两种方式：

1. **Tauri resources**: 在 `tauri.conf.json` 的 `bundle.resources` 中指定 sidecar 二进制文件，打包时自动包含
2. **Sidecar 外部管理**: 首次启动时从远程下载 sidecar，或通过 Velopack 管理独立更新

PoC 中 sidecar 使用 `dotnet run` 开发模式运行，生产环境需要改为发布后的独立二进制。

### 8.4 与 Velopack 的协作可能性

Tauri 的 `tauri-plugin-updater` 提供内置自动更新能力，但 DocuFiller 已有 Velopack 基础设施。两种协作模式：

- **模式 A — Velopack 管理 Tauri 安装包**: Velopack 将 Tauri 的 MSI/NSIS 输出作为更新载荷，完全复用现有的更新管道
- **模式 B — Tauri updater + 独立 sidecar 更新**: Tauri 负责主应用更新，sidecar 通过自有机制更新

推荐 **模式 A**，减少维护两套更新机制的成本。

---

## 9. 社区活跃度与维护状态

### 9.1 GitHub 数据（截至 2026-05）

| 指标 | 数值 |
|------|------|
| Stars | ~90,000+ |
| Forks | ~2,800+ |
| Open Issues | ~300+ (含功能请求) |
| 核心贡献者 | Tauri 团队（全职开发，有商业赞助） |
| 最新版本 | Tauri v2.x (持续迭代) |
| License | Apache-2.0 / MIT |

> 来源: [GitHub - tauri-apps/tauri](https://github.com/tauri-apps/tauri)

### 9.2 与 Electron.NET 对比

| 维度 | Tauri | Electron.NET |
|------|-------|-------------|
| GitHub Stars | ~90,000+ | ~7,600 |
| 核心维护者 | 10+ 全职 | ~2 兼职 |
| 版本更新频率 | 高（每月迭代） | 低（滞后 Electron 主线） |
| 商业支持 | 有（CrabNebula 等） | 有限 |
| Electron/Chromium 版本 | 不依赖 | 滞后（23.x vs 30+） |

### 9.3 Tauri v2 稳定性评估

Tauri v2 于 2024 年 10 月发布稳定版，经过 6+ 个月的社区验证：

**积极方面**:
- v2 架构相比 v1 有质的飞跃（插件系统、权限模型、移动端支持）
- 社区活跃，Issue 和 Discussion 响应及时
- 多个知名应用已采用 Tauri v2（如 Clash Verge、PingCAP 的 TiDB 工具）

**风险方面**:
- v2 部分 API 仍在迭代，Minor 版本可能有 Breaking Change
- 文档存在滞后于代码的情况
- 插件生态虽在快速扩张，但部分插件成熟度不足

**综合评估**: 社区活跃度和维护状态远优于 Electron.NET。Tauri 是 Rust 桌面应用领域的事实标准框架，长期维护风险低。

---

## 10. 性能特征

### 10.1 内存占用

| 方案 | 空闲内存 | 工作内存 |
|------|---------|---------|
| WPF (当前) | ~50-80 MB | ~100-200 MB |
| Tauri + .NET sidecar | ~60-100 MB | ~120-250 MB |
| Electron.NET (参考) | ~150-250 MB | ~200-400 MB |

Tauri + .NET sidecar 的内存占用主要由 .NET sidecar 贡献。Tauri 本体（Rust + WebView）空闲内存约 30-40 MB，加上 .NET sidecar 的 30-60 MB，总体显著低于 Electron.NET。

> 来源: [Tauri vs Electron 内存对比](https://raftlabs.medium.com/tauri-vs-electron-a-practical-guide-to-picking-the-right-framework-5df80e360f26), [LinkedIn 技术分析](https://www.linkedin.com/pulse/beyond-electron-why-tauri-v2-redefining-cross-platform-app)

### 10.2 启动速度

| 方案 | 冷启动 | 热启动 |
|------|--------|--------|
| WPF (当前) | ~1-2 秒 | ~0.5 秒 |
| Tauri + .NET sidecar | ~2-4 秒 | ~1-1.5 秒 |
| Electron.NET (参考) | ~3-5 秒 | ~1-2 秒 |

Tauri 本体启动极快（Rust 原生二进制 + 系统 WebView），主要延迟来自 .NET sidecar 启动时间。可通过 sidecar 常驻或延迟启动优化。

### 10.3 安装包体积

已在 [第 8 节](#8-打包与分发) 讨论。Tauri + .NET sidecar 的安装包体积约为 Electron.NET 方案的 1/2~1/3。

---

## 11. 优缺点总结

### SWOT 分析

#### 优势 (Strengths)

1. **性能优异**: 内存占用和安装包体积均显著优于 Electron.NET
2. **.NET 服务层高复用**: DocuFiller 的核心处理逻辑（OpenXml、EPPlus、DI）可完整复用到 sidecar
3. **安全性强**: Tauri 的权限模型（capabilities）和强制 CSP 提供多层安全保障
4. **社区活跃**: Tauri 社区远比 Electron.NET 活跃，长期维护风险低
5. **Rust 后端**: 原生性能、无 GC 停顿、内存安全保证

#### 劣势 (Weaknesses)

1. **双进程复杂度**: 需要管理 sidecar 生命周期（启动、健康检查、端口冲突、崩溃恢复）
2. **Rust 学习曲线**: 如果需要扩展 Tauri 后端功能（非 sidecar 部分），需要 Rust 技能
3. **UI 完全重写**: WPF 窗口需用 HTML/CSS/JS 重建
4. **Linux WebView 依赖**: WebKitGTK 需要用户手动安装，增加分发复杂度

#### 机会 (Opportunities)

1. **移动端扩展**: Tauri v2 原生支持 iOS/Android，为 DocuFiller 提供未来移动化路径
2. **安装包体积小**: 对比 Electron.NET 优势明显，用户下载体验更好
3. **生态系统增长**: Tauri 插件生态快速增长，可利用的现成方案越来越多
4. **Velopack 协作**: 现有 Velopack 更新管道可与 Tauri 打包格式良好协作

#### 威胁 (Threats)

1. **双进程架构风险**: sidecar 崩溃、端口冲突等运行时问题增加故障面
2. **WebView 兼容性**: 不同平台的 WebView 渲染差异可能导致 UI 不一致
3. **.NET sidecar 分发**: 自包含发布增加安装包体积，框架依赖则要求用户安装 .NET
4. **Tauri v2 成熟度**: 虽已稳定，但部分 API 仍在迭代

---

## 12. 成熟度评估

### 技术成熟度等级 (TRL)

| 维度 | 等级 | 说明 |
|------|------|------|
| Tauri v2 框架 | TRL 8 (系统完成并合格) | 稳定版已发布，多个生产级应用使用 |
| .NET sidecar 模式 | TRL 6 (技术验证) | PoC 验证了核心模式，需更多边界测试 |
| DocuFiller 服务迁移 | TRL 6 (技术验证) | 服务层无 WPF 依赖，但未在 sidecar 中实际运行 |
| 跨平台打包 + sidecar 捆绑 | TRL 5 (组件验证) | 理论可行，需实际验证三平台打包 |
| 自动更新方案 | TRL 5 (组件验证) | Velopack + Tauri 协作方案待验证 |
| 整体方案 | **TRL 6** | **技术可行性已通过 PoC 验证，距生产就绪需额外工作** |

### 适用场景判断

| 场景 | 推荐度 | 说明 |
|------|--------|------|
| 跨平台桌面应用（性能敏感） | ⭐⭐⭐⭐⭐ | Tauri 的最佳场景 |
| 跨平台桌面应用（一般） | ⭐⭐⭐⭐ | 性能和体积优势明显 |
| 仅 Windows 平台 | ⭐⭐ | 不需要跨平台时 WPF 更简单 |
| 需要极致 Web 兼容性 | ⭐⭐⭐ | 系统 WebView 可能不如 Chromium 完整 |

---

## 13. PoC 发现总结

基于 T01（项目脚手架）和 T02（核心功能实现）的实际开发经验：

### 13.1 成功验证的能力

1. **双工具链编译**: `cargo build` 和 `dotnet build` 均零错误通过。Tauri v2 编译需要 rustc 1.88+（因 `time` crate 0.3.47+），需从 1.86 升级到 1.95。
2. **原生文件对话框**: `tauri_plugin_dialog::DialogExt` 提供的 `dialog().file()` API 简洁可靠，支持文件类型过滤，通过 `#[tauri::command]` 暴露给前端一行调用。
3. **SSE 进度推送**: .NET sidecar 的 ASP.NET Core SSE 端点正常工作。前端使用 `ReadableStream` reader（非 `EventSource`）解析 SSE，错误处理更好且与 `fetch()` 配合遵循 CSP。
4. **CSP 配置**: `connect-src http://localhost:5000` 正确允许前端访问 sidecar，SSE 连接无 CSP 违规。
5. **sidecar 健康检查**: 前端 JavaScript `fetch('/api/health')` 检测 sidecar 状态，无需在 Rust 后端引入 `reqwest` 等重量级 HTTP 依赖。
6. **sidecar 启动**: `std::process::Command::new("dotnet")` 启动 sidecar 成功，比 `tauri-plugin-shell` 的 `ShellExt` API 更简单直接。

### 13.2 遇到的问题及解决方案

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| Rust 编译失败（`time` crate 要求 rustc 1.88+） | Tauri v2.11 的传递依赖 `plist v1.9.0` 固定 `time = "^0.3.47"`，要求 rustc 1.88+ | 升级 rustc 从 1.86.0 到 1.95.0 |
| 前端无法用 `reqwest` 做健康检查 | `reqwest` 引入大量传递依赖，增加编译时间和二进制体积 | 改用前端 JS `fetch()` 直接调 sidecar health endpoint |
| `EventSource` API 不适合 SSE 场景 | `EventSource` 不支持自定义请求头、错误处理有限 | 改用 `fetch()` + `ReadableStream` reader 手动解析 SSE |
| Sidecar 跨域问题 | Tauri WebView origin 与 localhost:5000 不同源 | sidecar SSE 响应添加 `Access-Control-Allow-Origin: *` 头 |

### 13.3 开发体验评价

- **Rust 后端**: `#[tauri::command]` 宏简化了 IPC 定义，但 Rust 语言本身的学习曲线较陡。对于简单的命令封装（如调用插件 API），代码量很少。
- **前端开发**: 标准 Web 开发流程，无需学习 Tauri 特有前端 API（除 `window.__TAURI__.core.invoke` 外）。可用任何前端框架或纯 JS。
- **.NET sidecar**: 标准 ASP.NET Core Minimal API 开发，体验优秀。DI、日志、配置等基础设施完全复用。
- **调试体验**: 前端可用浏览器 DevTools（Tauri dev 模式自动启用）；.NET sidecar 用标准 .NET 调试器；Rust 用 `println!` 或 `log` crate。三层的独立调试比 Electron.NET 的统一调试更分散但更灵活。
- **编译速度**: Rust 首次编译耗时约 15-25 秒（依赖多），增量编译约 2-5 秒。.NET sidecar 编译 1-2 秒。总体可接受。

---

## 14. 调研日期与信息来源

### 调研时间

2026 年 5 月

### 信息来源

| 来源 | URL/路径 |
|------|---------|
| Tauri v2 官方文档 | https://v2.tauri.app/ |
| Tauri GitHub 仓库 | https://github.com/tauri-apps/tauri |
| Tauri vs Electron 对比分析 | https://raftlabs.medium.com/tauri-vs-electron-a-practical-guide-to-picking-the-right-framework-5df80e360f26 |
| Tauri App Size 优化 | https://v2.tauri.app/concept/size/ |
| Tauri Windows Installer | https://v2.tauri.app/distribute/windows-installer/ |
| Tauri DMG 分发 | https://v2.tauri.app/distribute/dmg/ |
| Tauri AppImage 分发 | https://v2.tauri.app/distribute/appimage/ |
| Tauri v2 安全模型 | https://v2.tauri.app/concept/security/ |
| DocumentFormat.OpenXml NuGet | https://www.nuget.org/packages/DocumentFormat.OpenXml |
| EPPlus NuGet | https://www.nuget.org/packages/EPPlus |
| Velopack 文档 | https://docs.velopack.io/ |
| DocuFiller 技术架构文档 | `docs/DocuFiller技术架构文档.md` |
| Electron.NET 调研报告 | `docs/cross-platform-research/electron-net-research.md` |

### PoC 代码参考

| 文件 | 说明 |
|------|------|
| `poc/tauri-docufiller/src-tauri/Cargo.toml` | Rust 依赖配置（tauri 2.x + dialog/shell 插件） |
| `poc/tauri-docufiller/src-tauri/tauri.conf.json` | Tauri 应用配置（窗口、CSP、bundle） |
| `poc/tauri-docufiller/src-tauri/capabilities/default.json` | 权限声明（dialog + shell） |
| `poc/tauri-docufiller/src-tauri/src/lib.rs` | Rust 后端（文件对话框 + sidecar 启动命令） |
| `poc/tauri-docufiller/sidecar-dotnet/Program.cs` | .NET sidecar（HTTP API + SSE 进度推送） |
| `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj` | Sidecar 项目配置（net8.0 + ASP.NET Core） |
| `poc/tauri-docufiller/src/index.html` | 前端页面（文件选择 + 进度条 + 事件日志） |
| `poc/tauri-docufiller/src/app.js` | 前端逻辑（Tauri IPC + SSE 解析 + 健康检查） |

---

## 附录：方案决策建议

**推荐**: Tauri + .NET sidecar 是 DocuFiller 跨平台方案中**技术综合评分最高**的选择。

相比 Electron.NET 的核心优势：
1. **安装包体积**: 约为 Electron.NET 的 1/2~1/3
2. **内存占用**: 约为 Electron.NET 的 1/2
3. **社区活跃度**: Tauri stars 是 Electron.NET 的 10+ 倍，维护团队更专业
4. **安全性**: Tauri 的 capabilities 权限模型 + 强制 CSP 比 Electron 更严格

建议后续步骤：
1. **S05 方案横比**: 将本报告与 Electron.NET、MAUI、Avalonia 等方案进行综合对比
2. **sidecar 生命周期完善**: 实现 sidecar 崩溃重启、端口动态分配、优雅关闭
3. **三平台实际打包验证**: 验证 MSI/DMG/AppImage 的 sidecar 捆绑
4. **性能基准测试**: 在三平台上实测内存和启动时间
