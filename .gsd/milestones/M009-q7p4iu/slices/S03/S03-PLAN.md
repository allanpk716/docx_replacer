# S03: GUI 状态栏常驻更新提示

**Goal:** GUI 启动后状态栏常驻显示更新状态——未配置更新源时提醒配置、有新版本时提醒更新、便携版运行时提示使用安装版；点击提示走现有弹窗更新流程
**Demo:** After this: GUI 启动后状态栏常驻显示更新状态——未配置更新源/有新版本可用/便携版不支持自动更新；点击提示走现有弹窗更新流程

## Must-Haves

- 状态栏新增常驻更新提示区域，显示三种状态之一：便携版提示、有新版本可用、无更新
- 便携版运行时显示"便携版不支持自动更新"提示（IsInstalled == false）
- 有新版本时显示"有新版本可用，点击更新"提示
- 点击提示文字触发现有 CheckUpdateAsync 弹窗流程
- 现有"检查更新"按钮保持不变，继续工作
- dotnet build 通过，现有测试不被破坏

## Proof Level

- This slice proves: integration

## Integration Closure

Upstream surfaces consumed:
- IUpdateService.IsInstalled — 判断是否安装版
- IUpdateService.CheckForUpdatesAsync() — 检查新版本
- IUpdateService.IsUpdateUrlConfigured — 始终 true

New wiring:
- MainWindowViewModel 新增 UpdateStatusMessage / UpdateStatusColor 属性
- MainWindowViewModel 构造后调用自动检查更新
- MainWindow.xaml 状态栏新增 TextBlock 绑定到 ViewModel 属性

What remains: S04 CLI update 命令（独立 slice）

## Verification

- Runtime signals: MainWindowViewModel 自动检查更新时输出 Information 级别日志，包含状态判定结果（便携版/有更新/无更新/检查失败）
- Inspection surfaces: ViewModel 属性 UpdateStatusMessage 可通过 WPF 绑定观察
- Failure visibility: 检查更新失败时日志记录异常，状态栏显示"检查更新失败"

## Tasks

- [x] **T01: MainWindowViewModel 新增更新状态属性和自动检查逻辑** `est:45m`
  在 MainWindowViewModel 中新增更新状态判定属性（UpdateStatusMessage、UpdateStatusBrush、HasUpdateStatus、UpdateStatusClickCommand）和启动时自动检查更新逻辑。定义 UpdateStatus 枚举（None/PortableVersion/UpdateAvailable/UpToDate/Checking/Error）封装状态判定，构造函数末尾调用 InitializeUpdateStatusAsync 初始化常驻提示状态。
  - Files: `ViewModels/MainWindowViewModel.cs`
  - Verify: dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"Build succeeded"

- [x] **T02: MainWindow.xaml 状态栏新增常驻更新提示 UI + 构建验证** `est:30m`
  修改 MainWindow.xaml 底部状态栏，在版本号和中间消息之间新增常驻更新提示 TextBlock（或 Hyperlink），绑定到 ViewModel 的 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus 属性。点击触发 UpdateStatusClickCommand。验证 dotnet build 和现有测试通过。
  - Files: `MainWindow.xaml`
  - Verify: dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded" && dotnet test --no-build

## Files Likely Touched

- ViewModels/MainWindowViewModel.cs
- MainWindow.xaml
