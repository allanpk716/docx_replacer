# M013-ueix00: 更新配置持久化路径修复

**Gathered:** 2026-05-02
**Status:** Ready for planning

## Project Description

将 update-config.json 从 Velopack 安装目录（AppData\Local\DocuFiller\）迁移到用户 home 目录下的独立位置（~/.docx_replacer/），彻底隔离安装/更新生命周期，解决配置被覆盖的 bug。

## Why This Milestone

这是一个反复修过但没彻底解决的 bug。当前 update-config.json 放在 Velopack 安装根目录（`AppData\Local\DocuFiller\`），代码假设"Velopack 更新只替换 `current\` 子目录"。但 Setup.exe 全新安装会重建整个安装目录结构，导致配置被覆盖。自动更新在某些路径下也会触及安装根目录。用户每次重装或更新后内网更新地址丢失，必须重新配置。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 使用 Setup.exe 安装后，之前配置的内网更新地址仍然存在
- 程序自动更新后，内网更新地址不丢失
- CLI 和 GUI 都能正确读取和写入 ~/.docx_replacer/ 下的配置

### Entry point / environment

- Entry point: GUI（更新设置弹窗）和 CLI（update 命令）
- Environment: Windows 桌面，安装版和便携版

## Completion Class

- Contract complete means: UpdateService 和 UpdateSettingsViewModel 都从 ~/.docx_replacer/update-config.json 读写配置
- Integration complete means: GUI 保存配置 → CLI 读取配置 → 两者路径一致
- Operational complete means: Setup 安装和 Velopack 自动更新后配置不丢失

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 配置文件路径为 %USERPROFILE%\.docx_replacer\update-config.json
- GUI 保存内网 URL 后，文件内容正确写入
- 重新读取时（包括 CLI 模式）能正确获取保存的 URL
- 现有单元测试全部通过，新增路径逻辑有测试覆盖

## Architectural Decisions

### 配置文件路径

**Decision:** 使用 %USERPROFILE%\.docx_replacer\update-config.json

**Rationale:** 用户明确要求 ~/.docx_replacer/ 目录。完全独立于 Velopack 安装目录（AppData\Local\DocuFiller\），安装/更新/卸载都不会触及。

**Alternatives Considered:**
- AppData\Roaming\DocuFiller-config\ — 标准 Windows 方案，但用户明确指定了 .docx_replacer
- AppData\Local\DocuFiller\ — 当前方案，已被证明会被覆盖

### 不做旧路径迁移

**Decision:** 不自动迁移旧路径的配置文件

**Rationale:** 用户明确表示不需要迁移，重新配置即可。迁移逻辑增加复杂度且只需执行一次。

**Alternatives Considered:**
- 首次启动检测旧路径并自动迁移 — 用户否决

### 路径计算共享

**Decision:** 在 UpdateService 中保留 GetPersistentConfigPath 方法，UpdateSettingsViewModel 复用相同路径逻辑

**Rationale:** 两个类都需要读写同一个文件，路径计算逻辑必须一致。最简方案是两者使用相同的路径计算逻辑。

## Error Handling Strategy

- 配置文件不存在时返回默认值（null），不抛异常
- 目录不存在时自动创建 ~/.docx_replacer/
- 读写失败时 fallback 到 appsettings.json 配置，log warning
- 与现有错误处理策略一致

## Risks and Unknowns

- 低风险：路径变更可能影响现有测试中 PersistentConfigPath 的 mock — 需要更新测试

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — GetPersistentConfigPath() 当前通过 Velopack 安装结构推断路径
- `ViewModels/UpdateSettingsViewModel.cs` — ReadPersistentConfig() 复制了相同的路径推断逻辑
- `Tests/UpdateServiceTests.cs` — 测试中未直接测试 PersistentConfigPath

## Relevant Requirements

- R056 (新增) — 更新配置文件存储在用户 home 目录，不受安装/更新影响

## Scope

### In Scope

- 修改 GetPersistentConfigPath 返回 ~/.docx_replacer/update-config.json
- 修改 UpdateSettingsViewModel.ReadPersistentConfig 使用相同路径
- 目录不存在时自动创建
- 更新相关测试

### Out of Scope / Non-Goals

- 旧路径配置迁移
- appsettings.json 写回逻辑变更（保持现有行为作为备份）
- 其他配置文件路径变更

## Technical Constraints

- 路径使用 Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) 获取用户 home 目录
- 便携版/开发环境也使用相同路径（不再依赖 Update.exe 存在性判断）
- 文件格式不变：{"UpdateUrl":"...","Channel":"..."}

## Integration Points

- UpdateService 构造函数 → 读取配置
- UpdateSettingsViewModel 构造函数 → 读取配置
- UpdateService.ReloadSource → 写入配置
- CLI UpdateCommand → 通过 DI 使用 UpdateService

## Testing Requirements

- 现有 UpdateServiceTests 全部通过
- 现有 UpdateSettingsViewModelTests 全部通过
- 新增测试验证路径计算逻辑

## Acceptance Criteria

- PersistentConfigPath 返回 %USERPROFILE%\.docx_replacer\update-config.json
- 便携版/开发环境也能正常读写（不依赖 Update.exe）
- 目录不存在时自动创建
- dotnet build 0 errors, dotnet test 全部通过

## Open Questions

- 无（用户已明确所有关键决策）
