namespace DocuFiller.Models
{
    /// <summary>
    /// 输入源类型枚举
    /// </summary>
    public enum InputSourceType
    {
        /// <summary>
        /// 未选择
        /// </summary>
        None,

        /// <summary>
        /// 单个文件
        /// </summary>
        SingleFile,

        /// <summary>
        /// 文件夹（包含子文件夹）
        /// </summary>
        Folder
    }
}
