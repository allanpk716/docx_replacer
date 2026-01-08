using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 内容控件处理器
    /// </summary>
    public class ContentControlProcessor
    {
        private readonly ILogger<ContentControlProcessor> _logger;
        private readonly CommentManager _commentManager;

        public ContentControlProcessor(ILogger<ContentControlProcessor> logger, CommentManager commentManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commentManager = commentManager ?? throw new ArgumentNullException(nameof(commentManager));
        }

        /// <summary>
        /// 处理单个内容控件
        /// </summary>
        public void ProcessContentControl(SdtElement control, Dictionary<string, object> data, WordprocessingDocument document, ContentControlLocation location = ContentControlLocation.Body)
        {
            try
            {
                // 获取控件标签
                string? tag = GetControlTag(control);
                if (string.IsNullOrWhiteSpace(tag))
                {
                    _logger.LogWarning("内容控件标签为空，跳过处理");
                    return;
                }

                // 验证数据是否存在
                if (!data.ContainsKey(tag))
                {
                    _logger.LogWarning($"数据中未找到标签 '{tag}' 对应的值，跳过处理");
                    return;
                }

                string value = data[tag]?.ToString() ?? string.Empty;
                _logger.LogDebug($"找到匹配数据: '{tag}' -> '{value}'");

                // 记录旧值
                string oldValue = ExtractExistingText(control);

                // 处理内容替换
                ProcessContentReplacement(control, value);

                // 添加批注
                AddProcessingComment(document, control, tag, value, oldValue, location);

                _logger.LogInformation($"✓ 成功替换内容控件 '{tag}' ({location}) 为 '{value}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理内容控件时发生异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 处理文档中的所有内容控件（包括页眉页脚）
        /// </summary>
        public void ProcessContentControlsInDocument(
            WordprocessingDocument document,
            Dictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            if (document.MainDocumentPart == null)
            {
                _logger.LogError("文档主体部分不存在");
                return;
            }

            _logger.LogInformation("开始处理文档中的所有内容控件");

            // 处理文档主体
            ProcessControlsInPart(
                document.MainDocumentPart.Document,
                data,
                document,
                ContentControlLocation.Body,
                cancellationToken);

            // 处理所有页眉
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessControlsInHeaderPart(headerPart, data, document, cancellationToken);
            }

            // 处理所有页脚
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessControlsInFooterPart(footerPart, data, document, cancellationToken);
            }

            _logger.LogInformation("文档内容控件处理完成");
        }

        /// <summary>
        /// 处理文档部分中的内容控件
        /// </summary>
        private void ProcessControlsInPart(
            OpenXmlPartRootElement partRoot,
            Dictionary<string, object> data,
            WordprocessingDocument document,
            ContentControlLocation location,
            CancellationToken cancellationToken)
        {
            var contentControls = partRoot.Descendants<SdtElement>().ToList();
            _logger.LogDebug($"在 {location} 中找到 {contentControls.Count} 个内容控件");

            foreach (var control in contentControls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessContentControl(control, data, document, location);
            }
        }

        /// <summary>
        /// 处理页眉中的内容控件
        /// </summary>
        private void ProcessControlsInHeaderPart(
            HeaderPart headerPart,
            Dictionary<string, object> data,
            WordprocessingDocument document,
            CancellationToken cancellationToken)
        {
            if (headerPart.Header == null)
            {
                _logger.LogDebug("页眉部分为空，跳过处理");
                return;
            }

            _logger.LogDebug("开始处理页眉中的内容控件");
            ProcessControlsInPart(
                headerPart.Header,
                data,
                document,
                ContentControlLocation.Header,
                cancellationToken);
        }

        /// <summary>
        /// 处理页脚中的内容控件
        /// </summary>
        private void ProcessControlsInFooterPart(
            FooterPart footerPart,
            Dictionary<string, object> data,
            WordprocessingDocument document,
            CancellationToken cancellationToken)
        {
            if (footerPart.Footer == null)
            {
                _logger.LogDebug("页脚部分为空，跳过处理");
                return;
            }

            _logger.LogDebug("开始处理页脚中的内容控件");
            ProcessControlsInPart(
                footerPart.Footer,
                data,
                document,
                ContentControlLocation.Footer,
                cancellationToken);
        }

        /// <summary>
        /// 获取内容控件标签
        /// </summary>
        private string? GetControlTag(SdtElement control)
        {
            SdtProperties? properties = control.SdtProperties;
            return properties?.GetFirstChild<Tag>()?.Val?.Value;
        }

        /// <summary>
        /// 提取现有文本内容
        /// </summary>
        private string ExtractExistingText(SdtElement control)
        {
            List<Text> existingTextElements = control.Descendants<Text>().ToList();
            return existingTextElements.Any() ? string.Join("", existingTextElements.Select(static t => t.Text)) : string.Empty;
        }

        /// <summary>
        /// 处理内容替换
        /// </summary>
        private void ProcessContentReplacement(SdtElement control, string value)
        {
            // 尝试查找内容容器
            OpenXmlElement? content = FindContentContainer(control);

            if (content != null)
            {
                ReplaceContentInContainer(content, value, control);
            }
            else
            {
                ReplaceTextDirectly(control, value);
            }
        }

        /// <summary>
        /// 查找内容容器
        /// </summary>
        private OpenXmlElement? FindContentContainer(SdtElement control)
        {
            return control.Descendants<SdtContentRun>().FirstOrDefault() ??
                   control.Descendants<SdtContentBlock>().FirstOrDefault() as OpenXmlElement ??
                   control.Descendants<SdtContentCell>().FirstOrDefault();
        }

        /// <summary>
        /// 在容器中替换内容
        /// </summary>
        private void ReplaceContentInContainer(OpenXmlElement content, string value, SdtElement control)
        {
            _logger.LogDebug($"找到内容容器，类型: {content.GetType().Name}");

            // 清除现有内容
            content.RemoveAllChildren();

            // 添加新内容
            if (control is SdtBlock || content is SdtContentBlock || content is SdtContentCell)
            {
                Paragraph paragraph = CreateParagraphWithFormattedText(value);
                content.AppendChild(paragraph);
                _logger.LogDebug($"作为块级元素添加内容: '{value}'");
            }
            else
            {
                List<Run> runs = CreateFormattedRuns(value);
                foreach (Run run in runs)
                {
                    content.AppendChild(run);
                }
                _logger.LogDebug($"作为行内元素添加内容: '{value}'");
            }
        }

        /// <summary>
        /// 直接替换文本内容
        /// </summary>
        private void ReplaceTextDirectly(SdtElement control, string value)
        {
            List<Text> textElements = control.Descendants<Text>().ToList();
            if (!textElements.Any()) return;

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
                    firstRun.AppendChild(element);
                }

                // 移除其他多余的Run元素
                for (int i = 1; i < parentRuns.Count; i++)
                {
                    parentRuns[i].Remove();
                }
            }
        }

        /// <summary>
        /// 添加处理批注
        /// </summary>
        private void AddProcessingComment(WordprocessingDocument document, SdtElement control, string tag, string newValue, string oldValue, ContentControlLocation location)
        {
            // 查找所有相关的Run元素
            List<Run> targetRuns = FindAllTargetRuns(control);

            if (targetRuns.Count == 0)
            {
                _logger.LogWarning($"未找到目标Run元素，跳过批注添加，标签: '{tag}'");
                return;
            }

            string currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
            string locationText = location switch
            {
                ContentControlLocation.Header => "页眉",
                ContentControlLocation.Footer => "页脚",
                _ => "正文"
            };
            string commentText = $"此字段（{locationText}）已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{newValue}";

            // 根据Run数量选择批注方式
            if (targetRuns.Count == 1)
            {
                // 单行文本：使用原有方法
                _commentManager.AddCommentToElement(document, targetRuns[0], commentText, "DocuFiller系统", tag, location, control);
            }
            else
            {
                // 多行文本：使用新的范围批注方法
                _commentManager.AddCommentToRunRange(document, targetRuns, commentText, "DocuFiller系统", tag, location, control);
            }
        }

        /// <summary>
        /// 查找目标Run元素用于添加批注
        /// </summary>
        private Run? FindTargetRun(SdtElement control)
        {
            // 尝试从内容容器中查找Run
            OpenXmlElement? content = FindContentContainer(control);
            if (content != null)
            {
                if (content is SdtContentBlock || content is SdtContentCell)
                {
                    return content.Descendants<Run>().FirstOrDefault();
                }
                else if (content is SdtContentRun)
                {
                    return content.Descendants<Run>().FirstOrDefault();
                }
            }

            // 直接从控件中查找Run
            return control.Descendants<Run>().FirstOrDefault();
        }

        /// <summary>
        /// 查找内容控件中所有相关的Run元素
        /// </summary>
        private List<Run> FindAllTargetRuns(SdtElement control)
        {
            List<Run> runs = new List<Run>();

            // 尝试从内容容器中查找Run
            OpenXmlElement? content = FindContentContainer(control);
            if (content != null)
            {
                if (content is SdtContentBlock || content is SdtContentCell)
                {
                    // 块级控件：获取段落中的所有Run
                    runs = content.Descendants<Run>().ToList();
                }
                else if (content is SdtContentRun)
                {
                    // 行内控件：获取所有Run
                    runs = content.Descendants<Run>().ToList();
                }
            }
            else
            {
                // 直接从控件中查找Run
                runs = control.Descendants<Run>().ToList();
            }

            _logger.LogDebug($"在内容控件中找到 {runs.Count} 个Run元素");
            return runs;
        }

        /// <summary>
        /// 创建带格式的段落
        /// </summary>
        private Paragraph CreateParagraphWithFormattedText(string text)
        {
            Paragraph paragraph = new Paragraph();
            List<Run> runs = CreateFormattedRuns(text);
            foreach (Run run in runs)
            {
                paragraph.AppendChild(run);
            }
            return paragraph;
        }

        /// <summary>
        /// 创建格式化的Run元素列表
        /// </summary>
        private List<Run> CreateFormattedRuns(string text)
        {
            List<Run> runs = new List<Run>();

            if (string.IsNullOrEmpty(text))
                return runs;

            // 按换行符分割文本
            string[] lines = text.Split(["\n"], StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 创建带红色格式的Run
                Run run = new Run();
                RunProperties runProperties = new RunProperties();
                Color color = new Color() { Val = "FF0000" }; // 红色
                runProperties.AppendChild(color);
                run.AppendChild(runProperties);

                // 添加文本内容
                Text text_element = new Text(line);
                run.AppendChild(text_element);
                runs.Add(run);

                // 如果不是最后一行，添加换行符
                if (i < lines.Length - 1)
                {
                    Run breakRun = new Run(new Break());
                    runs.Add(breakRun);
                }
            }

            return runs;
        }

        /// <summary>
        /// 创建格式化的文本元素列表
        /// </summary>
        private List<OpenXmlElement> CreateFormattedTextElements(string text)
        {
            List<OpenXmlElement> elements = new List<OpenXmlElement>();

            if (string.IsNullOrEmpty(text))
                return elements;

            // 按换行符分割文本
            string[] lines = text.Split(["\n"], StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 设置文本颜色为红色
                RunProperties runProperties = new RunProperties();
                Color color = new Color() { Val = "FF0000" }; // 红色
                runProperties.AppendChild(color);
                elements.Add(runProperties);

                // 添加文本内容
                Text textElement = new Text(line);
                elements.Add(textElement);

                // 如果不是最后一行，添加换行符
                if (i < lines.Length - 1)
                {
                    elements.Add(new Break());
                }
            }

            return elements;
        }
    }
}