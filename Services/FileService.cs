using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocuFiller.Services.Interfaces;
using DocuFiller.Models;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public bool EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建目录失败: {DirectoryPath}", directoryPath);
                return false;
            }
        }

        public bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public long GetFileSize(string filePath)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            return fileInfo.Length;
        }

        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            try
            {
                await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制文件失败: {SourcePath} -> {DestinationPath}", sourcePath, destinationPath);
                return false;
            }
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件失败: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<string> ReadFileContentAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }

        public async Task<bool> WriteFileContentAsync(string filePath, string content)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写入文件失败: {FilePath}", filePath);
                return false;
            }
        }

        public string GenerateUniqueFileName(string directory, string fileName, string pattern, int index)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var uniqueName = string.Format(pattern, nameWithoutExtension, index, extension);
            return Path.Combine(directory, uniqueName);
        }

        public ValidationResult ValidateFileExtension(string filePath, List<string> allowedExtensions)
        {
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            var isValid = extension != null && allowedExtensions.Contains(extension);
            return new ValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? string.Empty : $"不支持的文件扩展名: {extension ?? "未知"}"
            };
        }

        public Models.FileInfo GetFileInfo(string filePath)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            return new Models.FileInfo
            {
                Name = fileInfo.Name,
                FullPath = fileInfo.FullName,
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Extension = fileInfo.Extension
            };
        }

        // 保留原有的同步方法以兼容现有代码
        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        public DateTime GetLastWriteTime(string filePath)
        {
            return File.GetLastWriteTime(filePath);
        }
    }
}