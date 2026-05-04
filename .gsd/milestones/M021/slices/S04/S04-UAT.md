# S04: S04: 服务接口补全 + CleanupService 日志补充 — UAT

**Milestone:** M021
**Written:** 2026-05-04T11:30:00.198Z

# UAT: S04 — 服务接口补全 + CleanupService 日志补充

## UAT Type
Contract audit — 静态代码分析验证，无运行时行为变更。

## Not Proven By This UAT
- 运行时文档处理管道（不在本 slice 范围）
- 异常触发后的日志输出内容验证（仅确认代码中存在 LogError 调用）

## Preconditions
- 项目可正常构建和测试
- 已安装 .NET 8 SDK

## Test Cases

### TC01: IDocumentProcessor 接口完整性
1. 打开 `Services/Interfaces/IDocumentProcessor.cs` 和 `Services/DocumentProcessorService.cs`
2. 逐一对比接口中声明的每个成员与服务中的公共方法/事件
3. **预期**：接口包含 7 个成员（ProcessDocumentsAsync、ValidateTemplateAsync、GetContentControlsAsync、ProcessDocumentWithFormattedDataAsync、ProcessFolderAsync、CancelProcessing、ProgressUpdated），与服务公共 API 完全匹配（Dispose 排除）
4. **预期**：所有参数类型、返回类型、默认值完全一致

### TC02: DocumentCleanupService 日志覆盖
1. 打开 `DocuFiller/Services/DocumentCleanupService.cs`
2. 搜索所有 `catch (Exception` 块，统计数量
3. 搜索所有 `_logger.LogError` 调用，统计数量
4. **预期**：catch 块数量 = _logger.LogError 数量 = 5
5. **预期**：每个 LogError 调用包含 `ex` 参数（异常对象传递）

### TC03: 构建和测试通过
1. 运行 `dotnet build --no-restore`
2. **预期**：0 errors, 0 warnings
3. 运行 `dotnet test --no-build --verbosity minimal`
4. **预期**：全部测试通过，0 failed
