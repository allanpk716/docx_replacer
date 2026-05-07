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
            bool containsTableCell = control.Descendants<TableCell>().Any();
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 是否在表格中: {isInTableCell}");
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 是否包含表格单元格: {containsTableCell}");

            // 3. 根据位置选择替换策略
            if (isInTableCell)
            {
                _logger.LogDebug("[SafeTextReplacer] 检测到表格单元格内容控件，使用安全替换策略");
                ReplaceTextInTableCell(contentContainer, newText, control);
            }
            else if (containsTableCell)
            {
                _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 不在单元格祖先链中但包含 TableCell，使用包装单元格替换策略");
                ReplaceTextInWrappedTableCell(control, newText);
            }
            else
            {
                _logger.LogDebug("[SafeTextReplacer] 非表格单元格内容控件，使用标准替换策略");
                ReplaceTextStandard(contentContainer, newText, control);
            }

            // 4. 替换完成后，确认控件的样式（修复旧程序遗留的格式覆盖问题）
            ApplyControlStyleToRuns(control);

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 替换完成");
        }

        /// <summary>
        /// 将控件 sdtPr/rPr 中定义的样式（如 rStyle）应用到内容中的所有 Run，
        /// 确保替换后的文本遵循控件预期的样式（如"样式1"），而不是旧程序遗留的格式覆盖（如标红色）。
        /// </summary>
        /// <param name="control">内容控件元素</param>
        private void ApplyControlStyleToRuns(SdtElement control)
        {
            // 从控件的 sdtPr/rPr 中获取预期的 rStyle 和格式信息
            var sdtPr = control.SdtProperties;
            if (sdtPr == null) return;

            var controlRPr = sdtPr.GetFirstChild<RunProperties>();
            if (controlRPr == null) return;

            // 提取控件级别的 rStyle（字符样式引用，如"样式1 Char"）
            var controlRunStyle = controlRPr.GetFirstChild<RunStyle>();
            if (controlRunStyle?.Val?.Value == null) return;

            string rStyle = controlRunStyle.Val.Value;
            string tag = sdtPr.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 检测到 rStyle='{rStyle}'，开始确认控件样式");

            // 找到内容容器中的所有 Run
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null) return;

            var runs = contentContainer.Descendants<Run>()
                .Where(r => ReferenceEquals(GetNearestAncestorSdt(r), control))
                .ToList();

            foreach (var run in runs)
            {
                var runProps = run.RunProperties;
                if (runProps == null)
                {
                    // Run 没有 RunProperties，创建一个并设置 rStyle
                    runProps = new RunProperties();
                    run.InsertAt(runProps, 0);
                }

                // 确保设置了正确的 rStyle
                var existingStyle = runProps.GetFirstChild<RunStyle>();
                if (existingStyle == null || existingStyle.Val?.Value != rStyle)
                {
                    if (existingStyle != null)
                    {
                        existingStyle.Val = rStyle;
                    }
                    else
                    {
                        runProps.InsertAt(new RunStyle() { Val = rStyle }, 0);
                    }

                    // 移除旧程序遗留的直接格式覆盖（如标红色等），
                    // 这些直接格式会覆盖样式定义，导致控件预期的样式不生效
                    RemoveDirectFormatOverrides(runProps);

                    _logger.LogDebug($"[SafeTextReplacer] 已为控件 '{tag}' 的 Run 应用 rStyle='{rStyle}' 并清除直接格式覆盖");
                }
            }
        }

        /// <summary>
        /// 移除 Run 中旧程序遗留的直接格式覆盖（如颜色、字体大小等），
        /// 这些格式会阻止控件的 rStyle 样式生效。
        /// 保留必要的属性（如上标/下标、语言设置等）。
        /// </summary>
        /// <param name="runProps">Run 的格式属性</param>
        private static void RemoveDirectFormatOverrides(RunProperties runProps)
        {
            // 移除可能由旧程序遗留的直接格式覆盖属性
            // 这些属性在存在 rStyle 时会覆盖样式定义
            var color = runProps.Color;
            if (color != null)
            {
                // 只移除非自动颜色的显式颜色设置
                if (color.Val?.Value != null && color.Val.Value != "auto")
                {
                    color.Remove();
                }
            }

            // 移除可能存在的其他旧程序格式覆盖
            // 注意：不删除 rFonts、sz 等格式属性，因为它们可能是有意设置的
            // 只移除明确由旧替换程序添加的标红色（color）即可
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

            SetRunTextWithLineBreaks(firstRun, newText);

            // 移除其他多余的 Run
            for (int i = 1; i < runs.Count; i++)
            {
                runs[i].Remove();
            }

            _logger.LogDebug("表格单元格安全替换: 保留第一个 Run，删除 {RemovedCount} 个多余 Run", runs.Count - 1);
        }

        /// <summary>
        /// 表格单元格中块级控件的安全替换策略
        /// 关键：确保 SdtContentBlock 容器中只有一个段落，并且该段落只有一个 Run
        /// 注意：不删除容器内的其他段落，因为它们可能属于其他控件
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
                var newParagraph = new Paragraph(new Run());
                contentContainer.AppendChild(newParagraph);
                _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 创建了新段落和 Run");
                paragraphs = [newParagraph];
            }

            ReplaceTextPreservingStructure(contentContainer, newText, control);

            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 替换了块级控件内容为 '{newText.Substring(0, Math.Min(50, newText.Length))}...'");
            _logger.LogInformation($"[SafeTextReplacer] 控件 '{tag}' 容器内仍有 {contentContainer.Elements<Paragraph>().Count()} 个段落（未删除）");
        }

        /// <summary>
        /// 标准替换策略（非表格单元格）
        /// </summary>
        /// <param name="contentContainer">内容容器</param>
        /// <param name="newText">新文本</param>
        /// <param name="control">内容控件</param>
        private void ReplaceTextStandard(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            ReplaceTextByRebuildingContent(contentContainer, newText);
        }

        private void ReplaceTextByRebuildingContent(OpenXmlElement contentContainer, string newText)
        {
            var baseRunProperties = contentContainer.Descendants<Run>()
                .Select(static r => r.RunProperties)
                .FirstOrDefault(static rp => rp != null)?
                .CloneNode(true) as RunProperties;

            contentContainer.RemoveAllChildren();

            if (contentContainer is SdtContentBlock || contentContainer is SdtContentCell)
            {
                var paragraph = new Paragraph();
                paragraph.AppendChild(CreateRunWithLineBreaks(newText, baseRunProperties));
                contentContainer.AppendChild(paragraph);
                return;
            }

            if (contentContainer is SdtContentRun)
            {
                contentContainer.AppendChild(CreateRunWithLineBreaks(newText, baseRunProperties));
                return;
            }

            contentContainer.AppendChild(CreateRunWithLineBreaks(newText, baseRunProperties));
        }

        private static Run CreateRunWithLineBreaks(string text, RunProperties? baseRunProperties)
        {
            var run = new Run();
            if (baseRunProperties != null)
            {
                run.AppendChild((RunProperties)baseRunProperties.CloneNode(true));
            }
            SetRunTextWithLineBreaks(run, text);
            return run;
        }

        private void ReplaceTextPreservingStructure(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            var candidateRuns = contentContainer.Descendants<Run>()
                .Where(r => ReferenceEquals(GetNearestAncestorSdt(r), control))
                .ToList();

            Run targetRun;
            if (candidateRuns.Count > 0)
            {
                targetRun = candidateRuns[0];
            }
            else
            {
                if (contentContainer is SdtContentBlock blockContent)
                {
                    var paragraph = blockContent.Elements<Paragraph>().FirstOrDefault();
                    if (paragraph == null)
                    {
                        paragraph = blockContent.AppendChild(new Paragraph());
                    }
                    targetRun = paragraph.AppendChild(new Run());
                }
                else
                {
                    targetRun = contentContainer.AppendChild(new Run());
                }
            }

            SetRunTextWithLineBreaks(targetRun, newText);

            foreach (var run in candidateRuns)
            {
                if (ReferenceEquals(run, targetRun))
                {
                    continue;
                }

                ClearRunTextContent(run);
            }
        }

        private void ReplaceTextInWrappedTableCell(SdtElement control, string newText)
        {
            var targetCell = control.Descendants<TableCell>().FirstOrDefault();
            if (targetCell == null)
            {
                string tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
                _logger.LogWarning($"[SafeTextReplacer] 控件 '{tag}' 未找到可替换的 TableCell，跳过包装单元格替换");
                return;
            }

            var candidateRuns = targetCell.Descendants<Run>()
                .Where(r => ReferenceEquals(GetNearestAncestorSdt(r), control))
                .ToList();

            Run targetRun;
            if (candidateRuns.Count > 0)
            {
                targetRun = candidateRuns[0];
            }
            else
            {
                var paragraph = targetCell.Elements<Paragraph>().FirstOrDefault();
                if (paragraph == null)
                {
                    paragraph = targetCell.AppendChild(new Paragraph());
                }

                targetRun = paragraph.AppendChild(new Run());
            }

            SetRunTextWithLineBreaks(targetRun, newText);

            var targetRunTextSet = targetRun.Descendants<Text>().ToHashSet();
            var textsToClear = targetCell.Descendants<Text>()
                .Where(t => ReferenceEquals(GetNearestAncestorSdt(t), control))
                .Where(t => !targetRunTextSet.Contains(t))
                .ToList();

            foreach (var text in textsToClear)
            {
                text.Text = string.Empty;
            }

            var removableElements = targetCell.Descendants()
                .Where(e => ReferenceEquals(GetNearestAncestorSdt(e), control))
                .Where(e => e is Break || e is TabChar || e is CarriageReturn)
                .Where(e => !ReferenceEquals(e.Parent, targetRun))
                .ToList();

            foreach (var element in removableElements)
            {
                element.Remove();
            }
        }

        private static SdtElement? GetNearestAncestorSdt(OpenXmlElement element)
        {
            var current = element.Parent;
            while (current != null)
            {
                if (current is SdtElement sdt)
                {
                    return sdt;
                }
                current = current.Parent;
            }
            return null;
        }

        private static void SetRunTextWithLineBreaks(Run run, string text)
        {
            var runProps = run.RunProperties?.CloneNode(true) as RunProperties;
            run.RemoveAllChildren();
            if (runProps != null)
            {
                run.AppendChild(runProps);
            }

            if (string.IsNullOrEmpty(text))
            {
                run.AppendChild(new Text(string.Empty) { Space = SpaceProcessingModeValues.Preserve });
                return;
            }

            var lines = text.Split(["\n"], StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                run.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
                if (i < lines.Length - 1)
                {
                    run.AppendChild(new Break());
                }
            }
        }

        private static void ClearRunTextContent(Run run)
        {
            var toRemove = run.ChildElements
                .Where(e => e is Text || e is Break || e is TabChar || e is CarriageReturn)
                .ToList();

            foreach (var element in toRemove)
            {
                element.Remove();
            }
        }
    }
}
