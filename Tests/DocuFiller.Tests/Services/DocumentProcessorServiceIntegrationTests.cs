using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// DocumentProcessorService 集成测试
    /// 测试格式化内容在表格单元格中的处理
    /// </summary>
    public class DocumentProcessorServiceIntegrationTests : IAsyncDisposable
    {
        private readonly string _testDataDirectory;
        private readonly List<string> _filesToDelete = new List<string>();

        public DocumentProcessorServiceIntegrationTests()
        {
            _testDataDirectory = Path.Combine("Tests", "TestData", "DocumentProcessorServiceIntegration");
            if (!Directory.Exists(_testDataDirectory))
            {
                Directory.CreateDirectory(_testDataDirectory);
            }
        }

        [Fact]
        public async Task ProcessDocumentWithFormattedDataAsync_InTableCell_PreservesTableStructure()
        {
            // Arrange
            var testFile = Path.Combine(_testDataDirectory, "formatted_table_template.docx");
            var outputFile = Path.Combine(_testDataDirectory, "formatted_table_output.docx");

            _filesToDelete.Add(testFile);
            _filesToDelete.Add(outputFile);

            // 创建测试文档
            CreateTestDocumentWithTableContentControl(testFile);

            var logger = new NullLogger<DocumentProcessorService>();
            var progressLogger = new NullLogger<ProgressReporterService>();
            var dataParser = new MockDataParser();
            var excelDataParser = new MockExcelDataParser();
            var fileService = new FileService();
            var progressReporter = new ProgressReporterService(progressLogger);
            var safeFormattedContentReplacer = new SafeFormattedContentReplacer(
                new NullLogger<SafeFormattedContentReplacer>());
            var commentManager = new CommentManager(new NullLogger<CommentManager>());
            var contentControlProcessor = new ContentControlProcessor(
                new NullLogger<ContentControlProcessor>(),
                commentManager,
                new SafeTextReplacer(new NullLogger<SafeTextReplacer>())
            );
            var serviceProvider = new MockServiceProvider();

            var processor = new DocumentProcessorService(
                logger, dataParser, excelDataParser, fileService, progressReporter,
                contentControlProcessor, commentManager, serviceProvider,
                safeFormattedContentReplacer
            );

            var formattedData = new Dictionary<string, FormattedCellValue>
            {
                { "table_field", new FormattedCellValue() }
            };
            formattedData["table_field"].Fragments = new List<TextFragment>
            {
                new TextFragment { Text = "H", IsSubscript = false },
                new TextFragment { Text = "2", IsSubscript = true },
                new TextFragment { Text = "O", IsSubscript = false }
            };

            // Act
            var result = await processor.ProcessDocumentWithFormattedDataAsync(
                testFile, formattedData, outputFile
            );

            // Assert
            Assert.True(result.IsSuccess);

            using var document = WordprocessingDocument.Open(outputFile, false);
            var table = document.MainDocumentPart.Document.Descendants<Table>().First();
            var cell = table.Descendants<TableCell>().First();

            // 验证单元格结构完整
            var paragraphs = cell.Elements<Paragraph>().ToList();
            Assert.Single(paragraphs);

            // 验证内容被正确替换
            var textElements = cell.Descendants<Text>().ToList();
            Assert.Equal(3, textElements.Count);
            Assert.Equal("H", textElements[0].Text);
            Assert.Equal("2", textElements[1].Text);
            Assert.Equal("O", textElements[2].Text);

            // 验证下标格式
            var run = textElements[1].Ancestors<Run>().First();
            var runProperties = run.RunProperties;
            Assert.NotNull(runProperties);
            var verticalAlignment = runProperties.GetFirstChild<VerticalTextAlignment>();
            Assert.NotNull(verticalAlignment);
            Assert.Equal(VerticalPositionValues.Subscript, verticalAlignment.Val.Value);
        }

        /// <summary>
        /// 创建包含表格和内容控件的测试文档
        /// </summary>
        private void CreateTestDocumentWithTableContentControl(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            var table = new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(
                            new SdtRun(
                                new SdtProperties(
                                    new Tag() { Val = "table_field" }
                                ),
                                new SdtContentRun(
                                    new Run(new Text("old"))
                                )
                            )
                        )
                    )
                )
            );

            mainPart.Document.Body = new Body(table);
            mainPart.Document.Save();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var file in _filesToDelete)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // 忽略清理失败
                    }
                }
            }

            if (Directory.Exists(_testDataDirectory))
            {
                try
                {
                    Directory.Delete(_testDataDirectory, true);
                }
                catch
                {
                    // 忽略清理失败
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Mock 数据解析器
        /// </summary>
        private class MockDataParser : IDataParser
        {
            public Task<List<Dictionary<string, object>>> ParseJsonFileAsync(string filePath)
            {
                return Task.FromResult(new List<Dictionary<string, object>>());
            }

            public List<Dictionary<string, object>> ParseJsonString(string jsonContent)
            {
                return new List<Dictionary<string, object>>();
            }

            public Task<ValidationResult> ValidateJsonFileAsync(string filePath)
            {
                return Task.FromResult(new ValidationResult { IsValid = true });
            }

            public ValidationResult ValidateJsonString(string jsonContent)
            {
                return new ValidationResult { IsValid = true };
            }

            public Task<List<Dictionary<string, object>>> GetDataPreviewAsync(string filePath, int maxRecords = 10)
            {
                return Task.FromResult(new List<Dictionary<string, object>>());
            }

            public Task<DataStatistics> GetDataStatisticsAsync(string filePath)
            {
                return Task.FromResult(new DataStatistics());
            }
        }

        /// <summary>
        /// Mock Excel 数据解析器
        /// </summary>
        private class MockExcelDataParser : IExcelDataParser
        {
            public Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(string filePath, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Dictionary<string, FormattedCellValue>());
            }

            public Task<ExcelValidationResult> ValidateExcelFileAsync(string filePath)
            {
                return Task.FromResult(new ExcelValidationResult { IsValid = true });
            }

            public Task<List<Dictionary<string, FormattedCellValue>>> GetDataPreviewAsync(string filePath, int maxRows = 10)
            {
                return Task.FromResult(new List<Dictionary<string, FormattedCellValue>>());
            }

            public Task<ExcelFileSummary> GetDataStatisticsAsync(string filePath)
            {
                return Task.FromResult(new ExcelFileSummary());
            }
        }

        /// <summary>
        /// Mock 服务提供者
        /// </summary>
        private class MockServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}
