---
id: S02
parent: M008-4uyz6m
milestone: M008-4uyz6m
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["appsettings.json", "Services/UpdateService.cs", "Services/Interfaces/IUpdateService.cs", "Tests/UpdateServiceTests.cs", "Tests/DocuFiller.Tests.csproj"]
key_decisions:
  - ["URL 构造使用简单字符串拼接（TrimEnd('/') + '/' + channel + '/'），不使用 Uri 类", "Channel 为空默认 stable，保持向后兼容", "测试项目使用 Microsoft.Extensions.Configuration 10.0.1 而非 8.0.0 避免版本冲突", "UpdateService 添加 internal EffectiveUpdateUrl 属性用于测试验证（无需 InternalsVisibleTo）"]
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-24T23:55:42.154Z
blocker_discovered: false
---

# S02: 客户端通道支持

**UpdateService 现在从 appsettings.json 读取 Channel 配置，构造 {UpdateUrl}/{Channel}/ 的通道感知更新 URL，Channel 为空默认 stable，6 个单元测试覆盖各种配置组合**

## What Happened

## 变更概述

S02 为 DocuFiller 的更新系统增加了 stable/beta 双通道支持。修改了 UpdateService，使其从 appsettings.json 的 Update:Channel 读取通道配置，构造通道感知的更新源 URL（{UpdateUrl}/{Channel}/），传给 Velopack UpdateManager。当 Channel 为空或 null 时默认使用 "stable"，保持向后兼容。

### T01: Channel 配置和 URL 构造
- appsettings.json 的 Update 节添加了 "Channel" 字段（默认空字符串）
- IUpdateService 接口新增只读 Channel 属性
- UpdateService 构造函数读取 "Update:Channel" 配置，空值默认 "stable"
- URL 构造逻辑：`rawUrl.TrimEnd('/') + "/" + channel + "/"` 
- UpdateUrl 为空时 IsUpdateUrlConfigured 返回 false，行为不变

### T02: 单元测试
- 新建 Tests/UpdateServiceTests.cs，包含 6 个 xunit 测试用例
- 覆盖场景：默认 stable、显式 beta、Channel 键缺失、UpdateUrl 为空、有末尾斜杠、无末尾斜杠
- UpdateService.cs 添加 internal EffectiveUpdateUrl 属性用于测试验证
- 测试项目添加 Microsoft.Extensions.Configuration 10.0.1（避免与 Logging.Console 10.0.1 冲突）

### 测试结果
- dotnet build: 0 errors
- dotnet test: 168 tests passed (141 unit + 27 e2e), 0 failures
- 新增 6 个 UpdateServiceTests + 141 原有 = 141 unit tests
- 27 E2E regression tests unchanged

## Verification

## 构建验证
- `dotnet build`: 0 errors
- `dotnet test --filter UpdateServiceTests`: 6 passed, 0 failed
- `dotnet test`: 168 total passed (141 + 27), 0 failed, 0 skipped

## 功能验证
- Channel="beta" 时 URL 为 `{UpdateUrl}/beta/`
- Channel="" 时默认 "stable"，URL 为 `{UpdateUrl}/stable/`
- Channel 配置键缺失时默认 "stable"
- UpdateUrl 为空时 IsUpdateUrlConfigured 返回 false
- URL 末尾斜杠正确处理（TrimEnd 后拼接）

## Requirements Advanced

- R036 — S02 新增 6 个通道测试（168 total），为双通道客户端侧提供测试基础。完整的端到端验证还需 S03/S04。

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
