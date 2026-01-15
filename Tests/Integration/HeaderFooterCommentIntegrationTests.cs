using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using WordprocessingDocumentType = DocumentFormat.OpenXml.WordprocessingDocumentType;

namespace DocuFiller.Tests.Integration
{
    /// <summary>
    /// 批注功能集成测试(仅正文区域)
    /// 注意: 页眉页脚不支持批注功能,这是 OpenXML 的限制
    /// </summary>
    public class HeaderFooterCommentIntegrationTests : IDisposable
    {
        private readonly string _testDir;
        private readonly ILoggerFactory _loggerFactory;

        public HeaderFooterCommentIntegrationTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "Integration_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); }
                catch { /* 忽略清理失败 */ }
            }
        }

        [Fact]
        public async Task ProcessDocumentWithHeaderFooter_ShouldAddCommentsOnlyToBody()
        {
            // Arrange
            string templatePath = Path.Combine(_testDir, "template.docx");
            string outputPath = Path.Combine(_testDir, "output.docx");
            string dataPath = Path.Combine(_testDir, "data.json");

            CreateTestTemplate(templatePath);
            File.WriteAllText(dataPath, @"[{""HeaderField"":""新页眉"",""BodyField"":""新正文"",""FooterField"":""新页脚""}]");

            var fileService = new FileService();
            var dataParser = new DataParserService(_loggerFactory.CreateLogger<DataParserService>(), fileService);
            var excelDataParser = new ExcelDataParserService(_loggerFactory.CreateLogger<ExcelDataParserService>(), fileService);
            var serviceProvider = new ServiceCollection()
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .BuildServiceProvider();
            var processor = new DocumentProcessorService(
                _loggerFactory.CreateLogger<DocumentProcessorService>(),
                dataParser,
                excelDataParser,
                fileService,
                new ProgressReporterService(_loggerFactory.CreateLogger<ProgressReporterService>()),
                new ContentControlProcessor(
                    _loggerFactory.CreateLogger<ContentControlProcessor>(),
                    new CommentManager(_loggerFactory.CreateLogger<CommentManager>()),
                    new SafeTextReplacer(_loggerFactory.CreateLogger<SafeTextReplacer>())),
                new CommentManager(_loggerFactory.CreateLogger<CommentManager>()),
                serviceProvider);

            // Act
            bool success = await processor.ProcessSingleDocumentAsync(
                templatePath,
                outputPath,
                (await dataParser.ParseJsonFileAsync(dataPath)).First());

            // Assert
            Assert.True(success);
            Assert.True(File.Exists(outputPath));

            using var document = WordprocessingDocument.Open(outputPath, false);

            // 验证批注在主文档中(只有正文的批注,页眉页脚的批注被跳过)
            Assert.NotNull(document.MainDocumentPart.WordprocessingCommentsPart);
            Assert.True(document.MainDocumentPart.WordprocessingCommentsPart.Comments.Any());

            // 验证只有正文的批注(应该只有1个批注)
            var comments = document.MainDocumentPart.WordprocessingCommentsPart.Comments.OfType<Comment>().ToList();
            Assert.Single(comments);

            // 验证批注内容是正文的批注
            var commentText = comments.First().GetFirstChild<Paragraph>()?.InnerText;
            Assert.Contains("正文", commentText);

            // 验证所有批注 ID 唯一
            var allIds = document.MainDocumentPart.WordprocessingCommentsPart.Comments.OfType<Comment>().Select(c => c.Id!.Value!);
            Assert.Equal(allIds.Count(), allIds.Distinct().Count());
        }

        private void CreateTestTemplate(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(
                new SdtBlock(
                    new SdtProperties(new Tag() { Val = "BodyField" }),
                    new SdtContentBlock(new Paragraph(new Run(new Text("正文占位符"))))
                )
            ));

            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(new SdtBlock(
                new SdtProperties(new Tag() { Val = "HeaderField" }),
                new SdtContentBlock(new Paragraph(new Run(new Text("页眉占位符"))))
            ));

            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(new SdtBlock(
                new SdtProperties(new Tag() { Val = "FooterField" }),
                new SdtContentBlock(new Paragraph(new Run(new Text("页脚占位符"))))
            ));
        }
    }
}
