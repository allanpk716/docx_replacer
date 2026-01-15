using System;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TableStructureAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";
            string jsonPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\FD68 IVDR.json";
            string outputPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\analyzed_output.docx";
            string logPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\table_structure_log.txt";

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (var logWriter = new StreamWriter(logPath))
            {
                logWriter.WriteLine("=== 表格结构分析日志 ===");
                logWriter.WriteLine($"时间: {DateTime.Now}\n");

                // 1. 分析原始模板
                logWriter.WriteLine("## 1. 原始模板 - 表格 6 (1.4.3.2 Instrument)");
                AnalyzeTable6(templatePath, logWriter, "原始");

                // 2. 复制并进行替换
                File.Copy(templatePath, outputPath, true);

                using (var document = WordprocessingDocument.Open(outputPath, true))
                {
                    if (document.MainDocumentPart == null)
                    {
                        Console.WriteLine("错误: 无法打开文档主体部分");
                        return;
                    }

                    // 查找表格 6 的所有内容控件
                    var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                    if (tables.Count < 6)
                    {
                        Console.WriteLine($"错误: 只找到 {tables.Count} 个表格，需要至少 6 个");
                        return;
                    }

                    var table6 = tables[5]; // 表格 6 (索引 5)
                    var controls = table6.Descendants<SdtElement>().ToList();

                    logWriter.WriteLine($"\n## 2. 表格 6 中找到 {controls.Count} 个内容控件");

                    foreach (var control in controls)
                    {
                        var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
                        logWriter.WriteLine($"  控件 Tag: {tag}, 类型: {control.GetType().Name}");

                        // 获取控件所在的单元格
                        var cell = OpenXmlTableCellHelper.GetParentTableCell(control);
                        if (cell != null)
                        {
                            // 获取单元格在行中的索引
                            var row = cell.Parent as TableRow;
                            if (row != null)
                            {
                                int cellIndex = row.Elements<TableCell>().ToList().IndexOf(cell);
                                logWriter.WriteLine($"    位置: 第 {cellIndex + 1} 列");

                                // 获取单元格内容
                                var cellText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                                var truncated = cellText.Length > 100 ? cellText.Substring(0, 100) + "..." : cellText;
                                logWriter.WriteLine($"    内容: \"{truncated}\"");
                            }
                        }
                    }

                    // 模拟替换操作（不实际修改，只记录）
                    logWriter.WriteLine($"\n## 3. 模拟替换操作");
                    foreach (var control in controls)
                    {
                        var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                        if (!string.IsNullOrEmpty(tag))
                        {
                            logWriter.WriteLine($"  将替换控件: {tag}");
                        }
                    }

                    document.Save();
                }

                // 3. 分析替换后的结构
                logWriter.WriteLine($"\n## 4. 替换后 - 表格 6 结构");
                AnalyzeTable6(outputPath, logWriter, "替换后");

                // 4. 执行段落合并
                using (var document = WordprocessingDocument.Open(outputPath, true))
                {
                    logWriter.WriteLine($"\n## 5. 执行段落合并操作");

                    var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                    var table6 = tables[5];
                    var rows = table6.Elements<TableRow>().ToList();

                    logWriter.WriteLine($"表格 6 有 {rows.Count} 行");

                    for (int r = 0; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        var cells = row.Elements<TableCell>().ToList();

                        logWriter.WriteLine($"\n  行 {r + 1}: 有 {cells.Count} 个单元格");

                        for (int c = 0; c < cells.Count; c++)
                        {
                            var cell = cells[c];
                            var paragraphs = cell.Elements<Paragraph>().ToList();
                            var cellText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                            var truncated = cellText.Length > 50 ? cellText.Substring(0, 50) + "..." : cellText;

                            logWriter.WriteLine($"    列 {c + 1}: 段落数={paragraphs.Count}, 文本=\"{truncated}\"");

                            if (paragraphs.Count > 1)
                            {
                                logWriter.WriteLine($"      警告: 列 {c + 1} 有多个段落！");
                                for (int p = 0; p < paragraphs.Count; p++)
                                {
                                    var para = paragraphs[p];
                                    var paraText = string.Join("", para.Descendants<Text>().Select(t => t.Text));
                                    logWriter.WriteLine($"        段落 {p + 1}: \"{paraText}\"");
                                }

                                // 执行合并
                                var firstParagraph = paragraphs[0];
                                for (int i = 1; i < paragraphs.Count; i++)
                                {
                                    var extraParagraph = paragraphs[i];
                                    var runs = extraParagraph.Elements<Run>().ToList();
                                    foreach (var run in runs)
                                    {
                                        run.Remove();
                                        firstParagraph.AppendChild(run);
                                    }
                                    extraParagraph.Remove();
                                }

                                logWriter.WriteLine($"      合并后段落数: {cell.Elements<Paragraph>().Count()}");
                            }
                        }
                    }

                    document.Save();
                }

                // 5. 分析合并后的最终结构
                logWriter.WriteLine($"\n## 6. 合并后 - 表格 6 最终结构");
                AnalyzeTable6(outputPath, logWriter, "最终");
            }

            Console.WriteLine("=== 分析完成 ===");
            Console.WriteLine($"日志文件: {logPath}");
        }

        static void AnalyzeTable6(string path, StreamWriter log, string stage)
        {
            using var document = WordprocessingDocument.Open(path, false);
            if (document.MainDocumentPart == null) return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            if (tables.Count < 6)
            {
                log.WriteLine($"错误: 只找到 {tables.Count} 个表格");
                return;
            }

            var table6 = tables[5];
            var rows = table6.Elements<TableRow>().ToList();

            log.WriteLine($"### {stage} - 表格 6: {rows.Count} 行");

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                var cells = row.Elements<TableCell>().ToList();

                log.WriteLine($"\n  行 {r + 1}: {cells.Count} 个单元格");

                for (int c = 0; c < cells.Count; c++)
                {
                    var cell = cells[c];
                    var paragraphs = cell.Elements<Paragraph>().ToList();
                    var cellText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                    var truncated = cellText.Length > 80 ? cellText.Substring(0, 80) + "..." : cellText;
                    var controls = cell.Descendants<SdtElement>().Count();

                    log.WriteLine($"    列 {c + 1}: 段落数={paragraphs.Count}, 控件数={controls}, 文本=\"{truncated}\"");
                }
            }
        }
    }

    /// <summary>
    /// 表格单元格辅助类
    /// </summary>
    public static class OpenXmlTableCellHelper
    {
        /// <summary>
        /// 获取元素所在的表格单元格
        /// </summary>
        public static TableCell? GetParentTableCell(OpenXmlElement element)
        {
            var current = element;
            while (current != null)
            {
                if (current is TableCell cell)
                    return cell;
                current = current.Parent;
            }
            return null;
        }
    }
}
