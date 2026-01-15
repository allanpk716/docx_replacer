using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace StepByStepSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";
            string dataPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\FD68 IVDR.json";
            string logPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\step_by_step_log.txt";

            var outputDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (var logWriter = new StreamWriter(logPath))
            {
                logWriter.WriteLine("=== 逐步模拟日志 ===");
                logWriter.WriteLine($"时间: {DateTime.Now}\n");

                // 读取 JSON 数据
                var jsonContent = File.ReadAllText(dataPath);
                var jsonDoc = JsonDocument.Parse(jsonContent);
                var data = new Dictionary<string, string>();
                foreach (var item in jsonDoc.RootElement.GetProperty("keywords").EnumerateArray())
                {
                    var key = item.GetProperty("key").GetString();
                    var value = item.GetProperty("value").GetString();
                    if (key != null && value != null)
                    {
                        data[key] = value;
                    }
                }

                // 复制模板
                string testPath = templatePath.Replace(".docx", "_test.docx");
                File.Copy(templatePath, testPath, true);

                using (var document = WordprocessingDocument.Open(testPath, true))
                {
                    if (document.MainDocumentPart == null) return;

                    var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                    if (tables.Count < 6) return;

                    var table6 = tables[5];
                    var row2 = table6.Elements<TableRow>().Skip(1).First();
                    var cells = row2.Elements<TableCell>().ToList();

                    logWriter.WriteLine("## 初始状态 - 第 2 行各列");
                    for (int c = 0; c < cells.Count; c++)
                    {
                        LogCellState(logWriter, c + 1, cells[c]);
                    }

                    // 查找列 5 的所有内容控件
                    var controls = cells[4].Descendants<SdtElement>().ToList();
                    logWriter.WriteLine($"\n## 找到 {controls.Count} 个内容控件");

                    foreach (var control in controls)
                    {
                        var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
                        logWriter.WriteLine($"\n处理控件: {tag}");

                        LogBeforeReplacement(logWriter, cells.ToArray());

                        // 模拟替换
                        SimulateReplacement(control, data.ContainsKey(tag) ? data[tag] : "", logWriter);

                        LogAfterReplacement(logWriter, cells.ToArray());
                    }

                    document.Save();
                }

                // 测试段落合并
                logWriter.WriteLine("\n\n## 测试 FixTableCellStructure");

                File.Copy(testPath, testPath.Replace("_test.docx", "_test2.docx"), true);

                using (var document = WordprocessingDocument.Open(testPath.Replace("_test.docx", "_test2.docx"), true))
                {
                    if (document.MainDocumentPart == null) return;

                    var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                    var table6 = tables[5];
                    var row2 = table6.Elements<TableRow>().Skip(1).First();
                    var cells = row2.Elements<TableCell>().ToList();

                    logWriter.WriteLine("\n### 合并前");
                    for (int c = 0; c < cells.Count; c++)
                    {
                        LogCellState(logWriter, c + 1, cells[c]);
                    }

                    // 执行合并
                    foreach (var cell in cells)
                    {
                        var paragraphs = cell.Elements<Paragraph>().ToList();
                        if (paragraphs.Count <= 1) continue;

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
                    }

                    logWriter.WriteLine("\n### 合并后");
                    for (int c = 0; c < cells.Count; c++)
                    {
                        LogCellState(logWriter, c + 1, cells[c]);
                    }

                    document.Save();
                }
            }

            Console.WriteLine("=== 模拟完成 ===");
            Console.WriteLine($"日志: {logPath}");
        }

        static void LogCellState(StreamWriter log, int colIndex, TableCell cell)
        {
            var paragraphs = cell.Elements<Paragraph>().ToList();
            var cellText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
            var truncated = cellText.Length > 60 ? cellText.Substring(0, 60) + "..." : cellText;

            log.WriteLine($"列 {colIndex}:");
            log.WriteLine($"  段落数: {paragraphs.Count}");
            log.WriteLine($"  文本: \"{truncated}\"");

            int displayCount = Math.Min(paragraphs.Count, 3);
            for (int p = 0; p < displayCount; p++)
            {
                var para = paragraphs[p];
                var paraText = string.Join("", para.Descendants<Text>().Select(t => t.Text));
                log.WriteLine($"    段落 {p + 1}: \"{paraText}\"");
            }
        }

        static void LogBeforeReplacement(StreamWriter log, TableCell[] cells)
        {
            log.WriteLine("  替换前各列文本:");
            for (int c = 0; c < cells.Length; c++)
            {
                var text = string.Join("", cells[c].Descendants<Text>().Select(t => t.Text));
                var truncated = text.Length > 40 ? text.Substring(0, 40) + "..." : text;
                log.WriteLine($"    列 {c + 1}: \"{truncated}\"");
            }
        }

        static void LogAfterReplacement(StreamWriter log, TableCell[] cells)
        {
            log.WriteLine("  替换后各列文本:");
            for (int c = 0; c < cells.Length; c++)
            {
                var text = string.Join("", cells[c].Descendants<Text>().Select(t => t.Text));
                var truncated = text.Length > 40 ? text.Substring(0, 40) + "..." : text;
                log.WriteLine($"    列 {c + 1}: \"{truncated}\"");
            }
        }

        static void SimulateReplacement(SdtElement control, string newText, StreamWriter log)
        {
            var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();
            if (blockContent == null) return;

            var paragraphs = blockContent.Elements<Paragraph>().ToList();
            if (paragraphs.Count == 0) return;

            var firstParagraph = paragraphs[0];

            log.WriteLine($"    第一个段落原有内容: \"{string.Join("", firstParagraph.Descendants<Text>().Select(t => t.Text))}\"");

            firstParagraph.RemoveAllChildren();

            var newRun = new Run();
            var runProperties = new RunProperties();
            var color = new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "FF0000" };
            runProperties.AppendChild(color);
            newRun.AppendChild(runProperties);

            var textElement = new Text(newText) { Space = SpaceProcessingModeValues.Preserve };
            newRun.AppendChild(textElement);

            firstParagraph.AppendChild(newRun);

            log.WriteLine($"    第一个段落新内容: \"{newText.Substring(0, Math.Min(50, newText.Length))}...\"");
            log.WriteLine($"    容器内段落数: {blockContent.Elements<Paragraph>().Count()}");
        }
    }
}
