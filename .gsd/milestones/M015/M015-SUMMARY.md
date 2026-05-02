---
id: M015
title: "GitHub 更新源从 API 切换到 CDN 直连"
status: complete
completed_at: 2026-05-02T16:52:31.407Z
key_decisions:
  - 使用 SimpleWebSource + CDN URL 替代 GithubSource，消除 API rate limit
  - 保持 UpdateSourceType 为 'GitHub' 不变，对上层透明
key_files:
  - Services/UpdateService.cs
  - Services/Interfaces/IUpdateService.cs
  - Tests/UpdateServiceTests.cs
lessons_learned:
  - Velopack 的 GithubSource 使用 GitHub API 查询 releases，匿名调用受 60 次/小时 rate limit 限制；SimpleWebSource 直接下载 CDN 文件无此限制
  - 替换更新源实现时，保持 UpdateSourceType 等上层枚举不变可确保 UI 和设置逻辑完全不受影响
---

# M015: GitHub 更新源从 API 切换到 CDN 直连

**将 GitHub 更新模式从 GithubSource（API，60次/小时 rate limit）切换为 SimpleWebSource（CDN 直连，无 rate limit），消除匿名用户频率限制**

## What Happened

M015 完成了将 GitHub 更新源从 Velopack GithubSource（GitHub API，60次/小时匿名 rate limit）切换为 SimpleWebSource（CDN 直连 /releases/latest/download/，无 rate limit）。

核心变更在 UpdateService.cs 中：
- 构造函数和 ReloadSource 方法中，GitHub 模式下创建 SimpleWebSource 替代 GithubSource
- 使用 CDN URL `https://github.com/allanpk716/docx_replacer/releases/latest/download/` 作为下载源
- EffectiveUpdateUrl 从空字符串变为 CDN URL（GitHub 模式）
- UpdateSourceType 保持 "GitHub" 不变，对上层透明

内网 HTTP 更新模式完全不受影响，行为不变。

测试更新：3 个测试断言从验证空字符串更新为验证 CDN URL。

验证结果：dotnet build 0 错误，dotnet test 249/249 通过，代码中无 GithubSource 残留。

## Success Criteria Results

### Success Criteria Results

| # | Criterion | Verdict | Evidence |
|---|-----------|---------|----------|
| 1 | GitHub 模式使用 SimpleWebSource 直接下载 release 资产，不调用 GitHub API | ✅ PASS | UpdateService.cs 中 7 处 SimpleWebSource 引用，0 处 GithubSource 引用（grep 验证） |
| 2 | 内网 HTTP 模式更新逻辑完全不受影响，行为不变 | ✅ PASS | slice summary 明确声明不受影响；仅修改 GitHub 分支代码路径 |
| 3 | dotnet build 无错误，dotnet test 全部通过 | ✅ PASS | build 0 错误 0 警告，test 249/249 通过（27 E2E + 222 单元） |
| 4 | 代码中不再引用 GithubSource 类 | ✅ PASS | `grep -rn "GithubSource" --include="*.cs"` 返回 0 匹配 |

## Definition of Done Results

### Definition of Done Results

| # | Item | Verdict | Evidence |
|---|------|---------|----------|
| 1 | All slices complete | ✅ PASS | S01: complete (1/1 tasks) — gsd_milestone_status confirms |
| 2 | All slice summaries exist | ✅ PASS | .gsd/milestones/M015/slices/S01/S01-SUMMARY.md exists |
| 3 | Cross-slice integration | ✅ PASS | Single slice milestone, no cross-slice dependencies |

## Requirement Outcomes

### Requirement Outcomes

No requirements changed status during M015. The milestone modified existing implementation code (UpdateService) without introducing new requirements or validating existing ones.

## Deviations

None.

## Follow-ups

None.
