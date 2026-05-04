---
estimated_steps: 8
estimated_files: 4
skills_used: []
---

# T02: DocumentProcessorService 取消功能修复——CancellationToken 穿透

取消功能目前完全失效：_cancellationTokenSource 从未被初始化（永远是 null），ProcessDocumentsAsync 不接受 CancellationToken，ProcessFolderAsync 接受但不检查，ProcessDocumentWithFormattedDataAsync 没有取消参数。

修复方案：
1. ProcessDocumentsAsync 新增 CancellationToken 参数，在调用链中传递
2. ProcessFolderAsync 在 foreach 循环迭代开头添加 cancellationToken.ThrowIfCancellationRequested()
3. ProcessDocumentWithFormattedDataAsync 新增 CancellationToken 参数
4. ProcessDocumentsAsync 内部初始化 _cancellationTokenSource 并链接外部 token
5. IDocumentProcessor 接口：ProcessDocumentsAsync 和 ProcessDocumentWithFormattedDataAsync 添加 CancellationToken 参数（带 default 值保持向后兼容）
6. ViewModel 层：StartProcessAsync 将 _cancellationTokenSource.Token 传递给 ProcessDocumentsAsync（ProcessFolderAsync 已经在传递，只需确保服务端真正检查）

## Inputs

- `Services/Interfaces/IDocumentProcessor.cs — 接口定义需要更新`
- `Services/DocumentProcessorService.cs — 核心处理服务`
- `ViewModels/MainWindowViewModel.cs — ViewModel 调用方`
- `Cli/Commands/FillCommand.cs — CLI 调用方（可能需要更新）`

## Expected Output

- `Services/Interfaces/IDocumentProcessor.cs — ProcessDocumentsAsync 和 ProcessDocumentWithFormattedDataAsync 新增 CancellationToken 参数`
- `Services/DocumentProcessorService.cs — CTS 正确初始化，所有处理方法检查取消`
- `ViewModels/MainWindowViewModel.cs — CancellationToken 传递到服务层`

## Verification

dotnet build && dotnet test
