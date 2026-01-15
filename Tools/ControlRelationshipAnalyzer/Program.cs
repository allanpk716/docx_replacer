using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ControlRelationshipAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";
            string logPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\control_relationship_log.txt";

            var outputDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (var logWriter = new StreamWriter(logPath))
            {
                logWriter.WriteLine("=== 内容控件关系分析日志 ===");
                logWriter.WriteLine($"时间: {DateTime.Now}\n");

                using var document = WordprocessingDocument.Open(templatePath, false);
                if (document.MainDocumentPart == null)
                {
                    Console.WriteLine("错误: 无法打开文档主体部分");
                    return;
                }

                // 查找表格 6
                var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                if (tables.Count < 6)
                {
                    Console.WriteLine($"错误: 只找到 {tables.Count} 个表格");
                    return;
                }

                var table6 = tables[5];
                var rows = table6.Elements<TableRow>().ToList();

                logWriter.WriteLine("## 表格 6 结构分析");
                logWriter.WriteLine($"总行数: {rows.Count}\n");

                // 分析第 2 行（数据行）
                if (rows.Count < 2)
                {
                    Console.WriteLine("错误: 表格没有第 2 行");
                    return;
                }

                var dataRow = rows[1];
                var cells = dataRow.Elements<TableCell>().ToList();

                logWriter.WriteLine("### 第 2 行单元格分析");
                logWriter.WriteLine($"总单元格数: {cells.Count}\n");

                for (int c = 0; c < cells.Count; c++)
                {
                    var cell = cells[c];
                    logWriter.WriteLine($"#### 列 {c + 1}");

                    // 获取所有段落
                    var paragraphs = cell.Elements<Paragraph>().ToList();
                    logWriter.WriteLine($"段落数: {paragraphs.Count}");

                    // 获取所有内容控件
                    var controls = cell.Descendants<SdtElement>().ToList();
                    logWriter.WriteLine($"内容控件数: {controls.Count}");

                    if (controls.Count > 0)
                    {
                        logWriter.WriteLine("\n内容控件详情:");

                        for (int i = 0; i < controls.Count; i++)
                        {
                            var control = controls[i];
                            var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";

                            logWriter.WriteLine($"\n  控件 {i + 1}: {tag} ({control.GetType().Name})");

                            // 获取内容容器
                            var runContent = control.Descendants<SdtContentRun>().FirstOrDefault();
                            var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();

                            if (blockContent != null)
                            {
                                logWriter.WriteLine($"    容器类型: SdtContentBlock");

                                // 获取容器中的段落
                                var containerParagraphs = blockContent.Elements<Paragraph>().ToList();
                                logWriter.WriteLine($"    容器内段落数: {containerParagraphs.Count}");

                                for (int p = 0; p < containerParagraphs.Count; p++)
                                {
                                    var para = containerParagraphs[p];
                                    var paraText = string.Join("", para.Descendants<Text>().Select(t => t.Text));
                                    var truncated = paraText.Length > 50 ? paraText.Substring(0, 50) + "..." : paraText;

                                    // 检查这个段落是否在单元格的直接子元素中
                                    var isDirectChild = cell.Elements<Paragraph>().Contains(para);
                                    var paraHashCode = para.GetHashCode();

                                    logWriter.WriteLine($"      段落 {p + 1}: Hash={paraHashCode}, IsDirectChild={isDirectChild}, Text=\"{truncated}\"");
                                }
                            }
                            else if (runContent != null)
                            {
                                logWriter.WriteLine($"    容器类型: SdtContentRun");

                                var runs = runContent.Elements<Run>().ToList();
                                logWriter.WriteLine($"    容器内 Run 数: {runs.Count}");
                            }
                        }
                    }
                    else
                    {
                        // 没有控件，显示段落内容
                        logWriter.WriteLine("\n段落内容:");
                        for (int p = 0; p < Math.Min(paragraphs.Count, 3); p++)
                        {
                            var para = paragraphs[p];
                            var paraText = string.Join("", para.Descendants<Text>().Select(t => t.Text));
                            var truncated = paraText.Length > 50 ? paraText.Substring(0, 50) + "..." : paraText;
                            logWriter.WriteLine($"  段落 {p + 1}: \"{truncated}\"");
                        }
                    }

                    logWriter.WriteLine();
                }

                // 检查段落共享情况
                logWriter.WriteLine("### 段落对象引用分析");

                var allParagraphsInCell5 = cells[4].Elements<Paragraph>().ToList();
                logWriter.WriteLine($"列 5 (单元格) 直接包含 {allParagraphsInCell5.Count} 个段落对象");

                var allSdtBlocks = cells[4].Descendants<SdtContentBlock>().ToList();
                logWriter.WriteLine($"列 5 包含 {allSdtBlocks.Count} 个 SdtContentBlock 容器");

                var allParagraphsInContainers = allSdtBlocks.SelectMany(cb => cb.Elements<Paragraph>()).ToList();
                logWriter.WriteLine($"所有 SdtContentBlock 容器中共有 {allParagraphsInContainers.Count} 个段落引用");

                // 检查是否有重复引用
                var uniqueParagraphs = allParagraphsInContainers.Distinct().ToList();
                logWriter.WriteLine($"去重后有 {uniqueParagraphs.Count} 个不同的段落对象");

                if (allParagraphsInContainers.Count != uniqueParagraphs.Count)
                {
                    logWriter.WriteLine("\n⚠️ 警告: 发现段落对象被多个容器引用！");
                    var duplicates = allParagraphsInContainers
                        .GroupBy(p => p.GetHashCode())
                        .Where(g => g.Count() > 1)
                        .ToList();

                    foreach (var group in duplicates)
                    {
                        var para = group.First();
                        var paraText = string.Join("", para.Descendants<Text>().Select(t => t.Text));
                        var truncated = paraText.Length > 30 ? paraText.Substring(0, 30) + "..." : paraText;
                        logWriter.WriteLine($"  段落 Hash={group.Key}: 被 {group.Count()} 个容器引用, Text=\"{truncated}\"");
                    }
                }
            }

            Console.WriteLine("=== 分析完成 ===");
            Console.WriteLine($"日志文件: {logPath}");
        }
    }
}
