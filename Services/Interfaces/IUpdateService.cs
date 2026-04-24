using System;
using System.Threading.Tasks;
using Velopack;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 更新服务接口，封装 Velopack UpdateManager
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>检查更新源是否有新版本</summary>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>下载更新包</summary>
        Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null);

        /// <summary>应用已下载的更新并重启应用</summary>
        void ApplyUpdatesAndRestart();

        /// <summary>更新源 URL 是否已配置</summary>
        bool IsUpdateUrlConfigured { get; }
    }
}
