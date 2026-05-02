---
id: M011-ns0oo0
title: "更新体验修复"
status: complete
completed_at: 2026-04-30T06:38:23.145Z
key_decisions:
  - D033: URL 回显直接从 IConfiguration 读取原始值，不从 EffectiveUpdateUrl 剥离
  - D034: 下载进度弹窗采用独立模态 WPF Window，阻塞主窗口
  - 累积平均速度计算（从第一个数据点开始累积）确保稳定性
  - 注入时间戳提供器和 Dispatcher 包装器实现 ViewModel 完全可单元测试
  - 调用方手动创建 ViewModel 传递运行时参数（totalBytes, version），通过 SetViewModel() 注入 Window
  - Task.Run + ShowDialog 模式实现后台下载 + 模态 UI
  - OnClosing 覆写路由到 CancelCommand 防止直接 X 按钮关闭
key_files:
  - ViewModels/UpdateSettingsViewModel.cs
  - ViewModels/DownloadProgressViewModel.cs
  - ViewModels/MainWindowViewModel.cs
  - DocuFiller/Views/DownloadProgressWindow.xaml
  - DocuFiller/Views/DownloadProgressWindow.xaml.cs
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
  - App.xaml.cs
  - Tests/UpdateSettingsViewModelTests.cs
  - Tests/DownloadProgressViewModelTests.cs
  - Tests/UpdateServiceTests.cs
lessons_learned:
  - IConfiguration 可直接注入 ViewModel 读取 appsettings.json 原始值，避免从运行时服务属性反推配置
  - 模态窗口需要运行时参数时，调用方手动创建 ViewModel 再通过 SetViewModel() 注入比 DI 工厂更灵活
  - 累积平均速度比瞬时速度更适合进度显示，避免 UI 抖动
  - WPF 模态窗口的 OnClosing 可覆写以拦截 X 按钮关闭行为，路由到 CancelCommand 统一处理
  - Velopack DownloadUpdatesAsync 原生支持 CancellationToken，可直接传递无需包装
---

# M011-ns0oo0: 更新体验修复

**Fixed URL echo in update settings via direct IConfiguration reads, and added modal download progress window with real-time progress bar, speed, ETA, and cancel support**

## What Happened

M011-ns0oo0 delivered two independent fixes for the Velopack update experience.

S01 fixed the URL echo issue in UpdateSettingsViewModel. The root cause was a fragile string-stripping approach that tried to extract the raw UpdateUrl from EffectiveUpdateUrl (which has channel suffixes like /stable/ appended). Decision D033 chose to bypass the stripping entirely — the ViewModel now injects IConfiguration directly and reads Update:UpdateUrl and Update:Channel as raw values. This eliminated ~20 lines of error-prone if/else/Substring logic and added 11 unit tests covering HTTP URLs, GitHub empty/null URLs, Channel fallback, Trim, SourceTypeDisplay, and null IConfiguration defense.

S02 built a complete modal download progress window from scratch. Three tasks were executed: T01 added CancellationToken support to IUpdateService.DownloadUpdatesAsync and created DownloadProgressViewModel with cumulative average speed calculation, ETA estimation, and cancel/complete/error state transitions. T02 created DownloadProgressWindow (450px modal XAML dialog) and wired it into MainWindowViewModel.CheckUpdateAsync via Task.Run + ShowDialog pattern. T03 wrote 38 unit tests covering all ViewModel logic and edge cases. Key pattern: caller creates ViewModel manually with runtime params (totalBytes, version), resolves Window from DI, injects via SetViewModel().

Both slices were risk-assessed as low/medium and completed with zero deviations, zero known limitations, and zero follow-ups. The full test suite (203 tests) passes cleanly.

## Success Criteria Results

- **更新设置窗口正确显示 UpdateUrl 和 Channel**: ✅ — UpdateSettingsViewModel 直接从 IConfiguration["Update:UpdateUrl"] 读取原始值，11 个单元测试验证所有场景（HTTP URL、GitHub URL、null、空值、Trim），203/203 测试通过
- **下载更新时弹出模态进度窗口，实时显示进度条/速度/ETA**: ✅ — DownloadProgressWindow 模态弹窗含 ProgressBar (0-100%)、速度 TextBlock (MB/s)、ETA TextBlock，DownloadProgressViewModel 通过累积平均速度计算驱动 UI 更新，38 个单元测试验证
- **取消按钮中断下载，应用继续正常运行**: ✅ — CancelCommand 触发 CancellationTokenSource.Cancel()，OperationCanceledException 被捕获，OnClosing 覆写防止下载期间 X 按钮关闭，单元测试验证状态转换
- **现有更新检查、状态栏提示、设置保存功能不受影响**: ✅ — 零偏差记录，IUpdateService.DownloadUpdatesAsync 的 CancellationToken 参数为默认值（向后兼容），全部 203 个测试通过

## Definition of Done Results

- **所有切片标记为 [x]**: ✅ — S01 和 S02 均在 ROADMAP.md 中标记完成
- **所有切片 SUMMARY.md 存在**: ✅ — S01-SUMMARY.md 和 S02-SUMMARY.md 均已生成
- **跨切片集成**: ✅ — S01 和 S02 无依赖关系，独立交付，无集成冲突
- **所有任务完成**: ✅ — S01 (1/1 tasks), S02 (3/3 tasks) 全部完成

## Requirement Outcomes

- R047: active → validated — UpdateSettingsViewModel 直接从 IConfiguration 读取原始 URL/Channel，11 个单元测试通过，203/203 全套测试通过，旧 EffectiveUpdateUrl 剥离逻辑完全移除
- R048: active → validated — DownloadProgressWindow 模态弹窗含进度条/速度/ETA，38 个单元测试通过，Task.Run + ShowDialog 模式集成到 MainWindowViewModel
- R049: active → validated — CancelCommand + CancellationToken 取消机制，OnClosing 防止 X 按钮关闭，OperationCanceledException 捕获后应用正常继续

## Deviations

None.

## Follow-ups

None.
