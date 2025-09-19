using System.Collections.Generic;
using System.Threading.Tasks;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 文件服务接口
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 验证文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否存在</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// 验证目录是否存在，不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>是否成功</returns>
        bool EnsureDirectoryExists(string directoryPath);

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件大小（字节）</returns>
        long GetFileSize(string filePath);

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destinationPath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns>是否成功</returns>
        Task<bool> CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool DeleteFile(string filePath);

        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容</returns>
        Task<string> ReadFileContentAsync(string filePath);

        /// <summary>
        /// 写入文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">文件内容</param>
        /// <returns>是否成功</returns>
        Task<bool> WriteFileContentAsync(string filePath, string content);

        /// <summary>
        /// 生成唯一文件名
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <param name="fileName">原始文件名</param>
        /// <param name="pattern">文件名模式</param>
        /// <param name="index">索引</param>
        /// <returns>唯一文件名</returns>
        string GenerateUniqueFileName(string directory, string fileName, string pattern, int index);

        /// <summary>
        /// 验证文件扩展名
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="allowedExtensions">允许的扩展名列表</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateFileExtension(string filePath, List<string> allowedExtensions);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件信息</returns>
        Models.FileInfo GetFileInfo(string filePath);

        /// <summary>
        /// 获取文件最后修改时间
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>最后修改时间</returns>
        System.DateTime GetLastWriteTime(string filePath);

        /// <summary>
        /// 读取文件所有文本内容（同步方法）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容</returns>
        string ReadAllText(string filePath);
    }

}