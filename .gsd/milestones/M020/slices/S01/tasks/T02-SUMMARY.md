---
id: T02
parent: S01
milestone: M020
key_files:
  - Services/Interfaces/IDocumentProcessor.cs
  - Services/DocumentProcessorService.cs
  - ViewModels/MainWindowViewModel.cs
  - Cli/Commands/FillCommand.cs
  - Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs
key_decisions:
  - 使用 CreateLinkedTokenSource 链接外部 token 和内部 CTS，确保 CancelProcessing() 和外部 token 都能触发取消
  - CLI FillCommand 传 CancellationToken.None——CLI 场景不需要取消功能
duration: 
verification_result: passed
completed_at: 2026-05-03T17:33:46.830Z
blocker_discovered: false
---

# T02: CancellationToken 穿透：IDocumentProcessor 接口两个方法新增 CancellationToken 参数，DocumentProcessorService 正确初始化 LinkedCTS，ProcessFolderAsync/ProcessDocumentWithFormattedDataAsync 循环内检查取消，ViewModel 传递 token

**CancellationToken 穿透：IDocumentProcessor 接口两个方法新增 CancellationToken 参数，DocumentProcessorService 正确初始化 LinkedCTS，ProcessFolderAsync/ProcessDocumentWithFormattedDataAsync 循环内检查取消，ViewModel 传递 token**

## What Happened

取消功能原先完全失效：_cancellationTokenSource 从未在 ProcessDocumentsAsync 和 ProcessFolderAsync 中初始化，ProcessDocumentWithFormattedDataAsync 不接受 CancellationToken，ProcessFolderAsync 的 foreach 循环不检查取消。

实现变更：

1. **IDocumentProcessor 接口**：ProcessDocumentsAsync 和 ProcessDocumentWithFormattedDataAsync 新增 `CancellationToken cancellationToken = default` 参数，保持向后兼容。

2. **DocumentProcessorService.ProcessDocumentsAsync**：新增 CancellationToken 参数，使用 `CancellationTokenSource.CreateLinkedTokenSource` 初始化 _cancellationTokenSource 并链接外部 token，将 linked token 传递给 ProcessExcelDataAsync。捕获 OperationCanceledException 记录日志后重新抛出。

3. **ProcessExcelDataAsync**：新增 CancellationToken 参数，在关键步骤前调用 ThrowIfCancellationRequested()，传递给 ProcessDocumentWithFormattedDataAsync。

4. **ProcessFolderAsync**：使用 CreateLinkedTokenSource 初始化 _cancellationTokenSource，在 foreach 循环迭代开头添加 ThrowIfCancellationRequested()，传递 linked token 给 ProcessDocumentWithFormattedDataAsync。捕获 OperationCanceledException 记录日志后重新抛出。finally 块中清理 _cancellationTokenSource。

5. **ProcessDocumentWithFormattedDataAsync**：新增 CancellationToken 参数，在方法入口、复制文件后、内容控件循环中检查取消。

6. **ViewModel (MainWindowViewModel)**：StartProcessAsync 中将 _cancellationTokenSource.Token 传递给 ProcessDocumentsAsync（ProcessFolderAsync 已经在传递）。

7. **CLI FillCommand**：传递 CancellationToken.None（CLI 不支持取消），添加 using System.Threading。

8. **测试修复**：CommandValidationTests 中的 StubDocumentProcessor 更新两个方法签名匹配新接口。

所有 249 个现有测试通过（222 + 27），无回归。

## Verification

1. `dotnet build` 通过，0 错误（仅预存在 warning）
2. `dotnet test` 全部 249 测试通过（222 + 27），无失败
3. 代码审查确认：(a) ProcessDocumentsAsync 接受并链接外部 CancellationToken；(b) ProcessFolderAsync foreach 循环开头检查 ThrowIfCancellationRequested；(c) ProcessDocumentWithFormattedDataAsync 在入口、文件复制后、内容控件循环中检查取消；(d) ViewModel 传递 token 到服务层

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -v q` | 0 | ✅ pass | 1420ms |
| 2 | `dotnet test --no-build -v q` | 0 | ✅ pass (249 tests) | 15000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Services/Interfaces/IDocumentProcessor.cs`
- `Services/DocumentProcessorService.cs`
- `ViewModels/MainWindowViewModel.cs`
- `Cli/Commands/FillCommand.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
