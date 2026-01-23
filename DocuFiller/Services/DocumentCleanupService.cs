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

#pragma warning disable CS0067 // 事件在接口中定义，为未来扩展预留
        public event EventHandler<CleanupProgressEventArgs>? ProgressChanged;
#pragma warning restore CS0067

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

        public async Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, string outputDirectory, CancellationToken cancellationToken = default)
        {
            if (fileItem == null)
                throw new ArgumentNullException(nameof(fileItem));

            if (string.IsNullOrEmpty(outputDirectory))
                throw new ArgumentException("输出目录不能为空", nameof(outputDirectory));

            _logger.LogInformation($"开始清理文档: {fileItem.FileName}，输出目录: {outputDirectory}");

            var result = new CleanupResult
            {
                FilePath = fileItem.FilePath,
                InputType = fileItem.InputType
            };

            try
            {
                // 验证输入文件/文件夹存在
                if (fileItem.InputType == InputSourceType.Folder)
                {
                    if (!Directory.Exists(fileItem.FilePath))
                    {
                        result.Success = false;
                        result.Message = "文件夹不存在";
                        return result;
                    }
                }
                else
                {
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
                }

                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);

                // 生成时间戳
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // 根据输入类型处理
                if (fileItem.InputType == InputSourceType.Folder)
                {
                    return await CleanupFolderAsync(fileItem, outputDirectory, timestamp, cancellationToken);
                }
                else
                {
                    return await CleanupSingleFileAsync(fileItem, outputDirectory, timestamp, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"清理失败: {ex.Message}";
                _logger.LogError(ex, $"清理文档 {fileItem.FileName} 时发生异常");
                return await Task.FromResult(result);
            }
        }

        private async Task<CleanupResult> CleanupSingleFileAsync(CleanupFileItem fileItem, string outputDirectory, string timestamp, CancellationToken cancellationToken)
        {
            var result = new CleanupResult
            {
                FilePath = fileItem.FilePath,
                InputType = InputSourceType.SingleFile
            };

            try
            {
                // 生成输出文件名：原文件名_cleaned_时间戳.docx
                string originalFileName = Path.GetFileNameWithoutExtension(fileItem.FileName);
                string outputFileName = $"{originalFileName}_cleaned_{timestamp}.docx";
                string outputPath = Path.Combine(outputDirectory, outputFileName);

                // 复制文件到输出目录
                File.Copy(fileItem.FilePath, outputPath, overwrite: true);
                _logger.LogInformation($"已复制文件到: {outputPath}");

                // 打开副本执行清理操作
                using var document = WordprocessingDocument.Open(outputPath, true);

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
                    result.OutputFilePath = outputPath;
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

                result.Success = true;
                result.Message = $"清理完成：删除 {result.CommentsRemoved} 个批注，解包 {result.ControlsUnwrapped} 个控件";
                result.OutputFilePath = outputPath;
                fileItem.OutputPath = outputPath;

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

        private async Task<CleanupResult> CleanupFolderAsync(CleanupFileItem fileItem, string outputDirectory, string timestamp, CancellationToken cancellationToken)
        {
            var result = new CleanupResult
            {
                FilePath = fileItem.FilePath,
                InputType = InputSourceType.Folder
            };

            try
            {
                string folderPath = fileItem.FilePath;
                string folderName = new DirectoryInfo(folderPath).Name;

                // 生成输出文件夹名：原文件夹名_cleaned_时间戳
                string outputFolderName = $"{folderName}_cleaned_{timestamp}";
                string outputFolderPath = Path.Combine(outputDirectory, outputFolderName);

                // 创建输出文件夹
                Directory.CreateDirectory(outputFolderPath);
                _logger.LogInformation($"创建输出文件夹: {outputFolderPath}");

                // 递归复制文件夹结构
                CopyDirectory(folderPath, outputFolderPath);

                // 查找所有 .docx 文件并清理
                var docxFiles = Directory.GetFiles(outputFolderPath, "*.docx", SearchOption.AllDirectories);
                int totalFiles = docxFiles.Length;
                int processedFiles = 0;
                int totalCommentsRemoved = 0;
                int totalControlsUnwrapped = 0;

                foreach (var docxFile in docxFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Message = $"处理已取消，已完成 {processedFiles}/{totalFiles} 个文件";
                        return result;
                    }

                    try
                    {
                        using var document = WordprocessingDocument.Open(docxFile, true);

                        if (document.MainDocumentPart == null)
                            continue;

                        // 检查是否有批注或控件
                        bool hasComments = document.MainDocumentPart.WordprocessingCommentsPart != null;
                        bool hasControls = document.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.SdtElement>().Any();

                        if (!hasComments && !hasControls)
                            continue;

                        // 处理批注
                        if (hasComments)
                        {
                            int commentsRemoved = _commentProcessor.ProcessComments(document);
                            totalCommentsRemoved += commentsRemoved;
                        }

                        // 处理内容控件
                        if (hasControls)
                        {
                            int controlsUnwrapped = _controlProcessor.ProcessControls(document);
                            totalControlsUnwrapped += controlsUnwrapped;
                        }

                        // 保存文档
                        document.MainDocumentPart.Document.Save();

                        processedFiles++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"清理文件 {docxFile} 时发生错误");
                    }
                }

                result.Success = true;
                result.OutputFolderPath = outputFolderPath;
                fileItem.OutputPath = outputFolderPath;

                if (processedFiles == 0)
                {
                    result.Message = "文件夹中的文档无需处理（无批注和内容控件）";
                }
                else
                {
                    result.CommentsRemoved = totalCommentsRemoved;
                    result.ControlsUnwrapped = totalControlsUnwrapped;
                    result.Message = $"清理完成：处理了 {processedFiles} 个文件，删除 {totalCommentsRemoved} 个批注，解包 {totalControlsUnwrapped} 个控件";
                }

                _logger.LogInformation($"文件夹 {folderName} 清理完成: {result.Message}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"清理失败: {ex.Message}";
                _logger.LogError(ex, $"清理文件夹 {fileItem.FileName} 时发生异常");
            }

            return await Task.FromResult(result);
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(directory, destDir);
            }
        }
    }
}