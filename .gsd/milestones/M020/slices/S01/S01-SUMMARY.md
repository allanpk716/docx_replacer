---
id: S01
parent: M020
milestone: M020
provides:
  - ["FileService 所有异常路径有结构化日志记录", "DocumentProcessorService 取消功能从完全失效变为真正可中断", "CancellationToken 从 IDocumentProcessor 接口穿透到 ViewModel", "FileService 和取消功能的单元测试模式可供 S05 复用"]
requires:
  []
affects:
  []
key_files:
  - ["Services/FileService.cs", "Services/Interfaces/IDocumentProcessor.cs", "Services/DocumentProcessorService.cs", "ViewModels/MainWindowViewModel.cs", "Cli/Commands/FillCommand.cs", "Tests/DocuFiller.Tests/Services/FileServiceTests.cs", "Tests/DocuFiller.Tests/Services/CancellationTests.cs", "Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs"]
key_decisions:
  - ["FileService ILogger 注入利用 DI 容器已有开放泛型注册，无需修改 App.xaml.cs", "使用 CreateLinkedTokenSource 链接外部 token 和内部 CTS", "CLI FillCommand 传 CancellationToken.None，CLI 场景不需要取消", "取消测试验证结果失败标记而非异常传播，因内部 catch(Exception) 吞噬 OCE"]
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M020/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M020/slices/S01/tasks/T02-SUMMARY.md", ".gsd/milestones/M020/slices/S01/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-03T17:40:03.819Z
blocker_discovered: false
---

# S01: 关键可靠性修复：FileService 异常处理 + 取消功能

**FileService 4 个裸 catch 块全部添加 ILogger 结构化日志记录，DocumentProcessorService 取消功能从完全失效变为真正可中断处理（CancellationToken 穿透到循环内部），新增 7 个单元测试验证异常路径和取消行为。**

## What Happened

## 修复内容

### T01: FileService ILogger 依赖注入 + 异常日志记录
- 为 FileService 添加 `ILogger<FileService>` 构造函数参数，利用 DI 容器已有的 `AddSingleton(typeof(ILogger<>), typeof(Logger<>))` 开放泛型注册自动注入，无需修改 App.xaml.cs
- 4 个裸 catch 块（EnsureDirectoryExists、CopyFileAsync、DeleteFile、WriteFileContentAsync）全部改为 `catch (Exception ex) { _logger.LogError(ex, "...", pathArgs...); return false; }`，每条日志包含异常对象和文件路径的结构化参数
- 更新 5 处测试代码中的 `new FileService()` 为 `new FileService(new NullLogger<FileService>())`

### T02: CancellationToken 穿透修复
- **IDocumentProcessor 接口**：ProcessDocumentsAsync 和 ProcessDocumentWithFormattedDataAsync 新增 `CancellationToken cancellationToken = default` 参数，保持向后兼容
- **DocumentProcessorService**：ProcessDocumentsAsync 和 ProcessFolderAsync 使用 `CreateLinkedTokenSource` 初始化 _cancellationTokenSource 并链接外部 token；foreach 循环迭代开头和关键处理步骤前添加 `ThrowIfCancellationRequested()`；finally 块清理 CTS
- **ViewModel**：StartProcessAsync 中将 _cancellationTokenSource.Token 传递给 ProcessDocumentsAsync
- **CLI FillCommand**：传递 CancellationToken.None（CLI 不支持取消）
- **测试修复**：CommandValidationTests 中的 StubDocumentProcessor 更新方法签名

### T03: 单元测试
- **FileServiceTests**（4 个测试）：验证 4 个方法的异常路径日志记录
- **CancellationTests**（3 个测试）：验证 pre-cancelled token 和运行时取消的处理结果
- 发现设计问题：内部 catch(Exception) 块会吞噬 OperationCanceledException，取消表现为处理失败而非异常传播。测试相应调整为验证失败标记

## 已知限制
- ProcessFolderAsync/ProcessDocumentsAsync 内部的 foreach catch(Exception) 会捕获 OperationCanceledException，导致取消不会传播为异常，而是记录为失败文件。这是现有异常处理结构带来的限制，后续 slice 可考虑改进。

## 对下游 slice 的影响
- S02（死代码清理）和 S03（重复代码提取）可在本 slice 基础上安全进行
- S05（配置清理和测试补充）可复用本 slice 建立的 FileService 测试模式

## Verification

- T01: `grep -c 'LogError' Services/FileService.cs` 返回 4；`dotnet build` 0 错误；`dotnet test` 249 测试通过
- T02: IDocumentProcessor 接口 3 个方法含 CancellationToken 参数；DocumentProcessorService 含 8 处 ThrowIfCancellationRequested 调用；ViewModel 和 CLI 正确传递 token；`dotnet test` 249 测试通过
- T03: 新增 FileServiceTests（4 测试）和 CancellationTests（3 测试），`dotnet test` 全部通过
- 代码审查确认：FileService 有 ILogger 注入和 4 个 LogError 调用；CancellationToken 从接口到服务到 ViewModel 完整穿透

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
