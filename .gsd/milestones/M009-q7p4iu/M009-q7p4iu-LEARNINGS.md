---
phase: completion
phase_name: Milestone Completion
project: DocuFiller
generated: "2026-04-26T11:45:00.000Z"
counts:
  decisions: 5
  lessons: 4
  patterns: 5
  surprises: 2
missing_artifacts: []
---

# M009-q7p4iu LEARNINGS

### Decisions

- **D025: UpdateService 多源切换策略** — UpdateUrl 非空走内网 Go 服务器（SimpleWebSource），为空走 GitHub Releases（GithubSource）。不需要同时检查两个源，按配置选一个。Source: M009-q7p4iu-CONTEXT.md/Architectural Decisions

- **D026: CLI update 命令纯 JSONL 交互** — 不使用交互式 Y/N 确认，用 --yes 参数确认执行。适合批处理脚本场景，保持输出格式一致性。Source: M009-q7p4iu-CONTEXT.md/Architectural Decisions

- **D027: IUpdateService 接口不改签名** — IsUpdateUrlConfigured 语义扩展为"有任一更新源可用"（GitHub Releases 永远可用）。最小化改动范围，避免连锁修改。Source: M009-q7p4iu-CONTEXT.md/Architectural Decisions

- **D028: GitHub Release 只走 stable 通道** — beta 继续走内网 Go 服务器。公司大部分用户访问 GitHub 不顺畅，GitHub 对外只发稳定版。Source: M009-q7p4iu-CONTEXT.md/Architectural Decisions

- **D029: 只支持安装版自动更新** — 便携版有明确提示引导用户使用安装版。Velopack 自更新机制依赖安装版，统一更新体验。Source: M009-q7p4iu-CONTEXT.md/Architectural Decisions

### Lessons

- **Velopack vpk pack 默认生成 Portable.zip** — 计划中提到使用 --packPortable 标志，实际该标志不存在（只有 --noPortable 跳过）。vpk pack 在 Windows 上默认产生 Portable.zip。Source: S01-SUMMARY.md/What Happened

- **Velopack UpdateManager 未实现 IDisposable** — 计划使用 `using var tempManager` 语法，实际运行时 UpdateManager 不支持 dispose，改为普通变量赋值。Source: S02-SUMMARY.md/Deviations

- **Velopack 类型使用 NuGet.Versioning.SemanticVersion** — 不是 System.Version。CLI 输出版本信息时需注意类型转换，不能直接用 ToString() 格式化。Source: S04-SUMMARY.md/Key Decisions

- **GitHub Actions workflow 中 grep 通配符处理** — Windows CMD 和 Git Bash 对 * 的 escape 行为不同。workflow 结构验证在 CMD 环境下失败，在 Git Bash 下通过。CI 环境默认是 Linux，不受影响。Source: S01-SUMMARY.md/What Happened

### Patterns

- **GitHub Actions workflow 观测性 echo tags** — 用 [PHASE_NAME] 格式的 echo 标记日志阶段（[GET_VERSION]、[CLEAN_BUILD]、[VPK_PACK]），便于 grep 快速定位，与 build-internal.bat 保持一致。Source: S01-SUMMARY.md/Key Decisions

- **UpdateService 构造时多源选择** — 构造函数根据配置选择 IUpdateSource 实例（SimpleWebSource 或 GithubSource），通过 UpdateSourceType 属性暴露给下游消费方。Source: S02-SUMMARY.md/Patterns Established

- **ViewModel 枚举驱动属性联动** — 单一枚举 CurrentUpdateStatus 的 setter 统一触发所有派生属性（Message/Brush/Visibility）的 PropertyChanged，避免多个 backing field 状态不一致。Source: S03-SUMMARY.md/Key Decisions

- **WPF TextBlock InputBindings 声明式点击** — 使用 InputBindings+MouseBinding 替代 code-behind 事件处理，保持 XAML 纯声明式绑定模式，配合 TextDecoration 暗示可点击。Source: S03-SUMMARY.md/Key Decisions

- **CLI post-command hook 模式** — 在 CliRunner.RunAsync 成功执行后，通过 IServiceProvider 延迟获取服务检查更新，条件性追加 JSONL 行。guard 条件：exitCode==0 AND subcommand != update AND updateInfo != null。Source: S04-SUMMARY.md/Patterns Established

### Surprises

- **IsUpdateUrlConfigured 语义从"有 HTTP 源"变为"有任一源"** — 原来 UpdateUrl 为空时返回 false 表示没有配置更新源，改为始终返回 true（因为 GitHub Releases 永远可用）。这个语义变化是合理的但可能让已习惯旧行为的用户困惑。Source: S02-SUMMARY.md/What Happened

- **IUpdateService 可选注入（null）模式** — S03 的 MainWindowViewModel 使用 IUpdateService? 可选注入，null 时安全跳过整个更新检查流程。这是处理服务不可用场景的简洁方案，但打破了 DI 容器"所有依赖必须注册"的常规模式。Source: S03-SUMMARY.md/Key Decisions
