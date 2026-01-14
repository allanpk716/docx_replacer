using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// Excel 文件摘要信息
    /// </summary>
    public class ExcelFileSummary
    {
        /// <summary>
        /// 总行数
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// 有效关键词行数
        /// </summary>
        public int ValidKeywordRows { get; set; }

        /// <summary>
        /// 重复的关键词列表
        /// </summary>
        public List<string> DuplicateKeywords { get; set; } = new();

        /// <summary>
        /// 格式不正确的关键词列表
        /// </summary>
        public List<string> InvalidFormatKeywords { get; set; } = new();
    }
}
