using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Services
{
    /// <summary>
    /// OpenXML Word文档处理器
    /// </summary>
    public class OpenXmlDocumentHandler
    {
        private readonly ILogger<OpenXmlDocumentHandler> _logger;
        private readonly IFileService _fileService;

        public OpenXmlDocumentHandler(ILogger<OpenXmlDocumentHandler> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        /// <summary>
        /// 处理单个文档
        /// </summary>
        public ProcessResult ProcessDocument(string templatePath, Dictionary<string, object> data, string outputPath)
        {
            var result = new ProcessResult
            {
                StartTime = DateTime.Now,
                TotalRecords = 1,
                SuccessfulRecords = 0
            };

            try
            {
                _logger.LogInformation($"开始处理文档: {templatePath} -> {outputPath}");

                // 验证模板文件
                var templateValidation = ValidateTemplate(templatePath);
                if (!templateValidation.IsValid)
                {
                    result.AddError($"模板验证失败: {templateValidation.ErrorMessage}");
                    return result;
                }

                // 复制模板到输出路径
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    _fileService.EnsureDirectoryExists(outputDirectory);
                }

                File.Copy(templatePath, outputPath, true);

                // 处理文档内容
                using (var document = WordprocessingDocument.Open(outputPath, true))
                {
                    if (document.MainDocumentPart == null)
                    {
                        result.AddError("文档主体部分不存在");
                        return result;
                    }

                    var contentControls = GetContentControls(document.MainDocumentPart.Document);
                    _logger.LogDebug($"找到 {contentControls.Count} 个内容控件");

                    int processedControls = 0;
                    foreach (var control in contentControls)
                    {
                        try
                        {
                            var processed = ProcessContentControl(control, data);
                            if (processed)
                            {
                                processedControls++;
                            }
                        }
                        catch (Exception ex)
                        {
                            var tag = GetContentControlTag(control);
                            result.AddWarning($"处理内容控件 '{tag}' 时发生错误: {ex.Message}");
                            _logger.LogWarning(ex, $"处理内容控件失败: {tag}");
                        }
                    }

                    // 保存文档
                    document.MainDocumentPart.Document.Save();
                    _logger.LogInformation($"文档处理完成，处理了 {processedControls} 个内容控件");
                }

                result.AddGeneratedFile(outputPath);
                result.IsSuccess = true;
                result.SuccessfulRecords = 1;
                result.EndTime = DateTime.Now;

                _logger.LogInformation($"文档处理成功: {outputPath}");
            }
            catch (Exception ex)
            {
                result.AddError($"处理文档时发生异常: {ex.Message}");
                _logger.LogError(ex, $"处理文档失败: {templatePath}");
            }

            return result;
        }

        /// <summary>
        /// 验证模板文件
        /// </summary>
        public ValidationResult ValidateTemplate(string templatePath)
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

                // 验证文件扩展名
                var extension = Path.GetExtension(templatePath)?.ToLowerInvariant();
                if (extension != ".docx")
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"不支持的文件格式: {extension}，仅支持 .docx 格式";
                    return result;
                }

                // 尝试打开文档验证格式
                using (var document = WordprocessingDocument.Open(templatePath, false))
                {
                    if (document.MainDocumentPart == null)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "无效的Word文档格式";
                        return result;
                    }

                    // 检查是否有内容控件
                    var contentControls = GetContentControls(document.MainDocumentPart.Document);
                    if (!contentControls.Any())
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "模板中未找到任何内容控件";
                        return result;
                    }

                    _logger.LogInformation($"模板验证成功，找到 {contentControls.Count} 个内容控件");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"验证模板时发生异常: {ex.Message}";
                _logger.LogError(ex, $"验证模板失败: {templatePath}");
            }

            return result;
        }

        /// <summary>
        /// 获取模板中的内容控件信息
        /// </summary>
        public List<ContentControlData> GetContentControlsInfo(string templatePath)
        {
            var contentControlsInfo = new List<ContentControlData>();

            try
            {
                using (var document = WordprocessingDocument.Open(templatePath, false))
                {
                    if (document.MainDocumentPart?.Document == null)
                    {
                        return contentControlsInfo;
                    }

                    var contentControls = GetContentControls(document.MainDocumentPart.Document);
                    
                    foreach (var control in contentControls)
                    {
                        var info = ExtractContentControlInfo(control);
                        if (info != null)
                        {
                            contentControlsInfo.Add(info);
                        }
                    }
                }

                _logger.LogInformation($"提取内容控件信息完成，共 {contentControlsInfo.Count} 个");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取内容控件信息失败: {templatePath}");
            }

            return contentControlsInfo;
        }

        /// <summary>
        /// 获取文档中的所有内容控件
        /// </summary>
        private List<SdtElement> GetContentControls(Document document)
        {
            var contentControls = new List<SdtElement>();

            // 获取文档正文中的内容控件
            var bodyControls = document.Body?.Descendants<SdtElement>().ToList() ?? new List<SdtElement>();
            contentControls.AddRange(bodyControls);

            // 获取页眉页脚中的内容控件
            try
            {
                var headerControls = new List<SdtElement>();
                var footerControls = new List<SdtElement>();

                // 获取页眉中的内容控件
                foreach (var headerPart in document.MainDocumentPart?.HeaderParts ?? Enumerable.Empty<HeaderPart>())
                {
                    var controls = headerPart.Header?.Descendants<SdtElement>() ?? Enumerable.Empty<SdtElement>();
                    headerControls.AddRange(controls);
                }

                // 获取页脚中的内容控件
                foreach (var footerPart in document.MainDocumentPart?.FooterParts ?? Enumerable.Empty<FooterPart>())
                {
                    var controls = footerPart.Footer?.Descendants<SdtElement>() ?? Enumerable.Empty<SdtElement>();
                    footerControls.AddRange(controls);
                }

                contentControls.AddRange(headerControls);
                contentControls.AddRange(footerControls);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取页眉页脚内容控件时发生异常");
            }

            return contentControls;
        }

        /// <summary>
        /// 处理单个内容控件
        /// </summary>
        private bool ProcessContentControl(SdtElement control, Dictionary<string, object> data)
        {
            try
            {
                var tag = GetContentControlTag(control);
                if (string.IsNullOrEmpty(tag))
                {
                    return false;
                }

                // 查找匹配的数据
                var value = FindMatchingValue(tag, data);
                if (value == null)
                {
                    _logger.LogDebug($"未找到标签 '{tag}' 对应的数据");
                    return false;
                }

                // 设置内容控件的值
                SetContentControlValue(control, value.ToString());
                _logger.LogDebug($"设置内容控件 '{tag}' 的值: {value}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理内容控件时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 获取内容控件的标签
        /// </summary>
        private string GetContentControlTag(SdtElement control)
        {
            try
            {
                var properties = control.GetFirstChild<SdtProperties>();
                var tag = properties?.GetFirstChild<Tag>();
                return tag?.Val?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 设置内容控件的值
        /// </summary>
        private void SetContentControlValue(SdtElement control, string value)
        {
            try
            {
                var contentBlock = control.GetFirstChild<SdtContentBlock>();
                var contentRun = control.GetFirstChild<SdtContentRun>();
                
                if (contentBlock == null && contentRun == null) return;

                // 处理块级内容控件
                if (contentBlock != null)
                {
                    contentBlock.RemoveAllChildren();
                    var paragraph = new Paragraph(new Run(new Text(value)));
                    contentBlock.AppendChild(paragraph);
                }
                // 处理行内内容控件
                else if (contentRun != null)
                {
                    contentRun.RemoveAllChildren();
                    var run = new Run(new Text(value));
                    contentRun.AppendChild(run);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置内容控件值时发生异常");
            }
        }

        /// <summary>
        /// 查找匹配的数据值
        /// </summary>
        private object FindMatchingValue(string tag, Dictionary<string, object> data)
        {
            // 直接匹配
            if (data.ContainsKey(tag))
            {
                return data[tag];
            }

            // 忽略大小写匹配
            var matchingKey = data.Keys.FirstOrDefault(k => 
                string.Equals(k, tag, StringComparison.OrdinalIgnoreCase));
            
            if (matchingKey != null)
            {
                return data[matchingKey];
            }

            return null;
        }

        /// <summary>
        /// 提取内容控件信息
        /// </summary>
        private ContentControlData ExtractContentControlInfo(SdtElement control)
        {
            try
            {
                var properties = control.GetFirstChild<SdtProperties>();
                if (properties == null) return null;

                var tag = properties.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty;
                var title = properties.GetFirstChild<SdtAlias>()?.Val?.Value ?? string.Empty;

                // 获取当前值
                var currentValue = GetContentControlCurrentValue(control);

                // 确定控件类型
                var controlType = DetermineContentControlType(properties);

                return new ContentControlData
                {
                    Tag = tag,
                    Title = title,
                    Value = currentValue,
                    Type = controlType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取内容控件信息时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 获取内容控件当前值
        /// </summary>
        private string GetContentControlCurrentValue(SdtElement control)
        {
            try
            {
                var contentBlock = control.GetFirstChild<SdtContentBlock>();
                var contentRun = control.GetFirstChild<SdtContentRun>();
                
                OpenXmlElement content = contentBlock ?? (OpenXmlElement)contentRun;
                if (content == null) return string.Empty;

                var texts = content.Descendants<Text>().Select(t => t.Text).ToList();
                return string.Join("", texts);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 确定内容控件类型
        /// </summary>
        private ContentControlType DetermineContentControlType(SdtProperties properties)
        {
            // 检查文本控件
            if (properties.Elements().Any(e => e.LocalName == "text"))
                return ContentControlType.Text;
            
            // 检查日期控件
            if (properties.Elements().Any(e => e.LocalName == "date"))
                return ContentControlType.Date;
            
            // 检查下拉列表控件
            if (properties.Elements().Any(e => e.LocalName == "dropDownList"))
                return ContentControlType.DropDownList;
            
            // 检查组合框控件
             if (properties.Elements().Any(e => e.LocalName == "comboBox"))
                 return ContentControlType.ComboBox;
            
            return ContentControlType.Text;
        }
    }
}