using System;

namespace DocuFiller.Models
{
    /// <summary>
    /// 进度事件参数
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 当前进度百分比 (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// 当前处理的项目索引
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// 总项目数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前状态消息
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// 当前处理的文件名
        /// </summary>
        public string CurrentFileName { get; set; } = string.Empty;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProgressEventArgs()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="currentIndex">当前索引</param>
        /// <param name="totalCount">总数</param>
        /// <param name="statusMessage">状态消息</param>
        public ProgressEventArgs(int currentIndex, int totalCount, string statusMessage = "")
        {
            CurrentIndex = currentIndex;
            TotalCount = totalCount;
            StatusMessage = statusMessage;
            ProgressPercentage = totalCount > 0 ? (int)((double)currentIndex / totalCount * 100) : 0;
        }

        /// <summary>
        /// 创建完成状态的进度事件
        /// </summary>
        /// <param name="totalCount">总数</param>
        /// <param name="message">完成消息</param>
        /// <returns>进度事件参数</returns>
        public static ProgressEventArgs CreateCompleted(int totalCount, string message = "处理完成")
        {
            return new ProgressEventArgs
            {
                CurrentIndex = totalCount,
                TotalCount = totalCount,
                ProgressPercentage = 100,
                StatusMessage = message,
                IsCompleted = true
            };
        }

        /// <summary>
        /// 创建错误状态的进度事件
        /// </summary>
        /// <param name="currentIndex">当前索引</param>
        /// <param name="totalCount">总数</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>进度事件参数</returns>
        public static ProgressEventArgs CreateError(int currentIndex, int totalCount, string errorMessage)
        {
            return new ProgressEventArgs(currentIndex, totalCount)
            {
                HasError = true,
                ErrorMessage = errorMessage,
                StatusMessage = "处理出错"
            };
        }
    }
}