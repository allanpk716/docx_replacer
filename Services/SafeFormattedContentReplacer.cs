using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 安全格式化内容替换服务实现
    /// 专门处理表格单元格中的富文本内容替换（如上标、下标等格式）
    /// </summary>
    public class SafeFormattedContentReplacer : ISafeFormattedContentReplacer
    {
        private readonly ILogger<SafeFormattedContentReplacer> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public SafeFormattedContentReplacer(ILogger<SafeFormattedContentReplacer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 安全地替换内容控件中的格式化内容，保留结构和格式
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="formattedValue">带格式的新内容</param>
        public void ReplaceFormattedContentInControl(SdtElement control, FormattedCellValue formattedValue)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (formattedValue == null)
                throw new ArgumentNullException(nameof(formattedValue));

            _logger.LogDebug("开始安全替换格式化内容: '{PlainText}'", formattedValue.PlainText);

            bool containsTableCell = control.Descendants<TableCell>().Any();
            if (containsTableCell)
            {
                _logger.LogDebug("检测到控件包装了 TableCell，使用包装单元格安全替换策略");
                ReplaceFormattedContentInWrappedTableCell(control, formattedValue);
                return;
            }

            // 1. 查找内容容器
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning("未找到内容容器，无法替换内容");
                return;
            }

            if (contentContainer is SdtContentRun runContainer)
            {
                ReplaceFormattedContentInRunContainer(runContainer, formattedValue);
                return;
            }

            if (contentContainer is SdtContentBlock blockContainer)
            {
                ReplaceFormattedContentInBlockContainer(control, blockContainer, formattedValue);
                return;
            }

            if (contentContainer is SdtContentCell)
            {
                _logger.LogWarning("内容容器为 SdtContentCell 但未检测到 TableCell，尝试按包装单元格策略处理");
                ReplaceFormattedContentInWrappedTableCell(control, formattedValue);
                return;
            }

            _logger.LogWarning("不支持的内容容器类型: {ContainerType}", contentContainer.GetType().Name);
        }

        private static RunProperties? GetBaseRunProperties(System.Collections.Generic.IReadOnlyList<Run> runs)
        {
            foreach (var run in runs)
            {
                if (run.RunProperties != null)
                {
                    return run.RunProperties.CloneNode(true) as RunProperties;
                }
            }

            return null;
        }

        private static RunProperties? CloneRunProperties(RunProperties? runProperties)
        {
            return runProperties?.CloneNode(true) as RunProperties;
        }

        private void ReplaceFormattedContentInRunContainer(SdtContentRun contentContainer, FormattedCellValue formattedValue)
        {
            var runs = contentContainer.Descendants<Run>().ToList();

            if (runs.Count == 0)
            {
                contentContainer.RemoveAllChildren();
                foreach (var fragment in formattedValue.Fragments)
                {
                    contentContainer.AppendChild(CreateRunForFragment(fragment, null));
                }
                return;
            }

            var baseRunProperties = GetBaseRunProperties(runs);
            _logger.LogDebug("行内控件替换: 基础RunProperties存在: {HasBaseRunProperties}", baseRunProperties != null);

            foreach (var run in runs)
            {
                run.Remove();
            }

            foreach (var fragment in formattedValue.Fragments)
            {
                contentContainer.AppendChild(CreateRunForFragment(fragment, baseRunProperties));
            }
        }

        private void ReplaceFormattedContentInBlockContainer(SdtElement control, SdtContentBlock contentContainer, FormattedCellValue formattedValue)
        {
            var candidateRuns = contentContainer.Descendants<Run>()
                .Where(r => ReferenceEquals(GetNearestAncestorSdt(r), control))
                .ToList();

            RunProperties? baseRunProperties = null;
            if (candidateRuns.Count > 0)
            {
                baseRunProperties = GetBaseRunProperties(candidateRuns);
            }

            _logger.LogDebug("块级控件替换: 基础RunProperties存在: {HasBaseRunProperties}", baseRunProperties != null);

            var paragraph = contentContainer.Elements<Paragraph>().FirstOrDefault();
            if (paragraph == null)
            {
                paragraph = contentContainer.AppendChild(new Paragraph());
            }

            foreach (var run in candidateRuns)
            {
                run.Remove();
            }

            foreach (var fragment in formattedValue.Fragments)
            {
                paragraph.AppendChild(CreateRunForFragment(fragment, baseRunProperties));
            }
        }

        private void ReplaceFormattedContentInWrappedTableCell(SdtElement control, FormattedCellValue formattedValue)
        {
            var targetCell = control.Descendants<TableCell>().FirstOrDefault();
            if (targetCell == null)
            {
                _logger.LogWarning("未找到可替换的 TableCell，跳过包装单元格替换");
                return;
            }

            var candidateRuns = targetCell.Descendants<Run>()
                .Where(r => ReferenceEquals(GetNearestAncestorSdt(r), control))
                .ToList();

            RunProperties? baseRunProperties = null;
            if (candidateRuns.Count > 0)
            {
                baseRunProperties = GetBaseRunProperties(candidateRuns);
            }

            _logger.LogDebug("包装单元格替换: 基础RunProperties存在: {HasBaseRunProperties}", baseRunProperties != null);

            var paragraph = targetCell.Elements<Paragraph>().FirstOrDefault();
            if (paragraph == null)
            {
                paragraph = targetCell.AppendChild(new Paragraph());
            }

            foreach (var run in candidateRuns)
            {
                run.Remove();
            }

            foreach (var fragment in formattedValue.Fragments)
            {
                paragraph.AppendChild(CreateRunForFragment(fragment, baseRunProperties));
            }
        }

        private Run CreateRunForFragment(TextFragment fragment, RunProperties? baseRunProperties)
        {
            var newRun = new Run();
            var runProperties = CloneRunProperties(baseRunProperties);

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
                newRun.AppendChild(runProperties);
            }

            AppendTextWithLineBreaks(newRun, fragment.Text);
            return newRun;
        }

        private static void AppendTextWithLineBreaks(Run run, string text)
        {
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
    }
}
