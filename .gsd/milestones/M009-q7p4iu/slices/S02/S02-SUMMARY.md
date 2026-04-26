---
id: S02
parent: M009-q7p4iu
milestone: M009-q7p4iu
provides:
  - ["IUpdateService.IsInstalled 属性（便携版检测）", "IUpdateService.UpdateSourceType 属性（源类型暴露）", "IsUpdateUrlConfigured 始终 true 的语义", "GithubSource 备选路径（stable 通道）", "10 个 UpdateService 单元测试覆盖全部切换路径"]
requires:
  []
affects:
  - ["S03", "S04"]
key_files:
  - ["Services/Interfaces/IUpdateService.cs", "Services/UpdateService.cs", "Tests/UpdateServiceTests.cs"]
key_decisions:
  - ["IsUpdateUrlConfigured 始终返回 true（GitHub Releases 永远可用）", "IsInstalled 构造时一次性检测并缓存", "GithubSource prerelease: false 遵循 D028 决策"]
patterns_established:
  - ["UpdateService 构造时选择 IUpdateSource 的多源切换模式", "IsInstalled 构造时缓存避免重复创建 UpdateManager", "UpdateSourceType 属性暴露源类型给下游消费方"]
observability_surfaces:
  - ["构造时 Information 日志输出源类型/通道/URL/IsInstalled 状态"]
drill_down_paths:
  - [".gsd/milestones/M009-q7p4iu/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M009-q7p4iu/slices/S02/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-26T11:08:58.102Z
blocker_discovered: false
---

# S02: UpdateService 多源支持 + 便携版检测

**UpdateService 支持双更新源自动切换（HTTP URL 优先，GithubSource 备选），便携版 IsInstalled 检测，10 个测试覆盖全部路径**

## What Happened

S02 实现了 UpdateService 的多源更新支持和便携版检测功能，为 M009 的 GUI 状态栏提示（S03）和 CLI update 命令（S04）提供基础设施。

T01 修改 IUpdateService 接口新增 IsInstalled 和 UpdateSourceType 两个只读属性，改造 UpdateService 构造函数实现双源切换逻辑：UpdateUrl 非空时使用 SimpleWebSource（内网 Go 服务器），为空时使用 GithubSource（GitHub Releases，prerelease: false，stable 通道）。IsInstalled 通过临时 UpdateManager 实例在构造时一次性检测并缓存。IsUpdateUrlConfigured 改为始终返回 true，因为 GitHub Releases 作为备选源永远可用。

T02 更新了所有现有测试并新增 4 个测试：空 URL 走 GitHub 源、空 URL 默认 stable 通道、非空 URL 走 HTTP 源、测试环境 IsInstalled 为 false、URL 有值时优先走 HTTP。共 10 个测试全部通过。

## Verification

- dotnet build -c Release: 0 errors（T01 验证）
- dotnet test --filter "UpdateService": 10/10 tests passed（T02 验证）
- 代码审查确认：GithubSource prerelease: false（D028 决策）、IsUpdateUrlConfigured 始终 true、CreateUpdateManager 使用 IUpdateSource 重载、IsInstalled 构造时缓存
- 零回归：现有方法签名未修改，接口只新增属性

## Requirements Advanced

- R043 — 现有方法签名零修改，仅新增 IsInstalled 和 UpdateSourceType 属性

## Requirements Validated

- R039 — 10 个单元测试通过：UpdateUrl 为空走 GithubSource、非空走 SimpleWebSource、Channel 默认 stable、接口签名未改

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

T01 计划中 IsInstalled 检测使用 `using var tempManager`，但 Velopack 0.0.1298 的 UpdateManager 未实现 IDisposable，改为普通变量赋值。

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
