using ExcelToWordVerifier.Models;

namespace ExcelToWordVerifier.Services;

/// <summary>
/// Excel 读取服务接口
/// </summary>
public interface IExcelReader
{
    /// <summary>
    /// 从 Excel 文件读取指定单元格的带格式文本
    /// </summary>
    /// <param name="filePath">Excel 文件路径</param>
    /// <param name="sheetName">工作表名称</param>
    /// <param name="cellAddress">单元格地址（如 "A1"）</param>
    /// <returns>带格式的文本对象</returns>
    FormattedText ReadCell(string filePath, string sheetName, string cellAddress);

    /// <summary>
    /// 读取 Excel 文件的第一个工作表的第一个单元格
    /// </summary>
    /// <param name="filePath">Excel 文件路径</param>
    /// <returns>带格式的文本对象</returns>
    FormattedText ReadFirstCell(string filePath);
}
