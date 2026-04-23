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

            var multilineControl = new SdtBlock();
            var multilineProperties = new SdtProperties(
                new SdtAlias { Val = "多行" },
                new Tag { Val = "#多行#" }
            );
            var multilineContent = new SdtContentBlock(
                new Paragraph(new Run(new Text("默认值")))
            );
            multilineControl.Append(multilineProperties, multilineContent);
            mainPart.Document.Body.Append(multilineControl);

            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "#页眉字段#" }),
                        new SdtContentRun(
                            new Run(new Text("123")),
                            new SdtRun(
                                new SdtProperties(),
                                new SdtContentRun(new Run(new Text("123")))
                            )
                        )
                    )
                )
            );
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

            worksheet.Cells[3, 1].Value = "#多行#";
            worksheet.Cells[3, 2].Value = "Line1\nLine2\nLine3";

            worksheet.Cells[4, 1].Value = "#页眉字段#";
            worksheet.Cells[4, 2].Value = "456";

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

        [Fact]
        public async Task EndToEnd_ExcelToWord_PreservesLineBreaks()
        {
            var excelData = await _excelParser.ParseExcelFileAsync(_testExcelPath);

            var result = await _documentProcessor.ProcessDocumentWithFormattedDataAsync(
                _testTemplatePath,
                excelData,
                _outputPath
            );

            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(_outputPath));

            using var outputDoc = WordprocessingDocument.Open(_outputPath, false);
            var sdt = outputDoc.MainDocumentPart?.Document.Body?.Descendants<SdtBlock>()
                .FirstOrDefault(b => b.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "#多行#");

            Assert.NotNull(sdt);
            var breaks = sdt!.Descendants<Break>().ToList();
            Assert.Equal(2, breaks.Count);

            var texts = sdt.Descendants<Text>().Select(t => t.Text).ToList();
            Assert.Contains("Line1", texts);
            Assert.Contains("Line2", texts);
            Assert.Contains("Line3", texts);
        }

        [Fact]
        public async Task EndToEnd_ExcelToWord_HeaderControl_ShouldNotLeaveOldText()
        {
            var excelData = await _excelParser.ParseExcelFileAsync(_testExcelPath);

            var result = await _documentProcessor.ProcessDocumentWithFormattedDataAsync(
                _testTemplatePath,
                excelData,
                _outputPath
            );

            Assert.True(result.IsSuccess);
            using var outputDoc = WordprocessingDocument.Open(_outputPath, false);
            var headerText = outputDoc.MainDocumentPart?.HeaderParts.First().Header?.InnerText;
            Assert.Equal("456", headerText);
        }

        /// <summary>
        /// 创建三列格式 Excel 文件（ID | 关键词 | 值）
        /// </summary>
        private void CreateThreeColumnTestExcel(string path)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // 三列格式：ID | 关键词 | 值
            worksheet.Cells[1, 1].Value = "ID-001";
            worksheet.Cells[1, 2].Value = "#产品名称#";
            worksheet.Cells[1, 3].Value = "三列测试产品";

            worksheet.Cells[2, 1].Value = "ID-002";
            worksheet.Cells[2, 2].Value = "#规格#";
            worksheet.Cells[2, 3].Value = "200ml";

            worksheet.Cells[3, 1].Value = "ID-003";
            worksheet.Cells[3, 2].Value = "#多行#";
            worksheet.Cells[3, 3].Value = "第一行\n第二行";

            worksheet.Cells[4, 1].Value = "ID-004";
            worksheet.Cells[4, 2].Value = "#页眉字段#";
            worksheet.Cells[4, 3].Value = "页眉值";

            package.SaveAs(new System.IO.FileInfo(path));
        }

        [Fact]
        public async Task EndToEnd_ThreeColumnExcelToWord_ReplacesCorrectlyAndExcludesIds()
        {
            // Arrange - 创建三列格式的 Excel 文件和独立输出路径
            var testDir = Path.GetDirectoryName(_testTemplatePath)!;
            var threeColExcelPath = Path.Combine(testDir, "three_column_data.xlsx");
            var threeColOutputPath = Path.Combine(testDir, "output_three_column.docx");
            CreateThreeColumnTestExcel(threeColExcelPath);

            try
            {
                // Act - 三列格式经 ParseExcelFileAsync 解析
                var excelData = await _excelParser.ParseExcelFileAsync(threeColExcelPath);

                // Assert - 解析结果应包含关键词，不包含 ID 值
                Assert.Equal(4, excelData.Count);
                Assert.True(excelData.ContainsKey("#产品名称#"));
                Assert.Equal("三列测试产品", excelData["#产品名称#"].PlainText);
                Assert.Equal("200ml", excelData["#规格#"].PlainText);
                Assert.DoesNotContain("ID-001", excelData.Keys);
                Assert.DoesNotContain("ID-002", excelData.Keys);

                // Act - 经 ProcessDocumentWithFormattedDataAsync 处理为 Word
                var result = await _documentProcessor.ProcessDocumentWithFormattedDataAsync(
                    _testTemplatePath,
                    excelData,
                    threeColOutputPath
                );

                // Assert - 处理成功
                Assert.True(result.IsSuccess);
                Assert.True(File.Exists(threeColOutputPath));

                using var outputDoc = WordprocessingDocument.Open(threeColOutputPath, false);
                var body = outputDoc.MainDocumentPart?.Document.Body;
                Assert.NotNull(body);

                // 验证产品名称控件被替换为正确值
                var productNameControl = body!.Descendants<SdtBlock>()
                    .FirstOrDefault(b => b.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "#产品名称#");
                Assert.NotNull(productNameControl);
                var productNameTexts = productNameControl!.Descendants<Text>().Select(t => t.Text);
                Assert.Contains("三列测试产品", productNameTexts);

                // 验证规格控件被替换为正确值
                var specControl = body.Descendants<SdtBlock>()
                    .FirstOrDefault(b => b.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "#规格#");
                Assert.NotNull(specControl);
                var specTexts = specControl!.Descendants<Text>().Select(t => t.Text);
                Assert.Contains("200ml", specTexts);

                // 验证多行控件被替换为正确值
                var multilineControl = body.Descendants<SdtBlock>()
                    .FirstOrDefault(b => b.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "#多行#");
                Assert.NotNull(multilineControl);
                var multilineTexts = multilineControl!.Descendants<Text>().Select(t => t.Text);
                Assert.Contains("第一行", multilineTexts);
                Assert.Contains("第二行", multilineTexts);

                // 验证 ID 列值不出现在输出文档正文中
                var allBodyText = string.Join("", body.Descendants<Text>().Select(t => t.Text));
                Assert.DoesNotContain("ID-001", allBodyText);
                Assert.DoesNotContain("ID-002", allBodyText);
                Assert.DoesNotContain("ID-003", allBodyText);
                Assert.DoesNotContain("ID-004", allBodyText);

                // 验证页眉替换正确
                var headerText = outputDoc.MainDocumentPart?.HeaderParts.First().Header?.InnerText;
                Assert.Equal("页眉值", headerText);
            }
            finally
            {
                if (File.Exists(threeColExcelPath)) File.Delete(threeColExcelPath);
                if (File.Exists(threeColOutputPath)) File.Delete(threeColOutputPath);
            }
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
