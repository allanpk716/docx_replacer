using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Text.Json;

namespace TableCellTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";
            string dataPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\FD68 IVDR.json";
            string outputPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\test_fixed.docx";

            Console.WriteLine("=== 表格单元格修复测试 ===\n");

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 1. 分析原始模板
            Console.WriteLine("1. 分析原始模板");
            AnalyzeTemplate(templatePath);

            // 2. 读取 JSON 数据
            Console.WriteLine("\n2. 读取 JSON 数据");
            var jsonContent = File.ReadAllText(dataPath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var keywords = jsonDoc.RootElement.GetProperty("keywords");

            // 转换为字典
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

            // 3. 模拟替换过程
            Console.WriteLine("\n3. 执行替换操作");
            File.Copy(templatePath, outputPath, true);

            using (var document = WordprocessingDocument.Open(outputPath, true))
            {
                if (document.MainDocumentPart == null)
                {
                    Console.WriteLine("错误: 无法打开文档主体部分");
                    return;
                }

                // 查找 1.4.3.2 Instrument 部分的表格
                var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                Console.WriteLine($"找到 {tables.Count} 个表格");

                // 查找包含内容控件的表格
                foreach (var table in tables)
                {
                    var controls = table.Descendants<SdtElement>().ToList();
                    if (controls.Count > 0)
                    {
                        Console.WriteLine($"\n表格中有 {controls.Count} 个内容控件");

                        foreach (var control in controls)
                        {
                            var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                            if (!string.IsNullOrEmpty(tag) && data.ContainsKey(tag))
                            {
                                var newValue = data[tag];
                                Console.WriteLine($"  替换控件: {tag}");
                                Console.WriteLine($"    新值长度: {newValue.Length}");

                                // 使用 SafeTextReplacer 的逻辑进行替换
                                ReplaceContentInControl(control, newValue);
                            }
                        }
                    }
                }

                // 4. 应用修复
                Console.WriteLine("\n4. 应用表格单元格结构修复");
                FixTableCellStructure(document);

                // 5. 再次分析
                Console.WriteLine("\n5. 分析修复后的文档");
                AnalyzeTableCells(document);

                document.Save();
            }

            Console.WriteLine("\n\n=== 测试完成 ===");
            Console.WriteLine($"输出文件: {outputPath}");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static void AnalyzeTemplate(string path)
        {
            using var document = WordprocessingDocument.Open(path, false);
            if (document.MainDocumentPart == null) return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            Console.WriteLine($"文档中共有 {tables.Count} 个表格");

            foreach (var table in tables)
            {
                var rows = table.Elements<TableRow>().ToList();
                Console.WriteLine($"\n表格: {rows.Count} 行");

                if (rows.Count > 0)
                {
                    var firstRow = rows[0];
                    var cells = firstRow.Elements<TableCell>().ToList();
                    Console.WriteLine($"  第一行: {cells.Count} 列");

                    // 检查每个单元格的段落数
                    for (int c = 0; c < cells.Count && c < 7; c++)
                    {
                        var cell = cells[c];
                        var paragraphs = cell.Elements<Paragraph>().ToList();
                        var controls = cell.Descendants<SdtElement>().ToList();

                        Console.Write($"  列 {c + 1}: {paragraphs.Count} 个段落");
                        if (controls.Count > 0)
                        {
                            Console.Write($", {controls.Count} 个控件");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }

        static void ReplaceContentInControl(SdtElement control, string newText)
        {
            // 查找内容容器
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
            if (contentContainer == null)
            {
                Console.WriteLine("    未找到内容容器");
                return;
            }

            // 清空并重新创建内容
            contentContainer.RemoveAllChildren();

            if (control is SdtBlock || contentContainer is SdtContentBlock)
            {
                // 块级控件：创建带格式的段落
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
                // 行内控件：创建带格式的 Run
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

        static void FixTableCellStructure(WordprocessingDocument document)
        {
            if (document.MainDocumentPart == null)
                return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            Console.WriteLine($"开始修复 {tables.Count} 个表格的单元格结构");

            int totalCellsFixed = 0;
            int totalParagraphsMerged = 0;

            foreach (var table in tables)
            {
                var cells = table.Descendants<TableCell>().ToList();

                foreach (var cell in cells)
                {
                    var paragraphs = cell.Elements<Paragraph>().ToList();

                    if (paragraphs.Count <= 1)
                        continue;

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
                }
            }

            Console.WriteLine($"修复完成: 修复了 {totalCellsFixed} 个单元格，合并了 {totalParagraphsMerged} 个段落");
        }

        static void AnalyzeTableCells(WordprocessingDocument document)
        {
            if (document.MainDocumentPart == null) return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            Console.WriteLine($"文档中共有 {tables.Count} 个表格");

            int cellsWithMultipleParagraphs = 0;

            foreach (var table in tables)
            {
                var rows = table.Elements<TableRow>().ToList();
                Console.WriteLine($"\n表格: {rows.Count} 行");

                if (rows.Count > 0)
                {
                    var firstRow = rows[0];
                    var cells = firstRow.Elements<TableCell>().ToList();
                    Console.WriteLine($"  第一行: {cells.Count} 列");

                    // 检查每个单元格的段落数
                    for (int c = 0; c < cells.Count && c < 7; c++)
                    {
                        var cell = cells[c];
                        var paragraphs = cell.Elements<Paragraph>().ToList();
                        var controls = cell.Descendants<SdtElement>().ToList();

                        if (paragraphs.Count > 1)
                        {
                            cellsWithMultipleParagraphs++;
                            Console.WriteLine($"  ⚠️ 列 {c + 1}: {paragraphs.Count} 个段落 (需要修复!)");
                        }
                        else
                        {
                            Console.Write($"  ✓ 列 {c + 1}: {paragraphs.Count} 个段落");
                            if (controls.Count > 0)
                            {
                                Console.Write($", {controls.Count} 个控件");
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }

            if (cellsWithMultipleParagraphs > 0)
            {
                Console.WriteLine($"\n⚠️ 警告: 仍有 {cellsWithMultipleParagraphs} 个单元格包含多个段落!");
            }
            else
            {
                Console.WriteLine("\n✓ 所有单元格的段落数都正常");
            }
        }
    }
}
