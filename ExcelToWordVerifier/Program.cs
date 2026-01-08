using System;
using System.Collections.Generic;
using System.IO;
using ExcelToWordVerifier.Models;
using ExcelToWordVerifier.Services;

namespace ExcelToWordVerifier;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Excel 到 Word 格式保留验证程序 ===\n");

        try
        {
            // 初始化服务
            var excelReader = new ExcelReaderService();
            var wordWriter = new WordWriterService();

            // 读取所有测试单元格
            var testCells = new[] { "A1", "A2", "A3", "A4", "A5", "A6" };
            var formattedTexts = new List<FormattedText>();

            Console.WriteLine("正在读取 Excel 文件...");
            var excelFile = Path.Combine("TestFiles", "FormattedTextTest.xlsx");

            foreach (var cell in testCells)
            {
                Console.Write($"  读取单元格 {cell}... ");
                var formattedText = excelReader.ReadCell(excelFile, "测试数据", cell);
                formattedTexts.Add(formattedText);

                // 显示读取结果
                Console.WriteLine($"成功");
                Console.WriteLine($"    内容: {formattedText.PlainText}");
                Console.WriteLine($"    文本运行数: {formattedText.Runs.Count}");
                foreach (var run in formattedText.Runs)
                {
                    Console.WriteLine($"      - {run}");
                }
                Console.WriteLine();
            }

            // 写入 Word 文档
            Console.WriteLine("正在写入 Word 文档...");
            var outputFile = Path.Combine("Output", $"FormattedTextOutput_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            wordWriter.WriteMany(formattedTexts, outputFile);

            Console.WriteLine($"成功！输出文件: {outputFile}");
            Console.WriteLine("\n验证完成！请打开生成的 Word 文档检查格式是否正确。");
            Console.WriteLine("预期结果:");
            Console.WriteLine("  1. 第1行: 2x10⁹ (Unicode 上标字符)");
            Console.WriteLine("  2. 第2行: H₂O (Unicode 下标字符)");
            Console.WriteLine("  3. 第3行: 粗体文本");
            Console.WriteLine("  4. 第4行: 斜体文本");
            Console.WriteLine("  5. 第5行: 下划线文本");
            Console.WriteLine("  6. 第6行: 粗体+斜体文本");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex.StackTrace}");
            Console.ResetColor();
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}
