using System;
using System.Threading;
using System.Threading.Tasks;
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update
{
    /// <summary>
    /// 更新服务接口
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// 检查是否有可用更新
        /// </summary>
        /// <param name="currentVersion">当前版本号</param>
        /// <param name="channel">发布渠道</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本信息，如果没有可用更新则返回 null</returns>
        Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel, CancellationToken cancellationToken = default);

        /// <summary>
        /// 下载更新包
        /// </summary>
        /// <param name="version">版本信息</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载的文件路径</returns>
        Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证文件哈希
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expectedHash">期望的哈希值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证是否通过</returns>
        Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, CancellationToken cancellationToken = default);

        /// <summary>
        /// 安装更新
        /// </summary>
        /// <param name="packagePath">更新包路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>安装是否成功</returns>
        Task<bool> InstallUpdateAsync(string packagePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取当前更新配置
        /// </summary>
        /// <returns>更新配置</returns>
        UpdateConfig GetConfig();

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="config">新配置</param>
        void UpdateConfig(UpdateConfig config);

        /// <summary>
        /// 更新可用事件
        /// </summary>
        event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

        /// <summary>
        /// 下载进度事件
        /// </summary>
        event EventHandler<DownloadProgress>? DownloadProgress;

        /// <summary>
        /// 更新安装前事件（可用于保存状态、关闭窗口等）
        /// </summary>
        event EventHandler? UpdateInstalling;
    }
}
