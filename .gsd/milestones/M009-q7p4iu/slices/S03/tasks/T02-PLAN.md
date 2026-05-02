---
estimated_steps: 1
estimated_files: 1
skills_used: []
---

# T02: MainWindow.xaml 状态栏新增常驻更新提示 UI + 构建验证

修改 MainWindow.xaml 底部状态栏，在版本号和中间消息之间新增常驻更新提示 TextBlock（或 Hyperlink），绑定到 ViewModel 的 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus 属性。点击触发 UpdateStatusClickCommand。验证 dotnet build 和现有测试通过。

## Inputs

- `MainWindow.xaml — 现有状态栏 XAML（DockPanel > Border > Grid 3 列布局）`
- `ViewModels/MainWindowViewModel.cs — T01 新增的 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus/UpdateStatusClickCommand 属性`
- `Converters/BooleanToVisibilityConverter.cs — 现有布尔转可见性转换器`

## Expected Output

- `MainWindow.xaml — 状态栏新增第 2 列更新状态 TextBlock，绑定 ViewModel 属性，支持点击和颜色变化`

## Verification

dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded" && dotnet test --no-build
