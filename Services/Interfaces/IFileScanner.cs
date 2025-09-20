using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 文件扫描服务接口，用于扫描文件夹中的文档文件
    /// </summary>
    public interface IFileScanner
    {
        /// <summary>
        /// 扫描指定文件夹中的docx文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="includeSubfolders">是否包含子文件夹</param>
        /// <returns>文件信息列表</returns>
        Task<List<FileInfo>> ScanDocxFilesAsync(string folderPath, bool includeSubfolders = true);

        /// <summary>
        /// 验证文件夹路径是否有效
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>是否有效</returns>
        bool IsValidFolder(string folderPath);

        /// <summary>
        /// 获取文件夹结构信息
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>文件夹结构</returns>
        Task<FolderStructure> GetFolderStructureAsync(string folderPath);
    }
}