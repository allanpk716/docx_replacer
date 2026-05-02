---
id: M014-jkpyu6
title: "ExplicitChannel 导致 GitHub 更新检测失败"
status: complete
completed_at: 2026-05-02T16:21:40.401Z
key_decisions:
  - 去掉 ExplicitChannel 而非设置 ExplicitChannel='win'：更简洁，让 Velopack 按操作系统默认 channel 工作，避免未来 OS 移植时再次遗漏
  - GitHub 模式跳过 beta→stable 回退：GitHub Releases 只分发 stable 版本（D028），回退检查无意义
key_files:
  - Services/UpdateService.cs
  - Tests/UpdateServiceTests.cs
lessons_learned:
  - Velopack ExplicitChannel 覆盖 OS 默认 channel 名称（Windows='win'），设置不当会导致查找错误的 releases 文件
  - Velopack UpdateOptions 默认行为（不设置 ExplicitChannel）通常是正确选择，除非有明确的跨 OS channel 统一需求
  - 内网 HTTP 模式和 GitHub 模式的回退逻辑应分开处理：HTTP 需要创建新源，GitHub 只走 stable 通道
---

# M014-jkpyu6: ExplicitChannel 导致 GitHub 更新检测失败

**去掉 Velopack ExplicitChannel 使客户端正确查找 releases.win.json（非 releases.stable.json），修正内网 HTTP 模式 beta→stable 回退逻辑**

## What Happened

## 里程碑概述

修复 Velopack ExplicitChannel="stable" 导致安装版通过 GitHub 源无法检测到更新的 bug。根本原因：ExplicitChannel="stable" 使 Velopack 查找 `releases.stable.json`，而 GitHub Releases 和内网 HTTP 服务器实际提供的文件名为 `releases.win.json`（Windows 默认 OS channel 为 "win"）。

## 变更内容

### S01: 去掉 ExplicitChannel，修复更新检测逻辑

**UpdateService.cs 修改：**
1. 移除 `ExplicitChannel` 和 `AllowVersionDowngrade`：`CreateUpdateManager()` 和 `CreateUpdateManagerForChannel()` 不再设置这两个 UpdateOptions 属性，Velopack 回退到 OS 默认 channel "win"，正确匹配 `releases.win.json`。
2. 新增 `_baseUrl` 字段：构造函数和 `ReloadSource` 中存储不含通道后缀的基础 URL，供回退逻辑使用。
3. 修正 HTTP 模式回退逻辑：`CheckForUpdatesAsync` 中 HTTP 模式用 `_baseUrl + targetChannel` 创建新 `SimpleWebSource` 实例化 `UpdateManager` 检查更新；GitHub 模式直接跳过回退（无意义）。
4. 清理注释残留：方法注释中提及 ExplicitChannel 的描述替换为实际行为说明。

### 验证结果
- dotnet build：0 错误，0 警告
- dotnet test：222 + 27 = 249 个测试全部通过，0 失败
- 全代码库 grep `ExplicitChannel`/`AllowVersionDowngrade`/`releases.stable`：0 匹配

## Success Criteria Results

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | 安装版 v1.3.4 通过 GitHub 源能检测到 v1.4.0 | ✅ COVERED | ExplicitChannel 已全代码库移除（grep → 0 matches）。CreateUpdateManager() 使用裸 UpdateOptions()，Velopack 按 OS 默认 channel "win" 匹配 releases.win.json。 |
| 2 | 内网 HTTP stable 通道能检测到更新 | ✅ COVERED | HTTP 模式构造 _updateUrl = _baseUrl + _channel + "/"，SimpleWebSource 指向 {server}/stable/。_baseUrl 在构造函数和 ReloadSource 中正确维护。 |
| 3 | 内网 HTTP beta→stable 回退逻辑正确创建新 SimpleWebSource | ✅ COVERED | CheckForUpdatesAsync 中 _sourceType == "HTTP" 守卫确保仅 HTTP 模式回退，new SimpleWebSource(_baseUrl + "stable/") 创建新源。GitHub 模式直接跳过回退。 |
| 4 | 所有现有测试通过，无编译错误 | ✅ COVERED | dotnet build: 0 errors; dotnet test: 249 pass, 0 fail; grep 确认全代码库无 ExplicitChannel/AllowVersionDowngrade/releases.stable 残留。 |

## Definition of Done Results

| Item | Status | Evidence |
|------|--------|----------|
| All slices [x] | ✅ | S01 marked complete in ROADMAP.md |
| All slice summaries exist | ✅ | S01-SUMMARY.md present |
| All tasks complete | ✅ | 2/2 tasks complete |
| No cross-slice integration issues | ✅ | Single-slice milestone, changes self-contained in UpdateService.cs |
| Build passes | ✅ | dotnet build: 0 errors |
| Tests pass | ✅ | 249 tests pass, 0 fail |

## Requirement Outcomes

No requirements changed status during this milestone. M014-jkpyu6 is a focused bug fix (removing ExplicitChannel) that corrects existing Velopack integration behavior without adding new capabilities or modifying requirement coverage.

## Deviations

None.

## Follow-ups

None.
