using System;
using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// 文档处理结果模型
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// 处理是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 处理开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 处理结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 总处理时间
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 成功处理的记录数
        /// </summary>
        public int SuccessfulRecords { get; set; }

        /// <summary>
        /// 已处理的记录数（兼容属性）
        /// </summary>
        public int ProcessedRecords => SuccessfulRecords;

        /// <summary>
        /// 失败的记录数
        /// </summary>
        public int FailedRecords => TotalRecords - SuccessfulRecords;

        /// <summary>
        /// 总处理数（兼容属性）
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// 总失败数（兼容属性）
        /// </summary>
        public int TotalFailed { get; set; }

        /// <summary>
        /// 错误消息（兼容属性）
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 已处理的文件列表
        /// </summary>
        public List<string> ProcessedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 失败的文件列表
        /// </summary>
        public List<string> FailedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalRecords > 0 ? (double)SuccessfulRecords / TotalRecords * 100 : 0;

        /// <summary>
        /// 生成的文件列表
        /// </summary>
        public List<string> GeneratedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 详细消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 添加错误信息
        /// </summary>
        /// <param name="error">错误信息</param>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add($"[{DateTime.Now:HH:mm:ss}] {error}");
            }
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        /// <param name="warning">警告信息</param>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add($"[{DateTime.Now:HH:mm:ss}] {warning}");
            }
        }

        /// <summary>
        /// 添加生成的文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void AddGeneratedFile(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                GeneratedFiles.Add(filePath);
            }
        }
    }
}