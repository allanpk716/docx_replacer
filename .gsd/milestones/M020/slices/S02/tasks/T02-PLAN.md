---
estimated_steps: 19
estimated_files: 4
skills_used: []
---

# T02: 删除 ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp，更新接口和测试

DocumentProcessorService 中有两段死代码：

1. **ProcessSingleDocumentAsync** — IDocumentProcessor 接口上的 public 方法，接收 `Dictionary<string, object>` 数据。活跃代码路径全部使用 `ProcessDocumentWithFormattedDataAsync`（接收 `Dictionary<string, FormattedCellValue>`）。ProcessSingleDocumentAsync 仅被 `HeaderFooterCommentIntegrationTests` 的 3 个测试调用，无任何生产代码引用。

2. **GenerateOutputFileNameWithTimestamp** — private 方法，从未被调用（输出文件名逻辑在 ProcessExcelDataAsync 和 ProcessFolderAsync 中内联了）。

## Steps

1. 打开 `Services/Interfaces/IDocumentProcessor.cs`，删除 `ProcessSingleDocumentAsync` 方法签名及其 XML 注释
2. 打开 `Services/DocumentProcessorService.cs`，删除 `ProcessSingleDocumentAsync` 方法实现（约 line 193-230）
3. 在同一文件中，删除 `GenerateOutputFileNameWithTimestamp` 方法（约 line 506-514）
4. 打开 `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`，从 `StubDocumentProcessor` 类中删除 `ProcessSingleDocumentAsync` 桩方法
5. 打开 `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`，改写 3 个测试：
   - 将 `Dictionary<string, object>` 数据转换为 `Dictionary<string, FormattedCellValue>`（每个值包装为 `new FormattedCellValue { PlainText = value, Fragments = [new TextFragment { Text = value }] }`）
   - 将 `processor.ProcessSingleDocumentAsync(templatePath, outputPath, data)` 调用改为 `processor.ProcessDocumentWithFormattedDataAsync(templatePath, formattedData, outputPath)`
   - 断言从 `Assert.True(success)` 改为 `Assert.True(result.IsSuccess)`（返回值从 bool 变为 ProcessResult）
6. 运行 `dotnet build` 和 `dotnet test` 确认无回归

## Must-Haves

- [ ] IDocumentProcessor 接口不再包含 ProcessSingleDocumentAsync
- [ ] DocumentProcessorService 中 ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp 已删除
- [ ] 所有 mock/stub 同步更新
- [ ] HeaderFooterCommentIntegrationTests 3 个测试改用 ProcessDocumentWithFormattedDataAsync
- [ ] dotnet build 0 错误，dotnet test 全部通过

## Inputs

- `Services/DocumentProcessorService.cs`
- `Services/Interfaces/IDocumentProcessor.cs`
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`

## Expected Output

- `Services/DocumentProcessorService.cs`
- `Services/Interfaces/IDocumentProcessor.cs`
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`

## Verification

grep -rn 'ProcessSingleDocumentAsync\|GenerateOutputFileNameWithTimestamp' --include='*.cs' 排除 .gsd 目录返回 0；dotnet build 0 错误；dotnet test 全部通过
