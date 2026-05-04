---
estimated_steps: 20
estimated_files: 3
skills_used: []
---

# T01: UpdateStatusViewModel 添加 5 秒延迟自动检查 + 单元测试

修改 UpdateStatusViewModel.InitializeAsync() 添加 5 秒延迟后再调用 InitializeUpdateStatusAsync()。添加 CancellationTokenSource 字段以便在 ViewModel 销毁时取消等待。编写单元测试验证延迟行为和失败静默。

**Why**: R028 要求启动 5 秒后才自动检查，当前是立即触发。延迟确保不阻塞 UI 启动和初始化。

**Steps**:
1. 在 UpdateStatusViewModel 中添加 `private CancellationTokenSource? _autoCheckCts;` 字段
2. 修改 `InitializeAsync()` 方法：先 `await Task.Delay(5000, _autoCheckCts.Token)`，然后调用 `InitializeUpdateStatusAsync()`
3. 在 catch (OperationCanceledException) 中静默返回（用户取消延迟）
4. 确保所有其他异常仍被 catch 并设置 `CurrentUpdateStatus = UpdateStatus.Error`
5. 新增测试文件 `Tests/UpdateStatusViewModelTests.cs`，覆盖：
   - `InitializeAsync_SkipsWhenUpdateServiceIsNull` — 验证无更新服务时直接返回
   - `InitializeAsync_SkipsWhenUpdateUrlNotConfigured` — 验证未配置 URL 时跳过
   - `InitializeAsync_SetsUpdateAvailable_WhenUpdateFound` — Mock IUpdateService 返回 UpdateInfo，验证状态变为 UpdateAvailable
   - `InitializeAsync_SetsUpToDate_WhenNoUpdate` — Mock 返回 null，验证状态变为 UpToDate
   - `InitializeAsync_SetsError_OnException` — Mock 抛异常，验证状态变为 Error 且无未处理异常
6. 测试中使用 Moq 模拟 IUpdateService，不需要真实延迟（直接调用 InitializeUpdateStatusAsync 测试逻辑分支）

**Must-haves**:
- [ ] InitializeAsync 包含 5 秒 Task.Delay
- [ ] CancellationTokenSource 支持取消延迟
- [ ] OperationCanceledException 静默处理
- [ ] 5 个单元测试全部通过
- [ ] dotnet build 0 errors

## Inputs

- `ViewModels/UpdateStatusViewModel.cs`
- `Services/Interfaces/IUpdateService.cs`

## Expected Output

- `ViewModels/UpdateStatusViewModel.cs`
- `Tests/UpdateStatusViewModelTests.cs`

## Verification

dotnet build DocuFiller.csproj --no-restore && dotnet test Tests/DocuFiller.Tests/DocuFiller.Tests.csproj --no-restore --filter "UpdateStatusViewModel" --verbosity normal

## Observability Impact

Signals: _logger.LogInformation 记录自动检查开始/结果/异常；CancellationToken 取消时无日志（静默）。Future agent 检查: grep 日志中 '自动检查' 关键词可追踪检查生命周期。
