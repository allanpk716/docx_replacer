# S05: R028 启动自动检查更新 + 通知徽章

**Goal:** 实现 R028：应用启动 5 秒后自动静默检查更新，有新版本时状态栏显示通知徽章（红色圆点叠加在⚙设置按钮上），检查失败静默处理不影响用户体验。
**Demo:** 应用启动 5 秒后状态栏自动显示更新状态，有新版本时显示视觉指示

## Must-Haves

- UpdateStatusViewModel.InitializeAsync() 在 5 秒延迟后才发起更新检查
- 有新版本时⚙设置按钮上方显示红色圆点徽章
- 自动检查失败时状态栏保持默认状态（无异常抛出、无弹窗）
- 手动"检查更新"按钮仍然可用且独立工作
- dotnet build 0 errors，dotnet test 全部通过
- 新增 UpdateStatusViewModel 单元测试覆盖延迟检查和失败静默行为

## Proof Level

- This slice proves: contract — 5秒延迟通过 Task.Delay + CancellationToken 测试验证，徽章通过 XAML 绑定编译验证，失败静默通过 Mock 异常测试验证。无真实更新服务器交互。

## Integration Closure

- Upstream consumed: UpdateStatusViewModel (from S01) 的 InitializeAsync + CheckUpdateCommand + CurrentUpdateStatus 属性
- New wiring: MainWindowViewModel 构造函数调用延迟 InitializeAsync（已存在，仅修改延迟行为），MainWindow.xaml 新增徽章 UI 元素绑定到 UpdateStatusVM
- Remaining: S06 文档同步需描述此功能

## Verification

- Signals: ILogger 已覆盖自动检查流程（InitializeUpdateStatusAsync 中的 LogInformation/LogError）
- Inspection: 状态栏 UpdateStatusText 可见性 + ⚙按钮徽章可见性 + 日志输出
- Failure: 静默处理，CurrentUpdateStatus 设为 Error，日志记录异常详情

## Tasks

- [ ] **T01: UpdateStatusViewModel 添加 5 秒延迟自动检查 + 单元测试** `est:1h`
  修改 UpdateStatusViewModel.InitializeAsync() 添加 5 秒延迟后再调用 InitializeUpdateStatusAsync()。添加 CancellationTokenSource 字段以便在 ViewModel 销毁时取消等待。编写单元测试验证延迟行为和失败静默。
  - Files: `ViewModels/UpdateStatusViewModel.cs`, `Services/Interfaces/IUpdateService.cs`, `Tests/UpdateStatusViewModelTests.cs`
  - Verify: dotnet build DocuFiller.csproj --no-restore && dotnet test Tests/DocuFiller.Tests/DocuFiller.Tests.csproj --no-restore --filter "UpdateStatusViewModel" --verbosity normal

- [ ] **T02: 状态栏⚙设置按钮添加通知徽章（红色圆点）** `est:30m`
  在 MainWindow.xaml 的⚙设置按钮上叠加红色圆点徽章，当 UpdateStatusVM.CurrentUpdateStatus == UpdateAvailable 时显示。使用 Grid + Ellipse 实现，绑定到 UpdateStatusVM 的现有属性。
  - Files: `ViewModels/UpdateStatusViewModel.cs`, `MainWindow.xaml`
  - Verify: dotnet build DocuFiller.csproj --no-restore && dotnet test Tests/DocuFiller.Tests/DocuFiller.Tests.csproj --no-restore --verbosity minimal

## Files Likely Touched

- ViewModels/UpdateStatusViewModel.cs
- Services/Interfaces/IUpdateService.cs
- Tests/UpdateStatusViewModelTests.cs
- MainWindow.xaml
