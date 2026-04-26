using System.Reflection;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Cli.Commands;

/// <summary>
/// update 子命令处理器：检查版本更新，--yes 时执行下载并重启应用。
/// </summary>
public class UpdateCommand : ICliCommand
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdateCommand> _logger;

    public UpdateCommand(IUpdateService updateService, ILogger<UpdateCommand> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    public string CommandName => "update";

    public async Task<int> ExecuteAsync(Dictionary<string, string> options)
    {
        var hasYes = options.ContainsKey("yes") &&
                     !options["yes"].Equals("false", StringComparison.OrdinalIgnoreCase);

        try
        {
            _logger.LogInformation("开始检查更新 (yes={HasYes})", hasYes);

            var updateInfo = await _updateService.CheckForUpdatesAsync();

            if (!hasYes)
            {
                // 无 --yes：仅输出版本信息
                var currentVersion = GetCurrentVersion();
                JsonlOutput.WriteUpdate(new
                {
                    currentVersion,
                    latestVersion = updateInfo?.TargetFullRelease.Version?.ToString() ?? currentVersion,
                    hasUpdate = updateInfo != null,
                    isInstalled = _updateService.IsInstalled,
                    updateSourceType = _updateService.UpdateSourceType,
                });

                if (updateInfo != null)
                {
                    JsonlOutput.WriteSummary(new
                    {
                        message = $"发现新版本: {updateInfo.TargetFullRelease.Version}",
                    });
                }
                else
                {
                    JsonlOutput.WriteSummary(new
                    {
                        message = "当前已是最新版本",
                    });
                }

                return 0;
            }

            // --yes 模式：执行下载和重启
            if (!_updateService.IsInstalled)
            {
                JsonlOutput.WriteError("便携版不支持自动更新，请使用安装版", "PORTABLE_NOT_SUPPORTED");
                return 1;
            }

            if (updateInfo == null)
            {
                JsonlOutput.WriteUpdate(new
                {
                    message = "当前已是最新版本",
                });
                return 0;
            }

            // 下载更新
            _logger.LogInformation("开始下载更新: {Version}", updateInfo.TargetFullRelease.Version);

            await _updateService.DownloadUpdatesAsync(updateInfo, progress =>
            {
                JsonlOutput.WriteUpdate(new
                {
                    progress,
                    message = $"正在下载更新 {progress}%",
                });
            });

            // 应用更新并重启（不会返回）
            _logger.LogInformation("下载完成，准备应用更新并重启");
            _updateService.ApplyUpdatesAndRestart();

            // 理论上不会到达这里
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新操作失败");

            var errorCode = hasYes ? "UPDATE_DOWNLOAD_ERROR" : "UPDATE_CHECK_ERROR";
            JsonlOutput.WriteError($"更新操作失败: {ex.Message}", errorCode);
            return 1;
        }
    }

    private static string GetCurrentVersion()
    {
        var v = Assembly.GetEntryAssembly()?.GetName().Version;
        return v is not null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0";
    }
}
