---
estimated_steps: 11
estimated_files: 3
skills_used: []
---

# T01: Create UpdateStatusViewModel with CT.Mvvm

从 MainWindowViewModel 提取更新域属性和命令到新文件 ViewModels/UpdateStatusViewModel.cs。此 ViewModel 管理：状态栏更新提示（UpdateStatusMessage, UpdateStatusBrush, HasUpdateStatus）、版本检查（CheckUpdateAsync, InitializeUpdateStatusAsync）、更新下载弹窗流程（OnUpdateStatusClickAsync）、更新源设置（OpenUpdateSettings）。使用 CT.Mvvm [ObservableProperty] + [RelayCommand] 模式，遵循项目已有的 DownloadProgressViewModel 模式（partial class + CommunityToolkit.Mvvm.ComponentModel.ObservableObject + [ObservableProperty] + [RelayCommand]）。UpdateStatus 枚举移入此文件。ExtractHostFromUrl 辅助方法跟随迁移。

**关键约束**：
- 必须使用完全限定名 CommunityToolkit.Mvvm.ComponentModel.ObservableObject 避免与项目自定义 ObservableObject.cs 冲突
- 类必须标记为 `partial class`
- CanExecute 逻辑通过 partial OnXxxChanged 方法调用 NotifyCanExecuteChanged
- 保持现有 ILogger 结构化日志不变
- CheckUpdateAsync 中创建 DownloadProgressViewModel 和 DownloadProgressWindow 的逻辑需要通过 IServiceProvider 或 Action 回调获取窗口实例

**CT.Mvvm 模式参考**（内联，避免外部文件依赖）：
- `[ObservableProperty] private string _statusMessage = string.Empty;` 自动生成 `public string StatusMessage { get; }`
- `[RelayCommand(CanExecute = nameof(CanCheck))] private async Task CheckAsync() { ... }` 自动生成 `public IAsyncRelayCommand CheckCommand { get; }`
- `partial void OnIsCheckingChanged(bool value) { CheckUpdateCommand.NotifyCanExecuteChanged(); }` 用于属性变更副作用

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`

## Expected Output

- `ViewModels/UpdateStatusViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`

## Verification

cd C:/WorkSpace/agent/docx_replacer && dotnet build 2>&1 | Select-Object -Last 5
