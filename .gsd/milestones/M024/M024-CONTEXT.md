# M024: 启动时更新检查进度动画

**Gathered:** 2026-05-07
**Status:** Ready for planning

## Project Description

在应用启动时，状态栏右下角的更新区域立刻显示旋转动画（spinner），让用户知道程序正在检查更新。当前启动时有 5 秒延迟+网络请求，期间用户完全无反馈，感觉程序"卡了"。

## Why This Milestone

用户体验问题：启动后右下角更新区域空白约 5-8 秒（延迟+网络请求），用户不知道程序在做什么。需要从启动瞬间就提供视觉反馈。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 启动程序后立刻在状态栏右下角看到旋转的加载动画
- 检查完成后动画停止，切换为结果状态（✓ 已是最新 / ✗ 检查失败）
- 手动点击"检查更新"时同样显示旋转动画

### Entry point / environment

- Entry point: GUI 模式启动应用
- Environment: Windows 桌面，WPF

## Completion Class

- Contract complete means: UpdateStatusViewModel 状态机扩展完成，新属性触发通知正确
- Integration complete means: XAML 动画资源正确加载，spinner 在 Checking 状态可见并旋转
- Operational complete means: dotnet build 0 错误，现有测试全部通过

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 启动后状态栏立刻出现旋转动画（Checking 状态绑定正确）
- 动画在检查完成后停止并切换为静态图标
- dotnet build 无错误，dotnet test 全部通过

## Architectural Decisions

### 动画样式选择

**Decision:** 旋转圆圈动画（Spinner），而非脉冲圆点

**Rationale:** 旋转动画过程感更强，用户更熟悉（通用加载指示器）。脉冲圆点过于含蓄。

**Alternatives Considered:**
- 脉冲呼吸灯圆点 — 更含蓄但过程感不够强

### 启动时序

**Decision:** 启动后立刻显示动画，不等 5 秒延迟

**Rationale:** 当前 5 秒延迟期间完全无反馈是主要痛点。立刻显示动画让用户知道程序在准备检查更新。

**Alternatives Considered:**
- 5 秒后才开始显示 — 简单但用户仍会经历无反馈期

### 实现策略

**Decision:** 在 InitializeAsync 中将 CurrentUpdateStatus = Checking 移到 Task.Delay 之前，XAML 添加旋转 Storyboard + spinner 元素

**Rationale:** 最小改动路径。Checking 状态已存在，只需提前设置。XAML 动画纯声明式，不需要额外控件库。

## Risks and Unknowns

- 无重大风险。改动范围小且集中。

## Existing Codebase / Prior Art

- `ViewModels/UpdateStatusViewModel.cs` — UpdateStatus 枚举已有 Checking 状态，InitializeAsync 有 5 秒延迟
- `MainWindow.xaml` — 状态栏已有 UpdateStatusText、版本号、⚙ 按钮、检查更新按钮
- `UpdateStatus.Checking` → 显示"正在检查更新..."灰色文字（已有，但只在延迟后出现）

## Relevant Requirements

- R078 — 启动时更新检查进度即时可见

## Scope

### In Scope

- InitializeAsync 中将 Checking 状态提前到延迟前
- 新增 ShowCheckingAnimation 计算属性
- MainWindow.xaml 添加旋转动画 Storyboard 资源
- 状态栏添加 spinner 元素，绑定到 ShowCheckingAnimation
- dotnet build + 测试验证

### Out of Scope / Non-Goals

- 下载进度流程的改动
- CLI 更新命令
- 更新设置弹窗改动
- 新增 WPF 自定义控件（使用内联 XAML 动画）

## Technical Constraints

- 不引入第三方动画库
- 动画不能影响 UI 线程性能
- 保持现有状态栏布局不变（版本号 + 状态文字 + ⚙ + 检查更新按钮）

## Integration Points

- UpdateStatusViewModel → MainWindow.xaml 数据绑定

## Testing Requirements

- dotnet build 0 错误
- dotnet test 全部通过
- 动画可见性由 ViewModel 状态驱动，通过单元测试验证状态转换

## Acceptance Criteria

- 程序启动后状态栏立刻显示旋转 spinner（不等 5 秒）
- 检查完成后 spinner 消失，结果状态文字正常显示
- 手动点击"检查更新"同样显示 spinner
- 无编译错误，无测试回归

## Open Questions

- None — 范围明确
