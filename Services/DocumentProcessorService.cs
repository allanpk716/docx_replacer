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
        private readonly IExcelDataParser _excelDataParser;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly ContentControlProcessor _contentControlProcessor;
        private readonly CommentManager _commentManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISafeFormattedContentReplacer _safeFormattedContentReplacer;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed = false;

        public event EventHandler<ProgressEventArgs>? ProgressUpdated;

        public DocumentProcessorService(
            ILogger<DocumentProcessorService> logger,
            IExcelDataParser excelDataParser,
            IFileService fileService,
            IProgressReporter progressReporter,
            ContentControlProcessor contentControlProcessor,
            CommentManager commentManager,
            IServiceProvider serviceProvider,
            ISafeFormattedContentReplacer safeFormattedContentReplacer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _excelDataParser = excelDataParser ?? throw new ArgumentNullException(nameof(excelDataParser));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _contentControlProcessor = contentControlProcessor ?? throw new ArgumentNullException(nameof(contentControlProcessor));
            _commentManager = commentManager ?? throw new ArgumentNullException(nameof(commentManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _safeFormattedContentReplacer = safeFormattedContentReplacer ?? throw new ArgumentNullException(nameof(safeFormattedContentReplacer));

            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }

        public async Task<ProcessResult> ProcessDocumentsAsync(ProcessRequest request, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ProcessResult result = new ProcessResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("开始批量处理文档");

                // 初始化 CancellationTokenSource 并链接外部 token
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

                // 使用 Excel 解析器处理
                return await ProcessExcelDataAsync(request, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("批量处理已被取消");
                result.AddError("处理已被用户取消");
                result.Message = "处理已被取消";
                throw;
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
        /// 处理 Excel 数据文件
        /// </summary>
        private async Task<ProcessResult> ProcessExcelDataAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            ProcessResult result = new ProcessResult { StartTime = DateTime.Now };

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("使用 Excel 解析器处理数据");

                // 解析 Excel 数据文件
                Dictionary<string, FormattedCellValue> excelData = await _excelDataParser.ParseExcelFileAsync(request.DataFilePath);
                if (excelData == null || !excelData.Any())
                {
                    result.AddError("Excel 数据文件为空或解析失败");
                    return result;
                }

                _logger.LogInformation($"成功解析 Excel 数据，共 {excelData.Count} 个键值对");

                cancellationToken.ThrowIfCancellationRequested();

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

                // 生成输出文件名
                string templateFileName = Path.GetFileNameWithoutExtension(request.TemplateFilePath);
                string timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
                string outputFileName = $"{templateFileName} -- 替换 --{timestamp}.docx";
                string outputPath = Path.Combine(request.OutputDirectory, outputFileName);

                // 使用格式化数据处理文档
                ProcessResult processResult = await ProcessDocumentWithFormattedDataAsync(
                    request.TemplateFilePath,
                    excelData,
                    outputPath,
                    cancellationToken);

                if (processResult.IsSuccess)
                {
                    result.IsSuccess = true;
                    result.SuccessfulRecords = 1;
                    result.GeneratedFiles.Add(outputPath);
                    result.Message = $"Excel 数据处理完成，成功生成 1 个文档";

                    _progressReporter.ReportCompleted(1, "处理完成");
                }
                else
                {
                    result.AddError($"处理文档失败: {string.Join(", ", processResult.Errors)}");
                }

                _logger.LogInformation($"Excel 数据处理完成，成功: {result.SuccessfulRecords}");
            }
            catch (Exception ex)
            {
                result.AddError($"Excel 数据处理过程中发生异常: {ex.Message}");
                _logger.LogError(ex, "Excel 数据处理异常");
            }

            return result;
        }



        /// <summary>
        /// 修复表格单元格结构
        /// 确保每个表格单元格中只有一个段落，多个段落会导致表格结构错乱
        /// 关键：只合并单元格直接包含的段落，跳过 SdtContentBlock 容器内的段落
        /// </summary>
        /// <param name="document">Word 文档</param>
        private void FixTableCellStructure(WordprocessingDocument document)
        {
            if (document.MainDocumentPart == null)
                return;

            _logger.LogInformation("开始修复表格单元格结构");

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            _logger.LogInformation($"找到 {tables.Count} 个表格");

            int totalCellsFixed = 0;
            int totalParagraphsMerged = 0;

            foreach (var table in tables)
            {
                var cells = table.Descendants<TableCell>().ToList();
                _logger.LogDebug($"表格中有 {cells.Count} 个单元格");

                foreach (var cell in cells)
                {
                    bool hasSdtContext = cell.Descendants<SdtElement>().Any() || cell.Ancestors<SdtElement>().Any();
                    if (!hasSdtContext)
                        continue;

                    // 获取所有段落，但过滤掉 SdtContentBlock 内的段落
                    var allParagraphs = cell.Elements<Paragraph>().ToList();
                    var directParagraphs = allParagraphs
                        .Where(p => !IsInSdtContentBlock(p))
                        .ToList();

                    if (directParagraphs.Count <= 1)
                        continue;

                    _logger.LogDebug($"单元格中有 {directParagraphs.Count} 个直接段落，需要合并（总段落数: {allParagraphs.Count}）");

                    // 保留第一个直接段落
                    var firstParagraph = directParagraphs[0];

                    // 将其他直接段落中的 Run 移动到第一个段落
                    for (int i = 1; i < directParagraphs.Count; i++)
                    {
                        var extraParagraph = directParagraphs[i];
                        bool extraHasContent = extraParagraph.ChildElements.Any(e => e is not ParagraphProperties);
                        if (extraHasContent && firstParagraph.ChildElements.Any(e => e is not ParagraphProperties))
                        {
                            firstParagraph.AppendChild(new Run(new Break()));
                        }

                        var elementsToMove = extraParagraph.ChildElements
                            .Where(e => e is not ParagraphProperties)
                            .ToList();

                        foreach (var element in elementsToMove)
                        {
                            element.Remove();
                            firstParagraph.AppendChild(element);
                        }

                        // 删除多余的段落
                        extraParagraph.Remove();
                        totalParagraphsMerged++;
                    }

                    totalCellsFixed++;
                }
            }

            _logger.LogInformation($"表格单元格结构修复完成: 修复了 {totalCellsFixed} 个单元格，合并了 {totalParagraphsMerged} 个段落");
        }

        /// <summary>
        /// 检查段落是否在 SdtContentBlock 容器内
        /// </summary>
        private bool IsInSdtContentBlock(Paragraph paragraph)
        {
            var current = paragraph.Parent;
            while (current != null)
            {
                if (current is SdtContentBlock)
                    return true;
                // 如果到达单元格级别，停止查找
                if (current is TableCell)
                    return false;
                current = current.Parent;
            }
            return false;
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

                // 处理文档主体
                IEnumerable<SdtElement> bodyControls = document.MainDocumentPart.Document.Descendants<SdtElement>();
                foreach (SdtElement control in bodyControls)
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
                            Type = ContentControlType.Text,
                            Location = ContentControlLocation.Body
                        });
                    }
                }

                // 处理页眉
                foreach (var headerPart in document.MainDocumentPart.HeaderParts)
                {
                    IEnumerable<SdtElement> headerControls = headerPart.Header?.Descendants<SdtElement>() ?? Enumerable.Empty<SdtElement>();
                    foreach (SdtElement control in headerControls)
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
                                Type = ContentControlType.Text,
                                Location = ContentControlLocation.Header
                            });
                        }
                    }
                }

                // 处理页脚
                foreach (var footerPart in document.MainDocumentPart.FooterParts)
                {
                    IEnumerable<SdtElement> footerControls = footerPart.Footer?.Descendants<SdtElement>() ?? Enumerable.Empty<SdtElement>();
                    foreach (SdtElement control in footerControls)
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
                                Type = ContentControlType.Text,
                                Location = ContentControlLocation.Footer
                            });
                        }
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
        /// 批量处理文件夹中的模板文件
        /// </summary>
        /// <param name="request">文件夹处理请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        public async Task<ProcessResult> ProcessFolderAsync(FolderProcessRequest request, CancellationToken cancellationToken = default)
        {
            ProcessResult result = new ProcessResult { IsSuccess = true, StartTime = DateTime.Now };
            List<string> processedFiles = new List<string>();
            List<string> failedFiles = new List<string>();

            try
            {
                // 初始化 CancellationTokenSource 并链接外部 token
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _logger.LogInformation($"开始批量处理文件夹: {request.TemplateFolderPath}");
                _logger.LogInformation($"数据文件: {request.DataFilePath}");
                _logger.LogInformation($"输出目录: {request.OutputDirectory}");
                _logger.LogInformation($"模板文件数量: {request.TemplateFiles.Count}");

                // 验证输入参数
                if (request.TemplateFiles == null || !request.TemplateFiles.Any())
                {
                    result.IsSuccess = false;
                    result.AddError("没有找到要处理的模板文件");
                    result.ErrorMessage = "没有找到要处理的模板文件";
                    return result;
                }

                if (!_fileService.FileExists(request.DataFilePath))
                {
                    result.IsSuccess = false;
                    result.AddError($"数据文件不存在: {request.DataFilePath}");
                    result.ErrorMessage = "数据文件不存在";
                    return result;
                }

                // Excel模式：解析为格式化数据（单条记录）
                _logger.LogInformation("使用 Excel 解析器处理数据");

                // 声明输出目录变量
                string timestampOutputDir = string.Empty;

                Dictionary<string, FormattedCellValue> excelData = await _excelDataParser.ParseExcelFileAsync(request.DataFilePath);

                if (excelData == null || !excelData.Any())
                {
                    result.IsSuccess = false;
                    result.AddError($"Excel 数据文件为空或解析失败: {request.DataFilePath}");
                    result.ErrorMessage = "Excel 数据文件为空或解析失败";
                    return result;
                }

                _logger.LogInformation($"成功解析 Excel 数据，共 {excelData.Count} 个键值对");

                // 创建时间戳输出文件夹
                string timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
                string inputFolderName = Path.GetFileName(request.TemplateFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string timestampFolderName = $"{inputFolderName}_{timestamp}";
                timestampOutputDir = Path.Combine(request.OutputDirectory, timestampFolderName);

                if (!_fileService.DirectoryExists(timestampOutputDir))
                {
                    _ = Directory.CreateDirectory(timestampOutputDir);
                    _logger.LogInformation($"创建时间戳输出目录: {timestampOutputDir}");
                }

                int totalOperations = request.TemplateFiles.Count;
                int currentOperation = 0;
                var linkedToken = _cancellationTokenSource.Token;

                // Excel模式：为每个模板文件生成一个文档
                foreach (Models.FileInfo templateFile in request.TemplateFiles)
                {
                    try
                    {
                        linkedToken.ThrowIfCancellationRequested();
                        currentOperation++;

                        _logger.LogInformation($"处理模板文件: {templateFile.Name}");

                        // 验证模板文件
                        ValidationResult validationResult = await ValidateTemplateAsync(templateFile.FullPath);
                        if (!validationResult.IsValid)
                        {
                            _logger.LogWarning($"模板文件验证失败: {templateFile.Name} - {validationResult.ErrorMessage}");
                            failedFiles.Add($"{templateFile.Name}: {validationResult.ErrorMessage}");

                            // 验证失败也要更新进度
                            _progressReporter.ReportProgress(currentOperation, totalOperations,
                                $"跳过 {templateFile.Name} - 验证失败 ({currentOperation}/{totalOperations})", templateFile.Name);
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

                        // 生成输出文件名 - 保持原始文件名
                        string outputFileName = templateFile.Name;
                        string outputPath = Path.Combine(outputSubDir, outputFileName);

                        _logger.LogDebug($"处理模板文件，输出到: {outputPath}");

                        // 使用格式化数据处理文档
                        ProcessResult processResult = await ProcessDocumentWithFormattedDataAsync(
                            templateFile.FullPath,
                            excelData,
                            outputPath,
                            linkedToken);

                        if (processResult.IsSuccess)
                        {
                            processedFiles.Add(outputPath);
                            _logger.LogDebug($"✓ 成功处理: {outputFileName}");
                        }
                        else
                        {
                            failedFiles.Add($"{templateFile.Name}: {string.Join(", ", processResult.Errors)}");
                            _logger.LogWarning($"✗ 处理失败: {outputFileName}");
                        }

                        // 更新进度 - 使用 _progressReporter 确保进度传播到 UI
                        _progressReporter.ReportProgress(currentOperation, totalOperations,
                            $"正在处理 {templateFile.Name} ({currentOperation}/{totalOperations})", templateFile.Name);

                        _logger.LogInformation($"✓ 完成模板文件处理: {templateFile.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"处理模板文件时发生异常: {templateFile.Name}");
                        failedFiles.Add($"{templateFile.Name}: {ex.Message}");

                        // 异常也要更新进度，确保进度条不会卡住
                        _progressReporter.ReportProgress(currentOperation, totalOperations,
                            $"处理失败 {templateFile.Name} ({currentOperation}/{totalOperations})", templateFile.Name);
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

                // 报告最终完成状态，确保进度条达到 100%
                _progressReporter.ReportCompleted(totalOperations,
                    $"批量处理完成，成功: {processedFiles.Count}，失败: {failedFiles.Count}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("文件夹批量处理已被取消");
                result.IsSuccess = false;
                result.AddError("处理已被用户取消");
                result.ErrorMessage = "处理已被用户取消";
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量处理文件夹时发生异常: {ex.Message}");
                result.IsSuccess = false;
                result.AddError($"批量处理失败: {ex.Message}");
                result.ErrorMessage = $"批量处理失败: {ex.Message}";
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
        /// 用格式化值填充内容控件（支持富文本）
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="formattedValue">格式化的值</param>
        /// <param name="document">Word 文档</param>
        /// <param name="location">控件位置（用于批注）</param>
        private void FillContentControlWithFormattedValue(
            SdtElement control,
            FormattedCellValue formattedValue,
            WordprocessingDocument document,
            ContentControlLocation location = ContentControlLocation.Body)
        {
            // 1. 获取控件标签（用于日志）
            string? tag = OpenXmlHelper.GetControlTag(control);
            if (string.IsNullOrWhiteSpace(tag))
            {
                _logger.LogWarning("内容控件标签为空，跳过处理");
                return;
            }

            _logger.LogDebug($"开始填充格式化内容控件: {tag}");

            // 2. 检查是否在表格单元格中
            bool isInTableCell = OpenXmlTableCellHelper.IsInTableCell(control);
            bool containsTableCell = control.Descendants<TableCell>().Any();

            // 3. 记录旧值（用于批注）
            string oldValue = OpenXmlHelper.ExtractExistingText(control);

            // 4. 根据位置选择填充策略
            if (isInTableCell)
            {
                _logger.LogDebug("检测到表格单元格内容控件，使用安全填充策略");
                _safeFormattedContentReplacer.ReplaceFormattedContentInControl(control, formattedValue);
            }
            else if (containsTableCell)
            {
                _logger.LogDebug("检测到控件包装了表格单元格，使用安全填充策略");
                _safeFormattedContentReplacer.ReplaceFormattedContentInControl(control, formattedValue);
            }
            else
            {
                _logger.LogDebug("非表格单元格内容控件，使用标准填充策略");
                FillFormattedContentStandard(control, formattedValue);
            }

            // 5. 添加批注（仅正文区域支持，页眉页脚不支持批注）
            if (location == ContentControlLocation.Body)
            {
                OpenXmlHelper.AddProcessingComment(document, control, tag, formattedValue.PlainText, oldValue, location, _commentManager, _logger);
            }
            else
            {
                _logger.LogDebug($"跳过批注添加(页眉页脚不支持批注功能),标签: '{tag}', 位置: {location}");
            }

            _logger.LogInformation($"✓ 成功填充格式化控件 '{tag}' ({location})");
        }

        /// <summary>
        /// 标准格式化内容填充（非表格单元格）
        /// </summary>
        private void FillFormattedContentStandard(SdtElement control, FormattedCellValue formattedValue)
        {
            // 查找内容容器
            var contentContainer = OpenXmlHelper.FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning($"未找到内容容器");
                return;
            }

            var baseRunProperties = contentContainer.Descendants<Run>()
                .Select(static r => r.RunProperties)
                .FirstOrDefault(static rp => rp != null)?
                .CloneNode(true) as RunProperties;

            _logger.LogDebug("标准格式化填充: 基础RunProperties存在: {HasBaseRunProperties}", baseRunProperties != null);

            // 清空现有内容
            contentContainer.RemoveAllChildren();

            // 根据控件类型创建新内容
            if (control is SdtBlock || contentContainer is SdtContentBlock)
            {
                // 块级控件：创建 Paragraph
                var paragraph = CreateParagraphWithFormattedText(formattedValue, baseRunProperties);
                contentContainer.AppendChild(paragraph);
            }
            else
            {
                // 行内控件：直接添加 Run
                foreach (var fragment in formattedValue.Fragments)
                {
                    var run = CreateFormattedRun(fragment, baseRunProperties);
                    contentContainer.AppendChild(run);
                }
            }
        }

        /// <summary>
        /// 创建包含格式化文本的段落
        /// </summary>
        private Paragraph CreateParagraphWithFormattedText(FormattedCellValue formattedValue, RunProperties? baseRunProperties)
        {
            var paragraph = new Paragraph();

            foreach (var fragment in formattedValue.Fragments)
            {
                var run = CreateFormattedRun(fragment, baseRunProperties);
                paragraph.AppendChild(run);
            }

            return paragraph;
        }

        /// <summary>
        /// 创建带格式的 Run 元素（支持上标、下标）
        /// </summary>
        private Run CreateFormattedRun(TextFragment fragment)
        {
            return CreateFormattedRun(fragment, null);
        }

        private Run CreateFormattedRun(TextFragment fragment, RunProperties? baseRunProperties)
        {
            var run = new Run();
            var runProperties = baseRunProperties?.CloneNode(true) as RunProperties;

            // 添加格式属性
            if (fragment.IsSuperscript || fragment.IsSubscript)
            {
                runProperties ??= new RunProperties();
                runProperties.VerticalTextAlignment = new VerticalTextAlignment
                {
                    Val = fragment.IsSuperscript ? VerticalPositionValues.Superscript : VerticalPositionValues.Subscript
                };
            }
            else if (runProperties?.VerticalTextAlignment != null)
            {
                runProperties.VerticalTextAlignment = null;
            }

            if (runProperties != null)
            {
                run.Append(runProperties);
            }

            AppendTextWithLineBreaks(run, fragment.Text);

            return run;
        }

        private static string NormalizeLineEndings(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static void AppendTextWithLineBreaks(Run run, string text)
        {
            var normalized = NormalizeLineEndings(text);
            if (string.IsNullOrEmpty(normalized))
            {
                run.AppendChild(new Text(string.Empty) { Space = SpaceProcessingModeValues.Preserve });
                return;
            }

            var lines = normalized.Split(["\n"], StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                run.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
                if (i < lines.Length - 1)
                {
                    run.AppendChild(new Break());
                }
            }
        }

        /// <summary>
        /// 获取文档中所有内容控件（包括页眉页脚）
        /// </summary>
        private List<(SdtElement Element, string Tag, ContentControlLocation Location)> GetAllContentControls(
            WordprocessingDocument document)
        {
            var result = new List<(SdtElement, string, ContentControlLocation)>();

            if (document.MainDocumentPart == null)
                return result;

            // 1. 文档主体
            foreach (var control in document.MainDocumentPart.Document.Descendants<SdtElement>())
            {
                string? tag = OpenXmlHelper.GetControlTag(control);
                if (!string.IsNullOrWhiteSpace(tag) && !OpenXmlHelper.HasAncestorWithSameTag(control, tag))
                {
                    result.Add((control, tag, ContentControlLocation.Body));
                }
            }

            // 2. 页眉
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                foreach (var control in headerPart.Header?.Descendants<SdtElement>()
                    ?? Enumerable.Empty<SdtElement>())
                {
                    string? tag = OpenXmlHelper.GetControlTag(control);
                    if (!string.IsNullOrWhiteSpace(tag) && !OpenXmlHelper.HasAncestorWithSameTag(control, tag))
                    {
                        result.Add((control, tag, ContentControlLocation.Header));
                    }
                }
            }

            // 3. 页脚
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                foreach (var control in footerPart.Footer?.Descendants<SdtElement>()
                    ?? Enumerable.Empty<SdtElement>())
                {
                    string? tag = OpenXmlHelper.GetControlTag(control);
                    if (!string.IsNullOrWhiteSpace(tag) && !OpenXmlHelper.HasAncestorWithSameTag(control, tag))
                    {
                        result.Add((control, tag, ContentControlLocation.Footer));
                    }
                }
            }

            return result;
        }

        public Task<ProcessResult> ProcessDocumentWithFormattedDataAsync(
            string templateFilePath,
            Dictionary<string, FormattedCellValue> formattedData,
            string outputFilePath,
            CancellationToken cancellationToken = default)
        {
            var result = new ProcessResult { IsSuccess = false, StartTime = DateTime.Now };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation($"开始处理文档（格式化数据）: {templateFilePath}");

                // 1. 验证输入
                if (!_fileService.FileExists(templateFilePath))
                {
                    result.Errors.Add($"模板文件不存在: {templateFilePath}");
                    result.EndTime = DateTime.Now;
                    return Task.FromResult(result);
                }

                // 2. 复制模板文件
                File.Copy(templateFilePath, outputFilePath, true);

                cancellationToken.ThrowIfCancellationRequested();

                // 3. 打开文档进行编辑
                using var document = WordprocessingDocument.Open(outputFilePath, true);

                // 4. 获取模板中的所有内容控件（包括页眉页脚）
                var allControls = GetAllContentControls(document);
                _logger.LogInformation($"找到 {allControls.Count} 个内容控件");

                // 5. 填充每个匹配的内容控件
                int filledCount = 0;
                foreach (var controlInfo in allControls)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (formattedData.TryGetValue(controlInfo.Tag, out var formattedValue))
                    {
                        FillContentControlWithFormattedValue(
                            controlInfo.Element,
                            formattedValue,
                            document,
                            controlInfo.Location);
                        filledCount++;
                    }
                    else
                    {
                        _logger.LogDebug($"未找到控件 '{controlInfo.Tag}' 的数据");
                    }
                }

                // 6. 修复表格单元格结构（重要：处理完所有内容控件后，确保单元格中只有一个段落）
                FixTableCellStructure(document);

                // 7. 保存并关闭
                document.Save();

                stopwatch.Stop();
                result.IsSuccess = true;
                result.SuccessfulRecords = 1;
                result.GeneratedFiles.Add(outputFilePath);
                result.Message = $"成功填充 {filledCount} 个内容控件";
                result.EndTime = DateTime.Now;

                _logger.LogInformation($"文档处理完成: {outputFilePath}, 耗时: {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Errors.Add($"处理文档失败: {ex.Message}");
                _logger.LogError(ex, $"处理文档失败: {templateFilePath}");
                result.EndTime = DateTime.Now;
            }

            return Task.FromResult(result);
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
