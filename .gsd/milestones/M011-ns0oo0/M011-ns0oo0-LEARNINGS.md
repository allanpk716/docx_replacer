---
phase: execution
phase_name: milestone-completion
project: DocuFiller
generated: 2026-04-30T06:40:00Z
counts:
  decisions: 2
  lessons: 3
  patterns: 3
  surprises: 0
missing_artifacts: []
---

# M011-ns0oo0 Learnings

## Decisions

- **D033: URL 回显数据源选择** — 选择直接从 IConfiguration 读取 Update:UpdateUrl 原始值，而非从 EffectiveUpdateUrl（拼接了通道路径后缀如 /stable/）剥离。EffectiveUpdateUrl 是运行时拼接的完整 URL，剥离逻辑容易出边界问题（尾部斜杠、大小写）。IConfiguration 中的值就是用户输入的原始值，更可靠更简单。
  Source: S01-SUMMARY.md/What Happened

- **D034: 下载进度弹窗形态** — 选择独立模态 WPF Window（DownloadProgressWindow），阻塞主窗口。非模态窗口可能让用户误操作主窗口功能；状态栏嵌入式进度条不够醒目。速度和 ETA 基于 Velopack Action<int> 回调 + VelopackAsset.Size 计算。
  Source: S02-SUMMARY.md/What Happened

## Lessons

- **IConfiguration 可直接注入 ViewModel** — 当需要 appsettings.json 原始配置值时，注入 IConfiguration 比从运行时服务属性反推更可靠。UpdateSettingsViewModel 原本从 EffectiveUpdateUrl 剥离通道路径，改为直接读取后消除了 ~20 行脆弱的字符串处理代码。
  Source: S01-SUMMARY.md/What Happened

- **累积平均速度比瞬时速度更适合进度 UI** — 瞬时速度会导致 UI 抖动，累积平均（从第一个数据点开始累积）提供稳定的用户感知。DownloadProgressViewModel 使用此策略计算下载速度和 ETA。
  Source: S02-SUMMARY.md/What Happened

- **WPF 模态窗口的 OnClosing 可覆写以拦截 X 按钮关闭** — DownloadProgressWindow 通过 OnClosing 覆写将 X 按钮关闭路由到 CancelCommand，确保取消逻辑统一处理，防止用户绕过取消流程直接关闭窗口。
  Source: S02-SUMMARY.md/What Happened

## Patterns

- **IConfiguration 注入到 ViewModel 读取原始配置值** — 适用于需要显示 appsettings.json 原始值（非运行时处理后值）的场景。DI 自动解析 IConfiguration（已注册为 Singleton），无需额外配置。
  Source: S01-SUMMARY.md/Patterns Established

- **模态窗口运行时参数传递：调用方手动创建 ViewModel + SetViewModel()** — 当模态窗口需要运行时参数（如 totalBytes、version）时，调用方手动 new ViewModel 传入参数，从 DI 解析 Window，通过 SetViewModel() 注入。比 DI 工厂更灵活。
  Source: S02-SUMMARY.md/Patterns Established

- **后台工作 + 模态 UI：Task.Run + ShowDialog 模式** — 下载等长时间操作在 Task.Run 中后台执行，ShowDialog 阻塞主窗口，通过 Dispatcher.InvokeAsync 线程安全更新 UI。适用于 WPF 中的等待型操作。
  Source: S02-SUMMARY.md/Patterns Established

## Surprises

None.
