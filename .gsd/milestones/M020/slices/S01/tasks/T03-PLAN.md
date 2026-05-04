---
estimated_steps: 12
estimated_files: 2
skills_used: []
---

# T03: FileService 异常路径和取消功能单元测试

为 T01 的 FileService 异常日志记录和 T02 的取消功能编写单元测试。

测试项目使用 xUnit + Moq，通过 Compile Include 链接源文件。

FileService 测试（新建 Tests/DocuFiller.Tests/Services/FileServiceTests.cs）：
- 测试 EnsureDirectoryExists 在异常时记录日志并返回 false
- 测试 CopyFileAsync 在异常时记录日志并返回 false
- 测试 DeleteFile 在异常时记录日志并返回 false
- 测试 WriteFileContentAsync 在异常时记录日志并返回 false

取消功能测试（在 Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs 或新建文件）：
- 测试 ProcessFolderAsync 在取消时抛出 OperationCanceledException
- 测试 ProcessDocumentsAsync 的 CancellationToken 传递
- 测试 CancelProcessing() 能中断处理（需要 mock 或简化的处理流程）

测试 FileService 时需要使用不存在的路径/只读文件等触发异常。测试取消功能时需要使用 pre-cancelled token。

## Inputs

- `Services/FileService.cs — T01 修改后的 FileService（带 ILogger）`
- `Services/DocumentProcessorService.cs — T02 修改后的取消功能实现`
- `Services/Interfaces/IDocumentProcessor.cs — 更新后的接口`
- `Tests/DocuFiller.Tests.csproj — 测试项目文件`

## Expected Output

- `Tests/DocuFiller.Tests/Services/FileServiceTests.cs — 4+ FileService 异常路径测试`
- `Tests/DocuFiller.Tests/Services/CancellationTests.cs — 3+ 取消功能测试`

## Verification

dotnet test
