# M018: 便携版自动更新支持

**Gathered:** 2026-05-03
**Status:** Ready for planning

## Project Description

移除 DocuFiller 便携版的自动更新阻断，让便携版（从 Portable.zip 解压运行）享有与安装版完全一致的检查→下载→应用更新能力。同时提供端到端自动化测试脚本，验证便携版更新在本地 HTTP 和真实内网 Go 服务器两种环境下都能跑通。

## Why This Milestone

当前便携版被硬性阻断在 Velopack 更新流程之外（`UpdateStatus.PortableVersion`、`IsInstalled` 守卫、CLI `PORTABLE_NOT_SUPPORTED` 错误）。这是决策 D029 的产物。但 Velopack 设计上支持便携版自更新（`IsPortable` 属性、Portable.zip 包含 Update.exe），阻断完全是应用层面的守卫。用户明确要求便携版也能自动更新，且必须验证真实内网 Go 服务器的完整链路。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 便携版解压后，状态栏正常显示更新检查结果（不是"不支持自动更新"）
- 便携版手动点击"检查更新"→ 发现新版本 → 下载 → 应用更新 → 重启后版本号正确升级
- 便携版通过 CLI `DocuFiller.exe update --yes` 全自动完成更新
- 运行 E2E 测试脚本一键验证本地和内网两种环境的便携版更新

### Entry point / environment

- Entry point: GUI 状态栏更新提示 / CLI `DocuFiller.exe update` / E2E 测试脚本
- Environment: Windows 桌面，便携版解压目录或安装目录

## Completion Class

- Contract complete means: 所有便携版更新阻断代码已移除，IUpdateService 新增 IsPortable，dotnet build 通过
- Integration complete means: 便携版 GUI 和 CLI 都能走完整更新流程，E2E 脚本在本地 HTTP 和内网 Go 服务器上都能跑通
- Operational complete means: 便携版更新后版本号正确升级，配置保留，安装版无回归

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 便携版 CLI `update --yes` 在本地 HTTP 环境下完成完整更新链路（版本号升级成功）
- 便携版 CLI `update --yes` 在内网 Go 服务器环境下完成完整更新链路
- 安装版更新行为无回归
- Velopack 便携版 Update.exe 能正确替换解压目录里的文件（E2E 验证而非假设）

## Architectural Decisions

### 便携版更新策略

**Decision:** 移除所有便携版更新阻断，便携版和安装版走完全相同的更新代码路径

**Rationale:** Velopack 设计上支持便携版自更新，当前的阻断是应用层面不必要的守卫。保持代码路径一致减少维护成本。

**Alternatives Considered:**
- 为便携版写单独的更新分支 — 增加代码复杂度，且 Velopack 已经处理了便携版差异
- 只解锁 CLI 不解锁 GUI — 不一致的用户体验

### E2E 测试自动化方式

**Decision:** 使用 CLI `update --yes` 实现全自动 E2E 测试，避开 GUI 弹窗交互

**Rationale:** GUI 更新流程有 MessageBox 确认和进度窗口，自动化困难。CLI `--yes` 模式覆盖相同的检查→下载→应用代码路径，测试等效性足够。

**Alternatives Considered:**
- UI 自动化工具 — 引入重依赖，维护成本高
- 仅手动测试 — 不可重复，无法回归

### IsInstalled 语义变更

**Decision:** `IsInstalled` 降级为纯信息属性（UI 展示），不再作为更新流程的守卫

**Rationale:** Velopack 的 `IsInstalled` 对便携版返回 false，但便携版实际上能更新。继续用它做守卫会错误阻断。

**Alternatives Considered:**
- 保持 IsInstalled 语义不变，为便携版加 bypass — 代码分支多，逻辑混乱

---

> See `.gsd/DECISIONS.md` for the full append-only register of all project decisions.

## Error Handling Strategy

便携版更新失败走和安装版完全相同的错误处理：
- GUI：日志记录 + MessageBox 显示错误信息
- CLI：JSONL error 输出 + 非 0 退出码
- Velopack 内部更新失败由框架自行回滚，应用层通过 catch 记录日志

不增加便携版专用的错误路径。

## Risks and Unknowns

- **Velopack 便携版 ApplyUpdatesAndRestart 行为** — Update.exe 替换便携版解压目录文件的实际行为需要 E2E 验证，文档没有详细描述便携版更新机制
- **便携版更新后 appsettings.json 被覆盖** — 非更新配置（Logging、UI 默认值）被新版本覆盖是正确行为；更新配置由 update-config.json 保护不受影响

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — 更新服务实现，Velopack UpdateManager 封装
- `Services/Interfaces/IUpdateService.cs` — 更新服务接口（需新增 IsPortable）
- `ViewModels/MainWindowViewModel.cs` — GUI 更新流程，含 PortableVersion 阻断逻辑
- `Cli/Commands/UpdateCommand.cs` — CLI update 命令，含 IsInstalled 守卫
- `scripts/e2e-update-test.bat` — 现有 E2E 测试脚本（仅覆盖安装版）
- `scripts/e2e-serve.py` — 本地 HTTP 测试服务器
- `scripts/build-internal.bat` — 构建打包脚本

## Relevant Requirements

- R001 — 便携版更新解锁（本 milestone 直接推进）
- R002 — IsPortable 暴露
- R003 — UI 阻断移除
- R004 — CLI 解锁
- R005-R007 — E2E 测试
- R008 — 推翻 D029

## Scope

### In Scope

- 移除便携版更新阻断代码
- IUpdateService 新增 IsPortable
- E2E 测试脚本（本地 HTTP + 内网 Go 服务器）
- 推翻决策 D029

### Out of Scope / Non-Goals

- 配置文件位置迁移
- Velopack 打包流程改动
- 新增更新功能特性
- GUI 弹窗自动化测试

## Technical Constraints

- Windows 环境，.NET 8，WPF
- Velopack 管理更新生命周期
- E2E 测试需要 vpk、dotnet、python、curl
- 内网测试需要 .env 配置 UPDATE_SERVER_HOST/PORT/TOKEN

## Integration Points

- Velopack UpdateManager — 便携版和安装版的更新核心
- 内网 Go 更新服务器 — 真实更新链路验证
- CLI JSONL 输出 — E2E 自动化测试的输出解析

## Testing Requirements

- 单元测试：UpdateService.IsPortable 属性测试
- E2E 自动化：CLI `update --yes` 本地 HTTP + 内网 Go 服务器
- 手动 UAT：GUI 便携版更新弹窗流程

## Acceptance Criteria

### S01 验收标准
- `UpdateStatus.PortableVersion` 枚举值已移除，编译通过
- `IUpdateService.IsPortable` 可用
- 便携版 GUI 状态栏显示正常更新状态
- 便携版 CLI `update --yes` 不再返回 PORTABLE_NOT_SUPPORTED
- dotnet build 通过

### S02 验收标准
- 本地 HTTP E2E：便携版从 v1.0.0 更新到 v1.1.0 成功
- 内网 Go E2E：便携版更新链路跑通
- 安装版回归：E2E 脚本同时验证安装版更新无回归

## Open Questions

- None — 所有决策已确认
