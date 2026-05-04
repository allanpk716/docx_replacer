---
id: T02
parent: S05
milestone: M021
key_files:
  - ViewModels/UpdateStatusViewModel.cs
  - MainWindow.xaml
  - Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs
key_decisions:
  - 使用 Grid + Ellipse 方式叠加红色圆点徽章，而非 Adorner 或自定义控件，保持最小改动原则
  - 新增 HasUpdateAvailable 计算属性简化 XAML 绑定，避免 Converter 链
duration: 
verification_result: passed
completed_at: 2026-05-04T11:44:42.587Z
blocker_discovered: false
---

# T02: 在⚙设置按钮上添加红色圆点徽章，绑定 HasUpdateAvailable 属性，仅 UpdateAvailable 状态时可见

**在⚙设置按钮上添加红色圆点徽章，绑定 HasUpdateAvailable 属性，仅 UpdateAvailable 状态时可见**

## What Happened

实现了状态栏⚙设置按钮的红色圆点通知徽章：

1. **UpdateStatusViewModel 新增 HasUpdateAvailable 属性**：添加计算属性 `HasUpdateAvailable => CurrentUpdateStatus == UpdateStatus.UpdateAvailable`，仅在检测到新版本时返回 true。
2. **属性变更通知**：在 `OnCurrentUpdateStatusChanged` 中添加 `OnPropertyChanged(nameof(HasUpdateAvailable))` 确保 UI 绑定实时更新。
3. **MainWindow.xaml 徽章 UI**：将原有 Button 包裹在 Grid 中，添加 8x8 红色 Ellipse 叠加在按钮右上角。Visibility 绑定到 `UpdateStatusVM.HasUpdateAvailable`，使用 BooleanToVisibilityConverter 转换。
4. **新增 4 个单元测试**：验证 HasUpdateAvailable 在默认/UpdateAvailable/UpToDate/Error 状态下的正确返回值。加上原有测试中对 HasUpdateAvailable 的断言补充，总共 16 个测试全部通过。

徽章效果：当应用自动检测到新版本时（5秒延迟检查，T01 实现），⚙设置按钮右上角会出现一个小红色圆点提示用户有更新可用。

## Verification

dotnet build 通过（0 errors, 71 warnings），dotnet test UpdateStatusViewModel 全部 16 个测试通过（含新增 4 个 HasUpdateAvailable 测试）。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2650ms |
| 2 | `dotnet test ./Tests/DocuFiller.Tests.csproj --no-restore --filter UpdateStatusViewModel` | 0 | ✅ pass (16 tests, 0 failed) | 2300ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/UpdateStatusViewModel.cs`
- `MainWindow.xaml`
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`
