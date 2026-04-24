# M007-wpaxa3: Velopack 自动更新 — 单 EXE 发布 + 内网更新

**Gathered:** 2026-04-24
**Status:** Ready for planning

## Project Description

为 DocuFiller 集成 Velopack 自动更新框架，实现手动检测远程最新版本号并一键自动升级。发布形态从多文件 zip 改为单 EXE（PublishSingleFile self-contained），同时提供安装版（Setup.exe）和便携版（Portable.zip）。更新源为内网 HTTP 静态文件服务器。

## Why This Milestone

DocuFiller 目前通过手动复制 zip 分发，用户无法感知新版本、无法自动升级。旧更新系统（update-client.exe + 内网更新服务器）已在 M004 完全移除。需要一个现代的、内网友好的、从零设计的更新方案。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在主窗口底部状态栏看到当前版本号
- 点击"检查更新"按钮，程序自动检测内网更新源是否有新版本
- 如有新版本，确认后自动下载、替换并重启应用
- 用户数据配置文件（appsettings.json、Logs/、Output/）在更新后完整保留
- 获得单 EXE 安装包（Setup.exe）或便携版（解压即用），均可自动更新

### Entry point / environment

- Entry point: 主窗口底部状态栏"检查更新"按钮
- Environment: Windows 桌面，内网环境（HTTP 更新源可达）
- Live dependencies involved: 内网 HTTP 静态文件服务器（存放 releases.win.json + .nupkg 文件）

## Completion Class

- Contract complete means: IUpdateService 接口实现通过单元测试，Velopack 集成正确初始化
- Integration complete means: 主窗口 UI 正确调用更新服务，发布脚本产出完整发布物
- Operational complete means: 在干净 Windows 上完成安装→检查更新→升级→配置保留的完整流程

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- build-internal.bat 产出 Setup.exe + Portable.zip + .nupkg + releases.win.json
- Setup.exe 在干净 Windows 上安装后能正常运行 DocuFiller
- 从旧版本通过 Velopack 更新到新版本后，应用正常启动且用户配置文件保留
- 主窗口状态栏显示版本号且检查更新按钮功能正常

## Architectural Decisions

### 更新框架选择：Velopack

**Decision:** 使用 Velopack 替代旧更新系统

**Rationale:** Squirrel.Windows/Clowd.Squirrel 的正统继任者。内网部署友好（只需 HTTP 静态文件目录），支持单 EXE、增量更新、便携模式自更新。3900+ commits，活跃维护。

**Alternatives Considered:**
- AutoUpdater.NET — 不支持增量更新，对 .NET 8 单 EXE 有已知兼容性问题
- 手写自更新 — 大量边界情况需要处理，等于重造轮子
- ClickOnce/MSIX — 不支持 .NET 8 WinExe 单 EXE，打包复杂度过高

### 更新源配置位置：appsettings.json

**Decision:** 更新服务器 URL 放在 appsettings.json 的 Update:UpdateUrl 节点

**Rationale:** 和现有配置风格一致（所有应用配置都在 appsettings.json），不使用 App.config（App.config 中旧更新配置将被清理）。Velopack UpdateManager 构造函数接受 URL 字符串，直接传入即可。

**Alternatives Considered:**
- App.config — 旧更新系统残留，风格不统一
- 硬编码 — 不灵活，无法针对不同部署环境配置

### 不使用 Velopack 内置更新对话框

**Decision:** 自定义 WPF 弹窗处理更新确认和进度展示

**Rationale:** Velopack 内置对话框是 WinForms 风格，与应用 WPF 视觉风格不一致。自定义弹窗可以匹配现有 UI 主题。

**Alternatives Considered:**
- Velopack 内置对话框 — 风格不匹配，无法自定义

### 不启用 PublishTrimmed

**Decision:** PublishSingleFile=true 但 PublishTrimmed=false

**Rationale:** EPPlus 和 DocumentFormat.OpenXml 使用反射，trimming 可能导致运行时错误。内网环境下载速度不是瓶颈，大体积（80-120MB）可接受。

**Alternatives Considered:**
- 启用 trimming — 有反射风险，节省体积但不值得冒稳定性风险

## Scope

### In Scope

- Velopack NuGet 包集成和初始化
- IUpdateService 接口和实现（检查/下载/安装/重启）
- 主窗口底部状态栏（版本号 + 检查更新按钮）
- 自定义更新确认和进度对话框
- 发布管道改造（PublishSingleFile + vpk pack）
- 旧更新系统残留清理（App.config 配置、旧脚本引用）
- 端到端更新流程验证

### Out of Scope / Non-Goals

- 后台自动检查更新通知（手动触发即可）
- 更新渠道管理（beta/stable 切换）
- 旧更新服务器兼容性
- 更新日志展示（使用 Velopack 的 ReleaseNotes 功能）
- 代码签名（内网环境不需要）

## Technical Constraints

- .NET 8 + WPF, PublishSingleFile self-contained win-x64
- Velopack UpdateManager 需要 HTTP 可达的静态文件目录作为更新源
- vpk 工具需要在构建机器上预装（dotnet tool install -g vpk）
- EPPlus 和 OpenXML 有反射使用，不能启用 PublishTrimmed

## Integration Points

- **内网 HTTP 服务器** — 存放 releases.win.json + .nupkg 文件，无需服务端逻辑
- **appsettings.json** — Update:UpdateUrl 配置更新源地址
- **Program.cs** — VelopackApp.Build().Run() 初始化
- **MainWindow** — 底部状态栏集成更新 UI

## Error Handling Strategy

- **网络不可达**：CheckForUpdatesAsync 捕获 HttpRequestException，UI 显示"无法连接到更新服务器"
- **下载中断**：Velopack 内部处理下载续传和校验
- **文件替换**：Velopack 通过重启应用在启动时替换文件，不需要额外进程管理
- **版本比较**：Velopack 使用语义化版本自动比较
- **未配置更新源**：UpdateUrl 为空时，检查更新按钮灰显，不报错
- **权限不足**：Velopack 自动处理 UAC 提权

## Risks and Unknowns

- **Velopack 与 WPF 单文件发布的兼容性** — 需要在 S01 尽早验证 dotnet publish + vpk pack 能正确产出并运行
- **vpk 工具在 Windows 上的安装和路径** — build-internal.bat 需要能找到 vpk 命令
- **现有测试是否受 Velopack 集成影响** — VelopackApp.Build().Run() 可能在测试环境中表现不同

## Relevant Requirements

- R022 — Velopack 集成 + 旧系统清理
- R023 — 更新服务层
- R024 — 主窗口状态栏 UI
- R025 — 发布管道改造
- R026 — 端到端验证
- R027 — 现有测试回归安全

## Existing Codebase / Prior Art

- `Program.cs` — 已有自定义 Main() 入口点（StartupObject=DocuFiller.Program），是添加 VelopackApp.Build().Run() 的正确位置
- `Utils/VersionHelper.cs` — 已有 GetCurrentVersion() 方法，使用 Assembly.GetExecutingAssembly()
- `App.config` — 残留旧更新配置项（UpdateServerUrl、UpdateChannel、CheckUpdateOnStartup）需清理
- `scripts/build-internal.bat` — 现有发布脚本，PublishSingleFile=false，需改为 true 并加入 vpk pack
- `Services/Interfaces/` — 遵循 IXxxService 命名约定，IUpdateService 应放在此处

## Acceptance Criteria

### S01: Velopack 集成 + 旧系统清理
- dotnet build 编译通过
- dotnet test 所有现有测试通过
- Program.cs 中 VelopackApp.Build().Run() 正确初始化
- App.config 中旧更新配置项已移除

### S02: 更新服务 + UI 集成
- 点击"检查更新"能正确连接更新源检测版本
- 已是最新版时显示"当前已是最新版本"
- 有新版本时显示更新确认对话框
- 更新源未配置时按钮灰显

### S03: 发布管道改造
- build-internal.bat 产出 Setup.exe + Portable.zip + .nupkg + releases.win.json
- Setup.exe 能安装并运行
- Portable.zip 解压后能直接运行

### S04: 端到端验证
- 安装旧版 → 检查更新 → 升级到新版 → 配置保留的完整流程通过

## Testing Requirements

- 现有单元测试和集成测试全部通过（R027）
- UpdateService 单元测试（mock Velopack UpdateManager）
- 端到端手动验证完整更新流程

## Open Questions

- vpk 工具版本是否需要锁定？建议在构建脚本中用 `dnx vpk --version x.y.z` 确保一致性
