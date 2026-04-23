---
estimated_steps: 37
estimated_files: 1
skills_used: []
---

# T02: 修改 App.OnStartup 实现 CLI/GUI 双模式分叉

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

## Inputs

- `Cli/ConsoleHelper.cs`
- `Cli/CliRunner.cs`
- `Cli/JsonlOutput.cs`

## Expected Output

- `修改后的 App.xaml.cs`

## Verification

dotnet build 编译成功；无参数启动 GUI 正常；有参数时不弹窗

## Observability Impact

CLI 入口分叉点是最关键的可观测性节点 — 错误路径直接输出 JSONL error 行
