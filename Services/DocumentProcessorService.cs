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
                    
                    // 生成输出文件名 - 使用新的时间戳格式
                    var outputFileName = GenerateOutputFileNameWithTimestamp(templateFileName);
                    var outputPath = Path.Combine(request.OutputDirectory, outputFileName);
                    _logger.LogDebug($"生成输出文件名: {outputFileName}");

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
                    ProcessContentControl(control, data, document);
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

        private void ProcessContentControl(SdtElement control, Dictionary<string, object> data, WordprocessingDocument document)
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
                _logger.LogDebug($"原始值包含换行符: {value.Contains("\n")}");
                
                // 记录替换前的旧值用于批注
                var oldValue = string.Empty;
                var existingTextElements = control.Descendants<Text>().ToList();
                if (existingTextElements.Any())
                {
                    oldValue = string.Join("", existingTextElements.Select(t => t.Text));
                }
                
                // 处理标记功能已集成到批注中，无需单独添加
                
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
                        // 清除现有文本元素
                        var parentRuns = textElements.Select(t => t.Parent).OfType<Run>().Distinct().ToList();
                        foreach (var run in parentRuns)
                        {
                            run.RemoveAllChildren();
                        }
                        
                        // 添加格式化的新内容
                        if (parentRuns.Any())
                        {
                            var firstRun = parentRuns.First();
                            var formattedElements = CreateFormattedTextElements(value);
                            foreach (var element in formattedElements)
                            {
                                firstRun.AppendChild(element);
                            }
                            
                            // 移除其他多余的Run元素
                            for (int i = 1; i < parentRuns.Count; i++)
                            {
                                parentRuns[i].Remove();
                            }
                            
                            // 为替换成功的Run添加批注
                            var currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
                            var commentText = $"此字段已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{value}";
                            AddCommentToElement(document, firstRun, commentText, "DocuFiller系统", tag);
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
                    Run targetRun = null;
                    if (control is SdtBlock || content is SdtContentBlock || content is SdtContentCell)
                    {
                        var paragraph = CreateParagraphWithFormattedText(value);
                        content.AppendChild(paragraph);
                        targetRun = paragraph.Descendants<Run>().FirstOrDefault();
                        _logger.LogDebug($"作为块级元素添加内容: '{value}'");
                    }
                    else if (control is SdtRun || content is SdtContentRun)
                    {
                        var runs = CreateFormattedRuns(value);
                        foreach (var run in runs)
                        {
                            content.AppendChild(run);
                        }
                        targetRun = runs.FirstOrDefault();
                        _logger.LogDebug($"作为行内元素添加内容: '{value}'");
                    }
                    else
                    {
                        // 通用处理方式
                        var runs = CreateFormattedRuns(value);
                        foreach (var run in runs)
                        {
                            content.AppendChild(run);
                        }
                        targetRun = runs.FirstOrDefault();
                        _logger.LogDebug($"使用通用方式添加内容: '{value}'");
                    }
                    
                    // 为替换成功的Run添加批注
                    if (targetRun != null)
                    {
                        var currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
                        var commentText = $"此字段已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{value}";
                        AddCommentToElement(document, targetRun, commentText, "DocuFiller系统", tag);
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

        public Task<ValidationResult> ValidateTemplateAsync(string templatePath)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                if (!_fileService.FileExists(templatePath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "模板文件不存在";
                    return Task.FromResult(result);
                }

                var extension = Path.GetExtension(templatePath).ToLowerInvariant();
                var allowedExtensions = new List<string> { ".docx", ".dotx" };
                if (!allowedExtensions.Contains(extension))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"不支持的文件格式: {extension}，仅支持 .docx 和 .dotx 文件";
                    return Task.FromResult(result);
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

            return Task.FromResult(result);
        }

        public Task<List<ContentControlData>> GetContentControlsAsync(string templatePath)
        {
            var controls = new List<ContentControlData>();

            try
            {
                using var document = WordprocessingDocument.Open(templatePath, false);
                if (document.MainDocumentPart == null)
                    return Task.FromResult(controls);

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
        /// 创建带格式的段落，支持换行符和红色文本
        /// </summary>
        private Paragraph CreateParagraphWithFormattedText(string text)
        {
            var paragraph = new Paragraph();
            var runs = CreateFormattedRuns(text);
            foreach (var run in runs)
            {
                paragraph.AppendChild(run);
            }
            return paragraph;
        }

        /// <summary>
        /// 创建格式化的Run元素列表，处理换行符并设置红色
        /// </summary>
        private List<Run> CreateFormattedRuns(string text)
        {
            var runs = new List<Run>();
            
            if (string.IsNullOrEmpty(text))
            {
                return runs;
            }

            // 按换行符分割文本
            var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // 创建带红色格式的Run
                var run = new Run();
                
                // 设置文本颜色为红色
                var runProperties = new RunProperties();
                var color = new Color() { Val = "FF0000" }; // 红色
                runProperties.AppendChild(color);
                run.AppendChild(runProperties);
                
                // 添加文本内容
                var text_element = new Text(line);
                run.AppendChild(text_element);
                runs.Add(run);
                
                // 如果不是最后一行，添加换行符
                if (i < lines.Length - 1)
                {
                    var breakRun = new Run(new Break());
                    runs.Add(breakRun);
                    _logger.LogDebug($"添加换行符在第 {i + 1} 行后");
                }
            }
            
            _logger.LogDebug($"创建了 {runs.Count} 个Run元素");
            return runs;
        }

        /// <summary>
        /// 创建格式化的文本元素列表，用于直接替换Text元素
        /// </summary>
        private List<OpenXmlElement> CreateFormattedTextElements(string text)
        {
            var elements = new List<OpenXmlElement>();
            
            if (string.IsNullOrEmpty(text))
            {
                return elements;
            }

            // 按换行符分割文本
            var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // 设置文本颜色为红色
                var runProperties = new RunProperties();
                var color = new Color() { Val = "FF0000" }; // 红色
                runProperties.AppendChild(color);
                elements.Add(runProperties);
                
                // 添加文本内容
                var textElement = new Text(line);
                elements.Add(textElement);
                
                // 如果不是最后一行，添加换行符
                if (i < lines.Length - 1)
                {
                    elements.Add(new Break());
                    _logger.LogDebug($"添加换行符在第 {i + 1} 行后");
                }
            }
            
            _logger.LogDebug($"创建了 {elements.Count} 个文本元素");
            return elements;
        }

        /// <summary>
        /// 生成带时间戳的输出文件名
        /// 格式：原文件名 -- 替换 --年月日时分秒.docx
        /// </summary>
        private string GenerateOutputFileNameWithTimestamp(string originalFileName)
        {
            var timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
            var outputFileName = $"{originalFileName} -- 替换 --{timestamp}.docx";
            _logger.LogDebug($"生成时间戳文件名: {outputFileName}");
            return outputFileName;
        }

        /// <summary>
        /// 为Run元素添加批注（按照OpenXML标准实现）
        /// </summary>
        private void AddCommentToElement(WordprocessingDocument document, Run targetRun, string commentText, string author, string tag)
        {
            try
            {
                _logger.LogDebug($"开始为Run元素添加批注，标签: '{tag}'");
                
                // 1. 准备批注环境 (Get or Create comments part)
                WordprocessingCommentsPart commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
                if (commentsPart == null)
                {
                    _logger.LogDebug("创建新的批注部分");
                    commentsPart = document.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
                    commentsPart.Comments = new Comments();
                }
                
                // 2. 生成唯一ID (Find the next available ID)
                string id = "0";
                if (commentsPart.Comments.HasChildren)
                {
                    // ID必须是唯一的数字字符串。我们找到当前最大的ID并加1。
                    var maxId = commentsPart.Comments.Descendants<Comment>()
                        .Select(c => int.TryParse(c.Id?.Value, out var commentId) ? commentId : 0)
                        .DefaultIfEmpty(0)
                        .Max();
                    id = (maxId + 1).ToString();
                }
                else
                {
                    id = "1";
                }
                _logger.LogDebug($"生成批注ID: {id}");
                
                // 3. 创建批注内容
                Paragraph p = new Paragraph(new Run(new Text(commentText)));
                Comment comment = new Comment()
                {
                    Id = id,
                    Author = author,
                    Date = DateTime.Now,
                    Initials = author.Length >= 2 ? author.Substring(0, 2) : author // 可选
                };
                comment.Append(p);
                
                // 将新批注添加到 comments.xml
                commentsPart.Comments.Append(comment);
                commentsPart.Comments.Save();
                _logger.LogDebug($"批注已添加到comments.xml，ID: {id}");
                
                // 4 & 5. 在正文中标记范围和添加引用
                // 在被批注的元素前后插入范围标记
                targetRun.InsertBeforeSelf(new CommentRangeStart() { Id = id });
                targetRun.InsertAfterSelf(new CommentRangeEnd() { Id = id });
                
                // 在被批注的元素（Run）中添加引用标记
                targetRun.Append(new CommentReference() { Id = id });
                
                _logger.LogInformation($"✓ 成功为Run元素添加批注，标签: '{tag}'，ID: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"为Run元素添加批注时发生异常，标签: '{tag}': {ex.Message}");
            }
        }

        /// <summary>
        /// 生成唯一的批注ID（已整合到AddCommentToElement方法中）
        /// </summary>
        private int GenerateCommentId(Comments comments)
        {
            var existingIds = comments.Elements<Comment>()
                .Select(c => int.TryParse(c.Id?.Value, out var id) ? id : 0)
                .ToList();
            
            return existingIds.Any() ? existingIds.Max() + 1 : 1;
        }

        // 旧的批注方法已被AddCommentToElement替代，此方法已删除

        // AddProcessingMarkToControl方法已删除，处理标记功能已集成到批注中
     }
 }