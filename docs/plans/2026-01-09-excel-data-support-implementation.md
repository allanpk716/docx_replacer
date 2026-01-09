# Excel 数据支持实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 扩展 DocuFiller 支持 Excel (.xlsx) 作为数据输入源，保留上标、下标格式，并提供 JSON 转 Excel 转换工具。

**Architecture:** 使用 EPPlus 读取 Excel 富文本格式，扩展 DocumentProcessor 支持 FormattedCellValue 模型，创建独立的转换工具窗口。保持向后兼容 JSON 支持。

**Tech Stack:** EPPlus 7.x, OpenXML SDK, WPF, Microsoft.Extensions.DependencyInjection

---

## 前置任务

### Task 0: 添加 EPPlus NuGet 包

**Files:**
- Modify: `DocuFiller.csproj`

**Step 1: 添加 EPPlus 包引用**

编辑 `DocuFiller.csproj`，在 `<ItemGroup>` 中添加：

```xml
<PackageReference Include="EPPlus" Version="7.5.2" />
```

**Step 2: 恢复依赖**

运行: `dotnet restore`
预期输出: 包恢复成功，无错误

**Step 3: 提交**

```bash
git add DocuFiller.csproj
git commit -m "feat: 添加 EPPlus NuGet 包依赖"
```

---

## 阶段一：核心数据模型和解析

### Task 1: 创建格式化数据模型

**Files:**
- Create: `Models/FormattedCellValue.cs`
- Create: `Models/TextFragment.cs`

**Step 1: 创建 TextFragment 模型**

创建文件 `Models/TextFragment.cs`:

```csharp
using System.Xml.Serialization;

namespace DocuFiller.Models
{
    /// <summary>
    /// 表示单个文本片段及其格式信息
    /// </summary>
    public class TextFragment
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 是否为上标
        /// </summary>
        public bool IsSuperscript { get; set; }

        /// <summary>
        /// 是否为下标
        /// </summary>
        public bool IsSubscript { get; set; }

        public override string ToString()
        {
            var formats = new List<string>();
            if (IsSuperscript) formats.Add("上标");
            if (IsSubscript) formats.Add("下标");
            var formatStr = formats.Count > 0 ? $" [{string.Join(", ", formats)}]" : "";
            return $"\"{Text}\"{formatStr}";
        }
    }
}
```

**Step 2: 创建 FormattedCellValue 模型**

创建文件 `Models/FormattedCellValue.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace DocuFiller.Models
{
    /// <summary>
    /// 表示带格式的单元格值
    /// </summary>
    public class FormattedCellValue
    {
        /// <summary>
        /// 纯文本内容（用于验证和显示）
        /// </summary>
        public string PlainText => string.Join("", Fragments.Select(f => f.Text));

        /// <summary>
        /// 文本片段列表，每个片段包含内容和格式信息
        /// </summary>
        public List<TextFragment> Fragments { get; set; } = new();

        /// <summary>
        /// 从纯文本创建单个片段的 FormattedCellValue
        /// </summary>
        public static FormattedCellValue FromPlainText(string text)
        {
            return new FormattedCellValue
            {
                Fragments = new List<TextFragment>
                {
                    new TextFragment { Text = text ?? string.Empty }
                }
            };
        }

        public override string ToString()
        {
            return $"FormattedCellValue: {PlainText} ({Fragments.Count} fragments)";
        }
    }
}
```

**Step 3: 编写单元测试**

创建文件 `Tests/FormattedCellValueTests.cs`:

```csharp
using Xunit;
using DocuFiller.Models;

namespace DocuFiller.Tests
{
    public class FormattedCellValueTests
    {
        [Fact]
        public void FromPlainText_CreatesSingleFragment()
        {
            // Arrange & Act
            var value = FormattedCellValue.FromPlainText("test text");

            // Assert
            Assert.Single(value.Fragments);
            Assert.Equal("test text", value.Fragments[0].Text);
            Assert.False(value.Fragments[0].IsSuperscript);
            Assert.False(value.Fragments[0].IsSubscript);
        }

        [Fact]
        public void PlainText_ReturnsConcatenatedFragments()
        {
            // Arrange
            var value = new FormattedCellValue
            {
                Fragments = new List<TextFragment>
                {
                    new TextFragment { Text = "2x10" },
                    new TextFragment { Text = "9", IsSuperscript = true }
                }
            };

            // Act & Assert
            Assert.Equal("2x109", value.PlainText);
        }

        [Fact]
        public void FromPlainText_WithNull_ReturnsEmptyFragment()
        {
            // Arrange & Act
            var value = FormattedCellValue.FromPlainText(null);

            // Assert
            Assert.Single(value.Fragments);
            Assert.Equal("", value.Fragments[0].Text);
        }
    }
}
```

**Step 4: 运行测试**

运行: `dotnet test Tests/DocuFiller.Tests.csproj --filter "FullyQualifiedName~FormattedCellValueTests"`
预期输出: 全部测试通过

**Step 5: 提交**

```bash
git add Models/FormattedCellValue.cs Models/TextFragment.cs Tests/FormattedCellValueTests.cs
git commit -m "feat: 添加格式化数据模型 FormattedCellValue 和 TextFragment"
```

---

### Task 2: 创建 Excel 验证结果模型

**Files:**
- Create: `Models/ExcelValidationResult.cs`
- Create: `Models/ExcelFileSummary.cs`

**Step 1: 创建 ExcelFileSummary 模型**

创建文件 `Models/ExcelFileSummary.cs`:

```csharp
using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// Excel 文件摘要信息
    /// </summary>
    public class ExcelFileSummary
    {
        /// <summary>
        /// 总行数
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// 有效关键词行数
        /// </summary>
        public int ValidKeywordRows { get; set; }

        /// <summary>
        /// 重复的关键词列表
        /// </summary>
        public List<string> DuplicateKeywords { get; set; } = new();

        /// <summary>
        /// 格式不正确的关键词列表
        /// </summary>
        public List<string> InvalidFormatKeywords { get; set; } = new();
    }
}
```

**Step 2: 创建 ExcelValidationResult 模型**

创建文件 `Models/ExcelValidationResult.cs`:

```csharp
using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// Excel 文件验证结果
    /// </summary>
    public class ExcelValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 文件摘要信息
        /// </summary>
        public ExcelFileSummary Summary { get; set; } = new();

        /// <summary>
        /// 添加错误信息
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}
```

**Step 3: 提交**

```bash
git add Models/ExcelValidationResult.cs Models/ExcelFileSummary.cs
git commit -m "feat: 添加 Excel 验证结果模型"
```

---

### Task 3: 创建 IExcelDataParser 接口

**Files:**
- Create: `Services/Interfaces/IExcelDataParser.cs`

**Step 1: 创建接口**

创建文件 `Services/Interfaces/IExcelDataParser.cs`:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// Excel 数据解析服务接口
    /// </summary>
    public interface IExcelDataParser
    {
        /// <summary>
        /// 解析 Excel 数据文件
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <returns>解析后的数据字典（关键词 -> 格式化值）</returns>
        Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(string filePath);

        /// <summary>
        /// 验证 Excel 数据文件
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <returns>验证结果</returns>
        Task<ExcelValidationResult> ValidateExcelFileAsync(string filePath);

        /// <summary>
        /// 获取 Excel 数据预览
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <param name="maxRows">最大行数</param>
        /// <returns>预览数据</returns>
        Task<List<Dictionary<string, FormattedCellValue>>> GetDataPreviewAsync(string filePath, int maxRows = 10);

        /// <summary>
        /// 获取 Excel 数据统计信息
        /// </summary>
        /// <param name="filePath">Excel 文件路径</param>
        /// <returns>统计信息</returns>
        Task<ExcelFileSummary> GetDataStatisticsAsync(string filePath);
    }
}
```

**Step 2: 提交**

```bash
git add Services/Interfaces/IExcelDataParser.cs
git commit -m "feat: 添加 IExcelDataParser 接口"
```

---

### Task 4: 实现 ExcelDataParserService

**Files:**
- Create: `Services/ExcelDataParserService.cs`

**Step 1: 创建服务实现**

创建文件 `Services/ExcelDataParserService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DocuFiller.Services
{
    /// <summary>
    /// Excel 数据解析服务实现
    /// </summary>
    public class ExcelDataParserService : IExcelDataParser
    {
        private readonly ILogger<ExcelDataParserService> _logger;
        private readonly IFileService _fileService;

        // 关键词格式正则：#开头#结尾
        private static readonly Regex KeywordRegex = new Regex(@"^#.*#$", RegexOptions.Compiled);

        public ExcelDataParserService(ILogger<ExcelDataParserService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
            // 设置 EPPlus 许可上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(string filePath)
        {
            var result = new Dictionary<string, FormattedCellValue>();

            try
            {
                _logger.LogInformation($"开始解析 Excel 文件: {filePath}");

                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogError($"Excel 文件不存在: {filePath}");
                    return result;
                }

                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet == null)
                {
                    _logger.LogError("Excel 文件没有工作表");
                    return result;
                }

                // 从第一行开始读取（无表头）
                var rowCount = worksheet.Dimension.Rows;
                for (int row = 1; row <= rowCount; row++)
                {
                    var keyCell = worksheet.Cells[row, 1];
                    var valueCell = worksheet.Cells[row, 2];

                    if (keyCell == null || string.IsNullOrEmpty(keyCell.Text))
                        continue;

                    var keyword = keyCell.Text.Trim();
                    var formattedValue = ParseCell(valueCell);

                    result[keyword] = formattedValue;
                    _logger.LogDebug($"解析行 {row}: {keyword} = {formattedValue.PlainText}");
                }

                _logger.LogInformation($"成功解析 Excel 数据，共 {result.Count} 条记录");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"解析 Excel 文件失败: {filePath}");
                return result;
            }
        }

        public async Task<ExcelValidationResult> ValidateExcelFileAsync(string filePath)
        {
            var result = new ExcelValidationResult { IsValid = true };

            try
            {
                // 文件存在性检查
                if (!_fileService.FileExists(filePath))
                {
                    result.AddError("Excel 文件不存在");
                    return result;
                }

                // 文件扩展名检查
                var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    result.AddError($"不支持的文件格式: {extension}，仅支持 .xlsx 格式");
                    return result;
                }

                using var package = new ExcelPackage(new FileInfo(filePath));
                var workbook = package.Workbook;

                // 工作表检查
                if (workbook.Worksheets.Count == 0)
                {
                    result.AddError("Excel 文件没有工作表");
                    return result;
                }

                var worksheet = workbook.Worksheets[0];
                if (worksheet.Dimension == null)
                {
                    result.AddError("工作表为空");
                    return result;
                }

                // 验证每一行
                var rowCount = worksheet.Dimension.Rows;
                var seenKeywords = new HashSet<string>();

                result.Summary.TotalRows = rowCount;

                for (int row = 1; row <= rowCount; row++)
                {
                    var keyCell = worksheet.Cells[row, 1];
                    var valueCell = worksheet.Cells[row, 2];

                    // 检查第一列是否为空
                    if (keyCell == null || string.IsNullOrEmpty(keyCell.Text))
                    {
                        continue; // 跳过空行
                    }

                    var keyword = keyCell.Text.Trim();

                    // 验证关键词格式
                    if (!KeywordRegex.IsMatch(keyword))
                    {
                        result.Summary.InvalidFormatKeywords.Add($"第 {row} 行: {keyword}");
                    }

                    // 检查重复关键词
                    if (seenKeywords.Contains(keyword))
                    {
                        result.Summary.DuplicateKeywords.Add(keyword);
                    }
                    else
                    {
                        seenKeywords.Add(keyword);
                    }

                    // 检查第二列是否为空（警告）
                    if (valueCell == null || string.IsNullOrEmpty(valueCell.Text))
                    {
                        result.AddWarning($"第 {row} 行: 值列为空（关键词: {keyword}）");
                    }

                    result.Summary.ValidKeywordRows = seenKeywords.Count;
                }

                // 根据检查结果设置 IsValid
                if (result.Summary.InvalidFormatKeywords.Count > 0)
                {
                    result.AddError($"存在 {result.Summary.InvalidFormatKeywords.Count} 个格式不正确的关键词");
                }

                if (result.Summary.DuplicateKeywords.Count > 0)
                {
                    result.AddError($"存在 {result.Summary.DuplicateKeywords.Count} 个重复关键词");
                }

                if (result.Summary.ValidKeywordRows == 0)
                {
                    result.AddError("没有找到有效的关键词数据");
                }

                _logger.LogInformation($"Excel 验证完成: {(result.IsValid ? "通过" : "失败")}, 有效行数: {result.Summary.ValidKeywordRows}");
            }
            catch (Exception ex)
            {
                result.AddError($"验证 Excel 文件时发生异常: {ex.Message}");
                _logger.LogError(ex, $"验证 Excel 文件失败: {filePath}");
            }

            return result;
        }

        public async Task<List<Dictionary<string, FormattedCellValue>>> GetDataPreviewAsync(string filePath, int maxRows = 10)
        {
            var result = new List<Dictionary<string, FormattedCellValue>>();

            try
            {
                var allData = await ParseExcelFileAsync(filePath);
                var previewCount = Math.Min(maxRows, allData.Count);

                // 将字典转换为列表以便预览
                for (int i = 0; i < previewCount; i++)
                {
                    var kvp = allData.ElementAt(i);
                    result.Add(new Dictionary<string, FormattedCellValue>
                    {
                        { kvp.Key, kvp.Value }
                    });
                }

                _logger.LogInformation($"获取数据预览: {result.Count} 条记录");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取数据预览失败: {filePath}");
            }

            return result;
        }

        public async Task<ExcelFileSummary> GetDataStatisticsAsync(string filePath)
        {
            var summary = new ExcelFileSummary();

            try
            {
                var validationResult = await ValidateExcelFileAsync(filePath);
                summary = validationResult.Summary;

                _logger.LogInformation($"获取数据统计: 总行数 {summary.TotalRows}, 有效行数 {summary.ValidKeywordRows}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取数据统计失败: {filePath}");
            }

            return summary;
        }

        /// <summary>
        /// 解析单元格，提取富文本格式
        /// </summary>
        private FormattedCellValue ParseCell(ExcelRange cell)
        {
            var formattedValue = new FormattedCellValue();

            if (cell == null || cell.Value == null)
            {
                formattedValue.Fragments.Add(new TextFragment { Text = "" });
                return formattedValue;
            }

            // 如果是富文本
            if (cell.IsRichText)
            {
                foreach (var rt in cell.RichText)
                {
                    var fragment = new TextFragment
                    {
                        Text = rt.Text,
                        IsSuperscript = rt.VerticalAlign == ExcelVerticalAlignmentFont.Superscript,
                        IsSubscript = rt.VerticalAlign == ExcelVerticalAlignmentFont.Subscript
                    };
                    formattedValue.Fragments.Add(fragment);
                }
            }
            else
            {
                // 普通文本，检查整个单元格的格式
                var fragment = new TextFragment
                {
                    Text = cell.Text ?? "",
                    IsSuperscript = cell.Style.Font.VerticalAlign == ExcelVerticalAlignmentFont.Superscript,
                    IsSubscript = cell.Style.Font.VerticalAlign == ExcelVerticalAlignmentFont.Subscript
                };
                formattedValue.Fragments.Add(fragment);
            }

            return formattedValue;
        }
    }
}
```

**Step 2: 编写单元测试**

创建文件 `Tests/ExcelDataParserServiceTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
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
            var fileService = new FileService(logger);
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

            package.SaveAs(new FileInfo(path));
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
            package.SaveAs(new FileInfo(invalidFilePath));

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
```

**Step 3: 运行测试**

运行: `dotnet test Tests/DocuFiller.Tests.csproj --filter "FullyQualifiedName~ExcelDataParserServiceTests"`
预期输出: 全部测试通过

**Step 4: 提交**

```bash
git add Services/ExcelDataParserService.cs Tests/ExcelDataParserServiceTests.cs
git commit -m "feat: 实现 ExcelDataParserService 及其测试"
```

---

### Task 5: 注册 ExcelDataParserService 到 DI 容器

**Files:**
- Modify: `App.xaml.cs`

**Step 1: 注册服务**

在 `App.xaml.cs` 的 `ConfigureServices` 方法中添加：

```csharp
services.AddSingleton<IExcelDataParser, ExcelDataParserService>();
```

在文件顶部的 using 中添加：

```csharp
using DocuFiller.Services.Interfaces;
```

**Step 2: 运行应用确保启动**

运行: `dotnet build`
预期输出: 编译成功

**Step 3: 提交**

```bash
git add App.xaml.cs
git commit -m "feat: 注册 ExcelDataParserService 到 DI 容器"
```

---

## 阶段二：文档处理扩展

### Task 6: 扩展 IDocumentProcessor 接口

**Files:**
- Modify: `Services/Interfaces/IDocumentProcessor.cs`

**Step 1: 添加新方法**

在 `IDocumentProcessor` 接口中添加新方法：

```csharp
/// <summary>
/// 处理文档（支持格式化值）
/// </summary>
/// <param name="templateFilePath">模板文件路径</param>
/// <param name="formattedData">格式化数据字典</param>
/// <param name="outputFilePath">输出文件路径</param>
/// <returns>处理结果</returns>
Task<ProcessResult> ProcessDocumentWithFormattedDataAsync(
    string templateFilePath,
    Dictionary<string, FormattedCellValue> formattedData,
    string outputFilePath);
```

确保文件顶部已导入：
```csharp
using DocuFiller.Models;
```

**Step 2: 更新 DocumentProcessorService 实现签名**

在 `Services/DocumentProcessorService.cs` 中添加该方法存根：

```csharp
public async Task<ProcessResult> ProcessDocumentWithFormattedDataAsync(
    string templateFilePath,
    Dictionary<string, FormattedCellValue> formattedData,
    string outputFilePath)
{
    // TODO: 实现带格式的文档处理
    throw new NotImplementedException();
}
```

**Step 3: 提交**

```bash
git add Services/Interfaces/IDocumentProcessor.cs Services/DocumentProcessorService.cs
git commit -m "feat: 添加 ProcessDocumentWithFormattedDataAsync 接口方法"
```

---

### Task 7: 实现 Word 格式化填充逻辑

**Files:**
- Modify: `Services/DocumentProcessorService.cs`

**Step 1: 添加辅助方法用于创建带格式的 Run**

在 `DocumentProcessorService` 类中添加以下方法：

```csharp
/// <summary>
/// 用格式化值填充内容控件
/// </summary>
private void FillContentControlWithFormattedValue(
    ContentControlData control,
    FormattedCellValue formattedValue,
    WordprocessingDocument document)
{
    // 清空现有内容
    control.Value = string.Empty;

    // 获取内容控件的 SdtBlock 或 SdtRun
    var sdtElement = FindContentControlElement(document, control.Tag);
    if (sdtElement == null) return;

    // 获取 SdtContent 部分
    var sdtContent = sdtElement.Descendants<SdtContentBlock>().FirstOrDefault()
        ?? sdtElement.Descendants<SdtContentRun>().FirstOrDefault();

    if (sdtContent == null) return;

    // 清空现有内容
    sdtContent.RemoveAllChildren();

    // 创建父容器（Paragraph 或 Run）
    if (sdtContent is SdtContentBlock)
    {
        var paragraph = new Paragraph();
        foreach (var fragment in formattedValue.Fragments)
        {
            var run = CreateFormattedRun(fragment, document);
            paragraph.Append(run);
        }
        sdtContent.Append(paragraph);
    }
    else if (sdtContent is SdtContentRun)
    {
        foreach (var fragment in formattedValue.Fragments)
        {
            var run = CreateFormattedRun(fragment, document);
            sdtContent.Append(run);
        }
    }
}

/// <summary>
/// 创建带格式的 Run 元素
/// </summary>
private Run CreateFormattedRun(TextFragment fragment, WordprocessingDocument document)
{
    var run = new Run(new Text(fragment.Text));

    // 设置上标或下标
    if (fragment.IsSuperscript || fragment.IsSubscript)
    {
        var runProperties = new RunProperties(
            new VerticalTextAlignment
            {
                Val = fragment.IsSuperscript
                    ? VerticalPositionValues.Superscript
                    : VerticalPositionValues.Subscript
            }
        );
        run.InsertBefore(runProperties, run.GetFirstChild<Text>());
    }

    return run;
}

/// <summary>
/// 查找内容控件元素
/// </summary>
private SdtElement FindContentControlElement(WordprocessingDocument document, string tag)
{
    var mainDocumentPart = document.MainDocumentPart;
    if (mainDocumentPart == null) return null;

    // 在正文、页眉、页脚中查找
    var elements = new List<SdtElement>();

    // 正文
    elements.AddRange(mainDocumentPart.Document.Descendants<SdtElement>());

    // 页眉
    foreach (var header in mainDocumentPart.HeaderParts)
    {
        elements.AddRange(header.Header.Descendants<SdtElement>());
    }

    // 页脚
    foreach (var footer in mainDocumentPart.FooterParts)
    {
        elements.AddRange(footer.Footer.Descendants<SdtElement>());
    }

    // 根据 Tag 查找
    return elements.FirstOrDefault(e =>
    {
        var sdtProperties = e.Descendants<SdtProperties>().FirstOrDefault();
        if (sdtProperties == null) return false;

        var tagElement = sdtProperties.Descendants<Tag>().FirstOrDefault();
        return tagElement != null && tagElement.Val == tag;
    });
}
```

确保文件顶部有以下 using：
```csharp
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
```

**Step 2: 实现 ProcessDocumentWithFormattedDataAsync 方法**

替换之前添加的存根实现：

```csharp
public async Task<ProcessResult> ProcessDocumentWithFormattedDataAsync(
    string templateFilePath,
    Dictionary<string, FormattedCellValue> formattedData,
    string outputFilePath)
{
    var result = new ProcessResult { IsSuccess = false };
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        _logger.LogInformation($"开始处理文档（格式化数据）: {templateFilePath}");

        // 验证输入
        if (!_fileService.FileExists(templateFilePath))
        {
            result.Errors.Add($"模板文件不存在: {templateFilePath}");
            return result;
        }

        // 复制模板文件
        File.Copy(templateFilePath, outputFilePath, true);

        // 打开文档进行编辑
        using var document = WordprocessingDocument.Open(outputFilePath, true);

        // 获取模板中的所有内容控件
        var templateControls = await GetContentControlsAsync(templateFilePath);

        // 填充每个内容控件
        foreach (var control in templateControls)
        {
            if (formattedData.TryGetValue(control.Tag, out var formattedValue))
            {
                FillContentControlWithFormattedValue(control, formattedValue, document);
                _logger.LogDebug($"填充内容控件 '{control.Tag}' = {formattedValue.PlainText}");
            }
            else
            {
                _logger.LogWarning($"未找到内容控件 '{control.Tag}' 的数据");
            }
        }

        document.Save();
        document.Close();

        stopwatch.Stop();
        result.IsSuccess = true;
        result.SuccessfulRecords = 1;
        result.GeneratedFiles.Add(outputFilePath);
        result.Duration = stopwatch.Elapsed;

        _logger.LogInformation($"文档处理完成: {outputFilePath}, 耗时: {stopwatch.Elapsed.TotalSeconds:F2}s");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        result.Errors.Add($"处理文档失败: {ex.Message}");
        _logger.LogError(ex, $"处理文档失败: {templateFilePath}");
        result.Duration = stopwatch.Elapsed;
    }

    return result;
}
```

**Step 3: 提交**

```bash
git add Services/DocumentProcessorService.cs
git commit -m "feat: 实现带格式的文档填充逻辑"
```

---

### Task 8: 更新 ProcessDocumentsAsync 使用格式化数据

**Files:**
- Modify: `Services/DocumentProcessorService.cs`

**Step 1: 修改 ProcessDocumentsAsync 方法**

找到现有的 `ProcessDocumentsAsync` 方法，修改逻辑以检测数据类型并调用相应的方法：

```csharp
public async Task<ProcessResult> ProcessDocumentsAsync(ProcessRequest request)
{
    var result = new ProcessResult { IsSuccess = false };

    try
    {
        _logger.LogInformation($"开始处理文档: {request.TemplateFilePath}");

        // 判断数据文件类型
        var extension = Path.GetExtension(request.DataFilePath).ToLowerInvariant();
        bool useFormattedData = extension == ".xlsx";

        if (useFormattedData)
        {
            // 使用格式化数据处理
            var excelParser = _serviceProvider.GetService<IExcelDataParser>();
            if (excelParser == null)
            {
                result.Errors.Add("Excel 数据解析服务未注册");
                return result;
            }

            var formattedData = await excelParser.ParseExcelFileAsync(request.DataFilePath);

            var outputFilePath = Path.Combine(
                request.OutputDirectory,
                $"{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            );

            return await ProcessDocumentWithFormattedDataAsync(
                request.TemplateFilePath,
                formattedData,
                outputFilePath
            );
        }
        else
        {
            // 使用现有的 JSON 处理逻辑
            // ... 保留原有实现 ...
        }
    }
    catch (Exception ex)
    {
        result.Errors.Add($"处理失败: {ex.Message}");
        _logger.LogError(ex, "处理文档失败");
    }

    return result;
}
```

注意：需要注入 `IServiceProvider` 到 `DocumentProcessorService` 构造函数中。

**Step 2: 添加 IServiceProvider 依赖**

在构造函数中添加参数：

```csharp
private readonly IServiceProvider _serviceProvider;

public DocumentProcessorService(
    ILogger<DocumentProcessorService> logger,
    IFileService fileService,
    IProgressReporter progressReporter,
    IServiceProvider serviceProvider)
    : base(logger, fileService)
{
    _progressReporter = progressReporter;
    _serviceProvider = serviceProvider;
}
```

**Step 3: 提交**

```bash
git add Services/DocumentProcessorService.cs
git commit -m "feat: 更新 ProcessDocumentsAsync 支持格式化数据"
```

---

## 阶段三：转换工具

### Task 9: 创建 ExcelToWordConverterService

**Files:**
- Create: `Services/ExcelToWordConverterService.cs`
- Create: `Services/Interfaces/IExcelToWordConverter.cs`

**Step 1: 创建接口**

创建文件 `Services/Interfaces/IExcelToWordConverter.cs`:

```csharp
using System.Threading.Tasks;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// JSON 到 Excel 转换服务接口
    /// </summary>
    public interface IExcelToWordConverter
    {
        /// <summary>
        /// 将 JSON 文件转换为 Excel 文件
        /// </summary>
        /// <param name="jsonFilePath">JSON 文件路径</param>
        /// <param name="outputExcelPath">输出 Excel 文件路径</param>
        /// <returns>转换是否成功</returns>
        Task<bool> ConvertJsonToExcelAsync(string jsonFilePath, string outputExcelPath);

        /// <summary>
        /// 批量转换 JSON 文件为 Excel
        /// </summary>
        /// <param name="jsonFilePaths">JSON 文件路径列表</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>转换结果</returns>
        Task<BatchConvertResult> ConvertBatchAsync(string[] jsonFilePaths, string outputDirectory);
    }

    /// <summary>
    /// 批量转换结果
    /// </summary>
    public class BatchConvertResult
    {
        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 转换详情
        /// </summary>
        public System.Collections.Generic.List<ConvertDetail> Details { get; set; } = new();
    }

    /// <summary>
    /// 单个转换详情
    /// </summary>
    public class ConvertDetail
    {
        /// <summary>
        /// 源文件路径
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// 输出文件路径
        /// </summary>
        public string OutputFile { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
```

**Step 2: 创建服务实现**

创建文件 `Services/ExcelToWordConverterService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

namespace DocuFiller.Services
{
    /// <summary>
    /// JSON 到 Excel 转换服务实现
    /// </summary>
    public class ExcelToWordConverterService : IExcelToWordConverter
    {
        private readonly ILogger<ExcelToWordConverterService> _logger;
        private readonly IFileService _fileService;

        public ExcelToWordConverterService(
            ILogger<ExcelToWordConverterService> logger,
            IFileService _fileService)
        {
            _logger = logger;
            _fileService = _fileService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<bool> ConvertJsonToExcelAsync(string jsonFilePath, string outputExcelPath)
        {
            try
            {
                _logger.LogInformation($"开始转换 JSON 到 Excel: {jsonFilePath}");

                // 读取 JSON 文件
                var jsonContent = await _fileService.ReadAllTextAsync(jsonFilePath);
                var jsonObject = JObject.Parse(jsonContent);

                // 提取 keywords 数组
                var keywordsArray = jsonObject["keywords"] as JArray;
                if (keywordsArray == null || !keywordsArray.Any())
                {
                    _logger.LogWarning($"JSON 文件中没有 keywords 数组: {jsonFilePath}");
                    return false;
                }

                // 创建 Excel 文件
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // 添加表头（可选，这里不添加表头，直接从第一行开始）
                int row = 1;

                foreach (var keywordItem in keywordsArray)
                {
                    var key = keywordItem["key"]?.ToString();
                    var value = keywordItem["value"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(key))
                    {
                        worksheet.Cells[row, 1].Value = key;
                        worksheet.Cells[row, 2].Value = value;
                        row++;
                    }
                }

                // 保存 Excel 文件
                var fileInfo = new FileInfo(outputExcelPath);
                await package.SaveAsAsync(fileInfo);

                _logger.LogInformation($"转换成功: {outputExcelPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"转换失败: {jsonFilePath}");
                return false;
            }
        }

        public async Task<BatchConvertResult> ConvertBatchAsync(string[] jsonFilePaths, string outputDirectory)
        {
            var result = new BatchConvertResult();

            _logger.LogInformation($"开始批量转换 {jsonFilePaths.Length} 个文件");

            foreach (var jsonPath in jsonFilePaths)
            {
                var detail = new ConvertDetail { SourceFile = jsonPath };

                try
                {
                    // 生成输出文件名
                    var fileName = Path.GetFileNameWithoutExtension(jsonPath) + ".xlsx";
                    var outputPath = Path.Combine(outputDirectory, fileName);

                    // 转换
                    var success = await ConvertJsonToExcelAsync(jsonPath, outputPath);

                    detail.OutputFile = outputPath;
                    detail.Success = success;

                    if (success)
                    {
                        result.SuccessCount++;
                        _logger.LogInformation($"转换成功: {Path.GetFileName(jsonPath)}");
                    }
                    else
                    {
                        result.FailureCount++;
                        detail.ErrorMessage = "转换失败，请检查 JSON 格式";
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    detail.Success = false;
                    detail.ErrorMessage = ex.Message;
                    _logger.LogError(ex, $"转换失败: {jsonPath}");
                }

                result.Details.Add(detail);
            }

            _logger.LogInformation($"批量转换完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}");
            return result;
        }
    }
}
```

**Step 3: 添加 FileService 扩展方法**

在 `IFileService` 接口中添加：

```csharp
Task<string> ReadAllTextAsync(string path);
```

在 `FileService` 实现中添加：

```csharp
public async Task<string> ReadAllTextAsync(string path)
{
    return await File.ReadAllTextAsync(path);
}
```

**Step 4: 提交**

```bash
git add Services/Interfaces/IExcelToWordConverter.cs Services/ExcelToWordConverterService.cs
git add Services/Interfaces/IFileService.cs Services/FileService.cs
git commit -m "feat: 添加 JSON 到 Excel 转换服务"
```

---

### Task 10: 创建转换器 ViewModel

**Files:**
- Create: `ViewModels/ConverterWindowViewModel.cs`

**Step 1: 创建 ViewModel**

创建文件 `ViewModels/ConverterWindowViewModel.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DocuFiller.Services.Interfaces;
using DocuFiller.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocuFiller.ViewModels
{
    public class ConverterWindowViewModel : INotifyPropertyChanged
    {
        private readonly IExcelToWordConverter _converter;
        private readonly ILogger<ConverterWindowViewModel> _logger;

        private string _outputDirectory = string.Empty;
        private string _progressMessage = "就绪";
        private double _progressPercentage = 0;
        private bool _isConverting = false;

        public ObservableCollection<string> SourceFiles { get; } = new();
        public ObservableCollection<ConvertItemViewModel> ConvertItems { get; } = new();

        public ConverterWindowViewModel(
            IExcelToWordConverter converter,
            ILogger<ConverterWindowViewModel> logger)
        {
            _converter = converter;
            _logger = logger;
            InitializeCommands();

            // 设置默认输出目录为源文件目录
            _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        #region Properties

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                if (SetProperty(ref _isConverting, value))
                {
                    OnPropertyChanged(nameof(CanStartConvert));
                }
            }
        }

        public bool CanStartConvert => !IsConverting && ConvertItems.Any(i => i.IsSelected);

        #endregion

        #region Commands

        public ICommand BrowseSourceCommand { get; private set; }
        public ICommand BrowseOutputCommand { get; private set; }
        public ICommand StartConvertCommand { get; private set; }
        public ICommand ClearListCommand { get; private set; }

        private void InitializeCommands()
        {
            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseOutputCommand = new RelayCommand(BrowseOutput);
            StartConvertCommand = new RelayCommand(async () => await StartConvertAsync(), () => CanStartConvert);
            ClearListCommand = new RelayCommand(ClearList);
        }

        #endregion

        #region Methods

        private void BrowseSource()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 JSON 文件",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Multiselect = true,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if (!SourceFiles.Contains(fileName))
                    {
                        SourceFiles.Add(fileName);
                        ConvertItems.Add(new ConvertItemViewModel { SourcePath = fileName });
                    }
                }
                OnPropertyChanged(nameof(CanStartConvert));
            }
        }

        private void BrowseOutput()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择输出目录"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputDirectory = dialog.FolderName;
            }
        }

        private async Task StartConvertAsync()
        {
            IsConverting = true;
            ProgressPercentage = 0;

            try
            {
                var selectedFiles = ConvertItems
                    .Where(i => i.IsSelected)
                    .Select(i => i.SourcePath)
                    .ToArray();

                var result = await _converter.ConvertBatchAsync(selectedFiles, OutputDirectory);

                // 更新转换结果
                for (int i = 0; i < result.Details.Count; i++)
                {
                    var detail = result.Details[i];
                    var item = ConvertItems.FirstOrDefault(x => x.SourcePath == detail.SourceFile);
                    if (item != null)
                    {
                        item.IsSuccess = detail.Success;
                        item.OutputPath = detail.OutputFile;
                        item.ErrorMessage = detail.ErrorMessage;
                    }

                    ProgressPercentage = (double)(i + 1) / result.Details.Count * 100;
                }

                ProgressMessage = $"转换完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量转换失败");
                MessageBox.Show($"转换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsConverting = false;
            }
        }

        private void ClearList()
        {
            SourceFiles.Clear();
            ConvertItems.Clear();
            OnPropertyChanged(nameof(CanStartConvert));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 转换项视图模型
    /// </summary>
    public class ConvertItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private bool _isSuccess;
        private string _outputPath = string.Empty;
        private string _errorMessage = string.Empty;

        public string SourcePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(SourcePath);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
```

**Step 2: 提交**

```bash
git add ViewModels/ConverterWindowViewModel.cs
git commit -m "feat: 添加转换器 ViewModel"
```

---

### Task 11: 创建转换器窗口

**Files:**
- Create: `Views/ConverterWindow.xaml`
- Create: `Views/ConverterWindow.xaml.cs`

**Step 1: 创建 XAML**

创建文件 `Views/ConverterWindow.xaml`:

```xml
<Window x:Class="DocuFiller.Views.ConverterWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="JSON 转 Excel 转换工具" Height="600" Width="800"
        WindowStartupLocation="CenterOwner">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 源文件选择 -->
        <StackPanel Grid.Row="0" Margin="0,0,0,15">
            <TextBlock Text="源文件:" FontWeight="Bold" Margin="0,0,0,5"/>
            <Grid>
                <Button Content="浏览..." Width="80" HorizontalAlignment="Left"
                        Command="{Binding BrowseSourceCommand}"/>
            </Grid>
        </StackPanel>

        <!-- 文件列表 -->
        <GroupBox Grid.Row="1" Header="待转换文件列表" Margin="0,0,0,15">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <DataGrid Grid.Row="0" ItemsSource="{Binding ConvertItems}"
                          AutoGenerateColumns="False" CanUserAddRows="False"
                          SelectionMode="Single" GridLinesVisibility="All">
                    <DataGrid.Columns>
                        <DataGridCheckBoxColumn Header="选择" Binding="{Binding IsSelected}" Width="50"/>
                        <DataGridTextColumn Header="文件名" Binding="{Binding FileName}" Width="*"/>
                        <DataGridTemplateColumn Header="状态" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Text="✓" Foreground="Green" FontWeight="Bold"
                                                   Visibility="{Binding IsSuccess, Converter={StaticResource BoolToVisibilityConverter}}"/>
                                        <TextBlock Text="✗" Foreground="Red" FontWeight="Bold"
                                                   Visibility="{Binding IsSuccess, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
                                    </StackPanel>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="输出路径" Binding="{Binding OutputPath}" Width="*"/>
                    </DataGrid.Columns>
                </DataGrid>

                <Button Grid.Row="1" Content="清空列表" Width="100" HorizontalAlignment="Right"
                        Command="{Binding ClearListCommand}" Margin="0,10,0,0"/>
            </Grid>
        </GroupBox>

        <!-- 输出目录 -->
        <StackPanel Grid.Row="2" Margin="0,0,0,15">
            <TextBlock Text="输出目录:" FontWeight="Bold" Margin="0,0,0,5"/>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBox Grid.Column="0" Text="{Binding OutputDirectory, UpdateSourceTrigger=PropertyChanged}"
                         IsReadOnly="True" Margin="0,0,10,0"/>
                <Button Grid.Column="1" Content="浏览..." Width="80"
                        Command="{Binding BrowseOutputCommand}"/>
            </Grid>
        </StackPanel>

        <!-- 操作按钮和进度 -->
        <StackPanel Grid.Row="3">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Column="0">
                    <TextBlock Text="{Binding ProgressMessage}" Margin="0,0,0,5"/>
                    <ProgressBar Height="20" Value="{Binding ProgressPercentage}" Maximum="100"/>
                </StackPanel>

                <Button Grid.Column="1" Content="开始转换" Width="100" Height="40"
                        Command="{Binding StartConvertCommand}" Margin="10,0,0,0"/>
            </Grid>
        </StackPanel>
    </Grid>
</Window>
```

**Step 2: 创建 Code-behind**

创建文件 `Views/ConverterWindow.xaml.cs`:

```csharp
using System.Windows;
using DocuFiller.ViewModels;

namespace DocuFiller.Views
{
    public partial class ConverterWindow : Window
    {
        public ConverterWindow(ConverterWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
```

**Step 3: 提交**

```bash
git add Views/ConverterWindow.xaml Views/ConverterWindow.xaml.cs
git commit -m "feat: 添加转换器窗口 UI"
```

---

### Task 12: 注册转换服务并添加菜单项

**Files:**
- Modify: `App.xaml.cs`
- Modify: `Views/MainWindow.xaml`

**Step 1: 注册服务**

在 `App.xaml.cs` 的 `ConfigureServices` 方法中添加：

```csharp
services.AddSingleton<IExcelToWordConverter, ExcelToWordConverterService>();
```

**Step 2: 添加菜单项**

在 `MainWindow.xaml` 的菜单区域添加：

```xml
<MenuItem Header="工具">
    <MenuItem Header="转换工具">
        <MenuItem Header="JSON 转 Excel 转换器..."
                  Command="{Binding OpenConverterCommand}"/>
    </MenuItem>
</MenuItem>
```

**Step 3: 在 MainWindowViewModel 中添加命令**

在 `ViewModels/MainWindowViewModel.cs` 中添加：

```csharp
private readonly IServiceProvider _serviceProvider;

// 在构造函数中添加
_serviceProvider = serviceProvider;

// 添加命令属性
public ICommand OpenConverterCommand { get; private set; }

// 在 InitializeCommands 中添加
OpenConverterCommand = new RelayCommand(OpenConverter);

// 添加方法
private void OpenConverter()
{
    try
    {
        var viewModel = _serviceProvider.GetService<ConverterWindowViewModel>();
        if (viewModel != null)
        {
            var window = new Views.ConverterWindow(viewModel);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "打开转换器窗口失败");
        MessageBox.Show($"打开转换器窗口失败: {ex.Message}", "错误",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

**Step 4: 提交**

```bash
git add App.xaml.cs Views/MainWindow.xaml ViewModels/MainWindowViewModel.cs
git commit -m "feat: 注册转换服务并添加菜单项"
```

---

## 阶段四：主界面集成

### Task 13: 更新主窗口文件选择器

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 更新 BrowseData 方法**

修改 `BrowseData` 方法中的文件过滤器：

```csharp
private void BrowseData()
{
    var dialog = new OpenFileDialog
    {
        Title = "选择数据文件",
        Filter = "支持的数据文件 (*.xlsx;*.json)|*.xlsx;*.json|Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
        CheckFileExists = true
    };

    if (dialog.ShowDialog() == true)
    {
        DataPath = dialog.FileName;
    }
}
```

**Step 2: 添加文件类型检测属性**

在 `MainWindowViewModel` 中添加属性：

```csharp
private DataFileType _dataFileType = DataFileType.Json;

public DataFileType DataFileType
{
    get => _dataFileType;
    set => SetProperty(ref _dataFileType, value);
}

public string DataFileTypeDisplay => DataFileType switch
{
    DataFileType.Excel => "Excel (支持格式)",
    DataFileType.Json => "JSON (纯文本)",
    _ => "未知"
};
```

添加枚举：

```csharp
public enum DataFileType
{
    Json,
    Excel
}
```

**Step 3: 更新 DataPath 属性**

修改 `DataPath` 属性的 setter：

```csharp
public string DataPath
{
    get => _dataPath;
    set
    {
        if (SetProperty(ref _dataPath, value))
        {
            // 检测文件类型
            if (!string.IsNullOrEmpty(value))
            {
                var extension = Path.GetExtension(value).ToLowerInvariant();
                DataFileType = extension == ".xlsx" ? DataFileType.Excel : DataFileType.Json;
            }

            UpdateFileInfo();
            OnPropertyChanged(nameof(CanStartProcess));
            OnPropertyChanged(nameof(DataFileTypeDisplay));
        }
    }
}
```

**Step 4: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat: 更新数据文件选择器支持 Excel 和 JSON"
```

---

### Task 14: 更新数据预览显示

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`
- Modify: `Views/MainWindow.xaml`

**Step 1: 更新 PreviewDataAsync 方法**

修改 `PreviewDataAsync` 方法以支持 Excel：

```csharp
private async void PreviewDataAsync()
{
    try
    {
        ProgressMessage = "加载数据预览...";

        if (DataFileType == DataFileType.Excel)
        {
            var excelParser = _serviceProvider.GetService<IExcelDataParser>();
            if (excelParser == null)
            {
                MessageBox.Show("Excel 数据解析服务未注册", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var preview = await excelParser.GetDataPreviewAsync(DataPath, 10);

            PreviewData.Clear();
            foreach (var item in preview)
            {
                PreviewData.Add(item);
            }

            var summary = await excelParser.GetDataStatisticsAsync(DataPath);
            DataStatistics = new DataStatistics
            {
                TotalRecords = summary.ValidKeywordRows,
                Fields = preview.SelectMany(d => d.Keys).Distinct().ToList()
            };

            ProgressMessage = $"Excel 数据加载完成，共 {summary.ValidKeywordRows} 条记录";
        }
        else
        {
            // 保留原有的 JSON 预览逻辑
            var statistics = await _dataParser.GetDataStatisticsAsync(DataPath);
            DataStatistics = statistics;

            var preview = await _dataParser.GetDataPreviewAsync(DataPath, 10);

            PreviewData.Clear();
            foreach (var item in preview)
            {
                PreviewData.Add(item);
            }

            ProgressMessage = $"JSON 数据加载完成，共 {statistics.TotalRecords} 条记录";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "预览数据时发生错误");
        ProgressMessage = "数据加载失败";
        MessageBox.Show($"预览数据时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

**Step 2: 添加 IServiceProvider 依赖**

在构造函数中添加：

```csharp
private readonly IServiceProvider _serviceProvider;

public MainWindowViewModel(
    IDocumentProcessor documentProcessor,
    IDataParser dataParser,
    IFileService fileService,
    IProgressReporter progressReporter,
    IFileScanner fileScanner,
    IDirectoryManager directoryManager,
    ILogger<MainWindowViewModel> logger,
    IServiceProvider serviceProvider)
{
    // ... 保留原有代码 ...
    _serviceProvider = serviceProvider;
}
```

**Step 3: 在 XAML 中添加文件类型显示**

在 `MainWindow.xaml` 的适当位置添加：

```xml
<TextBlock Text="{Binding DataFileTypeDisplay, StringFormat='文件类型: {0}'}"
           Margin="10,5,10,0" FontWeight="Bold"/>
```

**Step 4: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs Views/MainWindow.xaml
git commit -m "feat: 更新数据预览支持 Excel 格式显示"
```

---

### Task 15: 更新 ProcessDocumentsAsync 使用正确的解析器

**Files:**
- Modify: `Services/DocumentProcessorService.cs`

**Step 1: 修改处理逻辑**

确保 `ProcessDocumentsAsync` 方法根据文件类型选择正确的解析器（已在 Task 8 中实现）。

**Step 2: 提交**

```bash
git commit -m "fix: 确保文档处理使用正确的数据解析器"
```

---

## 阶段五：测试和优化

### Task 16: 创建集成测试

**Files:**
- Create: `Tests/ExcelIntegrationTests.cs`

**Step 1: 创建集成测试**

创建文件 `Tests/ExcelIntegrationTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

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
            var logger = LoggerFactory.Create(builder => { }).CreateLogger<ExcelDataParserService>();
            var fileService = new FileService(logger);
            _excelParser = new ExcelDataParserService(logger, fileService);

            var progressLogger = LoggerFactory.Create(builder => { }).CreateLogger<ProgressReporterService>();
            var progressReporter = new ProgressReporterService(progressLogger);
            _documentProcessor = new DocumentProcessorService(
                LoggerFactory.Create(builder => { }).CreateLogger<DocumentProcessorService>(),
                fileService,
                progressReporter,
                null // IServiceProvider for testing
            );
        }

        private void CreateTestTemplate(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // 添加带内容控件的段落
            var paragraph = new Paragraph();

            // 创建内容控件
            var sdtBlock = new SdtBlock();
            var sdtProperties = new SdtProperties(
                new Alias { Val = "产品名称" },
                new Tag { Val = "#产品名称#" }
            );

            var sdtContent = new SdtContentBlock(
                new Paragraph(new ParagraphProperties(new Justification() { Val = JustificationValues.Both }),
                    new Run(new Text("默认值")))
            );

            sdtBlock.Append(sdtProperties, sdtContent);
            paragraph.Append(sdtBlock);
            body.Append(paragraph);

            mainPart.Document.Append(body);
            document.Save();
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

            package.SaveAs(new FileInfo(path));
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
                Directory.Delete(testDir, true);
            }
        }
    }
}
```

**Step 2: 运行集成测试**

运行: `dotnet test Tests/DocuFiller.Tests.csproj --filter "FullyQualifiedName~ExcelIntegrationTests"`
预期输出: 测试通过

**Step 3: 提交**

```bash
git add Tests/ExcelIntegrationTests.cs
git commit -m "test: 添加 Excel 集成测试"
```

---

### Task 17: 性能优化和错误处理

**Files:**
- Modify: `Services/ExcelDataParserService.cs`
- Modify: `Services/DocumentProcessorService.cs`

**Step 1: 添加大文件处理优化**

在 `ExcelDataParserService` 中添加：

```csharp
// 添加最大行数限制常量
private const int MaxRows = 10000;
```

在 `ParseExcelFileAsync` 中添加检查：

```csharp
if (rowCount > MaxRows)
{
    _logger.LogWarning($"Excel 文件行数 ({rowCount}) 超过最大限制 ({MaxRows})");
    throw new InvalidOperationException($"Excel 文件过大，最多支持 {MaxRows} 行数据");
}
```

**Step 2: 改进错误消息**

在验证方法中添加更具体的错误消息：

```csharp
if (result.Summary.InvalidFormatKeywords.Count > 0)
{
    var examples = result.Summary.InvalidFormatKeywords.Take(3);
    result.AddError($"存在 {result.Summary.InvalidFormatKeywords.Count} 个格式不正确的关键词。示例: {string.Join("; ", examples)}");
}
```

**Step 3: 添加取消令牌支持**

在异步方法签名中添加 `CancellationToken`：

```csharp
public async Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(
    string filePath,
    CancellationToken cancellationToken = default)
{
    // 在循环中检查取消
    for (int row = 1; row <= rowCount; row++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // ...
    }
}
```

**Step 4: 提交**

```bash
git add Services/ExcelDataParserService.cs Services/DocumentProcessorService.cs
git commit -m "perf: 添加性能优化和错误处理改进"
```

---

### Task 18: 创建测试数据文件

**Files:**
- Create: `test_data/formatted_data.xlsx`

**Step 1: 创建测试 Excel 文件**

使用 EPPlus 创建测试文件（可在单独的控制台程序中运行）：

```csharp
var filePath = "test_data/formatted_data.xlsx";
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var package = new ExcelPackage();
var worksheet = package.Workbook.Worksheets.Add("Sheet1");

// 添加普通数据
worksheet.Cells[1, 1].Value = "#产品名称#";
worksheet.Cells[1, 2].Value = "D-二聚体测定试剂盒（胶乳免疫比浊法）";

worksheet.Cells[2, 1].Value = "#结构及组成#";
worksheet.Cells[2, 2].Value = "adsasdadsa";

worksheet.Cells[3, 1].Value = "#产品型号#";
worksheet.Cells[3, 2].Value = "产1品2型3号4";

// 添加带上标的数据
worksheet.Cells[4, 1].Value = "#规格#";
var cell = worksheet.Cells[4, 2];
cell.Value = "2x10";
cell.RichText.Add("9").VerticalAlign = OfficeOpenXml.Style.ExcelVerticalAlignmentFont.Superscript;

// 添加带下标的数据
worksheet.Cells[5, 1].Value = "#化学式#";
var chemCell = worksheet.Cells[5, 2];
chemCell.Value = "H";
chemCell.RichText.Add("2").VerticalAlign = OfficeOpenXml.Style.ExcelVerticalAlignmentFont.Subscript;
chemCell.RichText.Add("O");

package.SaveAs(new FileInfo(filePath));
```

**Step 2: 提交**

```bash
git add test_data/formatted_data.xlsx
git commit -m "test: 添加格式化测试数据文件"
```

---

### Task 19: 更新用户文档

**Files:**
- Create: `docs/excel-data-user-guide.md`

**Step 1: 创建用户指南**

创建文件 `docs/excel-data-user-guide.md`:

```markdown
# Excel 数据支持用户指南

## 概述

DocuFiller 现在支持使用 Excel (.xlsx) 文件作为数据源，可以保留上标、下标等格式信息。

## Excel 文件格式

### 基本结构

Excel 文件应包含两列：
- **A 列（关键词）**: 符合 `#关键词#` 格式的标识符
- **B 列（值）**: 对应的值，支持富文本格式

示例：

| A 列         | B 列              |
|--------------|-------------------|
| #产品名称#    | D-二聚体          |
| #规格#        | 2x10⁹             |

### 创建格式化内容

1. 在 Excel 中输入值
2. 选中要格式化的部分（如 "9"）
3. 右键 → 设置单元格格式 → 字体 → 上标/下标

## 使用流程

### 1. 准备 Excel 数据文件

按照上述格式创建 Excel 文件，输入关键词和对应的值。

### 2. 选择 Word 模板

在 DocuFiller 中选择包含内容控件的 Word 模板。

### 3. 选择 Excel 数据文件

点击"浏览"按钮，选择准备好的 Excel 文件。

### 4. 预览数据

系统会自动验证并显示数据预览。如有错误，会显示详细信息。

### 5. 开始处理

点击"开始处理"按钮，生成带格式的 Word 文档。

## JSON 转 Excel

如果您有旧的 JSON 数据文件，可以使用内置的转换工具：

1. 点击菜单：**工具 → 转换工具 → JSON 转 Excel 转换器**
2. 选择要转换的 JSON 文件
3. 选择输出目录
4. 点击"开始转换"

转换后，您可以在 Excel 中添加需要的格式。

## 验证规则

系统会严格验证 Excel 文件：

- ✅ 文件扩展名必须是 `.xlsx`
- ✅ 关键词必须以 `#` 开头和结尾
- ✅ 关键词不能重复
- ⚠️ 值列为空会产生警告

## 常见问题

### Q: 转换后的文档没有显示格式？

A: 确保在 Excel 中正确设置了格式（上标/下标），然后在 Word 中检查内容控件的样式设置。

### Q: 可以同时使用 JSON 和 Excel 吗？

A: 可以，程序同时支持两种格式。但建议逐步迁移到 Excel 以获得更好的格式支持。

### Q: 转换工具可以批量处理吗？

A: 是的，转换工具支持多选文件批量转换。
```

**Step 2: 提交**

```bash
git add docs/excel-data-user-guide.md
git commit -m "docs: 添加 Excel 数据支持用户指南"
```

---

### Task 20: 最终测试和验证

**Step 1: 完整功能测试清单**

- [ ] Excel 文件解析正确
- [ ] Excel 文件验证（格式、重复、空值）
- [ ] 数据预览显示正确
- [ ] Word 文档填充保留上标格式
- [ ] Word 文档填充保留下标格式
- [ ] JSON 文件仍然正常工作
- [ ] JSON 转 Excel 转换工具正常
- [ ] 批量转换功能正常
- [ ] 错误提示清晰明确
- [ ] 大文件处理性能可接受

**Step 2: 手动测试流程**

1. 使用 `test_data/formatted_data.xlsx` 进行完整测试
2. 创建新的 Excel 文件，手动添加格式
3. 使用转换工具转换 `test_data/1.json`
4. 验证生成的 Word 文档格式正确

**Step 3: 性能测试**

- 测试 100 行数据的处理时间
- 测试 1000 行数据的处理时间
- 确保内存使用合理

**Step 4: 创建测试报告**

创建文件 `docs/test-report-excel-support.md`，记录测试结果。

**Step 5: 最终提交**

```bash
git add docs/test-report-excel-support.md
git commit -m "test: 完成 Excel 数据支持的最终测试验证"
```

---

## 完成

实施完成后，DocuFiller 将支持：

1. ✅ Excel 文件作为数据输入源
2. ✅ 保留上标、下标格式
3. ✅ JSON 转 Excel 转换工具
4. ✅ 严格的 Excel 文件验证
5. ✅ 向后兼容 JSON 支持

用户可以在 Excel 中创建格式化的数据，然后填充到 Word 模板中，格式信息会被正确保留。
