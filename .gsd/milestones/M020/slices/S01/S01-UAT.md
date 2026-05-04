# S01: 关键可靠性修复：FileService 异常处理 + 取消功能 — UAT

**Milestone:** M020
**Written:** 2026-05-03T17:40:03.820Z

# S01 UAT: 关键可靠性修复

## UAT Type
自动化单元测试覆盖（contract-level proof）。FileService 异常路径通过 mock ILogger 验证日志调用；取消功能通过 pre-cancelled token 和运行时取消验证处理结果。

## Not Proven By This UAT
- GUI 取消按钮的端到端行为（需要手动 WPF 测试）
- 多线程并发取消场景
- 大批量文件（1000+）处理中的取消响应延迟
- 日志输出格式的可视化检查（仅验证 LogError 被调用）

## Test Cases

### TC-01: FileService 异常日志记录
**前置条件**: FileService 已注入 ILogger
1. 调用 EnsureDirectoryExists，传入非法路径（含 null 字符）
   - 预期：返回 false，LogError 被调用 1 次，日志包含路径信息
2. 调用 CopyFileAsync，传入不存在的源文件路径
   - 预期：返回 false，LogError 被调用，日志包含源路径和目标路径
3. 调用 DeleteFile，传入深层不存在的文件路径
   - 预期：返回 false，LogError 被调用
4. 调用 WriteFileContentAsync，传入非法路径
   - 预期：返回 false，LogError 被调用

### TC-02: 取消功能 - Pre-cancelled Token
**前置条件**: IDocumentProcessor 接口接受 CancellationToken
1. 创建已取消的 CancellationTokenSource
2. 调用 ProcessFolderAsync，传入 pre-cancelled token
   - 预期：处理结果包含失败标记（不抛出 OperationCanceledException）
3. 调用 ProcessDocumentsAsync，传入 pre-cancelled token
   - 预期：ProcessResult 包含错误信息

### TC-03: 取消功能 - 运行时中断
**前置条件**: 处理循环内部检查 ThrowIfCancellationRequested
1. 启动 ProcessFolderAsync，在 Excel 数据解析回调中调用 CancelProcessing()
   - 预期：后续文件处理失败，结果包含失败文件记录
2. 验证 _cancellationTokenSource 在 finally 块中被正确清理

### TC-04: CLI 向后兼容
**前置条件**: FillCommand 传递 CancellationToken.None
1. 运行 CLI fill 命令
   - 预期：正常完成，不受 CancellationToken 影响
