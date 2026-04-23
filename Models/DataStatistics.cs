using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// 数据统计信息
    /// </summary>
    public class DataStatistics
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 字段列表
        /// </summary>
        public List<string> Fields { get; set; } = new List<string>();

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSizeBytes { get; set; }
    }
}
