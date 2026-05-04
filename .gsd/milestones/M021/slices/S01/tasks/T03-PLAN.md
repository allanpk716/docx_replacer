---
estimated_steps: 23
estimated_files: 4
skills_used: []
---

# T03: Refactor MainWindowVM to coordinator + wire XAML bindings

将 MainWindowViewModel 重构为纯协调器，持有 FillVM 和 UpdateStatusVM 子 ViewModel 引用。更新 MainWindow.xaml 绑定路径和 MainWindow.xaml.cs code-behind。

**MainWindowViewModel 协调器职责**：
- 属性：FillVM（FillViewModel 只读属性）、UpdateStatusVM（UpdateStatusViewModel 只读属性）、IsTopmost
- 命令：ToggleTopmostCommand, ExitCommand（引用 FillVM.IsProcessing 判断退出确认）
- 保留全部 cleanup 域代码（S02 处理）
- 构造函数：接收子 VM 实例或自行创建

**MainWindow.xaml 绑定变更**：
- Tab 1 (关键词替换) DockPanel：添加 `DataContext="{Binding FillVM}"`，所有子元素绑定路径不变
- 状态栏：`{Binding CurrentVersion}` → `{Binding UpdateStatusVM.CurrentVersion}`，`{Binding UpdateStatusMessage}` → `{Binding UpdateStatusVM.UpdateStatusMessage}` 等
- 状态栏 ProgressMessage：`{Binding ProgressMessage}` → `{Binding FillVM.ProgressMessage}`
- Tab 2 (审核清理)：保持不变（cleanup 仍在 MainWindowVM）
- 标题栏 ToggleTopmostCommand：保持不变

**MainWindow.xaml.cs code-behind 变更**：
- `viewModel.DataPath = filePath` → `viewModel.FillVM.DataPath = filePath`
- `viewModel.HandleSingleFileDropAsync(path)` → `viewModel.FillVM.HandleSingleFileDropAsync(path)`
- `viewModel.HandleFolderDropAsync(path)` → `viewModel.FillVM.HandleFolderDropAsync(path)`
- `viewModel.PreviewDataCommand?.Execute(null)` → `viewModel.FillVM.PreviewDataCommand.Execute(null)`
- OnClosing 中 `viewModel.IsProcessing` → `viewModel.FillVM.IsProcessing`
- `viewModel.CancelProcessCommand?.Execute(null)` → `viewModel.FillVM.CancelProcessCommand.Execute(null)`
- Cleanup 域 code-behind 保持不变

**DI 注册**：
- App.xaml.cs 添加 FillViewModel 和 UpdateStatusViewModel 的注册
- MainWindowViewModel 构造函数接收子 VM 实例

## Inputs

- `ViewModels/FillViewModel.cs`
- `ViewModels/UpdateStatusViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `App.xaml.cs`

## Expected Output

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `App.xaml.cs`

## Verification

cd C:/WorkSpace/agent/docx_replacer && dotnet build 2>&1 | Select-Object -Last 5 && dotnet test --no-build 2>&1 | Select-Object -Last 10
