using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// 取消功能单元测试
    /// 验证 T02 实现的 CancellationToken 穿透和 CancelProcessing() 功能
    /// </summary>
    public class CancellationTests : IDisposable
    {
        private readonly string _testDataDirectory;

        public CancellationTests()
        {
            _testDataDirectory = Path.Combine(Path.GetTempPath(), $"CancellationTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDataDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDataDirectory))
                {
                    Directory.Delete(_testDataDirectory, true);
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        /// <summary>
        /// 创建用于测试的 DocumentProcessorService 实例
        /// 使用真实 FileService + mock 其他依赖项
        /// </summary>
        private DocumentProcessorService CreateProcessor(
            out Mock<IExcelDataParser> excelParserMock,
            out Mock<IProgressReporter> progressReporterMock,
            out Mock<ISafeFormattedContentReplacer> formattedReplacerMock)
        {
            excelParserMock = new Mock<IExcelDataParser>();
            progressReporterMock = new Mock<IProgressReporter>();
            formattedReplacerMock = new Mock<ISafeFormattedContentReplacer>();

            var fileService = new FileService(new NullLogger<FileService>());
            var commentManager = new CommentManager(new NullLogger<CommentManager>());
            var contentControlProcessor = new ContentControlProcessor(
                new NullLogger<ContentControlProcessor>(),
                commentManager,
                new SafeTextReplacer(new NullLogger<SafeTextReplacer>()));
            var serviceProvider = new Mock<IServiceProvider>();

            var processor = new DocumentProcessorService(
                new NullLogger<DocumentProcessorService>(),
                excelParserMock.Object,
                fileService,
                progressReporterMock.Object,
                contentControlProcessor,
                commentManager,
                serviceProvider.Object,
                formattedReplacerMock.Object);

            return processor;
        }

        /// <summary>
        /// 测试 ProcessFolderAsync 在取消时标记处理失败
        /// 使用 pre-cancelled token，验证取消请求被传播到处理结果
        /// </summary>
        [Fact]
        public async Task ProcessFolderAsync_PreCancelledToken_RecordsFailureInResult()
        {
            // Arrange
            var processor = CreateProcessor(
                out var excelParserMock,
                out var progressReporterMock,
                out var formattedReplacerMock);

            // 创建 pre-cancelled token
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var cancelledToken = cts.Token;

            // 创建假的 data.xlsx 文件
            var dataFilePath = Path.Combine(_testDataDirectory, "data.xlsx");
            File.WriteAllText(dataFilePath, "fake");

            var request = new FolderProcessRequest
            {
                TemplateFolderPath = _testDataDirectory,
                DataFilePath = dataFilePath,
                OutputDirectory = Path.Combine(_testDataDirectory, "output"),
                TemplateFiles = new List<Models.FileInfo>
                {
                    new Models.FileInfo
                    {
                        Name = "template.docx",
                        FullPath = Path.Combine(_testDataDirectory, "template.docx"),
                        Size = 1024,
                        LastModified = DateTime.Now,
                        Extension = ".docx"
                    }
                }
            };

            // 配置 Excel 解析器返回有效数据
            var excelData = new Dictionary<string, FormattedCellValue>
            {
                { "#field#", new FormattedCellValue { Fragments = new List<TextFragment> { new TextFragment { Text = "value" } } } }
            };
            excelParserMock
                .Setup(x => x.ParseExcelFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(excelData);

            // Act
            var result = await processor.ProcessFolderAsync(request, cancelledToken);

            // Assert: 取消被传播到处理结果（文件处理失败）
            Assert.False(result.IsSuccess);
            Assert.True(result.TotalFailed > 0, "应至少有一个失败的文件（取消触发）");
        }

        /// <summary>
        /// 测试 ProcessDocumentsAsync 的 CancellationToken 传递
        /// 使用 pre-cancelled token，验证处理在模板验证通过后正确响应取消
        /// </summary>
        [Fact]
        public async Task ProcessDocumentsAsync_PreCancelledToken_RecordsErrorInResult()
        {
            // Arrange
            var processor = CreateProcessor(
                out var excelParserMock,
                out var progressReporterMock,
                out var formattedReplacerMock);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // 创建一个有效的测试模板文件
            var templatePath = Path.Combine(_testDataDirectory, "template.docx");
            CreateMinimalDocx(templatePath);

            // 创建假数据文件
            var dataPath = Path.Combine(_testDataDirectory, "data.xlsx");
            File.WriteAllText(dataPath, "fake");

            // 配置 Excel 解析器返回有效数据
            var excelData = new Dictionary<string, FormattedCellValue>
            {
                { "#field#", new FormattedCellValue { Fragments = new List<TextFragment> { new TextFragment { Text = "value" } } } }
            };
            excelParserMock
                .Setup(x => x.ParseExcelFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(excelData);

            var request = new ProcessRequest
            {
                TemplateFilePath = templatePath,
                DataFilePath = dataPath,
                OutputDirectory = _testDataDirectory
            };

            // Act
            var result = await processor.ProcessDocumentsAsync(request, cts.Token);

            // Assert: 取消被记录为错误（不抛出异常，因为内部 catch 捕获了 OCE）
            Assert.False(result.IsSuccess);
        }

        /// <summary>
        /// 测试 CancelProcessing() 能中断处理并记录失败
        /// CancelProcessing() 调用后，ProcessFolderAsync 的后续处理应被中断
        /// </summary>
        [Fact]
        public async Task CancelProcessing_InterruptsProcessFolderAsync_RecordsFailure()
        {
            // Arrange
            var processor = CreateProcessor(
                out var excelParserMock,
                out var progressReporterMock,
                out var formattedReplacerMock);

            var dataFilePath = Path.Combine(_testDataDirectory, "data.xlsx");
            File.WriteAllText(dataFilePath, "fake");

            // 创建两个模板文件——第一个会在处理时被取消打断
            var request = new FolderProcessRequest
            {
                TemplateFolderPath = _testDataDirectory,
                DataFilePath = dataFilePath,
                OutputDirectory = Path.Combine(_testDataDirectory, "output"),
                TemplateFiles = new List<Models.FileInfo>
                {
                    new Models.FileInfo
                    {
                        Name = "template1.docx",
                        FullPath = Path.Combine(_testDataDirectory, "template1.docx"),
                        Size = 1024,
                        LastModified = DateTime.Now,
                        Extension = ".docx"
                    },
                    new Models.FileInfo
                    {
                        Name = "template2.docx",
                        FullPath = Path.Combine(_testDataDirectory, "template2.docx"),
                        Size = 1024,
                        LastModified = DateTime.Now,
                        Extension = ".docx"
                    }
                }
            };

            // 配置 Excel 解析器在返回数据后触发取消
            var excelData = new Dictionary<string, FormattedCellValue>
            {
                { "#field#", new FormattedCellValue { Fragments = new List<TextFragment> { new TextFragment { Text = "value" } } } }
            };

            int parseCallCount = 0;
            excelParserMock
                .Setup(x => x.ParseExcelFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((_, _) =>
                {
                    parseCallCount++;
                    // 在第一次解析后触发取消
                    if (parseCallCount == 1)
                    {
                        processor.CancelProcessing();
                    }
                })
                .ReturnsAsync(excelData);

            // Act
            var result = await processor.ProcessFolderAsync(request, CancellationToken.None);

            // Assert: 取消应该导致处理失败
            Assert.False(result.IsSuccess);
            // 至少一个文件应该失败（被取消的文件）
            Assert.True(result.TotalFailed > 0, "至少应有一个文件因取消而失败");
        }

        /// <summary>
        /// 创建最小的 .docx 文件用于测试
        /// </summary>
        private void CreateMinimalDocx(string path)
        {
            using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(
                path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
                new DocumentFormat.OpenXml.Wordprocessing.Body());
            mainPart.Document.Save();
        }
    }
}
