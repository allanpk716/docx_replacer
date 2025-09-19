using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocuFiller.Utils
{
    /// <summary>
    /// 验证帮助类
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 验证文件路径是否有效
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="checkExists">是否检查文件存在</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateFilePath(string filePath, bool checkExists = true)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("文件路径不能为空");
                return result;
            }
            
            try
            {
                // 检查路径格式是否有效
                var fullPath = Path.GetFullPath(filePath);
                
                // 检查路径中是否包含无效字符
                var invalidChars = Path.GetInvalidPathChars();
                if (filePath.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError("文件路径包含无效字符");
                    return result;
                }
                
                // 检查文件名是否包含无效字符
                var fileName = Path.GetFileName(filePath);
                var invalidFileNameChars = Path.GetInvalidFileNameChars();
                if (fileName.Any(c => invalidFileNameChars.Contains(c)))
                {
                    result.AddError("文件名包含无效字符");
                    return result;
                }
                
                // 检查文件是否存在
                if (checkExists && !File.Exists(filePath))
                {
                    result.AddError("文件不存在");
                    return result;
                }
                
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.AddError($"文件路径验证失败: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证目录路径是否有效
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="checkExists">是否检查目录存在</param>
        /// <param name="createIfNotExists">如果不存在是否创建</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateDirectoryPath(string directoryPath, bool checkExists = true, bool createIfNotExists = false)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                result.AddError("目录路径不能为空");
                return result;
            }
            
            try
            {
                // 检查路径格式是否有效
                var fullPath = Path.GetFullPath(directoryPath);
                
                // 检查路径中是否包含无效字符
                var invalidChars = Path.GetInvalidPathChars();
                if (directoryPath.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError("目录路径包含无效字符");
                    return result;
                }
                
                // 检查目录是否存在
                if (checkExists)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        if (createIfNotExists)
                        {
                            Directory.CreateDirectory(directoryPath);
                            result.AddWarning($"已创建目录: {directoryPath}");
                        }
                        else
                        {
                            result.AddError("目录不存在");
                            return result;
                        }
                    }
                }
                
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.AddError($"目录路径验证失败: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证文件扩展名
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="allowedExtensions">允许的扩展名列表</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateFileExtension(string filePath, params string[] allowedExtensions)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("文件路径不能为空");
                return result;
            }
            
            if (allowedExtensions == null || allowedExtensions.Length == 0)
            {
                result.IsValid = true;
                return result;
            }
            
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var normalizedExtensions = allowedExtensions.Select(ext => 
                    ext.StartsWith(".") ? ext.ToLowerInvariant() : $".{ext.ToLowerInvariant()}").ToArray();
                
                if (!normalizedExtensions.Contains(extension))
                {
                    result.AddError($"不支持的文件类型。支持的类型: {string.Join(", ", normalizedExtensions)}");
                    return result;
                }
                
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.AddError($"文件扩展名验证失败: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证JSON格式
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateJsonFormat(string jsonContent)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.AddError("JSON内容不能为空");
                return result;
            }
            
            try
            {
                Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);
                result.IsValid = true;
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                result.AddError($"JSON格式错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.AddError($"JSON验证失败: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证字符串是否为有效的文件名
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateFileName(string fileName)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddError("文件名不能为空");
                return result;
            }
            
            try
            {
                // 检查文件名长度
                if (fileName.Length > 255)
                {
                    result.AddError("文件名过长（超过255个字符）");
                    return result;
                }
                
                // 检查是否包含无效字符
                var invalidChars = Path.GetInvalidFileNameChars();
                if (fileName.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError($"文件名包含无效字符: {string.Join(", ", invalidChars.Where(c => fileName.Contains(c)))}");
                    return result;
                }
                
                // 检查是否为保留名称
                var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
                if (reservedNames.Contains(nameWithoutExtension))
                {
                    result.AddError($"文件名不能使用系统保留名称: {nameWithoutExtension}");
                    return result;
                }
                
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.AddError($"文件名验证失败: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证命名模式
        /// </summary>
        /// <param name="pattern">命名模式</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateNamingPattern(string pattern)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(pattern))
            {
                result.AddError("命名模式不能为空");
                return result;
            }
            
            try
            {
                // 检查是否包含有效的占位符
                var validPatterns = new[] { "{index}", "{timestamp}", "{guid}" };
                var hasValidPattern = validPatterns.Any(p => pattern.Contains(p)) || 
                                    Regex.IsMatch(pattern, @"\{field:[a-zA-Z_][a-zA-Z0-9_]*\}") ||
                                    Regex.IsMatch(pattern, @"\{index:\d+\}");
                
                if (!hasValidPattern)
                {
                    result.AddWarning("命名模式中没有找到有效的占位符，将使用固定文件名");
                }
                
                // 检查模式中是否包含无效字符
                var testFileName = pattern.Replace("{index}", "1")
                                         .Replace("{timestamp}", DateTime.Now.ToString("yyyyMMddHHmmss"))
                                         .Replace("{guid}", Guid.NewGuid().ToString("N")[..8]);
                
                // 替换字段占位符
                testFileName = Regex.Replace(testFileName, @"\{field:[a-zA-Z_][a-zA-Z0-9_]*\}", "test");
                testFileName = Regex.Replace(testFileName, @"\{index:\d+\}", "001");
                
                var fileNameValidation = ValidateFileName(testFileName + ".docx");
                if (!fileNameValidation.IsValid)
                {
                    result.AddError($"命名模式生成的文件名无效: {string.Join(", ", fileNameValidation.Errors)}");
                    return result;
                }
                
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.AddError($"命名模式验证失败: {ex.Message}");
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// 验证结果类
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = false;
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }
        
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }
        
        public string GetErrorMessage()
        {
            return string.Join("\n", Errors);
        }
        
        public string GetWarningMessage()
        {
            return string.Join("\n", Warnings);
        }
        
        public string GetAllMessages()
        {
            var messages = new List<string>();
            if (Errors.Count > 0)
            {
                messages.Add($"错误:\n{GetErrorMessage()}");
            }
            if (Warnings.Count > 0)
            {
                messages.Add($"警告:\n{GetWarningMessage()}");
            }
            return string.Join("\n\n", messages);
        }
    }
}