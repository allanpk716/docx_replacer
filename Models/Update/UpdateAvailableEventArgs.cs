using System;

namespace DocuFiller.Models.Update
{
    /// <summary>
    /// 更新可用事件参数
    /// </summary>
    public class UpdateAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// 版本信息
        /// </summary>
        public VersionInfo Version { get; set; } = new VersionInfo();

        /// <summary>
        /// 是否已下载
        /// </summary>
        public bool IsDownloaded { get; set; }

        /// <summary>
        /// 当前版本号
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// 是否为强制更新
        /// </summary>
        public bool IsMandatory => Version.Mandatory;

        /// <summary>
        /// 获取版本差异描述
        /// </summary>
        public string GetVersionDifference()
        {
            return $"当前版本: {CurrentVersion} -> 最新版本: {Version.Version}";
        }

        /// <summary>
        /// 创建更新可用事件参数
        /// </summary>
        /// <param name="version">版本信息</param>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="isDownloaded">是否已下载</param>
        /// <returns>事件参数实例</returns>
        public static UpdateAvailableEventArgs Create(VersionInfo version, string currentVersion, bool isDownloaded = false)
        {
            return new UpdateAvailableEventArgs
            {
                Version = version,
                CurrentVersion = currentVersion,
                IsDownloaded = isDownloaded
            };
        }
    }
}
