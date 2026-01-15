using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocuFiller.Utils
{
    /// <summary>
    /// OpenXML 表格单元格检测和操作辅助工具类
    /// </summary>
    public static class OpenXmlTableCellHelper
    {
        /// <summary>
        /// 检测指定元素是否位于表格单元格内
        /// </summary>
        /// <param name="element">要检测的 OpenXML 元素</param>
        /// <returns>如果元素在表格单元格内返回 true，否则返回 false</returns>
        public static bool IsInTableCell(OpenXmlElement? element)
        {
            if (element == null)
            {
                return false;
            }

            return GetParentTableCell(element) != null;
        }

        /// <summary>
        /// 获取指定元素所在的父级表格单元格
        /// </summary>
        /// <param name="element">要查询的 OpenXML 元素</param>
        /// <returns>找到的表格单元格对象，如果不在表格内则返回 null</returns>
        public static TableCell? GetParentTableCell(OpenXmlElement? element)
        {
            if (element == null)
            {
                return null;
            }

            // 向上遍历父元素链，查找 TableCell
            var current = element;
            while (current != null)
            {
                if (current is TableCell tableCell)
                {
                    return tableCell;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// 获取或创建单元格中的唯一段落
        /// </summary>
        /// <remarks>
        /// 此方法确保单元格中只有一个段落，多余的段落会被删除。
        /// 如果单元格为空，则创建一个新的段落。
        /// 这对于保持表格单元格内容的正确格式非常重要。
        /// </remarks>
        /// <param name="cell">表格单元格</param>
        /// <returns>单元格中的唯一段落</returns>
        public static Paragraph GetOrCreateSingleParagraph(TableCell cell)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            // 获取单元格中的所有段落
            var paragraphs = cell.Elements<Paragraph>().ToList();

            // 如果没有段落，创建一个新的
            if (paragraphs.Count == 0)
            {
                var newParagraph = new Paragraph();
                cell.Append(newParagraph);
                return newParagraph;
            }

            // 如果有多个段落，删除除第一个之外的所有段落
            if (paragraphs.Count > 1)
            {
                for (int i = paragraphs.Count - 1; i >= 1; i--)
                {
                    paragraphs[i].Remove();
                }
            }

            // 返回第一个（也是唯一的）段落
            return paragraphs[0];
        }
    }
}
