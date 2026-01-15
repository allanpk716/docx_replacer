using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 安全文本替换服务实现
    /// 通过精确替换文本节点而非删除重建，保留表格单元格结构
    /// </summary>
    public class SafeTextReplacer : ISafeTextReplacer
    {
        private readonly ILogger<SafeTextReplacer> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public SafeTextReplacer(ILogger<SafeTextReplacer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 安全地替换内容控件中的文本，保留结构
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="newText">新文本</param>
        public void ReplaceTextInControl(SdtElement control, string newText)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            string tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
            _logger.LogInformation($"[SafeTextReplacer] 开始替换控件 '{tag}' (类型: {control.GetType().Name}), 新文本: '{newText}'");

            // 1. 查找内容容器
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning($"[SafeTextReplacer] 控件 '{tag}' 未找到内容容器，无法替换文本");
                return;
            }

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 内容容器类型: {contentContainer.GetType().Name}");

            // 2. 检查是否在表格单元格中
            bool isInTableCell = OpenXmlTableCellHelper.IsInTableCell(control);
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 是否在表格中: {isInTableCell}");

            // 3. 根据位置选择替换策略
            if (isInTableCell)
            {
                _logger.LogDebug("[SafeTextReplacer] 检测到表格单元格内容控件，使用安全替换策略");
                ReplaceTextInTableCell(contentContainer, newText, control);
            }
            else
            {
                _logger.LogDebug("[SafeTextReplacer] 非表格单元格内容控件，使用标准替换策略");
                ReplaceTextStandard(contentContainer, newText, control);
            }

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 替换完成");
        }

        /// <summary>
        /// 查找内容控件的内容容器
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <returns>找到的内容容器，如果未找到则返回 null</returns>
        private OpenXmlElement? FindContentContainer(SdtElement control)
        {
            var runContent = control.Descendants<SdtContentRun>().FirstOrDefault();
            if (runContent != null) return runContent;

            var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();
            if (blockContent != null) return blockContent;

            var cellContent = control.Descendants<SdtContentCell>().FirstOrDefault();
            return cellContent;
        }

        /// <summary>
        /// 表格单元格安全替换策略
        /// 关键：不删除段落结构，只替换文本内容
        /// </summary>
        /// <param name="contentContainer">内容容器</param>
        /// <param name="newText">新文本</param>
        /// <param name="control">内容控件</param>
        private void ReplaceTextInTableCell(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            // 特殊处理：如果是 SdtBlock（格式文本内容控件），需要特别处理段落结构
            if (control is SdtBlock || contentContainer is SdtContentBlock)
            {
                ReplaceTextInTableCellForBlock(control, newText);
                return;
            }

            // 获取所有 Run 元素（SdtRun 等行内控件）
            var runs = contentContainer.Descendants<Run>().ToList();

            if (runs.Count == 0)
            {
                _logger.LogWarning("内容容器中没有找到 Run 元素");
                return;
            }

            // 策略：保留第一个 Run，清空其内容并设置新文本
            // 删除其他多余的 Run（但保留第一个 Run 的格式）
            var firstRun = runs[0];

            // 清空第一个 Run 的所有子元素
            firstRun.RemoveAllChildren();

            // 创建新的 Text 元素
            var textElement = new Text(newText)
            {
                Space = SpaceProcessingModeValues.Preserve
            };
            firstRun.AppendChild(textElement);

            // 移除其他多余的 Run
            for (int i = 1; i < runs.Count; i++)
            {
                runs[i].Remove();
            }

            _logger.LogDebug("表格单元格安全替换: 保留第一个 Run，删除 {RemovedCount} 个多余 Run", runs.Count - 1);
        }

        /// <summary>
        /// 表格单元格中块级控件的安全替换策略
        /// 关键：确保单元格中只有一个段落，并且该段落只有一个 Run
        /// </summary>
        /// <param name="control">内容控件</param>
        /// <param name="newText">新文本</param>
        private void ReplaceTextInTableCellForBlock(SdtElement control, string newText)
        {
            string tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
            _logger.LogInformation($"[SafeTextReplacer] 处理块级控件 '{tag}', 使用表格单元格块级替换策略");

            // 获取内容容器
            var contentContainer = control.Descendants<SdtContentBlock>().FirstOrDefault();
            if (contentContainer == null)
            {
                _logger.LogWarning($"[SafeTextReplacer] 控件 '{tag}' 未找到 SdtContentBlock 容器");
                return;
            }

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 找到 SdtContentBlock 容器");

            // 获取容器中的所有段落
            var paragraphs = contentContainer.Elements<Paragraph>().ToList();
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 容器中有 {paragraphs.Count} 个段落");

            if (paragraphs.Count == 0)
            {
                // 如果没有段落，创建一个新的
                var newParagraph = new Paragraph(
                    new Run(new Text(newText) { Space = SpaceProcessingModeValues.Preserve })
                );
                contentContainer.AppendChild(newParagraph);
                _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 创建了新段落");
                return;
            }

            // 保留第一个段落，删除其他段落（但保留第一个段落的 Run 结构）
            var firstParagraph = paragraphs[0];
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 第一个段落有 {firstParagraph.ChildElements.Count} 个子元素");

            // 清空第一个段落的所有内容
            firstParagraph.RemoveAllChildren();

            // 在第一个段落中创建新的 Run
            var newRun = new Run();
            var runProperties = new RunProperties();
            var color = new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "FF0000" }; // 红色
            runProperties.AppendChild(color);
            newRun.AppendChild(runProperties);

            var textElement = new Text(newText)
            {
                Space = SpaceProcessingModeValues.Preserve
            };
            newRun.AppendChild(textElement);

            firstParagraph.AppendChild(newRun);

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 在第一个段落中设置了新文本");

            // 移除其他多余的段落
            int removedCount = 0;
            for (int i = 1; i < paragraphs.Count; i++)
            {
                paragraphs[i].Remove();
                removedCount++;
            }

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 删除了 {removedCount} 个多余段落, 现在容器中有 {contentContainer.Elements<Paragraph>().Count()} 个段落");
        }

        /// <summary>
        /// 标准替换策略（非表格单元格）
        /// </summary>
        /// <param name="contentContainer">内容容器</param>
        /// <param name="newText">新文本</param>
        /// <param name="control">内容控件</param>
        private void ReplaceTextStandard(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            // 清空容器并重新创建内容
            contentContainer.RemoveAllChildren();

            if (control is SdtBlock)
            {
                // 块级控件：创建带格式的段落
                var paragraph = new Paragraph();
                var runs = CreateFormattedRuns(newText);
                foreach (var run in runs)
                {
                    paragraph.AppendChild(run);
                }
                contentContainer.AppendChild(paragraph);
            }
            else
            {
                // 行内控件：创建带格式的 Run
                var runs = CreateFormattedRuns(newText);
                foreach (var run in runs)
                {
                    contentContainer.AppendChild(run);
                }
            }
        }

        /// <summary>
        /// 创建格式化的 Run 元素列表（处理换行符）
        /// </summary>
        /// <param name="text">要格式化的文本</param>
        /// <returns>格式化的 Run 列表</returns>
        private List<Run> CreateFormattedRuns(string text)
        {
            var runs = new List<Run>();

            if (string.IsNullOrEmpty(text))
                return runs;

            // 按换行符分割文本
            string[] lines = text.Split(["\n"], StringSplitOptions.None);
            _logger.LogDebug($"文本分割为 {lines.Length} 行");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 创建带红色格式的Run
                var run = new Run();
                var runProperties = new RunProperties();
                var color = new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "FF0000" }; // 红色
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
                }
            }

            return runs;
        }
    }
}
