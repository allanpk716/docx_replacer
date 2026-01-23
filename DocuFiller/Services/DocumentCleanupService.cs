using System.IO;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    public class DocumentCleanupService : IDocumentCleanupService
    {
        private readonly ILogger<DocumentCleanupService> _logger;
        private readonly CleanupCommentProcessor _commentProcessor;
        private readonly CleanupControlProcessor _controlProcessor;

        public event EventHandler<CleanupProgressEventArgs>? ProgressChanged;

        public DocumentCleanupService(
            ILogger<DocumentCleanupService> logger,
            CleanupCommentProcessor commentProcessor,
            CleanupControlProcessor controlProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commentProcessor = commentProcessor ?? throw new ArgumentNullException(nameof(commentProcessor));
            _controlProcessor = controlProcessor ?? throw new ArgumentNullException(nameof(controlProcessor));
        }

        public async Task<CleanupResult> CleanupAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await CleanupAsync(new CleanupFileItem { FilePath = filePath }, cancellationToken);
        }

        public async Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, CancellationToken cancellationToken = default)
        {
            if (fileItem == null)
                throw new ArgumentNullException(nameof(fileItem));

            _logger.LogInformation($"开始清理文档: {fileItem.FileName}");

            var result = new CleanupResult();

            try
            {
                // 验证文件存在
                if (!File.Exists(fileItem.FilePath))
                {
                    result.Success = false;
                    result.Message = "文件不存在";
                    return result;
                }

                // 验证文件格式
                if (!fileItem.FilePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Message = "不支持的文件格式，仅支持 .docx";
                    return result;
                }

                // 打开文档
                using var document = WordprocessingDocument.Open(fileItem.FilePath, true);

                if (document.MainDocumentPart == null)
                {
                    result.Success = false;
                    result.Message = "文档格式无效";
                    return result;
                }

                // 检查是否有批注或控件
                bool hasComments = document.MainDocumentPart.WordprocessingCommentsPart != null;
                bool hasControls = document.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.SdtElement>().Any();

                if (!hasComments && !hasControls)
                {
                    result.Success = true;
                    result.Message = "文档无需处理（无批注和内容控件）";
                    _logger.LogInformation($"文档 {fileItem.FileName} 无需处理");
                    return result;
                }

                // 处理批注
                if (hasComments)
                {
                    _logger.LogInformation($"开始清理文档 {fileItem.FileName} 的批注");
                    result.CommentsRemoved = _commentProcessor.ProcessComments(document);
                }

                // 处理内容控件
                if (hasControls)
                {
                    _logger.LogInformation($"开始解包文档 {fileItem.FileName} 的内容控件");
                    result.ControlsUnwrapped = _controlProcessor.ProcessControls(document);
                }

                // 保存文档
                document.MainDocumentPart.Document.Save();
                document.Close();

                result.Success = true;
                result.Message = $"清理完成：删除 {result.CommentsRemoved} 个批注，解包 {result.ControlsUnwrapped} 个控件";
                _logger.LogInformation($"文档 {fileItem.FileName} 清理完成: {result.Message}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"清理失败: {ex.Message}";
                _logger.LogError(ex, $"清理文档 {fileItem.FileName} 时发生异常");
            }

            return await Task.FromResult(result);
        }
    }
}