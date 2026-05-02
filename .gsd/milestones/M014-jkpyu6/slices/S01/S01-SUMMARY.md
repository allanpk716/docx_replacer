---
id: S01
parent: M014-jkpyu6
milestone: M014-jkpyu6
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["Services/UpdateService.cs", "Tests/UpdateServiceTests.cs"]
key_decisions:
  - (none)
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-02T15:56:22.144Z
blocker_discovered: false
---

# S01: 去掉 ExplicitChannel，修复更新检测逻辑

**去掉 Velopack ExplicitChannel 使客户端查找 releases.win.json（非 releases.stable.json），修正 HTTP 模式 beta→stable 回退逻辑**

## What Happened

## 变更概述

修复 Velopack ExplicitChannel="stable" 导致 GitHub 更新检测失败的 bug。根本原因：ExplicitChannel="stable" 使 Velopack 查找 releases.stable.json，而服务器（GitHub Releases / 内网 HTTP）实际提供 releases.win.json（OS 默认 channel 为 "win"）。

## 核心修改

### UpdateService.cs（T01 + T02）
1. **去掉 ExplicitChannel 和 AllowVersionDowngrade**：CreateUpdateManager() 和 CreateUpdateManagerForChannel() 不再设置这两个 UpdateOptions 属性，Velopack 回退到 OS 默认 channel "win"，正确匹配 releases.win.json。
2. **新增 _baseUrl 字段**：构造函数和 ReloadSource 中存储不含通道后缀的基础 URL，供回退逻辑使用。
3. **修正 HTTP 模式回退逻辑**：CheckForUpdatesAsync 中 HTTP 模式用 _baseUrl + targetChannel 创建新 SimpleWebSource 实例化 UpdateManager 检查更新；GitHub 模式直接跳过回退（无意义）。
4. **清理注释残留**：将方法注释中提及 ExplicitChannel 的描述替换为实际行为说明。

## 验证结果

- dotnet build：0 错误
- dotnet test：222 个测试全部通过，0 失败
- grep ExplicitChannel/AllowVersionDowngrade：全代码库无匹配

## Verification

1. `dotnet build` — 0 错误，95 个预存在警告
2. `dotnet test` — 222 通过，0 失败（含 UpdateServiceTests 全部通过）
3. `grep -rn "ExplicitChannel|AllowVersionDowngrade" --include="*.cs"` — 无匹配（exit code 1）
4. GitHub 模式跳过通道回退，HTTP 模式回退创建新 SimpleWebSource

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
