using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DocuFiller.Utils;

namespace DocuFiller.Models
{
    /// <summary>
    /// 文件夹批量处理请求
    /// </summary>
    public class FolderProcessRequest
    {
        /// <summary>
        /// 模板文件夹路径
        /// </summary>
        [Required(ErrorMessage = "模板文件夹路径不能为空")]
        public string TemplateFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 数据文件路径
        /// </summary>
        [Required(ErrorMessage = "数据文件路径不能为空")]
        public string DataFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录路径
        /// </summary>
        [Required(ErrorMessage = "输出目录路径不能为空")]
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否包含子文件夹
        /// </summary>
        public bool IncludeSubfolders { get; set; } = true;

        /// <summary>
        /// 是否保持目录结构
        /// </summary>
        public bool PreserveDirectoryStructure { get; set; } = true;

        /// <summary>
        /// 是否创建时间戳文件夹
        /// </summary>
        public bool CreateTimestampFolder { get; set; } = true;

        /// <summary>
        /// 输出文件名模式
        /// </summary>
        public string OutputFileNamePattern { get; set; } = "{index}_{originalName}";

        /// <summary>
        /// 是否覆盖已存在的文件
        /// </summary>
        public bool OverwriteExistingFiles { get; set; } = false;

        /// <summary>
        /// 要处理的文件列表（如果为空则处理所有找到的docx文件）
        /// </summary>
        public List<string> SelectedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 模板文件信息列表
        /// </summary>
        public List<FileInfo> TemplateFiles { get; set; } = new List<FileInfo>();

        /// <summary>
        /// 验证请求参数
        /// </summary>
        /// <returns>验证结果</returns>
        public Utils.ValidationResult Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TemplateFolderPath))
                errors.Add("模板文件夹路径不能为空");

            if (string.IsNullOrWhiteSpace(DataFilePath))
                errors.Add("数据文件路径不能为空");

            if (string.IsNullOrWhiteSpace(OutputDirectory))
                errors.Add("输出目录路径不能为空");

            if (!System.IO.Directory.Exists(TemplateFolderPath))
                errors.Add("模板文件夹不存在");

            if (!System.IO.File.Exists(DataFilePath))
                errors.Add("数据文件不存在");

            var result = new Utils.ValidationResult { IsValid = errors.Count == 0 };
            foreach (var error in errors)
            {
                result.AddError(error);
            }
            return result;
        }
    }
}