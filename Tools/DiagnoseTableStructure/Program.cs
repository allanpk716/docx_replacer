using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DiagnoseTableStructure
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"错误: 找不到模板文件 {templatePath}");
                return;
            }

            Console.WriteLine("=== 分析文档中的表格单元格内容控件 ===\n");

            using var document = WordprocessingDocument.Open(templatePath, false);
            if (document.MainDocumentPart == null)
            {
                Console.WriteLine("错误: 无法打开文档主体部分");
                return;
            }

            // 查找所有表格
            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            Console.WriteLine($"找到 {tables.Count} 个表格\n");

            int tableIndex = 0;
            foreach (var table in tables)
            {
                tableIndex++;
                Console.WriteLine($"--- 表格 {tableIndex} ---");

                // 查找表格中的所有内容控件
                var controls = table.Descendants<SdtElement>().ToList();
                Console.WriteLine($"  找到 {controls.Count} 个内容控件");

                int controlIndex = 0;
                foreach (var control in controls)
                {
                    controlIndex++;

                    // 获取控件标签
                    var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                    var alias = control.SdtProperties?.GetFirstChild<SdtAlias>()?.Val?.Value;

                    // 确定控件类型
                    string controlType = control.GetType().Name;

                    // 查找内容容器
                    var runContent = control.Descendants<SdtContentRun>().FirstOrDefault();
                    var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();
                    var cellContent = control.Descendants<SdtContentCell>().FirstOrDefault();

                    string contentType = runContent != null ? "SdtContentRun (行内)" :
                                       blockContent != null ? "SdtContentBlock (块级)" :
                                       cellContent != null ? "SdtContentCell (单元格)" :
                                       "未知";

                    // 获取所在单元格信息
                    var cell = control.Ancestors<TableCell>().FirstOrDefault();
                    string cellInfo = "";
                    if (cell != null)
                    {
                        // 查找单元格在表格中的位置
                        var row = cell.Ancestors<TableRow>().FirstOrDefault();
                        if (row != null)
                        {
                            var rowIndex = row.Ancestors<Table>().First().Elements<TableRow>().ToList().IndexOf(row);
                            var cellIndex = row.Elements<TableCell>().ToList().IndexOf(cell);
                            cellInfo = $"位于: 行 {rowIndex + 1}, 列 {cellIndex + 1}";

                            // 统计单元格中的段落数
                            var paragraphCount = cell.Elements<Paragraph>().Count();
                            cellInfo += $", 段落数: {paragraphCount}";
                        }
                    }

                    Console.WriteLine($"\n  控件 {controlIndex}:");
                    Console.WriteLine($"    标题: {alias ?? "(无)"}");
                    Console.WriteLine($"    标记: {tag ?? "(无)"}");
                    Console.WriteLine($"    类型: {controlType}");
                    Console.WriteLine($"    内容容器: {contentType}");
                    if (!string.IsNullOrEmpty(cellInfo))
                    {
                        Console.WriteLine($"    {cellInfo}");
                    }

                    // 获取现有文本
                    var existingTexts = control.Descendants<Text>().ToList();
                    if (existingTexts.Any())
                    {
                        var text = string.Join("", existingTexts.Select(t => t.Text));
                        Console.WriteLine($"    当前内容: \"{text.Substring(0, Math.Min(50, text.Length))}{(text.Length > 50 ? "..." : "")}\"");
                    }

                    // 分析内容容器结构
                    Console.WriteLine($"    内容结构分析:");
                    var allRuns = control.Descendants<Run>().ToList();
                    Console.WriteLine($"      - Run 元素数量: {allRuns.Count}");

                    var allParagraphs = control.Descendants<Paragraph>().ToList();
                    Console.WriteLine($"      - Paragraph 元素数量: {allParagraphs.Count}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n=== 分析完成 ===");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
