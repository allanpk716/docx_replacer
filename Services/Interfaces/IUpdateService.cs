using System;
using System.Threading;
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
        Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null, CancellationToken cancellationToken = default);

        /// <summary>应用已下载的更新并重启应用</summary>
        void ApplyUpdatesAndRestart();

        /// <summary>更新源 URL 是否已配置（始终为 true，GitHub Releases 作为备选源）</summary>
        bool IsUpdateUrlConfigured { get; }

        /// <summary>当前更新通道（stable/beta），默认 stable</summary>
        string Channel { get; }

        /// <summary>当前应用是否为安装版（信息属性，不用于流程阻断）</summary>
        bool IsInstalled { get; }

        /// <summary>当前应用是否为便携版运行（解压自 Portable.zip）</summary>
        bool IsPortable { get; }

        /// <summary>当前更新源类型（"GitHub" 或 "HTTP"），用于诊断和测试</summary>
        string UpdateSourceType { get; }

        /// <summary>
        /// 当前生效的完整更新源 URL（含通道路径）。GitHub 模式返回 CDN 直连 URL。
        /// </summary>
        string EffectiveUpdateUrl { get; }

        /// <summary>
        /// 热重载更新源。<paramref name="updateUrl"/> 为空时走 GitHub Releases，非空时走 HTTP。
        /// 同时持久化到 appsettings.json。
        /// </summary>
        /// <param name="updateUrl">更新源 URL，空字符串或 null 表示使用 GitHub Releases</param>
        /// <param name="channel">更新通道（stable/beta），null 默认 "stable"</param>
        void ReloadSource(string updateUrl, string channel);
    }
}
