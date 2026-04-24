---
id: S03
parent: M006-rj9bue
milestone: M006-rj9bue
provides:
  - ["E2E 测试项目跨 d81cd00 和当前分支均能构建运行"]
requires:
  []
affects:
  []
key_files:
  - ["Tests/E2ERegression/E2ERegression.csproj"]
key_decisions:
  - ["在 csproj 通配符 Compile Include 上添加 Exclude 防止与条件 Include 重叠导致 NETSDK1022 错误"]
patterns_established:
  - ["通过反射 FindType() 条件注册 DI 服务实现跨版本兼容"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-24T00:33:33.484Z
blocker_discovered: false
---

# S03: d81cd00 基准跨版本验证

**验证 E2E 回归测试在 d81cd00 基准版本（9 参数构造函数）和当前里程碑分支（8 参数构造函数）上均能构建通过，证明了跨版本兼容性**

## What Happened

## 概述

S03 是 M006 的最终验证切片，目标是在 d81cd00 基准版本和当前里程碑分支上均能成功构建和运行 E2E 回归测试，证明跨版本兼容性。

## T01: d81cd00 基准版本构建和测试

E2E 测试项目创建于 d81cd00 之后的提交（82177a8/98a59b9），因此 checkout d81cd00 不包含 E2E 测试文件。执行方案是先将 E2E 源文件复制到 d81cd00 工作树上。

构建过程中发现 NETSDK1022 重复编译项错误：`IDataParser.cs` 同时被条件 `<Compile Include="...IDataParser.cs" Condition="Exists(...)">` 和通配符 `<Compile Include="..\..\Services\Interfaces\*.cs">` 包含。通过在通配符 include 上添加 `Exclude="..\..\Services\Interfaces\IDataParser.cs"` 修复。

**d81cd00 结果：** 构建成功（0 错误），25/27 测试通过。2 个失败测试（`ExcelParsing_LD68_ThreeColumnFormat` 和 `ExcelParsing_BothFormats_HaveCommonKeywords`）是预期的 — d81cd00 的 ExcelDataParserService 不支持三列格式。所有 7 个替换正确性测试和其他基础设施测试均通过。

## T02: 里程碑分支全量测试验证

切回 milestone/M006-rj9bue 分支，执行 `dotnet build`（0 错误）和 `dotnet test`（135/135 通过，108 DocuFiller.Tests + 27 E2ERegression）。计划中估算 123 个测试，实际 135 个是因为 E2E 测试对 LD68 和 FD68 两个数据源分别运行。

## 关键发现

1. **ServiceFactory 的条件注册机制有效**：通过反射 FindType() 在运行时自适应注册 IDataParser，d81cd00 上 9 参数构造函数、当前分支上 8 参数构造函数，DI 容器自动解析正确版本。
2. **csproj 条件编译需要 Exclude 配合**：通配符 include 和条件 include 可能重叠，需要用 Exclude 避免重复。
3. **文档处理管道完全兼容**：7 个替换正确性测试在两个版本上均通过，证明 SafeTextReplacer、ContentControlManager 等核心组件的接口行为未变。

## Verification

## 验证结果

### d81cd00 基准版本
1. `dotnet build Tests/E2ERegression/E2ERegression.csproj` — ✅ 成功（0 错误）
2. `dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal` — ⚠️ 25/27 通过（2 个预期失败：三列格式不支持）
3. 所有 7 个替换正确性测试通过 — ✅ 文档处理管道兼容

### 里程碑分支
1. `dotnet build` — ✅ 成功（0 错误）
2. `dotnet test --verbosity normal` — ✅ 135/135 通过（108 + 27）
3. 工作树状态干净 — ✅ 无残留编译产物

### 代码修改
唯一修改：`Tests/E2ERegression/E2ERegression.csproj` — 添加 Exclude 防止 IDataParser.cs 重复编译

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
