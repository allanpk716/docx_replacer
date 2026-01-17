using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordprocessingDocumentType = DocumentFormat.OpenXml.WordprocessingDocumentType;

namespace DocuFiller.Tests
{
    public class ExcelIntegrationTests : IDisposable
    {
        private readonly string _testTemplatePath;
        private readonly string _testExcelPath;
        private readonly string _outputPath;
        private readonly IExcelDataParser _excelParser;
        private readonly IDocumentProcessor _documentProcessor;

        public ExcelIntegrationTests()
        {
            // 设置测试文件路径
            var testDir = Path.Combine(Path.GetTempPath(), $"DocuFiller_Test_{Guid.NewGuid()}");
            Directory.CreateDirectory(testDir);

            _testTemplatePath = Path.Combine(testDir, "template.docx");
            _testExcelPath = Path.Combine(testDir, "data.xlsx");
            _outputPath = Path.Combine(testDir, "output.docx");

            // 创建测试模板
            CreateTestTemplate(_testTemplatePath);

            // 创建测试 Excel
            CreateTestExcel(_testExcelPath);

            // 初始化服务
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IExcelDataParser, ExcelDataParserService>();
            services.AddSingleton<ContentControlProcessor>();
            services.AddSingleton<CommentManager>();
            services.AddSingleton<ISafeTextReplacer, SafeTextReplacer>();
            services.AddSingleton<ISafeFormattedContentReplacer, SafeFormattedContentReplacer>();
            services.AddSingleton<IProgressReporter, ProgressReporterService>();
            services.AddSingleton<IDataParser, DataParserService>();
            services.AddSingleton<IDocumentProcessor, DocumentProcessorService>();

            var serviceProvider = services.BuildServiceProvider();

            _excelParser = serviceProvider.GetRequiredService<IExcelDataParser>();
            _documentProcessor = serviceProvider.GetRequiredService<IDocumentProcessor>();
        }

        private void CreateTestTemplate(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var productNameControl = new SdtBlock();
            var productNameProperties = new SdtProperties(
                new SdtAlias { Val = "产品名称" },
                new Tag { Val = "#产品名称#" }
            );

            var productNameContent = new SdtContentBlock(
                new Paragraph(new Run(new Text("默认值")))
            );

            productNameControl.Append(productNameProperties, productNameContent);
            mainPart.Document.Body.Append(productNameControl);

            var specControl = new SdtBlock();
            var specProperties = new SdtProperties(
                new SdtAlias { Val = "规格" },
                new Tag { Val = "#规格#" }
            );

            var specContent = new SdtContentBlock(
                new Paragraph(new Run(new Text("默认值")))
            );

            specControl.Append(specProperties, specContent);
            mainPart.Document.Body.Append(specControl);
            mainPart.Document.Save();
        }

        private void CreateTestExcel(string path)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            worksheet.Cells[1, 1].Value = "#产品名称#";
            worksheet.Cells[1, 2].Value = "D-二聚体";

            // 添加带格式的内容
            worksheet.Cells[2, 1].Value = "#规格#";
            var cell = worksheet.Cells[2, 2];
            cell.Value = "2x10";
            cell.RichText.Add("9").VerticalAlign = OfficeOpenXml.Style.ExcelVerticalAlignmentFont.Superscript;

            package.SaveAs(new System.IO.FileInfo(path));
        }

        [Fact]
        public async Task EndToEnd_ExcelToWord_RetainsFormatting()
        {
            // Arrange
            var excelData = await _excelParser.ParseExcelFileAsync(_testExcelPath);

            // Act
            var result = await _documentProcessor.ProcessDocumentWithFormattedDataAsync(
                _testTemplatePath,
                excelData,
                _outputPath
            );

            // Assert
            Assert.True(result.IsSuccess);

            // 验证输出文件存在
            Assert.True(File.Exists(_outputPath));

            // 验证上标格式
            using var outputDoc = WordprocessingDocument.Open(_outputPath, false);
            var mainPart = outputDoc.MainDocumentPart;
            var runs = mainPart.Document.Descendants<Run>();

            // 查找包含上标的 Run
            var superscriptRun = runs.FirstOrDefault(r =>
            {
                var runProps = r.RunProperties;
                return runProps != null &&
                       runProps.VerticalTextAlignment != null &&
                       runProps.VerticalTextAlignment.Val == VerticalPositionValues.Superscript;
            });

            Assert.NotNull(superscriptRun);
        }

        public void Dispose()
        {
            var testDir = Path.GetDirectoryName(_testTemplatePath);
            if (Directory.Exists(testDir))
            {
                try
                {
                    Directory.Delete(testDir, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }
    }
}
