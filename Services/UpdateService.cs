using System;
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
        private readonly IUpdateSource _updateSource;
        private readonly string _updateUrl;
        private readonly string _channel;
        private readonly string _sourceType;
        private readonly bool _isInstalled;

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

        /// <summary>
        /// 构造后的完整更新源 URL（包含通道路径），用于测试验证
        /// </summary>
        internal string EffectiveUpdateUrl => _updateUrl;

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
        public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null)
        {
            if (updateInfo == null) throw new ArgumentNullException(nameof(updateInfo));

            _logger.LogInformation("开始下载更新: {Version}", updateInfo.TargetFullRelease.Version);

            var updateManager = CreateUpdateManager();
            await updateManager.DownloadUpdatesAsync(updateInfo, progressCallback);

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

        private UpdateManager CreateUpdateManager()
        {
            return new UpdateManager(_updateSource, new UpdateOptions { ExplicitChannel = _channel });
        }
    }
}
