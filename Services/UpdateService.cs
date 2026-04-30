using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace DocuFiller.Services
{
    /// <summary>
    /// 更新服务实现，封装 Velopack UpdateManager 的检查、下载、应用更新功能
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private IUpdateSource _updateSource;
        private string _updateUrl;
        private string _channel;
        private string _sourceType;
        private readonly bool _isInstalled;

        /// <summary>
        /// appsettings.json 文件路径，用于测试时替换为临时文件路径
        /// </summary>
        internal string AppSettingsPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var url = configuration?["Update:UpdateUrl"];
            var rawUrl = string.IsNullOrWhiteSpace(url) ? "" : url;

            // Channel 为空或 null 时默认 "stable"
            var channel = configuration?["Update:Channel"];
            _channel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim();

            if (!string.IsNullOrWhiteSpace(rawUrl))
            {
                // HTTP URL 模式：内网 Go 服务器
                _updateUrl = rawUrl.TrimEnd('/') + "/" + _channel + "/";
                _updateSource = new SimpleWebSource(_updateUrl);
                _sourceType = "HTTP";
            }
            else
            {
                // GitHub Releases 模式：外网用户备选
                _updateUrl = "";
                _updateSource = new GithubSource("https://github.com/allanpk716/docx_replacer", accessToken: null, prerelease: false);
                _sourceType = "GitHub";
            }

            // 检测 IsInstalled 状态（便携版/开发环境返回 false）
            try
            {
                var tempManager = CreateUpdateManager();
                _isInstalled = tempManager.IsInstalled;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检测安装状态失败，默认为未安装");
                _isInstalled = false;
            }

            _logger.LogInformation("更新服务初始化，源类型: {SourceType}，通道: {Channel}，更新源: {UpdateUrl}，IsInstalled: {IsInstalled}",
                _sourceType, _channel, _updateUrl != "" ? _updateUrl : "GitHub Releases", _isInstalled);
        }

        /// <inheritdoc />
        public bool IsUpdateUrlConfigured => true; // GitHub Releases 始终可用作备选

        /// <inheritdoc />
        public string Channel => _channel;

        /// <inheritdoc />
        public bool IsInstalled => _isInstalled;

        /// <inheritdoc />
        public string UpdateSourceType => _sourceType;

        /// <inheritdoc />
        public string EffectiveUpdateUrl => _updateUrl;

        /// <inheritdoc />
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            _logger.LogInformation("开始检查更新，更新源: {UpdateUrl}", _updateUrl);

            var updateManager = CreateUpdateManager();
            var updateInfo = await updateManager.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                _logger.LogInformation("发现新版本: {Version}", updateInfo.TargetFullRelease.Version);
            }
            else
            {
                _logger.LogInformation("当前已是最新版本");
            }

            return updateInfo;
        }

        /// <inheritdoc />
        public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (updateInfo == null) throw new ArgumentNullException(nameof(updateInfo));

            _logger.LogInformation("开始下载更新: {Version}", updateInfo.TargetFullRelease.Version);

            var updateManager = CreateUpdateManager();
            await updateManager.DownloadUpdatesAsync(updateInfo, progressCallback, cancellationToken);

            _logger.LogInformation("更新下载完成");
        }

        /// <inheritdoc />
        public void ApplyUpdatesAndRestart()
        {
            _logger.LogInformation("开始应用更新并重启应用");

            var updateManager = CreateUpdateManager();

            // 优先使用已下载待应用的更新包，若不存在则使用 UpdateInfo 中的目标版本
            var pendingAsset = updateManager.UpdatePendingRestart;
            updateManager.ApplyUpdatesAndRestart(pendingAsset);
        }

        /// <inheritdoc />
        public void ReloadSource(string updateUrl, string channel)
        {
            updateUrl ??= "";
            channel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim();

            var oldSourceType = _sourceType;
            var oldUpdateUrl = _updateUrl;
            var oldChannel = _channel;

            _logger.LogInformation("热重载更新源：源类型 {OldSourceType} → {NewSourceType}，通道 {OldChannel} → {NewChannel}，URL {OldUrl} → {NewUrl}",
                oldSourceType, string.IsNullOrWhiteSpace(updateUrl) ? "GitHub" : "HTTP",
                oldChannel, channel,
                oldUpdateUrl != "" ? oldUpdateUrl : "GitHub Releases",
                string.IsNullOrWhiteSpace(updateUrl) ? "GitHub Releases" : updateUrl);

            if (!string.IsNullOrWhiteSpace(updateUrl))
            {
                _updateUrl = updateUrl.TrimEnd('/') + "/" + channel + "/";
                _updateSource = new SimpleWebSource(_updateUrl);
                _sourceType = "HTTP";
            }
            else
            {
                _updateUrl = "";
                _updateSource = new GithubSource("https://github.com/allanpk716/docx_replacer", accessToken: null, prerelease: false);
                _sourceType = "GitHub";
            }

            _channel = channel;

            _logger.LogInformation("更新源热重载完成，源类型: {SourceType}，通道: {Channel}，更新源: {UpdateUrl}",
                _sourceType, _channel, _updateUrl != "" ? _updateUrl : "GitHub Releases");

            PersistToAppSettings(updateUrl, _channel);
        }

        /// <summary>
        /// 将更新源配置持久化到 appsettings.json 文件
        /// </summary>
        private void PersistToAppSettings(string updateUrl, string channel)
        {
            try
            {
                var path = AppSettingsPath;
                if (!File.Exists(path))
                {
                    _logger.LogWarning("appsettings.json 文件不存在，跳过持久化: {Path}", path);
                    return;
                }
                var json = File.ReadAllText(path);
                var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Failed to parse appsettings.json");
                if (node["Update"] == null)
                    node["Update"] = new JsonObject();
                node["Update"]!["UpdateUrl"] = updateUrl;
                node["Update"]!["Channel"] = channel;
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, node.ToJsonString(options));
                _logger.LogInformation("已将更新源配置持久化到 appsettings.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "持久化更新源配置到 appsettings.json 失败，内存热重载已生效");
            }
        }

        private UpdateManager CreateUpdateManager()
        {
            return new UpdateManager(_updateSource, new UpdateOptions { ExplicitChannel = _channel });
        }
    }
}
