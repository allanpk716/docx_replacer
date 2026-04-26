---
estimated_steps: 34
estimated_files: 2
skills_used: []
---

# T01: 实现 UpdateService 多源切换 + 便携版检测 + IsInstalled 属性

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

## Inputs

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`

## Expected Output

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`

## Verification

dotnet build -c Release 2>&1 | tail -3

## Observability Impact

日志新增源类型信息（SourceType: HTTP/GitHub），IsInstalled 状态在构造时记录。下游 agent 可通过 UpdateSourceType 和 IsInstalled 属性诊断更新问题。
