---
phase: execute
phase_name: "M005-3te7t8: CLI 接口 — LLM Agent 集成"
project: DocuFiller
generated: "2026-04-23T16:45:00Z"
counts:
  decisions: 3
  lessons: 3
  patterns: 3
  surprises: 2
missing_artifacts: []
---

### Decisions

- **Program.cs 自定义入口点替代 WPF 自动生成 Main**: 创建 Program.cs 并设置 `<StartupObject>DocuFiller.Program</StartupObject>`，在 Main 中检查 args.Length 分叉 CLI/GUI 路径。CLI 路径完全绕过 WPF Application 初始化。Alternatives: (a) 在 App.OnStartup 中分叉 — 失败因为 InitializeComponent() 先执行； (b) 修改 OutputType=Exe — 失败因为 GUI 模式会闪控制台窗口。
  Source: S01-SUMMARY.md/关键修复

- **手写参数解析器代替 System.CommandLine**: 3 个子命令各 2-5 个参数，手写 args[] 切片解析约 100 行代码，不值得引入外部依赖。CLI 面向机器消费（JSONL 输出），不需要人性化的参数解析功能（如缩写、互斥组等）。
  Source: .gsd/DECISIONS.md/D011

- **JSONL 统一 envelope schema**: 所有 CLI 输出使用 `{type, status, timestamp, data}` envelope，让 agent 用统一逻辑解析所有行。System.Text.Json 内置，无额外依赖。
  Source: .gsd/DECISIONS.md/D012

### Lessons

- **WPF InitializeComponent() 在 OnStartup 之前执行**: WPF 自动生成的 Main 调用 `new App().InitializeComponent()`，这发生在 `app.Run()` 和 `OnStartup` 事件之前。在非交互终端中 BAML 资源加载会抛 FileNotFoundException。解决方案是用自定义 Main + StartupObject 完全绕过 WPF Application 生命周期。
  Source: S01-SUMMARY.md/关键修复

- **CLI 模式需要禁用 console logger**: Serilog console sink 会直接写入 stdout，污染 JSONL 输出。在 LoggerConfiguration.CreateLoggerFactory 中添加 `enableConsole` 参数，CLI 模式传 false。
  Source: S01-SUMMARY.md/验证结果

- **xUnit Console.SetOut 需要 DisableTestParallelization**: 多个测试并行调用 Console.SetOut 会互相覆盖输出流，导致测试时序不稳定。在 AssemblyInfo 中设置 `[CollectionBehavior(DisableTestParallelization = true)]`。
  Source: S03-SUMMARY.md/关键技术决策

### Patterns

- **ICliCommand 命令模式**: 每个子命令是独立类实现 ICliCommand 接口（CommandName + ExecuteAsync），通过 DI 注册为 singleton。CliRunner 通过 ServiceProvider.GetServices<ICliCommand>() 查找处理器并按名称匹配。新子命令只需实现接口 + 注册 DI。
  Source: S01-SUMMARY.md/公共组件

- **JSONL 输出三类型约定**: result（单条结果）、progress（进度事件）、summary（汇总统计）、error（错误信息）。每种类型有固定的 data schema。子命令处理器负责构造 data 对象，JsonlOutput 负责封装 envelope + 序列化。
  Source: S02-SUMMARY.md/JSONL 输出格式

- **WinExe CLI 支持 P/Invoke 模式**: 保持 OutputType=WinExe 避免 GUI 闪屏，CLI 模式通过 ConsoleHelper.AttachConsole(-1) 将 stdout 附加到父控制台。双击启动时无父控制台所以不会弹窗。这是 WPF 应用添加 CLI 支持的标准方案。
  Source: S01-SUMMARY.md/公共组件

### Surprises

- **App.OnStartup 方案完全不可行**: 原计划在 App.OnStartup 中实现 CLI/GUI 分叉是最直觉的方案，但实际验证发现 WPF 框架在调用 OnStartup 之前就已经加载了 BAML 资源，在无显示器的 CI/Agent 环境中直接崩溃。最终需要在 App.xaml.cs 之外创建完全独立的入口点。
  Source: S01-SUMMARY.md/偏差说明

- **从 cmd 调用 WinExe 程序时 stdout 默认被丢弃**: Windows 子进程的 stdout 不会自动附加到父控制台。AttachConsole(-1) P/Invoke 是唯一可靠的解决方案，且必须在实际写入 stdout 之前调用。ConsoleHelper 封装了这个时序依赖。
  Source: S01-SUMMARY.md/公共组件
