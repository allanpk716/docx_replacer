# M009-q7p4iu: GitHub CI/CD 发布 + 多源更新提醒

**Gathered:** 2026-04-26
**Status:** Ready for planning

## Project Description

为 DocuFiller 建立 GitHub CI/CD 发布流水线和多源更新提醒体验。打 `v*` tag 自动构建发布到 GitHub Release，应用启动时智能选择更新源（内网 Go 服务器优先，GitHub Releases 备选），GUI 和 CLI 都提供对应的更新提示和操作能力。

## Why This Milestone

目前 DocuFiller 缺少 GitHub CI/CD 流水线，发布依赖本地 `build-internal.bat` 手动执行。Velopack 更新框架和内网 Go 服务器已部署，但外网用户（没有内网 Go 服务器）无法自动更新。GUI 的更新提示只在用户主动点击"检查更新"按钮时触发，用户是小白，不会主动操作。CLI 模式完全缺少更新能力。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 打 `v1.0.0` 格式的 tag 推送后，GitHub Release 页面自动出现可下载的安装包
- GUI 启动后状态栏常驻显示更新状态（未配置源/有新版本/便携版提示），点击可执行更新
- 没有内网更新服务器的外网用户，自动通过 GitHub Releases 检查和执行更新
- CLI 用户执行 `DocuFiller.exe update` 查看版本信息，`--yes` 确认后自动更新
- CLI 用户每次执行命令后，如果有更新信息，JSONL 输出末尾会有提示

### Entry point / environment

- Entry point: GitHub tag push → Actions workflow；GUI 启动 → 状态栏提示；CLI `DocuFiller.exe update`
- Environment: Windows 桌面应用（安装版），GitHub Actions CI/CD

## Completion Class

- Contract complete means: GitHub Actions workflow 正确触发和产出；UpdateService 双源切换逻辑正确；CLI update 命令 JSONL 输出格式正确
- Integration complete means: 从 tag push 到 GitHub Release 创建的全流程跑通；GUI 状态栏与 UpdateService 联动正确；CLI 命令后更新提醒正确追加
- Operational complete means: 真实打 tag 后 GitHub Release 包含全部 4 类文件；安装版应用能通过 GitHub Releases 检查和下载更新

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 打 `v*` tag 推送后 GitHub Release 自动创建，包含 Setup.exe + Portable.zip + .nupkg + releases.win.json
- `UpdateUrl` 为空时 UpdateService 使用 GitHubSource，非空时使用 HTTP URL
- GUI 状态栏显示三种更新状态（未配置源/有新版本/便携版）
- CLI `update` 命令 JSONL 输出正确，`--yes` 执行下载应用
- dotnet build 通过，现有测试不被破坏

## Architectural Decisions

### UpdateService 多源切换

**Decision:** UpdateUrl 非空用 HTTP URL 走内网 Go 服务器，为空用 GitHubSource 走 GitHub Releases

**Rationale:** 公司用户访问 GitHub 不顺畅，内网 Go 服务器是首选。GitHub Releases 作为外网用户的备选通道。不需要同时检查两个源。

**Alternatives Considered:**
- 双源并行检查 → 不必要，增加复杂度和延迟
- 只支持 GitHub Releases → 公司用户体验差

### CLI update 命令交互

**Decision:** 纯 JSONL 输出 + `--yes` 参数确认执行，无交互式 Y/N

**Rationale:** CLI 场景可能用于批处理脚本，交互式确认会打断工作流。JSONL 保持输出格式一致性。

**Alternatives Considered:**
- 交互式 Y/N 确认 → 不适合批处理场景

### IUpdateService 接口不变

**Decision:** 不改接口签名，`IsUpdateUrlConfigured` 语义扩展为"有任一更新源可用"

**Rationale:** 内网 Go 服务器和 GitHub Releases 都算"有更新源"，调用方不需要知道具体走哪个源。最小化改动范围。

**Alternatives Considered:**
- 拆分为 `IsUpdateSourceAvailable` + `GetSourceType()` → 过度设计

### 只发布安装版

**Decision:** GitHub Release 只提供 Setup.exe 安装版和 Portable.zip，Velopack 自更新只在安装版下工作

**Rationale:** 统一更新体验，避免 Portable 版无法自更新的用户困惑。便携版有明确提示告知用户。

**Alternatives Considered:**
- 同时支持 Portable 自更新 → 需要额外机制，复杂度高

### GitHub 只走 stable 通道

**Decision:** GitHub Releases 只分发 stable 版本，beta 继续走内网 Go 服务器

**Rationale:** 公司大部分用户访问 GitHub 不顺畅，beta 测试在内网进行。GitHub 对外只发稳定版。

**Alternatives Considered:**
- GitHub 也分 stable/beta → 增加维护复杂度，收益低

## Error Handling Strategy

| 场景 | 处理方式 |
|------|----------|
| GitHub Actions 构建失败 | Actions 自身报错，不创建 Release。tag 保留，开发者手动修复后删 tag 重打 |
| update 命令检查失败（网络不通） | JSONL 输出 `{"type":"update","status":"error","data":{"message":"..."}}`,退出码 1 |
| GUI 检查更新失败（网络不通） | 状态栏保持显示"检查更新失败"，不弹窗打断用户 |
| 下载更新中断 | update --yes 输出错误 JSONL 并退出；GUI 走现有弹窗错误提示 |
| 便携版运行（Velopack 未安装） | 状态栏显示"当前为便携版，不支持自动更新。请使用安装版以获得自动更新功能" |

## Risks and Unknowns

- Velopack GitHubSource 在公司网络环境下可能不稳定 → 不阻塞，GitHub 是备选通道
- CLI 短生命周期进程下载更新后重启的行为需要验证 → Velopack 的 ApplyUpdatesAndRestart 在 CLI 进程中是否能正确工作

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — 现有更新服务，只支持 HTTP URL 源
- `Services/Interfaces/IUpdateService.cs` — 更新服务接口，不改签名
- `ViewModels/MainWindowViewModel.cs` — 已有 CheckUpdateCommand 和 CheckUpdateAsync 流程
- `MainWindow.xaml` — 已有状态栏版本号和"检查更新"按钮
- `Cli/CliRunner.cs` — CLI 路由器，需要注册 update 子命令
- `Cli/JsonlOutput.cs` — JSONL 输出工具，需要支持 update 类型
- `scripts/build-internal.bat` — 现有构建脚本，Velopack 打包流程可参考
- `Program.cs` — VelopackApp.Build().Run() 已在 Main 最先调用

## Relevant Requirements

- R037 — GitHub Actions CI/CD
- R038 — Release 产物格式
- R039 — UpdateService 多源支持
- R040 — GUI 状态栏常驻提示
- R041 — CLI update 子命令
- R042 — CLI JSONL 更新提醒
- R043 — 现有更新流程零回归

## Scope

### In Scope

- GitHub Actions workflow（tag 触发，Velopack 打包，Release 创建）
- UpdateService 多源支持（HTTP URL + GitHubSource）
- 便携版检测（UpdateManager.IsInstalled）
- GUI 状态栏常驻提示（三种状态）
- CLI update 子命令（JSONL 输出 + --yes 执行）
- CLI JSONL 更新提醒（actionable 时追加）

### Out of Scope / Non-Goals

- Portable 版自更新支持
- push 到 main 的自动构建
- 同时检查多个更新源
- GitHub beta 通道
- UI 渠道切换（R029）
- 自动启动检查更新（R028）

## Technical Constraints

- Velopack 0.0.1298 已集成
- .NET 8 + WPF，Windows only
- Go 更新服务器不在本里程碑改动
- 公司网络访问 GitHub 不顺畅

## Integration Points

- GitHub Actions — CI/CD 触发和 Release 创建
- GitHub Releases — 分发 + Velopack GitHubSource 更新源
- 内网 Go 更新服务器 — 现有，不改动

## Testing Requirements

- GitHub Actions workflow：手动打 tag 触发验证
- UpdateService 多源逻辑：单元测试覆盖
- GUI 状态栏提示：手动验证三种状态
- CLI update 命令：手动运行构建产物验证 JSONL 输出
- CLI JSONL 更新提醒：验证只在 actionable 时输出
- dotnet build 和现有测试不被破坏

## Acceptance Criteria

### S01: GitHub Actions workflow 能被 tag 触发，产出 4 类文件到 Release
### S02: UpdateService 在 UpdateUrl 为空时使用 GitHubSource，非空时使用 HTTP URL；便携版正确检测
### S03: GUI 状态栏显示三种更新状态，便携版有明确提示和指引
### S04: CLI update 命令 JSONL 输出正确，--yes 执行下载应用；其他命令有条件追加 update 行

## Open Questions

- None — all resolved during discussion
