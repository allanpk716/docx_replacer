namespace DocuFiller.Models.Update
{
    /// <summary>
    /// 更新配置模型
    /// </summary>
    public class UpdateConfig
    {
        /// <summary>
        /// 更新服务器 URL
        /// </summary>
        public string ServerUrl { get; set; } = "http://192.168.1.100:8080";

        /// <summary>
        /// 发布渠道（stable/beta）
        /// </summary>
        public string Channel { get; set; } = "stable";

        /// <summary>
        /// 是否在启动时检查更新
        /// </summary>
        public bool CheckOnStartup { get; set; } = true;

        /// <summary>
        /// 是否自动下载更新
        /// </summary>
        public bool AutoDownload { get; set; } = true;

        /// <summary>
        /// 检查更新的间隔时间（小时）
        /// </summary>
        public int CheckIntervalHours { get; set; } = 24;

        /// <summary>
        /// 是否启用静默更新（后台下载，用户确认后安装）
        /// </summary>
        public bool SilentUpdate { get; set; } = true;

        /// <summary>
        /// 下载超时时间（秒）
        /// </summary>
        public int DownloadTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 临时文件目录
        /// </summary>
        public string TempDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置检查间隔时间（TimeSpan 格式）
        /// </summary>
        public System.TimeSpan CheckInterval => System.TimeSpan.FromHours(CheckIntervalHours);

        /// <summary>
        /// 获取默认的临时目录路径
        /// </summary>
        public string GetDefaultTempDirectory()
        {
            if (!string.IsNullOrEmpty(TempDirectory))
            {
                return TempDirectory;
            }

            var tempDir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DocuFiller",
                "Updates"
            );
            return tempDir;
        }
    }
}
