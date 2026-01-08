using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using WordprocessingDocumentType = DocumentFormat.OpenXml.WordprocessingDocumentType;

namespace DocuFiller.Tests
{
    /// <summary>
    /// 批注功能测试(仅正文区域)
    /// 注意: 页眉页脚不支持批注功能,这是 OpenXML 的限制
    /// </summary>
    public class HeaderFooterCommentTests : IDisposable
    {
        private readonly string _testOutputDir;
        private readonly ILogger<CommentManager> _logger;
        private readonly CommentManager _commentManager;

        public HeaderFooterCommentTests()
        {
            _testOutputDir = Path.Combine(Path.GetTempPath(), "DocuFiller_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testOutputDir);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<CommentManager>();
            _commentManager = new CommentManager(_logger);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testOutputDir))
            {
                try { Directory.Delete(_testOutputDir, true); }
                catch { /* 忽略清理失败 */ }
            }
        }

        [Fact]
        public void AddCommentToBody_ShouldCreateMainDocumentCommentsPart()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template.docx");
            CreateTestDocumentWithBody(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var body = document.MainDocumentPart!.Document.Body!;
            var sdtBlock = body.Descendants<SdtBlock>().First();
            var run = sdtBlock.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(
                document,
                run,
                "测试批注",
                "测试作者",
                "TestTag");

            // Assert - 批注存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Equal(1, commentsPart.Comments.Count());
            Assert.Equal("测试批注", commentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);
        }

        [Fact]
        public void AddComment_MultipleComments_IncrementsCommentIds()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_multi_comments.docx");
            CreateTestDocumentWithBody(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var body = document.MainDocumentPart!.Document.Body!;
            var sdtBlock = body.Descendants<SdtBlock>().First();
            var runs = sdtBlock.Descendants<Run>().ToList();

            // 确保有足够的Runs
            if (runs.Count < 2)
            {
                // 添加第二个Run用于测试
                var paragraph = sdtBlock.Descendants<Paragraph>().First();
                paragraph.Append(new Run(new Text("第二个文本")));
            }

            var run = runs.First();
            var run2 = sdtBlock.Descendants<Run>().ToList()[1];

            // Act - 添加第一个批注
            _commentManager.AddCommentToElement(
                document,
                run,
                "第一个批注",
                "测试作者",
                "Tag1");

            // Act - 添加第二个批注
            _commentManager.AddCommentToElement(
                document,
                run2,
                "第二个批注",
                "测试作者",
                "Tag2");

            // Assert - 所有批注都存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Equal(2, commentsPart.Comments.Count());

            var comments = commentsPart.Comments.Descendants<Comment>().ToList();
            Assert.Equal("1", comments[0].Id?.Value);
            Assert.Equal("2", comments[1].Id?.Value);
        }

        [Fact]
        public void AddCommentToRunRange_Body_MultilineComment_CoversAllRuns()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_multiline_body.docx");
            CreateTestDocumentWithMultilineBody(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var body = document.MainDocumentPart!.Document.Body!;
            var sdtBlock = body.Descendants<SdtBlock>().First();
            var runs = sdtBlock.Descendants<Run>().ToList();

            // Act
            _commentManager.AddCommentToRunRange(
                document,
                runs,
                "多行正文批注",
                "测试作者",
                "MultiLineTag");

            // Assert - 所有批注都存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Single(commentsPart.Comments);
            Assert.Equal("多行正文批注", commentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);

            // 验证批注范围标记在正文中
            var commentRangeStarts = body.Descendants<CommentRangeStart>().ToList();
            var commentRangeEnds = body.Descendants<CommentRangeEnd>().ToList();

            Assert.Single(commentRangeStarts);
            Assert.Single(commentRangeEnds);
        }

        [Fact]
        public void GenerateCommentId_WithExistingComments_IncrementsCorrectly()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_existing_comments.docx");
            CreateTestDocumentWithBodyAndExistingComments(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var body = document.MainDocumentPart!.Document.Body!;
            var sdtBlock = body.Descendants<SdtBlock>().First();
            var run = sdtBlock.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(
                document,
                run,
                "新批注",
                "测试作者",
                "NewTag");

            // Assert - 新批注ID应该是2（因为已经存在ID为1的批注在主文档中）
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            var comments = commentsPart.Comments.Descendants<Comment>().ToList();
            Assert.Equal(2, comments.Count);
            Assert.Equal("1", comments[0].Id?.Value);
            Assert.Equal("2", comments[1].Id?.Value);
        }

        private void CreateTestDocumentWithBody(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(
                new SdtBlock(
                    new SdtProperties(
                        new Tag() { Val = "BodyField" }
                    ),
                    new SdtContentBlock(
                        new Paragraph(new Run(new Text("正文内容")))
                    )
                )
            ));
        }

        private void CreateTestDocumentWithMultilineBody(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加包含多行文本的正文
            var paragraph = new Paragraph();

            // 创建多个Run模拟多行文本
            var run1 = new Run(new Text("第一行"));
            var run2 = new Run(new Break());
            var run3 = new Run(new Text("第二行"));

            paragraph.Append(run1, run2, run3);

            var sdtBlock = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "MultilineBodyField" }
                ),
                new SdtContentBlock(paragraph)
            );

            mainPart.Document.Body.Append(sdtBlock);
        }

        private void CreateTestDocumentWithBodyAndExistingComments(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(
                new SdtBlock(
                    new SdtProperties(
                        new Tag() { Val = "BodyField" }
                    ),
                    new SdtContentBlock(
                        new Paragraph(new Run(new Text("正文内容")))
                    )
                )
            ));

            // 添加现有的批注到主文档
            var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments();
            var existingComment = new Comment()
            {
                Id = "1",
                Author = "现有作者",
                Date = DateTime.Now
            };
            existingComment.Append(new Paragraph(new Run(new Text("现有批注"))));
            commentsPart.Comments.Append(existingComment);
            commentsPart.Comments.Save();
        }
    }
}
