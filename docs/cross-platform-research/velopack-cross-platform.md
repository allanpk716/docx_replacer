# Velopack 跨平台能力调研报告

> **调研日期**: 2026-05  
> **调研范围**: Velopack 作为 DocuFiller 跨平台统一更新框架的可行性评估  
> **基于**: Velopack 官方文档 (docs.velopack.io)、GitHub 仓库 (velopack/velopack)、NuGet、社区反馈  
> **版本**: 1.0

---

## 目录

1. [技术概述](#1-技术概述)
2. [Windows 支持现状](#2-windows-支持现状)
3. [macOS 支持](#3-macos-支持)
4. [Linux 支持](#4-linux-支持)
5. [vpk CLI 跨平台能力](#5-vpk-cli-跨平台能力)
6. [跨平台 Releases Feed 格式](#6-跨平台-releases-feed-格式)
7. [增量更新（Delta Updates）](#7-增量更新delta-updates)
8. [局限性与已知问题](#8-局限性与已知问题)
9. [与替代方案对比](#9-与替代方案对比)
10. [对 DocuFiller 的建议](#10-对-docufiller-的建议)
11. [优缺点总结](#11-优缺点总结)
12. [调研日期与信息来源](#12-调研日期与信息来源)

---

## 1. 技术概述

Velopack 是一个开源的、跨平台的桌面应用安装与自动更新框架，由 Caelan Sayler (caesay) 开发，使用 Rust 编写核心引擎。它是 Clowd.Squirrel（Squirrel.Windows 的 .NET 重写版）的继任者，旨在用一个统一的工具替代 Windows (Squirrel)、macOS (Sparkle)、Linux (各种自定义方案) 上各自不同的更新框架。

### 1.1 核心定位

| 特性 | 说明 |
|------|------|
| **零配置** | 一条 `vpk pack` 命令即可生成安装包、更新包、增量包和自更新便携包 |
| **跨平台** | 支持 Windows、macOS、Linux 三平台打包与分发 |
| **高性能** | Rust 编写，多线程打包，Zstandard 二进制差分算法 |
| **语言无关** | 提供 C# / .NET、Rust、C/C++、JavaScript (Electron)、Go、Java 等语言 SDK |
| **自动迁移** | 可从 Squirrel.Windows 等旧框架自动迁移 |

### 1.2 架构组件

Velopack 由三个核心组件组成：

| 组件 | 职责 |
|------|------|
| **Velopack SDK** (NuGet / crate / lib) | 集成到应用中，处理启动钩子、更新检查、下载和应用 |
| **vpk CLI 工具** | 命令行打包工具，生成 `.nupkg` 更新包、安装程序、增量包 |
| **Update binary** | 由 vpk 打包到应用中，独立处理更新应用过程（重启、替换文件） |

### 1.3 版本信息

| 项目 | 数据 |
|------|------|
| **GitHub 仓库** | [velopack/velopack](https://github.com/velopack/velopack) |
| **开源协议** | MIT |
| **Rust crate 最新版** | 0.0.1589-ga2c5a97 (2026-04-14) |
| **NuGet 最新稳定版** | 0.0.1298 (DocuFiller 当前使用版本) |
| **NuGet 最新预览版** | 0.0.1369-g1d5c984 (2025-09-24) |
| **NuGet 总下载量** | 数十万级（持续增长） |
| **Rust crate 月下载** | ~1,436 |
| **总提交数** | ~3,943 (develop 分支) |
| **主要作者** | Caelan Sayler (caesay)，同时也是 Clowd.Squirrel 作者 |

### 1.4 与 Squirrel / Clowd.Squirrel 的关系

Velopack 是 Clowd.Squirrel 的精神继承者和实质性替代品。Caelan Sayler 在维护 Clowd.Squirrel 数年后，基于实际使用经验用 Rust 从头重写了整个框架。关键改进包括：

- **原生性能**：从 .NET 迁移到 Rust，打包速度显著提升
- **真正的跨平台**：Clowd.Squirrel 仅支持 Windows，Velopack 扩展到 macOS 和 Linux
- **增量更新**：内置 Zstandard 二进制差分，无需额外工具
- **简化 API**：去除了 Squirrel 复杂的事件驱动模型，改为简洁的 check → download → apply 流程

> 来源: [Velopack GitHub](https://github.com/velopack/velopack)、[Velopack 官网](https://velopack.io/)

---

## 2. Windows 支持现状

Windows 是 Velopack 最成熟、最完善的平台。DocuFiller 当前完全基于 Velopack 的 Windows 能力运行。

### 2.1 安装机制

Velopack 在 Windows 上创建一个文件夹结构，类似于 Squirrel.Windows：

```
%LocalAppData%\
└── {packId}
    ├── current\
    │   ├── YourApp.exe
    │   ├── YourApp.dll
    │   └── sq.version
    └── Update.exe
```

安装程序为 `Setup.exe`（基于 WiX/MSI 的自定义引导程序），支持自定义标题、图标、启动画面等。同时生成 `Portable.zip` 便携版本。

### 2.2 DocuFiller 的当前使用方式

DocuFiller 当前使用以下 Velopack 模式（来自项目记忆和代码）：

| 模式 | 实现方式 |
|------|---------|
| **启动初始化** | `VelopackApp.Build().Run()` 作为 `Program.Main()` 第一行 |
| **更新管理** | 每次 API 调用创建独立 `UpdateManager` 实例 |
| **应用更新** | `UpdateManager.ApplyUpdatesAndRestart()` 传入 `VelopackAsset` 参数 |
| **通道策略** | 不设置 `ExplicitChannel` 和 `AllowVersionDowngrade`，使用 OS 默认通道 |
| **更新源** | `SimpleWebSource`（内网）和 `GithubSource`（外网）双源模式 |
| **UI 策略** | 自定义 WPF 弹窗替代 Velopack 内置对话框 |
| **NuGet 版本** | Velopack 0.0.1298（稳定版） |
| **打包工具** | Velopack.Build 0.0.1298 |

### 2.3 Windows 更新流程

1. `UpdateManager.CheckForUpdatesAsync()` → 查询 `releases.win.json`
2. 如有更新，`UpdateManager.DownloadUpdatesAsync()` → 下载 full 或 delta `.nupkg`
3. `UpdateManager.ApplyUpdatesAndRestart()` → 替换 `current` 目录，重启应用

如果 `current` 目录被锁定（进程占用、杀毒软件等），Velopack 会尝试：
1. 自动杀死占用进程
2. 搜索系统中可能锁定文件夹的其他进程，弹窗提示用户
3. 如锁定进程无法识别（如以管理员运行），报错退出

### 2.4 代码签名（Windows）

| 证书类型 | SmartScreen 行为 |
|----------|-----------------|
| 无签名 | 每次发布新版本都会触发 SmartScreen 警告 |
| OV 证书 | 证书积累声誉后，新版本不再警告 |
| EV 证书 | 永不触发警告 |

推荐使用 **Azure Artifact Signing**（$10/月），支持 CI 自动化签名且内置即时声誉。

> 来源: [Windows Overview](https://docs.velopack.io/packaging/operating-systems/windows)、[Code Signing](https://docs.velopack.io/packaging/signing)

---

## 3. macOS 支持

Velopack 对 macOS 提供了基本的打包和更新支持，但成熟度明显低于 Windows。

### 3.1 打包格式

| 格式 | 说明 |
|------|------|
| **`.app` bundle** | 标准 macOS 应用包，可手动创建或由 Velopack 自动生成 |
| **`.pkg` installer** | Velopack 自动生成并签名的安装包，支持 README / License / Conclusion 页面 |
| **`.zip` portable** | 便携版本，在 Finder 中点击自动解压 |
| **DMG** | 需手动将 `.zip` 解压后创建 DMG（Velopack 不直接生成） |

Velopack 可以自动创建 `.app` bundle，只需提供 `--icon` 参数（必须为 `.icns` 格式）。也可以提供自定义的 `.app` 目录，Velopack 会直接使用。

### 3.2 代码签名与公证（Notarization）

**Apple 强制要求**：未经代码签名和公证的应用在 macOS 上无法运行。Velopack 在打包过程中内置了签名支持，需要在 macOS 上使用 `codesign`、`xcrun`、`productbuild` 等工具。

签名流程：
1. 使用 `codesign` 对 `.app` bundle 内的所有二进制文件签名
2. 使用 `productbuild` 创建 `.pkg` 安装包
3. 使用 `xcrun notarytool` 提交 Apple 公证
4. 对 `.pkg` 进行 Staple（附加公证票据）

这意味着 **macOS 打包必须在 macOS 上进行**（或通过远程 macOS CI runner）。

### 3.3 更新机制

- 更新包下载到 `/tmp`
- 更新时替换整个 `.app` bundle
- 如果安装在 `/Applications`（而非 `~/Applications`），需要提权
- 提权方式：通过 AppleScript 请求管理员权限
- `.app` bundle 是便携的，即使被用户移动到其他位置仍可更新

### 3.4 与 Sparkle 的关系

Velopack **不兼容** Sparkle 协议（appcast.xml）。Velopack 使用自己的 `releases.{channel}.json` 格式。如果 DocuFiller 未来迁移到 Velopack，不能复用 Sparkle 的更新基础设施。

Velopack 在 macOS 上的更新能力类似于 Sparkle 的核心功能（检查更新 → 下载 → 替换 .app bundle → 重启），但实现方式完全不同。Sparkle 使用 EdDSA 签名的 appcast.xml RSS 格式，Velopack 使用 JSON 格式的 releases feed。

### 3.5 成熟度评估

| 维度 | 评估 |
|------|------|
| 基本打包 | ✅ 可用，自动生成 .app 和 .pkg |
| 代码签名 | ✅ 支持，但需要 Apple Developer 账号和证书 |
| 公证 | ✅ 支持，通过 xcrun notarytool |
| DMG 分发 | ⚠️ 不直接支持，需额外工具 |
| Sparkle 兼容 | ❌ 不兼容 |
| 自动更新 | ✅ 可用，与 Windows API 一致 |
| 增量更新 | ✅ 支持（Zstandard delta） |
| 社区验证 | ⚠️ 社区反馈较少，案例不如 Windows 多 |

> 来源: [MacOS Overview](https://docs.velopack.io/packaging/operating-systems/macos)、[Code Signing](https://docs.velopack.io/packaging/signing)

---

## 4. Linux 支持

Velopack 在 Linux 上的支持最为有限，仅支持 AppImage 格式。

### 4.1 打包格式

Velopack 在 Linux 上**仅创建 `.AppImage` 文件**，不生成 deb、rpm 或其他原生包格式。

| 特性 | 说明 |
|------|------|
| **输出格式** | 仅 `.AppImage` |
| **图标要求** | PNG 格式（通过 `--icon` 参数） |
| **AppDir 支持** | 可手动创建 `.AppDir` 目录（需符合 AppImageKit 规范） |
| **自动生成** | vpk 可自动创建 AppImage/AppDir 结构 |

### 4.2 AppImage 特性

AppImage 是一种便携式应用格式，具有以下特点：

- **单文件分发**：一个 `.AppImage` 文件包含所有依赖
- **无需安装**：下载后 `chmod +x` 即可运行
- **跨发行版兼容**：理论上可在任何 Linux 发行版上运行
- **不修改系统**：不安装文件到系统目录，不注册包管理器
- **FUSE 依赖**：运行时需要 `libfuse2`（`sudo apt install libfuse2`）

### 4.3 更新机制

- 更新包下载到 `/var/tmp`
- 直接替换 `.AppImage` 文件
- 如果 `.AppImage` 在受保护目录，通过 `pkexec` 请求 sudo 权限
- 支持在应用运行时更新（Velopack 会尝试关闭/重启应用）

### 4.4 deb/rpm 支持现状

**Velopack 不支持 deb 和 rpm 包格式**。GitHub Issue #370 中有用户请求 deb/rpm 和 systemd 服务支持，但截至调研日期，该功能尚未实现。

| 格式 | 支持状态 |
|------|---------|
| AppImage | ✅ 原生支持 |
| deb | ❌ 不支持 |
| rpm | ❌ 不支持 |
| Snap | ❌ 不支持 |
| Flatpak | ❌ 不支持 |
| systemd 集成 | ⚠️ 有社区请求（Issue #370），未实现 |

### 4.5 已知问题

- **Issue #304**：Velopack 生成的 `.desktop` 文件缺少 `X-AppImage-Version` 字段，导致 AppImageLauncher 等工具无法正确显示版本号
- **FUSE 依赖**：部分现代 Linux 发行版（如 Ubuntu 24.04+）正在弃用 FUSE，可能影响 AppImage 运行
- **systemd 服务集成**：无法直接作为 systemd 服务运行（需要额外配置，Issue #370 中有用户反馈 ApplyUpdatesAndExit 与 systemd restart 冲突）

### 4.6 成熟度评估

| 维度 | 评估 |
|------|------|
| 基本打包 | ✅ 可用，自动生成 AppImage |
| 原生包格式 | ❌ 仅 AppImage，无 deb/rpm |
| 自动更新 | ✅ 可用，与 Windows API 一致 |
| 增量更新 | ✅ 支持（Zstandard delta） |
| 系统集成 | ⚠️ 弱，无桌面快捷方式自动创建 |
| 包管理器分发 | ❌ 不支持 |
| 社区验证 | ⚠️ 案例最少，存在已知问题 |

> 来源: [Linux Overview](https://docs.velopack.io/packaging/operating-systems/linux)、[GitHub Issue #370](https://github.com/velopack/velopack/issues/370)、[GitHub Issue #304](https://github.com/velopack/velopack/issues/304)

---

## 5. vpk CLI 跨平台能力

### 5.1 基本命令

`vpk` 是 Velopack 的命令行打包工具，通过 .NET 全局工具安装（`dotnet tool install -g vpk`），需要 .NET SDK 8。所有平台共享相同的命令结构，但各平台有特定的参数和输出格式。

### 5.2 各平台打包命令差异

```bash
# === Windows ===
vpk pack --packId MyApp --packVersion 1.0.0 \
  --packDir ./publish --mainExe MyApp.exe \
  --icon app.ico --packTitle "My Application"
# 输出: Setup.exe, Portable.zip, *.nupkg, releases.win.json

# === macOS ===
vpk pack --packId MyApp --packVersion 1.0.0 \
  --packDir ./publish --mainExe MyApp \
  --icon app.icns
# 输出: MyApp.pkg, MyApp.zip, *.nupkg, releases.osx.json

# === Linux ===
vpk pack --packId MyApp --packVersion 1.0.0 \
  --packDir ./publish --mainExe MyApp \
  --icon app.png
# 输出: MyApp.AppImage, *.nupkg, releases.linux.json
```

### 5.3 跨平台编译

Velopack 支持在任意平台上为目标平台打包（跨平台编译），但需要显式指定：

```bash
# 在 Windows 上为 Linux 打包
vpk [linux] pack --runtime linux-x64 --packId MyApp ...

# 在 macOS 上为 Windows 打包
vpk [win] pack --runtime win-x64 --packId MyApp ...
```

使用 `vpk [os] -h` 可查看特定平台的可用选项。部分平台特有的配置（如 macOS 的 `codesign`、`xcrun`、`productbuild`）仅在对应平台上可用。

### 5.4 各平台独有参数

| 参数 | Windows | macOS | Linux |
|------|---------|-------|-------|
| `--splashImage` | ✅ | ❌ | ❌ |
| `--packTitle` | ✅ | ❌ | ❌ |
| `--icon` (.ico) | ✅ (.ico) | ✅ (.icns) | ✅ (.png) |
| `--categories` | ❌ | ❌ | ✅ (XDG 桌面分类) |
| 代码签名参数 | ✅ (signtool) | ✅ (codesign) | ❌ |

### 5.5 CI/CD 集成

Velopack 的 `vpk` 工具完全支持在 CI/CD 流水线中使用：

```bash
# CI 中自动下载最新版本以生成 delta
vpk download -s "https://your-server.com/releases"

# 打包新版本
vpk pack --packId MyApp --packVersion 2.0.0 --packDir ./publish --mainExe MyApp

# 上传到服务器
vpk upload -s "https://your-server.com/releases" -p ./Releases
```

支持的上传目标：AWS S3、Azure Storage、BackBlaze B2、GitHub Releases、任意 HTTP 服务器。

> 来源: [Packaging Overview](https://docs.velopack.io/packaging/overview)、[Cross Compiling](https://docs.velopack.io/packaging/cross-compiling)、[vpk CLI Reference](https://docs.velopack.io/reference/cli/content/vpk-linux)

---

## 6. 跨平台 Releases Feed 格式

### 6.1 Feed 文件格式

Velopack 使用 JSON 格式的 releases feed 文件，替代了 Squirrel.Windows 的 `RELEASES` 文本格式：

```
Releases/
├── MyApp-1.0.0-full.nupkg       # 完整更新包
├── MyApp-1.0.0-delta.nupkg      # 增量更新包
├── MyApp-Setup.exe              # Windows 安装程序
├── MyApp.pkg                    # macOS 安装程序
├── MyApp.AppImage               # Linux AppImage
├── MyApp-Portable.zip           # Windows/macOS 便携包
├── releases.win.json            # Windows 更新 feed
├── releases.osx.json            # macOS 更新 feed
├── releases.linux.json          # Linux 更新 feed
├── assets.win.json              # Windows 资产清单
├── assets.osx.json              # macOS 资产清单
├── assets.linux.json            # Linux 资产清单
├── RELEASES                     # Windows 遗留格式
└── RELEASES-osx                 # macOS 遗留格式
```

### 6.2 通道（Channel）机制

每个发布必须属于一个**通道**（channel）。如果不指定 `--channel` 参数，默认通道名为操作系统名称：

| 平台 | 默认通道名 | Feed 文件 |
|------|-----------|----------|
| Windows | `win` | `releases.win.json` |
| macOS | `osx` | `releases.osx.json` |
| Linux | `linux` | `releases.linux.json` |

可以使用自定义通道（如 `stable`、`beta`、`dev`）：

```bash
vpk pack --packId MyApp --packVersion 1.0.0 --channel stable ...
# 输出: releases.stable.json
```

`UpdateManager` 默认不指定通道（传 `null`），它会自动查找当前安装版本所属的通道对应的 feed 文件。这意味着 **Windows 用户只看 `releases.win.json`，macOS 用户只看 `releases.osx.json`**，三平台天然隔离。

### 6.3 DocuFiller 的 Feed 架构

DocuFiller 当前使用双源模式：

| 源 | 用途 | Feed 路径 |
|----|------|----------|
| `SimpleWebSource` | 内网更新 | `http://内网服务器/releases` |
| `GithubSource` | 外网更新 | GitHub Releases |

跨平台迁移后，需要为每个平台在服务器上维护独立的 feed 目录或使用通道区分：

```
/releases/
├── win/
│   └── releases.win.json
├── osx/
│   └── releases.osx.json
└── linux/
    └── releases.linux.json
```

`UpdateManager` 的 `sourceUrl` 参数需要根据运行平台动态调整。由于 DocuFiller 不设置 `ExplicitChannel`，`UpdateManager` 会自动使用 OS 默认通道名，因此 `sourceUrl` 可以保持统一（如 `https://server.com/releases`），只要 feed 文件名匹配即可。

> 来源: [Release Channels](https://docs.velopack.io/packaging/channels)、[Distributing Overview](https://docs.velopack.io/distributing/overview)

---

## 7. 增量更新（Delta Updates）

### 7.1 技术实现

Velopack 的增量更新使用 **Zstandard (zstd)** 算法创建二进制补丁，对包中的每个文件生成独立的差分。

| 特性 | 说明 |
|------|------|
| **算法** | Zstandard (Facebook) |
| **粒度** | 文件级差分（非块级） |
| **限制** | 单个文件不超过 2 GB（Zstandard 限制） |
| **默认模式** | 平衡大小与速度 |
| **优化模式** | `--delta BestSize`（更小但更慢，约等于 bsdiff） |
| **禁用方式** | `--delta none` |

### 7.2 跨平台可用性

增量更新在**所有三个平台上均可用**。文档明确标注 "Applies to: Windows, MacOS, Linux"。

### 7.3 工作原理

1. `vpk pack` 时，如果输出目录中有前一版本，自动生成 delta `.nupkg`
2. CI 环境中可用 `vpk download` 先下载最新版本再打包
3. 客户端 `UpdateManager` 智能选择最优更新路径：
   - 如果当前版本是 1.0.0，目标是 1.0.3
   - 会下载 1.0.1-delta + 1.0.2-delta + 1.0.3-delta
   - 或直接下载 1.0.3-full（取决于启发式判断）
4. Delta 包在本地 `%LocalAppData\{packId}\packages` 中按顺序应用
5. 应用完成后执行更新

### 7.4 大小对比

| 应用类型 | 完整包大小 | Delta 大小（典型） | 压缩比 |
|----------|-----------|-------------------|--------|
| 小型应用 (~10MB) | 10 MB | 1-3 MB | ~80-90% |
| 中型应用 (~50MB) | 50 MB | 5-15 MB | ~70-90% |
| 大型应用 (~200MB) | 200 MB | 10-50 MB | ~75-95% |

> 来源: [Delta Updates](https://docs.velopack.io/packaging/deltas)

---

## 8. 局限性与已知问题

### 8.1 各平台成熟度差异

| 维度 | Windows | macOS | Linux |
|------|---------|-------|-------|
| 整体成熟度 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| 安装程序 | Setup.exe (完整) | .pkg (基本) | 无（仅 AppImage） |
| 代码签名 | 成熟 | 需 Apple Developer | 无需/不适用 |
| 桌面集成 | 注册表/快捷方式 | Launch Services | ⚠️ 需 AppImageLauncher |
| 增量更新 | ✅ | ✅ | ✅ |
| 文件锁定处理 | 完善 | 基础 | N/A |
| 社区案例 | 丰富 | 较少 | 极少 |

### 8.2 已知问题

| 问题 | 平台 | 状态 | Issue |
|------|------|------|-------|
| `.desktop` 缺少 `X-AppImage-Version` | Linux | 未修复 | #304 |
| deb/rpm 包格式不支持 | Linux | 无计划 | #370 |
| systemd 服务集成不完善 | Linux | 未解决 | #370 |
| DMG 不直接生成 | macOS | 需手动创建 | — |
| Sparkle 协议不兼容 | macOS | 设计如此 | — |
| macOS 打包必须在 macOS 上执行 | macOS | Apple 限制 | — |
| FUSE 依赖在部分新版 Linux 上被弃用 | Linux | 外部问题 | — |

### 8.3 平台生态风险

- **Apple 平台政策**：Apple 对非 App Store 分发的应用施加越来越多的限制，包括 Gatekeeper、公证要求、以及可能的运行时限制。这影响了所有非 App Store 分发方案（包括 Velopack 和 Sparkle），但 Velopack 作为一个较新的框架，可能更难快速适应政策变化
- **Linux 碎片化**：Linux 桌面生态高度碎片化，AppImage、Flatpak、Snap 三种格式各有优劣。Velopack 选择 AppImage 意味着放弃了使用系统包管理器的用户群体。Flatpak 和 Snap 都内置了自动更新能力，这使得 Velopack 在 Linux 上的价值相对有限
- **.NET 运行时依赖**：对于 .NET 应用（如 DocuFiller），Velopack 打包的 AppImage 需要包含完整的 .NET 运行时，导致文件体积较大（通常 60-100MB+）。相比之下，系统包管理器可以依赖系统已安装的 .NET 运行时

### 8.4 社区反馈

- **正面**：Windows 用户普遍反馈 Velopack 比 Squirrel.Windows 快得多，打包体验优秀。有用户评价"30 年开发经验中用过的最好的安装框架"。另一位从 Clowd.Squirrel 迁移的用户表示"发布和升级速度惊人地快"
- **负面**：YouTube 上有视频 "I Tried VeloPack Installer and Regret It"，主要抱怨跨平台编译体验差（从 Mac 交叉编译到 Windows 失败）。Rust 社区论坛上有开发者询问"是否安全，是否会在用户不知情的情况下回传数据"
- **关注点**：Linux 社区对 AppImage 的接受度有限，部分开发者更倾向 Flatpak 或原生包。Briefcase（Python 打包工具）已经明确不推荐 AppImage，认为其与现代 GUI 框架和 DBus 集成存在根本性问题

> 来源: [Velopack GitHub Issues](https://github.com/velopack/velopack/issues)、[YouTube 评论](https://www.youtube.com/watch?v=cHiKyNEqvHY)

---

## 9. 与替代方案对比

### 9.1 macOS: Sparkle vs Velopack

| 维度 | Sparkle 2 | Velopack |
|------|-----------|----------|
| **历史** | 2006 年至今，macOS 事实标准 | 2023 年，新兴框架 |
| **语言** | Objective-C/Swift | Rust 核心 + 多语言 SDK |
| **协议** | appcast.xml (RSS) | releases.{channel}.json |
| **签名** | EdDSA + Apple 代码签名 | 自定义签名 + Apple 代码签名 |
| **跨平台** | ❌ 仅 macOS | ✅ Windows + macOS + Linux |
| **增量更新** | ✅ (自定义差分) | ✅ (Zstandard) |
| **社区** | 极大，几乎所有独立 macOS 开发者使用 | 较小但增长中 |
| **App Store 兼容** | ✅ 沙盒模式 | ❌ 不适用 |
| **维护状态** | 活跃 | 活跃 |

### 9.2 Linux: AppImageUpdate vs Velopack

| 维度 | AppImageUpdate | Velopack |
|------|---------------|----------|
| **格式** | 仅 AppImage | AppImage |
| **跨平台** | ❌ 仅 Linux | ✅ 三平台统一 |
| **增量更新** | ✅ (block-level diff) | ✅ (Zstandard file diff) |
| **语言 SDK** | C 库 | .NET / Rust / C / JS / Go / Java |
| **桌面集成** | 与 AppImage 生态一致 | 弱（缺少 X-AppImage-Version 等） |
| **deb/rpm** | ❌ | ❌ |
| **FUSE 问题** | 同样受影响 | 同样受影响 |

### 9.3 Windows: Squirrel.Windows vs Velopack

| 维度 | Squirrel.Windows | Velopack |
|------|-----------------|----------|
| **维护状态** | 基本停止 | 活跃 |
| **性能** | 慢（.NET 实现） | 快（Rust 实现） |
| **跨平台** | ❌ 仅 Windows | ✅ 三平台统一 |
| **API 设计** | 事件驱动，复杂 | 简洁的 check/download/apply |
| **迁移** | — | 自动迁移支持 |

### 9.4 综合对比

| 框架 | Windows | macOS | Linux | 增量更新 | 统一 API |
|------|---------|-------|-------|---------|---------|
| **Velopack** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ✅ | ✅ |
| **Sparkle** | ❌ | ⭐⭐⭐⭐⭐ | ❌ | ✅ | ❌ |
| **AppImageUpdate** | ❌ | ❌ | ⭐⭐⭐ | ✅ | ❌ |
| **Squirrel.Windows** | ⭐⭐⭐ | ❌ | ❌ | ✅ | ❌ |
| **原生方案组合** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ❌ | ❌ |

> 来源: [Sparkle](https://sparkle-project.org/)、[AppImageUpdate](https://github.com/AppImage/AppImageUpdate)、[Velopack Blog](https://docs.velopack.io/blog/2024/03/07/why-is-distribution-so-hard)

---

## 10. 对 DocuFiller 的建议

### 10.1 能否统一三平台更新？

**结论：可以统一，但 Linux 端需要额外工具补充。**

| 平台 | Velopack 能否满足 | 需要额外工具 |
|------|------------------|-------------|
| **Windows** | ✅ 完全满足，当前已在使用 | 无 |
| **macOS** | ✅ 基本满足 | Apple Developer 账号、DMG 打包工具 |
| **Linux** | ⚠️ 基本可用但有限 | NFPM（生成 deb/rpm）或额外构建脚本 |

### 10.2 迁移路线建议

#### 第一阶段：Avalonia 迁移 + Windows 适配（保持 Velopack）

- 将 `TargetFramework` 从 `net8.0-windows` 改为 `net8.0`
- 将 `UseWPF` 替换为 Avalonia 引用
- 保持 `VelopackApp.Build().Run()` 作为 `Program.Main()` 第一行
- Velopack SDK 代码（`UpdateManager`、`SimpleWebSource`、`GithubSource`）**无需修改**，因为它们是平台无关的 .NET Standard 2.0 库
- 升级 Velopack 到最新稳定版（从 0.0.1298 升级）

#### 第二阶段：macOS 支持

- 在 macOS CI runner 上配置 `vpk [osx] pack`
- 配置 Apple Developer 证书和公证流程
- 准备 `.icns` 图标文件
- DMG 需要额外工具（如 `create-dmg`）从 `.zip` 生成

#### 第三阶段：Linux 支持

- 使用 `vpk [linux] pack` 生成 AppImage
- 准备 `.png` 图标文件
- 如果需要 deb/rpm，使用 [NFPM](https://nfpm.goreleaser.com/) 等工具从同一 build 目录额外生成
- 注意：NFPM 生成的包无法使用 Velopack 的自动更新功能

### 10.3 代码影响评估

| 文件/组件 | 迁移影响 |
|----------|---------|
| `Program.cs` | 无需修改（`VelopackApp.Build().Run()` 模式不变） |
| `UpdateService.cs` | 无需修改（`UpdateManager` API 跨平台一致） |
| `MainWindowViewModel.cs` | 需要替换 WPF 更新弹窗为 Avalonia 对话框 |
| `DocuFiller.csproj` | 添加 Avalonia 包引用，移除 `UseWPF`，Velopack 包引用保留 |
| CI/CD | 需要添加 macOS 和 Linux 的构建/打包步骤 |
| 服务器 | 需要为 osx 和 linux 通道部署 feed 文件 |

### 10.4 关键决策点

1. **Linux 分发策略**：仅分发 AppImage（简单但接受度低）还是额外提供 deb/rpm（需要 NFPM 等额外工具）
2. **macOS 签名**：是否购买 Apple Developer 账号（$99/年），否则用户需要手动绕过 Gatekeeper
3. **Velopack 升级**：是否升级到最新版（可能带来 API 变更），还是继续使用 0.0.1298
4. **DMG 分发**：是否需要 DMG 格式（macOS 用户期望），还是仅分发 .pkg 和 .zip

### 10.5 风险评估

| 风险 | 影响 | 可能性 | 缓解措施 |
|------|------|--------|---------|
| Velopack Linux 支持停滞 | 高 | 中 | 使用 NFPM 补充 deb/rpm，不依赖 Velopack 的 Linux 更新 |
| Apple 提高非 App Store 分发门槛 | 高 | 中 | 关注 Apple 政策变化，预留 App Store 分发方案 |
| Velopack API 在跨平台场景有隐藏 bug | 中 | 低 | 升级到最新版获取修复，编写跨平台集成测试 |
| AppImage 在新版 Linux 上无法运行 | 高 | 低 | 考虑无 FUSE 的 AppImage 方案（如 AppImageLauncher 的提取模式） |
| 内网更新服务器的多平台 feed 管理复杂度 | 低 | 高 | 使用 vpk upload 命令统一管理，CI 自动化部署 |

---

## 11. 优缺点总结

### 优点

1. **统一 API**：`UpdateManager` 的 check/download/apply 模式在所有平台上一致，代码无需平台分支
2. **Windows 生态成熟**：DocuFiller 已有完整的 Windows 端使用经验，迁移风险低
3. **增量更新**：Zstandard 算法三平台通用，显著减少更新下载量
4. **零配置打包**：`vpk pack` 一条命令生成所有产物，CI/CD 友好
5. **双源更新**：`SimpleWebSource` + `GithubSource` 模式可直接复用到新平台
6. **语言无关**：即使未来 DocuFiller 核心语言不变，Velopack SDK 仍可使用
7. **活跃维护**：核心作者全职维护，社区增长中，版本迭代频繁
8. **MIT 开源**：无商业授权风险

### 缺点

1. **Linux 支持有限**：仅 AppImage，无 deb/rpm，桌面集成弱
2. **macOS 需要苹果生态**：必须购买 Apple Developer 账号，打包必须在 macOS 上
3. **社区案例不均衡**：Windows 案例丰富，macOS/Linux 较少
4. **AppImage 趋势**：Linux 社区对 AppImage 的热情在降低，Flatpak/Snap 更受欢迎
5. **跨平台编译受限**：部分平台特有功能（如代码签名）只能在目标平台上执行
6. **不兼容现有协议**：不兼容 Sparkle (macOS) 或 AppImageUpdate (Linux) 的 feed 格式
7. **Linux FUSE 问题**：部分新版 Linux 发行版弃用 FUSE，可能影响 AppImage 运行

---

## 12. 调研日期与信息来源

| # | 来源 | URL |
|---|------|-----|
| 1 | Velopack 官方文档 | https://docs.velopack.io/ |
| 2 | Velopack GitHub 仓库 | https://github.com/velopack/velopack |
| 3 | Velopack 官网 | https://velopack.io/ |
| 4 | NuGet: Velopack 0.0.1298 | https://www.nuget.org/packages/velopack |
| 5 | Velopack Rust Crate (lib.rs) | https://lib.rs/crates/velopack |
| 6 | Windows Overview | https://docs.velopack.io/packaging/operating-systems/windows |
| 7 | MacOS Overview | https://docs.velopack.io/packaging/operating-systems/macos |
| 8 | Linux Overview | https://docs.velopack.io/packaging/operating-systems/linux |
| 9 | Code Signing | https://docs.velopack.io/packaging/signing |
| 10 | Delta Updates | https://docs.velopack.io/packaging/deltas |
| 11 | Release Channels | https://docs.velopack.io/packaging/channels |
| 12 | Cross Compiling | https://docs.velopack.io/packaging/cross-compiling |
| 13 | Distributing Overview | https://docs.velopack.io/distributing/overview |
| 14 | Packaging Overview | https://docs.velopack.io/packaging/overview |
| 15 | Integrating Overview | https://docs.velopack.io/integrating/overview |
| 16 | GitHub Issue #370 (deb/rpm 支持) | https://github.com/velopack/velopack/issues/370 |
| 17 | GitHub Issue #304 (X-AppImage-Version) | https://github.com/velopack/velopack/issues/304 |
| 18 | vpk CLI Reference (Linux) | https://docs.velopack.io/reference/cli/content/vpk-linux |
| 19 | Velopack Blog: 跨平台分发之痛 | https://docs.velopack.io/blog/2024/03/07/why-is-distribution-so-hard |
| 20 | Reddit: Velopack for Flutter | https://www.reddit.com/r/FlutterDev/comments/1em8izd/ |
| 21 | Rust Users Forum: Velopack 讨论 | https://users.rust-lang.org/t/anybody-tried-velopack-the-packager/139913 |
| 22 | Hypermedia: Velopack Delta Updates for Electron | https://seed.hyper.media/hm/z6Mkvz9TgGtv9zsGsdrksfNk1ajbFancgHREJEz3Y2HsAVdk/ |
| 23 | DocuFiller 项目: DocuFiller.csproj | 项目本地文件 |
| 24 | DocuFiller 项目: 项目记忆 | GSD Memory Store |
