using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// 表示单个文本片段及其格式信息
    /// </summary>
    public class TextFragment
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 是否为上标
        /// </summary>
        public bool IsSuperscript { get; set; }

        /// <summary>
        /// 是否为下标
        /// </summary>
        public bool IsSubscript { get; set; }

        public override string ToString()
        {
            var formats = new List<string>();
            if (IsSuperscript) formats.Add("上标");
            if (IsSubscript) formats.Add("下标");
            var formatStr = formats.Count > 0 ? $" [{string.Join(", ", formats)}]" : "";
            return $"\"{Text}\"{formatStr}";
        }
    }
}
