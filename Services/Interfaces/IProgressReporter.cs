using System;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 进度报告服务接口
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// 进度更新事件
        /// </summary>
        event EventHandler<ProgressEventArgs>? ProgressUpdated;

        /// <summary>
        /// 报告进度
        /// </summary>
        /// <param name="currentIndex">当前索引</param>
        /// <param name="totalCount">总数</param>
        /// <param name="statusMessage">状态消息</param>
        /// <param name="currentFileName">当前文件名</param>
        void ReportProgress(int currentIndex, int totalCount, string statusMessage = "", string currentFileName = "");

        /// <summary>
        /// 报告完成
        /// </summary>
        /// <param name="totalCount">总数</param>
        /// <param name="message">完成消息</param>
        void ReportCompleted(int totalCount, string message = "处理完成");

        /// <summary>
        /// 报告错误
        /// </summary>
        /// <param name="currentIndex">当前索引</param>
        /// <param name="totalCount">总数</param>
        /// <param name="errorMessage">错误消息</param>
        void ReportError(int currentIndex, int totalCount, string errorMessage);

        /// <summary>
        /// 重置进度
        /// </summary>
        void Reset();

        /// <summary>
        /// 获取当前进度百分比
        /// </summary>
        /// <returns>进度百分比</returns>
        int GetCurrentProgress();

        /// <summary>
        /// 是否已完成
        /// </summary>
        /// <returns>是否完成</returns>
        bool IsCompleted();

        /// <summary>
        /// 是否有错误
        /// </summary>
        /// <returns>是否有错误</returns>
        bool HasError();
    }
}