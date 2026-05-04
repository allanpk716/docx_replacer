# S02: S02: 死代码清理 — UAT

**Milestone:** M020
**Written:** 2026-05-03T17:52:13.715Z

# UAT: S02 死代码清理

## UAT Type
Contract verification — 确认死代码删除后编译通过、测试通过、行为不变。

## Not Proven By This UAT
- 运行时文档处理行为（需要实际 .docx/.xlsx 文件）
- GUI 交互功能

## Preconditions
- .NET 8 SDK 已安装
- NuGet 包可正常还原

## Test Cases

### TC-01: ContentControlProcessor 死方法完全移除
1. 打开 `Services/ContentControlProcessor.cs`
2. 搜索 `ReplaceContentInContainer`、`ReplaceTextDirectly`、`FindTargetRun`、`CreateFormattedRuns`、`CreateFormattedTextElements`、`CreateParagraphWithFormattedText`
3. **预期**: 0 个匹配结果
4. 搜索 `FindContentContainer`、`FindAllTargetRuns`
5. **预期**: 仍有匹配（活跃方法保留）

### TC-02: DocumentProcessorService 死代码完全移除
1. 搜索整个代码库（排除 .gsd/）中的 `ProcessSingleDocumentAsync`
2. **预期**: 0 个匹配
3. 搜索 `GenerateOutputFileNameWithTimestamp`
4. **预期**: 0 个匹配

### TC-03: IDocumentProcessor 接口更新
1. 打开 `Services/Interfaces/IDocumentProcessor.cs`
2. **预期**: 不包含 `ProcessSingleDocumentAsync` 方法签名
3. **预期**: 包含 `ProcessDocumentWithFormattedDataAsync` 方法签名

### TC-04: 测试代码适配
1. 打开 `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
2. **预期**: StubDocumentProcessor 类不包含 `ProcessSingleDocumentAsync` 方法
3. 打开 `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
4. **预期**: 使用 `ProcessDocumentWithFormattedDataAsync` 而非 `ProcessSingleDocumentAsync`
5. **预期**: 使用 `FormattedCellValue.FromPlainText()` 构造测试数据

### TC-05: 编译和测试通过
1. 运行 `dotnet build`
2. **预期**: 0 个编译错误
3. 运行 `dotnet test`
4. **预期**: 所有测试通过，0 失败
