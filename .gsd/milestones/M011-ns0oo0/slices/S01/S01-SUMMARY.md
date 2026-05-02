---
id: S01
parent: M011-ns0oo0
milestone: M011-ns0oo0
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["ViewModels/UpdateSettingsViewModel.cs", "Tests/UpdateSettingsViewModelTests.cs", "Tests/DocuFiller.Tests.csproj", "Tests/UpdateServiceTests.cs"]
key_decisions:
  - ["UpdateSettingsViewModel 直接从 IConfiguration 读取 UpdateUrl/Channel 原始值，不再从 IUpdateService.EffectiveUpdateUrl 剥离通道路径（决策 D033）"]
patterns_established:
  - ["IConfiguration 注入到 ViewModel 用于读取 appsettings.json 原始配置值，避免从运行时服务属性反推配置"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-30T06:06:07.623Z
blocker_discovered: false
---

# S01: 修复更新设置 URL 回显

**UpdateSettingsViewModel 改为直接从 IConfiguration 读取 UpdateUrl/Channel 原始值，替换脆弱的 EffectiveUpdateUrl 剥离逻辑，新增 11 个单元测试覆盖所有场景**

## What Happened

## 变更概述

UpdateSettingsViewModel 构造函数原本从 `IUpdateService.EffectiveUpdateUrl` 剥离通道路径后缀（如 "/stable/"）来恢复用户输入的原始 URL。这种剥离逻辑在尾部斜杠、大小写等边界条件下容易出错。按照决策 D033，将数据源改为直接从 `IConfiguration["Update:UpdateUrl"]` 读取原始值。

## 具体实现

1. **UpdateSettingsViewModel 构造函数**新增 `IConfiguration` 参数，从 `configuration["Update:UpdateUrl"]` 直接读取原始 URL（空值/null 返回空字符串，非空值 Trim），从 `configuration["Update:Channel"]` 读取通道（为空时 fallback 到 `_updateService.Channel`）
2. **删除旧的 EffectiveUpdateUrl 剥离逻辑**（约 20 行 if/else + EndsWith/Substring 代码）
3. **DI 注册无需修改**：IConfiguration 已在 DI 中注册为 Singleton，UpdateSettingsViewModel 已注册为 Transient，DI 自动解析新参数
4. **11 个单元测试**覆盖：HTTP URL 回显、GitHub 空/null URL、Channel 读取、Channel 空/null fallback、SourceTypeDisplay（HTTP/GitHub）、Trim、Channels 集合、null IConfiguration 防御

## 附带修复

- `Tests/DocuFiller.Tests.csproj` 添加 `UseWPF=true` 和 Moq 包引用（测试编译需要）
- `Tests/UpdateServiceTests.cs` 修复 `using System.IO` 缺失问题（UseWPF 引入后类型冲突）

## 验证结果

- `dotnet build` — 0 错误 0 警告
- `dotnet test` — 203/203 通过（176 DocuFiller.Tests + 27 E2ERegression）
- 旧 EffectiveUpdateUrl 剥离逻辑已完全移除（grep 确认 0 匹配）

## Verification

1. `dotnet build --verbosity minimal` — 0 错误 0 警告
2. `dotnet test --filter "FullyQualifiedName~UpdateSettingsViewModelTests"` — 11/11 通过
3. `dotnet test --no-restore --verbosity minimal` — 203/203 通过（176+27）
4. `grep -n "EffectiveUpdateUrl" ViewModels/UpdateSettingsViewModel.cs` — 0 匹配，旧逻辑完全移除
5. 代码审查确认：构造函数从 IConfiguration 直接读取，null 防御完整

## Requirements Advanced

None.

## Requirements Validated

- R047 — UpdateSettingsViewModel 构造函数直接从 IConfiguration[Update:UpdateUrl] 读取原始值，11 个单元测试覆盖所有场景，203/203 测试通过

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
