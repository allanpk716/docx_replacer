---
estimated_steps: 6
estimated_files: 4
skills_used: []
---

# T01: WindowChrome 标题栏 + 图钉按钮 + 拖放提示

1. MainWindowViewModel 添加 IsTopmost 属性和 ToggleTopmostCommand 命令
2. MainWindow.xaml 添加 WindowChrome，在 DockPanel 顶部放自定义标题栏（图标+标题+图钉按钮）
3. 图钉按钮绑定 ToggleTopmostCommand，IsTopmost 时高亮显示
4. 关键词替换 tab 模板和数据 TextBox 下方各加一行 TextBlock 拖放提示（11px, 浅灰色）
5. dotnet build 验证编译通过
6. 验证窗口拖放功能不受 WindowChrome 影响

## Inputs

- `M016-CONTEXT.md`
- `MainWindow.xaml (current)`

## Expected Output

- `MainWindow.xaml — WindowChrome + 自定义标题栏 + 图钉按钮 + 拖放提示`

## Verification

dotnet build
