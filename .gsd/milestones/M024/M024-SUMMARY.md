---
id: M024
title: "启动时更新检查进度动画"
status: complete
completed_at: 2026-05-07T09:19:21.653Z
key_decisions:
  - 使用 Canvas + Ellipse + StrokeDashArray 虚线描边实现旋转视觉效果，而非 GIF 或第三方控件 — 纯 XAML 声明式实现，零外部依赖
  - ShowCheckingAnimation 同时覆盖自动检查（Checking 状态）和手动检查（IsCheckingUpdate 属性）两条路径 — 单一计算属性聚合多状态标志
  - 将 CurrentUpdateStatus = Checking 移到 Task.Delay(5000) 之前 — 消除 5 秒无反馈等待期
key_files:
  - ViewModels/UpdateStatusViewModel.cs
  - Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs
  - MainWindow.xaml
lessons_learned:
  - WPF 动画生命周期用 DataTrigger + BeginStoryboard/StopStoryboard 管理比代码后台手动控制更可靠，避免忘记停止导致资源泄漏
  - 计算属性聚合多个布尔标志（如 ShowCheckingAnimation = Checking || IsCheckingUpdate）是 WPF MVVM 中减少 XAML 绑定复杂度的有效模式
  - UI 反馈类功能在代码级验证外必须补充运行时截图证据——纯单元测试无法证明视觉效果的实际呈现
---

# M024: 启动时更新检查进度动画

**启动时状态栏立刻显示旋转 spinner 动画，覆盖 5 秒延迟和整个更新检查过程，检查完成后自动切换为结果状态**

## What Happened

## What Happened

M024 was a single-slice milestone focused on a specific UX pain point: during application startup, the status bar showed no visual feedback for 5-8 seconds while the update check was running (5-second delay + network request). Users perceived this as the application being frozen.

### Implementation

The fix was minimal and surgical:

1. **ShowCheckingAnimation computed property** (T01): Added a new computed property to `UpdateStatusViewModel` that returns `true` when either `CurrentUpdateStatus == Checking` (automatic check path) or `IsCheckingUpdate == true` (manual check path). The `CurrentUpdateStatus = Checking` assignment was moved before the `Task.Delay(5000)` call so the spinner appears instantly on startup. PropertyChanged notifications were wired through partial methods.

2. **XAML spinner animation** (T02): Added a Canvas + Ellipse element in the status bar with `StrokeDashArray="2 2.5"` dashed stroke for visual effect. A `DoubleAnimation` rotates from 0° to 360° in a 1-second infinite loop. `DataTrigger` bound to `ShowCheckingAnimation` controls `BeginStoryboard`/`StopStoryboard` for animation lifecycle. Canvas visibility uses `BooleanToVisibilityConverter`.

### Verification

- 5 new unit tests covering all state combinations for `ShowCheckingAnimation`
- Full test suite: 274 unit + 27 E2E tests, 0 failures
- Build: 0 errors, 0 warnings
- Runtime screenshots confirmed spinner visible during check and disappeared after completion

### Key Patterns

The computed-property-aggregation pattern (single property combining multiple boolean flags) proved effective at reducing XAML binding complexity. The DataTrigger + BeginStoryboard/StopStoryboard pattern for WPF animation lifecycle management avoids manual code-behind animation control.

## Success Criteria Results

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| 1 | 启动后状态栏立刻显示旋转 spinner | ✅ MET | `CurrentUpdateStatus = Checking` moved before `Task.Delay(5000)`; XAML DataTrigger + BooleanToVisibilityConverter; runtime screenshot `operational-screenshot-full.png` shows spinner visible |
| 2 | 检查完成后 spinner 消失，结果显示 | ✅ MET | ShowCheckingAnimation returns false when not Checking; screenshot `operational-screenshot-after-check.png` confirms spinner gone, "已是最新版本" displayed |
| 3 | 手动点击"检查更新"同样显示 spinner | ✅ MET | ShowCheckingAnimation checks IsCheckingUpdate path; unit test covers manual trigger |
| 4 | 无编译错误，无测试回归 | ✅ MET | dotnet build: 0 errors, 0 warnings; 274 unit + 27 E2E tests all pass |

## Definition of Done Results

- [x] All slices complete: S01 status=complete, all 2 tasks done
- [x] All slice summaries exist: S01-SUMMARY.md present with full narrative
- [x] Cross-slice integration verified: Single slice milestone, trivially passes
- [x] Code change verification: 3 non-.gsd files modified (MainWindow.xaml, UpdateStatusViewModel.cs, UpdateStatusViewModelTests.cs), +106/-21 lines
- [x] Build succeeds: dotnet build 0 errors 0 warnings
- [x] Test suite passes: 274 unit + 27 E2E tests, 0 failures
- [x] Runtime evidence captured: 3 operational screenshots confirming spinner visibility and lifecycle

## Requirement Outcomes

No requirement status transitions in this milestone. R076 and R077 are scoped to other work. M024's scope (startup animation) was not tracked as a standalone requirement in REQUIREMENTS.md — it was a UX improvement driven by the milestone brief (M024-CONTEXT.md).

## Deviations

None.

## Follow-ups

None.
