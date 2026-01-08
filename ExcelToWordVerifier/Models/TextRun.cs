using System.Collections.Generic;

namespace ExcelToWordVerifier.Models;

/// <summary>
/// 表示单个文本运行，包含文本内容和格式信息
/// </summary>
public class TextRun
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 是否为粗体
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// 是否为斜体
    /// </summary>
    public bool IsItalic { get; set; }

    /// <summary>
    /// 是否有下划线
    /// </summary>
    public bool IsUnderline { get; set; }

    /// <summary>
    /// 是否为上标
    /// </summary>
    public bool IsSuperscript { get; set; }

    /// <summary>
    /// 是否为下标
    /// </summary>
    public bool IsSubscript { get; set; }

    public override string ToString()
    {
        var formats = new List<string>();
        if (IsBold) formats.Add("粗体");
        if (IsItalic) formats.Add("斜体");
        if (IsUnderline) formats.Add("下划线");
        if (IsSuperscript) formats.Add("上标");
        if (IsSubscript) formats.Add("下标");

        var formatStr = formats.Count > 0 ? $" [{string.Join(", ", formats)}]" : "";
        return $"\"{Text}\"{formatStr}";
    }
}
