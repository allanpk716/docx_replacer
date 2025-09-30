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

                // 批量处理文档
                for (int i = 0; i < dataList.Count; i++)
                {
                    if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        _logger.LogDebug("检测到取消请求，停止批量处理");
                        result.AddWarning("处理被用户取消");
                        break;
                    }

                    if (_cancellationTokenSource == null)
                    {
                        _logger.LogDebug("警告：_cancellationTokenSource为null，无法检查取消状态");
                    }

                    Dictionary<string, object> data = dataList[i];
                    string templateFileName = Path.GetFileNameWithoutExtension(request.TemplateFilePath);
                    string templateExtension = Path.GetExtension(request.TemplateFilePath);

                    // 生成输出文件名 - 使用新的时间戳格式
                    string outputFileName = GenerateOutputFileNameWithTimestamp(templateFileName);
                    string outputPath = Path.Combine(request.OutputDirectory, outputFileName);
                    _logger.LogDebug($"生成输出文件名: {outputFileName}");

                    _progressReporter.ReportProgress(i + 1, dataList.Count,
                        $"正在处理第 {i + 1} 个文档", outputFileName);

                    try
                    {
                        bool success = await ProcessSingleDocumentAsync(
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
                _ = await _fileService.CopyFileAsync(templatePath, outputPath, true);

                // 打开并处理文档
                using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, true);
                if (document.MainDocumentPart == null)
                {
                    _logger.LogError($"无法打开文档的主要部分: {outputPath}");
                    return false;
                }

                // 处理内容控件
                List<SdtElement> contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>().ToList();
                foreach (SdtElement? control in contentControls)
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
                SdtProperties? properties = control.SdtProperties;
                string? tag = properties?.GetFirstChild<Tag>()?.Val?.Value;

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

                string value = data[tag]?.ToString() ?? string.Empty;
                _logger.LogDebug($"找到匹配数据: '{tag}' -> '{value}'");
                _logger.LogDebug($"原始值包含换行符: {value.Contains("\n")}");

                // 记录替换前的旧值用于批注
                string oldValue = string.Empty;
                List<Text> existingTextElements = control.Descendants<Text>().ToList();
                if (existingTextElements.Any())
                {
                    oldValue = string.Join("", existingTextElements.Select(static t => t.Text));
                }

                // 处理标记功能已集成到批注中，无需单独添加

                // 尝试多种方式查找内容容器
                OpenXmlElement? content = control.Descendants<SdtContentRun>().FirstOrDefault() ??
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
                    List<Text> textElements = control.Descendants<Text>().ToList();
                    if (textElements.Any())
                    {
                        _logger.LogDebug($"找到 {textElements.Count} 个文本元素，直接替换文本内容");
                        // 清除现有文本元素
                        List<Run> parentRuns = textElements.Select(static t => t.Parent).OfType<Run>().Distinct().ToList();
                        foreach (Run? run in parentRuns)
                        {
                            run.RemoveAllChildren();
                        }

                        // 添加格式化的新内容
                        if (parentRuns.Any())
                        {
                            Run firstRun = parentRuns.First();
                            List<OpenXmlElement> formattedElements = CreateFormattedTextElements(value);
                            foreach (OpenXmlElement element in formattedElements)
                            {
                                _ = firstRun.AppendChild(element);
                            }

                            // 移除其他多余的Run元素
                            for (int i = 1; i < parentRuns.Count; i++)
                            {
                                parentRuns[i].Remove();
                            }

                            // 为替换成功的Run添加批注
                            string currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
                            string commentText = $"此字段已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{value}";
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
                    int childCount = content.ChildElements.Count;
                    content.RemoveAllChildren();
                    _logger.LogDebug($"清除了 {childCount} 个子元素");

                    // 添加新内容
                    Run? targetRun = null;
                    if (control is SdtBlock || content is SdtContentBlock || content is SdtContentCell)
                    {
                        Paragraph paragraph = CreateParagraphWithFormattedText(value);
                        _ = content.AppendChild(paragraph);
                        targetRun = paragraph.Descendants<Run>().FirstOrDefault();
                        _logger.LogDebug($"作为块级元素添加内容: '{value}'");
                    }
                    else if (control is SdtRun || content is SdtContentRun)
                    {
                        List<Run> runs = CreateFormattedRuns(value);
                        foreach (Run run in runs)
                        {
                            _ = content.AppendChild(run);
                        }
                        targetRun = runs.FirstOrDefault();
                        _logger.LogDebug($"作为行内元素添加内容: '{value}'");
                    }
                    else
                    {
                        // 通用处理方式
                        List<Run> runs = CreateFormattedRuns(value);
                        foreach (Run run in runs)
                        {
                            _ = content.AppendChild(run);
                        }
                        targetRun = runs.FirstOrDefault();
                        _logger.LogDebug($"使用通用方式添加内容: '{value}'");
                    }

                    // 为替换成功的Run添加批注
                    if (targetRun != null)
                    {
                        string currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
                        string commentText = $"此字段已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{value}";
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
                    foreach (OpenXmlElement child in control.ChildElements)
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
        /// 创建带格式的段落，支持换行符和红色文本
        /// </summary>
        private Paragraph CreateParagraphWithFormattedText(string text)
        {
            Paragraph paragraph = new Paragraph();
            List<Run> runs = CreateFormattedRuns(text);
            foreach (Run run in runs)
            {
                _ = paragraph.AppendChild(run);
            }
            return paragraph;
        }

        /// <summary>
        /// 创建格式化的Run元素列表，处理换行符并设置红色
        /// </summary>
        private List<Run> CreateFormattedRuns(string text)
        {
            List<Run> runs = new List<Run>();

            if (string.IsNullOrEmpty(text))
            {
                return runs;
            }

            // 按换行符分割文本
            string[] lines = text.Split(["\n"], StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 创建带红色格式的Run
                Run run = new Run();

                // 设置文本颜色为红色
                RunProperties runProperties = new RunProperties();
                Color color = new Color() { Val = "FF0000" }; // 红色
                _ = runProperties.AppendChild(color);
                _ = run.AppendChild(runProperties);

                // 添加文本内容
                Text text_element = new Text(line);
                _ = run.AppendChild(text_element);
                runs.Add(run);

                // 如果不是最后一行，添加换行符
                if (i < lines.Length - 1)
                {
                    Run breakRun = new Run(new Break());
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
            List<OpenXmlElement> elements = new List<OpenXmlElement>();

            if (string.IsNullOrEmpty(text))
            {
                return elements;
            }

            // 按换行符分割文本
            string[] lines = text.Split(["\n"], StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 设置文本颜色为红色
                RunProperties runProperties = new RunProperties();
                Color color = new Color() { Val = "FF0000" }; // 红色
                _ = runProperties.AppendChild(color);
                elements.Add(runProperties);

                // 添加文本内容
                Text textElement = new Text(line);
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
            string timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
            string outputFileName = $"{originalFileName} -- 替换 --{timestamp}.docx";
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
                WordprocessingCommentsPart? commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
                if (commentsPart == null)
                {
                    _logger.LogDebug("创建新的批注部分");
                    commentsPart = document.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
                    commentsPart.Comments = new Comments();
                }

                // 2. 生成唯一ID (Find the next available ID)
                string id = "0";
                if (commentsPart.Comments != null && commentsPart.Comments.HasChildren)
                {
                    // ID必须是唯一的数字字符串。我们找到当前最大的ID并加1。
                    int maxId = commentsPart.Comments.Descendants<Comment>()
                        .Select(static c => int.TryParse(c.Id?.Value, out int commentId) ? commentId : 0)
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
                Paragraph p = new(new Run(new Text(commentText)));
                Comment comment = new()
                {
                    Id = id,
                    Author = author,
                    Date = DateTime.Now,
                    Initials = author.Length >= 2 ? author[..2] : author // 可选
                };
                comment.Append(p);

                // 将新批注添加到 comments.xml
                commentsPart.Comments.Append(comment);
                commentsPart.Comments.Save();
                _logger.LogDebug($"批注已添加到comments.xml，ID: {id}");

                // 4 & 5. 在正文中标记范围和添加引用
                // 在被批注的元素前后插入范围标记
                _ = targetRun.InsertBeforeSelf(new CommentRangeStart() { Id = id });
                _ = targetRun.InsertAfterSelf(new CommentRangeEnd() { Id = id });

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
        private static int GenerateCommentId(Comments comments)
        {
            List<int> existingIds = comments.Elements<Comment>()
                .Select(static c => int.TryParse(c.Id?.Value, out int id) ? id : 0)
                .ToList();

            return existingIds.Any() ? existingIds.Max() + 1 : 1;
        }

        // 旧的批注方法已被AddCommentToElement替代，此方法已删除

        // AddProcessingMarkToControl方法已删除，处理标记功能已集成到批注中

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
            throw new NotImplementedException();
        }
    }
}