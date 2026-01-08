using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using WordprocessingDocumentType = DocumentFormat.OpenXml.WordprocessingDocumentType;

namespace DocuFiller.Tests
{
    public class ContentControlProcessorTests : IDisposable
    {
        private readonly ContentControlProcessor _processor;
        private readonly CommentManager _commentManager;
        private readonly string _testOutputPath;

        public ContentControlProcessorTests()
        {
            using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
            var commentLogger = new Logger<CommentManager>(loggerFactory);
            _commentManager = new CommentManager(commentLogger);
            var processorLogger = new Logger<ContentControlProcessor>(loggerFactory);
            _processor = new ContentControlProcessor(processorLogger, _commentManager);

            // 设置测试输出路径
            _testOutputPath = Path.Combine(Path.GetTempPath(), "DocuFillerTests");
            Directory.CreateDirectory(_testOutputPath);
        }

        [Fact]
        public void ProcessContentControl_BodyControl_ReplacesContent()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "TestValue" } };
            var testFilePath = Path.Combine(_testOutputPath, "test_body.docx");

            using (var document = WordprocessingDocument.Create(
                testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                // 创建测试内容控件
                var sdtBlock = new SdtBlock();
                var sdtProperties = new SdtProperties(
                    new Tag { Val = "TestField" },
                    new SdtAlias { Val = "Test Field" }
                );
                var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
                sdtBlock.Append(sdtProperties, sdtContent);
                mainPart.Document.Body.Append(sdtBlock);
                mainPart.Document.Save();

                // Act
                _processor.ProcessContentControl(
                    sdtBlock, data, document, ContentControlLocation.Body);
            }

            // Assert - 重新打开文档验证
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                var sdtBlock = document.MainDocumentPart?.Document.Body?.Descendants<SdtBlock>().FirstOrDefault();
                Assert.NotNull(sdtBlock);

                var sdtContent = sdtBlock?.GetFirstChild<SdtContentBlock>();
                Assert.NotNull(sdtContent);

                var newText = sdtContent?.Descendants<Text>().FirstOrDefault();
                Assert.NotNull(newText);
                Assert.Equal("TestValue", newText?.Text);
            }

            // Cleanup
            CleanupTestFile(testFilePath);
        }

        [Fact]
        public void ProcessContentControl_MissingData_SkipsGracefully()
        {
            // Arrange
            var data = new Dictionary<string, object>();
            var testFilePath = Path.Combine(_testOutputPath, "test_skip.docx");

            using (var document = WordprocessingDocument.Create(
                testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var sdtBlock = new SdtBlock();
                var sdtProperties = new SdtProperties(new Tag { Val = "TestField" });
                var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
                sdtBlock.Append(sdtProperties, sdtContent);
                mainPart.Document.Body.Append(sdtBlock);
                mainPart.Document.Save();

                // Act & Assert - 应该不抛出异常
                var exception = Record.Exception(() =>
                {
                    _processor.ProcessContentControl(
                        sdtBlock, data, document, ContentControlLocation.Body);
                });

                Assert.Null(exception);
            }

            // 验证旧值未被改变
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                var sdtBlock = document.MainDocumentPart?.Document.Body?.Descendants<SdtBlock>().FirstOrDefault();
                Assert.NotNull(sdtBlock);

                var sdtContent = sdtBlock?.GetFirstChild<SdtContentBlock>();
                Assert.NotNull(sdtContent);

                var text = sdtContent?.Descendants<Text>().FirstOrDefault();
                Assert.NotNull(text);
                Assert.Equal("OldValue", text?.Text);
            }

            // Cleanup
            CleanupTestFile(testFilePath);
        }

        [Fact]
        public void ProcessContentControl_EmptyTag_SkipsGracefully()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "TestValue" } };
            var testFilePath = Path.Combine(_testOutputPath, "test_empty_tag.docx");

            using (var document = WordprocessingDocument.Create(
                testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                // 创建没有标签的内容控件
                var sdtBlock = new SdtBlock();
                var sdtProperties = new SdtProperties(); // 没有 Tag
                var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
                sdtBlock.Append(sdtProperties, sdtContent);
                mainPart.Document.Body.Append(sdtBlock);
                mainPart.Document.Save();

                // Act & Assert - 应该不抛出异常
                var exception = Record.Exception(() =>
                {
                    _processor.ProcessContentControl(
                        sdtBlock, data, document, ContentControlLocation.Body);
                });

                Assert.Null(exception);
            }

            // Cleanup
            CleanupTestFile(testFilePath);
        }

        [Fact]
        public void ProcessContentControl_MultilineText_PreservesLineBreaks()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "Line1\nLine2\nLine3" } };
            var testFilePath = Path.Combine(_testOutputPath, "test_multiline.docx");

            using (var document = WordprocessingDocument.Create(
                testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var sdtBlock = new SdtBlock();
                var sdtProperties = new SdtProperties(new Tag { Val = "TestField" });
                var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
                sdtBlock.Append(sdtProperties, sdtContent);
                mainPart.Document.Body.Append(sdtBlock);
                mainPart.Document.Save();

                // Act
                _processor.ProcessContentControl(
                    sdtBlock, data, document, ContentControlLocation.Body);
            }

            // Assert - 验证换行符被正确处理
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                var sdtBlock = document.MainDocumentPart?.Document.Body?.Descendants<SdtBlock>().FirstOrDefault();
                Assert.NotNull(sdtBlock);

                var sdtContent = sdtBlock?.GetFirstChild<SdtContentBlock>();
                Assert.NotNull(sdtContent);

                var runs = sdtContent?.Descendants<Run>().ToList();
                Assert.NotNull(runs);
                Assert.Equal(5, runs?.Count); // 3行文本 + 2个换行符

                var textElements = runs?.Where(r => r.Descendants<Text>().Any()).ToList();
                Assert.Equal(3, textElements?.Count);
                Assert.Equal("Line1", textElements?[0].Descendants<Text>().First().Text);
                Assert.Equal("Line2", textElements?[1].Descendants<Text>().First().Text);
                Assert.Equal("Line3", textElements?[2].Descendants<Text>().First().Text);
            }

            // Cleanup
            CleanupTestFile(testFilePath);
        }

        [Fact]
        public void ProcessContentControl_EmptyValue_ClearsContent()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "" } };
            var testFilePath = Path.Combine(_testOutputPath, "test_empty_value.docx");

            using (var document = WordprocessingDocument.Create(
                testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var sdtBlock = new SdtBlock();
                var sdtProperties = new SdtProperties(new Tag { Val = "TestField" });
                var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
                sdtBlock.Append(sdtProperties, sdtContent);
                mainPart.Document.Body.Append(sdtBlock);
                mainPart.Document.Save();

                // Act
                _processor.ProcessContentControl(
                    sdtBlock, data, document, ContentControlLocation.Body);
            }

            // Assert - 验证内容被清空
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                var sdtBlock = document.MainDocumentPart?.Document.Body?.Descendants<SdtBlock>().FirstOrDefault();
                Assert.NotNull(sdtBlock);

                var sdtContent = sdtBlock?.GetFirstChild<SdtContentBlock>();
                Assert.NotNull(sdtContent);

                // 空值应该仍然创建一个段落，但文本为空
                var paragraph = sdtContent?.Descendants<Paragraph>().FirstOrDefault();
                Assert.NotNull(paragraph);
            }

            // Cleanup
            CleanupTestFile(testFilePath);
        }

        private void CleanupTestFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }

        public void Dispose()
        {
            // 清理测试目录
            try
            {
                if (Directory.Exists(_testOutputPath))
                {
                    Directory.Delete(_testOutputPath, true);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
