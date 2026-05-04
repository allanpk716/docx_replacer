---
id: S03
parent: M020
milestone: M020
provides:
  - ["Utils/OpenXmlHelper.cs shared static utility class with 6 OpenXML content control methods", "Eliminated code duplication between DPS and CCP"]
requires:
  []
affects:
  []
key_files:
  - ["Utils/OpenXmlHelper.cs", "Services/DocumentProcessorService.cs", "Services/ContentControlProcessor.cs", "Tests/DocuFiller.Tests.csproj", "Tests/E2ERegression/E2ERegression.csproj"]
key_decisions:
  - ["OpenXmlHelper as static utility class with CommentManager/ILogger passed as parameters to AddProcessingComment", "Unified HasAncestorWithSameTag using direct SdtProperties access (DPS version) over GetControlTag call (CCP version)", "Preserved ProcessContentReplacement in CCP as non-duplicated instance method"]
patterns_established:
  - ["Static utility classes in Utils/ for shared OpenXML operations — no DI registration needed", "AddProcessingComment pattern: pass CommentManager+ILogger as explicit parameters for stateless utility methods"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-04T00:53:44.231Z
blocker_discovered: false
---

# S03: 消除 DocumentProcessorService 和 ContentControlProcessor 重复代码

**将 6 个重复方法提取到共享静态工具类 OpenXmlHelper，两个服务类改为引用共享实现，编译通过且 256 测试全部通过**

## What Happened

## 工作概要

将 DocumentProcessorService（DPS）和 ContentControlProcessor（CCP）之间的 6 个重复私有方法提取到共享静态工具类 `Utils/OpenXmlHelper.cs`，两个服务类改为调用共享实现并删除各自的私有副本。

## 提取的方法

| 方法 | 签名 | 用途 |
|------|------|------|
| `GetControlTag` | `string? GetControlTag(SdtElement)` | 获取内容控件标签值 |
| `ExtractExistingText` | `string ExtractExistingText(SdtElement)` | 提取控件现有文本（用于批注记录旧值） |
| `FindContentContainer` | `OpenXmlElement? FindContentContainer(SdtElement)` | 查找内容容器（SdtContentRun/Block/Cell） |
| `FindAllTargetRuns` | `List<Run> FindAllTargetRuns(SdtElement)` | 查找控件中所有 Run 元素 |
| `AddProcessingComment` | `void AddProcessingComment(WordprocessingDocument, SdtElement, string, string, string, ContentControlLocation, CommentManager, ILogger)` | 添加处理批注 |
| `HasAncestorWithSameTag` | `bool HasAncestorWithSameTag(SdtElement, string)` | 检查嵌套控件祖先是否有相同标签 |

## 关键设计决策

1. **静态类 + 参数传递**：`AddProcessingComment` 需要 `CommentManager` 和 `ILogger`，通过参数传递而非字段注入，保持类为纯静态工具类，无需 DI 注册
2. **统一 HasAncestorWithSameTag**：采用 DPS 版本的直接 `SdtProperties` 访问方式（而非 CCP 版本调用 `GetControlTag`），更高效一致
3. **保留 ProcessContentReplacement**：CCP 中的此方法依赖实例字段 `_safeTextReplacer`，不属于 6 个重复方法，保留在 CCP 中

## 调用点迁移

- **DPS**：10 个调用点迁移到 `OpenXmlHelper.` 前缀
- **CCP**：5 个调用点迁移到 `OpenXmlHelper.` 前缀

## 偏差

- T01 增加了 `using DocuFiller.Services` 导入（CommentManager 访问需要）和测试项目 csproj 链接文件
- T02 修复了 E2ERegression.csproj 缺失 OpenXmlHelper.cs 链接文件的预存构建问题

## Verification

## 验证结果

所有验证均通过：

1. **dotnet build DocuFiller.csproj** — 0 错误 ✅
2. **dotnet build Tests/DocuFiller.Tests.csproj** — 0 错误 ✅
3. **dotnet build Tests/E2ERegression/E2ERegression.csproj** — 0 错误 ✅
4. **dotnet test DocuFiller.Tests** — 229 测试通过 ✅
5. **dotnet test E2ERegression** — 27 测试通过 ✅
6. **grep 零残留** — DPS 和 CCP 源码中无私有方法定义（GetControlTag、ExtractExistingText、FindContentContainer、FindAllTargetRuns、AddProcessingComment、HasAncestorWithSameTag）✅
7. **OpenXmlHelper.cs** — 包含 6 个 public static 方法 ✅
8. **DPS 引用计数** — 10 个 OpenXmlHelper. 调用 ✅
9. **CCP 引用计数** — 5 个 OpenXmlHelper. 调用 ✅

总计：256 测试全部通过（229 + 27），零编译错误，零残留重复实现。

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
