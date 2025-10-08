using System;
using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Utils;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 文档处理服务接口
    /// </summary>
    public interface IDocumentProcessor
    {
        /// <summary>
        /// 进度更新事件
        /// </summary>
        event EventHandler<ProgressEventArgs>? ProgressUpdated;

        /// <summary>
        /// 批量处理文档
        /// </summary>
        /// <param name="request">处理请求</param>
        /// <returns>处理结果</returns>
        Task<ProcessResult> ProcessDocumentsAsync(ProcessRequest request);

        /// <summary>
        /// 处理单个文档
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="data">填充数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> ProcessSingleDocumentAsync(string templatePath, string outputPath,
            System.Collections.Generic.Dictionary<string, object> data,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证模板文件
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateTemplateAsync(string templatePath);

        /// <summary>
        /// 获取模板中的内容控件信息
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <returns>内容控件列表</returns>
        Task<System.Collections.Generic.List<ContentControlData>> GetContentControlsAsync(string templatePath);

        /// <summary>
        /// 批量处理文件夹中的模板文件
        /// </summary>
        /// <param name="request">文件夹处理请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<ProcessResult> ProcessFolderAsync(FolderProcessRequest request, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消当前处理操作
        /// </summary>
        void CancelProcessing();
    }
}