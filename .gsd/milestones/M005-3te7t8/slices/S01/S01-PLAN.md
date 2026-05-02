# S01: CLI 框架 + inspect 子命令

**Goal:** 搭建 CLI 基础框架（入口分叉、JSONL 输出、控制台 P/Invoke、--help），实现 inspect 子命令端到端可用，证明最关键的技术风险已解决
**Demo:** dotnet build 成功后，执行 DocuFiller.exe --help 输出 JSONL 格式帮助文档；执行 DocuFiller.exe inspect --template <path> 输出模板控件列表 JSONL；无参数启动 GUI 正常

## Must-Haves

- 1. DocuFiller.exe --help 输出 JSONL 格式的完整帮助，包含 inspect 子命令参数说明
- 2. DocuFiller.exe inspect --template <path> 输出控件列表 JSONL（每行一个 JSON 对象，含 tag/title/type/location）
- 3. 无参数启动时 WPF GUI 正常工作（无控制台闪屏）
- 4. 参数错误时输出错误 JSONL + exit code 1
- 5. dotnet build 0 错误，现有 71 个测试全部通过

## Proof Level

- This slice proves: contract: 通过 dotnet build 和现有测试回归 + 手动 CLI 执行验证 stdout 输出

## Integration Closure

S01 建立 CliRunner（参数解析+分发）、JsonlOutput（JSONL 格式化）、ConsoleHelper（WinExe stdout P/Invoke）三个公共组件，供 S02 复用

## Verification

- CLI 输出本身就是结构化 JSONL，天然具备 agent 可观测性。每行含 type/status 字段，错误行含 message/code 字段

## Tasks

- [x] **T01: 创建 CLI 基础设施（ConsoleHelper + JsonlOutput + CliRunner）** `est:2 hours`
  创建三个核心 CLI 基础设施类：

1. **Cli/ConsoleHelper.cs** — WinExe stdout P/Invoke 解决方案
   - `[DllImport("kernel32.dll")] static extern bool AttachConsole(int dwProcessId)` (-1 = ATTACH_PARENT_PROCESS)
   - `[DllImport("kernel32.dll")] static extern bool FreeConsole()`
   - `Initialize()` 方法：调用 AttachConsole(-1)，失败则 AllocConsole
   - `Cleanup()` 方法：调用 FreeConsole()
   - Console.Out 确保在 AttachConsole 后可用

2. **Cli/JsonlOutput.cs** — JSONL 格式化输出工具
   - `WriteHelp(object helpData)` — 输出帮助 JSONL
   - `WriteResult(string type, object data)` — 输出结果 JSONL
   - `WriteError(string message, string? code = null)` — 输出错误 JSONL
   - `WriteSummary(object summary)` — 输出汇总 JSONL
   - 统一 envelope：`{"type":"...","status":"success|error","timestamp":"...", ...}`
   - 使用 System.Text.Json 序列化，每行一个 JSON 对象，无额外换行

3. **Cli/CliRunner.cs** — 参数解析和命令分发
   - `async Task<int> RunAsync(string[] args)` — 主入口
   - 解析 args[0] 为子命令名（fill/cleanup/inspect/help）
   - 解析后续参数为 key-value 对（--key value 格式）
   - 支持 --help/-h 全局和子命令级别
   - 支持 --version
   - 通过 ServiceProvider 解析命令处理器
   - 返回 exit code（0=成功, 1=失败）
  - Files: `Cli/ConsoleHelper.cs`, `Cli/JsonlOutput.cs`, `Cli/CliRunner.cs`
  - Verify: dotnet build 编译成功，三个新文件无编译错误

- [x] **T02: 修改 App.OnStartup 实现 CLI/GUI 双模式分叉** `est:1 hour`
  在 App.xaml.cs 的 OnStartup 中添加 CLI/GUI 分叉逻辑：

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    if (e.Args.Length > 0)
    {
        // CLI 模式
        ConfigureServices();
        var consoleHelper = new ConsoleHelper();
        consoleHelper.Initialize();
        try
        {
            var cliRunner = new CliRunner(_serviceProvider, new JsonlOutput());
            var exitCode = cliRunner.RunAsync(e.Args).GetAwaiter().GetResult();
            Shutdown(exitCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{{\"type\":\"error\",\"status\":\"error\",\"message\":\"{ex.Message}\"}}");
            Shutdown(1);
        }
        finally
        {
            consoleHelper.Cleanup();
        }
        return;
    }
    
    // 原有 GUI 启动逻辑（不变）
    try { ... }
}
```

关键要点：
- CLI 模式下不创建 MainWindow
- CLI 模式下跳过全局异常处理中的 MessageBox 调用（CLI 路径不应弹窗）
- 使用 GetAwaiter().GetResult() 同步等待异步 CLI 执行（OnStartup 不是 async）
- 确保 finally 块中 FreeConsole 被调用
  - Files: `App.xaml.cs`
  - Verify: dotnet build 编译成功；无参数启动 GUI 正常；有参数时不弹窗

- [x] **T03: 实现 inspect 子命令** `est:1.5 hours`
  实现 inspect 子命令处理器：

**Cli/Commands/InspectCommand.cs**

```
async Task<int> ExecuteAsync(string[] args)
```

参数：
- `--template <path>` (必需) — 模板文件路径
- `--help` — 显示 inspect 子命令帮助

执行流程：
1. 验证 --template 参数存在且文件存在
2. 通过 DI 解析 IDocumentProcessor
3. 调用 GetContentControlsAsync(templatePath)
4. 对每个 ContentControlData 输出一行 JSONL：
   ```jsonl
   {"type":"control","tag":"...","title":"...","contentType":"Text","location":"Body"}
   ```
5. 最后输出汇总行：
   ```jsonl
   {"type":"summary","status":"success","totalControls":5}
   ```
6. 文件不存在或参数缺失时输出错误 JSONL 并返回 exit code 1

注册为命令名 `inspect`，在 CliRunner 中通过 ServiceProvider 解析。

ICommand 接口：`interface ICommand { string Name { get; } Task<int> ExecuteAsync(string[] args); }`
  - Files: `Cli/Commands/ICommand.cs`, `Cli/Commands/InspectCommand.cs`
  - Verify: 使用测试模板执行 DocuFiller.exe inspect --template <path> 输出正确 JSONL 控件列表

- [x] **T04: 实现 --help JSONL 输出（全局 + 子命令级别）** `est:1 hour`
  在 CliRunner 中实现 --help 输出，格式为 JSONL：

```jsonl
{"type":"help","name":"DocuFiller","version":"1.0.0","description":"Word文档批量填充工具"}
{"type":"command","name":"fill","description":"使用Excel数据批量填充Word模板","usage":"DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> [options]","options":[{"name":"--template","required":true,"description":"模板文件路径"},{"name":"--data","required":true,"description":"Excel数据文件路径"},{"name":"--output","required":true,"description":"输出目录"},{"name":"--folder","required":false,"description":"文件夹批量模式"},{"name":"--overwrite","required":false,"description":"覆盖已存在文件"}]}
{"type":"command","name":"cleanup","description":"清理Word文档中的批注和内容控件","usage":"DocuFiller.exe cleanup --input <path> [options]","options":[{"name":"--input","required":true,"description":"文件或文件夹路径"},{"name":"--output","required":false,"description":"输出目录"},{"name":"--folder","required":false,"description":"文件夹批量模式"}]}
{"type":"command","name":"inspect","description":"查询模板中的内容控件列表","usage":"DocuFiller.exe inspect --template <path>","options":[{"name":"--template","required":true,"description":"模板文件路径"}]}
{"type":"examples","items":["DocuFiller.exe inspect --template report.docx","DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output","DocuFiller.exe cleanup --input ./docs"]}
```

全局 --help/-h 或无子命令时输出完整帮助。
子命令 --help 输出该子命令的帮助行。
--version 输出版本行。
  - Files: `Cli/CliRunner.cs`
  - Verify: 执行 DocuFiller.exe --help 输出完整 JSONL 帮助文档，包含 fill/cleanup/inspect 三个子命令的参数和使用示例

- [x] **T05: 端到端验证：CLI --help + inspect + GUI 回归** `est:1 hour`
  执行完整的端到端验证：

1. `dotnet build` — 编译成功（0 错误）
2. `dotnet test` — 所有 71 个现有测试通过
3. 准备一个含内容控件的测试 .docx 文件（在测试中已有模板可复用）
4. 执行 `DocuFiller.exe --help` — 验证输出为 JSONL 格式，每行可被 System.Text.Json 解析，包含三个子命令
5. 执行 `DocuFiller.exe inspect --template <测试模板>` — 验证输出 JSONL 包含正确的控件 tag/title/type/location
6. 执行 `DocuFiller.exe inspect` — 验证输出错误 JSONL（缺少 --template），exit code 1
7. 执行 `DocuFiller.exe --unknown-cmd` — 验证输出错误 JSONL（未知命令），exit code 1
8. 无参数启动 — 验证 GUI 正常弹窗（无控制台闪屏）
9. 验证所有 JSONL 输出行的 timestamp 字段为有效 ISO 8601 格式
  - Verify: 上述 9 项验证全部通过

## Files Likely Touched

- Cli/ConsoleHelper.cs
- Cli/JsonlOutput.cs
- Cli/CliRunner.cs
- App.xaml.cs
- Cli/Commands/ICommand.cs
- Cli/Commands/InspectCommand.cs
