---
estimated_steps: 22
estimated_files: 3
skills_used: []
---

# T01: 创建 CLI 基础设施（ConsoleHelper + JsonlOutput + CliRunner）

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

## Inputs

- `DocuFiller.csproj (OutputType=WinExe 确认)`
- `Services/Interfaces/IDocumentProcessor.cs`

## Expected Output

- `Cli/ConsoleHelper.cs`
- `Cli/JsonlOutput.cs`
- `Cli/CliRunner.cs`

## Verification

dotnet build 编译成功，三个新文件无编译错误

## Observability Impact

JsonlOutput 的统一 envelope 格式让每条输出都有 type/status/timestamp，agent 可逐行解析跟踪执行进度
