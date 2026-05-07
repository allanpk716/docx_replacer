---
phase: complete-milestone
phase_name: Complete Milestone M024
project: DocuFiller
generated: 2026-05-07T16:00:00Z
counts:
  decisions: 2
  lessons: 2
  patterns: 2
  surprises: 0
missing_artifacts: []
---

# M024: 启动时更新检查进度动画 — Learnings

### Decisions

- **旋转圆圈动画（Spinner）而非脉冲圆点**：选择 Canvas + Ellipse + StrokeDashArray 虚线描边实现旋转视觉效果，而非 GIF 或第三方控件。理由：纯 XAML 声明式实现，零外部依赖，性能开销极低。
  Source: M024-CONTEXT.md/Architectural Decisions

- **启动后立刻显示动画**：将 `CurrentUpdateStatus = Checking` 赋值移到 `Task.Delay(5000)` 之前，消除 5 秒无反馈等待期。理由：用户体验痛点明确，改动最小。
  Source: M024-CONTEXT.md/Architectural Decisions

### Lessons

- **UI 反馈类功能需要运行时截图证据**：纯单元测试和代码审查无法证明视觉效果的实际呈现。M024 首轮验证（needs-attention）就是因为缺乏运行时截图，补充截图后才通过。
  Source: M024-VALIDATION.md/Verification Class Compliance

- **WPF 动画生命周期用 DataTrigger 管理更可靠**：DataTrigger + BeginStoryboard/StopStoryboard 比 C# 代码后台手动控制 Storyboard 更安全，避免忘记 StopStoryboard 导致动画资源泄漏或视觉残留。
  Source: S01-SUMMARY.md/Patterns Established

### Patterns

- **计算属性聚合多状态标志**：在 ViewModel 中创建一个计算属性（如 `ShowCheckingAnimation = Checking || IsCheckingUpdate`）聚合多个布尔标志，XAML 通过单一绑定控制 UI 元素可见性和动画生命周期。减少 XAML 绑定数量，降低复杂度。
  Source: S01-SUMMARY.md/Patterns Established

- **DataTrigger + Storyboard 动画控制模式**：WPF 中使用 DataTrigger 绑定 ViewModel 属性控制 BeginStoryboard/StopStoryboard，实现动画的声明式启动/停止。避免代码后台手动管理动画状态。
  Source: S01-SUMMARY.md/Patterns Established

### Surprises

*(None)*
