# S01: 三列格式解析与 ID 唯一性校验 — UAT

**Milestone:** M001-a60bo7
**Written:** 2026-04-23T04:07:00.787Z

# S01: 三列格式解析与 ID 唯一性校验 — UAT

**Milestone:** M001-a60bo7
**Written:** 2026-04-23

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a backend parsing/validation service with no UI. Correctness is fully verified through automated unit tests that exercise all format detection, parsing, and validation paths.

## Preconditions

- .NET 8 SDK installed
- Project builds successfully (`dotnet build`)
- Test project references are resolved

## Smoke Test

Run `dotnet test Tests/DocuFiller.Tests.csproj --filter "FullyQualifiedName~ExcelDataParserServiceTests" -v n` — all 9 tests should pass.

## Test Cases

### 1. 三列 Excel 解析正确跳过 ID 列

1. 运行测试 `ParseExcelFileAsync_ThreeColumnFormat_SkipsIdAndParsesCorrectly`
2. **Expected:** 解析结果仅包含 keyword→value 对，不包含 ID 列数据。关键词来自第 2 列，值来自第 3 列。

### 2. ID 列不出现在解析结果中

1. 运行测试 `ParseExcelFileAsync_ThreeColumnFormat_DoesNotIncludeIdColumn`
2. **Expected:** 结果字典中不存在任何键匹配 ID 列的值（如 "Row1"、"Row2" 等）。

### 3. 三列格式验证通过

1. 运行测试 `ValidateExcelFileAsync_ThreeColumnFormat_PassesValidation`
2. **Expected:** `ExcelValidationResult.IsValid == true`，`Errors` 为空。

### 4. ID 重复时验证报错

1. 运行测试 `ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds`
2. **Expected:** `ExcelValidationResult.IsValid == false`，`Errors` 包含重复 ID 的具体名称，`ExcelFileSummary.DuplicateRowIds` 不为空。

### 5. 两列格式向后兼容 — 解析

1. 运行测试 `ParseExcelFileAsync_TwoColumnFormat_UnchangedBehavior`
2. **Expected:** 解析结果与修改前完全一致，keyword→value 正确映射。

### 6. 两列格式向后兼容 — 验证

1. 运行测试 `ValidateExcelFileAsync_TwoColumnFormat_NoDuplicateRowIds`
2. **Expected:** 两列模式下不触发 ID 唯一性校验，`DuplicateRowIds` 为空。

### 7. 全量回归测试

1. 运行 `dotnet test Tests/DocuFiller.Tests.csproj -v n`
2. **Expected:** 全部 61 个测试通过，0 失败。

## Edge Cases

### 首行为空时格式检测

1. `DetectExcelFormat` 方法跳过空行，查找第一个非空行
2. **Expected:** 正确识别格式，不会因为空首行而误判

## Failure Signals

- 任何 ExcelDataParserServiceTests 测试失败
- 全量测试套件出现失败（回归）
- 编译错误

## Not Proven By This UAT

- 真实 .xlsx 文件的端到端处理（需要手动测试或集成测试）
- 包含空行、特殊字符等边界情况的 Excel 文件
- UI 层的格式提示或错误展示

## Notes for Tester

所有测试均为自动化单元测试，使用 EPPlus 在内存中创建 Excel 文件。无需准备外部测试数据文件。
