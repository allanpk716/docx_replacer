using System.Collections.Generic;
using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Utils;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 数据解析服务接口
    /// </summary>
    public interface IDataParser
    {
        /// <summary>
        /// 解析JSON数据文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>解析后的数据列表</returns>
        Task<List<Dictionary<string, object>>> ParseJsonFileAsync(string filePath);

        /// <summary>
        /// 解析JSON字符串
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        /// <returns>解析后的数据列表</returns>
        List<Dictionary<string, object>> ParseJsonString(string jsonContent);

        /// <summary>
        /// 验证JSON数据格式
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateJsonFileAsync(string filePath);

        /// <summary>
        /// 验证JSON字符串格式
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateJsonString(string jsonContent);

        /// <summary>
        /// 获取JSON数据预览
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <param name="maxRecords">最大记录数</param>
        /// <returns>预览数据</returns>
        Task<List<Dictionary<string, object>>> GetDataPreviewAsync(string filePath, int maxRecords = 10);

        /// <summary>
        /// 获取JSON数据统计信息
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>统计信息</returns>
        Task<DataStatistics> GetDataStatisticsAsync(string filePath);
    }

    /// <summary>
    /// 数据统计信息
    /// </summary>
    public class DataStatistics
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 字段列表
        /// </summary>
        public List<string> Fields { get; set; } = new List<string>();

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// 数据类型分布
        /// </summary>
        public Dictionary<string, string> FieldTypes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 空值统计
        /// </summary>
        public Dictionary<string, int> NullCounts { get; set; } = new Dictionary<string, int>();
    }
}