---
id: T01
parent: S05
milestone: M021
key_files:
  - ViewModels/UpdateStatusViewModel.cs
  - Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs
  - Tests/DocuFiller.Tests/Stubs/WindowStubs.cs
  - Tests/DocuFiller.Tests.csproj
key_decisions:
  - Tests use reflection to invoke private InitializeUpdateStatusAsync directly, bypassing 5-second delay for fast test execution
  - Added WPF Window stubs (DownloadProgressWindow/UpdateSettingsWindow) in Tests/DocuFiller.Tests/Stubs/ to satisfy UpdateStatusViewModel compilation without pulling in full XAML
duration: 
verification_result: passed
completed_at: 2026-05-04T11:41:50.774Z
blocker_discovered: false
---

# T01: UpdateStatusViewModel 添加 5 秒延迟自动检查更新 + CancellationToken 取消支持 + 12 个单元测试

**UpdateStatusViewModel 添加 5 秒延迟自动检查更新 + CancellationToken 取消支持 + 12 个单元测试**

## What Happened

修改 UpdateStatusViewModel.InitializeAsync() 添加 5 秒 Task.Delay 延迟后再调用 InitializeUpdateStatusAsync()。新增 _autoCheckCts 字段（CancellationTokenSource），在 InitializeAsync 中创建并在 Task.Delay 中使用其 Token。OperationCanceledException 被静默捕获并记录日志。所有其他异常仍由 InitializeUpdateStatusAsync 的 catch 块处理，设置 CurrentUpdateStatus = UpdateStatus.Error。

创建 12 个单元测试（Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs）覆盖：跳过逻辑（null 服务、未配置 URL）、成功路径（有更新、无更新）、异常处理、取消路径、以及派生属性验证。通过反射直接调用 private InitializeUpdateStatusAsync 绕过 5 秒延迟，确保测试执行快速。

同时添加了：VersionHelper.cs 到测试项目编译引用、NuGet.Versioning 包引用（SemanticVersion 需要）、WPF Window stubs（DownloadProgressWindow/UpdateSettingsWindow）满足 UpdateStatusViewModel 编译依赖。

## Verification

dotnet build DocuFiller.csproj --no-restore: 0 errors, 0 warnings
dotnet build Tests/DocuFiller.Tests.csproj --no-restore: 0 errors
dotnet test --filter "UpdateStatusViewModel": 12/12 passed (0.82s)

所有 5 个必要检查点已验证：
1. InitializeAsync 包含 await Task.Delay(5000, _autoCheckCts.Token) ✅
2. CancellationTokenSource _autoCheckCts 字段支持取消 ✅
3. OperationCanceledException 静默处理 ✅
4. 5 个核心单元测试 + 7 个辅助测试全部通过 ✅
5. dotnet build 0 errors ✅

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj --no-restore` | 0 | ✅ pass | 2240ms |
| 2 | `dotnet build Tests/DocuFiller.Tests.csproj --no-restore` | 0 | ✅ pass | 1480ms |
| 3 | `dotnet test Tests/DocuFiller.Tests.csproj --no-restore --filter "UpdateStatusViewModel" --verbosity normal` | 0 | ✅ pass (12/12 tests) | 1880ms |

## Deviations

Added NuGet.Versioning package reference to test csproj (needed for SemanticVersion used in Velopack UpdateInfo construction). Added WPF Window stubs file not mentioned in plan (necessary for test compilation of UpdateStatusViewModel which references Views.DownloadProgressWindow/UpdateSettingsWindow). Added 7 additional tests beyond the 5 required in the plan for better coverage of derived properties and state transitions.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/UpdateStatusViewModel.cs`
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`
- `Tests/DocuFiller.Tests/Stubs/WindowStubs.cs`
- `Tests/DocuFiller.Tests.csproj`
