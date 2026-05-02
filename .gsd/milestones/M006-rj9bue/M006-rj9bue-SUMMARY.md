---
id: M006-rj9bue
title: "真实数据端到端回归测试"
status: complete
completed_at: 2026-04-24T00:36:09.310Z
key_decisions:
  - D013: E2E 测试项目使用 ServiceCollection DI + 条件类型注册实现版本兼容，代替自定义反射工厂
  - D014: csproj 中对已删除文件使用 Condition=Exists(...) 条件编译包含，确保跨版本编译
  - D015: 测试数据路径发现使用向上导航策略，覆盖 worktree 和主仓库场景
  - 页眉页脚验证使用 SDT 标签查找代替全文搜索，更精确
  - 批注验证使用 WordprocessingCommentsPart（OpenXml 3.x API）代替 CommentsPart
key_files:
  - Tests/E2ERegression/E2ERegression.csproj
  - Tests/E2ERegression/ServiceFactory.cs
  - Tests/E2ERegression/TestDataHelper.cs
  - Tests/E2ERegression/InfrastructureTests.cs
  - Tests/E2ERegression/ReplacementCorrectnessTests.cs
  - Tests/E2ERegression/TwoColumnFormatTests.cs
  - Tests/E2ERegression/TableStructureTests.cs
  - Tests/E2ERegression/RichTextFormatTests.cs
  - Tests/E2ERegression/HeaderFooterCommentTests.cs
  - DocuFiller.sln
lessons_learned:
  - csproj 通配符 Include 和条件 Include 可能重叠，需要用 Exclude 避免重复编译项（NETSDK1022）
  - 通过反射 FindType() 在运行时条件注册 DI 服务是实现跨版本兼容的有效模式
  - checkout --detach 后未跟踪文件保留在工作树中，可利用此特性在基准版本上验证新测试
  - 向上导航查找资源的策略需要设置合理的目录层级上限，避免无限循环
---

# M006-rj9bue: 真实数据端到端回归测试

**用真实业务数据（LD68/FD68 IVDR.xlsx + 43 Word 模板）创建独立 E2E 回归测试项目，覆盖替换正确性、表格结构、富文本保留、页眉页脚替换、批注追踪五个维度，通过 d81cd00 基准跨版本验证**

## What Happened

## 概述

M006-rj9bue 创建了独立 E2E 回归测试项目，使用真实业务数据验证文档处理管道的五个关键维度。

## S01: E2E 测试基础设施 + 基本替换验证

创建 `Tests/E2ERegression/` 项目，包含：
- **ServiceFactory**: DI 条件注册机制，通过反射 `FindType()` 自适应注册 IDataParser，兼容 d81cd00（9 参数构造函数）和当前代码（8 参数构造函数）
- **TestDataHelper**: 向上导航查找 `test_data/` 目录，支持 worktree 和主仓库两种场景
- **InfrastructureTests**: 8 个烟雾测试（处理器构建、Excel 解析、模板发现）
- **ReplacementCorrectnessTests**: 7 个替换正确性测试（LD68/FD68 × CE01/CE00/CE06-01）

结果：123/123 测试通过（108 + 15 E2E）

## S02: 五维度验证

新增 4 个测试文件覆盖剩余维度：
- **TwoColumnFormatTests**: FD68 两列格式替换验证
- **TableStructureTests**: 表格行/单元格数量替换前后不变
- **RichTextFormatTests**: 上标格式保留（VerticalPositionValues.Superscript）
- **HeaderFooterCommentTests**: SDT 标签查找页眉页脚，WordprocessingCommentsPart 批注验证

结果：135/135 测试通过（108 + 27 E2E）

## S03: d81cd00 基准跨版本验证

checkout d81cd00 后验证：
- 修复 csproj `Exclude` 防止通配符与条件编译项重叠（NETSDK1022）
- 25/27 通过（2 个预期失败：三列格式在 d81cd00 不支持）
- 切回里程碑分支，135/135 全部通过

## 关键成果

- 独立 xUnit 测试项目，可被 `dotnet test` 发现运行
- 27 个 E2E 测试覆盖 5 个维度
- 跨版本兼容性验证通过
- 所有 135 个测试零失败

## Success Criteria Results

| # | Success Criterion | Status | Evidence |
|---|-------------------|--------|----------|
| 1 | E2E 测试项目创建完成，独立 xUnit 项目可被 dotnet test 发现运行 | ✅ PASS | Tests/E2ERegression/ 含 9 个源文件，已添加到 DocuFiller.sln，`dotnet test` 发现并运行 27 个 E2E 测试 |
| 2 | ServiceFactory 成功构建 DocumentProcessorService 并通过测试 | ✅ PASS | InfrastructureTests_ProcessorBuilds_Succeeds 等 8 个烟雾测试全部通过 |
| 3 | LD68 IVDR.xlsx（三列格式 74 关键词）正确解析 | ✅ PASS | ReplacementCorrectnessTests_LD68_CE01_Succeeds 等测试通过 |
| 4 | FD68 IVDR.xlsx（两列格式 59 关键词）正确解析 | ✅ PASS | TwoColumnFormatTests_FD68_CE06-01_Succeeds 测试通过 |
| 5 | 表格结构未被破坏（替换前后 TableRow/TableCell 数量不变） | ✅ PASS | TableStructureTests 验证 CE01 和 CE06-01 替换前后结构一致 |
| 6 | 富文本上标格式正确保留 | ✅ PASS | RichTextFormatTests 验证 ≥1 run with Superscript |
| 7 | 页眉/页脚中的内容控件被正确替换 | ✅ PASS | HeaderFooterCommentTests 使用 SDT 标签查找验证 |
| 8 | 正文区域的批注被正确添加 | ✅ PASS | HeaderFooterCommentTests 验证 WordprocessingCommentsPart 有 Comment 元素 |
| 9 | dotnet test 全部通过（108 现有 + 新增 E2E） | ✅ PASS | `dotnet test --verbosity minimal`: 135 passed, 0 failed, 0 skipped |

## Definition of Done Results

| Item | Status | Evidence |
|------|--------|----------|
| S01 complete | ✅ | gsd_milestone_status: S01 status=complete, 3/3 tasks done |
| S02 complete | ✅ | gsd_milestone_status: S02 status=complete, 4/4 tasks done |
| S03 complete | ✅ | gsd_milestone_status: S03 status=complete, 2/2 tasks done |
| S01 summary exists | ✅ | S01-SUMMARY.md inlined, verification_result=passed |
| S02 summary exists | ✅ | S02-SUMMARY.md inlined, verification_result=passed |
| S03 summary exists | ✅ | S03-SUMMARY.md inlined, verification_result=passed |
| Cross-slice integration | ✅ | S03 验证 d81cd00 基准版本和当前分支均能构建运行 |
| No verification failures | ✅ | 代码变更 10 文件 1036 插入，所有成功标准满足 |

## Requirement Outcomes

M006-rj9bue 没有改变任何现有需求的状态。所有需求在之前的里程碑中已完成验证（20 个 validated，1 个 out-of-scope）。M006 创建了 E2E 回归测试作为验证层，但没有新增或修改需求定义。

## Deviations

None.

## Follow-ups

None.
