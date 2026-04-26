---
estimated_steps: 28
estimated_files: 4
skills_used: []
---

# T01: 实现 UpdateCommand + JsonlOutput.WriteUpdate + DI 注册

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

## Inputs

- `Cli/CliRunner.cs — 子命令分发和帮助文本注册`
- `Cli/JsonlOutput.cs — JSONL 输出工具类`
- `Cli/Commands/InspectCommand.cs — ICliCommand 实现模式参考`
- `Cli/Commands/FillCommand.cs — ICliCommand 实现模式参考`
- `Services/Interfaces/IUpdateService.cs — 更新服务接口`
- `Services/UpdateService.cs — 更新服务实现参考`
- `App.xaml.cs — DI 注册位置`

## Expected Output

- `Cli/Commands/UpdateCommand.cs — update 子命令处理器`
- `Cli/JsonlOutput.cs — 新增 WriteUpdate 方法`
- `Cli/CliRunner.cs — update 路由和帮助文本`
- `App.xaml.cs — DI 注册 UpdateCommand`

## Verification

dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded"
