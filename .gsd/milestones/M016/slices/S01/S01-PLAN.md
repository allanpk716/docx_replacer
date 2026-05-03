# S01: 窗口置顶按钮 + 拖放提示

**Goal:** 实现标题栏置顶开关和拖放提示文字，不破坏现有功能
**Demo:** 标题栏右侧可见图钉按钮，点击切换 Topmost；关键词替换 tab TextBox 下方有拖放提示文字

## Must-Haves

- 标题栏右侧有图钉按钮，点击切换 Window.Topmost\n- 置顶时按钮视觉高亮（如填充色变化）\n- 关键词替换 tab 模板和数据 TextBox 下方有浅灰色拖放提示文字\n- 审核清理 tab 和状态栏不受影响\n- dotnet build 编译通过

## Proof Level

- This slice proves: contract

## Integration Closure

WindowChrome 不破坏现有 AllowDrop + PreviewDragOver 拖放行为

## Verification

- 图钉按钮状态即 Topmost 属性值，无需额外日志

## Tasks

- [x] **T01: WindowChrome 标题栏 + 图钉按钮 + 拖放提示** `est:30min`
  1. MainWindowViewModel 添加 IsTopmost 属性和 ToggleTopmostCommand 命令
2. MainWindow.xaml 添加 WindowChrome，在 DockPanel 顶部放自定义标题栏（图标+标题+图钉按钮）
3. 图钉按钮绑定 ToggleTopmostCommand，IsTopmost 时高亮显示
4. 关键词替换 tab 模板和数据 TextBox 下方各加一行 TextBlock 拖放提示（11px, 浅灰色）
5. dotnet build 验证编译通过
6. 验证窗口拖放功能不受 WindowChrome 影响
  - Files: `MainWindow.xaml`, `MainWindow.xaml.cs`, `ViewModels/MainWindowViewModel.cs`, `App.xaml`
  - Verify: dotnet build

## Files Likely Touched

- MainWindow.xaml
- MainWindow.xaml.cs
- ViewModels/MainWindowViewModel.cs
- App.xaml
