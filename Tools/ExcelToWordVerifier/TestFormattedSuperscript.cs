using System;
using System.Collections.Generic;
using System.IO;
using ExcelToWordVerifier.Models;
using ExcelToWordVerifier.Services;

namespace ExcelToWordVerifier;

/// <summary>
/// 测试 Excel 格式化上标的读取和 Word 写入
/// </summary>
class TestFormattedSuperscript
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Excel 格式化上标/下标验证程序 ===\n");
        Console.WriteLine("测试文件包含使用 Excel 格式化功能设置的上标/下标");
        Console.WriteLine("（不是 Unicode 字符）\n");

        try
        {
            // 初始化服务
            var excelReader = new ExcelReaderService();
            var wordWriter = new WordWriterService();

            // 读取所有测试单元格
            var testCases = new[]
            {
                ("A1", "2x10⁹", "科学计数法（上标）"),
                ("A2", "H₂O", "水分子式（下标）"),
                ("A3", "x²", "平方（上标）"),
                ("A4", "y³", "立方（上标）"),
                ("A5", "E = mc²", "质能方程（粗体+上标）"),
                ("A6", "H₂SO₄", "硫酸分子式（多个下标）")
            };

            var formattedTexts = new List<FormattedText>();

            Console.WriteLine("正在读取 Excel 文件...");
            var excelFile = Path.Combine("TestFiles", "FormattedSuperscriptTest.xlsx");

            foreach (var (cell, expectedContent, description) in testCases)
            {
                Console.Write($"  {description} ({cell})... ");
                var formattedText = excelReader.ReadCell(excelFile, "格式化测试", cell);
                formattedTexts.Add(formattedText);

                // 显示读取结果
                Console.WriteLine($"成功");
                Console.WriteLine($"    预期内容: {expectedContent}");
                Console.WriteLine($"    实际内容: {formattedText.PlainText}");
                Console.WriteLine($"    文本运行数: {formattedText.Runs.Count}");

                foreach (var run in formattedText.Runs)
                {
                    var formatInfo = new List<string>();
                    if (run.IsBold) formatInfo.Add("粗体");
                    if (run.IsItalic) formatInfo.Add("斜体");
                    if (run.IsUnderline) formatInfo.Add("下划线");
                    if (run.IsSuperscript) formatInfo.Add("上标");
                    if (run.IsSubscript) formatInfo.Add("下标");

                    var formatStr = formatInfo.Count > 0 ? $" [{string.Join(", ", formatInfo)}]" : "";
                    Console.WriteLine($"      - \"{run.Text}\"{formatStr}");
                }
                Console.WriteLine();
            }

            // 写入 Word 文档
            Console.WriteLine("正在写入 Word 文档...");
            var outputFile = Path.Combine("Output", $"FormattedSuperscriptOutput_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            wordWriter.WriteMany(formattedTexts, outputFile);

            Console.WriteLine($"✓ 成功！输出文件: {outputFile}");
            Console.WriteLine("\n验证完成！");
            Console.WriteLine("\n请打开生成的 Word 文档并验证：");
            Console.WriteLine("  1. 上标字符是否正确显示为上标");
            Console.WriteLine("  2. 下标字符是否正确显示为下标");
            Console.WriteLine("  3. 粗体格式是否保留");
            Console.WriteLine("  4. 整体布局是否正确");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex.StackTrace}");
            Console.ResetColor();
        }

        Console.WriteLine("\n按任意键退出...");
        try
        {
            Console.ReadKey();
        }
        catch (InvalidOperationException)
        {
            // 在非交互式环境中忽略此错误
        }
    }
}
