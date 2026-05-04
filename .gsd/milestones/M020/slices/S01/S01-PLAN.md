# S01: 关键可靠性修复：FileService 异常处理 + 取消功能

**Goal:** 修复两个关键可靠性问题：(1) FileService 所有方法中裸 catch 块添加 ILogger 依赖并记录异常，消除静默吞没；(2) DocumentProcessorService 的取消功能从"完全失效"变为真正可中断处理——CancellationToken 从 ViewModel 穿透到文档处理循环内部。
**Demo:** FileService 所有方法捕获异常时记录日志，取消按钮真正中断处理

## Must-Haves

- FileService 的 EnsureDirectoryExists、CopyFileAsync、DeleteFile、WriteFileContentAsync 方法中所有 catch 块通过 ILogger 记录异常详情（异常类型、消息、路径参数）
- FileService 构造函数接受 ILogger&lt;FileService&gt; 参数
- ProcessDocumentsAsync（单文件模式）接受 CancellationToken 参数并在处理循环中检查
- ProcessFolderAsync 在 foreach 循环的每次迭代开头检查 CancellationToken.ThrowIfCancellationRequested()
- ProcessDocumentWithFormattedDataAsync 接受 CancellationToken 参数
- CancelProcessing() 正确初始化并使用 _cancellationTokenSource
- ViewModel 层的 CancellationToken 正确传递到服务层
- dotnet build 0 错误，dotnet test 全部通过
- 新增单元测试覆盖：FileService 异常路径日志记录、取消操作中断处理

## Proof Level

- This slice proves: contract — 通过单元测试验证异常日志记录和取消行为，使用 mock ILogger 和 CancellationTokenSource

## Integration Closure

Upstream surfaces consumed: IDocumentProcessor 接口签名变更（新增 CancellationToken 参数），IFileService 接口不变但实现变更。New wiring: FileService 构造函数新增 ILogger 参数，App.xaml.cs DI 注册需要更新。ViewModel 的 StartProcessAsync 和 ProcessFolderAsync 需要将 CancellationToken 传递到新的方法签名。What remains: S02/S03 的死代码清理和重复代码提取。

## Verification

- FileService 从零日志变为所有异常路径有结构化日志（logger.LogError），包含文件路径和异常详情。取消操作从静默忽略变为抛出 OperationCanceledException 并记录日志。

## Tasks

- [ ] **T01: FileService 添加 ILogger 并修复裸 catch 异常吞没** `est:30m`
  FileService 当前有 4 个方法使用裸 catch 块静默吞掉异常（EnsureDirectoryExists、CopyFileAsync、DeleteFile、WriteFileContentAsync），且完全没有 ILogger 依赖。这导致文件操作失败时无法诊断问题。
  - Files: `Services/FileService.cs`, `Services/Interfaces/IFileService.cs`, `App.xaml.cs`
  - Verify: dotnet build && grep -c 'LogError' Services/FileService.cs (应返回 4+)

- [ ] **T02: DocumentProcessorService 取消功能修复——CancellationToken 穿透** `est:1h`
  取消功能目前完全失效：_cancellationTokenSource 从未被初始化（永远是 null），ProcessDocumentsAsync 不接受 CancellationToken，ProcessFolderAsync 接受但不检查，ProcessDocumentWithFormattedDataAsync 没有取消参数。
  - Files: `Services/Interfaces/IDocumentProcessor.cs`, `Services/DocumentProcessorService.cs`, `ViewModels/MainWindowViewModel.cs`, `Cli/Commands/FillCommand.cs`
  - Verify: dotnet build && dotnet test

- [ ] **T03: FileService 异常路径和取消功能单元测试** `est:45m`
  为 T01 的 FileService 异常日志记录和 T02 的取消功能编写单元测试。
  - Files: `Tests/DocuFiller.Tests/Services/FileServiceTests.cs`, `Tests/DocuFiller.Tests/Services/CancellationTests.cs`
  - Verify: dotnet test

## Files Likely Touched

- Services/FileService.cs
- Services/Interfaces/IFileService.cs
- App.xaml.cs
- Services/Interfaces/IDocumentProcessor.cs
- Services/DocumentProcessorService.cs
- ViewModels/MainWindowViewModel.cs
- Cli/Commands/FillCommand.cs
- Tests/DocuFiller.Tests/Services/FileServiceTests.cs
- Tests/DocuFiller.Tests/Services/CancellationTests.cs
