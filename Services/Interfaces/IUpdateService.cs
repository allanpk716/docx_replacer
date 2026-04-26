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

        /// <summary>更新源 URL 是否已配置（始终为 true，GitHub Releases 作为备选源）</summary>
        bool IsUpdateUrlConfigured { get; }

        /// <summary>当前更新通道（stable/beta），默认 stable</summary>
        string Channel { get; }

        /// <summary>当前应用是否为安装版（便携版返回 false）</summary>
        bool IsInstalled { get; }

        /// <summary>当前更新源类型（"GitHub" 或 "HTTP"），用于诊断和测试</summary>
        string UpdateSourceType { get; }
    }
}
