using System;
using System.Threading.Tasks;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Velopack;

namespace DocuFiller.Services
{
    /// <summary>
    /// 更新服务实现，封装 Velopack UpdateManager 的检查、下载、应用更新功能
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly string _updateUrl;

        public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var url = configuration?["Update:UpdateUrl"];
            _updateUrl = string.IsNullOrWhiteSpace(url) ? "" : url;

            if (!IsUpdateUrlConfigured)
            {
                _logger.LogWarning("更新源 URL 未配置（Update:UpdateUrl 为空），更新功能不可用");
            }
        }

        /// <inheritdoc />
        public bool IsUpdateUrlConfigured => !string.IsNullOrWhiteSpace(_updateUrl);

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
            return new UpdateManager(_updateUrl);
        }
    }
}
