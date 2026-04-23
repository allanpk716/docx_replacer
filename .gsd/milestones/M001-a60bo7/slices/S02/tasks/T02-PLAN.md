---
estimated_steps: 2
estimated_files: 1
skills_used: []
---

# T02: Add 3-column integration test and verify full suite passes

现有 ExcelIntegrationTests 仅使用两列格式 Excel 测试端到端流程。本任务新增三列格式的集成测试，验证三列 Excel 数据经 ParseExcelFileAsync → ProcessDocumentWithFormattedDataAsync 管道后，输出的 Word 文档包含正确的替换结果且 ID 列值不出现。

最后运行 dotnet test 验证全部测试通过（含新增测试和所有现有测试），完成 R004 验证。

## Inputs

- `Tests/ExcelIntegrationTests.cs`
- `Tests/ExcelDataParserServiceTests.cs`

## Expected Output

- `Tests/ExcelIntegrationTests.cs`

## Verification

dotnet test 2>&1 | tail -5

## Observability Impact

None — test-only task.
