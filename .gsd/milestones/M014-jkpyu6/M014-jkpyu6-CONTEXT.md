# M014-jkpyu6: ExplicitChannel 导致 GitHub 更新检测失败

**Gathered:** 2026-05-02
**Status:** Ready for planning

## Project Description

DocuFiller 是一个 .NET 8 + WPF 桌面应用，使用 Velopack 实现自动更新。应用支持两种更新源：内网 Go HTTP 服务器（SimpleWebSource）和 GitHub Releases（GithubSource）。用户可在设置界面切换更新源和通道（stable/beta）。

## Why This Milestone

`UpdateService.CreateUpdateManager()` 始终在 `UpdateOptions` 中设置 `ExplicitChannel = _channel`（默认 "stable"）。当更新源为 `GithubSource` 时，Velopack 的 GitHub 发布源在收到 `ExplicitChannel` 参数后无法正确匹配 GitHub Release 中的 Velopack 包，导致 `CheckForUpdatesAsync()` 检测不到已发布的最新版本——始终返回 null（"当前已是最新版本"），即使 GitHub 上存在更新的 release。

这个问题只影响 GitHub Releases 模式（UpdateUrl 为空时）。HTTP 内网源（SimpleWebSource）正常工作。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在 GitHub Releases 模式下（UpdateUrl 留空），启动应用后正确检测到 GitHub 上发布的最新版本
- 通过 GUI 或 CLI `update` 命令看到"发现新版本"提示，并能下载和安装更新

### Entry point / environment

- Entry point: GUI 自动检查更新 / CLI `DocuFiller.exe update` 命令
- Environment: 已安装的 Velopack 安装版（非便携版），UpdateUrl 为空，使用 GitHub Releases 源
- Live dependencies involved: GitHub Releases API（公网访问）

## Completion Class

- Contract complete means: `CheckForUpdatesAsync()` 在 GitHub Releases 模式下能正确返回可用更新信息（非 null），单元测试覆盖 GitHub 模式不传 ExplicitChannel 的行为
- Integration complete means: 安装版应用在 GitHub 模式下实际能检测到发布版本并下载更新
- Operational complete means: 不引入对 HTTP 内网源（SimpleWebSource）功能的回归

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- UpdateUrl 为空时，`CreateUpdateManager()` 创建的 UpdateManager 不包含 ExplicitChannel 参数，Velopack 使用包自身的默认通道
- UpdateUrl 非空时（HTTP 模式），ExplicitChannel 行为不变，继续正常工作
- beta 通道回退逻辑（`CheckForUpdatesAsync` 中 fallback 到 stable）在两种源模式下均正确

## Architectural Decisions

### GitHub 模式不传 ExplicitChannel

**Decision:** 当 `_sourceType == "GitHub"` 时，`CreateUpdateManager()` 不设置 `ExplicitChannel`，让 Velopack 使用构建时嵌入包的默认通道。

**Rationale:** Velopack 的 `GithubSource` 实现在带 `ExplicitChannel` 参数时，会通过通道名过滤 GitHub Release 资产。而项目的 `vpk pack` 命令没有指定 `--channel` 参数，所有构建都使用 Velopack 默认通道。这导致 ExplicitChannel="stable" 与 GitHub Release 中的资产命名不匹配，从而检测不到更新。不传 ExplicitChannel 让 Velopack 自动从本地包元数据读取通道，避免过滤不匹配。

**Alternatives Considered:**
- 统一保留 ExplicitChannel 但修复通道值 — 需要确保通道值与 `vpk pack --channel` 完全一致，增加构建和运行时的耦合风险
- 在构建脚本中添加 `--channel` 参数给 vpk pack — 改动范围更大，且无法修复已发布的版本

### HTTP 模式保留 ExplicitChannel

**Decision:** HTTP 内网源（SimpleWebSource）继续使用 `ExplicitChannel`，因为 URL 中已包含通道路径（如 `http://server/stable/`），且内网服务器的通道隔离逻辑依赖此参数。

**Rationale:** SimpleWebSource 的通道管理是项目自己控制的（Go 服务器按通道目录存放 release），ExplicitChannel 与 URL 路径一致，不存在过滤不匹配问题。

## Error Handling Strategy

现有错误处理已足够：
- `CheckForUpdatesAsync()` 在当前通道无更新时，已有 beta→stable 的回退逻辑
- `CreateUpdateManager()` 在构造函数中有 try-catch 处理 IsInstalled 检测失败
- 不需要新增错误处理路径，只需修复 ExplicitChannel 的传递逻辑

## Risks and Unknowns

- **Velopack 默认通道名称** — `vpk pack` 不指定 `--channel` 时的默认通道名是什么？文档说是平台默认（Windows 为 "win"），需要确认不传 ExplicitChannel 时 Velopack 能正确匹配。如果默认通道不是 "stable"，则不传 ExplicitChannel 可能同样失败
- **已发布包的通道元数据** — 已发布到 GitHub 的 Velopack 包的通道元数据是什么？如果是 "win"（而非 "stable"），可能需要额外的兼容处理
- **beta 通道回退逻辑的 GitHub 兼容性** — `CheckForUpdatesAsync()` 中 `_channel != "stable"` 时创建 stable 通道 manager 的回退逻辑，在 GitHub 模式下是否还需要？如果 GitHub 不传 ExplicitChannel，回退可能不再必要

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — 核心修改目标，`CreateUpdateManager()` 和 `CreateUpdateManagerForChannel()` 方法
- `Services/Interfaces/IUpdateService.cs` — 接口不变
- `ViewModels/UpdateSettingsViewModel.cs` — 调用 `ReloadSource()` 切换源，不直接创建 UpdateManager
- `Cli/Commands/UpdateCommand.cs` — CLI 更新命令，调用 `CheckForUpdatesAsync()`
- `Tests/UpdateServiceTests.cs` — 现有单元测试，需要新增 GitHub 模式的测试用例
- `scripts/build-internal.bat` — `vpk pack` 命令未指定 `--channel` 参数
- `docs/plans/e2e-update-test-guide.md` — E2E 更新测试指南

## Relevant Requirements

- 自动更新功能的核心可靠性要求 — GitHub Releases 是外网用户的默认更新源，必须正常工作

## Scope

### In Scope

- 修改 `CreateUpdateManager()` / `CreateUpdateManagerForChannel()` 使 GitHub 模式不传 ExplicitChannel
- 更新 `CheckForUpdatesAsync()` 中 beta→stable 回退逻辑，在 GitHub 模式下跳过或调整
- 添加/更新单元测试覆盖 GitHub 模式行为
- 验证 HTTP 模式（SimpleWebSource + ExplicitChannel）不受影响

### Out of Scope / Non-Goals

- 不修改构建脚本（`vpk pack` 不加 `--channel`）
- 不修改 Velopack 版本（保持 0.0.1298）
- 不修改 `IUpdateService` 接口
- 不修改 GUI 更新设置界面

## Technical Constraints

- Velopack 0.0.1298 版本行为不可变
- 必须同时支持 HTTP 内网源和 GitHub Releases 源
- 已发布到 GitHub 的包不能重新发布

## Integration Points

- GitHub Releases API — GithubSource 通过 GitHub API 获取 release 列表和资产
- 内网 Go 更新服务器 — SimpleWebSource 通过 HTTP 获取 releases 文件

## Testing Requirements

- **单元测试**：验证 GitHub 模式（UpdateUrl 为空）时 `CreateUpdateManager()` 的行为
- **单元测试**：验证 HTTP 模式（UpdateUrl 非空）时 ExplicitChannel 行为不变
- **单元测试**：验证 `ReloadSource()` 从 GitHub 切换到 HTTP 后通道参数正确
- **现有测试**：所有现有 `UpdateServiceTests` 必须继续通过

## Acceptance Criteria

1. UpdateUrl 为空 + Channel 为 "stable" 时，`UpdateManager` 不设置 `ExplicitChannel`
2. UpdateUrl 为空 + Channel 为 "beta" 时，`UpdateManager` 不设置 `ExplicitChannel`（GitHub 不做通道过滤）
3. UpdateUrl 非空时，`UpdateManager` 仍然设置 `ExplicitChannel = _channel`
4. `CheckForUpdatesAsync()` 的 beta→stable 回退逻辑仅在 HTTP 模式下生效
5. 所有现有单元测试通过

## Open Questions

- Velopack `vpk pack` 不指定 `--channel` 时的默认通道名是否为 "stable"？如果默认是 "win"，不传 ExplicitChannel 可能需要额外处理 — 需要在实现阶段通过 `vpk pack` 输出或 Velopack 源码确认
