# S03: GUI 状态栏常驻更新提示 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26T11:19:52.922Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: 功能是 WPF UI 绑定 + ViewModel 逻辑，没有独立运行时进程。通过构建验证（编译+测试）和代码审查确认功能正确性。

## Preconditions

- .NET 8 SDK 已安装
- 工作目录在 M009-q7p4iu worktree

## Smoke Test

1. 执行 `dotnet build -c Release`，确认 0 错误
2. **Expected:** Build succeeded

## Test Cases

### 1. 编译验证

1. 执行 `dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded"`
2. **Expected:** 输出仅含 "Build succeeded"，无 error CS 或 error MC

### 2. 全量测试通过

1. 执行 `dotnet test --no-build -c Release`
2. **Expected:** 总计 172 测试全部通过（0 failed, 0 skipped）

### 3. UpdateStatus 枚举完整性

1. 读取 `ViewModels/MainWindowViewModel.cs`，搜索 `enum UpdateStatus`
2. **Expected:** 包含 None, PortableVersion, UpdateAvailable, UpToDate, Checking, Error 六个值

### 4. ViewModel 属性绑定链

1. 读取 `ViewModels/MainWindowViewModel.cs`，验证 CurrentUpdateStatus setter 触发 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus 的 PropertyChanged
2. **Expected:** setter 内包含三个 PropertyChanged 调用，分别对应三个属性名

### 5. XAML 状态栏绑定

1. 读取 `MainWindow.xaml`，定位 UpdateStatusText TextBlock
2. **Expected:** Text 绑定 UpdateStatusMessage，Foreground 绑定 UpdateStatusBrush，Visibility 绑定 HasUpdateStatus + BooleanToVisibilityConverter，InputBindings 包含 MouseBinding→UpdateStatusClickCommand

### 6. 现有检查更新按钮保留

1. 读取 `MainWindow.xaml`，定位"检查更新"按钮
2. **Expected:** 按钮存在，Grid.Column="3"，Command 绑定不变

### 7. 启动时自动检查逻辑

1. 读取 `ViewModels/MainWindowViewModel.cs`，搜索 `InitializeUpdateStatusAsync`
2. **Expected:** 构造函数末尾有 `_ = InitializeUpdateStatusAsync()` fire-and-forget 调用；方法内包含 _updateService null 检查、IsInstalled 检查、IsUpdateUrlConfigured 检查、CheckForUpdatesAsync 调用、try-catch 异常处理

## Edge Cases

### 更新服务未注册

1. _updateService 为 null 时
2. **Expected:** InitializeUpdateStatusAsync 直接 return，不设置任何状态，状态栏提示不可见（HasUpdateStatus = false）

### 检查更新失败

1. CheckForUpdatesAsync 抛出异常时
2. **Expected:** CurrentUpdateStatus 设为 Error，UpdateStatusMessage 显示"检查更新失败"，UpdateStatusBrush 为红色

### 便携版运行

1. IsInstalled 返回 false 时
2. **Expected:** CurrentUpdateStatus 设为 PortableVersion，显示"便携版不支持自动更新"灰色提示

## Failure Signals

- dotnet build 报 error CS/MC → XAML 绑定或 ViewModel 代码有编译错误
- dotnet test 失败 → 现有功能回归
- CurrentUpdateStatus setter 未触发派生属性通知 → 状态栏不更新

## Not Proven By This UAT

- 实际 GUI 运行时视觉效果（需人工启动应用验证）
- CheckForUpdatesAsync 与真实 GitHub Releases/内网服务器的交互
- 下载更新和重启流程（由现有 CheckUpdateAsync 提供，非本 slice 改动）

## Notes for Tester

- 本 slice 仅新增状态栏常驻提示，不修改现有"检查更新"按钮的功能
- IUpdateService 是可选注入，服务未注册时不崩溃，仅静默跳过
- 所有状态判定结果输出 Information 级别日志，可通过日志观察运行时状态
