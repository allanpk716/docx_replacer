using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// Excel 数据解析服务接口
    /// </summary>
    public interface IExcelDataParser
    {
        /// <summary>
        /// 解析 Excel 数据文件
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>解析后的数据字典（关键词 -> 格式化值）</returns>
        Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证 Excel 数据文件
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <returns>验证结果</returns>
        Task<ExcelValidationResult> ValidateExcelFileAsync(string filePath);

        /// <summary>
        /// 获取 Excel 数据预览
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <param name="maxRows">最大行数</param>
        /// <returns>预览数据</returns>
        Task<List<Dictionary<string, FormattedCellValue>>> GetDataPreviewAsync(string filePath, int maxRows = 10);

        /// <summary>
        /// 获取 Excel 数据统计信息
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <returns>统计信息</returns>
        Task<ExcelFileSummary> GetDataStatisticsAsync(string filePath);
    }
}
