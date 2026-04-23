---
estimated_steps: 7
estimated_files: 1
skills_used: []
---

# T01: Add edge case unit tests for 3-column format detection and parsing

S01 的 6 个测试覆盖了基本三列解析、ID 去除和重复检测。本任务补充以下边界场景：

1. **空文件**: Excel 无数据行 → DetectExcelFormat 默认 TwoColumn，ParseExcelFileAsync 返回空字典，ValidateExcelFileAsync 报错
2. **空白首行**: 前几行第一列为空，数据从第 N 行开始 → 正确检测格式
3. **三列空 ID**: ID 列为空但关键词/值列有效 → 正确解析，不崩溃
4. **单行三列**: 只有一行数据 → 正确解析
5. **ID 去空格**: ID 值含前后空格（如 "  001  "）→ 重复检测时正确 trim
6. **多重重复 ID**: 多个不同 ID 各有重复 → DuplicateRowIds 包含所有重复 ID

## Inputs

- `Tests/ExcelDataParserServiceTests.cs`

## Expected Output

- `Tests/ExcelDataParserServiceTests.cs`

## Verification

dotnet test --filter "ExcelDataParserServiceTests" 2>&1 | tail -5

## Observability Impact

None — test-only task.
