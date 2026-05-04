# S01: FillViewModel + UpdateStatusViewModel 拆分

**Goal:** 将 MainWindowViewModel（1623行）拆分为纯协调器 + FillViewModel（CT.Mvvm，关键词替换Tab全部业务逻辑）+ UpdateStatusViewModel（CT.Mvvm，更新状态管理）。XAML绑定从MainWindowVM切换到对应子VM。
**Demo:** 关键词替换 Tab 和更新状态功能全部正常工作，MainWindowVM 降至 400 行以内

## Must-Haves

- FillViewModel 创建为 partial class，继承 CommunityToolkit.Mvvm.ComponentModel.ObservableObject，使用 [ObservableProperty] + [RelayCommand]
- UpdateStatusViewModel 创建为 partial class，同样使用 CT.Mvvm 模式
- MainWindowViewModel 降为纯协调器（持有子VM引用 + cleanup域代码 + IsTopmost/ExitCommand）
- MainWindow.xaml Tab 1 的 DataContext 绑定到 FillViewModel
- 状态栏更新相关绑定指向 UpdateStatusViewModel（通过 MainWindowVM.UpdateStatusVM 属性）
- dotnet build 编译通过
- dotnet test 全部通过
- 现有功能行为不变

## Proof Level

- This slice proves: integration — 此 slice 跨越 ViewModel → XAML 绑定边界，需要编译通过 + 测试通过验证集成正确性

## Integration Closure

- Upstream surfaces consumed: MainWindowViewModel 的全部 fill/update 域属性和命令
- New wiring introduced: MainWindowVM 持有 FillVM/UpdateStatusVM 属性，XAML 通过子属性路径绑定，DI 容器注册新 ViewModel
- What remains: S02 复用 CleanupViewModel 消除 MainWindowVM 中 cleanup 域代码，S03 提取拖放为 DragDropBehavior

## Verification

- Runtime signals: ILogger 结构化日志跟随属性/命令迁移到子 VM，日志前缀变更为子 VM 类名
- Inspection surfaces: 编译错误和绑定失败通过 dotnet build + XAML binding errors 检测
- Failure state exposed: 若绑定路径错误，WPF Output 窗口会输出 BindingExpression 警告

## Tasks

- [ ] **T01: Create UpdateStatusViewModel with CT.Mvvm** `est:2h`
  从 MainWindowViewModel 提取更新域属性和命令到新文件 ViewModels/UpdateStatusViewModel.cs。此 ViewModel 管理：状态栏更新提示（UpdateStatusMessage, UpdateStatusBrush, HasUpdateStatus）、版本检查（CheckUpdateAsync, InitializeUpdateStatusAsync）、更新下载弹窗流程（OnUpdateStatusClickAsync）、更新源设置（OpenUpdateSettings）。使用 CT.Mvvm [ObservableProperty] + [RelayCommand] 模式，遵循项目已有的 DownloadProgressViewModel 模式（partial class + CommunityToolkit.Mvvm.ComponentModel.ObservableObject + [ObservableProperty] + [RelayCommand]）。UpdateStatus 枚举移入此文件。ExtractHostFromUrl 辅助方法跟随迁移。
  - Files: `ViewModels/UpdateStatusViewModel.cs`, `ViewModels/MainWindowViewModel.cs`, `App.xaml.cs`
  - Verify: cd C:/WorkSpace/agent/docx_replacer && dotnet build 2>&1 | Select-Object -Last 5

- [ ] **T02: Create FillViewModel with CT.Mvvm** `est:3h`
  从 MainWindowViewModel 提取关键词替换 Tab 全部业务逻辑到新文件 ViewModels/FillViewModel.cs。这是最大的提取任务（~700行），包含：
  - Files: `ViewModels/FillViewModel.cs`, `ViewModels/MainWindowViewModel.cs`, `App.xaml.cs`
  - Verify: cd C:/WorkSpace/agent/docx_replacer && dotnet build 2>&1 | Select-Object -Last 5

- [ ] **T03: Refactor MainWindowVM to coordinator + wire XAML bindings** `est:2h`
  将 MainWindowViewModel 重构为纯协调器，持有 FillVM 和 UpdateStatusVM 子 ViewModel 引用。更新 MainWindow.xaml 绑定路径和 MainWindow.xaml.cs code-behind。
  - Files: `ViewModels/MainWindowViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `App.xaml.cs`
  - Verify: cd C:/WorkSpace/agent/docx_replacer && dotnet build 2>&1 | Select-Object -Last 5 && dotnet test --no-build 2>&1 | Select-Object -Last 10

## Files Likely Touched

- ViewModels/UpdateStatusViewModel.cs
- ViewModels/MainWindowViewModel.cs
- App.xaml.cs
- ViewModels/FillViewModel.cs
- MainWindow.xaml
- MainWindow.xaml.cs
