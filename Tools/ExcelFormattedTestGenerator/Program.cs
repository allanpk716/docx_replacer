using System;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ExcelToWordVerifier;

/// <summary>
/// 创建包含真正 Excel 格式化上标/下标的测试文件
/// </summary>
class CreateFormattedExcel
{
    static void Main(string[] args)
    {
        Console.WriteLine("创建 Excel 格式化上标测试文件...");

        // 设置 EPPlus 许可上下文
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("格式化测试");

        // 测试 1: 科学计数法 - 使用 Excel 上标格式
        // 输入 "2x10" 然后给 "9" 设置上标格式
        var cell1 = worksheet.Cells["A1"];
        cell1.Value = "2x10";  // 先输入基础文本
        // 使用 RichText 设置上标
        var rt1 = cell1.RichText.Add("2x10");
        var rt2 = cell1.RichText.Add("9");
        rt2.Bold = false;
        rt2.VerticalAlign = ExcelVerticalAlignmentFont.Superscript;

        // 测试 2: 水分子式 - 使用 Excel 下标格式
        var cell2 = worksheet.Cells["A2"];
        var rt3 = cell2.RichText.Add("H");
        var rt4 = cell2.RichText.Add("2");
        rt4.VerticalAlign = ExcelVerticalAlignmentFont.Subscript;
        var rt5 = cell2.RichText.Add("O");

        // 测试 3: 平方公式 (x²)
        var cell3 = worksheet.Cells["A3"];
        var rt6 = cell3.RichText.Add("x");
        var rt7 = cell3.RichText.Add("2");
        rt7.VerticalAlign = ExcelVerticalAlignmentFont.Superscript;

        // 测试 4: 立方公式 (x³)
        var cell4 = worksheet.Cells["A4"];
        var rt8 = cell4.RichText.Add("y");
        var rt9 = cell4.RichText.Add("3");
        rt9.VerticalAlign = ExcelVerticalAlignmentFont.Superscript;

        // 测试 5: 混合格式 - 粗体 + 上标
        var cell5 = worksheet.Cells["A5"];
        var rt10 = cell5.RichText.Add("E = mc");
        rt10.Bold = true;
        var rt11 = cell5.RichText.Add("2");
        rt11.Bold = true;
        rt11.VerticalAlign = ExcelVerticalAlignmentFont.Superscript;

        // 测试 6: 下标示例 - 化学式
        var cell6 = worksheet.Cells["A6"];
        var rt12 = cell6.RichText.Add("H");
        var rt13 = rt12;
        var rt14 = cell6.RichText.Add("2");
        rt14.VerticalAlign = ExcelVerticalAlignmentFont.Subscript;
        var rt15 = cell6.RichText.Add("SO");
        var rt16 = cell6.RichText.Add("4");
        rt16.VerticalAlign = ExcelVerticalAlignmentFont.Subscript;

        // 添加说明列
        worksheet.Cells["B1"].Value = "2x10⁹ (上标)";
        worksheet.Cells["B2"].Value = "H₂O (下标)";
        worksheet.Cells["B3"].Value = "x² (上标)";
        worksheet.Cells["B4"].Value = "y³ (上标)";
        worksheet.Cells["B5"].Value = "E = mc² (粗体+上标)";
        worksheet.Cells["B6"].Value = "H₂SO₄ (下标)";

        // 自动调整列宽
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // 保存文件
        var outputFile = "TestFiles/FormattedSuperscriptTest.xlsx";
        package.SaveAs(new FileInfo(outputFile));

        Console.WriteLine($"✓ 测试文件已创建: {outputFile}");
        Console.WriteLine("\n测试内容:");
        Console.WriteLine("  A1: 2x10⁹ (使用 Excel 上标格式)");
        Console.WriteLine("  A2: H₂O (使用 Excel 下标格式)");
        Console.WriteLine("  A3: x² (上标)");
        Console.WriteLine("  A4: y³ (上标)");
        Console.WriteLine("  A5: E = mc² (粗体+上标)");
        Console.WriteLine("  A6: H₂SO₄ (多个下标)");
        Console.WriteLine("\n注意: 这些是使用 Excel 的格式化功能设置的上标/下标,");
        Console.WriteLine("      不是 Unicode 字符。");
    }
}
