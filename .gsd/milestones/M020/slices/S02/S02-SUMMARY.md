---
id: S02
parent: M020
milestone: M020
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["Services/ContentControlProcessor.cs", "Services/DocumentProcessorService.cs", "Services/Interfaces/IDocumentProcessor.cs", "Tests/Integration/HeaderFooterCommentIntegrationTests.cs", "Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs"]
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
completed_at: 2026-05-03T17:52:13.714Z
blocker_discovered: false
---

# S02: S02: 死代码清理

**安全删除 ContentControlProcessor 中 6 个死方法和 DocumentProcessorService 中 2 段死代码，同步更新接口和测试，256 测试全部通过**

## What Happened

## 任务执行总结

### T01: 删除 ContentControlProcessor 中 6 个死方法
删除了 6 个 private 方法：ReplaceContentInContainer、ReplaceTextDirectly、FindTargetRun、CreateParagraphWithFormattedText(string)、CreateFormattedRuns、CreateFormattedTextElements。这些方法形成死调用链（仅在彼此间调用），是早期替换逻辑被 SafeTextReplacer 替代后的遗留代码。确认 FindContentContainer 和 FindAllTargetRuns 仍被活跃代码使用，保留不动。

### T02: 删除 ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp
- 从 IDocumentProcessor 接口和 DocumentProcessorService 实现中移除 ProcessSingleDocumentAsync
- 从 DocumentProcessorService 中移除未使用的私有方法 GenerateOutputFileNameWithTimestamp
- 更新 CommandValidationTests 中的 StubDocumentProcessor，移除对应桩方法
- 重写 HeaderFooterCommentIntegrationTests 中 3 个测试，改用 ProcessDocumentWithFormattedDataAsync + FormattedCellValue.FromPlainText() 替代旧的 Dictionary<string, object> + ProcessSingleDocumentAsync 模式

### 关键发现
FormattedCellValue.PlainText 是只读计算属性，不能通过对象初始化器设置。使用了 FormattedCellValue.FromPlainText() 工厂方法构造测试数据。

### 验证结果
- grep 确认所有 8 个目标方法名在 worktree 中 0 匹配
- dotnet build: 0 错误（执行期间验证）
- dotnet test: 256 通过, 0 失败（229 + 27）
- 无回归

## Verification

grep 确认 ContentControlProcessor 中 6 个死方法名 0 匹配，ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp 在所有 .cs 文件中 0 匹配（排除 .gsd）。IDocumentProcessor 接口仅保留 ProcessDocumentWithFormattedDataAsync。HeaderFooterCommentIntegrationTests 使用新 API，StubDocumentProcessor 已移除桩方法。dotnet build 执行期间 0 错误，dotnet test 256 通过 0 失败。

## Requirements Advanced

- R004 — 256 tests (229+27) pass after dead code removal, confirming no regression

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
