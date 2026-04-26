using System.Reflection;
using System.Text.Json;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Cli;

/// <summary>
/// 参数解析和命令分发。手写简单解析器，不引入第三方命令行库。
/// 支持子命令：inspect、fill、cleanup、help。
/// 参数格式：--key value
/// </summary>
internal class CliRunner
{
    private readonly IServiceProvider _serviceProvider;

    public CliRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// CLI 主入口。解析参数并分发到对应命令处理器。
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>Exit code: 0=成功, 1=失败</returns>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            // 附加到父控制台
            ConsoleHelper.Initialize();

            // 无参数 → 不是 CLI 模式，返回 -1 让调用方知道应启动 GUI
            if (args.Length == 0)
            {
                ConsoleHelper.Cleanup();
                return -1;
            }

            // 检测子命令（第一个参数不以 - 开头时视为子命令）
            if (!args[0].StartsWith("-"))
            {
                string subCommand = args[0].ToLowerInvariant();

                // 子命令级 --help
                if (args.Length > 1 && IsHelp(args[1..]))
                {
                    ConsoleHelper.Initialize();
                    WriteSubCommandHelp(subCommand);
                    return 0;
                }

                // 解析 key-value 参数
                var options = ParseOptions(args[1..]);

                // 分发到子命令处理器
                int exitCode = subCommand switch
                {
                    "inspect" => await RunSubCommandAsync("inspect", options),
                    "fill" => await RunSubCommandAsync("fill", options),
                    "cleanup" => await RunSubCommandAsync("cleanup", options),
                    "update" => await RunSubCommandAsync("update", options),
                    _ => HandleUnknownCommand(subCommand),
                };

                // Post-command update reminder: only for successful non-update commands
                if (exitCode == 0 && !subCommand.Equals("update", StringComparison.OrdinalIgnoreCase))
                {
                    await TryAppendUpdateReminderAsync();
                }

                return exitCode;
            }

            // 全局 --help/-h（仅在未匹配子命令时）
            if (IsHelp(args))
            {
                WriteGlobalHelp();
                return 0;
            }

            // --version
            if (IsVersion(args))
            {
                WriteVersion();
                return 0;
            }

            // 未识别的全局选项
            JsonlOutput.WriteError($"未知参数: {string.Join(" ", args)}", "UNKNOWN_ARGUMENT");
            return 1;
        }
        catch (Exception ex)
        {
            JsonlOutput.WriteError($"CLI 内部错误: {ex.Message}", "INTERNAL_ERROR");
            return 1;
        }
        finally
        {
            ConsoleHelper.Cleanup();
        }
    }

    /// <summary>
    /// 解析 --key value 参数对。
    /// </summary>
    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") || args[i].StartsWith("-"))
            {
                string key = args[i].TrimStart('-');
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[key] = args[i + 1];
                    i++;
                }
                else
                {
                    // 标志型参数（不带值）
                    options[key] = "true";
                }
            }
        }
        return options;
    }

    private static bool IsHelp(string[] args)
    {
        return args.Any(a => a is "--help" or "-h");
    }

    private static bool IsVersion(string[] args)
    {
        return args.Any(a => a is "--version" or "-v");
    }

    private async Task<int> RunSubCommandAsync(string command, Dictionary<string, string> options)
    {
        // 查找注册的 ICliCommand 实现
        var handlers = _serviceProvider.GetServices<ICliCommand>();
        var handler = handlers.FirstOrDefault(h =>
            h.CommandName.Equals(command, StringComparison.OrdinalIgnoreCase));

        if (handler is null)
        {
            JsonlOutput.WriteError($"未实现的子命令: {command}", "COMMAND_NOT_IMPLEMENTED");
            return 1;
        }

        return await handler.ExecuteAsync(options);
    }

    private static int HandleUnknownCommand(string command)
    {
        JsonlOutput.WriteError($"未知子命令: {command}。支持的子命令: inspect, fill, cleanup, update", "UNKNOWN_COMMAND");
        return 1;
    }

    private static void WriteGlobalHelp()
    {
        var version = GetVersion();
        // Line 1: help overview
        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "help",
            ["name"] = "DocuFiller",
            ["version"] = version,
            ["description"] = "Word文档批量填充工具",
        });

        // Line 2-4: subcommand entries (fill, cleanup, inspect)
        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "command",
            ["name"] = "fill",
            ["description"] = "使用Excel数据批量填充Word模板",
            ["usage"] = "DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> [options]",
            ["options"] = new[]
            {
                new { name = "--template", required = true, description = "模板文件路径" },
                new { name = "--data", required = true, description = "Excel数据文件路径" },
                new { name = "--output", required = true, description = "输出目录" },
                new { name = "--folder", required = false, description = "文件夹批量模式" },
                new { name = "--overwrite", required = false, description = "覆盖已存在文件" },
            },
        });

        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "command",
            ["name"] = "cleanup",
            ["description"] = "清理Word文档中的批注和内容控件",
            ["usage"] = "DocuFiller.exe cleanup --input <path> [options]",
            ["options"] = new[]
            {
                new { name = "--input", required = true, description = "文件或文件夹路径" },
                new { name = "--output", required = false, description = "输出目录" },
                new { name = "--folder", required = false, description = "文件夹批量模式" },
            },
        });

        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "command",
            ["name"] = "inspect",
            ["description"] = "查询模板中的内容控件列表",
            ["usage"] = "DocuFiller.exe inspect --template <path>",
            ["options"] = new[]
            {
                new { name = "--template", required = true, description = "模板文件路径" },
            },
        });

        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "command",
            ["name"] = "update",
            ["description"] = "检查并安装应用更新",
            ["usage"] = "DocuFiller.exe update [options]",
            ["options"] = new[]
            {
                new { name = "--yes", required = false, description = "自动下载更新并重启应用" },
            },
        });

        // Line 5: examples
        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "examples",
            ["items"] = new[]
            {
                "DocuFiller.exe inspect --template report.docx",
                "DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output",
                "DocuFiller.exe cleanup --input ./docs",
                "DocuFiller.exe update",
                "DocuFiller.exe update --yes",
            },
        });
    }

    private static void WriteSubCommandHelp(string command)
    {
        Dictionary<string, object>? lineData = command switch
        {
            "fill" => new Dictionary<string, object>
            {
                ["type"] = "command",
                ["name"] = "fill",
                ["description"] = "使用Excel数据批量填充Word模板",
                ["usage"] = "DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> [options]",
                ["options"] = new[]
                {
                    new { name = "--template", required = true, description = "模板文件路径" },
                    new { name = "--data", required = true, description = "Excel数据文件路径" },
                    new { name = "--output", required = true, description = "输出目录" },
                    new { name = "--folder", required = false, description = "文件夹批量模式" },
                    new { name = "--overwrite", required = false, description = "覆盖已存在文件" },
                },
            },
            "cleanup" => new Dictionary<string, object>
            {
                ["type"] = "command",
                ["name"] = "cleanup",
                ["description"] = "清理Word文档中的批注和内容控件",
                ["usage"] = "DocuFiller.exe cleanup --input <path> [options]",
                ["options"] = new[]
                {
                    new { name = "--input", required = true, description = "文件或文件夹路径" },
                    new { name = "--output", required = false, description = "输出目录" },
                    new { name = "--folder", required = false, description = "文件夹批量模式" },
                },
            },
            "inspect" => new Dictionary<string, object>
            {
                ["type"] = "command",
                ["name"] = "inspect",
                ["description"] = "查询模板中的内容控件列表",
                ["usage"] = "DocuFiller.exe inspect --template <path>",
                ["options"] = new[]
                {
                    new { name = "--template", required = true, description = "模板文件路径" },
                },
            },
            "update" => new Dictionary<string, object>
            {
                ["type"] = "command",
                ["name"] = "update",
                ["description"] = "检查并安装应用更新",
                ["usage"] = "DocuFiller.exe update [options]",
                ["options"] = new[]
                {
                    new { name = "--yes", required = false, description = "自动下载更新并重启应用" },
                },
            },
            _ => null,
        };

        if (lineData is not null)
        {
            WriteJsonLine(lineData);
        }
        else
        {
            JsonlOutput.WriteError($"未知子命令: {command}。支持的子命令: inspect, fill, cleanup, update", "UNKNOWN_COMMAND");
        }
    }

    private static void WriteVersion()
    {
        WriteJsonLine(new Dictionary<string, object>
        {
            ["type"] = "version",
            ["version"] = GetVersion(),
        });
    }

    private static string GetVersion()
    {
        var v = Assembly.GetEntryAssembly()?.GetName().Version;
        return v is not null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0";
    }

    /// <summary>
    /// 条件性追加 update reminder JSONL 行。
    /// 仅在有新版本时输出，已是最新或检查失败时不输出。
    /// 不影响调用方的 exitCode。
    /// </summary>
    private async Task TryAppendUpdateReminderAsync()
    {
        try
        {
            var updateService = _serviceProvider.GetService<IUpdateService>();
            if (updateService is null)
            {
                return;
            }

            var updateInfo = await updateService.CheckForUpdatesAsync();
            if (updateInfo is not null)
            {
                JsonlOutput.WriteUpdate(new
                {
                    reminder = true,
                    latestVersion = updateInfo.TargetFullRelease.Version?.ToString(),
                    message = $"发现新版本: {updateInfo.TargetFullRelease.Version}，运行 update --yes 更新",
                });
            }

            // 已是最新版本时不输出任何内容
        }
        catch
        {
            // 更新检查失败不影响原命令结果，静默跳过
        }
    }

    /// <summary>
    /// 序列化对象为单行 JSON 并输出到控制台（无 envelope 包裹）。
    /// </summary>
    private static void WriteJsonLine(object data)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };
        string json = JsonSerializer.Serialize(data, options);
        Console.WriteLine(json);
    }
}

/// <summary>
/// CLI 子命令处理器接口。
/// </summary>
public interface ICliCommand
{
    /// <summary>
    /// 子命令名称（如 "inspect"、"fill"）。
    /// </summary>
    string CommandName { get; }

    /// <summary>
    /// 执行子命令。
    /// </summary>
    /// <param name="options">解析后的 key-value 参数</param>
    /// <returns>Exit code: 0=成功, 1=失败</returns>
    Task<int> ExecuteAsync(Dictionary<string, string> options);
}
