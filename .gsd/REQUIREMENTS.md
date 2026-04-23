# Requirements

This file is the explicit capability and coverage contract for the project.

## Validated

### R001 — Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Class: core-capability
- Status: validated
- Description: Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Why it matters: 允许用户在 Excel 中为每行添加人类可读的标签，方便维护大型数据表
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: 3-column Excel (ID|#keyword#|value) correctly parsed via DetectExcelFormat heuristic. 6 new xunit tests prove: 3-col parsing returns correct keyword-value pairs, ID column excluded from results, and format detection distinguishes 2-col vs 3-col. All 61 tests pass.
- Notes: 检测依据为第一行第一列内容是否匹配 #xxx# 格式

### R002 — 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Class: core-capability
- Status: validated
- Description: 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Why it matters: 防止数据表维护错误导致混淆
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: ID uniqueness validation implemented in ValidateExcelFileAsync using HashSet tracking. Duplicate IDs populate ExcelFileSummary.DuplicateRowIds and add errors to ExcelValidationResult.Errors with specific duplicate ID names. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms behavior.
- Notes: 两列模式不触发此校验

### R003 — 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Class: constraint
- Status: validated
- Description: 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Why it matters: 已有用户和数据模板不能因为新功能被破坏
- Source: inferred
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: M001-a60bo7/S02
- Validation: All 3 pre-existing ExcelDataParserServiceTests pass unchanged (ParseExcelFileAsync_ValidFile_ReturnsData, ValidateExcelFileAsync_ValidFile_PassesValidation, ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation). Full 61-test suite passes with zero regressions. 2-column parsing and validation behavior is completely unchanged.
- Notes: 硬性约束，零回归

### R004 — 所有现有单元测试和集成测试在改动后继续通过
- Class: quality-attribute
- Status: validated
- Description: 所有现有单元测试和集成测试在改动后继续通过
- Why it matters: 回归安全的底线
- Source: inferred
- Primary owning slice: M001-a60bo7/S02
- Supporting slices: none
- Validation: Full test suite passes with 71 tests (0 failures, 0 skipped). Includes 12 new edge case unit tests for 3-column format (empty file, blank first rows, empty ID, single row, ID trim, multi-duplicate) and 1 new end-to-end integration test proving 3-column Excel→Word pipeline works correctly. All pre-existing tests remain green with zero regressions.
- Notes: 包含新增的三列解析和唯一性校验测试

## Traceability

| ID | Class | Status | Primary owner | Supporting | Proof |
|---|---|---|---|---|---|
| R001 | core-capability | validated | M001-a60bo7/S01 | none | 3-column Excel (ID|#keyword#|value) correctly parsed via DetectExcelFormat heuristic. 6 new xunit tests prove: 3-col parsing returns correct keyword-value pairs, ID column excluded from results, and format detection distinguishes 2-col vs 3-col. All 61 tests pass. |
| R002 | core-capability | validated | M001-a60bo7/S01 | none | ID uniqueness validation implemented in ValidateExcelFileAsync using HashSet tracking. Duplicate IDs populate ExcelFileSummary.DuplicateRowIds and add errors to ExcelValidationResult.Errors with specific duplicate ID names. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms behavior. |
| R003 | constraint | validated | M001-a60bo7/S01 | M001-a60bo7/S02 | All 3 pre-existing ExcelDataParserServiceTests pass unchanged (ParseExcelFileAsync_ValidFile_ReturnsData, ValidateExcelFileAsync_ValidFile_PassesValidation, ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation). Full 61-test suite passes with zero regressions. 2-column parsing and validation behavior is completely unchanged. |
| R004 | quality-attribute | validated | M001-a60bo7/S02 | none | Full test suite passes with 71 tests (0 failures, 0 skipped). Includes 12 new edge case unit tests for 3-column format (empty file, blank first rows, empty ID, single row, ID trim, multi-duplicate) and 1 new end-to-end integration test proving 3-column Excel→Word pipeline works correctly. All pre-existing tests remain green with zero regressions. |

## Coverage Summary

- Active requirements: 0
- Mapped to slices: 0
- Validated: 4 (R001, R002, R003, R004)
- Unmapped active requirements: 0
