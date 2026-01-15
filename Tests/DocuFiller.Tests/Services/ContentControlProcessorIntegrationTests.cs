using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DocumentFormat.OpenXml;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// ContentControlProcessor 集成测试
    /// </summary>
    public class ContentControlProcessorIntegrationTests
    {
        [Fact]
        public void ProcessContentControl_InTableCell_PreservesTableStructure()
        {
            // Arrange
            var testFile = Path.Combine("TestData", "table_template.docx");
            var outputFile = Path.Combine("TestData", "table_output.docx");

            // 创建测试文档（包含表格和内容控件）
            CreateTestDocumentWithTableContentControl(testFile);

            var processor = new ContentControlProcessor(
                new NullLogger<ContentControlProcessor>(),
                new CommentManager(new NullLogger<CommentManager>()),
                new SafeTextReplacer(new NullLogger<SafeTextReplacer>())
            );

            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "table_field", "new value in table" }
            };

            // Act
            string resultText;
            int paragraphCount;
            File.Copy(testFile, outputFile, true);

            using (var document = WordprocessingDocument.Open(outputFile, true))
            {
                var control = document.MainDocumentPart.Document.Descendants<SdtRun>()
                    .FirstOrDefault(c => c.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "table_field");

                processor.ProcessContentControl(control, data, document, Models.ContentControlLocation.Body);
                document.Save();

                // Assert
                // 验证表格结构完整
                var table = document.MainDocumentPart.Document.Descendants<Table>().First();
                Assert.NotNull(table);

                var cell = table.Descendants<TableCell>().First();
                var paragraphs = cell.Elements<Paragraph>().ToList();
                paragraphCount = paragraphs.Count;

                // 单元格应该只有一个段落
                Assert.Single(paragraphs);

                // 验证内容被替换
                resultText = cell.Descendants<Text>().First()?.Text;
                Assert.Equal("new value in table", resultText);
            }

            // 清理测试文件
            File.Delete(testFile);
            File.Delete(outputFile);
        }

        /// <summary>
        /// 创建包含表格和内容控件的测试文档
        /// </summary>
        private void CreateTestDocumentWithTableContentControl(string path)
        {
            // 确保 TestData 目录存在
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建包含表格和内容控件的测试文档
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            var table = new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(
                            new SdtRun(
                                new SdtProperties(new Tag() { Val = "table_field" }),
                                new SdtContentRun(new Run(new Text("old value")))
                            )
                        )
                    )
                )
            );

            mainPart.Document.Body = new Body(table);
            mainPart.Document.Save();
        }
    }
}
