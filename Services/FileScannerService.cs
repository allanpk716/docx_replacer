using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 文件扫描服务实现
    /// </summary>
    public class FileScannerService : IFileScanner
    {
        private readonly ILogger<FileScannerService> _logger;

        public FileScannerService(ILogger<FileScannerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 扫描指定文件夹中的docx文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="includeSubfolders">是否包含子文件夹</param>
        /// <returns>文件信息列表</returns>
        public async Task<List<Models.FileInfo>> ScanDocxFilesAsync(string folderPath, bool includeSubfolders = true)
        {
            try
            {
                _logger.LogInformation($"开始扫描文件夹: {folderPath}, 包含子文件夹: {includeSubfolders}");

                if (!IsValidFolder(folderPath))
                {
                    _logger.LogWarning($"无效的文件夹路径: {folderPath}");
                    return new List<Models.FileInfo>();
                }

                var files = new List<Models.FileInfo>();
                var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                await Task.Run(() =>
                {
                    var docxFiles = Directory.GetFiles(folderPath, "*.docx", searchOption)
                        .Where(file => !Path.GetFileName(file).StartsWith("~$")) // 排除临时文件
                        .ToArray();

                    _logger.LogInformation($"找到 {docxFiles.Length} 个docx文件");
                    foreach (var file in docxFiles)
                    {
                        _logger.LogInformation($"发现文件: {file}");
                    }

                    foreach (var filePath in docxFiles)
                    {
                        try
                        {
                            var fileInfo = new System.IO.FileInfo(filePath);
                            var docFileInfo = new Models.FileInfo
                            {
                                Name = fileInfo.Name,
                                FullPath = fileInfo.FullName,
                                Size = fileInfo.Length,
                                CreationTime = fileInfo.CreationTime,
                                LastModified = fileInfo.LastWriteTime,
                                Extension = fileInfo.Extension,
                                IsReadOnly = fileInfo.IsReadOnly,
                                DirectoryPath = fileInfo.DirectoryName ?? string.Empty,
                                RelativePath = Path.GetRelativePath(folderPath, fileInfo.FullName),
                                RelativeDirectoryPath = Path.GetRelativePath(folderPath, fileInfo.DirectoryName ?? string.Empty)
                            };

                            files.Add(docFileInfo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"无法读取文件信息: {filePath}, 错误: {ex.Message}");
                        }
                    }
                });

                _logger.LogInformation($"扫描完成，找到 {files.Count} 个docx文件");
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"扫描文件夹时发生错误: {folderPath}");
                throw;
            }
        }

        /// <summary>
        /// 验证文件夹路径是否有效
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>是否有效</returns>
        public bool IsValidFolder(string folderPath)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"验证文件夹路径时发生错误: {folderPath}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取文件夹结构信息
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>文件夹结构</returns>
        public async Task<FolderStructure> GetFolderStructureAsync(string folderPath)
        {
            try
            {
                _logger.LogInformation($"开始获取文件夹结构: {folderPath}");

                if (!IsValidFolder(folderPath))
                {
                    throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");
                }

                return await Task.Run(() => BuildFolderStructure(folderPath, folderPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取文件夹结构时发生错误: {folderPath}");
                throw;
            }
        }

        /// <summary>
        /// 递归构建文件夹结构
        /// </summary>
        /// <param name="currentPath">当前路径</param>
        /// <param name="rootPath">根路径</param>
        /// <returns>文件夹结构</returns>
        private FolderStructure BuildFolderStructure(string currentPath, string rootPath)
        {
            var directoryInfo = new DirectoryInfo(currentPath);
            var folderStructure = new FolderStructure
            {
                Name = directoryInfo.Name,
                FullPath = directoryInfo.FullName,
                RelativePath = Path.GetRelativePath(rootPath, directoryInfo.FullName),
                CreationTime = directoryInfo.CreationTime,
                LastModified = directoryInfo.LastWriteTime
            };

            // 获取当前文件夹中的docx文件
            try
            {
                var docxFiles = directoryInfo.GetFiles("*.docx")
                    .Where(file => !file.Name.StartsWith("~$")) // 排除临时文件
                    .Select(file => new Models.FileInfo
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        Size = file.Length,
                        CreationTime = file.CreationTime,
                        LastModified = file.LastWriteTime,
                        Extension = file.Extension,
                        IsReadOnly = file.IsReadOnly,
                        DirectoryPath = file.DirectoryName ?? string.Empty,
                        RelativePath = Path.GetRelativePath(rootPath, file.FullName),
                        RelativeDirectoryPath = Path.GetRelativePath(rootPath, file.DirectoryName ?? string.Empty)
                    })
                    .ToList();

                folderStructure.DocxFiles = docxFiles;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"读取文件夹中的文件时发生错误: {currentPath}, 错误: {ex.Message}");
            }

            // 递归获取子文件夹结构
            try
            {
                var subDirectories = directoryInfo.GetDirectories();
                foreach (var subDir in subDirectories)
                {
                    try
                    {
                        var subFolderStructure = BuildFolderStructure(subDir.FullName, rootPath);
                        folderStructure.SubFolders.Add(subFolderStructure);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"处理子文件夹时发生错误: {subDir.FullName}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"读取子文件夹时发生错误: {currentPath}, 错误: {ex.Message}");
            }

            return folderStructure;
        }
    }
}