using DocuFiller.Models;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Services
{
    /// <summary>
    /// 关键词验证服务实现
    /// </summary>
    public class KeywordValidationService : IKeywordValidationService
    {
        private readonly ILogger<KeywordValidationService> _logger;
        private static readonly Regex KeyNamePattern = new Regex(@"^#[^#]+#$", RegexOptions.Compiled);
        private static readonly Regex FileNamePattern = new Regex(@"^.+\.(docx?|txt|pdf)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string[] ReservedKeywords = { "#DATE#", "#TIME#", "#NOW#", "#USER#", "#SYSTEM#" };

        public KeywordValidationService(ILogger<KeywordValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValidationResult ValidateKeyword(JsonKeywordItem keyword)
        {
            _logger.LogInformation($"[调试] 开始验证关键词: {keyword?.Key}");

            var result = new ValidationResult { IsValid = true };

            if (keyword == null)
            {
                result.AddError("关键词对象不能为空");
                return result;
            }

            // 验证键名
            var keyValidation = ValidateKeyName(keyword.Key);
            result.Errors.AddRange(keyValidation.Errors);
            result.Warnings.AddRange(keyValidation.Warnings);

            // 验证值
            var valueValidation = ValidateValue(keyword.Value);
            result.Errors.AddRange(valueValidation.Errors);
            result.Warnings.AddRange(valueValidation.Warnings);

            // 验证来源文件
            var sourceFileValidation = ValidateSourceFileName(keyword.SourceFile);
            result.Errors.AddRange(sourceFileValidation.Errors);
            result.Warnings.AddRange(sourceFileValidation.Warnings);

            result.IsValid = result.Errors.Count == 0;
            
            _logger.LogInformation($"[调试] 关键词验证完成: {keyword.Key}, 结果: {(result.IsValid ? "通过" : "失败")}, 错误数: {result.Errors.Count}");
            return result;
        }

        public ValidationResult ValidateKeywordList(IEnumerable<JsonKeywordItem> keywords)
        {
            _logger.LogInformation("[调试] 开始验证关键词列表");

            var result = new ValidationResult { IsValid = true };
            var keywordList = keywords?.ToList() ?? new List<JsonKeywordItem>();

            if (keywordList.Count == 0)
            {
                result.AddWarning("关键词列表为空");
                return result;
            }

            // 验证每个关键词
            for (int i = 0; i < keywordList.Count; i++)
            {
                var keyword = keywordList[i];
                var keywordResult = ValidateKeyword(keyword);
                
                foreach (var error in keywordResult.Errors)
                {
                    result.AddError($"第{i + 1}个关键词: {error}");
                }
                
                foreach (var warning in keywordResult.Warnings)
                {
                    result.AddWarning($"第{i + 1}个关键词: {warning}");
                }
            }

            // 检查重复键名
            var duplicateValidation = CheckDuplicateKeys(keywordList);
            result.Errors.AddRange(duplicateValidation.Errors);
            result.Warnings.AddRange(duplicateValidation.Warnings);

            result.IsValid = result.Errors.Count == 0;
            
            _logger.LogInformation($"[调试] 关键词列表验证完成，总数: {keywordList.Count}, 结果: {(result.IsValid ? "通过" : "失败")}, 错误数: {result.Errors.Count}");
            return result;
        }

        public ValidationResult CheckDuplicateKeys(IEnumerable<JsonKeywordItem> keywords)
        {
            var result = new ValidationResult { IsValid = true };
            var keywordList = keywords?.ToList() ?? new List<JsonKeywordItem>();

            var keyGroups = keywordList
                .Where(k => !string.IsNullOrWhiteSpace(k.Key))
                .GroupBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in keyGroups)
            {
                result.AddError($"发现重复的键名: {group.Key} (出现{group.Count()}次)");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public ValidationResult ValidateKeyName(string keyName)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(keyName))
            {
                result.AddError("键名不能为空");
                return result;
            }

            // 检查格式
            if (!KeyNamePattern.IsMatch(keyName))
            {
                result.AddError("键名必须以#开头和结尾，格式如: #关键词#");
            }

            // 检查长度
            if (keyName.Length < 3)
            {
                result.AddError("键名长度不能少于3个字符");
            }
            else if (keyName.Length > 50)
            {
                result.AddError("键名长度不能超过50个字符");
            }

            // 检查保留关键词
            if (ReservedKeywords.Contains(keyName.ToUpper()))
            {
                result.AddWarning($"'{keyName}' 是系统保留关键词，可能会与系统功能冲突");
            }

            // 检查特殊字符
            var innerContent = keyName.Trim('#');
            if (string.IsNullOrWhiteSpace(innerContent))
            {
                result.AddError("键名内容不能为空");
            }
            else if (innerContent.Contains("#"))
            {
                result.AddError("键名内容中不能包含#字符");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public ValidationResult ValidateValue(string value)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(value))
            {
                result.AddWarning("值为空，替换时将使用空字符串");
                return result;
            }

            // 检查长度
            if (value.Length > 1000)
            {
                result.AddWarning("值的长度超过1000个字符，可能影响文档处理性能");
            }

            // 检查特殊字符
            if (value.Contains("\0"))
            {
                result.AddError("值中不能包含空字符(\\0)");
            }

            // 检查控制字符
            var controlChars = value.Where(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t').ToList();
            if (controlChars.Any())
            {
                result.AddWarning($"值中包含控制字符，可能影响显示效果");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public ValidationResult ValidateSourceFileName(string fileName)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddWarning("来源文件名为空");
                return result;
            }

            // 检查文件名格式
            if (!FileNamePattern.IsMatch(fileName))
            {
                result.AddError("文件名格式无效，支持的格式: .docx, .doc, .txt, .pdf");
            }

            // 检查文件名长度
            if (fileName.Length > 255)
            {
                result.AddError("文件名长度不能超过255个字符");
            }

            // 检查非法字符
            var invalidChars = Path.GetInvalidFileNameChars();
            var hasInvalidChars = fileName.Any(c => invalidChars.Contains(c));
            if (hasInvalidChars)
            {
                result.AddError("文件名包含非法字符");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public List<string> GetKeySuggestions(string partialKey)
        {
            var suggestions = new List<string>();

            if (string.IsNullOrWhiteSpace(partialKey))
            {
                // 提供常用关键词建议
                suggestions.AddRange(new[]
                {
                    "#姓名#", "#日期#", "#时间#", "#地址#", "#电话#",
                    "#邮箱#", "#公司#", "#部门#", "#职位#", "#金额#"
                });
                return suggestions;
            }

            var input = partialKey.ToLower();
            
            // 如果输入不以#开头，自动添加
            if (!input.StartsWith("#"))
            {
                input = "#" + input;
            }

            // 基于输入提供建议
            var commonKeywords = new Dictionary<string, string[]>
            {
                { "姓", new[] { "#姓名#", "#姓氏#" } },
                { "名", new[] { "#姓名#", "#用户名#" } },
                { "日", new[] { "#日期#", "#生日#", "#入职日期#" } },
                { "时", new[] { "#时间#", "#时刻#" } },
                { "地", new[] { "#地址#", "#地点#" } },
                { "电", new[] { "#电话#", "#电子邮箱#" } },
                { "公", new[] { "#公司#", "#公司名称#" } },
                { "部", new[] { "#部门#" } },
                { "职", new[] { "#职位#", "#职务#" } },
                { "金", new[] { "#金额#", "#价格#" } }
            };

            foreach (var kvp in commonKeywords)
            {
                if (input.Contains(kvp.Key))
                {
                    suggestions.AddRange(kvp.Value);
                }
            }

            // 如果没有匹配的建议，提供格式化建议
            if (suggestions.Count == 0 && input.Length > 1)
            {
                var formatted = FormatKeyName(input);
                if (!string.IsNullOrEmpty(formatted))
                {
                    suggestions.Add(formatted);
                }
            }

            return suggestions.Distinct().Take(10).ToList();
        }

        public string FormatKeyName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var trimmed = input.Trim();
            
            // 移除多余的#
            trimmed = trimmed.Trim('#');
            
            if (string.IsNullOrWhiteSpace(trimmed))
                return string.Empty;

            // 添加标准格式的#
            return $"#{trimmed}#";
        }

        public ValidationResult CheckValueIssues(string value)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(value))
            {
                result.AddError("值为空");
                result.IsValid = false;
                return result;
            }

            // 检查常见问题
            if (value.StartsWith(" ") || value.EndsWith(" "))
            {
                result.AddWarning("值的开头或结尾包含空格");
            }

            if (value.Contains("  "))
            {
                result.AddWarning("值中包含连续空格");
            }

            if (value.Contains("\t"))
            {
                result.AddWarning("值中包含制表符");
            }

            if (value.Contains("\r\n") || value.Contains("\n"))
            {
                result.AddWarning("值中包含换行符");
            }

            // 检查可能的编码问题
            if (value.Any(c => c > 127 && c < 160))
            {
                result.AddWarning("值中可能包含特殊编码字符");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public ValidationResult ValidateKeyFormat(string keyFormat)
        {
            return ValidateKeyName(keyFormat);
        }

        public ValidationResult ValidateKeyValue(string keyValue)
        {
            return ValidateValue(keyValue);
        }

        public ValidationResult ValidateSourceFile(string sourceFile)
        {
            return ValidateSourceFileName(sourceFile);
        }

        public List<string> GetKeywordSuggestions(string input, List<JsonKeywordItem> existingKeywords)
        {
            var suggestions = GetKeySuggestions(input);
            
            // 过滤掉已存在的关键词
            if (existingKeywords != null && existingKeywords.Any())
            {
                var existingKeys = existingKeywords.Select(k => k.Key?.ToLower()).Where(k => !string.IsNullOrEmpty(k)).ToHashSet();
                suggestions = suggestions.Where(s => !existingKeys.Contains(s.ToLower())).ToList();
            }
            
            return suggestions;
        }

        public bool IsKeywordDuplicate(string key, List<JsonKeywordItem> keywords, int excludeIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(key) || keywords == null || keywords.Count == 0)
                return false;

            for (int i = 0; i < keywords.Count; i++)
            {
                if (i == excludeIndex)
                    continue;

                if (string.Equals(keywords[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public ValidationResult ValidateKeywordFormat(JsonKeywordItem keyword)
        {
            var result = ValidateKeyword(keyword);
            
            // 添加格式特定的验证
            if (keyword != null)
            {
                // 检查键值对的一致性
                if (!string.IsNullOrEmpty(keyword.Key) && !string.IsNullOrEmpty(keyword.Value))
                {
                    if (keyword.Key.Length > keyword.Value.Length * 2)
                    {
                        result.AddWarning("键名相对于值来说过长，可能影响可读性");
                    }
                }

                // 检查来源文件与键名的关联性
                if (!string.IsNullOrEmpty(keyword.SourceFile) && !string.IsNullOrEmpty(keyword.Key))
                {
                    var keyContent = keyword.Key.Trim('#').ToLower();
                    var fileName = Path.GetFileNameWithoutExtension(keyword.SourceFile).ToLower();
                    
                    if (keyContent.Contains(fileName) || fileName.Contains(keyContent))
                    {
                        result.AddWarning("键名与文件名高度相关，请确认是否正确");
                    }
                }
            }

            return result;
        }
    }
}