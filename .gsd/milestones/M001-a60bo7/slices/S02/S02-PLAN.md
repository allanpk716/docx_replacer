# S02: 测试覆盖验证

**Goal:** 验证三列 Excel 格式的完整测试覆盖：补充 S01 未覆盖的边界场景单元测试（空文件、空白首行、空 ID、ID 去空格、多重重复 ID），新增三列格式的端到端集成测试，确保 dotnet test 全部通过。
**Demo:** dotnet test 全部通过，包含新增的三列解析、格式检测、ID 唯一性校验测试用例

## Must-Haves

- Edge case unit tests added: empty file, blank leading rows, empty ID, single row, whitespace-trimmed ID, multiple duplicate IDs\n- 3-column integration test added proving end-to-end Excel→Word pipeline works correctly\n- dotnet test passes with 0 failures (all existing + new tests)\n- R004 validated: all existing unit and integration tests pass after changes

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: ExcelDataParserService.ParseExcelFileAsync (3-column path), DocumentProcessorService.ProcessDocumentWithFormattedDataAsync\n- New wiring: 3-column Excel integration test in ExcelIntegrationTests\n- What remains: nothing — milestone is end-to-end usable after this slice

## Verification

- Not provided.

## Tasks

- [x] **T01: Add edge case unit tests for 3-column format detection and parsing** `est:30m`
  S01 的 6 个测试覆盖了基本三列解析、ID 去除和重复检测。本任务补充以下边界场景：

1. **空文件**: Excel 无数据行 → DetectExcelFormat 默认 TwoColumn，ParseExcelFileAsync 返回空字典，ValidateExcelFileAsync 报错
2. **空白首行**: 前几行第一列为空，数据从第 N 行开始 → 正确检测格式
3. **三列空 ID**: ID 列为空但关键词/值列有效 → 正确解析，不崩溃
4. **单行三列**: 只有一行数据 → 正确解析
5. **ID 去空格**: ID 值含前后空格（如 "  001  "）→ 重复检测时正确 trim
6. **多重重复 ID**: 多个不同 ID 各有重复 → DuplicateRowIds 包含所有重复 ID
  - Files: `Tests/ExcelDataParserServiceTests.cs`
  - Verify: dotnet test --filter "ExcelDataParserServiceTests" 2>&1 | tail -5

- [x] **T02: Add 3-column integration test and verify full suite passes** `est:30m`
  现有 ExcelIntegrationTests 仅使用两列格式 Excel 测试端到端流程。本任务新增三列格式的集成测试，验证三列 Excel 数据经 ParseExcelFileAsync → ProcessDocumentWithFormattedDataAsync 管道后，输出的 Word 文档包含正确的替换结果且 ID 列值不出现。

最后运行 dotnet test 验证全部测试通过（含新增测试和所有现有测试），完成 R004 验证。
  - Files: `Tests/ExcelIntegrationTests.cs`
  - Verify: dotnet test 2>&1 | tail -5

## Files Likely Touched

- Tests/ExcelDataParserServiceTests.cs
- Tests/ExcelIntegrationTests.cs
