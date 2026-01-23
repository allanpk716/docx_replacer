using System;

namespace DocuFiller.Models
{
    /// <summary>
    /// 清理进度事件参数
    /// </summary>
    public class CleanupProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 已处理文件数
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// 成功处理的文件数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败的文件数
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 跳过的文件数
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 当前处理的文件名
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;

        /// <summary>
        /// 当前状态消息
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public int ProgressPercentage => TotalFiles > 0 ? (int)((double)ProcessedFiles / TotalFiles * 100) : 0;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => ProcessedFiles >= TotalFiles && TotalFiles > 0;
    }
}
