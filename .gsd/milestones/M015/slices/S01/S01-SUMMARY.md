---
id: S01
parent: M015
milestone: M015
provides:
  - ["GitHub 更新模式无 rate limit 限制，底层统一为 SimpleWebSource"]
requires:
  []
affects:
  []
key_files:
  - ["Services/UpdateService.cs", "Services/Interfaces/IUpdateService.cs", "Tests/UpdateServiceTests.cs"]
key_decisions:
  - ["使用 SimpleWebSource + CDN URL 替代 GithubSource，消除 API rate limit"]
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-02T16:50:24.865Z
blocker_discovered: false
---

# S01: GithubSource 替换为 SimpleWebSource

**将 GitHub 更新模式从 GithubSource（API，60次/小时 rate limit）切换为 SimpleWebSource（CDN 直连，无 rate limit），消除匿名用户频率限制**

## What Happened

将 UpdateService 中两处 GithubSource 实例化（构造函数和 ReloadSource）替换为 SimpleWebSource，使用 GitHub CDN 直连 URL `https://github.com/allanpk716/docx_replacer/releases/latest/download/`。EffectiveUpdateUrl 从空字符串变为 CDN URL（GitHub 模式），UpdateSourceType 保持 "GitHub" 不变。内网 HTTP 更新逻辑完全不受影响。

关键变更：
- UpdateService 构造函数：GitHub 模式创建 SimpleWebSource 替代 GithubSource
- UpdateService ReloadSource：热重载时同样使用 SimpleWebSource
- EffectiveUpdateUrl：GitHub 模式返回 CDN URL 而非空字符串
- IUpdateService 接口文档注释更新
- 3 个测试断言从空字符串更新为 CDN URL

验证结果：dotnet build 0 错误，dotnet test 249 测试全通过（27 E2E + 222 单元），代码中无 GithubSource 残留引用。

## Verification

1. `dotnet build` — 0 错误 0 警告 ✅
2. `dotnet test` — 249/249 通过（27 E2E + 222 单元） ✅
3. `powershell Select-String "GithubSource" *.cs` — 无匹配，GithubSource 完全移除 ✅
4. `powershell Select-String "SimpleWebSource" *.cs` — UpdateService.cs 中 7 处引用确认 SimpleWebSource 已替代 ✅

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
