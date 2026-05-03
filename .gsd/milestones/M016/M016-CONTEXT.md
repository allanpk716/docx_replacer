# M016: 窗口置顶开关 + 拖放提示

**Gathered:** 2026-05-03
**Status:** Ready for planning

## Project Description

在主窗口标题栏添加窗口置顶（Topmost）开关按钮，并在关键词替换 tab 的路径 TextBox 下方添加拖放提示文字。

## Why This Milestone

用户反馈：1) 对照多个文档操作时需要窗口始终在前台，目前只能通过系统任务栏右键置顶；2) UI 紧凑化后拖放区域视觉提示消失，用户误以为拖放功能丢失。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 点击标题栏右侧的图钉按钮切换窗口置顶状态，按钮颜色/图标随状态变化
- 在关键词替换 tab 看到模板和数据 TextBox 下方的拖放提示文字

### Entry point / environment

- Entry point: 主窗口 GUI
- Environment: WPF 桌面应用

## Completion Class

- Contract complete means: 图钉按钮切换 Topmost + 拖放提示文字可见
- Integration complete means: 不影响现有拖放功能、Tab 切换、状态栏等其他 UI
- Operational complete means: 编译通过，现有测试不回归

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 图钉按钮点击后 Window.Topmost 属性切换，再次点击恢复
- 拖放提示文字在关键词替换 tab 可见，审核清理 tab 不受影响
- dotnet build 编译通过

## Architectural Decisions

### 标题栏按钮实现方式

**Decision:** 使用 WindowChrome 将标题栏扩展到客户区，在 DockPanel 顶部放置自定义标题栏（含图钉按钮 + 系统按钮）

**Rationale:** 标准 WPF 标题栏无法添加自定义按钮。WindowChrome 是最轻量的方案，不需要完全自绘窗口，保留系统窗口行为（拖动、缩放、Aero Snap）。

**Alternatives Considered:**
- 无框窗口 + 完全自绘标题栏 — 工作量大，需要手动实现拖动/缩放/Aero Snap
- 状态栏放置按钮 — 用户期望不一致，标题栏是置顶控件的惯例位置

### 拖放提示文字

**Decision:** 在模板和数据 TextBox 下方各加一行小字 TextBlock，浅灰色 11px

**Rationale:** 最小改动，不改变现有布局结构，仅增加视觉提示。

## Error Handling Strategy

无特殊错误处理。Topmost 是 Window 属性，不存在失败场景。

## Risks and Unknowns

- WindowChrome 可能影响窗口拖放行为（AllowDrop + PreviewDragOver）— 需验证

## Existing Codebase / Prior Art

- `MainWindow.xaml` — 窗口定义，AllowDrop=True，PreviewDragOver 事件
- `MainWindow.xaml.cs` — code-behind，拖放事件处理
- `ViewModels/MainWindowViewModel.cs` — ViewModel，可扩展 IsTopmost 属性
- `App.xaml` — 全局样式资源

## Relevant Requirements

- R057 — 窗口置顶开关
- R058 — 拖放提示文字

## Scope

### In Scope

- 标题栏图钉按钮（WindowChrome + 自定义标题栏）
- 关键词替换 tab 拖放提示文字
- ViewModel 添加 IsTopmost 属性

### Out of Scope / Non-Goals

- 置顶状态持久化（关闭后不记忆）
- 审核清理 tab 拖放区域改动
- CLI 模式任何改动

## Technical Constraints

- .NET 8 WPF
- 保持 900x550 窗口尺寸
- 不破坏现有拖放功能

## Integration Points

- MainWindow.xaml — 窗口模板改动
- MainWindowViewModel.cs — 新增属性和命令
- App.xaml — 可能需要添加标题栏按钮样式

## Testing Requirements

- dotnet build 编译通过
- 现有测试不回归
- 手动验证：图钉按钮切换、拖放仍可用

## Acceptance Criteria

- 标题栏右侧可见图钉按钮，点击切换 Topmost
- 置顶状态时图钉按钮视觉高亮
- 关键词替换 tab 模板和数据 TextBox 下方有拖放提示
- 审核清理 tab 不受影响

## Open Questions

- None
