using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;

namespace DocuFiller.Services
{
    /// <summary>
    /// 数据解析服务实现
    /// </summary>
    public class DataParserService : IDataParser
    {
        private readonly ILogger<DataParserService> _logger;
        private readonly IFileService _fileService;

        public DataParserService(ILogger<DataParserService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        public Task<List<Dictionary<string, object>>> ParseJsonFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"开始解析JSON文件: {filePath}");

                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogError($"JSON文件不存在: {filePath}");
                    return Task.FromResult(new List<Dictionary<string, object>>());
                }

                var jsonContent = _fileService.ReadAllText(filePath);
                return Task.FromResult(ParseJsonString(jsonContent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"解析JSON文件失败: {filePath}");
                return Task.FromResult(new List<Dictionary<string, object>>());
            }
        }

        public List<Dictionary<string, object>> ParseJsonString(string jsonContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("JSON内容为空");
                    return new List<Dictionary<string, object>>();
                }

                var jsonToken = JToken.Parse(jsonContent);
                var result = new List<Dictionary<string, object>>();

                if (jsonToken is JArray jsonArray)
                {
                    // JSON数组格式
                    foreach (var item in jsonArray)
                    {
                        if (item is JObject jobject)
                        {
                            result.Add(ConvertJObjectToDictionary(jobject));
                        }
                    }
                }
                else if (jsonToken is JObject singleObject)
                {
                    // 单个JSON对象格式
                    result.Add(ConvertJObjectToDictionary(singleObject));
                }

                _logger.LogInformation($"成功解析JSON数据，共 {result.Count} 条记录");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON格式错误");
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析JSON字符串失败");
                return new List<Dictionary<string, object>>();
            }
        }

        public Task<ValidationResult> ValidateJsonFileAsync(string filePath)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                if (!_fileService.FileExists(filePath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "JSON文件不存在";
                    return Task.FromResult(result);
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                if (extension != ".json")
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"不支持的文件格式: {extension}，仅支持 .json 格式";
                    return Task.FromResult(result);
                }

                var jsonContent = _fileService.ReadAllText(filePath);
                return Task.FromResult(ValidateJsonString(jsonContent));
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"验证JSON文件时发生异常: {ex.Message}";
                _logger.LogError(ex, $"验证JSON文件失败: {filePath}");
            }

            return Task.FromResult(result);
        }

        public ValidationResult ValidateJsonString(string jsonContent)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "JSON内容为空";
                    return result;
                }

                var jsonToken = JToken.Parse(jsonContent);
                
                if (jsonToken is JArray jsonArray)
                {
                    if (jsonArray.Count == 0)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "JSON数组为空";
                        return result;
                    }

                    // 验证数组中的每个元素都是对象
                    for (int i = 0; i < jsonArray.Count; i++)
                    {
                        if (!(jsonArray[i] is JObject))
                        {
                            result.IsValid = false;
                            result.ErrorMessage = $"JSON数组第 {i + 1} 个元素不是有效的对象";
                            return result;
                        }
                    }
                }
                else if (!(jsonToken is JObject))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "JSON格式必须是对象或对象数组";
                    return result;
                }
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"JSON格式错误: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"验证JSON时发生异常: {ex.Message}";
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetDataPreviewAsync(string filePath, int maxRecords = 10)
        {
            try
            {
                var allData = await ParseJsonFileAsync(filePath);
                return allData.Take(maxRecords).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取数据预览失败: {filePath}");
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<DataStatistics> GetDataStatisticsAsync(string filePath)
        {
            var statistics = new DataStatistics();

            try
            {
                statistics.FileSizeBytes = _fileService.GetFileSize(filePath);

                var allData = await ParseJsonFileAsync(filePath);
                statistics.TotalRecords = allData.Count;

                if (allData.Any())
                {
                    // 收集所有字段
                    var allFields = new HashSet<string>();
                    foreach (var record in allData)
                    {
                        foreach (var key in record.Keys)
                        {
                            allFields.Add(key);
                        }
                    }
                    statistics.Fields = allFields.ToList();

                    // 分析字段类型和空值
                    foreach (var field in statistics.Fields)
                    {
                        var fieldValues = allData.Select(r => r.ContainsKey(field) ? r[field] : null).ToList();
                        var nullCount = fieldValues.Count(v => v == null || string.IsNullOrWhiteSpace(v?.ToString()));
                        statistics.NullCounts[field] = nullCount;

                        // 推断字段类型
                        var nonNullValues = fieldValues.Where(v => v != null && !string.IsNullOrWhiteSpace(v.ToString())).ToList();
                        if (nonNullValues.Any())
                        {
                        var firstValue = nonNullValues.First()!;
                            statistics.FieldTypes[field] = GetValueType(firstValue);
                        }
                        else
                        {
                            statistics.FieldTypes[field] = "Unknown";
                        }
                    }
                }

                _logger.LogInformation($"生成数据统计信息: {statistics.TotalRecords} 条记录，{statistics.Fields.Count} 个字段");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取数据统计信息失败: {filePath}");
            }

            return statistics;
        }

        private Dictionary<string, object> ConvertJObjectToDictionary(JObject jobject)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var property in jobject.Properties())
            {
                // 特殊处理 keywords 数组
                if (property.Name.Equals("keywords", StringComparison.OrdinalIgnoreCase) && property.Value is JArray keywordsArray)
                {
                    _logger.LogDebug($"处理 keywords 数组，包含 {keywordsArray.Count} 个元素");
                    
                    foreach (var keywordItem in keywordsArray)
                    {
                        if (keywordItem is JObject keywordObj)
                        {
                            var key = keywordObj["key"]?.ToString();
                            var value = keywordObj["value"]?.ToString();
                            
                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                dictionary[key] = value ?? string.Empty;
                                _logger.LogDebug($"添加键值对: {key} = {value}");
                            }
                        }
                    }
                }
                else
                {
                    // 处理其他普通属性
                    var value = ConvertJTokenToObject(property.Value);
                    dictionary[property.Name] = value;
                    _logger.LogDebug($"添加普通属性: {property.Name} = {value}");
                }
            }

            _logger.LogInformation($"解析完成，字典包含 {dictionary.Count} 个键值对");
            foreach (var kvp in dictionary)
            {
                _logger.LogDebug($"最终字典内容: [{kvp.Key}] = [{kvp.Value}]");
            }

            return dictionary;
        }

        private object ConvertJTokenToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>() ?? string.Empty;
                case JTokenType.Integer:
                    return token.Value<long>();
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Date:
                    return token.Value<DateTime>();
                case JTokenType.Null:
                    return string.Empty;
                default:
                    return token.ToString();
            }
        }

        private string GetValueType(object value)
        {
            if (value == null) return "Null";
            
            var type = value.GetType();
            if (type == typeof(string)) return "String";
            if (type == typeof(int) || type == typeof(long)) return "Integer";
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return "Number";
            if (type == typeof(bool)) return "Boolean";
            if (type == typeof(DateTime)) return "Date";
            
            return "Object";
        }
    }
}