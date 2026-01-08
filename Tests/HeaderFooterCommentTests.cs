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
        public void AddCommentToHeader_ShouldCreateMainDocumentCommentsPart()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template.docx");
            CreateTestDocumentWithHeader(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var header = headerPart.Header!;
            var sdtBlock = header.Descendants<SdtBlock>().First();
            var run = sdtBlock.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(
                document,
                run,
                "测试批注",
                "测试作者",
                "TestTag",
                ContentControlLocation.Header,
                sdtBlock);

            // Assert - 所有批注都存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Equal(1, commentsPart.Comments.Count());
            Assert.Equal("测试批注", commentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);

            // 验证页眉中没有批注部分
            var headerCommentsPart = headerPart.GetPartsOfType<WordprocessingCommentsPart>().FirstOrDefault();
            Assert.Null(headerCommentsPart);
        }

        [Fact]
        public void AddCommentToFooter_ShouldCreateMainDocumentCommentsPart()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_with_footer.docx");
            CreateTestDocumentWithFooter(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var footerPart = document.MainDocumentPart!.FooterParts.First();
            var footer = footerPart.Footer!;
            var sdtBlock = footer.Descendants<SdtBlock>().First();
            var run = sdtBlock.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(
                document,
                run,
                "页脚测试批注",
                "测试作者",
                "FooterTag",
                ContentControlLocation.Footer,
                sdtBlock);

            // Assert - 所有批注都存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Equal(1, commentsPart.Comments.Count());
            Assert.Equal("页脚测试批注", commentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);

            // 验证页脚中没有批注部分
            var footerCommentsPart = footerPart.GetPartsOfType<WordprocessingCommentsPart>().FirstOrDefault();
            Assert.Null(footerCommentsPart);
        }

        [Fact]
        public void AddCommentToHeader_MultipleComments_IncrementsCommentIds()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_multi_comments.docx");
            CreateTestDocumentWithHeader(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var header = headerPart.Header!;
            var sdtBlock = header.Descendants<SdtBlock>().First();
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
                "Tag1",
                ContentControlLocation.Header,
                sdtBlock);

            // Act - 添加第二个批注
            _commentManager.AddCommentToElement(
                document,
                run2,
                "第二个批注",
                "测试作者",
                "Tag2",
                ContentControlLocation.Header,
                sdtBlock);

            // Assert - 所有批注都存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Equal(2, commentsPart.Comments.Count());

            var comments = commentsPart.Comments.Descendants<Comment>().ToList();
            Assert.Equal("1", comments[0].Id?.Value);
            Assert.Equal("2", comments[1].Id?.Value);
        }

        [Fact]
        public void AddCommentToHeaderAndFooter_ShouldCreateMainDocumentCommentsPart()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_header_footer.docx");
            CreateTestDocumentWithHeaderAndFooter(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var footerPart = document.MainDocumentPart.FooterParts.First();

            var headerSdt = headerPart.Header!.Descendants<SdtBlock>().First();
            var headerRun = headerSdt.Descendants<Run>().First();

            var footerSdt = footerPart.Footer!.Descendants<SdtBlock>().First();
            var footerRun = footerSdt.Descendants<Run>().First();

            // Act - 添加页眉批注
            _commentManager.AddCommentToElement(
                document,
                headerRun,
                "页眉批注",
                "测试作者",
                "HeaderTag",
                ContentControlLocation.Header,
                headerSdt);

            // Act - 添加页脚批注
            _commentManager.AddCommentToElement(
                document,
                footerRun,
                "页脚批注",
                "测试作者",
                "FooterTag",
                ContentControlLocation.Footer,
                footerSdt);

            // Assert - 所有批注都存储在主文档的批注部分中
            var mainCommentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(mainCommentsPart);
            Assert.Equal(2, mainCommentsPart.Comments.Count());

            // 验证页眉和页脚中没有批注部分
            var headerCommentsPart = headerPart.GetPartsOfType<WordprocessingCommentsPart>().FirstOrDefault();
            var footerCommentsPart = footerPart.GetPartsOfType<WordprocessingCommentsPart>().FirstOrDefault();

            Assert.Null(headerCommentsPart);
            Assert.Null(footerCommentsPart);

            // 验证批注内容
            var comments = mainCommentsPart.Comments.Descendants<Comment>().ToList();
            Assert.Contains(comments, c => c.GetFirstChild<Paragraph>()?.InnerText == "页眉批注");
            Assert.Contains(comments, c => c.GetFirstChild<Paragraph>()?.InnerText == "页脚批注");
        }

        [Fact]
        public void AddCommentToRunRange_Header_MultilineComment_CoversAllRuns()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_multiline_header.docx");
            CreateTestDocumentWithMultilineHeader(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var header = headerPart.Header!;
            var sdtBlock = header.Descendants<SdtBlock>().First();
            var runs = sdtBlock.Descendants<Run>().ToList();

            // Act
            _commentManager.AddCommentToRunRange(
                document,
                runs,
                "多行页眉批注",
                "测试作者",
                "MultiLineTag",
                ContentControlLocation.Header,
                sdtBlock);

            // Assert - 所有批注都存储在主文档的批注部分中
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            Assert.Single(commentsPart.Comments);
            Assert.Equal("多行页眉批注", commentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);

            // 验证批注范围标记在页眉中
            var commentRangeStarts = header.Descendants<CommentRangeStart>().ToList();
            var commentRangeEnds = header.Descendants<CommentRangeEnd>().ToList();

            Assert.Single(commentRangeStarts);
            Assert.Single(commentRangeEnds);
        }

        [Fact]
        public void GenerateCommentId_WithExistingComments_IncrementsCorrectly()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template_existing_comments.docx");
            CreateTestDocumentWithHeaderAndExistingComments(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var header = headerPart.Header!;
            var sdtBlock = header.Descendants<SdtBlock>().First();
            var run = sdtBlock.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(
                document,
                run,
                "新批注",
                "测试作者",
                "NewTag",
                ContentControlLocation.Header,
                sdtBlock);

            // Assert - 新批注ID应该是2（因为已经存在ID为1的批注在主文档中）
            var commentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(commentsPart);
            var comments = commentsPart.Comments.Descendants<Comment>().ToList();
            Assert.Equal(2, comments.Count);
            Assert.Equal("1", comments[0].Id?.Value);
            Assert.Equal("2", comments[1].Id?.Value);
        }

        [Fact]
        public void CommentsInDifferentParts_ShouldHaveUniqueIds()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template.docx");
            CreateTestDocumentWithHeaderAndFooterAndBody(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var footerPart = document.MainDocumentPart.FooterParts.First();
            var body = document.MainDocumentPart.Document.Body!;

            var headerSdt = headerPart.Header!.Descendants<SdtBlock>().First();
            var footerSdt = footerPart.Footer!.Descendants<SdtBlock>().First();
            var bodySdt = body.Descendants<SdtBlock>().First();

            var headerRun = headerSdt.Descendants<Run>().First();
            var footerRun = footerSdt.Descendants<Run>().First();
            var bodyRun = bodySdt.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(document, headerRun, "页眉批注", "作者", "Tag1", ContentControlLocation.Header, headerSdt);
            _commentManager.AddCommentToElement(document, footerRun, "页脚批注", "作者", "Tag2", ContentControlLocation.Footer, footerSdt);
            _commentManager.AddCommentToElement(document, bodyRun, "正文批注", "作者", "Tag3", ContentControlLocation.Body, bodySdt);

            // Assert - 验证所有批注都存储在主文档中且 ID 全局唯一
            var mainCommentsPart = document.MainDocumentPart.WordprocessingCommentsPart;
            Assert.NotNull(mainCommentsPart);

            var allCommentIds = mainCommentsPart.Comments.Descendants<Comment>().Select(c => c.Id!.Value!).ToList();

            Assert.Equal(3, allCommentIds.Count);
            Assert.Equal(3, allCommentIds.Distinct().Count());

            // 验证页眉和页脚中没有批注部分
            Assert.Empty(headerPart.GetPartsOfType<WordprocessingCommentsPart>());
            Assert.Empty(footerPart.GetPartsOfType<WordprocessingCommentsPart>());
        }

        private void CreateTestDocumentWithHeader(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加页眉
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header();
            var sdtBlock = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "HeaderField" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("页眉内容")))
                )
            );
            header.Append(sdtBlock);
            headerPart.Header = header;
        }

        private void CreateTestDocumentWithFooter(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加页脚
            var footerPart = mainPart.AddNewPart<FooterPart>();
            var footer = new Footer();
            var sdtBlock = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "FooterField" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("页脚内容")))
                )
            );
            footer.Append(sdtBlock);
            footerPart.Footer = footer;
        }

        private void CreateTestDocumentWithHeaderAndFooter(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加页眉
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header();
            var headerSdt = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "HeaderField" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("页眉内容")))
                )
            );
            header.Append(headerSdt);
            headerPart.Header = header;

            // 添加页脚
            var footerPart = mainPart.AddNewPart<FooterPart>();
            var footer = new Footer();
            var footerSdt = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "FooterField" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("页脚内容")))
                )
            );
            footer.Append(footerSdt);
            footerPart.Footer = footer;
        }

        private void CreateTestDocumentWithMultilineHeader(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加包含多行文本的页眉
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header();
            var paragraph = new Paragraph();

            // 创建多个Run模拟多行文本
            var run1 = new Run(new Text("第一行"));
            var run2 = new Run(new Break());
            var run3 = new Run(new Text("第二行"));

            paragraph.Append(run1, run2, run3);

            var sdtBlock = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "MultilineHeaderField" }
                ),
                new SdtContentBlock(paragraph)
            );

            header.Append(sdtBlock);
            headerPart.Header = header;
        }

        private void CreateTestDocumentWithHeaderAndExistingComments(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加页眉
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header();
            var sdtBlock = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "HeaderField" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("页眉内容")))
                )
            );
            header.Append(sdtBlock);
            headerPart.Header = header;

            // 添加现有的批注到主文档（不是页眉）
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

        private void CreateTestDocumentWithHeaderAndFooterAndBody(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(
                new SdtBlock(
                    new SdtProperties(new Tag() { Val = "BodyField" }),
                    new SdtContentBlock(new Paragraph(new Run(new Text("正文内容"))))
                )
            ));

            // 添加页眉
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(new SdtBlock(
                new SdtProperties(new Tag() { Val = "HeaderField" }),
                new SdtContentBlock(new Paragraph(new Run(new Text("页眉内容"))))
            ));

            // 添加页脚
            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(new SdtBlock(
                new SdtProperties(new Tag() { Val = "FooterField" }),
                new SdtContentBlock(new Paragraph(new Run(new Text("页脚内容"))))
            ));
        }
    }
}
