---
id: T02
parent: S02
milestone: M009-q7p4iu
key_files:
  - Tests/UpdateServiceTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-26T11:08:12.216Z
blocker_discovered: false
---

# T02: 更新 UpdateService 测试：重命名空 URL 测试，新增 4 个多源切换 + 便携版检测测试，共 10 个测试全部通过

**更新 UpdateService 测试：重命名空 URL 测试，新增 4 个多源切换 + 便携版检测测试，共 10 个测试全部通过**

## What Happened

按照任务计划更新和新增 UpdateService 测试，验证 T01 实现的多源切换和便携版检测功能：

1. **重命名和更新 `UpdateUrl_empty_not_configured`** → `UpdateUrl_empty_uses_github_source`：
   - `IsUpdateUrlConfigured` 从 false 改为 true（GitHub Releases 作为备选源始终可用）
   - 新增 `UpdateSourceType` 为 "GitHub" 断言
   - `EffectiveUpdateUrl` 断言为空字符串

2. **更新现有 URL 格式测试**：`UpdateUrl_with_trailing_slash` 和 `UpdateUrl_without_trailing_slash` 新增 `UpdateSourceType` 为 "HTTP" 断言。

3. **新增 `UpdateUrl_empty_uses_stable_channel_for_github`**：验证 UpdateUrl 和 Channel 都为空时，Channel 默认 stable，源类型为 GitHub。

4. **新增 `UpdateUrl_nonempty_uses_http_source`**：验证 UpdateUrl 有值时走 HTTP 源。

5. **新增 `IsInstalled_returns_false_in_test_env`**：验证测试环境中 IsInstalled 为 false（无 Velopack 安装）。

6. **新增 `Both_url_and_github_available_prefers_http`**：验证 URL 有值时优先走 HTTP 而非 GitHub。

所有 10 个测试通过 `dotnet test --filter "UpdateService"` 验证。

## Verification

通过 `dotnet test --filter "UpdateService" --logger "console;verbosity=detailed"` 运行全部 UpdateService 测试，共 10 个测试全部通过（已通过），覆盖了通道默认值、源类型切换、便携版检测等场景。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter "UpdateService" --logger "console;verbosity=detailed" 2>&1 | grep -E "(已通过|已失败|测试总数)"` | 0 | ✅ pass | 8000ms |

## Deviations

无偏差。按计划执行。

## Known Issues

None.

## Files Created/Modified

- `Tests/UpdateServiceTests.cs`
