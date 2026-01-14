using System.Collections.Generic;
using System.Linq;

namespace DocuFiller.Models
{
    /// <summary>
    /// 表示带格式的单元格值
    /// </summary>
    public class FormattedCellValue
    {
        /// <summary>
        /// 纯文本内容（用于验证和显示）
        /// </summary>
        public string PlainText => string.Join("", Fragments.Select(f => f.Text));

        /// <summary>
        /// 文本片段列表，每个片段包含内容和格式信息
        /// </summary>
        public List<TextFragment> Fragments { get; set; } = new();

        /// <summary>
        /// 从纯文本创建单个片段的 FormattedCellValue
        /// </summary>
        public static FormattedCellValue FromPlainText(string text)
        {
            return new FormattedCellValue
            {
                Fragments = new List<TextFragment>
                {
                    new TextFragment { Text = text ?? string.Empty }
                }
            };
        }

        public override string ToString()
        {
            return $"FormattedCellValue: {PlainText} ({Fragments.Count} fragments)";
        }
    }
}
