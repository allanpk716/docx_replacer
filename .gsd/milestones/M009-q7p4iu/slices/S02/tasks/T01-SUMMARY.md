---
id: T01
parent: S02
milestone: M009-q7p4iu
key_files:
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
key_decisions:
  - UpdateManager 在 Velopack 0.0.1298 中未实现 IDisposable，IsInstalled 检测时不能使用 using var
duration: 
verification_result: passed
completed_at: 2026-04-26T11:05:48.182Z
blocker_discovered: false
---

# T01: 实现 UpdateService 多源切换（HTTP URL + GitHubSource）并添加便携版 IsInstalled 检测

**实现 UpdateService 多源切换（HTTP URL + GitHubSource）并添加便携版 IsInstalled 检测**

## What Happened

修改 IUpdateService 接口新增 IsInstalled 和 UpdateSourceType 属性，修改 UpdateService 实现双更新源支持：

1. **接口扩展**：IUpdateService 新增 `bool IsInstalled { get; }` 和 `string UpdateSourceType { get; }` 属性，更新 XML 注释说明语义。

2. **多源切换**：UpdateService 构造函数根据 UpdateUrl 配置自动选择更新源：
   - UpdateUrl 非空 → `SimpleWebSource`（内网 Go 服务器），`_sourceType = "HTTP"`
   - UpdateUrl 为空 → `GithubSource("https://github.com/allanpk716/docx_replacer", prerelease: false)`，`_sourceType = "GitHub"`

3. **CreateUpdateManager 改造**：从 `new UpdateManager(_updateUrl)` 改为 `new UpdateManager(_updateSource, new UpdateOptions { ExplicitChannel = _channel })`，使用 IUpdateSource 重载。

4. **IsInstalled 检测**：构造时通过临时 UpdateManager 实例检测 IsInstalled 并缓存到 `_isInstalled` 字段，便携版和开发环境返回 false。检测失败时默认 false 并记录警告日志。

5. **IsUpdateUrlConfigured 始终返回 true**：GitHub Releases 作为备选源永远可用。

6. **日志增强**：构造时输出源类型（SourceType: HTTP/GitHub）、通道、更新源 URL 和 IsInstalled 状态。

注意：UpdateManager 在 Velopack 0.0.1298 中未实现 IDisposable，不能使用 `using var`。

## Verification

通过 `dotnet build -c Release` 编译验证，0 错误。所有变更满足任务计划的 Must-Haves 清单：
- IUpdateService 接口新增 IsInstalled 和 UpdateSourceType ✓
- UpdateService 构造函数根据 UpdateUrl 选择 IUpdateSource ✓
- CreateUpdateManager() 使用 IUpdateSource 构造函数重载 ✓
- IsUpdateUrlConfigured 始终返回 true ✓
- IsInstalled 通过 UpdateManager.IsInstalled 检测并缓存 ✓

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -c Release 2>&1 | tail -5` | 0 | ✅ pass | 2490ms |

## Deviations

计划中 IsInstalled 检测使用 `using var tempManager`，但 Velopack 0.0.1298 的 UpdateManager 未实现 IDisposable，改为普通变量赋值。

## Known Issues

None.

## Files Created/Modified

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
