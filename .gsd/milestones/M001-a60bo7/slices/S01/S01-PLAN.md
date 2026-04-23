# S01: 三列格式解析与 ID 唯一性校验

**Goal:** ExcelDataParserService 自动检测两列/三列格式，三列模式下跳过 ID 列正确解析关键词和值，验证 ID 唯一性并在重复时报错，两列模式行为零变化。
**Demo:** 三列 Excel 正确解析且 ID 列不影响替换结果；ID 重复时验证报错提示具体重复项；旧两列 Excel 解析和验证行为零变化

## Must-Haves

- 三列 Excel（ID | #关键词# | 值）解析结果与等效两列 Excel 完全一致，ID 列不参与替换
- 格式检测准确区分两列和三列
- ID 重复时 ExcelValidationResult.IsValid == false 且 Errors 包含重复 ID
- 旧两列 Excel 解析和验证行为零变化（dotnet test 现有测试全部通过）

## Proof Level

- This slice proves: contract — 自动化单元测试覆盖三列解析、格式检测、ID 唯一性三个核心场景

## Integration Closure

- Upstream surfaces consumed: IExcelDataParser 接口（不修改）、IFileService（不修改）
- New wiring: 无新入口，仅在 ExcelDataParserService 内部新增 DetectExcelFormat 私有方法
- What remains: S02 补充更全面的测试覆盖用例

## Verification

- Signals added: 日志中输出检测到的格式类型（两列/三列），ID 重复时日志记录具体重复项
- How a future agent inspects: 查看日志输出中的格式检测信息和 ID 校验结果
- Failure state exposed: ExcelValidationResult.Errors 包含具体重复 ID 列表，ExcelFileSummary.DuplicateRowIds 记录重复项

## Tasks

- [x] **T01: Add DuplicateRowIds model field and 3-column format detection + parsing** `est:45m`
  Add DuplicateRowIds field to ExcelFileSummary model. Add private DetectExcelFormat method to ExcelDataParserService that reads the first non-empty row's first column — if it matches #xxx# it's 2-column mode, otherwise 3-column. Modify ParseExcelFileAsync to use the detected format: in 3-column mode read col 2 as keyword and col 3 as value, skipping col 1 (ID). Ensure 2-column mode behavior is unchanged.
  - Files: `Models/ExcelFileSummary.cs`, `Services/ExcelDataParserService.cs`
  - Verify: dotnet test --filter "ExcelDataParserServiceTests" --no-build does not exist yet; instead run: dotnet build && dotnet test --filter "FullyQualifiedName~ExcelDataParserServiceTests"

- [x] **T02: Add ID uniqueness validation and basic 3-column tests** `est:1h`
  Modify ValidateExcelFileAsync to: (1) use DetectExcelFormat to determine format, (2) in 3-column mode track IDs from column 1 in a HashSet, (3) detect duplicates and populate ExcelFileSummary.DuplicateRowIds + add errors to ExcelValidationResult, (4) validate keywords from column 2 and values from column 3. Then add inline xunit tests in ExcelDataParserServiceTests.cs: test 3-col parsing returns correct keyword-value pairs (ID column skipped), test format detection distinguishes 2-col vs 3-col, test ID duplicates produce validation errors with specific IDs. Verify all existing tests still pass.
  - Files: `Services/ExcelDataParserService.cs`, `Tests/ExcelDataParserServiceTests.cs`
  - Verify: dotnet test --filter "FullyQualifiedName~ExcelDataParserServiceTests"

## Files Likely Touched

- Models/ExcelFileSummary.cs
- Services/ExcelDataParserService.cs
- Tests/ExcelDataParserServiceTests.cs
