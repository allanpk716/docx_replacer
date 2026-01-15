using System;

namespace DocuFiller.Models.Update
{
    /// <summary>
    /// 版本信息模型
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// 版本号（如 1.2.0）
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 发布渠道（stable/beta）
        /// </summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件 SHA256 哈希值
        /// </summary>
        public string FileHash { get; set; } = string.Empty;

        /// <summary>
        /// 发布说明
        /// </summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>
        /// 发布日期
        /// </summary>
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// 是否为强制更新
        /// </summary>
        public bool Mandatory { get; set; }

        /// <summary>
        /// 是否已下载
        /// </summary>
        public bool IsDownloaded { get; set; }

        /// <summary>
        /// 下载 URL（完整路径）
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 获取格式化的文件大小
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                const long KB = 1024;
                const long MB = 1024 * 1024;
                const long GB = 1024 * 1024 * 1024;

                if (FileSize >= GB)
                {
                    return $"{FileSize / (double)GB:F2} GB";
                }
                else if (FileSize >= MB)
                {
                    return $"{FileSize / (double)MB:F2} MB";
                }
                else if (FileSize >= KB)
                {
                    return $"{FileSize / (double)KB:F2} KB";
                }
                else
                {
                    return $"{FileSize} B";
                }
            }
        }

        /// <summary>
        /// 获取发布日期的友好显示格式
        /// </summary>
        public string FormattedPublishDate => PublishDate.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// 比较版本号是否大于指定版本
        /// </summary>
        /// <param name="currentVersion">当前版本号</param>
        /// <returns>是否需要更新</returns>
        public bool IsNewerThan(string currentVersion)
        {
            try
            {
                var current = System.Version.Parse(currentVersion);
                var latest = System.Version.Parse(this.Version);
                return latest > current;
            }
            catch
            {
                // 如果版本号格式不正确，使用字符串比较
                return string.Compare(this.Version, currentVersion, StringComparison.Ordinal) > 0;
            }
        }
    }
}
