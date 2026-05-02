---
estimated_steps: 17
estimated_files: 1
skills_used: []
---

# T02: 实现 CLI 命令后更新提醒（CliRunner post-command hook）

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

## Inputs

- `Cli/CliRunner.cs — 需要在 RunAsync 中添加 post-command hook`
- `Cli/JsonlOutput.cs — WriteUpdate 方法（T01 创建）`
- `Services/Interfaces/IUpdateService.cs — CheckForUpdatesAsync 接口`

## Expected Output

- `Cli/CliRunner.cs — 包含 post-command update reminder 逻辑`

## Verification

dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"error MC" /C:"Build succeeded"
