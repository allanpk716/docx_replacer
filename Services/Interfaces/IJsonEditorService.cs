using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Utils;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// JSON编辑器服务接口
    /// </summary>
    public interface IJsonEditorService
    {
        /// <summary>
        /// 加载JSON项目文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>JSON项目模型</returns>
        Task<JsonProjectModel> LoadProjectAsync(string filePath);

        /// <summary>
        /// 保存JSON项目到文件
        /// </summary>
        /// <param name="project">项目模型</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>保存是否成功</returns>
        Task<bool> SaveProjectAsync(JsonProjectModel project, string filePath);

        /// <summary>
        /// 验证JSON项目数据
        /// </summary>
        /// <param name="project">项目模型</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateProject(JsonProjectModel project);

        /// <summary>
        /// 格式化JSON字符串
        /// </summary>
        /// <param name="project">项目模型</param>
        /// <returns>格式化后的JSON字符串</returns>
        string FormatJsonString(JsonProjectModel project);

        /// <summary>
        /// 从JSON字符串解析项目
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        /// <returns>项目模型</returns>
        JsonProjectModel ParseJsonString(string jsonContent);

        /// <summary>
        /// 创建新的空项目
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <returns>新项目模型</returns>
        JsonProjectModel CreateNewProject(string projectName = "新项目");

        /// <summary>
        /// 创建项目备份
        /// </summary>
        /// <param name="filePath">原文件路径</param>
        /// <returns>备份是否成功</returns>
        Task<bool> CreateBackupAsync(string filePath);

        /// <summary>
        /// 验证文件路径是否有效
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateFilePath(string filePath);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件信息</returns>
        Task<DocuFiller.Models.FileInfo?> GetFileInfoAsync(string filePath);
    }
}