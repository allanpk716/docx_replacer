using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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

        // 最大行数限制
        private const int MaxRows = 10000;

        public ExcelDataParserService(ILogger<ExcelDataParserService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
            // 设置 EPPlus 许可上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
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

                    using var package = new ExcelPackage(new System.IO.FileInfo(filePath));
                    var worksheet = package.Workbook.Worksheets[0];

                    if (worksheet == null)
                    {
                        _logger.LogError("Excel 文件没有工作表");
                        return result;
                    }

                    // 从第一行开始读取（无表头）
                    var rowCount = worksheet.Dimension.Rows;

                    // 检查行数限制
                    if (rowCount > MaxRows)
                    {
                        _logger.LogWarning($"Excel 文件行数 ({rowCount}) 超过最大限制 ({MaxRows})");
                        throw new InvalidOperationException($"Excel 文件过大，最多支持 {MaxRows} 行数据");
                    }

                    for (int row = 1; row <= rowCount; row++)
                    {
                        // 检查取消请求
                        cancellationToken.ThrowIfCancellationRequested();

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
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"Excel 文件解析已取消: {filePath}");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"解析 Excel 文件失败: {filePath}");
                    throw;
                }
            }, cancellationToken);
        }

        public Task<ExcelValidationResult> ValidateExcelFileAsync(string filePath)
        {
            return Task.Run(() =>
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

                    using var package = new ExcelPackage(new System.IO.FileInfo(filePath));
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
                        var examples = result.Summary.InvalidFormatKeywords.Take(3);
                        result.AddError($"存在 {result.Summary.InvalidFormatKeywords.Count} 个格式不正确的关键词。示例: {string.Join("; ", examples)}");
                    }

                    if (result.Summary.DuplicateKeywords.Count > 0)
                    {
                        var examples = result.Summary.DuplicateKeywords.Take(Math.Min(5, result.Summary.DuplicateKeywords.Count));
                        result.AddError($"存在 {result.Summary.DuplicateKeywords.Count} 个重复关键词。示例: {string.Join(", ", examples)}");
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
            });
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
                        Text = NormalizeLineEndings(rt.Text),
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
                    Text = NormalizeLineEndings(cell.Text ?? ""),
                    IsSuperscript = cell.Style.Font.VerticalAlign == ExcelVerticalAlignmentFont.Superscript,
                    IsSubscript = cell.Style.Font.VerticalAlign == ExcelVerticalAlignmentFont.Subscript
                };
                formattedValue.Fragments.Add(fragment);
            }

            return formattedValue;
        }

        private static string NormalizeLineEndings(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
