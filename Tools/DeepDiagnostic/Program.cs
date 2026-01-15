using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace DeepDiagnostic
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";
            string dataPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\FD68 IVDR.json";
            string outputPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\deep_diagnostic.docx";
            string logPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\diagnostic_log.txt";

            Console.WriteLine("=== 深度诊断工具 ===\n");

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (var logWriter = new StreamWriter(logPath))
            {
                logWriter.WriteLine("=== 深度诊断日志 ===");
                logWriter.WriteLine($"时间: {DateTime.Now}\n");

                // 1. 分析原始模板
                Console.WriteLine("1. 分析原始模板");
                logWriter.WriteLine("## 1. 原始模板分析");

                AnalyzeTemplate(templatePath, logWriter);

                // 2. 读取 JSON 数据
                Console.WriteLine("\n2. 读取 JSON 数据");
                var jsonContent = File.ReadAllText(dataPath);
                var jsonDoc = JsonDocument.Parse(jsonContent);
                var keywords = jsonDoc.RootElement.GetProperty("keywords");

                var data = new Dictionary<string, string>();
                foreach (var item in keywords.EnumerateArray())
                {
                    var key = item.GetProperty("key").GetString();
                    var value = item.GetProperty("value").GetString();
                    if (key != null && value != null)
                    {
                        data[key] = value;
                    }
                }

                logWriter.WriteLine($"\n## 2. 数据加载");
                logWriter.WriteLine($"加载了 {data.Count} 个数据项");

                // 3. 复制并替换
                Console.WriteLine("\n3. 执行替换操作");
                File.Copy(templatePath, outputPath, true);

                logWriter.WriteLine($"\n## 3. 替换前单元格快照");
                CaptureTableCells(templatePath, logWriter, "替换前");

                using (var document = WordprocessingDocument.Open(outputPath, true))
                {
                    if (document.MainDocumentPart == null)
                    {
                        Console.WriteLine("错误: 无法打开文档主体部分");
                        return;
                    }

                    // 执行替换
                    var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                    Console.WriteLine($"找到 {tables.Count} 个表格");

                    foreach (var table in tables)
                    {
                        var controls = table.Descendants<SdtElement>().ToList();
                        if (controls.Count > 0)
                        {
                            logWriter.WriteLine($"\n### 表格有 {controls.Count} 个内容控件");

                            foreach (var control in controls)
                            {
                                var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                                if (!string.IsNullOrEmpty(tag) && data.ContainsKey(tag))
                                {
                                    var newValue = data[tag];
                                    logWriter.WriteLine($"替换控件: {tag}, 新值长度: {newValue.Length}");

                                    ReplaceContentInControl(control, newValue);
                                }
                            }
                        }
                    }

                    document.Save();
                }

                logWriter.WriteLine($"\n## 4. 替换后单元格快照");
                CaptureTableCells(outputPath, logWriter, "替换后");

                // 4. 应用修复
                Console.WriteLine("\n4. 应用表格单元格结构修复");

                using (var document = WordprocessingDocument.Open(outputPath, true))
                {
                    FixTableCellStructureWithLogging(document, logWriter);
                    document.Save();
                }

                logWriter.WriteLine($"\n## 5. 修复后单元格快照");
                CaptureTableCells(outputPath, logWriter, "修复后");

                Console.WriteLine("\n=== 诊断完成 ===");
                Console.WriteLine($"日志文件: {logPath}");
                Console.WriteLine("\n按任意键退出...");
            }

            // Console.ReadKey(); // 注释掉以避免在非交互式环境中出错
        }

        static void AnalyzeTemplate(string path, StreamWriter log)
        {
            using var document = WordprocessingDocument.Open(path, false);
            if (document.MainDocumentPart == null) return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            log.WriteLine($"文档中共有 {tables.Count} 个表格");

            foreach (var table in tables)
            {
                var rows = table.Elements<TableRow>().ToList();
                if (rows.Count > 0)
                {
                    var firstRow = rows[0];
                    var cells = firstRow.Elements<TableCell>().ToList();

                    log.WriteLine($"\n表格: {rows.Count} 行, 第一行: {cells.Count} 列");

                    // 检查每个单元格的内容
                    for (int c = 0; c < cells.Count && c < 7; c++)
                    {
                        var cell = cells[c];
                        var text = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                        var truncated = text.Length > 50 ? text.Substring(0, 50) + "..." : text;

                        log.WriteLine($"  列 {c + 1}: \"{truncated}\"");
                    }
                }
            }
        }

        static void CaptureTableCells(string path, StreamWriter log, string stage)
        {
            using var document = WordprocessingDocument.Open(path, false);
            if (document.MainDocumentPart == null) return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            log.WriteLine($"\n### {stage} - 共 {tables.Count} 个表格");

            int tableIndex = 0;
            foreach (var table in tables)
            {
                tableIndex++;
                var rows = table.Elements<TableRow>().ToList();
                if (rows.Count > 0)
                {
                    var firstRow = rows[0];
                    var cells = firstRow.Elements<TableCell>().ToList();

                    log.WriteLine($"\n#### 表格 {tableIndex}: {rows.Count} 行, {cells.Count} 列");

                    for (int c = 0; c < cells.Count && c < 7; c++)
                    {
                        var cell = cells[c];
                        var paragraphs = cell.Elements<Paragraph>().ToList();
                        var text = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                        var controls = cell.Descendants<SdtElement>().ToList();

                        var truncated = text.Length > 80 ? text.Substring(0, 80) + "..." : text;

                        log.WriteLine($"  列 {c + 1}: 段落数={paragraphs.Count}, 控件数={controls.Count}, 文本=\"{truncated}\"");
                    }
                }
            }
        }

        static void ReplaceContentInControl(SdtElement control, string newText)
        {
            var runContent = control.Descendants<SdtContentRun>().FirstOrDefault();
            OpenXmlElement? contentContainer = null;

            if (runContent != null)
            {
                contentContainer = runContent;
            }
            else
            {
                var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();
                if (blockContent != null)
                {
                    contentContainer = blockContent;
                }
            }

            if (contentContainer == null) return;

            contentContainer.RemoveAllChildren();

            if (control is SdtBlock || contentContainer is SdtContentBlock)
            {
                var paragraph = new Paragraph();
                var run = new Run();

                var runProperties = new RunProperties();
                var color = new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "FF0000" };
                runProperties.AppendChild(color);
                run.AppendChild(runProperties);

                var textElement = new Text(newText)
                {
                    Space = SpaceProcessingModeValues.Preserve
                };
                run.AppendChild(textElement);
                paragraph.AppendChild(run);

                contentContainer.AppendChild(paragraph);
            }
            else
            {
                var run = new Run();

                var runProperties = new RunProperties();
                var color = new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "FF0000" };
                runProperties.AppendChild(color);
                run.AppendChild(runProperties);

                var textElement = new Text(newText)
                {
                    Space = SpaceProcessingModeValues.Preserve
                };
                run.AppendChild(textElement);

                contentContainer.AppendChild(run);
            }
        }

        static void FixTableCellStructureWithLogging(WordprocessingDocument document, StreamWriter log)
        {
            if (document.MainDocumentPart == null)
                return;

            log.WriteLine($"\n### 开始修复表格单元格结构");

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            log.WriteLine($"找到 {tables.Count} 个表格");

            int totalCellsFixed = 0;
            int totalParagraphsMerged = 0;

            int tableIndex = 0;
            foreach (var table in tables)
            {
                tableIndex++;
                var cells = table.Descendants<TableCell>().ToList();

                log.WriteLine($"\n#### 表格 {tableIndex}: {cells.Count} 个单元格");

                foreach (var cell in cells)
                {
                    var paragraphs = cell.Elements<Paragraph>().ToList();

                    if (paragraphs.Count <= 1)
                        continue;

                    // 获取单元格文本用于日志
                    var cellText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                    var truncated = cellText.Length > 50 ? cellText.Substring(0, 50) + "..." : cellText;

                    log.WriteLine($"  单元格有 {paragraphs.Count} 个段落, 文本=\"{truncated}\"");

                    var firstParagraph = paragraphs[0];

                    // 将其他段落中的 Run 移动到第一个段落
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
                        totalParagraphsMerged++;
                    }

                    totalCellsFixed++;

                    // 修复后的文本
                    var fixedText = string.Join("", cell.Descendants<Text>().Select(t => t.Text));
                    var fixedTruncated = fixedText.Length > 50 ? fixedText.Substring(0, 50) + "..." : fixedText;
                    log.WriteLine($"  修复后文本=\"{fixedTruncated}\"");
                }
            }

            log.WriteLine($"\n#### 修复完成: 修复了 {totalCellsFixed} 个单元格，合并了 {totalParagraphsMerged} 个段落");
        }
    }
}
