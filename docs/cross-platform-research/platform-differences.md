# DocuFiller 跨平台迁移 — 平台差异处理调研报告

> **调研日期**: 2026-05  
> **调研范围**: DocuFiller 从 net8.0-windows (WPF) 迁移到跨平台 (net8.0) 时需处理的操作系统差异  
> **基于**: DocuFiller 源码分析、.NET BCL 跨平台文档、Avalonia/Tauri/Electron 官方文档、XDG 规范、社区实践  
> **版本**: 1.0

---

## 目录

1. [调研概述](#1-调研概述)
2. [文件对话框](#2-文件对话框)
3. [拖放功能](#3-拖放功能)
4. [路径处理](#4-路径处理)
5. [文件系统权限与目录规范](#5-文件系统权限与目录规范)
6. [注册表使用](#6-注册表使用)
7. [进程管理](#7-进程管理)
8. [其他平台差异](#8-其他平台差异)
9. [DocuFiller 代码中的平台特定点清单](#9-docufiller-代码中的平台特定点清单)
10. [迁移策略建议](#10-迁移策略建议)
11. [优缺点总结](#11-优缺点总结)
12. [调研日期与信息来源](#12-调研日期与信息来源)

---

## 1. 调研概述

### 1.1 调研背景

DocuFiller 当前基于 WPF (net8.0-windows) 构建，深度依赖 Windows 专有 API：
- `Microsoft.Win32.OpenFileDialog` / `OpenFolderDialog`（文件对话框）
- `System.Windows.DragDrop`（拖放行为）
- `Environment.SpecialFolder`（Windows 路径约定）
- `System.Diagnostics.Process.Start` with `UseShellExecute`（打开文件夹）
- `System.Configuration.ConfigurationManager`（遗留配置）

迁移到跨平台 (net8.0) 意味着这些 API 必须替换为跨平台等效方案，或由所选 UI 框架的对应 API 替代。

### 1.2 调研方法

1. 逐一扫描 DocuFiller 源码中所有 Windows 专有 API 调用
2. 对每个差异点，分析 Avalonia、Tauri、Electron 三大跨平台方案的对应处理方式
3. 评估 .NET BCL 本身的跨平台能力（`System.IO.Path`、`Environment.SpecialFolder` 等）
4. 参考社区迁移实践和官方指南

### 1.3 UI 框架候选方案

| 框架 | 语言 | 文件对话框 | 拖放 | 跨平台一致性 |
|------|------|-----------|------|-------------|
| **Avalonia UI** | C# / XAML | 内置 `StorageProvider` API | 内置 DragDrop 事件 | ★★★★★ (WPF DNA，迁移最平滑) |
| **Tauri** | Rust + Web 前端 | `dialog` 插件 | Web 标准 Drag & Drop API | ★★★★☆ |
| **Electron** | JavaScript | `dialog.showOpenDialog()` | Web 标准 Drag & Drop API | ★★★★☆ |

> **注意**: 本报告聚焦于平台差异本身，不推荐具体框架选择——框架选型由 S05 评估阶段决定。以下分析会在每个差异点列出各框架的处理方式。

---

## 2. 文件对话框

### 2.1 现状分析

DocuFiller 当前使用 `Microsoft.Win32.OpenFileDialog` 和 `Microsoft.Win32.OpenFolderDialog`：

**涉及文件及用途**：

| 文件 | API | 用途 |
|------|-----|------|
| `ViewModels/FillViewModel.cs` | `OpenFileDialog` × 2 | 选择 Word 模板文件、选择 Excel 数据文件 |
| `ViewModels/FillViewModel.cs` | `OpenFolderDialog` × 2 | 选择输出目录、批量选择输出目录 |
| `DocuFiller/ViewModels/CleanupViewModel.cs` | `OpenFolderDialog` | 选择清理输出目录 |

**关键代码模式** (FillViewModel.cs):
```csharp
// 打开文件对话框
var dialog = new OpenFileDialog
{
    Title = "选择Word模板文件",
    Filter = "Word文档 (*.docx)|*.docx|所有文件 (*.*)|*.*",
    CheckFileExists = true
};
if (dialog.ShowDialog() == true) { ... }

// 打开文件夹对话框
var dialog = new OpenFolderDialog { Title = "选择输出目录" };
if (dialog.ShowDialog() == true) { ... }
```

### 2.2 `Microsoft.Win32.OpenFileDialog` 的跨平台限制

- `Microsoft.Win32.OpenFileDialog` 是 **Windows 专有** API，定义在 `Microsoft.Win32.Registry.dll` / `PresentationFramework.dll` 中
- `Microsoft.Win32.OpenFolderDialog` 是 .NET 8 新增的 WPF 专用 API，**仅限 Windows**
- 在非 Windows 平台上调用会抛出 `PlatformNotSupportedException`
- 没有官方的 NuGet 包将这些 API 搬到 Linux/macOS

### 2.3 跨平台替代方案

#### Avalonia UI 方案

Avalonia 提供 `IStorageProvider` 接口，通过 `TopLevel` 级别的 `StorageProvider` 属性访问：

```csharp
// 获取 StorageProvider
var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

// 打开文件对话框
var files = await storageProvider!.OpenFilePickerAsync(new FilePickerOpenOptions
{
    Title = "选择Word模板文件",
    AllowMultiple = false,
    FileTypeFilter = new[]
    {
        new FilePickerFileType("Word文档")
        {
            Patterns = new[] { "*.docx" }
        }
    }
});

// 打开文件夹对话框
var folders = await storageProvider!.OpenFolderPickerAsync(new FolderPickerOpenOptions
{
    Title = "选择输出目录",
    AllowMultiple = false
});
```

**平台行为**：
- **Windows**: 调用 Windows Shell 对话框 (IFileDialog)
- **macOS**: 调用 NSSavePanel / NSOpenPanel
- **Linux**: 调用 Zenity/KDialog/FreeDesktop 文件对话框协议

**优势**：API 统一、异步优先、原生外观。

#### Tauri 方案

```javascript
// 打开文件
const selected = await dialog.open({
  multiple: false,
  filters: [{ name: 'Word文档', extensions: ['docx'] }]
});

// 打开文件夹
const folder = await dialog.open({ directory: true });
```

**平台行为**：Rust 端调用系统原生对话框 API。

#### Electron 方案

```javascript
// 打开文件
const result = await dialog.showOpenDialog({
  properties: ['openFile'],
  filters: [{ name: 'Word文档', extensions: ['docx'] }]
});

// 打开文件夹
const result = await dialog.showOpenDialog({
  properties: ['openDirectory']
});
```

### 2.4 迁移难度评估

| 方面 | 难度 | 说明 |
|------|------|------|
| API 替换 | ★★☆☆☆ | 文件对话框是标准 UI 功能，所有框架都有成熟支持 |
| 同步→异步 | ★★★☆☆ | WPF 的 `ShowDialog()` 是同步的，Avalonia 是异步的，需要 `await` |
| 过滤器映射 | ★★☆☆☆ | 过滤器语法略有不同，但模式一致 |
| UI 测试 | ★★☆☆☆ | 对话框是模态的，自动化测试需要 mock |

---

## 3. 拖放功能

### 3.1 现状分析

DocuFiller 实现了完整的 WPF 拖放行为系统 (`Behaviors/FileDragDrop.cs`)：

**核心特征**：
- 使用 `DependencyProperty` 注册附加属性（`IsEnabled`、`Filter`、`DropCommand`）
- 区分 `TextBox` 目标（Preview 隧道事件）和 `Border` 目标（冒泡事件）
- 使用 `DataFormats.FileDrop` 获取拖放的文件路径
- 实现视觉反馈（高亮边框、背景色变化）
- 文件类型验证（`.docx`、`.xlsx`、文件夹）

**使用位置**：
| XAML | 目标控件 | 过滤器 | 用途 |
|------|---------|--------|------|
| MainWindow.xaml | TextBox | `DocxOrFolder` | 模板文件拖放 |
| MainWindow.xaml | TextBox | `ExcelFile` | 数据文件拖放 |
| CleanupWindow.xaml | Border | `DocxFile` | 清理文件拖放 |

### 3.2 WPF DragDrop 的跨平台限制

- `System.Windows.DragDrop` 是 **WPF 专有**
- `DragEventArgs`、`DragDropEffects`、`DataFormats` 均在 `PresentationCore.dll` 中
- WPF 的附加属性 (`DependencyProperty`) 机制在非 WPF 框架中不存在

### 3.3 跨平台替代方案

#### Avalonia UI 方案

Avalonia 内置完整的拖放支持，API 设计与 WPF 高度相似：

```csharp
// 启用拖放（XAML）
<Border AllowDrop="True" DragDrop.Drop="OnDrop" />

// 或代码方式
DragDrop.SetAllowDrop(element, true);

// 处理 Drop 事件
private async void OnDrop(object sender, DragEventArgs e)
{
    if (e.Data.Contains(DataFormats.Files))
    {
        var files = await e.Data.GetFilesAsync();
        // files 是 IStorageItem[]，需要转换为路径
        var paths = files?.Select(f => f.Path.LocalPath).ToArray();
    }
}
```

**关键差异**：
| 方面 | WPF | Avalonia |
|------|-----|----------|
| 事件模型 | DragEnter/DragOver/DragDrop | 相同事件名 |
| 数据格式 | `DataFormats.FileDrop` 返回 `string[]` | `DataFormats.Files` 返回 `IStorageItem[]` |
| 附加属性 | `DependencyProperty.RegisterAttached` | Avalonia `StyledProperty` / `DirectProperty` |
| DragEventArgs | `e.Data.GetData(DataFormats.FileDrop)` | `await e.Data.GetFilesAsync()` |
| 视觉反馈 | 直接修改控件属性 | 相同，但推荐使用 Styles/Classes 切换 |

**迁移路径**：
1. 将 `FileDragDrop` Behavior 的 `DependencyProperty` 替换为 Avalonia `StyledProperty`
2. `DataFormats.FileDrop` → `DataFormats.Files`
3. `e.Data.GetData()` → `await e.Data.GetFilesAsync()`
4. `string[]` → `IStorageItem[].Select(f => f.Path.LocalPath).ToArray()`
5. 视觉反馈逻辑基本不变

#### Tauri / Electron 方案

Web 标准拖放 API：

```javascript
// HTML
<div ondragover="event.preventDefault()" ondrop="handleDrop(event)"></div>

// JavaScript
function handleDrop(event) {
  event.preventDefault();
  const files = event.dataTransfer.files;
  // files 是 FileList，Tauri 可通过 path 扩展获取完整路径
  // Electron: file.path 属性直接可用
}
```

**注意**：浏览器安全模型限制了对文件系统路径的访问。Tauri 通过 `convertFileSrc` 和 `path` 扩展解决了这个问题；Electron 的 `File.path` 属性也提供完整路径。

### 3.4 迁移难度评估

| 方面 | 难度 | 说明 |
|------|------|------|
| 事件模型 | ★★☆☆☆ (Avalonia) | 高度相似，概念映射直接 |
| 数据格式 | ★★★☆☆ | `string[]` → `IStorageItem[]` 需要适配层 |
| 附加属性 | ★★★☆☆ | DependencyProperty → StyledProperty 模式转换 |
| 视觉反馈 | ★☆☆☆☆ | 逻辑完全可复用 |
| 异步处理 | ★★☆☆☆ | Avalonia 拖放数据获取是异步的 |

---

## 4. 路径处理

### 4.1 现状分析

DocuFiller 中的路径处理主要使用 `System.IO.Path` 和 `Environment.SpecialFolder`：

**涉及代码**：

| 文件 | 代码 | 风险级别 |
|------|------|---------|
| `ViewModels/FillViewModel.cs:86` | `Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)` | ✅ 安全 |
| `DocuFiller/ViewModels/CleanupViewModel.cs:30` | `Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)` | ✅ 安全 |
| `Utils/GlobalExceptionHandler.cs:18` | `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` | ✅ 安全 |
| `Services/UpdateService.cs:112` | `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)` | ✅ 安全 |
| `Utils/VersionHelper.cs:18` | `Environment.ProcessPath` | ✅ 安全 |
| 多处 | `Path.Combine()`、`Path.GetExtension()`、`Path.GetFileName()` | ✅ 安全 |
| 多处 | `Path.GetDirectoryName()`、`Path.GetFileNameWithoutExtension()` | ✅ 安全 |

### 4.2 `System.IO.Path` 跨平台行为

.NET 的 `System.IO.Path` 类 **天然跨平台**，核心方法行为：

| 方法 | 跨平台行为 | 说明 |
|------|-----------|------|
| `Path.Combine(a, b)` | ✅ 安全 | 使用当前平台的分隔符 |
| `Path.GetExtension(path)` | ✅ 安全 | 基于 `.` 字符判断，与平台无关 |
| `Path.GetFileName(path)` | ✅ 安全 | 自动识别 `/` 和 `\` |
| `Path.GetDirectoryName(path)` | ⚠️ 注意 | 返回的平台路径使用当前 OS 的分隔符 |
| `Path.DirectorySeparatorChar` | `\` on Windows, `/` on Linux/macOS | |
| `Path.AltDirectorySeparatorChar` | `/` on Windows, `\` on Linux/macOS | |

**关键规则**：
- ✅ **永远使用 `Path.Combine()` 而不是字符串拼接** — DocuFiller 已遵循此规则
- ✅ **不要硬编码路径分隔符** — 已遵循
- ⚠️ **解析来自外部源的路径时要小心** — 如果路径来自配置文件或用户输入，可能包含不同平台的分隔符

### 4.3 `Environment.SpecialFolder` 跨平台行为

| SpecialFolder | Windows | macOS | Linux |
|--------------|---------|-------|-------|
| `MyDocuments` | `C:\Users\{user}\Documents` | `/Users/{user}/Documents` | `/home/{user}/Documents` (若不存在则 fallback 到 `$HOME`) |
| `LocalApplicationData` | `C:\Users\{user}\AppData\Local` | `/Users/{user}/Library/Application Support` | `$HOME/.local/share` |
| `UserProfile` | `C:\Users\{user}` | `/Users/{user}` | `$HOME` |
| `Desktop` | `C:\Users\{user}\Desktop` | `/Users/{user}/Desktop` | `$HOME/Desktop` |
| `ApplicationData` | `C:\Users\{user}\AppData\Roaming` | `/Users/{user}/Library/Application Support` | `$HOME/.config` |

**DocuFiller 使用的 SpecialFolder 均已由 .NET Runtime 跨平台实现**，无需额外处理。

### 4.4 路径迁移检查清单

- [x] `Path.Combine()` — 已跨平台
- [x] `Path.GetExtension()` — 已跨平台
- [x] `Path.GetFileName()` — 已跨平台
- [x] `Environment.SpecialFolder.MyDocuments` — 已跨平台
- [x] `Environment.SpecialFolder.LocalApplicationData` — 已跨平台
- [x] `Environment.SpecialFolder.UserProfile` — 已跨平台
- [x] `Environment.ProcessPath` — 已跨平台
- [ ] 检查配置文件中的路径值（如 `update-config.json` 中的路径）— 需要在运行时动态解析
- [ ] 检查日志文件路径 — `GlobalExceptionHandler.LogDirectory` 使用 `LocalApplicationData`，已安全

### 4.5 迁移难度评估

| 方面 | 难度 | 说明 |
|------|------|------|
| Path API | ★☆☆☆☆ | `System.IO.Path` 天然跨平台，无需修改 |
| SpecialFolder | ★☆☆☆☆ | .NET Runtime 已映射各平台等效路径 |
| 配置文件中的路径 | ★★☆☆☆ | `update-config.json` 中的 URL 路径无需调整（是 URL 非文件路径） |

---

## 5. 文件系统权限与目录规范

### 5.1 概述

DocuFiller 作为桌面应用，主要进行以下文件系统操作：
1. 读取 .docx/.xlsx 文件（用户数据）
2. 写入输出 .docx 文件到用户指定目录
3. 创建日志文件（`%LOCALAPPDATA%/DocuFiller/Logs`）
4. 存储配置（`%USERPROFILE%/.docx_replacer/update-config.json`）
5. 写入 Velopack 更新缓存

这些操作在各平台的权限模型下表现不同。

### 5.2 Windows vs macOS vs Linux 权限模型

| 方面 | Windows | macOS | Linux |
|------|---------|-------|-------|
| **权限模型** | NTFS ACL (Read/Write/Execute 权限) | POSIX + ACL (rwx + 扩展属性) | POSIX (rwx) |
| **应用数据目录** | `%LOCALAPPDATA%\App\` | `~/Library/Application Support/App/` | `~/.local/share/App/` |
| **配置目录** | `%APPDATA%\App\` | `~/Library/Preferences/` 或同上 | `~/.config/App/` |
| **日志目录** | `%LOCALAPPDATA%\App\Logs\` | `~/Library/Logs/App/` | `~/.local/state/App/` |
| **缓存目录** | `%LOCALAPPDATA%\App\Cache\` | `~/Library/Caches/App/` | `~/.cache/App/` |
| **安装位置** | `Program Files\` 或用户目录 | `/Applications/` 或 `~/Applications/` | `/opt/` 或 `/usr/local/` 或用户目录 |

### 5.3 XDG Base Directory 规范 (Linux)

Linux 桌面应用应遵循 [XDG Base Directory Specification](https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html)：

| 变量 | 默认值 | 用途 |
|------|--------|------|
| `XDG_DATA_HOME` | `~/.local/share` | 应用数据 |
| `XDG_CONFIG_HOME` | `~/.config` | 配置文件 |
| `XDG_CACHE_HOME` | `~/.cache` | 缓存数据 |
| `XDG_STATE_HOME` | `~/.local/state` | 日志等状态数据 |

**.NET 映射关系**：
- `Environment.SpecialFolder.LocalApplicationData` → 遵循 XDG (`.NET Runtime` 自动处理)
- `Environment.SpecialFolder.ApplicationData` → 遵循 XDG_CONFIG_HOME
- 无直接映射的用 `Environment.GetEnvironmentVariable("XDG_...")` 获取

### 5.4 macOS 特有规范

- **代码签名**：所有 `.app` 必须经过代码签名才能在 macOS Gatekeeper 下运行
- **公证 (Notarization)**：分发到外部用户需要 Apple 公证（需 Apple Developer 账号，$99/年）
- **沙盒限制**：如上架 Mac App Store，需遵循沙盒限制（文件访问需通过安全书签）
- **App Bundle 结构**：`.app` 必须遵循特定目录结构：
  ```
  DocuFiller.app/
  ├── Contents/
  │   ├── Info.plist
  │   ├── MacOS/
  │   │   └── DocuFiller      (可执行文件)
  │   └── Resources/
  │       └── app.icns
  ```

### 5.5 DocuFiller 当前目录使用

| 用途 | 当前路径 | 跨平台等效 |
|------|---------|-----------|
| 默认输出目录 | `MyDocuments/DocuFiller输出` | ✅ `SpecialFolder.MyDocuments` 已跨平台映射 |
| 日志目录 | `LocalApplicationData/DocuFiller/Logs` | ✅ 自动映射，但 Linux 最佳实践是 `$XDG_STATE_HOME` |
| 更新配置 | `UserProfile/.docx_replacer/update-config.json` | ⚠️ 使用点前缀，Unix 惯例可接受但不如 XDG 规范 |
| 应用配置 | `appsettings.json`（应用目录旁） | ✅ 相对路径，各平台均可用 |

### 5.6 文件系统权限注意事项

| 场景 | 风险 | 处理方式 |
|------|------|---------|
| 写入用户文档目录 | ★☆☆☆☆ | 各平台用户对自己的 Documents 目录有完全权限 |
| 写入应用数据目录 | ★☆☆☆☆ | `SpecialFolder.LocalApplicationData` 映射的目录用户有权限 |
| 可执行权限 (Linux/macOS) | ★★☆☆☆ | Linux/macOS 需要文件有 `+x` 权限才能执行。Velopack 已处理此问题 |
| 只读文件系统 | ★★☆☆☆ | 应捕获 `UnauthorizedAccessException` 并提示用户 |
| macOS SIP 保护 | ★☆☆☆☆ | DocuFiller 不涉及系统目录，不受影响 |

### 5.7 迁移难度评估

| 方面 | 难度 | 说明 |
|------|------|------|
| 目录规范 | ★☆☆☆☆ | `Environment.SpecialFolder` 已自动处理各平台映射 |
| 文件权限 | ★★☆☆☆ | DocuFiller 只访问用户目录，权限问题较少 |
| macOS App Bundle | ★★★★☆ | 如选择上架 App Store 或支持公证，需要额外的打包工作 |
| Linux 打包格式 | ★★★☆☆ | deb/rpm/AppImage 各有不同的目录布局约定 |

---

## 6. 注册表使用

### 6.1 现状分析

经过源码全量扫描：

```
grep -rn "Registry\|RegistryKey\|CurrentUser\|LocalMachine" --include="*.cs"
```

**结果：DocuFiller 不使用 Windows 注册表。**

DocuFiller 的配置存储方式：
- 应用配置：`appsettings.json`（JSON 文件，跨平台友好）
- 更新配置：`update-config.json`（JSON 文件，跨平台友好）
- CLI 配置：无注册表依赖

### 6.2 遗留配置系统

DocuFiller 仍有一处 `System.Configuration.ConfigurationManager` 使用：

```csharp
// App.xaml.cs:256
var value = System.Configuration.ConfigurationManager.AppSettings[key];
```

这是 .NET Framework 时代的遗留配置系统，依赖 `App.config` / `Web.config` XML 文件。在跨平台场景下：
- `System.Configuration.ConfigurationManager` NuGet 包可以在 net8.0 上运行（不依赖 Windows）
- 但它是遗留技术，T02 调研已建议迁移到 `Microsoft.Extensions.Configuration`
- DocuFiller 已在使用 `Microsoft.Extensions.Configuration`（`appsettings.json`），仅需清理遗留引用

### 6.3 迁移影响

| 方面 | 难度 | 说明 |
|------|------|------|
| 注册表 | ★☆☆☆☆ (无) | 不使用注册表，无需迁移 |
| ConfigurationManager | ★★☆☆☆ | 仅一处调用，已有替代方案 |

---

## 7. 进程管理

### 7.1 现状分析

DocuFiller 使用 `System.Diagnostics.Process.Start()` 的场景：

| 文件 | 代码 | 用途 |
|------|------|------|
| `CleanupViewModel.cs:178` | `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })` | 在文件管理器中打开输出文件夹 |
| `Velopack` (内部) | `Process.Start()` | 下载更新后启动新版本 |

### 7.2 `Process.Start()` with `UseShellExecute = true` 跨平台行为

`UseShellExecute = true` 的含义是"使用操作系统 shell 打开文件/URL"。

| 平台 | 行为 | 命令 |
|------|------|------|
| **Windows** | `explorer.exe {path}` | 打开文件夹或文件 |
| **macOS** | `open {path}` | 使用默认应用打开 |
| **Linux** | `xdg-open {path}` | 使用默认应用打开 |

**.NET 8 行为**：在 .NET 8 中，`UseShellExecute = true` **在各平台上均可用**，Runtime 内部调用对应的系统命令。无需额外处理。

### 7.3 Velopack 进程管理

Velopack 内部使用 `Process.Start()` 进行：
- 更新后重启应用
- 运行安装/卸载脚本

这些操作由 Velopack 自身处理，已支持跨平台（详见 T01 调研报告）。

### 7.4 其他进程管理注意事项

| 场景 | 说明 |
|------|------|
| 单实例检测 | Windows 常用 `Mutex` 实现；Linux 使用 PID 文件（`/tmp/AppName.lock`），macOS 两者皆可 |
| 命令行参数 | `Environment.GetCommandLineArgs()` 跨平台可用 |
| 退出码 | `Environment.Exit(code)` 跨平台可用 |
| 进程信号 | Linux/macOS 支持 SIGTERM/SIGINT；Windows 使用 Ctrl+C 事件。.NET `Process.WaitForExit` 已统一 |

### 7.5 迁移难度评估

| 方面 | 难度 | 说明 |
|------|------|------|
| 打开文件夹 | ★☆☆☆☆ | `UseShellExecute = true` 已跨平台 |
| 更新重启 | ★☆☆☆☆ | Velopack 已处理 |
| 单实例检测 | ★★☆☆☆ | 需要平台条件编译或跨平台方案 |

---

## 8. 其他平台差异

### 8.1 线程模型

DocuFiller 使用 `Dispatcher.Invoke` 进行 UI 线程调度（见 `DownloadProgressWindow` 的 `CloseCallback`）：

```csharp
Dispatcher.Invoke(() => {
    Window.DialogResult = true;
    Window.Close();
});
```

**跨平台差异**：
| 框架 | UI 线程调度 |
|------|-----------|
| WPF | `Dispatcher.Invoke()` |
| Avalonia | `Dispatcher.UIThread.InvokeAsync()` |
| Tauri/Electron | Web 模型，主线程即 UI 线程，使用 `setTimeout`/`requestAnimationFrame` |

### 8.2 窗口管理

DocuFiller 有多个 WPF 窗口：
- `MainWindow` — 主窗口
- `CleanupWindow` — 清理功能窗口
- `DownloadProgressWindow` — 更新进度窗口
- `UpdateSettingsWindow` — 更新设置窗口

窗口管理在各框架中都有对应方案：
- **Avalonia**: `Window` 类，概念映射 1:1
- **Tauri/Electron**: 多窗口 API，但编程模型差异较大

### 8.3 图标与资源

| 资源 | Windows | macOS | Linux |
|------|---------|-------|-------|
| 应用图标 | `.ico` 文件 | `.icns` 文件 (App Bundle 内) | `.png` 文件 (desktop entry) |
| 资源嵌入 | WPF `Resource` / `pack://` | App Bundle `Resources/` | 安装目录相对路径 |
| 字体 | 系统字体 | 系统字体 | 系统字体 (可能缺少中文字体) |

**注意**：Linux 发行版可能缺少中文字体。DocuFiller 如使用内嵌字体或依赖特定字体渲染中文，需要在 Linux 上确保字体可用。

### 8.4 文件关联与协议注册

DocuFiller 当前没有注册文件关联（`.docx` 双击打开）或自定义 URI 协议。如需添加：
- **Windows**: 注册表写入
- **macOS**: `Info.plist` 中的 `CFBundleDocumentTypes`
- **Linux**: `.desktop` 文件中的 `MimeType` 字段

### 8.5 系统通知

DocuFiller 当前未使用系统通知功能。如需添加：
- **Windows**: `ToastNotification` (WinRT API)
- **macOS**: `NSUserNotificationCenter` 或 UserNotifications 框架
- **Linux**: `libnotify` / D-Bus `org.freedesktop.Notifications`
- **跨平台**: Avalonia 有社区插件 `Avalonia.Controls.Notifications`

### 8.6 文件系统监听

DocuFiller 当前未使用 `FileSystemWatcher`。如需添加，需注意：
- Linux/macOS 使用 `inotify`/`FSEvents`/`kqueue`
- .NET 的 `FileSystemWatcher` 已跨平台实现
- Linux 的 `inotify` 有 watch 数量限制（可通过 `fs.inotify.max_user_watches` 调整）

---

## 9. DocuFiller 代码中的平台特定点清单

### 9.1 必须修改（编译不通过或运行时崩溃）

| 序号 | 文件 | 代码 | 问题 | 替代方案 |
|------|------|------|------|---------|
| 1 | `ViewModels/FillViewModel.cs` | `using Microsoft.Win32;` | Windows 专有命名空间 | 框架对应 API |
| 2 | `ViewModels/FillViewModel.cs` | `new OpenFileDialog {...}` × 2 | WPF 文件对话框 | 框架对应 API |
| 3 | `ViewModels/FillViewModel.cs` | `new OpenFolderDialog {...}` × 2 | WPF 文件夹对话框 | 框架对应 API |
| 4 | `CleanupViewModel.cs` | `using Microsoft.Win32;` | Windows 专有命名空间 | 框架对应 API |
| 5 | `CleanupViewModel.cs` | `new OpenFolderDialog {...}` | WPF 文件夹对话框 | 框架对应 API |
| 6 | `Behaviors/FileDragDrop.cs` | 全文（WPF DependencyProperty + DragDrop） | WPF 拖放系统 | 框架对应拖放 API |
| 7 | `MainWindow.xaml` / `CleanupWindow.xaml` | `FileDragDrop.IsEnabled` 等附加属性 | WPF 行为绑定 | 框架对应行为模式 |
| 8 | `App.xaml.cs` | `System.Configuration.ConfigurationManager` | 遗留配置系统 | `IConfiguration` |
| 9 | `DocuFiller.csproj` | `<TargetFramework>net8.0-windows</TargetFramework>` | Windows 专用 TFM | `net8.0` |
| 10 | `DocuFiller.csproj` | `<UseWPF>true</UseWPF>` | WPF 框架引用 | 新 UI 框架引用 |

### 9.2 建议修改（功能正确性）

| 序号 | 文件 | 代码 | 问题 | 建议 |
|------|------|------|------|------|
| 11 | `CleanupViewModel.cs` | `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })` | 已跨平台可用 | 无需修改 |
| 12 | `Utils/GlobalExceptionHandler.cs` | `SpecialFolder.LocalApplicationData` + `"DocuFiller/Logs"` | Linux 规范推荐 `$XDG_STATE_HOME` | 可选优化 |
| 13 | `Services/UpdateService.cs` | `SpecialFolder.UserProfile` + `".docx_replacer/"` | Linux 可考虑 `$XDG_CONFIG_HOME` | 可选优化 |

### 9.3 无需修改

| 代码 | 说明 |
|------|------|
| `Path.Combine()` / `Path.GetExtension()` 等 | `System.IO.Path` 天然跨平台 |
| `Environment.SpecialFolder.*` | .NET Runtime 自动映射各平台路径 |
| `File.Exists()` / `Directory.Exists()` / `File.Copy()` | `System.IO` 已跨平台 |
| `Environment.ProcessPath` | .NET 6+ 跨平台可用 |
| `Process.Start` with `UseShellExecute` | .NET 8 已跨平台处理 |
| `DocumentFormat.OpenXml` / `EPPlus` 操作 | 纯托管代码，无平台依赖 |

---

## 10. 迁移策略建议

### 10.1 抽象层设计

推荐引入平台抽象接口，将平台相关操作封装：

```csharp
public interface IFileDialogService
{
    Task<string?> OpenFileAsync(string title, string filter);
    Task<string?> OpenFolderAsync(string title);
}

public interface IDragDropService
{
    // 框架特定的拖放支持
}

public interface IPlatformService
{
    string GetDefaultOutputDirectory();
    string GetLogDirectory();
    string GetConfigDirectory();
    void OpenInFileManager(string path);
}
```

**好处**：
- 业务逻辑与平台代码解耦
- 可通过 DI 注入不同平台的实现
- 方便单元测试（mock 接口）
- 支持渐进式迁移

### 10.2 条件编译策略

对于少量平台差异代码，可使用条件编译：

```csharp
#if WINDOWS
    // Windows 特有代码
#elif MACOS
    // macOS 特有代码
#elif LINUX
    // Linux 特有代码
#endif
```

但推荐优先使用运行时判断，避免编译时分裂：

```csharp
if (OperatingSystem.IsWindows()) { ... }
else if (OperatingSystem.IsMacOS()) { ... }
else if (OperatingSystem.IsLinux()) { ... }
```

### 10.3 迁移优先级

| 优先级 | 差异点 | 原因 |
|--------|--------|------|
| P0 | 目标框架从 `net8.0-windows` 改为 `net8.0` | 前置条件 |
| P0 | UI 框架选择和整体迁移 | 所有的平台特定 API 都随 UI 框架改变 |
| P1 | 文件对话框替换 | 核心用户交互入口 |
| P1 | 拖放功能替换 | 核心用户交互入口 |
| P2 | 遗留 `ConfigurationManager` 清理 | 代码卫生 |
| P3 | 目录规范优化 (XDG) | Linux 最佳实践 |

---

## 11. 优缺点总结

### 11.1 跨平台迁移的有利因素

✅ **DocuFiller 不使用 Windows 注册表** — 消除了一个常见的迁移障碍  
✅ **核心业务逻辑（OpenXml + EPPlus）为纯托管代码** — 无平台依赖  
✅ **路径处理已使用 `System.IO.Path`** — 无硬编码分隔符  
✅ **目录使用 `Environment.SpecialFolder`** — .NET Runtime 自动映射  
✅ **配置存储为 JSON 文件** — 无平台依赖  
✅ **`Process.Start + UseShellExecute` 已跨平台** — 无需修改  

### 11.2 主要挑战

⚠️ **WPF UI 层完全需要重写** — 文件对话框、拖放、窗口管理、XAML 全部需要替换  
⚠️ **FileDragDrop Behavior 是 WPF DependencyProperty** — 需要完全重写为新框架的行为模式  
⚠️ **macOS 代码签名和公证** — 额外的成本和流程（$99/年 + Notarization 流程）  
⚠️ **Linux 打包格式多样性** — deb/rpm/AppImage/Flatpak 各有要求  
⚠️ **中文字体渲染** — Linux 可能缺少合适的中文字体  
⚠️ **文件对话框同步→异步** — API 模式变化需要调用方适配  

### 11.3 风险矩阵

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|---------|
| WPF 行为无法 1:1 复制 | 高 | 中 | Avalonia 最为接近，行为映射率高 |
| Linux 文件对话框外观差异 | 中 | 低 | 使用 Zenity/GTK 对话框已足够 |
| macOS 用户拒绝未签名应用 | 高 | 高 | 必须取得代码签名和公证 |
| 路径处理遗漏 | 低 | 中 | 已全面审查，`System.IO.Path` 跨平台 |
| 文件权限导致写入失败 | 低 | 低 | DocuFiller 只操作用户目录 |

---

## 12. 调研日期与信息来源

### 12.1 调研信息

- **调研完成日期**: 2026-05
- **调研者**: GSD auto-mode executor
- **基于代码版本**: DocuFiller v1.10.1

### 12.2 信息来源

1. **DocuFiller 源码分析** — 全量扫描 `.cs` 和 `.xaml` 文件中的 Windows 专有 API
2. **.NET 跨平台文档** — [Microsoft Learn: .NET Cross-Platform](https://learn.microsoft.com/en-us/dotnet/core/)
3. **Avalonia UI 文档** — [docs.avaloniaui.net](https://docs.avaloniaui.net/) — StorageProvider API, DragDrop
4. **Tauri 对话框文档** — [tauri.app/v1/api/js/dialog](https://tauri.app/v1/api/js/dialog/)
5. **Electron dialog 文档** — [electronjs.org/docs/api/dialog](https://www.electronjs.org/docs/api/dialog)
6. **XDG Base Directory Specification** — [freedesktop.org](https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html)
7. **System.IO.Path 跨平台行为** — [Microsoft Learn: Path Class](https://learn.microsoft.com/en-us/dotnet/api/system.io.path)
8. **Velopack 跨平台能力调研** — 本项目 T01 调研报告
9. **核心依赖库兼容性调研** — 本项目 T02 调研报告
