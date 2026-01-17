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

            // 1. 查找内容容器
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning("未找到内容容器，无法替换内容");
                return;
            }

            // 2. 获取所有 Run 元素
            var runs = contentContainer.Descendants<Run>().ToList();

            if (runs.Count == 0)
            {
                _logger.LogWarning("内容容器中没有找到 Run 元素");
                return;
            }

            var baseRunProperties = GetBaseRunProperties(runs);
            _logger.LogDebug("格式化替换: 基础RunProperties存在: {HasBaseRunProperties}", baseRunProperties != null);

            // 3. 策略：删除所有现有 Run，然后为每个片段创建新的 Run
            // 移除所有现有的 Run
            foreach (var run in runs)
            {
                run.Remove();
            }

            // 为每个格式化片段创建新的 Run
            foreach (var fragment in formattedValue.Fragments)
            {
                var textElement = new Text(fragment.Text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                };

                var newRun = new Run();

                var runProperties = CloneRunProperties(baseRunProperties);

                if (fragment.IsSuperscript || fragment.IsSubscript)
                {
                    if (fragment.IsSuperscript)
                    {
                        runProperties ??= new RunProperties();
                        runProperties.VerticalTextAlignment = new VerticalTextAlignment { Val = VerticalPositionValues.Superscript };
                    }
                    else if (fragment.IsSubscript)
                    {
                        runProperties ??= new RunProperties();
                        runProperties.VerticalTextAlignment = new VerticalTextAlignment { Val = VerticalPositionValues.Subscript };
                    }
                }
                else if (runProperties?.VerticalTextAlignment != null)
                {
                    runProperties.VerticalTextAlignment = null;
                }

                if (runProperties != null)
                {
                    newRun.AppendChild(runProperties);
                }

                newRun.AppendChild(textElement);
                contentContainer.AppendChild(newRun);
            }

            _logger.LogDebug("格式化内容替换完成: 保留了 {FragmentCount} 个片段，删除了 {RemovedCount} 个多余 Run",
                formattedValue.Fragments.Count, runs.Count - 1);
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
