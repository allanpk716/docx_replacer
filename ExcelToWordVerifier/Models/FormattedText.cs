using System.Collections.Generic;
using System.Linq;

namespace ExcelToWordVerifier.Models;

/// <summary>
/// 表示带格式的文本，由多个文本运行组成
/// </summary>
public class FormattedText
{
    /// <summary>
    /// 文本运行集合
    /// </summary>
    public List<TextRun> Runs { get; set; } = new();

    /// <summary>
    /// 获取纯文本内容（不包含格式）
    /// </summary>
    public string PlainText => string.Join("", Runs.Select(r => r.Text));

    public override string ToString()
    {
        return $"FormattedText: {PlainText} ({Runs.Count} runs)";
    }
}
