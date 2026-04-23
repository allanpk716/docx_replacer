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

        // === 三列格式测试 ===

        /// <summary>
        /// 辅助方法：创建三列格式 Excel 文件（ID | 关键词 | 值）
        /// </summary>
        private string CreateThreeColumnExcelFile(Action<ExcelWorksheet, string>? customize = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"threeCol_{Guid.NewGuid()}.xlsx");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // 三列格式：ID | 关键词 | 值
            worksheet.Cells[1, 1].Value = "001";
            worksheet.Cells[1, 2].Value = "#产品名称#";
            worksheet.Cells[1, 3].Value = "D-二聚体测定试剂盒";

            worksheet.Cells[2, 1].Value = "002";
            worksheet.Cells[2, 2].Value = "#型号#";
            worksheet.Cells[2, 3].Value = "Type-A";

            worksheet.Cells[3, 1].Value = "003";
            worksheet.Cells[3, 2].Value = "#规格#";
            worksheet.Cells[3, 3].Value = "100ml";

            customize?.Invoke(worksheet, path);
            if (customize == null)
            {
                package.SaveAs(new System.IO.FileInfo(path));
            }
            return path;
        }

        [Fact]
        public async Task ParseExcelFileAsync_ThreeColumnFormat_SkipsIdAndParsesCorrectly()
        {
            // Arrange
            var path = CreateThreeColumnExcelFile();

            // Act
            var result = await _parser.ParseExcelFileAsync(path);

            // Assert - ID 列不影响关键词，关键词来自第二列，值来自第三列
            Assert.Equal(3, result.Count);
            Assert.True(result.ContainsKey("#产品名称#"));
            Assert.Equal("D-二聚体测定试剂盒", result["#产品名称#"].PlainText);
            Assert.Equal("Type-A", result["#型号#"].PlainText);
            Assert.Equal("100ml", result["#规格#"].PlainText);

            File.Delete(path);
        }

        [Fact]
        public async Task ParseExcelFileAsync_ThreeColumnFormat_DoesNotIncludeIdColumn()
        {
            // Arrange
            var path = CreateThreeColumnExcelFile();

            // Act
            var result = await _parser.ParseExcelFileAsync(path);

            // Assert - ID 值不应作为关键词出现
            Assert.False(result.ContainsKey("001"));
            Assert.False(result.ContainsKey("002"));
            Assert.False(result.ContainsKey("003"));

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_ThreeColumnFormat_PassesValidation()
        {
            // Arrange
            var path = CreateThreeColumnExcelFile();

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(3, result.Summary.ValidKeywordRows);
            Assert.Empty(result.Summary.DuplicateRowIds);

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds()
        {
            // Arrange - 创建包含重复 ID 的三列格式文件
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"dupId_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                worksheet.Cells[1, 1].Value = "001";
                worksheet.Cells[1, 2].Value = "#产品名称#";
                worksheet.Cells[1, 3].Value = "产品A";

                worksheet.Cells[2, 1].Value = "001"; // 重复 ID
                worksheet.Cells[2, 2].Value = "#型号#";
                worksheet.Cells[2, 3].Value = "Type-A";

                worksheet.Cells[3, 1].Value = "002";
                worksheet.Cells[3, 2].Value = "#规格#";
                worksheet.Cells[3, 3].Value = "100ml";

                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("001", result.Summary.DuplicateRowIds);
            Assert.Contains("重复 ID", result.Errors[0]);

            File.Delete(path);
        }

        // === 边界场景测试 (S02 T01) ===

        [Fact]
        public async Task ParseExcelFileAsync_EmptyFile_ThrowsNullReference()
        {
            // Arrange - 创建空工作表（有 worksheet 但无数据行）
            // 当前服务的 ParseExcelFileAsync 在 Dimension == null 时不做保护检查，
            // 会抛出 NullReferenceException（由外层 catch 捕获并重新抛出）
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add("Sheet1");
                // 不写入任何数据行 -> Dimension == null
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act & Assert - 空文件导致 Dimension == null，访问 .Rows 时抛 NullReferenceException
            await Assert.ThrowsAsync<NullReferenceException>(() => _parser.ParseExcelFileAsync(path));

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_EmptyFile_ReportsError()
        {
            // Arrange - 空工作表
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"empty_val_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add("Sheet1");
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert - 空文件的 Dimension == null, 服务返回"工作表为空"错误
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("空"));

            File.Delete(path);
        }

        [Fact]
        public async Task ParseExcelFileAsync_BlankFirstRows_DetectsFormatFromFirstDataRow()
        {
            // Arrange - 前两行第一列为空，数据从第三行开始
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"blankfirst_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                // 前两行空白（写入第二列占位确保 Dimension 不为 null）
                worksheet.Cells[1, 2].Value = "";
                worksheet.Cells[2, 2].Value = "";
                // 第三行开始有数据 - 三列格式
                worksheet.Cells[3, 1].Value = "001";
                worksheet.Cells[3, 2].Value = "#产品名称#";
                worksheet.Cells[3, 3].Value = "产品A";
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ParseExcelFileAsync(path);

            // Assert - 跳过空行，正确从第三行检测三列格式并解析
            Assert.Single(result);
            Assert.True(result.ContainsKey("#产品名称#"));
            Assert.Equal("产品A", result["#产品名称#"].PlainText);

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_BlankFirstRows_DetectsThreeColumnCorrectly()
        {
            // Arrange - 前两行空白，第三行开始有三列数据
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"blankfirst_val_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                worksheet.Cells[1, 2].Value = "";
                worksheet.Cells[2, 2].Value = "";
                worksheet.Cells[3, 1].Value = "001";
                worksheet.Cells[3, 2].Value = "#产品名称#";
                worksheet.Cells[3, 3].Value = "产品A";
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert - 验证通过
            Assert.True(result.IsValid);
            Assert.Equal(1, result.Summary.ValidKeywordRows);

            File.Delete(path);
        }

        [Fact]
        public async Task ParseExcelFileAsync_ThreeColumnEmptyId_ParsesCorrectly()
        {
            // Arrange - 三列格式，ID 列为空但关键词/值列有效
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"emptyid_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                // 第一行 ID 为空
                worksheet.Cells[1, 1].Value = "";
                worksheet.Cells[1, 2].Value = "#产品名称#";
                worksheet.Cells[1, 3].Value = "产品A";
                // 第二行 ID 不为空
                worksheet.Cells[2, 1].Value = "002";
                worksheet.Cells[2, 2].Value = "#型号#";
                worksheet.Cells[2, 3].Value = "Type-B";
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ParseExcelFileAsync(path);

            // Assert - 不崩溃，正常解析关键词
            Assert.Equal(2, result.Count);
            Assert.Equal("产品A", result["#产品名称#"].PlainText);
            Assert.Equal("Type-B", result["#型号#"].PlainText);

            File.Delete(path);
        }

        [Fact]
        public async Task ParseExcelFileAsync_SingleRowThreeColumn_ParsesCorrectly()
        {
            // Arrange - 只有一行数据的三列格式
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"singlerow_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                worksheet.Cells[1, 1].Value = "001";
                worksheet.Cells[1, 2].Value = "#唯一字段#";
                worksheet.Cells[1, 3].Value = "唯一值";
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ParseExcelFileAsync(path);

            // Assert
            Assert.Single(result);
            Assert.Equal("唯一值", result["#唯一字段#"].PlainText);

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_SingleRowThreeColumn_PassesValidation()
        {
            // Arrange - 只有一行数据的三列格式
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"singlerow_val_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                worksheet.Cells[1, 1].Value = "001";
                worksheet.Cells[1, 2].Value = "#唯一字段#";
                worksheet.Cells[1, 3].Value = "唯一值";
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(1, result.Summary.ValidKeywordRows);
            Assert.Empty(result.Summary.DuplicateRowIds);

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_ThreeColumnIdWithSpaces_TrimmedForDuplicateDetection()
        {
            // Arrange - ID 值含前后空格，trim 后应被视为相同 ID
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"idspace_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                worksheet.Cells[1, 1].Value = "  001  ";
                worksheet.Cells[1, 2].Value = "#产品名称#";
                worksheet.Cells[1, 3].Value = "产品A";

                worksheet.Cells[2, 1].Value = "001";
                worksheet.Cells[2, 2].Value = "#型号#";
                worksheet.Cells[2, 3].Value = "Type-A";

                worksheet.Cells[3, 1].Value = "002";
                worksheet.Cells[3, 2].Value = "#规格#";
                worksheet.Cells[3, 3].Value = "100ml";
                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert - "  001  " trim 后与 "001" 相同，应检测为重复
            Assert.False(result.IsValid);
            Assert.Contains("001", result.Summary.DuplicateRowIds);

            File.Delete(path);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_MultipleDuplicateIds_AllReported()
        {
            // Arrange - 多个不同 ID 各有重复
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var path = Path.Combine(Path.GetTempPath(), $"multidup_{Guid.NewGuid()}.xlsx");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                worksheet.Cells[1, 1].Value = "001";
                worksheet.Cells[1, 2].Value = "#产品名称#";
                worksheet.Cells[1, 3].Value = "产品A";

                worksheet.Cells[2, 1].Value = "001"; // 重复
                worksheet.Cells[2, 2].Value = "#型号#";
                worksheet.Cells[2, 3].Value = "Type-A";

                worksheet.Cells[3, 1].Value = "002";
                worksheet.Cells[3, 2].Value = "#规格#";
                worksheet.Cells[3, 3].Value = "100ml";

                worksheet.Cells[4, 1].Value = "002"; // 重复
                worksheet.Cells[4, 2].Value = "#批号#";
                worksheet.Cells[4, 3].Value = "B20240101";

                worksheet.Cells[5, 1].Value = "003";
                worksheet.Cells[5, 2].Value = "#有效期#";
                worksheet.Cells[5, 3].Value = "2025-12-31";

                package.SaveAs(new System.IO.FileInfo(path));
            }

            // Act
            var result = await _parser.ValidateExcelFileAsync(path);

            // Assert - 两个重复 ID 都应被检测到
            Assert.False(result.IsValid);
            Assert.Contains("001", result.Summary.DuplicateRowIds);
            Assert.Contains("002", result.Summary.DuplicateRowIds);
            Assert.Equal(2, result.Summary.DuplicateRowIds.Count);

            File.Delete(path);
        }

        [Fact]
        public async Task ParseExcelFileAsync_TwoColumnFormat_UnchangedBehavior()
        {
            // Arrange - 使用原有的两列格式测试文件（已在构造函数中创建）
            // Act
            var result = await _parser.ParseExcelFileAsync(_testFilePath);

            // Assert - 行为应与之前完全一致
            Assert.Equal(3, result.Count);
            Assert.Equal("D-二聚体测定试剂盒", result["#产品名称#"].PlainText);
            Assert.Equal("Type-A", result["#型号#"].PlainText);
        }

        [Fact]
        public async Task ValidateExcelFileAsync_TwoColumnFormat_NoDuplicateRowIds()
        {
            // Arrange - 使用原有的两列格式测试文件
            // Act
            var result = await _parser.ValidateExcelFileAsync(_testFilePath);

            // Assert - 两列模式下不应有重复 ID 检测
            Assert.Empty(result.Summary.DuplicateRowIds);
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
