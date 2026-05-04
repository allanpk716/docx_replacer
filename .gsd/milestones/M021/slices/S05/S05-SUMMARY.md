---
id: S05
parent: M021
milestone: M021
provides:
  - ["UpdateStatusViewModel 自动检查能力（5 秒延迟 + CancellationToken + 静默失败处理）", "⚙按钮红色圆点通知徽章 UI 绑定", "16 个 UpdateStatusViewModel 单元测试"]
requires:
  []
affects:
  []
key_files:
  - ["ViewModels/UpdateStatusViewModel.cs", "MainWindow.xaml", "Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs", "Tests/DocuFiller.Tests/Stubs/WindowStubs.cs"]
key_decisions:
  - ["使用 Task.Delay(5000) + CancellationToken 实现 5 秒延迟，OperationCanceledException 静默捕获", "使用 Grid + Ellipse 方式叠加红色圆点徽章，保持最小改动原则", "新增 HasUpdateAvailable 计算属性简化 XAML 绑定，避免 Converter 链", "测试使用反射直接调用 private InitializeUpdateStatusAsync 绕过 5 秒延迟"]
patterns_established:
  - ["延迟初始化模式：public InitializeAsync() 用 Task.Delay + CTS 包装，private 内部方法承载实际逻辑，便于测试", "通知徽章模式：计算属性 + OnChanged 通知链 + Grid 叠加 Ellipse"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-04T11:45:52.500Z
blocker_discovered: false
---

# S05: R028 启动自动检查更新 + 通知徽章

**实现 R028：应用启动 5 秒后自动静默检查更新，有新版本时⚙设置按钮显示红色圆点徽章**

## What Happened

## 交付内容

### T01: UpdateStatusViewModel 5 秒延迟自动检查 + 12 个单元测试
- 修改 `UpdateStatusViewModel.InitializeAsync()` 添加 5 秒 `Task.Delay(5000, _autoCheckCts.Token)` 延迟后再调用 `InitializeUpdateStatusAsync()`
- 新增 `_autoCheckCts`（CancellationTokenSource）字段支持 ViewModel 销毁时取消等待
- `OperationCanceledException` 静默捕获并记录日志，其他异常仍由现有 catch 块处理
- 创建 12 个单元测试覆盖：跳过逻辑（null 服务、未配置 URL）、成功路径（有更新/无更新）、异常处理、取消路径、派生属性验证
- 测试使用反射直接调用 private `InitializeUpdateStatusAsync` 绕过 5 秒延迟，确保快速执行
- 新增 `Tests/DocuFiller.Tests/Stubs/WindowStubs.cs` 满足 ViewModel 编译依赖

### T02: ⚙设置按钮红色圆点通知徽章
- 新增 `HasUpdateAvailable` 计算属性（`CurrentUpdateStatus == UpdateStatus.UpdateAvailable`）
- `OnCurrentUpdateStatusChanged` 中通知 `HasUpdateAvailable` 变更
- MainWindow.xaml 中将⚙按钮包裹在 Grid 中，叠加 8x8 红色 Ellipse，Visibility 绑定到 `UpdateStatusVM.HasUpdateAvailable`
- 新增 4 个单元测试验证 HasUpdateAvailable 在各状态下的正确返回值

## 验证结果
- `dotnet build DocuFiller.csproj --no-restore`: 0 errors, 0 warnings
- `dotnet test Tests/DocuFiller.Tests.csproj --no-restore --verbosity minimal`: 269/269 passed, 0 failed

## Verification

- dotnet build: 0 errors, 0 warnings ✅
- dotnet test: 269/269 passed, 0 failed ✅
- InitializeAsync 包含 5 秒 Task.Delay + CancellationToken ✅
- HasUpdateAvailable 属性 + OnCurrentUpdateStatusChanged 通知链 ✅
- MainWindow.xaml Grid+Ellipse 徽章 UI + BooleanToVisibilityConverter 绑定 ✅
- 16 个 UpdateStatusViewModel 单元测试全部通过 ✅
- 手动检查更新按钮仍独立工作（CheckUpdateCommand 未修改） ✅

## Requirements Advanced

- R028 — UpdateStatusViewModel.InitializeAsync() 添加 5 秒延迟自动检查，有新版本时 HasUpdateAvailable 触发红色圆点徽章

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
