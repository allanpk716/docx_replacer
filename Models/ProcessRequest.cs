using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// 文档处理请求模型
    /// </summary>
    public class ProcessRequest
    {
        /// <summary>
        /// Word模板文件路径
        /// </summary>
        public string TemplateFilePath { get; set; } = string.Empty;

        /// <summary>
        /// JSON数据文件路径
        /// </summary>
        public string DataFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录路径
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 输出文件名模式
        /// </summary>
        public string OutputFileNamePattern { get; set; } = "{index}_{originalName}";

        /// <summary>
        /// 是否覆盖已存在的文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// 验证请求参数是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(TemplateFilePath) &&
                   !string.IsNullOrWhiteSpace(DataFilePath) &&
                   !string.IsNullOrWhiteSpace(OutputDirectory) &&
                   !string.IsNullOrWhiteSpace(OutputFileNamePattern);
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <returns>错误信息列表</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TemplateFilePath))
                errors.Add("模板文件路径不能为空");

            if (string.IsNullOrWhiteSpace(DataFilePath))
                errors.Add("数据文件路径不能为空");

            if (string.IsNullOrWhiteSpace(OutputDirectory))
                errors.Add("输出目录不能为空");

            if (string.IsNullOrWhiteSpace(OutputFileNamePattern))
                errors.Add("输出文件名模式不能为空");

            return errors;
        }
    }
}