# S01: 状态栏更新检查旋转动画

**Goal:** 启动程序后状态栏立刻显示旋转 spinner 动画，覆盖 5 秒延迟和实际网络检查全过程；检查完成后动画自动停止，切换为结果状态文字。手动点击"检查更新"同样显示旋转动画。
**Demo:** 启动程序后状态栏立刻显示旋转 spinner，检查完成后切换为结果状态

## Must-Haves

- InitializeAsync 在 Task.Delay(5000) 之前设置 CurrentUpdateStatus = Checking，启动后立即显示 spinner
- ShowCheckingAnimation 属性在 Checking 状态或 IsCheckingUpdate=true 时返回 true
- XAML 中 Canvas 元素通过 DoubleAnimation(RotateTransform) 产生旋转动画
- spinner 可见性绑定到 ShowCheckingAnimation（通过 BooleanToVisibilityConverter）
- dotnet build 零错误，dotnet test 全部通过

## Proof Level

- This slice proves: integration — ViewModel 单元测试验证状态转换，XAML 编译验证动画资源正确加载

## Integration Closure

- Upstream surfaces consumed: UpdateStatusViewModel 的 CurrentUpdateStatus 和 IsCheckingUpdate 属性
- New wiring introduced: ShowCheckingAnimation 绑定连接 ViewModel 和 XAML spinner 元素
- What remains before the milestone is truly usable end-to-end: nothing — 这是唯一 slice

## Verification

- Runtime signals: CurrentUpdateStatus 状态转换日志（已有），ShowCheckingAnimation 属性变化触发 PropertyChanged
- Inspection surfaces: 调试时可通过 WPF TreeViewer 查看 spinner 元素 Visibility 和 RotateTransform Angle
- Failure visibility: 动画不显示时可通过 ShowCheckingAnimation 属性值和 CurrentUpdateStatus 状态定位问题

## Tasks

- [ ] **T01: Add ShowCheckingAnimation property and move Checking state before delay** `est:45m`
  在 UpdateStatusViewModel 中实现 ShowCheckingAnimation 计算属性，并将 InitializeAsync 中的 Checking 状态设置移到 Task.Delay 之前，确保启动后立刻显示动画。添加单元测试验证属性行为和状态转换。
  - Files: `ViewModels/UpdateStatusViewModel.cs`, `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`
  - Verify: dotnet test --filter "FullyQualifiedName~ShowCheckingAnimation"

- [ ] **T02: Add spinner rotation animation to status bar XAML** `est:45m`
  在 MainWindow.xaml 状态栏中添加 Canvas 元素实现的旋转 spinner 动画，绑定到 ShowCheckingAnimation 属性，在更新检查期间显示旋转动画。
  - Files: `MainWindow.xaml`
  - Verify: dotnet build 2>&1 | grep -v "update-client" | grep -i "error"

## Files Likely Touched

- ViewModels/UpdateStatusViewModel.cs
- Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs
- MainWindow.xaml
