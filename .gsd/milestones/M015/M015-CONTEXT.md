# M015: GitHub 更新源从 API 切换到 CDN 直连

**Gathered:** 2026-05-03
**Status:** Ready for planning

## Project Description

DocuFiller 是一个基于 .NET 8 + WPF 的桌面应用，使用 Velopack 做自动更新。当前有两种更新源模式：内网 HTTP（SimpleWebSource）和 GitHub（GithubSource）。本里程碑将 GitHub 模式从 GithubSource（GitHub API，受 60 次/小时匿名 rate limit）切换为 SimpleWebSource（CDN 直连），消除匿名 API 调用的频率限制。

## Why This Milestone

GithubSource 通过 GitHub REST API（`/repos/{owner}/{repo}/releases`）获取版本列表和下载资产。匿名请求限额为 60 次/小时，超出后返回 429 Too Many Requests。对于频繁检查更新的用户，尤其是多台机器场景，这个限制很容易触发。CDN 直连（`/releases/latest/download/`）不受 API rate limit 约束，且与内网 HTTP 模式统一使用 SimpleWebSource，简化代码。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在外网环境下无限次检查更新，不再受 GitHub API rate limit 拦截
- 在设置界面看到源类型显示为 "CDN" 而非 "GitHub"

### Entry point / environment

- Entry point: GUI 设置界面的更新源配置 + CLI `update` 命令（通过 IUpdateService 间接使用）
- Environment: 已安装版 DocuFiller（Velopack IsInstalled = true），外网环境
- Live dependencies involved: GitHub Releases CDN（`https://github.com/allanpk716/docx_replacer/releases/latest/download/`）

## Completion Class

- Contract complete means: `UpdateService` 构造函数和 `ReloadSource` 方法中不再引用 `GithubSource` 类；GitHub 模式使用 `SimpleWebSource` + CDN URL；`dotnet build` 无错误；`dotnet test` 全部通过
- Integration complete means: 在安装版 DocuFiller 中，清空 UpdateUrl 后能通过 CDN 检查并下载更新
- Operational complete means: CDN 请求失败时给出明确错误信息，不 crash

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- `UpdateService` 在 UpdateUrl 为空时创建 `SimpleWebSource`，URL 为 `https://github.com/allanpk716/docx_replacer/releases/latest/download/`
- `UpdateSourceType` 返回 `"CDN"` 而非 `"GitHub"`
- `ReloadSource` 传入空 URL 时创建 `SimpleWebSource`（CDN）而非 `GithubSource`
- 内网 HTTP 模式（UpdateUrl 非空）行为完全不变
- 所有现有单元测试通过（更新断言值以匹配新的行为）
- 无法模拟：实际通过 CDN 下载 .nupkg 并应用更新的端到端流程（需要真实 GitHub Release + 安装版环境，属于发布后手动验证）

## Architectural Decisions

### CDN URL 模式选择

**Decision:** 使用 `https://github.com/allanpk716/docx_replacer/releases/latest/download/` 作为 SimpleWebSource 的 base URL

**Rationale:** GitHub Releases CDN 的 `/releases/latest/download/` 路径可以直接访问最新 release 的所有 asset 文件（包括 `releases.win.json` 和 `.nupkg`），无需 API 调用，无 rate limit。GitHub Actions workflow 已经将 `releases.win.json` 和 `.nupkg` 上传到 Release assets，CDN 可达。

**Alternatives Considered:**
- `/releases/download/{tag}/` — 需要拼接具体版本 tag，SimpleWebSource 不支持动态拼接
- 自定义 CDN URL 配置 — 过度设计，当前只需要一个固定 URL

### 源类型显示名称

**Decision:** 将 `UpdateSourceType` 的返回值从 `"GitHub"` 改为 `"CDN"`

**Rationale:** 更准确地描述底层实现（SimpleWebSource + CDN 直连），与 `"HTTP"` 源类型命名对称。从用户视角看，"CDN" 暗示直连下载，比 "GitHub"（暗示 API 调用）更直观。

**Alternatives Considered:**
- 保持 `"GitHub"` — 不准确，底层已不再调用 GitHub API

### 通道回退策略

**Decision:** CDN 模式跳过 beta→stable 通道回退，与当前行为一致

**Rationale:** `/releases/latest/download/` 始终指向最新 release，无论是否标记为 prerelease。GitHub Releases 没有 stable/beta 目录分离，通道回退没有意义。

**Alternatives Considered:**
- 利用 prerelease 标记区分 beta/stable — 需要自定义逻辑解析 GitHub API，违背"消除 API 调用"的目标

### 错误处理策略

**Decision:** CDN 请求失败时直接报错给用户，不 fallback 到旧路径

**Rationale:** 简单明确，与内网 HTTP 模式行为一致。引入 fallback 会增加代码复杂度，且 GithubSource 本身即将移除，没有可回退的目标。

**Alternatives Considered:**
- CDN 失败后回退到 GithubSource 重试 — 增加复杂度，保留要删除的代码，得不偿失

### CLI 命令影响

**Decision:** CLI `update` 命令不需要改动

**Rationale:** CLI `UpdateCommand` 已经通过 `IUpdateService` 接口调用，底层实现切换后自动生效，不需要额外改动。

## Error Handling Strategy

CDN 模式的错误处理与内网 HTTP 模式一致：
- 网络超时：Velopack 的 `SimpleWebSource` 底层使用 `IFileDownloader`，会抛出标准 HTTP 异常
- 404（无 release 或文件缺失）：`CheckForUpdatesAsync` 返回 `null`（无可用更新）
- DNS 解析失败：标准 `HttpRequestException`，由调用方捕获并显示给用户
- 不引入重试逻辑，与 HTTP 模式保持一致

## Risks and Unknowns

- **Delta 包 CDN 缺失** — `releases.win.json` 中可能引用 delta 包（如 `DocuFiller-1.3.3-delta.nupkg`），如果 GitHub Release 上传时没有包含 delta 包，CDN 下载会 404。**缓解**：当前 workflow 只上传 `*.nupkg`（包含 delta），如果 delta 确实缺失，Velopack 会自动 fallback 到 full 包下载
- **`/releases/latest/download/` 只包含最新 release 的文件** — `releases.win.json` 里可能有多个版本条目，但 CDN 目录只有最新版本的文件。**影响**：对于增量更新场景，如果当前版本不是"最新-1"，可能无法下载中间版本的 delta 包。**缓解**：Velopack 会自动 fallback 到 full 包

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — 核心改动点：构造函数和 `ReloadSource` 方法中的 `GithubSource` 替换为 `SimpleWebSource`
- `Services/Interfaces/IUpdateService.cs` — 接口不变，仅实现改变
- `ViewModels/UpdateSettingsViewModel.cs` — 无需改动，通过接口交互
- `Cli/Commands/UpdateCommand.cs` — 无需改动，通过接口交互
- `Tests/UpdateServiceTests.cs` — 需要更新：所有断言 `UpdateSourceType == "GitHub"` 的测试改为 `"CDN"`
- `Tests/UpdateSettingsViewModelTests.cs` — 可能需要更新 mock 返回值
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — 可能需要更新 mock 返回值
- `.github/workflows/*.yml` — 无需改动，已上传 `releases.win.json` 和 `*.nupkg`

## Relevant Requirements

- R056 — update-config.json 持久化配置，本 milestone 不涉及配置存储变更

## Scope

### In Scope

- 将 `UpdateService` 中的 `GithubSource` 替换为 `SimpleWebSource` + CDN URL
- 将 `UpdateSourceType` 返回值从 `"GitHub"` 改为 `"CDN"`
- 更新所有相关单元测试的断言值
- 更新 `IsUpdateUrlConfigured` 注释（移除"GitHub Releases 始终可用"措辞，改为"CDN 始终可用"）

### Out of Scope / Non-Goals

- CLI 命令改动
- 内网 HTTP 更新逻辑改动
- 更新设置 UI 改动（UpdateSettingsWindow.xaml）
- GitHub Actions workflow 改动
- 发布流程文档更新（如需要，可在后续单独处理）
- 支持自定义 CDN URL 配置
- Delta 包策略调整

## Technical Constraints

- Velopack 版本 0.0.1298，`SimpleWebSource` 构造函数接受一个 base URL 字符串
- GitHub CDN URL 必须指向 release assets 可公开访问的路径
- 代码在 Windows 上运行，.NET 8

## Integration Points

- GitHub Releases CDN — `https://github.com/allanpk716/docx_replacer/releases/latest/download/releases.win.json`（版本清单）和 `*.nupkg`（更新包）
- Velopack UpdateManager — 通过 `SimpleWebSource` 获取 release feed 并下载资产
- 持久化配置（`~/.docx_replacer/update-config.json`） — UpdateUrl 为空时走 CDN 模式

## Testing Requirements

- **单元测试**：所有现有 `UpdateServiceTests` 更新断言值（`"GitHub"` → `"CDN"`）
- **单元测试**：验证 CDN URL 正确拼接（UpdateUrl 为空时 `EffectiveUpdateUrl` 应为 CDN 地址）
- **单元测试**：验证 `ReloadSource` 传入空 URL 时创建 CDN 源
- **单元测试**：验证 `ReloadSource` 传入非空 URL 时仍创建 HTTP 源（不受影响）
- **构建验证**：`dotnet build` 无错误
- **回归验证**：`dotnet test` 全部通过

## Acceptance Criteria

1. `UpdateService` 代码中不再引用 `GithubSource` 类（`using Velopack.Sources` 中只保留 `SimpleWebSource`）
2. UpdateUrl 为空时，`UpdateSourceType` 返回 `"CDN"`
3. UpdateUrl 为空时，`EffectiveUpdateUrl` 返回 CDN base URL
4. UpdateUrl 非空时，所有行为与改动前完全一致
5. `dotnet build` 和 `dotnet test` 全部通过

## Open Questions

- 无 — 所有关键问题已在讨论中确认
