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

            _logger.LogDebug("开始安全替换内容控件文本: '{NewText}'", newText);

            // 1. 查找内容容器
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning("未找到内容容器，无法替换文本");
                return;
            }

            // 2. 检查是否在表格单元格中
            bool isInTableCell = OpenXmlTableCellHelper.IsInTableCell(control);

            // 3. 根据位置选择替换策略
            if (isInTableCell)
            {
                _logger.LogDebug("检测到表格单元格内容控件，使用安全替换策略");
                ReplaceTextInTableCell(contentContainer, newText, control);
            }
            else
            {
                _logger.LogDebug("非表格单元格内容控件，使用标准替换策略");
                ReplaceTextStandard(contentContainer, newText, control);
            }

            _logger.LogInformation("安全替换完成");
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
            // 获取所有 Run 元素
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
        /// 标准替换策略（非表格单元格）
        /// </summary>
        /// <param name="contentContainer">内容容器</param>
        /// <param name="newText">新文本</param>
        /// <param name="control">内容控件</param>
        private void ReplaceTextStandard(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            // 标准逻辑：清空容器并重新创建内容
            // 这个逻辑与现有代码类似，但更安全
            contentContainer.RemoveAllChildren();

            if (control is SdtBlock)
            {
                var paragraph = new Paragraph(new Run(new Text(newText)
                {
                    Space = SpaceProcessingModeValues.Preserve
                }));
                contentContainer.AppendChild(paragraph);
            }
            else
            {
                var run = new Run(new Text(newText)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
                contentContainer.AppendChild(run);
            }
        }
    }
}
