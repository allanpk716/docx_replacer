using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Velopack;
using Xunit;
using DocuFiller.Cli;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Tests.Cli;

/// <summary>
/// CliRunner 路由和分发测试
/// </summary>
public class CliRunnerTests
{
    /// <summary>
    /// Stub ICliCommand for testing routing
    /// </summary>
    private class StubCommand : ICliCommand
    {
        public string CommandName { get; init; } = "";
        public Func<Dictionary<string, string>, Task<int>>? ExecuteFn { get; init; }

        public Task<int> ExecuteAsync(Dictionary<string, string> options)
        {
            return ExecuteFn != null ? ExecuteFn(options) : Task.FromResult(0);
        }
    }

    private static CliRunner CreateRunner(params ICliCommand[] commands)
    {
        var services = new ServiceCollection();
        foreach (var cmd in commands)
        {
            services.AddSingleton<ICliCommand>(cmd);
        }
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return new CliRunner(provider);
    }

    private static async Task<(int exitCode, string output)> RunAndCapture(
        CliRunner runner, string[] args)
    {
        var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            var code = await runner.RunAsync(args);
            return (code, sw.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static JsonDocument ParseLine(string output, int lineIndex = 0)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lineIndex < lines.Length, $"Line {lineIndex} not found in output");
        return JsonDocument.Parse(lines[lineIndex]);
    }

    // === Tests ===

    [Fact]
    public async Task EmptyArgs_ReturnsMinusOne_ForGuiMode()
    {
        var runner = CreateRunner();
        var (exitCode, _) = await RunAndCapture(runner, Array.Empty<string>());

        Assert.Equal(-1, exitCode);
    }

    [Fact]
    public async Task HelpFlag_OutputsGlobalHelp()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "--help" });

        Assert.Equal(0, exitCode);
        // Should output multiple JSON lines for help
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 2, "Expected at least 2 lines of help output");

        // First line should be help overview
        var first = JsonDocument.Parse(lines[0]);
        Assert.Equal("help", first.RootElement.GetProperty("type").GetString());
        Assert.Equal("DocuFiller", first.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ShortHelpFlag_OutputsGlobalHelp()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "-h" });

        Assert.Equal(0, exitCode);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 1);
        var first = JsonDocument.Parse(lines[0]);
        Assert.Equal("help", first.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task VersionFlag_OutputsVersionJson()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "--version" });

        Assert.Equal(0, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("version", doc.RootElement.GetProperty("type").GetString());
        Assert.True(doc.RootElement.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task ShortVersionFlag_OutputsVersionJson()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "-v" });

        Assert.Equal(0, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("version", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task UnknownCommand_OutputsUnknownCommandError()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "badcommand" });

        Assert.Equal(1, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("error", doc.RootElement.GetProperty("type").GetString());
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("UNKNOWN_COMMAND", data.GetProperty("code").GetString());
    }

    [Fact]
    public async Task FillHelp_OutputsFillCommandHelp()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "fill", "--help" });

        Assert.Equal(0, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("command", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("fill", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task InspectHelp_OutputsInspectCommandHelp()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "inspect", "--help" });

        Assert.Equal(0, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("command", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("inspect", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task CleanupHelp_OutputsCleanupCommandHelp()
    {
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "cleanup", "--help" });

        Assert.Equal(0, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("command", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("cleanup", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task FillCommand_DispatchesToCorrectHandler()
    {
        var dispatched = false;
        var fillCmd = new StubCommand
        {
            CommandName = "fill",
            ExecuteFn = _ => { dispatched = true; return Task.FromResult(0); }
        };
        var runner = CreateRunner(fillCmd);

        // Use a non-existent template so validation fails — but routing should reach FillCommand
        var (exitCode, _) = await RunAndCapture(runner,
            new[] { "fill", "--template", "nonexistent.docx", "--data", "nonexistent.xlsx", "--output", "./out" });

        // Command was dispatched (it's FillCommand, not our stub — because the real FillCommand is compiled in too)
        // But since we register our stub AND the real commands aren't registered, only our stub should run
        Assert.True(dispatched, "fill command should have been dispatched to the stub");
    }

    [Fact]
    public async Task InspectCommand_DispatchesToCorrectHandler()
    {
        var dispatched = false;
        var inspectCmd = new StubCommand
        {
            CommandName = "inspect",
            ExecuteFn = _ => { dispatched = true; return Task.FromResult(0); }
        };
        var runner = CreateRunner(inspectCmd);

        await RunAndCapture(runner, new[] { "inspect", "--template", "test.docx" });

        Assert.True(dispatched, "inspect command should have been dispatched to the stub");
    }

    [Fact]
    public async Task CleanupCommand_DispatchesToCorrectHandler()
    {
        var dispatched = false;
        var cleanupCmd = new StubCommand
        {
            CommandName = "cleanup",
            ExecuteFn = _ => { dispatched = true; return Task.FromResult(0); }
        };
        var runner = CreateRunner(cleanupCmd);

        await RunAndCapture(runner, new[] { "cleanup", "--input", "test.docx" });

        Assert.True(dispatched, "cleanup command should have been dispatched to the stub");
    }

    [Fact]
    public async Task UnregisteredCommand_ReturnsCommandNotImplementeded()
    {
        // No commands registered — any subcommand should get COMMAND_NOT_IMPLEMENTED
        var runner = CreateRunner();
        var (exitCode, output) = await RunAndCapture(runner, new[] { "fill", "--template", "a.docx" });

        Assert.Equal(1, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("error", doc.RootElement.GetProperty("type").GetString());
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("COMMAND_NOT_IMPLEMENTED", data.GetProperty("code").GetString());
    }

    // === Post-command update reminder tests ===

    /// <summary>
    /// Stub IUpdateService for post-command update reminder testing
    /// </summary>
    private class StubUpdateService : IUpdateService
    {
        public UpdateInfo? UpdateInfoToReturn { get; init; }
        public bool IsInstalled => true;
        public bool IsPortable => false;
        public bool IsUpdateUrlConfigured => true;
        public string Channel => "stable";
        public string UpdateSourceType => "GitHub";
        public string EffectiveUpdateUrl => "";

        public Task<UpdateInfo?> CheckForUpdatesAsync() => Task.FromResult(UpdateInfoToReturn);
        public Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ApplyUpdatesAndRestart() { }
        public void ReloadSource(string updateUrl, string channel) { }
    }

    private static UpdateInfo CreateTestUpdateInfo(string version = "2.0.0")
    {
        var asset = new VelopackAsset
        {
            Version = SemanticVersion.Parse(version),
            PackageId = "DocuFiller",
            Type = VelopackAssetType.Full,
        };
        return new UpdateInfo(asset, false, asset, Array.Empty<VelopackAsset>());
    }

    private static CliRunner CreateRunnerWithUpdateService(
        IUpdateService updateService, params ICliCommand[] commands)
    {
        var services = new ServiceCollection();
        services.AddSingleton(updateService);
        foreach (var cmd in commands)
        {
            services.AddSingleton<ICliCommand>(cmd);
        }
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return new CliRunner(provider);
    }

    [Fact]
    public async Task PostCommand_UpdateAvailable_AppendsUpdateLine()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = CreateTestUpdateInfo("2.0.0"),
        };
        var stubCmd = new StubCommand
        {
            CommandName = "fill",
            ExecuteFn = _ => Task.FromResult(0), // success
        };
        var runner = CreateRunnerWithUpdateService(updateService, stubCmd);
        var (exitCode, output) = await RunAndCapture(runner,
            new[] { "fill", "--template", "test.docx", "--data", "test.xlsx", "--output", "./out" });

        Assert.Equal(0, exitCode);

        // Should have appended an update reminder line at the end
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 1, "Expected at least one output line");

        // Last line should be the update reminder
        var lastLine = JsonDocument.Parse(lines[^1]);
        Assert.Equal("update", lastLine.RootElement.GetProperty("type").GetString());
        var data = lastLine.RootElement.GetProperty("data");
        Assert.True(data.TryGetProperty("reminder", out var reminder));
        Assert.True(reminder.GetBoolean());
        Assert.True(data.TryGetProperty("latestVersion", out _));
    }

    [Fact]
    public async Task PostCommand_NoUpdate_NoExtraLine()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = null, // no update
        };
        var stubCmd = new StubCommand
        {
            CommandName = "fill",
            ExecuteFn = _ => Task.FromResult(0), // success
        };
        var runner = CreateRunnerWithUpdateService(updateService, stubCmd);
        var (exitCode, output) = await RunAndCapture(runner,
            new[] { "fill", "--template", "test.docx", "--data", "test.xlsx", "--output", "./out" });

        Assert.Equal(0, exitCode);

        // No update reminder should be appended
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var doc = JsonDocument.Parse(line);
            Assert.NotEqual("update", doc.RootElement.GetProperty("type").GetString());
        }
    }

    [Fact]
    public async Task PostCommand_FailedCommand_NoUpdateLine()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = CreateTestUpdateInfo("2.0.0"), // update available
        };
        var stubCmd = new StubCommand
        {
            CommandName = "fill",
            ExecuteFn = _ => Task.FromResult(1), // failure
        };
        var runner = CreateRunnerWithUpdateService(updateService, stubCmd);
        var (exitCode, output) = await RunAndCapture(runner,
            new[] { "fill", "--template", "test.docx", "--data", "test.xlsx", "--output", "./out" });

        Assert.Equal(1, exitCode);

        // No update reminder for failed commands
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var doc = JsonDocument.Parse(line);
            Assert.NotEqual("update", doc.RootElement.GetProperty("type").GetString());
        }
    }
}
