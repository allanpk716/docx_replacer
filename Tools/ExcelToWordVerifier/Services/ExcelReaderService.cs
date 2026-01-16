using System;
using System.IO;
using ExcelToWordVerifier.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ExcelToWordVerifier.Services;

/// <summary>
/// 使用 EPPlus 读取 Excel 文件的富文本格式
/// </summary>
public class ExcelReaderService : IExcelReader
{
    public FormattedText ReadCell(string filePath, string sheetName, string cellAddress)
    {
        var formattedText = new FormattedText();

        // 设置 EPPlus 许可上下文（社区版）
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[sheetName];
        if (worksheet == null)
        {
            throw new Exception($"工作表 '{sheetName}' 不存在");
        }

        var cell = worksheet.Cells[cellAddress];
        if (cell == null || cell.Value == null)
        {
            return formattedText;
        }

        // 读取富文本格式（EPPlus 7.x）
        if (cell.IsRichText)
        {
            foreach (var rt in cell.RichText)
            {
                var run = new TextRun
                {
                    Text = rt.Text,
                    IsBold = rt.Bold,
                    IsItalic = rt.Italic
                    // 注意：EPPlus 7.x 的 ExcelRichText 不再直接提供 Underline 属性
                    // 如果需要下划线信息，需要通过其他方式获取
                };

                // EPPlus 中上标/下标通过 VerticalAlign 属性判断
                if (rt.VerticalAlign != null)
                {
                    if (rt.VerticalAlign == ExcelVerticalAlignmentFont.Superscript)
                        run.IsSuperscript = true;
                    else if (rt.VerticalAlign == ExcelVerticalAlignmentFont.Subscript)
                        run.IsSubscript = true;
                }

                formattedText.Runs.Add(run);
            }
        }
        else
        {
            // 如果不是富文本，读取整个单元格的格式
            var run = new TextRun
            {
                Text = cell.Text ?? "",
                IsBold = cell.Style.Font.Bold,
                IsItalic = cell.Style.Font.Italic,
                IsUnderline = cell.Style.Font.UnderLine
            };

            // 检查整个单元格的垂直对齐（上标/下标）
            var vertAlign = cell.Style.Font.VerticalAlign;
            if (vertAlign == ExcelVerticalAlignmentFont.Superscript)
                run.IsSuperscript = true;
            else if (vertAlign == ExcelVerticalAlignmentFont.Subscript)
                run.IsSubscript = true;

            formattedText.Runs.Add(run);
        }

        return formattedText;
    }

    public FormattedText ReadFirstCell(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];
        var cell = worksheet.Cells[1, 1]; // A1 单元格

        var formattedText = new FormattedText();

        if (cell == null || cell.Value == null)
        {
            return formattedText;
        }

        // 读取富文本格式（EPPlus 7.x）
        if (cell.IsRichText)
        {
            foreach (var rt in cell.RichText)
            {
                var run = new TextRun
                {
                    Text = rt.Text,
                    IsBold = rt.Bold,
                    IsItalic = rt.Italic
                    // 注意：EPPlus 7.x 的 ExcelRichText 不再直接提供 Underline 属性
                };

                if (rt.VerticalAlign != null)
                {
                    if (rt.VerticalAlign == ExcelVerticalAlignmentFont.Superscript)
                        run.IsSuperscript = true;
                    else if (rt.VerticalAlign == ExcelVerticalAlignmentFont.Subscript)
                        run.IsSubscript = true;
                }

                formattedText.Runs.Add(run);
            }
        }
        else
        {
            var run = new TextRun
            {
                Text = cell.Text ?? "",
                IsBold = cell.Style.Font.Bold,
                IsItalic = cell.Style.Font.Italic,
                IsUnderline = cell.Style.Font.UnderLine
            };

            var vertAlign = cell.Style.Font.VerticalAlign;
            if (vertAlign == ExcelVerticalAlignmentFont.Superscript)
                run.IsSuperscript = true;
            else if (vertAlign == ExcelVerticalAlignmentFont.Subscript)
                run.IsSubscript = true;

            formattedText.Runs.Add(run);
        }

        return formattedText;
    }
}
