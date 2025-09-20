using System;
using System.Threading.Tasks;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 目录管理服务接口，用于管理输出目录结构
    /// </summary>
    public interface IDirectoryManager
    {
        /// <summary>
        /// 创建带时间戳的输出目录
        /// </summary>
        /// <param name="baseOutputPath">基础输出路径</param>
        /// <returns>创建的目录路径</returns>
        Task<string> CreateTimestampedDirectoryAsync(string baseOutputPath);

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>是否成功创建或已存在</returns>
        Task<bool> EnsureDirectoryExistsAsync(string directoryPath);

        /// <summary>
        /// 根据源文件路径和基础路径计算相对路径
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="basePath">基础路径</param>
        /// <returns>相对路径</returns>
        string GetRelativePath(string sourceFilePath, string basePath);

        /// <summary>
        /// 在输出目录中创建与源目录结构相同的子目录
        /// </summary>
        /// <param name="outputBasePath">输出基础路径</param>
        /// <param name="relativePath">相对路径</param>
        /// <returns>创建的完整目录路径</returns>
        Task<string> CreateMirrorDirectoryAsync(string outputBasePath, string relativePath);

        /// <summary>
        /// 生成时间戳格式的文件夹名称
        /// </summary>
        /// <returns>时间戳文件夹名称，格式如：2025年1月19日163945</returns>
        string GenerateTimestampFolderName();
    }
}