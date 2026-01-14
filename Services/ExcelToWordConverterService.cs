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
            IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
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
                var fileInfo = new System.IO.FileInfo(outputExcelPath);
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
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(jsonPath) + ".xlsx";
                    var outputPath = System.IO.Path.Combine(outputDirectory, fileName);

                    // 转换
                    var success = await ConvertJsonToExcelAsync(jsonPath, outputPath);

                    detail.OutputFile = outputPath;
                    detail.Success = success;

                    if (success)
                    {
                        result.SuccessCount++;
                        _logger.LogInformation($"转换成功: {System.IO.Path.GetFileName(jsonPath)}");
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
