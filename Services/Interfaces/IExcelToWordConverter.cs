using System.Threading.Tasks;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// JSON 到 Excel 转换服务接口
    /// </summary>
    public interface IExcelToWordConverter
    {
        /// <summary>
        /// 将 JSON 文件转换为 Excel 文件
        /// </summary>
        /// <param name="jsonFilePath">JSON 文件路径</param>
        /// <param name="outputExcelPath">输出 Excel 文件路径</param>
        /// <returns>转换是否成功</returns>
        Task<bool> ConvertJsonToExcelAsync(string jsonFilePath, string outputExcelPath);

        /// <summary>
        /// 批量转换 JSON 文件为 Excel
        /// </summary>
        /// <param name="jsonFilePaths">JSON 文件路径列表</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>转换结果</returns>
        Task<BatchConvertResult> ConvertBatchAsync(string[] jsonFilePaths, string outputDirectory);
    }

    /// <summary>
    /// 批量转换结果
    /// </summary>
    public class BatchConvertResult
    {
        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 转换详情
        /// </summary>
        public System.Collections.Generic.List<ConvertDetail> Details { get; set; } = new();
    }

    /// <summary>
    /// 单个转换详情
    /// </summary>
    public class ConvertDetail
    {
        /// <summary>
        /// 源文件路径
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// 输出文件路径
        /// </summary>
        public string OutputFile { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
