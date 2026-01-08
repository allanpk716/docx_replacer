using System.Collections.Generic;
using ExcelToWordVerifier.Models;

namespace ExcelToWordVerifier.Services;

/// <summary>
/// Word 写入服务接口
/// </summary>
public interface IWordWriter
{
    /// <summary>
    /// 将带格式的文本写入 Word 文档
    /// </summary>
    /// <param name="formattedText">带格式的文本对象</param>
    /// <param name="outputPath">输出 Word 文档路径</param>
    void Write(FormattedText formattedText, string outputPath);

    /// <summary>
    /// 将多个带格式的文本写入 Word 文档
    /// </summary>
    /// <param name="formattedTexts">带格式的文本对象列表</param>
    /// <param name="outputPath">输出 Word 文档路径</param>
    void WriteMany(List<FormattedText> formattedTexts, string outputPath);
}
