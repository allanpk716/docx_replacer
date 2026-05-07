---
id: S01
parent: M024
milestone: M024
provides:
  - ["ShowCheckingAnimation 计算属性：在 Checking 状态或手动检查时返回 true", "SpinnerRotateAnimation Storyboard 资源：1 秒周期 360° 无限循环旋转动画", "状态栏 spinner UI 元素：通过 DataTrigger 和 BooleanToVisibilityConverter 绑定 ViewModel"]
requires:
  []
affects:
  []
key_files:
  - ["ViewModels/UpdateStatusViewModel.cs", "Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs", "MainWindow.xaml"]
key_decisions:
  - ["使用 Canvas + Ellipse + StrokeDashArray 虚线描边实现旋转视觉效果，而非 GIF 或第三方控件", "ShowCheckingAnimation 同时覆盖自动检查（Checking 状态）和手动检查（IsCheckingUpdate 属性）两条路径"]
patterns_established:
  - ["计算属性（ShowCheckingAnimation）聚合多个状态标志，XAML 通过单一绑定控制 UI 元素可见性和动画生命周期", "DataTrigger + BeginStoryboard/StopStoryboard 控制 WPF 动画的启动和停止，避免代码后台手动管理"]
observability_surfaces:
  - ["CurrentUpdateStatus 状态转换已有日志记录", "ShowCheckingAnimation 属性变化触发 PropertyChanged 事件"]
drill_down_paths:
  - [".gsd/milestones/M024/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M024/slices/S01/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-07T07:44:17.134Z
blocker_discovered: false
---

# S01: 状态栏更新检查旋转动画

**状态栏启动时立刻显示旋转 spinner 动画，覆盖 5 秒延迟和整个更新检查过程；检查完成后动画自动停止切换为结果状态文字**

## What Happened

## Task T01: ShowCheckingAnimation 属性与 Checking 状态前移
在 UpdateStatusViewModel 中实现了 ShowCheckingAnimation 计算属性：当 CurrentUpdateStatus 为 Checking（自动检查路径）或 IsCheckingUpdate 为 true（手动检查路径）时返回 true。将 InitializeAsync 中的 CurrentUpdateStatus = Checking 赋值移到 Task.Delay(5000) 之前，确保程序启动后 spinner 立刻可见。在 OnCurrentUpdateStatusChanged 和 OnIsCheckingUpdateChanged 两个 partial 方法中添加了 ShowCheckingAnimation 的 PropertyChanged 通知。新增 5 个单元测试覆盖所有状态组合。

## Task T02: XAML 旋转动画
在 MainWindow.xaml 状态栏添加了 Canvas + Ellipse 实现的旋转 spinner。使用 StrokeDashArray="2 2.5" 虚线描边产生旋转视觉效果，DoubleAnimation 从 0° 到 360° 以 1 秒周期无限循环。通过 DataTrigger 绑定 UpdateStatusVM.ShowCheckingAnimation 控制动画的启动和停止（BeginStoryboard/StopStoryboard），Canvas 可见性通过 BooleanToVisibilityConverter 绑定同一属性。布局保持与原状态栏一致。

## Verification

- dotnet test --filter "FullyQualifiedName~ShowCheckingAnimation": 5 passed, 0 failed (via --no-restore, NuGet restore has pre-existing env issue unrelated to this change)
- dotnet build DocuFiller.csproj --no-restore: 0 warnings, 0 errors
- Full test suite previously verified at 274 unit + 27 E2E tests, 0 failures (per T01 summary)

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
