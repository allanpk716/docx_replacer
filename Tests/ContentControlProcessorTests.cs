using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
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
            var safeReplacerLogger = new Logger<SafeTextReplacer>(loggerFactory);
            _processor = new ContentControlProcessor(processorLogger, _commentManager, new SafeTextReplacer(safeReplacerLogger));

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
                Assert.Single(runs);

                var run = runs![0];
                var texts = run.Descendants<Text>().Select(t => t.Text).ToList();
                Assert.Equal(3, texts.Count);
                Assert.Equal("Line1", texts[0]);
                Assert.Equal("Line2", texts[1]);
                Assert.Equal("Line3", texts[2]);

                var breaks = run.Descendants<Break>().ToList();
                Assert.Equal(2, breaks.Count);
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

        [Fact]
        public void AddProcessingComment_MultilineText_CoversAllLines()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "Line1\nLine2\nLine3" } };
            var testFilePath = Path.Combine(_testOutputPath, "test_multiline_comment.docx");

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

            // Assert - 验证批注覆盖所有行
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                // 检查批注范围标记
                var commentRangeStarts = document.MainDocumentPart?.Document.Body?.Descendants<CommentRangeStart>().ToList();
                var commentRangeEnds = document.MainDocumentPart?.Document.Body?.Descendants<CommentRangeEnd>().ToList();
                var commentReferences = document.MainDocumentPart?.Document.Body?.Descendants<CommentReference>().ToList();

                Assert.NotNull(commentRangeStarts);
                Assert.Single(commentRangeStarts);

                Assert.NotNull(commentRangeEnds);
                Assert.Single(commentRangeEnds);

                Assert.NotNull(commentReferences);
                Assert.Single(commentReferences);

                // 验证批注ID匹配
                Assert.Equal(commentRangeStarts[0].Id?.Value, commentRangeEnds[0].Id?.Value);
                Assert.Equal(commentRangeStarts[0].Id?.Value, commentReferences[0].Id?.Value);
            }

            CleanupTestFile(testFilePath);
        }

        [Fact]
        public void AddProcessingComment_SingleLineText_UsesOriginalMethod()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "SingleLine" } };
            var testFilePath = Path.Combine(_testOutputPath, "test_singleline_comment.docx");

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

            // Assert - 验证单行文本批注仍然正常工作
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                var comments = document.MainDocumentPart?.WordprocessingCommentsPart?.Comments?.Descendants<Comment>().ToList();

                Assert.NotNull(comments);
                Assert.Single(comments);

                // 验证批注范围标记也存在（单行也使用范围标记）
                var commentRangeStarts = document.MainDocumentPart?.Document.Body?.Descendants<CommentRangeStart>().ToList();
                var commentRangeEnds = document.MainDocumentPart?.Document.Body?.Descendants<CommentRangeEnd>().ToList();

                Assert.NotNull(commentRangeStarts);
                Assert.Single(commentRangeStarts);

                Assert.NotNull(commentRangeEnds);
                Assert.Single(commentRangeEnds);
            }

            CleanupTestFile(testFilePath);
        }

        [Fact]
        public void FindAllTargetRuns_WithMultipleRuns_ReturnsAllRuns()
        {
            // Arrange
            var testFilePath = Path.Combine(_testOutputPath, "test_find_all_runs.docx");

            using (var document = WordprocessingDocument.Create(
                testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var paragraph = new Paragraph();
                var run1 = new Run(new Text("Line1"));
                var run2 = new Run(new Break());
                var run3 = new Run(new Text("Line2"));

                paragraph.Append(run1, run2, run3);

                var sdtContent = new SdtContentBlock(paragraph);
                var sdtBlock = new SdtBlock(sdtContent);

                // Act
                var runs = sdtBlock.Descendants<Run>().ToList();

                // Assert
                Assert.Equal(3, runs.Count);
            }

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
