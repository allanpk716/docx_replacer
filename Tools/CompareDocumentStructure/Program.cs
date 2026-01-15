using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CompareDocumentStructure
{
    class Program
    {
        static void Main(string[] args)
        {
            string templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx";
            string dataPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\FD68 IVDR.json";
            string outputPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\output\compare_test.docx";

            Console.WriteLine("=== 文档结构比较工具 ===\n");

            // 确保输出目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // 读取 JSON 数据
            var jsonContent = File.ReadAllText(dataPath);
            Console.WriteLine($"JSON 数据已加载: {jsonContent.Length} 字符\n");

            // 分析原始文档
            Console.WriteLine("1. 分析原始模板文档结构");
            AnalyzeDocument(templatePath, "原始模板");

            // 复制模板到输出
            File.Copy(templatePath, outputPath, true);

            // 使用 OpenXML 修改文档
            Console.WriteLine("\n2. 模拟替换操作");
            using (var document = WordprocessingDocument.Open(outputPath, true))
            {
                if (document.MainDocumentPart == null)
                {
                    Console.WriteLine("错误: 无法打开文档主体部分");
                    return;
                }

                // 查找所有表格中的内容控件
                var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
                Console.WriteLine($"找到 {tables.Count} 个表格");

                foreach (var table in tables)
                {
                    var controls = table.Descendants<SdtElement>().ToList();
                    Console.WriteLine($"  表格中有 {controls.Count} 个内容控件");

                    foreach (var control in controls)
                    {
                        var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                        if (!string.IsNullOrEmpty(tag))
                        {
                            Console.WriteLine($"    处理控件: {tag}");

                            // 模拟替换操作 - 设置一个测试文本
                            var contentContainer = control.Descendants<SdtContentRun>().FirstOrDefault()
                                ?? control.Descendants<SdtContentBlock>().FirstOrDefault()
                                ?? control.Descendants<SdtContentCell>().FirstOrDefault();

                            if (contentContainer != null)
                            {
                                // 清空内容
                                contentContainer.RemoveAllChildren();

                                // 添加新内容
                                if (control is SdtBlock)
                                {
                                    var paragraph = new Paragraph(
                                        new Run(
                                            new RunProperties(new Color() { Val = "FF0000" }),
                                            new Text("TEST REPLACEMENT") { Space = SpaceProcessingModeValues.Preserve }
                                        )
                                    );
                                    contentContainer.AppendChild(paragraph);
                                }
                                else
                                {
                                    var run = new Run(
                                        new RunProperties(new Color() { Val = "FF0000" }),
                                        new Text("TEST REPLACEMENT") { Space = SpaceProcessingModeValues.Preserve }
                                    );
                                    contentContainer.AppendChild(run);
                                }
                            }
                        }
                    }
                }

                document.Save();
            }

            Console.WriteLine("\n3. 分析修改后的文档结构");
            AnalyzeDocument(outputPath, "修改后");

            // 比较表格结构
            Console.WriteLine("\n4. 比较表格结构差异");
            CompareTableStructure(templatePath, outputPath);

            Console.WriteLine("\n分析完成！请查看修改后的文档: " + outputPath);
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static void AnalyzeDocument(string path, string label)
        {
            using var document = WordprocessingDocument.Open(path, false);
            if (document.MainDocumentPart == null) return;

            var tables = document.MainDocumentPart.Document.Descendants<Table>().ToList();
            Console.WriteLine($"\n{label} - 文档结构:");
            Console.WriteLine($"  总表格数: {tables.Count}");

            for (int i = 0; i < Math.Min(3, tables.Count); i++)
            {
                var table = tables[i];
                var rows = table.Elements<TableRow>().ToList();
                Console.WriteLine($"\n  表格 {i + 1}:");
                Console.WriteLine($"    行数: {rows.Count}");

                if (rows.Count > 0)
                {
                    var firstRow = rows[0];
                    var cells = firstRow.Elements<TableCell>().ToList();
                    Console.WriteLine($"    第一行列数: {cells.Count}");

                    // 分析每个单元格的段落数
                    for (int c = 0; c < Math.Min(5, cells.Count); c++)
                    {
                        var cell = cells[c];
                        var paragraphs = cell.Elements<Paragraph>().ToList();
                        Console.WriteLine($"      列 {c + 1}: {paragraphs.Count} 个段落");

                        // 如果有内容控件，显示其信息
                        var controls = cell.Descendants<SdtElement>().ToList();
                        if (controls.Count > 0)
                        {
                            foreach (var control in controls)
                            {
                                var tag = control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                                var controlType = control.GetType().Name;
                                Console.WriteLine($"        内容控件: {controlType}, 标记: {tag}");
                            }
                        }
                    }
                }
            }
        }

        static void CompareTableStructure(string path1, string path2)
        {
            using var doc1 = WordprocessingDocument.Open(path1, false);
            using var doc2 = WordprocessingDocument.Open(path2, false);

            if (doc1.MainDocumentPart == null || doc2.MainDocumentPart == null) return;

            var tables1 = doc1.MainDocumentPart.Document.Descendants<Table>().ToList();
            var tables2 = doc2.MainDocumentPart.Document.Descendants<Table>().ToList();

            Console.WriteLine($"  原始文档表格数: {tables1.Count}");
            Console.WriteLine($"  修改后文档表格数: {tables2.Count}");

            if (tables1.Count != tables2.Count)
            {
                Console.WriteLine("  ⚠️ 表格数量不同！");
            }

            // 比较每个表格的行数和列数
            for (int i = 0; i < Math.Min(tables1.Count, tables2.Count); i++)
            {
                var rows1 = tables1[i].Elements<TableRow>().ToList();
                var rows2 = tables2[i].Elements<TableRow>().ToList();

                if (rows1.Count != rows2.Count)
                {
                    Console.WriteLine($"  表格 {i + 1}: 行数不同 ({rows1.Count} vs {rows2.Count})");
                }

                // 比较第一行的列数
                if (rows1.Count > 0 && rows2.Count > 0)
                {
                    var cells1 = rows1[0].Elements<TableCell>().ToList();
                    var cells2 = rows2[0].Elements<TableCell>().ToList();

                    if (cells1.Count != cells2.Count)
                    {
                        Console.WriteLine($"  表格 {i + 1} 第一行: ⚠️ 列数不同 ({cells1.Count} vs {cells2.Count})");
                        Console.WriteLine($"    这是导致列偏移的原因！");
                    }
                    else
                    {
                        Console.WriteLine($"  表格 {i + 1} 第一行: 列数相同 ({cells1.Count})");
                    }
                }
            }
        }
    }
}
