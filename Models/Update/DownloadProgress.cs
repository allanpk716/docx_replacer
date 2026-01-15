namespace DocuFiller.Models.Update
{
    /// <summary>
    /// 下载进度模型
    /// </summary>
    public class DownloadProgress
    {
        /// <summary>
        /// 已接收的字节数
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// 当前状态描述
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 下载速度（字节/秒）
        /// </summary>
        public double DownloadSpeed { get; set; }

        /// <summary>
        /// 剩余时间估算（秒）
        /// </summary>
        public long? RemainingSeconds { get; set; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => ProgressPercentage >= 100;

        /// <summary>
        /// 获取格式化的已接收字节数
        /// </summary>
        public string FormattedBytesReceived => FormatBytes(BytesReceived);

        /// <summary>
        /// 获取格式化的总字节数
        /// </summary>
        public string FormattedTotalBytes => FormatBytes(TotalBytes);

        /// <summary>
        /// 获取格式化的下载速度
        /// </summary>
        public string FormattedDownloadSpeed => FormatBytes((long)DownloadSpeed) + "/s";

        /// <summary>
        /// 获取格式化的剩余时间
        /// </summary>
        public string FormattedRemainingTime
        {
            get
            {
                if (!RemainingSeconds.HasValue || RemainingSeconds.Value < 0)
                {
                    return "计算中...";
                }

                var seconds = RemainingSeconds.Value;
                if (seconds < 60)
                {
                    return $"{seconds} 秒";
                }
                else if (seconds < 3600)
                {
                    var minutes = seconds / 60;
                    return $"{minutes} 分钟";
                }
                else
                {
                    var hours = seconds / 3600;
                    var minutes = (seconds % 3600) / 60;
                    return $"{hours} 小时 {minutes} 分钟";
                }
            }
        }

        /// <summary>
        /// 格式化字节数
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的字符串</returns>
        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = 1024 * 1024;
            const long GB = 1024 * 1024 * 1024;

            if (bytes >= GB)
            {
                return $"{bytes / (double)GB:F2} GB";
            }
            else if (bytes >= MB)
            {
                return $"{bytes / (double)MB:F2} MB";
            }
            else if (bytes >= KB)
            {
                return $"{bytes / (double)KB:F2} KB";
            }
            else
            {
                return $"{bytes} B";
            }
        }

        /// <summary>
        /// 创建初始下载进度
        /// </summary>
        /// <param name="totalBytes">总字节数</param>
        /// <returns>下载进度实例</returns>
        public static DownloadProgress CreateInitial(long totalBytes)
        {
            return new DownloadProgress
            {
                BytesReceived = 0,
                TotalBytes = totalBytes,
                ProgressPercentage = 0,
                Status = "准备下载..."
            };
        }

        /// <summary>
        /// 创建完成的下载进度
        /// </summary>
        /// <returns>下载进度实例</returns>
        public static DownloadProgress CreateCompleted()
        {
            return new DownloadProgress
            {
                BytesReceived = 0,
                TotalBytes = 0,
                ProgressPercentage = 100,
                Status = "下载完成"
            };
        }
    }
}
