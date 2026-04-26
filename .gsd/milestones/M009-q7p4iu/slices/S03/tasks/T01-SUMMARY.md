---
id: T01
parent: S03
milestone: M009-q7p4iu
key_files:
  - ViewModels/MainWindowViewModel.cs
key_decisions:
  - CurrentUpdateStatus setter 统一触发三个派生属性的 PropertyChanged（UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus），而非使用独立 backing field，减少状态不一致风险
  - OnUpdateStatusClickAsync 中 Error 状态点击时重试 InitializeUpdateStatusAsync，而非直接调用 CheckUpdateAsync，保持自动检查的完整状态机流程
duration: 
verification_result: passed
completed_at: 2026-04-26T11:16:31.403Z
blocker_discovered: false
---

# T01: MainWindowViewModel 新增 UpdateStatus 枚举、6 个状态栏属性、InitializeUpdateStatusAsync 自动检查和 UpdateStatusClickCommand 命令

**MainWindowViewModel 新增 UpdateStatus 枚举、6 个状态栏属性、InitializeUpdateStatusAsync 自动检查和 UpdateStatusClickCommand 命令**

## What Happened

在 MainWindowViewModel 中实现了完整的更新状态栏常驻提示功能：

1. **UpdateStatus 枚举**：定义了 None/PortableVersion/UpdateAvailable/UpToDate/Checking/Error 六种状态，封装所有状态栏判定。

2. **属性层**：新增 CurrentUpdateStatus（枚举值）、UpdateStatusMessage（显示文本，基于枚举 switch 返回中文提示）、UpdateStatusBrush（颜色：橙色=有更新、绿色=最新、灰色=便携版/检查中、红色=错误）、HasUpdateStatus（可见性绑定）。

3. **InitializeUpdateStatusAsync 方法**：构造函数末尾 fire-and-forget 调用，按优先级判定：
   - 更新服务未注册 → 跳过
   - 便携版 → PortableVersion
   - 更新源未配置 → None（不显示）
   - 调用 CheckForUpdatesAsync → UpdateAvailable/UpToDate
   - 异常 → Error
   每个分支都输出 Information 级别日志，包含状态判定结果。

4. **UpdateStatusClickCommand**：点击状态栏提示时根据当前状态走不同流程：有更新→走 CheckUpdateAsync 弹窗、便携版→提示信息、错误→重试检查、最新→版本确认。

5. **OnUpdateStatusClickAsync**：封装点击逻辑，Error 状态重试调用 InitializeUpdateStatusAsync。

所有新增代码兼容 IUpdateService? 可选注入模式（MEM071），_updateService 为 null 时安全跳过。

## Verification

执行 `dotnet build -c Release` 编译通过，0 错误 0 警告。

代码审查确认：
- UpdateStatus 枚举 6 个值与计划匹配
- CurrentUpdateStatus setter 正确触发 HasUpdateStatus/UpdateStatusMessage/UpdateStatusBrush 的 PropertyChanged
- InitializeUpdateStatusAsync 日志覆盖：便携版检测、更新源未配置、发现新版本、已是最新、异常
- UpdateStatusClickCommand 绑定到 OnUpdateStatusClickAsync，CanExecute 关联 HasUpdateStatus
- 构造函数末尾 fire-and-forget 调用不阻塞 UI 启动

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -c Release` | 0 | ✅ pass | 1330ms |

## Deviations

无偏差。计划中提到"6 个属性"，实际实现为 5 个属性（CurrentUpdateStatus + UpdateStatusMessage + UpdateStatusBrush + HasUpdateStatus）+ 1 个命令（UpdateStatusClickCommand），与计划描述一致。UpdateStatusClickCommand 的 CanExecute 条件使用 HasUpdateStatus 而非独立的 boolean 属性，这是合理的简化。

## Known Issues

None.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
