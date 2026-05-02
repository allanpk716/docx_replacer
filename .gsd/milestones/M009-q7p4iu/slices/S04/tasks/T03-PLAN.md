---
estimated_steps: 26
estimated_files: 2
skills_used: []
---

# T03: 单元测试：UpdateCommand 路由、JSONL 格式、更新提醒逻辑

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

## Inputs

- `Cli/Commands/UpdateCommand.cs — 被测对象（T01 创建）`
- `Cli/CliRunner.cs — 被测对象（T02 修改）`
- `Cli/JsonlOutput.cs — WriteUpdate 方法（T01 创建）`
- `Services/Interfaces/IUpdateService.cs — Mock 目标接口`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs — 已有测试模式参考`

## Expected Output

- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs — UpdateCommand 单元测试`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs — 添加 post-command 更新提醒测试`

## Verification

dotnet test --filter "UpdateCommand|PostCommand" 2>&1
