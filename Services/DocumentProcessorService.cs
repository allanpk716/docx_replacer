using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Exceptions;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 文档处理服务实现
    /// </summary>
    public class DocumentProcessorService : IDocumentProcessor, IDisposable
    {
        private readonly ILogger<DocumentProcessorService> _logger;
        private readonly IDataParser _dataParser;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly ContentControlProcessor _contentControlProcessor;
        private readonly CommentManager _commentManager;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed = false;

        public event EventHandler<ProgressEventArgs>? ProgressUpdated;

        public DocumentProcessorService(
            ILogger<DocumentProcessorService> logger,
            IDataParser dataParser,
            IFileService fileService,
            IProgressReporter progressReporter,
            ContentControlProcessor contentControlProcessor,
            CommentManager commentManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _contentControlProcessor = contentControlProcessor ?? throw new ArgumentNullException(nameof(contentControlProcessor));
            _commentManager = commentManager ?? throw new ArgumentNullException(nameof(commentManager));

            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }

        public async Task<ProcessResult> ProcessDocumentsAsync(ProcessRequest request)
        {
            ThrowIfDisposed();

            ProcessResult result = new ProcessResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("开始批量处理文档");

                // 验证请求参数
                if (!request.IsValid())
                {
                    List<string> errors = request.GetValidationErrors();
                    result.Errors.AddRange(errors);
                    result.Message = "请求参数验证失败";
                    return result;
                }

                // 验证模板文件
                ValidationResult templateValidation = await ValidateTemplateAsync(request.TemplateFilePath);
                if (!templateValidation.IsValid)
                {
                    result.AddError($"模板文件验证失败: {templateValidation.ErrorMessage}");
                    return result;
                }

                // 解析数据文件
                List<Dictionary<string, object>> dataList = await _dataParser.ParseJsonFileAsync(request.DataFilePath);
                if (dataList == null || !dataList.Any())
                {
                    result.AddError("数据文件为空或解析失败");
                    return result;
                }

                result.TotalRecords = dataList.Count;
                _cancellationTokenSource = new CancellationTokenSource();

                // 确保输出目录存在
                try
                {
                    _ = _fileService.EnsureDirectoryExists(request.OutputDirectory);
                }
                catch (Exception ex)
                {
                    result.AddError($"无法创建输出目录: {request.OutputDirectory}, 错误: {ex.Message}");
                    return result;
                }

                // 使用并行处理批量处理文档
                await ProcessDocumentsInParallelAsync(request, dataList, result, _cancellationTokenSource.Token);

                _progressReporter.ReportCompleted(dataList.Count,
                    $"处理完成，成功: {result.SuccessfulRecords}，失败: {result.FailedRecords}");

                result.IsSuccess = result.SuccessfulRecords > 0;
                result.Message = $"批量处理完成，成功处理 {result.SuccessfulRecords} 个文档";

                _logger.LogInformation($"批量处理完成，成功: {result.SuccessfulRecords}，失败: {result.FailedRecords}");
            }
            catch (Exception ex)
            {
                result.AddError($"批量处理过程中发生异常: {ex.Message}");
                _logger.LogError(ex, "批量处理异常");
            }
            finally
            {
                result.EndTime = DateTime.Now;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            return result;
        }

        /// <summary>
        /// 并行处理文档
        /// </summary>
        private async Task ProcessDocumentsInParallelAsync(ProcessRequest request, List<Dictionary<string, object>> dataList, ProcessResult result, CancellationToken cancellationToken)
        {
            const int maxConcurrency = 4; // 限制并发数避免资源竞争
            SemaphoreSlim semaphore = new(maxConcurrency, maxConcurrency);

            try
            {
                var tasks = dataList.Select(async (data, index) =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string templateFileName = Path.GetFileNameWithoutExtension(request.TemplateFilePath);
                        string outputFileName = GenerateOutputFileNameWithTimestamp(templateFileName);
                        string outputPath = Path.Combine(request.OutputDirectory, outputFileName);

                        _logger.LogDebug($"开始处理第 {index + 1} 个文档: {outputFileName}");

                        // 更新进度
                        _progressReporter.ReportProgress(index + 1, dataList.Count,
                            $"正在处理第 {index + 1} 个文档", outputFileName);

                        bool success = await ProcessSingleDocumentAsync(request.TemplateFilePath, outputPath, data, cancellationToken);

                        lock (result) // 线程安全地更新结果
                        {
                            if (success)
                            {
                                result.SuccessfulRecords++;
                                result.AddGeneratedFile(outputPath);
                                _logger.LogDebug($"成功处理文档: {outputPath}");
                            }
                            else
                            {
                                result.AddError($"处理文档失败: {outputFileName}");
                            }
                        }

                        return (index, success, outputPath);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug($"处理第 {index + 1} 个文档被取消");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        lock (result)
                        {
                            string outputFileName = GenerateOutputFileNameWithTimestamp(Path.GetFileNameWithoutExtension(request.TemplateFilePath));
                            result.AddError($"处理文档时发生异常: {ex.Message}");
                        }
                        _logger.LogError(ex, $"处理文档异常");
                        return (index, false, string.Empty);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("批量处理被用户取消");
                result.AddWarning("处理被用户取消");
            }
            finally
            {
                semaphore.Dispose();
            }
        }

        public async Task<bool> ProcessSingleDocumentAsync(string templatePath, string outputPath,
            Dictionary<string, object> data, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 复制模板文件到输出路径
                _ = await _fileService.CopyFileAsync(templatePath, outputPath, true);

                cancellationToken.ThrowIfCancellationRequested();

                // 打开并处理文档
                using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, true);
                if (document.MainDocumentPart == null)
                {
                    _logger.LogError($"无法打开文档的主要部分: {outputPath}");
                    return false;
                }

                // 处理文档中的所有内容控件（包括页眉页脚）
                _contentControlProcessor.ProcessContentControlsInDocument(document, data, cancellationToken);

                // 保存文档
                document.MainDocumentPart.Document.Save();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理单个文档失败: {outputPath}");
                return false;
            }
        }

        public Task<ValidationResult> ValidateTemplateAsync(string templatePath)
        {
            ValidationResult result = new ValidationResult { IsValid = true };

            try
            {
                // 添加详细的调试信息
                _logger.LogInformation($"[调试] 开始验证模板文件");
                _logger.LogInformation($"[调试] 接收到的模板文件路径: '{templatePath}'");

                // 检查路径是否为空或null
                if (string.IsNullOrEmpty(templatePath))
                {
                    _logger.LogError($"[调试] 模板文件路径为空或null");
                    result.IsValid = false;
                    result.ErrorMessage = "模板文件路径为空";
                    return Task.FromResult(result);
                }

                // 输出绝对路径
                string absolutePath = Path.GetFullPath(templatePath);
                _logger.LogInformation($"[调试] 模板文件绝对路径: '{absolutePath}'");

                // 检查文件是否存在
                bool fileExists = _fileService.FileExists(templatePath);
                _logger.LogInformation($"[调试] 文件是否存在: {fileExists}");

                if (!fileExists)
                {
                    // 输出当前工作目录
                    string currentDirectory = Directory.GetCurrentDirectory();
                    _logger.LogError($"[调试] 当前工作目录: '{currentDirectory}'");

                    // 输出文件所在目录的内容（如果目录存在）
                    string? directoryPath = Path.GetDirectoryName(absolutePath);
                    if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                    {
                        _logger.LogError($"[调试] 文件所在目录: '{directoryPath}'");
                        string[] files = Directory.GetFiles(directoryPath);
                        _logger.LogError($"[调试] 目录中的文件数量: {files.Length}");
                        foreach (string? file in files.Take(10)) // 只显示前10个文件
                        {
                            _logger.LogError($"[调试] 目录中的文件: '{Path.GetFileName(file)}'");
                        }
                    }
                    else
                    {
                        _logger.LogError($"[调试] 文件所在目录不存在: '{directoryPath}'");
                    }

                    result.IsValid = false;
                    result.ErrorMessage = "模板文件不存在";
                    return Task.FromResult(result);
                }

                string extension = Path.GetExtension(templatePath).ToLowerInvariant();
                List<string> allowedExtensions = new List<string> { ".docx", ".dotx" };
                if (!allowedExtensions.Contains(extension))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"不支持的文件格式: {extension}，仅支持 .docx 和 .dotx 文件";
                    return Task.FromResult(result);
                }

                // 尝试打开文档验证格式
                using WordprocessingDocument document = WordprocessingDocument.Open(templatePath, false);
                if (document.MainDocumentPart == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "无效的Word文档格式";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"验证模板文件时发生异常: {ex.Message}";
            }

            return Task.FromResult(result);
        }

        public Task<List<ContentControlData>> GetContentControlsAsync(string templatePath)
        {
            List<ContentControlData> controls = new List<ContentControlData>();

            try
            {
                using WordprocessingDocument document = WordprocessingDocument.Open(templatePath, false);
                if (document.MainDocumentPart == null)
                {
                    return Task.FromResult(controls);
                }

                IEnumerable<SdtElement> contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>();
                foreach (SdtElement control in contentControls)
                {
                    SdtProperties? properties = control.SdtProperties;
                    string tag = properties?.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty;
                    string alias = properties?.GetFirstChild<SdtAlias>()?.Val?.Value ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        controls.Add(new ContentControlData
                        {
                            Tag = tag,
                            Title = alias,
                            Type = ContentControlType.Text
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取内容控件信息失败: {templatePath}");
            }

            return Task.FromResult(controls);
        }

        public void CancelProcessing()
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation("用户取消了文档处理操作");
        }

        private void OnProgressUpdated(object? sender, ProgressEventArgs e)
        {
            ProgressUpdated?.Invoke(this, e);
        }

        /// <summary>
        /// 生成带时间戳的输出文件名
        /// 格式：原文件名 -- 替换 --年月日时分秒.docx
        /// </summary>
        private string GenerateOutputFileNameWithTimestamp(string originalFileName)
        {
            string timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
            string outputFileName = $"{originalFileName} -- 替换 --{timestamp}.docx";
            _logger.LogDebug($"生成时间戳文件名: {outputFileName}");
            return outputFileName;
        }

        /// <summary>
        /// 批量处理文件夹中的模板文件
        /// </summary>
        /// <param name="request">文件夹处理请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        public async Task<ProcessResult> ProcessFolderAsync(FolderProcessRequest request, CancellationToken cancellationToken = default)
        {
            ProcessResult result = new ProcessResult { IsSuccess = true };
            List<string> processedFiles = new List<string>();
            List<string> failedFiles = new List<string>();

            try
            {
                _logger.LogInformation($"开始批量处理文件夹: {request.TemplateFolderPath}");
                _logger.LogInformation($"数据文件: {request.DataFilePath}");
                _logger.LogInformation($"输出目录: {request.OutputDirectory}");
                _logger.LogInformation($"模板文件数量: {request.TemplateFiles.Count}");

                // 验证输入参数
                if (request.TemplateFiles == null || !request.TemplateFiles.Any())
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "没有找到要处理的模板文件";
                    return result;
                }

                if (!_fileService.FileExists(request.DataFilePath))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "数据文件不存在";
                    return result;
                }

                // 解析数据文件
                List<Dictionary<string, object>> dataList = await _dataParser.ParseJsonFileAsync(request.DataFilePath);
                if (dataList == null || !dataList.Any())
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "数据文件为空或格式不正确";
                    return result;
                }

                // 创建时间戳输出文件夹
                string timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
                string inputFolderName = Path.GetFileName(request.TemplateFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string timestampFolderName = $"{inputFolderName}_{timestamp}";
                string timestampOutputDir = Path.Combine(request.OutputDirectory, timestampFolderName);

                if (!_fileService.DirectoryExists(timestampOutputDir))
                {
                    _ = Directory.CreateDirectory(timestampOutputDir);
                    _logger.LogInformation($"创建时间戳输出目录: {timestampOutputDir}");
                }

                int totalOperations = request.TemplateFiles.Count * dataList.Count();
                int currentOperation = 0;

                // 处理每个模板文件
                foreach (Models.FileInfo templateFile in request.TemplateFiles)
                {
                    try
                    {
                        _logger.LogInformation($"处理模板文件: {templateFile.Name}");

                        // 验证模板文件
                        ValidationResult validationResult = await ValidateTemplateAsync(templateFile.FullPath);
                        if (!validationResult.IsValid)
                        {
                            _logger.LogWarning($"模板文件验证失败: {templateFile.Name} - {validationResult.ErrorMessage}");
                            failedFiles.Add($"{templateFile.Name}: {validationResult.ErrorMessage}");
                            continue;
                        }

                        // 在时间戳文件夹中创建与原始结构对应的子目录
                        string relativeDir = string.IsNullOrEmpty(templateFile.RelativeDirectoryPath) ? "" : templateFile.RelativeDirectoryPath;
                        string outputSubDir = string.IsNullOrEmpty(relativeDir) ? timestampOutputDir : Path.Combine(timestampOutputDir, relativeDir);

                        if (!string.IsNullOrEmpty(relativeDir) && !_fileService.DirectoryExists(outputSubDir))
                        {
                            _ = Directory.CreateDirectory(outputSubDir);
                            _logger.LogDebug($"创建子目录: {outputSubDir}");
                        }

                        // 为每条数据生成文档
                        for (int i = 0; i < dataList.Count(); i++)
                        {
                            if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                            {
                                _logger.LogDebug("检测到取消请求，停止处理");
                                result.IsSuccess = false;
                                result.ErrorMessage = "操作已被取消";
                                return result;
                            }

                            if (_cancellationTokenSource == null)
                            {
                                _logger.LogDebug("警告：_cancellationTokenSource为null，无法检查取消状态");
                            }

                            Dictionary<string, object> data = dataList[i];
                            currentOperation++;

                            // 生成输出文件名 - 保持原始文件名
                            string outputFileName = templateFile.Name;
                            string outputPath = Path.Combine(outputSubDir, outputFileName);

                            _logger.LogDebug($"处理第 {i + 1} 条数据，输出到: {outputPath}");

                            // 处理单个文档
                            bool success = await ProcessSingleDocumentAsync(templateFile.FullPath, outputPath, data);

                            if (success)
                            {
                                processedFiles.Add(outputPath);
                                _logger.LogDebug($"✓ 成功处理: {outputFileName}");
                            }
                            else
                            {
                                failedFiles.Add($"{templateFile.Name} (第{i + 1}条数据): 处理失败");
                                _logger.LogWarning($"✗ 处理失败: {outputFileName}");
                            }

                            // 更新进度
                            double progress = (double)currentOperation / totalOperations * 100;
                            ProgressUpdated?.Invoke(this, new ProgressEventArgs
                            {
                                ProgressPercentage = (int)progress,
                                StatusMessage = $"正在处理 {templateFile.Name} (第{i + 1}/{dataList.Count()}条数据)",
                                CurrentFileName = templateFile.Name
                            });
                        }

                        _logger.LogInformation($"✓ 完成模板文件处理: {templateFile.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"处理模板文件时发生异常: {templateFile.Name}");
                        failedFiles.Add($"{templateFile.Name}: {ex.Message}");
                    }
                }

                // 设置结果
                result.ProcessedFiles = processedFiles;
                result.FailedFiles = failedFiles;
                result.OutputDirectory = timestampOutputDir;
                result.TotalProcessed = processedFiles.Count;
                result.TotalFailed = failedFiles.Count;

                if (failedFiles.Any())
                {
                    result.IsSuccess = processedFiles.Any(); // 如果有成功的文件，仍然算作部分成功
                    result.ErrorMessage = $"部分文件处理失败，成功: {processedFiles.Count}，失败: {failedFiles.Count}";
                }

                _logger.LogInformation($"批量处理完成 - 成功: {processedFiles.Count}，失败: {failedFiles.Count}");
                _logger.LogInformation($"输出目录: {timestampOutputDir}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量处理文件夹时发生异常: {ex.Message}");
                result.IsSuccess = false;
                result.ErrorMessage = $"批量处理失败: {ex.Message}";
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;

                    // 取消订阅事件
                    if (_progressReporter != null)
                    {
                        _progressReporter.ProgressUpdated -= OnProgressUpdated;
                    }
                }

                // 释放非托管资源
                _disposed = true;
            }
        }

        ~DocumentProcessorService()
        {
            Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DocumentProcessorService));
            }
        }
    }
}