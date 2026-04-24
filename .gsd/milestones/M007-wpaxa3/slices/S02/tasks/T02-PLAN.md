---
estimated_steps: 1
estimated_files: 3
skills_used: []
---

# T02: MainWindow 底部状态栏 UI + 更新检查流程

在 `MainWindow.xaml` 底部添加状态栏，显示 `VersionHelper.GetCurrentVersion()` 版本号和"检查更新"按钮。在 `MainWindowViewModel` 中注入 `IUpdateService`，添加 `CurrentVersion` 属性、`CheckUpdateCommand` 命令、`IsCheckingUpdate`/`CanCheckUpdate` 属性。点击检查更新时异步调用 `IUpdateService.CheckForUpdatesAsync()`，根据结果显示自定义 MessageBox 对话框：有新版本→显示版本号并确认下载+重启，无新版本→显示"已是最新版本"，异常→显示错误信息。更新源未配置时 `CanCheckUpdate` 为 false，按钮灰显。

## Inputs

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Utils/VersionHelper.cs`
- `MainWindow.xaml`
- `ViewModels/MainWindowViewModel.cs`

## Expected Output

- `MainWindow.xaml`
- `ViewModels/MainWindowViewModel.cs`

## Verification

dotnet build && dotnet test
