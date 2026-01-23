using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 内容控件解包处理器
    /// 负责将内容控件正常化，移除 SdtElement 包装，保留原有内容
    /// </summary>
    public class CleanupControlProcessor
    {
        private readonly ILogger<CleanupControlProcessor> _logger;

        public CleanupControlProcessor(ILogger<CleanupControlProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理文档中的所有内容控件
        /// </summary>
        /// <param name="document">WordprocessingDocument 对象</param>
        /// <returns>解包的控件数量</returns>
        public int ProcessControls(WordprocessingDocument document)
        {
            int controlsUnwrapped = 0;

            if (document.MainDocumentPart?.Document == null)
            {
                _logger.LogWarning("文档主体不存在，无法处理内容控件");
                return 0;
            }

            // 处理文档主体中的控件
            controlsUnwrapped += ProcessControlsInPart(document.MainDocumentPart.Document, "正文");

            // 处理页眉中的控件
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    controlsUnwrapped += ProcessControlsInPart(headerPart.Header, "页眉");
                }
            }

            // 处理页脚中的控件
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    controlsUnwrapped += ProcessControlsInPart(footerPart.Footer, "页脚");
                }
            }

            _logger.LogInformation($"共解包 {controlsUnwrapped} 个内容控件");
            return controlsUnwrapped;
        }

        /// <summary>
        /// 处理指定部分中的内容控件
        /// </summary>
        /// <param name="partRoot">OpenXmlPartRootElement 对象</param>
        /// <param name="location">位置描述（用于日志）</param>
        /// <returns>解包的控件数量</returns>
        private int ProcessControlsInPart(OpenXmlPartRootElement partRoot, string location)
        {
            int count = 0;
            var allControls = partRoot.Descendants<SdtElement>().ToList();

            _logger.LogDebug($"在 {location} 中找到 {allControls.Count} 个内容控件");

            foreach (var control in allControls)
            {
                try
                {
                    UnwrapControl(control);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"解包控件时发生异常 ({location}): {ex.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// 解包单个内容控件
        /// </summary>
        /// <param name="sdtElement">内容控件元素</param>
        private void UnwrapControl(SdtElement sdtElement)
        {
            bool isInTableCell = OpenXmlTableCellHelper.IsInTableCell(sdtElement);
            bool containsTableCell = sdtElement.Descendants<TableCell>().Any();

            _logger.LogDebug($"解包控件 - 类型: {sdtElement.GetType().Name}, 在表格中: {isInTableCell}, 包含单元格: {containsTableCell}");

            // 场景2：控件包装整个单元格
            if (containsTableCell && !isInTableCell)
            {
                UnwrapWrappedTableCell(sdtElement);
            }
            // 场景1和3：普通解包
            else
            {
                UnwrapStandard(sdtElement);
            }
        }

        /// <summary>
        /// 解包包装表格单元格的控件
        /// 关键：保留 TableCell 结构，只移除 SdtCell 包装
        /// </summary>
        /// <param name="sdtElement">包装表格单元格的内容控件</param>
        private void UnwrapWrappedTableCell(SdtElement sdtElement)
        {
            // 找到被包装的 TableCell
            var wrappedCell = sdtElement.Descendants<TableCell>().FirstOrDefault();
            if (wrappedCell == null)
            {
                _logger.LogWarning("控件包装单元格场景：未找到 TableCell，使用普通解包");
                UnwrapStandard(sdtElement);
                return;
            }

            _logger.LogDebug("控件包装单元格场景：保留 TableCell，移除 SdtCell 包装");

            // 获取控件的父元素
            var parent = sdtElement.Parent;
            if (parent == null)
            {
                _logger.LogWarning("无法获取控件父元素");
                return;
            }

            // 将 TableCell 提升到控件外
            parent.InsertBefore(wrappedCell, sdtElement);

            // 删除控件包装
            sdtElement.Remove();

            _logger.LogDebug("已解包包装单元格控件");
        }

        /// <summary>
        /// 标准解包处理
        /// 将内容容器内的所有子元素移动到控件外，然后删除空的包装
        /// </summary>
        /// <param name="sdtElement">需要解包的内容控件</param>
        private void UnwrapStandard(SdtElement sdtElement)
        {
            // 查找内容容器
            var content = FindContentContainer(sdtElement);
            if (content == null)
            {
                _logger.LogWarning("未找到内容容器，跳过解包");
                return;
            }

            // 获取父元素
            var parent = sdtElement.Parent;
            if (parent == null)
            {
                _logger.LogWarning("无法获取控件父元素");
                return;
            }

            // 将内容容器的所有子元素移动到父元素中
            var children = content.ChildElements.ToList();
            foreach (var child in children)
            {
                parent.InsertBefore(child, sdtElement);
            }

            // 删除空的控件包装
            sdtElement.Remove();

            _logger.LogDebug($"已解包控件，移动了 {children.Count} 个子元素");
        }

        /// <summary>
        /// 查找内容控件的内容容器
        /// 支持 SdtContentRun, SdtContentBlock, SdtContentCell 三种类型
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