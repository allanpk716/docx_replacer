# M011-ns0oo0: 更新体验修复

**Gathered:** 2026-04-30
**Status:** Ready for planning

## Project Description

修复更新功能的两个用户体验问题：(1) 更新设置窗口不回显 appsettings.json 中已配置的 UpdateUrl；(2) 下载更新时没有进度反馈。两个问题都涉及 UpdateService 和 WPF UI 的交互层。

## Why This Milestone

这两个问题直接影响用户对更新功能的信任度。看不到 URL 配置意味着用户无法确认更新源是否正确；没有下载进度意味着用户不知道更新是否在正常进行。两者都是高频交互场景。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 打开更新设置窗口时，在 URL 输入框中看到当前 appsettings.json 配置的 UpdateUrl（如 `http://172.18.200.47:30001`），Channel 下拉框也正确显示当前通道
- 确认下载更新后，看到一个独立的模态进度窗口，显示进度条（0-100%）、下载速度（MB/s）和预估剩余时间
- 在下载过程中点击取消按钮停止下载，应用继续正常运行

### Entry point / environment

- Entry point: GUI 状态栏"检查更新"按钮 + 齿轮图标设置按钮
- Environment: 已安装版 DocuFiller 桌面应用

## Completion Class

- Contract complete means: 单元测试验证 URL 剥离逻辑正确、进度计算逻辑正确
- Integration complete means: WPF 窗口正确绑定数据、Velopack 回调正确驱动 UI 更新
- Operational complete means: 从已有 UpdateUrl 配置启动应用后打开设置窗口能看到 URL；下载更新时进度弹窗正常工作

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 配置了 HTTP UpdateUrl 的 appsettings.json 启动后，打开更新设置窗口能看到正确的原始 URL（不含通道后缀）
- 有新版本时点击下载，进度弹窗弹出并实时更新百分比、速度、剩余时间
- 取消按钮能中断下载，应用不退出

## Architectural Decisions

### 下载进度弹窗形态

**Decision:** 独立模态 WPF Window，阻塞主窗口，含进度条、速度、剩余时间、取消按钮

**Rationale:** 用户明确要求"类似安装进度"的独立窗口，阻塞式等待下载完成。速度和剩余时间基于 Velopack 进度回调 + VelopackAsset.Size 计算。

**Alternatives Considered:**
- 状态栏嵌入式进度条 — 不够醒目，用户容易忽略
- 非模态窗口 — 用户可能在下载中误操作主窗口功能

### URL 回显修复策略

**Decision:** 在 UpdateSettingsViewModel 构造函数中直接从 IConfiguration 读取 UpdateUrl 原始值（不含通道后缀），而非从 UpdateService.EffectiveUpdateUrl 剥离

**Rationale:** EffectiveUpdateUrl 是拼接后的完整 URL（含 /{channel}/ 后缀），剥离逻辑容易出边界问题。IConfiguration 中的 Update:UpdateUrl 就是用户输入的原始值，直接读取更可靠。

**Alternatives Considered:**
- 修复 EffectiveUpdateUrl 剥离逻辑 — 根本问题是数据源不对，EffectiveUpdateUrl 本身就不是用户输入的原始值

## Error Handling Strategy

- URL 读取失败：显示空字符串，不崩溃（和当前行为一致）
- 下载失败：进度弹窗显示错误信息，关闭弹窗回到主窗口
- 取消操作：捕获 OperationCanceledException，进度弹窗显示"下载已取消"后自动关闭
- 进度回调异常：回调在后台线程，需要 Dispatcher.InvokeAsync 更新 UI

## Risks and Unknowns

- URL 不显示的具体根因需要运行时调试确认 — 可能是 IConfiguration 的 reloadOnChange 延迟、UpdateService Singleton 缓存、或字符串剥离边界问题
- Velopack 进度回调频率未知 — 如果回调过于频繁（如每 1%）需要节流 UI 更新；如果回调稀疏则速度计算不准确
- VelopackAsset.Size 是否总是有有效值 — delta 更新和 full 更新的 Size 可能不同

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — UpdateService Singleton，构造函数从 IConfiguration 读取配置，EffectiveUpdateUrl 含通道后缀
- `Services/Interfaces/IUpdateService.cs` — 接口定义，含 EffectiveUpdateUrl、Channel、DownloadUpdatesAsync(progressCallback)
- `ViewModels/UpdateSettingsViewModel.cs` — 当前从 EffectiveUpdateUrl 剥离后缀获取原始 URL
- `ViewModels/MainWindowViewModel.cs` — CheckUpdateAsync 中下载调用，当前 progress callback 只写 LogDebug
- `DocuFiller/Views/UpdateSettingsWindow.xaml` — 设置窗口 XAML
- `appsettings.json` — Update:UpdateUrl 和 Update:Channel 配置节点

## Relevant Requirements

- R047 — URL 回显修复（M011/S01）
- R048 — 下载进度弹窗（M011/S02）
- R049 — 下载取消支持（M011/S02）

## Scope

### In Scope

- 修复 UpdateSettingsViewModel 的 URL 读取逻辑
- 新建 DownloadProgressWindow + ViewModel
- MainWindowViewModel.CheckUpdateAsync 集成进度弹窗
- 进度计算（速度、剩余时间）
- 取消功能（CancellationToken）

### Out of Scope / Non-Goals

- 更新设置窗口的其他 UI 改进
- Velopack 框架本身的修改
- CLI update 命令的进度输出（当前已是 JSONL 格式，不需要改动）

## Technical Constraints

- WPF MVVM 模式，进度窗口需要独立的 ViewModel
- Velopack DownloadUpdatesAsync 的 progress 回调在后台线程执行，UI 更新需 Dispatcher
- UpdateService 是 Singleton，UpdateSettingsViewModel 是 Transient
- VelopackAsset.Size 单位为字节

## Integration Points

- IUpdateService.DownloadUpdatesAsync — progress callback + CancellationToken
- IConfiguration["Update:UpdateUrl"] — 原始用户输入 URL
- MainWindowViewModel.CheckUpdateAsync — 下载流程入口

## Testing Requirements

- URL 剥离/读取逻辑的单元测试
- 进度计算（速度、剩余时间）的单元测试
- 手动集成验证：配置 URL 后打开设置窗口确认显示
- 手动集成验证：下载更新确认进度弹窗工作

## Acceptance Criteria

- S01: 打开更新设置窗口，URL 输入框显示 appsettings.json 中的 UpdateUrl 原始值（不含通道后缀），Channel 下拉框显示当前通道
- S02: 确认下载后弹出模态进度窗口，实时显示百分比进度条、下载速度（MB/s）、预估剩余时间；取消按钮能中断下载

## Open Questions

- Velopack 进度回调的频率和精度 — 执行时通过调试确认
