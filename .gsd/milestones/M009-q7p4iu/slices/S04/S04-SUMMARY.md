---
id: S04
parent: M009-q7p4iu
milestone: M009-q7p4iu
provides:
  - ["UpdateCommand 类（ICliCommand 实现）— CLI update 子命令完整生命周期", "JsonlOutput.WriteUpdate() 方法 — type=update envelope 输出", "CliRunner update 路由 + 帮助文本", "CliRunner post-command update reminder 钩子", "UpdateCommand + post-command 更新提醒单元测试（9 个）"]
requires:
  - slice: S02
    provides: IUpdateService.CheckForUpdatesAsync(), DownloadUpdatesAsync(), ApplyUpdatesAndRestart(), IsUpdateUrlConfigured, IsInstalled
affects:
  - []
key_files:
  - ["Cli/Commands/UpdateCommand.cs", "Cli/JsonlOutput.cs", "Cli/CliRunner.cs", "App.xaml.cs", "Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs", "Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs"]
key_decisions:
  - ["Error code split: UPDATE_CHECK_ERROR for check failures, UPDATE_DOWNLOAD_ERROR for download failures", "Post-command reminder guard: exitCode==0 AND subcommand != update AND updateInfo != null", "IUpdateService lazy resolution via IServiceProvider.GetService() in CliRunner", "Velopack types use NuGet.Versioning.SemanticVersion, not System.Version"]
patterns_established:
  - ["CLI post-command hook pattern — conditionally append JSONL after successful commands via IServiceProvider lazy resolution", "Error code split pattern — UPDATE_CHECK_ERROR vs UPDATE_DOWNLOAD_ERROR for different failure modes"]
observability_surfaces:
  - ["UpdateCommand 使用 ILogger 记录检查/下载/应用更新各阶段", "Update reminder 使用 LogDebug 记录是否追加提醒", "CLI JSONL 输出本身就是可观测信号"]
drill_down_paths:
  - [".gsd/milestones/M009-q7p4iu/slices/S04/tasks/T01-SUMMARY.md", ".gsd/milestones/M009-q7p4iu/slices/S04/tasks/T02-SUMMARY.md", ".gsd/milestones/M009-q7p4iu/slices/S04/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-26T11:37:32.832Z
blocker_discovered: false
---

# S04: CLI update 命令 + JSONL 更新提醒

**CLI update 子命令输出版本检查 JSONL，--yes 执行下载重启，post-command hook 条件性追加更新提醒行**

## What Happened

S04 实现了 CLI 更新命令和命令后更新提醒两大功能。

**UpdateCommand（T01）**：创建了 `UpdateCommand` 类实现 `ICliCommand`，提供完整的更新生命周期——无 `--yes` 时调用 `IUpdateService.CheckForUpdatesAsync()` 并输出 type=update JSONL（含 currentVersion/latestVersion/hasUpdate/isInstalled/updateSourceType）；有 `--yes` 时先检查 IsInstalled（便携版输出 PORTABLE_NOT_SUPPORTED 错误），再检查是否有新版本（无新版本输出 ALREADY_UP_TO_DATE），有新版本时执行下载+重启。下载过程中通过进度回调输出 progress JSONL 行。新增 `JsonlOutput.WriteUpdate()` 方法，在 CliRunner 注册 update 路由和帮助文本，在 App.xaml.cs 注册 DI。

**Post-command 更新提醒（T02）**：在 CliRunner.RunAsync 中添加 `TryAppendUpdateReminderAsync` 钩子，成功执行的非 update 子命令（inspect/fill/cleanup）后，通过 IServiceProvider 延迟获取 IUpdateService 检查更新，仅当有新版本可用时追加 type=update JSONL 行（reminder=true）。检查失败静默跳过，不影响原命令 exitCode。

**单元测试（T03）**：编写 9 个新测试（6 个 UpdateCommand 测试 + 3 个 post-command 提醒测试），全部通过。全量测试 154/154 通过，零回归。

关键技术决策：(1) 错误码拆分为 UPDATE_CHECK_ERROR 和 UPDATE_DOWNLOAD_ERROR；(2) post-command 提醒仅对 exitCode==0 且非 update 子命令生效；(3) IUpdateService 延迟解析避免构造函数签名变更。

## Verification

dotnet build -c Release 通过（0 错误 0 警告）。dotnet test 全量 154 测试通过（9 新增 + 145 已有，零回归）。新增测试覆盖：update 帮助输出、路由分发、无 --yes 版本信息 JSONL 格式、无更新场景、--yes 便携版错误、--yes 已最新、post-command 有更新追加、post-command 无更新不追加、post-command 失败不追加。

## Requirements Advanced

- R041 — 创建了 UpdateCommand 类、JsonlOutput.WriteUpdate 方法，注册到 CliRunner 和 DI
- R042 — 在 CliRunner 添加 TryAppendUpdateReminderAsync 钩子，条件性追加 update JSONL

## Requirements Validated

- R041 — UpdateCommand 实现 ICliCommand，无 --yes 输出完整版本 JSONL，--yes 执行下载重启，便携版输出 PORTABLE_NOT_SUPPORTED 错误。单元测试覆盖所有路径，154/154 测试通过
- R042 — CliRunner post-command hook 仅在 exitCode==0、非 update 子命令、updateInfo != null 时追加 type=update 行。单元测试验证三种场景（有更新追加/无更新不追加/失败不追加），全量测试通过

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

- `Cli/Commands/UpdateCommand.cs` — 新建 — CLI update 子命令实现，含版本检查/下载/重启全生命周期
- `Cli/JsonlOutput.cs` — 新增 WriteUpdate 方法 — type=update envelope 输出
- `Cli/CliRunner.cs` — 新增 update 路由+帮助+post-command TryAppendUpdateReminderAsync 钩子
- `App.xaml.cs` — DI 注册 UpdateCommand 为 ICliCommand singleton
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — 新建 — 6 个 UpdateCommand 单元测试
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs` — 新增 3 个 post-command 更新提醒测试
