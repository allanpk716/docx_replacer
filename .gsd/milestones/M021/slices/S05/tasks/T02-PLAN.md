---
estimated_steps: 16
estimated_files: 2
skills_used: []
---

# T02: 状态栏⚙设置按钮添加通知徽章（红色圆点）

在 MainWindow.xaml 的⚙设置按钮上叠加红色圆点徽章，当 UpdateStatusVM.CurrentUpdateStatus == UpdateAvailable 时显示。使用 Grid + Ellipse 实现，绑定到 UpdateStatusVM 的现有属性。

**Why**: R028 要求有新版本时显示通知徽章。当前状态栏只有文本提示，添加红色圆点徽章能提供更醒目的视觉指示。

**Steps**:
1. 在 UpdateStatusViewModel 中确认已有 `CurrentUpdateStatus` 属性和 `OnCurrentUpdateStatusChanged` 通知机制
2. 确认不需要新增属性——可直接使用 `HasUpdateStatus` + 颜色判断，或新增 `HasUpdateAvailable` 属性简化绑定
3. 新增简单计算属性：`public bool HasUpdateAvailable => CurrentUpdateStatus == UpdateStatus.UpdateAvailable;`
4. 在 `OnCurrentUpdateStatusChanged` 中添加 `OnPropertyChanged(nameof(HasUpdateAvailable));`
5. 修改 MainWindow.xaml 的⚙设置按钮区域：将 Button 包裹在 Grid 中，添加一个小红色 Ellipse（宽高 8-10px），设置 `Visibility` 绑定到 `UpdateStatusVM.HasUpdateAvailable`，使用 BooleanToVisibilityConverter
6. 红色圆点位置：设置在按钮右上角（HorizontalAlignment=Right, VerticalAlignment=Top, Margin 调整偏移）
7. 运行 `dotnet build` 确认编译通过

**Must-haves**:
- [ ] UpdateStatusViewModel 新增 HasUpdateAvailable 属性
- [ ] MainWindow.xaml ⚙按钮区域有红色圆点叠加
- [ ] 圆点仅在 UpdateAvailable 状态可见
- [ ] dotnet build 0 errors
- [ ] dotnet test 全部通过

## Inputs

- `ViewModels/UpdateStatusViewModel.cs`
- `MainWindow.xaml`

## Expected Output

- `ViewModels/UpdateStatusViewModel.cs`
- `MainWindow.xaml`

## Verification

dotnet build DocuFiller.csproj --no-restore && dotnet test Tests/DocuFiller.Tests/DocuFiller.Tests.csproj --no-restore --verbosity minimal
