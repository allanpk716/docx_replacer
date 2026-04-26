# S02: UpdateService 多源支持 + 便携版检测

**Goal:** 改造 UpdateService 支持双更新源（HTTP URL + GitHubSource），并检测便携版运行状态。UpdateUrl 非空时走内网 Go 服务器 HTTP URL；UpdateUrl 为空时走 Velopack GitHubSource 指向 GitHub Releases（仅 stable 通道）。便携版运行时 IsInstalled 返回 false。
**Demo:** After this: UpdateUrl 为空时自动走 GitHub Releases 检查更新；UpdateUrl 有值时走内网 Go 服务器；便携版运行时 IsInstalled 返回 false

## Must-Haves

- UpdateService 构造时根据 UpdateUrl 是否为空选择 UpdateManager source
- UpdateUrl 为空时使用 new GithubSource("https://github.com/allanpk716/docx_replacer", prerelease: false)
- UpdateUrl 非空时使用现有 HTTP URL 逻辑（url + channel 后缀）
- IsUpdateUrlConfigured 在有任一更新源可用时返回 true（现在始终为 true，因为 GitHub Releases 永远可用）
- IsInstalled 属性通过 UpdateManager.IsInstalled 暴露
- dotnet build 0 errors，现有测试全部通过
- 新增测试覆盖多源切换逻辑和 IsInstalled 属性

## Proof Level

- This slice proves: contract — 通过单元测试证明多源切换逻辑正确、接口语义正确

## Integration Closure

- Upstream surfaces consumed: Velopack GithubSource API（Velopack.Sources.GithubSource 构造函数）、UpdateManager.IsInstalled 属性
- New wiring introduced: UpdateService.CreateUpdateManager() 根据配置选择 IUpdateSource
- What remains before the milestone is truly usable end-to-end: S03 需要本切片提供的 IsInstalled 和 IsUpdateUrlConfigured 来驱动 GUI 状态栏；S04 需要本切片提供的 CheckForUpdatesAsync 来驱动 CLI update 命令

## Verification

- UpdateService 构造时日志输出选择的更新源类型（"HTTP URL" 或 "GitHub Releases"），便于诊断源切换问题
- IsInstalled 状态在日志中记录，便于诊断便携版检测问题

## Tasks

- [x] **T01: 实现 UpdateService 多源切换 + 便携版检测 + IsInstalled 属性** `est:1h`
  ## Steps

1. **修改 `IUpdateService.cs`**：添加 `bool IsInstalled { get; }` 只读属性，XML 注释说明"当前应用是否为安装版（便携版返回 false）"。同时添加 `string UpdateSourceType { get; }` 只读属性用于暴露当前源类型（"GitHub" 或 "HTTP"），方便测试和下游消费。不要修改现有方法签名。

2. **修改 `UpdateService.cs` 构造函数**：
   - 引入 `using Velopack.Sources;`
   - 添加私有字段 `private readonly IUpdateSource _updateSource;` 和 `private readonly string _sourceType;`
   - 构造函数中判断 UpdateUrl：
     - 非空：`_updateSource = new SimpleWebSource(_updateUrl);`，`_sourceType = "HTTP";`，保持现有 URL + Channel 拼接逻辑
     - 为空：`_updateSource = new GithubSource("https://github.com/allanpk716/docx_replacer", accessToken: null, prerelease: false);`，`_sourceType = "GitHub";`
   - IsUpdateUrlConfigured 改为始终返回 true（因为 GitHub Releases 永远可用作备选）
   - 日志输出更新源类型：`_logger.LogInformation("更新服务初始化，源类型: {SourceType}，通道: {Channel}，更新源: {UpdateUrl}", _sourceType, _channel, _updateUrl ?? "GitHub Releases");`

3. **修改 `UpdateService.cs` 的 `CreateUpdateManager()` 方法**：
   - 从 `new UpdateManager(_updateUrl)` 改为 `new UpdateManager(_updateSource, new UpdateOptions { ExplicitChannel = _channel })`
   - GitHub 源走 stable 通道（ExplicitChannel 为 "stable"），HTTP 源保持现有 channel 配置

4. **实现 `IsInstalled` 属性**：
   - 添加 `public bool IsInstalled` 属性
   - 实现方式：在 `CreateUpdateManager()` 创建的实例上读取 `IsInstalled` 属性
   - 考虑缓存策略：构造时检测一次并缓存到私有字段，避免每次调用都创建 UpdateManager
   - 如果 Velopack 未安装（便携版/开发环境），`UpdateManager.IsInstalled` 返回 false

5. **实现 `UpdateSourceType` 属性**：
   - 直接返回 `_sourceType` 字段

6. **添加 `using Velopack.Sources;` 到 UpdateService.cs**

## Must-Haves

- [ ] IUpdateService 接口新增 IsInstalled 和 UpdateSourceType 属性
- [ ] UpdateService 构造函数根据 UpdateUrl 选择 IUpdateSource（GithubSource vs SimpleWebSource）
- [ ] CreateUpdateManager() 使用 IUpdateSource 构造函数重载
- [ ] IsUpdateUrlConfigured 始终返回 true
- [ ] IsInstalled 通过 UpdateManager.IsInstalled 检测并缓存
- [ ] dotnet build -c Release 0 errors

## Important Constraints

- 不要修改现有方法签名（CheckForUpdatesAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart）
- GithubSource 构造函数：prerelease 必须为 false（D028 决策：GitHub 只发 stable）
- 每个 API 方法创建独立 UpdateManager 实例（MEM069 记忆）
- 注意 Velopack 0.0.1298 的 ApplyUpdatesAndRestart 需要传入 VelopackAsset 参数（MEM072 记忆）
- Test projects 已引用 Velopack 包（MEM068 记忆），不需要额外添加
  - Files: `Services/UpdateService.cs`, `Services/Interfaces/IUpdateService.cs`
  - Verify: dotnet build -c Release 2>&1 | tail -3

- [x] **T02: 更新现有测试 + 新增多源切换和便携版检测测试** `est:45m`
  ## Steps

1. **更新 `UpdateUrl_empty_not_configured` 测试**：
   - `IsUpdateUrlConfigured` 现在应返回 true（不是 false），因为 GitHub Releases 作为备选源始终可用
   - 改名为 `UpdateUrl_empty_uses_github_source` 更贴切
   - 断言 `service.IsUpdateUrlConfigured` 为 true
   - 断言 `service.UpdateSourceType` 为 "GitHub"

2. **更新 `UpdateUrl_with_trailing_slash` 和 `UpdateUrl_without_trailing_slash` 测试**：
   - 这两个测试 UpdateUrl 有值，源类型应为 HTTP
   - 断言 `service.UpdateSourceType` 为 "HTTP"
   - 其余断言保持不变

3. **新增 `UpdateUrl_empty_uses_stable_channel_for_github` 测试**：
   - UpdateUrl 为空，Channel 为空或 "stable"
   - 验证 `service.Channel` 为 "stable"
   - 验证 `service.UpdateSourceType` 为 "GitHub"

4. **新增 `UpdateUrl_nonempty_uses_http_source` 测试**：
   - UpdateUrl 为 "http://server/updates"
   - 验证 `service.UpdateSourceType` 为 "HTTP"
   - 验证 `service.IsUpdateUrlConfigured` 为 true

5. **新增 `IsInstalled_returns_false_in_test_env` 测试**：
   - 创建 UpdateService 实例
   - 在测试环境中（非安装版）`IsInstalled` 应返回 false
   - 这是预期行为：测试环境没有 Velopack 安装

6. **新增 `Both_url_and_github_available` 测试**：
   - UpdateUrl 有值时，即使 GitHub 也可用，也应使用 HTTP 源
   - 验证 `UpdateSourceType` 为 "HTTP"

7. **运行所有 UpdateService 测试**：
   - `dotnet test --filter "UpdateService"` 确保全部通过

## Must-Haves

- [ ] 所有现有 UpdateService 测试更新后通过
- [ ] 新增至少 4 个测试覆盖多源切换
- [ ] IsInstalled 测试覆盖
- [ ] dotnet test --filter "UpdateService" 全部通过

## Important Notes

- 测试中无法验证 GithubSource 是否被正确构造（因为需要真实网络），但可以通过 UpdateSourceType 属性间接验证源选择逻辑
- IsInstalled 在测试环境中始终为 false（无 Velopack 安装），这是正确行为
- 不要删除现有通过的测试，而是更新断言
  - Files: `Tests/UpdateServiceTests.cs`
  - Verify: dotnet test --filter "UpdateService" 2>&1 | tail -5

## Files Likely Touched

- Services/UpdateService.cs
- Services/Interfaces/IUpdateService.cs
- Tests/UpdateServiceTests.cs
