---
estimated_steps: 36
estimated_files: 1
skills_used: []
---

# T02: 更新现有测试 + 新增多源切换和便携版检测测试

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

## Inputs

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Tests/UpdateServiceTests.cs`

## Expected Output

- `Tests/UpdateServiceTests.cs`

## Verification

dotnet test --filter "UpdateService" 2>&1 | tail -5
