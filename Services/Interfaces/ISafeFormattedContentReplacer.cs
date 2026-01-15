using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 安全格式化内容替换服务接口
    /// 专门处理表格单元格中的富文本内容替换（如上标、下标等格式）
    /// </summary>
    public interface ISafeFormattedContentReplacer
    {
        /// <summary>
        /// 安全地替换内容控件中的格式化内容，保留结构和格式
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="formattedValue">带格式的新内容</param>
        void ReplaceFormattedContentInControl(SdtElement control, FormattedCellValue formattedValue);
    }
}
