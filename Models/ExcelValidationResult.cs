using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// Excel 文件验证结果
    /// </summary>
    public class ExcelValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 文件摘要信息
        /// </summary>
        public ExcelFileSummary Summary { get; set; } = new();

        /// <summary>
        /// 添加错误信息
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}
