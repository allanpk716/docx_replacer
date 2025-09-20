using System;
using System.IO;
using System.Threading.Tasks;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 目录管理服务实现
    /// </summary>
    public class DirectoryManagerService : IDirectoryManager
    {
        private readonly ILogger<DirectoryManagerService> _logger;

        public DirectoryManagerService(ILogger<DirectoryManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建带时间戳的输出目录
        /// </summary>
        /// <param name="baseOutputPath">基础输出路径</param>
        /// <returns>创建的目录路径</returns>
        public async Task<string> CreateTimestampedDirectoryAsync(string baseOutputPath)
        {
            try
            {
                _logger.LogInformation($"创建时间戳目录，基础路径: {baseOutputPath}");

                if (string.IsNullOrWhiteSpace(baseOutputPath))
                {
                    throw new ArgumentException("基础输出路径不能为空", nameof(baseOutputPath));
                }

                // 确保基础目录存在
                await EnsureDirectoryExistsAsync(baseOutputPath);

                // 生成时间戳文件夹名称
                var timestampFolderName = GenerateTimestampFolderName();
                var timestampedPath = Path.Combine(baseOutputPath, timestampFolderName);

                // 创建时间戳目录
                await Task.Run(() => Directory.CreateDirectory(timestampedPath));

                _logger.LogInformation($"时间戳目录创建成功: {timestampedPath}");
                return timestampedPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建时间戳目录时发生错误: {baseOutputPath}");
                throw;
            }
        }

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>是否成功创建或已存在</returns>
        public async Task<bool> EnsureDirectoryExistsAsync(string directoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    _logger.LogWarning("目录路径为空");
                    return false;
                }

                if (Directory.Exists(directoryPath))
                {
                    return true;
                }

                await Task.Run(() => Directory.CreateDirectory(directoryPath));
                _logger.LogInformation($"目录创建成功: {directoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"确保目录存在时发生错误: {directoryPath}");
                return false;
            }
        }

        /// <summary>
        /// 根据源文件路径和基础路径计算相对路径
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="basePath">基础路径</param>
        /// <returns>相对路径</returns>
        public string GetRelativePath(string sourceFilePath, string basePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFilePath) || string.IsNullOrWhiteSpace(basePath))
                {
                    return string.Empty;
                }

                return Path.GetRelativePath(basePath, sourceFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"计算相对路径时发生错误: {sourceFilePath}, 基础路径: {basePath}, 错误: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 在输出目录中创建与源目录结构相同的子目录
        /// </summary>
        /// <param name="outputBasePath">输出基础路径</param>
        /// <param name="relativePath">相对路径</param>
        /// <returns>创建的完整目录路径</returns>
        public async Task<string> CreateMirrorDirectoryAsync(string outputBasePath, string relativePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputBasePath))
                {
                    throw new ArgumentException("输出基础路径不能为空", nameof(outputBasePath));
                }

                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    return outputBasePath;
                }

                // 获取相对路径的目录部分
                var relativeDirectory = Path.GetDirectoryName(relativePath);
                if (string.IsNullOrWhiteSpace(relativeDirectory))
                {
                    return outputBasePath;
                }

                // 构建完整的输出目录路径
                var fullOutputPath = Path.Combine(outputBasePath, relativeDirectory);

                // 确保目录存在
                await EnsureDirectoryExistsAsync(fullOutputPath);

                _logger.LogDebug($"镜像目录创建成功: {fullOutputPath}");
                return fullOutputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建镜像目录时发生错误: {outputBasePath}, 相对路径: {relativePath}");
                throw;
            }
        }

        /// <summary>
        /// 生成时间戳格式的文件夹名称
        /// </summary>
        /// <returns>时间戳文件夹名称，格式如：2025年1月19日163945</returns>
        public string GenerateTimestampFolderName()
        {
            try
            {
                var now = DateTime.Now;
                var folderName = $"{now.Year}年{now.Month}月{now.Day}日{now.Hour:D2}{now.Minute:D2}{now.Second:D2}";
                
                _logger.LogDebug($"生成时间戳文件夹名称: {folderName}");
                return folderName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成时间戳文件夹名称时发生错误");
                // 返回一个简单的时间戳作为备用
                return DateTime.Now.ToString("yyyyMMddHHmmss");
            }
        }
    }
}