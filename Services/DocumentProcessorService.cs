using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DocuFiller.Services
{
    /// <summary>
    /// 文档处理服务实现
    /// </summary>
    public class DocumentProcessorService : IDocumentProcessor
    {
        private readonly ILogger<DocumentProcessorService> _logger;
        private readonly IDataParser _dataParser;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<ProgressEventArgs>? ProgressUpdated;

        public DocumentProcessorService(
            ILogger<DocumentProcessorService> logger,
            IDataParser dataParser,
            IFileService fileService,
            IProgressReporter progressReporter)
        {
            _logger = logger;
            _dataParser = dataParser;
            _fileService = fileService;
            _progressReporter = progressReporter;
            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }

        public async Task<ProcessResult> ProcessDocumentsAsync(ProcessRequest request)
        {
            var result = new ProcessResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("开始批量处理文档");

                // 验证请求参数
                if (!request.IsValid())
                {
                    var errors = request.GetValidationErrors();
                    result.Errors.AddRange(errors);
                    result.Message = "请求参数验证失败";
                    return result;
                }

                // 验证模板文件
                var templateValidation = await ValidateTemplateAsync(request.TemplateFilePath);
                if (!templateValidation.IsValid)
                {
                    result.AddError($"模板文件验证失败: {templateValidation.ErrorMessage}");
                    return result;
                }

                // 解析数据文件
                var dataList = await _dataParser.ParseJsonFileAsync(request.DataFilePath);
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
                    _fileService.EnsureDirectoryExists(request.OutputDirectory);
                }
                catch (Exception ex)
                {
                    result.AddError($"无法创建输出目录: {request.OutputDirectory}, 错误: {ex.Message}");
                    return result;
                }

                // 批量处理文档
                for (int i = 0; i < dataList.Count; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        result.AddWarning("处理被用户取消");
                        break;
                    }

                    var data = dataList[i];
                    var templateFileName = Path.GetFileNameWithoutExtension(request.TemplateFilePath);
                    var templateExtension = Path.GetExtension(request.TemplateFilePath);
                    var outputFileName = $"{templateFileName}_{i + 1:D3}{templateExtension}";
                    
                    if (!string.IsNullOrWhiteSpace(request.OutputFileNamePattern))
                    {
                        outputFileName = request.OutputFileNamePattern.Replace("{index}", (i + 1).ToString("D3")) + templateExtension;
                    }

                    var outputPath = Path.Combine(request.OutputDirectory, outputFileName);

                    _progressReporter.ReportProgress(i + 1, dataList.Count, 
                        $"正在处理第 {i + 1} 个文档", outputFileName);

                    try
                    {
                        var success = await ProcessSingleDocumentAsync(
                            request.TemplateFilePath, outputPath, data);

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
                    catch (Exception ex)
                    {
                        result.AddError($"处理文档 {outputFileName} 时发生异常: {ex.Message}");
                        _logger.LogError(ex, $"处理文档异常: {outputFileName}");
                    }
                }

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

        public async Task<bool> ProcessSingleDocumentAsync(string templatePath, string outputPath, 
            Dictionary<string, object> data)
        {
            try
            {
                // 复制模板文件到输出路径
                await _fileService.CopyFileAsync(templatePath, outputPath, true);

                // 打开并处理文档
                using var document = WordprocessingDocument.Open(outputPath, true);
                if (document.MainDocumentPart == null)
                {
                    _logger.LogError($"无法打开文档的主要部分: {outputPath}");
                    return false;
                }

                // 处理内容控件
                var contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>().ToList();
                foreach (var control in contentControls)
                {
                    ProcessContentControl(control, data);
                }

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

        private void ProcessContentControl(SdtElement control, Dictionary<string, object> data)
        {
            try
            {
                var properties = control.SdtProperties;
                var tag = properties?.GetFirstChild<Tag>()?.Val?.Value;
                
                _logger.LogDebug($"处理内容控件 - 标签: '{tag}'");

                if (string.IsNullOrWhiteSpace(tag))
                {
                    _logger.LogWarning("内容控件标签为空，跳过处理");
                    return;
                }
                
                if (!data.ContainsKey(tag))
                {
                    _logger.LogWarning($"数据中未找到标签 '{tag}' 对应的值，跳过处理");
                    return;
                }

                var value = data[tag]?.ToString() ?? string.Empty;
                _logger.LogDebug($"找到匹配数据: '{tag}' -> '{value}'");
                
                // 尝试多种方式查找内容容器
                var content = control.Descendants<SdtContentRun>().FirstOrDefault() ?? 
                             control.Descendants<SdtContentBlock>().FirstOrDefault() as OpenXmlElement;
                
                // 如果没找到标准内容容器，尝试查找其他可能的容器
                if (content == null)
                {
                    content = control.Descendants<SdtContentCell>().FirstOrDefault();
                    _logger.LogDebug("尝试查找 SdtContentCell 容器");
                }
                
                if (content == null)
                {
                    // 尝试直接查找文本容器
                    var textElements = control.Descendants<Text>().ToList();
                    if (textElements.Any())
                    {
                        _logger.LogDebug($"找到 {textElements.Count} 个文本元素，直接替换文本内容");
                        foreach (var textElement in textElements)
                        {
                            textElement.Text = value;
                        }
                        _logger.LogInformation($"✓ 成功替换内容控件 '{tag}' 的文本为 '{value}'");
                        return;
                    }
                }

                if (content != null)
                {
                    _logger.LogDebug($"找到内容容器，类型: {content.GetType().Name}");
                    
                    // 清除现有内容
                    var childCount = content.ChildElements.Count;
                    content.RemoveAllChildren();
                    _logger.LogDebug($"清除了 {childCount} 个子元素");

                    // 添加新内容
                    if (control is SdtBlock || content is SdtContentBlock || content is SdtContentCell)
                    {
                        var paragraph = new Paragraph(new Run(new Text(value)));
                        content.AppendChild(paragraph);
                        _logger.LogDebug($"作为块级元素添加内容: '{value}'");
                    }
                    else if (control is SdtRun || content is SdtContentRun)
                    {
                        var run = new Run(new Text(value));
                        content.AppendChild(run);
                        _logger.LogDebug($"作为行内元素添加内容: '{value}'");
                    }
                    else
                    {
                        // 通用处理方式
                        var run = new Run(new Text(value));
                        content.AppendChild(run);
                        _logger.LogDebug($"使用通用方式添加内容: '{value}'");
                    }
                    
                    _logger.LogInformation($"✓ 成功替换内容控件 '{tag}' 为 '{value}'");
                }
                else
                {
                    _logger.LogError($"未找到内容控件 '{tag}' 的任何内容容器");
                    
                    // 输出控件结构信息用于调试
                    _logger.LogDebug($"内容控件结构调试信息:");
                    _logger.LogDebug($"  控件类型: {control.GetType().Name}");
                    _logger.LogDebug($"  子元素数量: {control.ChildElements.Count}");
                    foreach (var child in control.ChildElements)
                    {
                        _logger.LogDebug($"    子元素: {child.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理内容控件时发生异常: {ex.Message}");
            }
        }

        public async Task<ValidationResult> ValidateTemplateAsync(string templatePath)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                if (!_fileService.FileExists(templatePath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "模板文件不存在";
                    return result;
                }

                var extension = Path.GetExtension(templatePath).ToLowerInvariant();
                var allowedExtensions = new List<string> { ".docx", ".dotx" };
                if (!allowedExtensions.Contains(extension))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"不支持的文件格式: {extension}，仅支持 .docx 和 .dotx 文件";
                    return result;
                }

                // 尝试打开文档验证格式
                using var document = WordprocessingDocument.Open(templatePath, false);
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

            return result;
        }

        public async Task<List<ContentControlData>> GetContentControlsAsync(string templatePath)
        {
            var controls = new List<ContentControlData>();

            try
            {
                using var document = WordprocessingDocument.Open(templatePath, false);
                if (document.MainDocumentPart == null)
                    return controls;

                var contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>();
                foreach (var control in contentControls)
                {
                    var properties = control.SdtProperties;
                    var tag = properties?.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty;
                    var alias = properties?.GetFirstChild<SdtAlias>()?.Val?.Value ?? string.Empty;

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

            return controls;
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
    }
}