using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocuFiller.Services;
using DocuFiller.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace DocuFiller.Tests
{
    public class ExcelDataParserServiceTests : IDisposable
    {
        private readonly string _testFilePath;
        private readonly ExcelDataParserService _parser;

        public ExcelDataParserServiceTests()
        {
            // 创建测试 Excel 文件
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");
            CreateTestExcelFile(_testFilePath);

            var logger = LoggerFactory.Create(builder => { }).CreateLogger<ExcelDataParserService>();
            var fileService = new FileService();
            _parser = new ExcelDataParserService(logger, fileService);
        }

        private void CreateTestExcelFile(string path)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // 添加测试数据
            worksheet.Cells[1, 1].Value = "#产品名称#";
            worksheet.Cells[1, 2].Value = "D-二聚体测定试剂盒";

            worksheet.Cells[2, 1].Value = "#型号#";
            worksheet.Cells[2, 2].Value = "Type-A";

            // 添加带上标的单元格
            var cell = worksheet.Cells[3, 1];
            cell.Value = "#规格#";
            worksheet.Cells[3, 2].Value = "2x10";
            worksheet.Cells[3, 2].RichText.Add("9").VerticalAlign = OfficeOpenXml.Style.ExcelVerticalAlignmentFont.Superscript;

            package.SaveAs(new System.IO.FileInfo(path));
        }

        [Fact]
        public async Task ParseExcelFileAsync_ValidFile_ReturnsData()
        {
            // Act
            var result = await _parser.ParseExcelFileAsync(_testFilePath);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.True(result.ContainsKey("#产品名称#"));
            Assert.Equal("D-二聚体测定试剂盒", result["#产品名称#"].PlainText);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_ValidFile_PassesValidation()
        {
            // Act
            var result = await _parser.ValidateExcelFileAsync(_testFilePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(3, result.Summary.ValidKeywordRows);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation()
        {
            // Arrange - 创建包含错误格式的文件
            var invalidFilePath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid()}.xlsx");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");
            worksheet.Cells[1, 1].Value = "InvalidKeyword"; // 不以 # 开头结尾
            worksheet.Cells[1, 2].Value = "Value";
            package.SaveAs(new System.IO.FileInfo(invalidFilePath));

            // Act
            var result = await _parser.ValidateExcelFileAsync(invalidFilePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Summary.InvalidFormatKeywords);

            // Cleanup
            File.Delete(invalidFilePath);
        }

        public void Dispose()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }
    }
}
