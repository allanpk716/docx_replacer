using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Velopack;
using Xunit;
using DocuFiller.Cli;
using DocuFiller.Cli.Commands;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Tests.Cli;

/// <summary>
/// UpdateCommand 路由、JSONL 输出格式、post-command 更新提醒逻辑测试
/// </summary>
public class UpdateCommandTests
{
    /// <summary>
    /// Stub IUpdateService for testing UpdateCommand
    /// </summary>
    private class StubUpdateService : IUpdateService
    {
        public UpdateInfo? UpdateInfoToReturn { get; init; }
        public bool IsInstalledValue { get; init; } = true;
        public bool IsUpdateUrlConfigured => true;
        public string Channel => "stable";
        public bool IsInstalled => IsInstalledValue;
        public string UpdateSourceType => "GitHub";
        public string EffectiveUpdateUrl => "";

        public Task<UpdateInfo?> CheckForUpdatesAsync() => Task.FromResult(UpdateInfoToReturn);

        public Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null)
        {
            progressCallback?.Invoke(100);
            return Task.CompletedTask;
        }

        public void ApplyUpdatesAndRestart()
        {
            // No-op in tests — real implementation would restart the process
        }

        public void ReloadSource(string updateUrl, string channel) { }
    }

    private static UpdateInfo CreateUpdateInfo(string version = "2.0.0")
    {
        var asset = new VelopackAsset
        {
            Version = SemanticVersion.Parse(version),
            PackageId = "DocuFiller",
            Type = VelopackAssetType.Full,
        };
        return new UpdateInfo(asset, false, asset, Array.Empty<VelopackAsset>());
    }

    private static (CliRunner runner, ServiceProvider provider) CreateRunnerWithUpdate(
        StubUpdateService updateService, params ICliCommand[] extraCommands)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUpdateService>(updateService);
        services.AddSingleton<ILogger<UpdateCommand>, Logger<UpdateCommand>>();
        services.AddSingleton<ICliCommand, UpdateCommand>();
        foreach (var cmd in extraCommands)
        {
            services.AddSingleton<ICliCommand>(cmd);
        }
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return (new CliRunner(provider), provider);
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

    // === UpdateCommand routing and help tests ===

    [Fact]
    public async Task Update_Help_OutputsUpdateCommandHelp()
    {
        var updateService = new StubUpdateService();
        var (runner, _) = CreateRunnerWithUpdate(updateService);
        var (exitCode, output) = await RunAndCapture(runner, new[] { "update", "--help" });

        Assert.Equal(0, exitCode);
        var doc = ParseLine(output);
        Assert.Equal("command", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("update", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Update_DispatchesToCorrectHandler()
    {
        var dispatched = false;
        var updateService = new StubUpdateService();
        // We register the real UpdateCommand (not a stub) via DI in CreateRunnerWithUpdate
        // and just verify routing works by checking output
        var (runner, _) = CreateRunnerWithUpdate(updateService);
        var (exitCode, output) = await RunAndCapture(runner, new[] { "update" });

        // Should succeed and output JSONL update info (not an error about unknown command)
        Assert.Equal(0, exitCode);
        Assert.Contains("update", output);
        Assert.DoesNotContain("UNKNOWN_COMMAND", output);
        Assert.DoesNotContain("COMMAND_NOT_IMPLEMENTED", output);
    }

    // === UpdateCommand no --yes: version info JSONL output ===

    [Fact]
    public async Task Update_NoYes_OutputsVersionInfo()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = CreateUpdateInfo("2.0.0"),
        };
        var (runner, _) = CreateRunnerWithUpdate(updateService);
        var (exitCode, output) = await RunAndCapture(runner, new[] { "update" });

        Assert.Equal(0, exitCode);

        // First line: type=update with version info
        var updateLine = ParseLine(output, 0);
        Assert.Equal("update", updateLine.RootElement.GetProperty("type").GetString());
        Assert.Equal("success", updateLine.RootElement.GetProperty("status").GetString());

        var data = updateLine.RootElement.GetProperty("data");
        Assert.True(data.TryGetProperty("hasUpdate", out var hasUpdate));
        Assert.True(hasUpdate.GetBoolean());
        Assert.True(data.TryGetProperty("latestVersion", out _));
        Assert.True(data.TryGetProperty("currentVersion", out _));

        // Second line: type=summary with "发现新版本"
        var summaryLine = ParseLine(output, 1);
        Assert.Equal("summary", summaryLine.RootElement.GetProperty("type").GetString());
        Assert.Contains("发现新版本", summaryLine.RootElement.GetProperty("data").GetProperty("message").GetString());
    }

    [Fact]
    public async Task Update_NoYes_NoUpdate_OutputsAlreadyUpToDate()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = null, // no update available
        };
        var (runner, _) = CreateRunnerWithUpdate(updateService);
        var (exitCode, output) = await RunAndCapture(runner, new[] { "update" });

        Assert.Equal(0, exitCode);

        // First line: type=update with hasUpdate=false
        var updateLine = ParseLine(output, 0);
        Assert.Equal("update", updateLine.RootElement.GetProperty("type").GetString());
        var data = updateLine.RootElement.GetProperty("data");
        Assert.False(data.GetProperty("hasUpdate").GetBoolean());

        // Second line: type=summary with "当前已是最新版本"
        var summaryLine = ParseLine(output, 1);
        Assert.Contains("当前已是最新版本", summaryLine.RootElement.GetProperty("data").GetProperty("message").GetString());
    }

    // === UpdateCommand --yes: portable error and no-update cases ===

    [Fact]
    public async Task Update_WithYes_Portable_OutputsError()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = CreateUpdateInfo("2.0.0"),
            IsInstalledValue = false, // portable mode
        };
        var (runner, _) = CreateRunnerWithUpdate(updateService);
        var (exitCode, output) = await RunAndCapture(runner, new[] { "update", "--yes" });

        Assert.Equal(1, exitCode);
        var errorLine = ParseLine(output);
        Assert.Equal("error", errorLine.RootElement.GetProperty("type").GetString());
        var data = errorLine.RootElement.GetProperty("data");
        Assert.Equal("PORTABLE_NOT_SUPPORTED", data.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_WithYes_NoUpdate_ReturnsSuccess()
    {
        var updateService = new StubUpdateService
        {
            UpdateInfoToReturn = null, // no update
            IsInstalledValue = true,
        };
        var (runner, _) = CreateRunnerWithUpdate(updateService);
        var (exitCode, output) = await RunAndCapture(runner, new[] { "update", "--yes" });

        Assert.Equal(0, exitCode);
        var updateLine = ParseLine(output);
        Assert.Equal("update", updateLine.RootElement.GetProperty("type").GetString());
        Assert.Contains("当前已是最新版本", updateLine.RootElement.GetProperty("data").GetProperty("message").GetString());
    }
}
