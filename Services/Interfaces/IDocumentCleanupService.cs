using System;
using System.Threading;
using System.Threading.Tasks;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 文档清理服务接口
    /// 用于去除程序生成的批注痕迹并将内容控件正常化
    /// </summary>
    public interface IDocumentCleanupService
    {
        /// <summary>
        /// 进度更新事件
        /// </summary>
        event EventHandler<CleanupProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// 清理单个文档（通过文件路径）
        /// </summary>
        /// <param name="filePath">文档文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理结果</returns>
        Task<CleanupResult> CleanupAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理单个文档（通过文件项）
        /// </summary>
        /// <param name="fileItem">清理文件项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理结果</returns>
        Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理单个文档并输出到指定目录
        /// </summary>
        /// <param name="fileItem">清理文件项</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理结果</returns>
        Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, string outputDirectory, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 清理结果模型
    /// </summary>
    public class CleanupResult
    {
        /// <summary>
        /// 是否清理成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 移除的批注数量
        /// </summary>
        public int CommentsRemoved { get; set; }

        /// <summary>
        /// 解除的内容控件数量
        /// </summary>
        public int ControlsUnwrapped { get; set; }

        /// <summary>
        /// 处理的文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 输入类型（单文件或文件夹）
        /// </summary>
        public InputSourceType InputType { get; set; }

        /// <summary>
        /// 单文件模式：输出文件的完整路径
        /// </summary>
        public string OutputFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件夹模式：输出文件夹的完整路径
        /// </summary>
        public string OutputFolderPath { get; set; } = string.Empty;
    }
}
