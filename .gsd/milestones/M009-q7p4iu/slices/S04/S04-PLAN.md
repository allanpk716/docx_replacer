# S04: CLI update 命令 + JSONL 更新提醒

**Goal:** CLI update 命令输出当前版本和最新版本的 JSONL 信息；--yes 确认后下载应用重启；其他 CLI 命令在 actionable 时（未配置源、有新版本可用）追加 update 类型 JSONL 行
**Demo:** After this: DocuFiller.exe update 输出当前版本和最新版本的 JSONL 信息；--yes 确认后下载应用重启；其他命令在 actionable 时追加 update 行

## Must-Haves

- `DocuFiller.exe update` 无 --yes 时输出 type=update JSONL 包含 currentVersion/latestVersion/hasUpdate/isInstalled
- `DocuFiller.exe update --yes` 时在安装版环境下执行下载和应用更新重启
- 便携版执行 `update --yes` 时输出明确错误提示
- fill/cleanup/inspect 命令执行后，仅有新版本可用时追加 type=update JSONL 行
- 已是最新版本时不追加 update 行
- dotnet build 零错误，现有测试全部通过
- 新增 UpdateCommand 单元测试验证路由和输出格式

## Proof Level

- This slice proves: contract — CLI update 命令 JSONL 输出格式和路由正确性可通过单元测试证明；下载/重启依赖 Velopack 运行时，在测试环境中验证错误处理路径

## Integration Closure

- Upstream surfaces consumed: IUpdateService.CheckForUpdatesAsync(), DownloadUpdatesAsync(), ApplyUpdatesAndRestart(), IsUpdateUrlConfigured, IsInstalled, UpdateSourceType
- New wiring: UpdateCommand 注册为 ICliCommand singleton；CliRunner.RunAsync 子命令分发增加 "update" 路由；CliRunner.RunAsync 添加 post-command update reminder 钩子
- What remains: 端到端验证需要真实安装版环境（milestone acceptance）

## Verification

- Signals added: UpdateCommand 使用 ILogger 记录检查/下载/应用更新各阶段；update reminder 使用 LogDebug 记录是否追加提醒
- Inspection: CLI JSONL 输出本身就是可观测信号；logger 输出到 Logs/ 目录
- Failure state: 检查失败输出 error JSONL（type=update, status=error）；便携版更新提示明确告知不支持

## Tasks

- [x] **T01: 实现 UpdateCommand + JsonlOutput.WriteUpdate + DI 注册** `est:45m`
  创建 UpdateCommand 类（ICliCommand 实现），实现无 --yes 时输出版本信息 JSONL，--yes 时执行下载和重启。在 JsonlOutput 中新增 WriteUpdate 方法。在 CliRunner 中注册 update 子命令路由和帮助文本。在 App.xaml.cs 中注册 DI。

## Steps

1. 在 `Cli/JsonlOutput.cs` 中新增 `WriteUpdate(object data)` 方法，使用 envelope 格式输出 type=update JSONL 行
2. 创建 `Cli/Commands/UpdateCommand.cs`，实现 ICliCommand 接口，CommandName="update"，构造函数注入 IUpdateService 和 ILogger
3. UpdateCommand.ExecuteAsync 逻辑：
   - 调用 IUpdateService.CheckForUpdatesAsync()
   - 无 --yes 时：输出 update JSONL 包含 currentVersion（Assembly 版本）、latestVersion、hasUpdate、isInstalled、updateSourceType
   - 有 --yes 时：
     - 先检查 IsInstalled，便携版输出错误 "便携版不支持自动更新，请使用安装版" (PORTABLE_NOT_SUPPORTED)
     - 检查是否有新版本 (updateInfo != null)，无新版本输出信息 "当前已是最新版本" (ALREADY_UP_TO_DATE) 返回 0
     - 有新版本时调用 DownloadUpdatesAsync 并输出 progress JSONL
     - 下载完成后调用 ApplyUpdatesAndRestart（应用重启，不会返回）
   - CheckForUpdatesAsync 异常时输出 error JSONL (UPDATE_CHECK_ERROR)
   - 下载异常时输出 error JSONL (UPDATE_DOWNLOAD_ERROR)
4. 在 `Cli/CliRunner.cs` 中：
   - RunAsync 的 subCommand switch 添加 "update" case
   - HandleUnknownCommand 的支持子命令列表添加 "update"
   - WriteGlobalHelp 添加 update 命令的帮助信息行
   - WriteSubCommandHelp 添加 "update" case 的帮助文本
5. 在 `App.xaml.cs` 的 BuildServiceProvider 中注册 `services.AddSingleton<ICliCommand, UpdateCommand>()`

## Must-Haves

- [ ] UpdateCommand 类实现 ICliCommand，CommandName="update"
- [ ] 无 --yes 时输出完整的版本信息 JSONL（currentVersion, latestVersion, hasUpdate, isInstalled, updateSourceType）
- [ ] --yes 时便携版输出 PORTABLE_NOT_SUPPORTED 错误
- [ ] --yes 时安装版执行下载+重启流程
- [ ] CliRunner 注册 update 路由和帮助文本
- [ ] App.xaml.cs 注册 UpdateCommand 为 ICliCommand singleton
- [ ] dotnet build 零错误
  - Files: `Cli/Commands/UpdateCommand.cs`, `Cli/JsonlOutput.cs`, `Cli/CliRunner.cs`, `App.xaml.cs`
  - Verify: dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded"

- [x] **T02: 实现 CLI 命令后更新提醒（CliRunner post-command hook）** `est:30m`
  在 CliRunner.RunAsync 中，所有子命令执行完成后，条件性地追加 type=update JSONL 行。仅在 actionable 时输出（有新版本可用时），已最新版本时不输出，避免干扰 JSONL 解析器。

## Steps

1. 在 `Cli/CliRunner.cs` 的 RunAsync 方法中，`RunSubCommandAsync` 返回后、return exitCode 之前，添加更新检查逻辑：
   - 只在 exitCode == 0 时检查更新（命令成功才提示，避免错误场景干扰）
   - 从 _serviceProvider 获取 IUpdateService（可能为 null，安全处理）
   - 调用 CheckForUpdatesAsync()（try-catch 包裹，失败静默跳过）
   - 仅当 updateInfo != null（有新版本）时调用 JsonlOutput.WriteUpdate 输出提醒
   - 已是最新版本时不输出任何内容
   - 检查异常不影响原命令的 exitCode
2. 确保更新检查是异步的（await），不影响原有命令执行流程
3. 注意：CliRunner 构造函数不需要注入 IUpdateService（已通过 IServiceProvider 延迟获取）

## Must-Haves

- [ ] 子命令成功执行后（exitCode==0），条件性追加 update JSONL 行
- [ ] 仅在有新版本时输出，已是最新不输出
- [ ] 更新检查失败不影响原命令 exitCode
- [ ] IUpdateService 未注册时不报错（安全处理 null）
- [ ] dotnet build 零错误
  - Files: `Cli/CliRunner.cs`
  - Verify: dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded"

- [x] **T03: 单元测试：UpdateCommand 路由、JSONL 格式、更新提醒逻辑** `est:45m`
  为 UpdateCommand 路由、update JSONL 输出格式、post-command 更新提醒逻辑编写单元测试。使用 CliRunnerTests 中已有的 StubCommand + StringWriter 捕获模式。

## Steps

1. 在 `Tests/DocuFiller.Tests/Cli/` 目录下创建 `UpdateCommandTests.cs`
2. 创建 Mock/Stub IUpdateService（或使用现有模式），实现：
   - CheckForUpdatesAsync 返回 UpdateInfo（有更新）/ null（无更新）
   - IsInstalled 返回 true/false
   - 其他属性返回测试值
3. 编写测试用例：
   - `Update_Help_OutputsUpdateCommandHelp`: update --help 输出正确的帮助 JSONL
   - `Update_DispatchesToCorrectHandler`: update 子命令路由到 UpdateCommand
   - `Update_NoYes_OutputsVersionInfo`: 无 --yes 时输出包含 currentVersion/latestVersion/hasUpdate 的 JSONL
   - `Update_WithYes_Portable_OutputsError`: IsInstalled=false + --yes 输出 PORTABLE_NOT_SUPPORTED 错误
   - `Update_WithYes_NoUpdate_ReturnsSuccess`: --yes 但无新版本时输出 ALREADY_UP_TO_DATE
4. 在 `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs` 中添加更新提醒测试：
   - `PostCommand_UpdateAvailable_AppendsUpdateLine`: 子命令成功 + 有新版本 → 追加 update JSONL
   - `PostCommand_NoUpdate_NoExtraLine`: 子命令成功 + 无新版本 → 不追加
   - `PostCommand_FailedCommand_NoUpdateLine`: 子命令失败 → 不追加 update 行
5. 确保 `dotnet test --filter "UpdateCommand" ` 和 `dotnet test --filter "PostCommand"` 全部通过
6. 确保现有测试不被破坏：`dotnet test`

## Must-Haves

- [ ] UpdateCommand 路由测试（help + dispatch）
- [ ] UpdateCommand 无 --yes 时 JSONL 输出格式测试
- [ ] UpdateCommand --yes 便携版错误测试
- [ ] Post-command update reminder 条件性输出测试
- [ ] 所有新增测试通过
- [ ] 现有测试不被破坏
  - Files: `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`, `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
  - Verify: dotnet test --filter "UpdateCommand|PostCommand" 2>&1

## Files Likely Touched

- Cli/Commands/UpdateCommand.cs
- Cli/JsonlOutput.cs
- Cli/CliRunner.cs
- App.xaml.cs
- Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
- Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
