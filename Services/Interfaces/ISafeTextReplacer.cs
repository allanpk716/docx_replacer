using DocumentFormat.OpenXml.Wordprocessing;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 安全文本替换服务接口
    /// </summary>
    public interface ISafeTextReplacer
    {
        /// <summary>
        /// 安全地替换内容控件中的文本，保留结构
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="newText">新文本</param>
        void ReplaceTextInControl(SdtElement control, string newText);
    }
}
