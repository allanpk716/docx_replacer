using DocuFiller.Utils;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 模板缓存服务接口
    /// </summary>
    public interface ITemplateCacheService
    {
        /// <summary>
        /// 获取模板验证结果（如果存在缓存）
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <returns>验证结果，如果未缓存则返回null</returns>
        ValidationResult? GetCachedValidationResult(string templatePath);

        /// <summary>
        /// 缓存模板验证结果
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <param name="result">验证结果</param>
        void CacheValidationResult(string templatePath, ValidationResult result);

        /// <summary>
        /// 获取模板内容控件信息（如果存在缓存）
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <returns>内容控件列表，如果未缓存则返回null</returns>
        List<Models.ContentControlData>? GetCachedContentControls(string templatePath);

        /// <summary>
        /// 缓存模板内容控件信息
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        /// <param name="controls">内容控件列表</param>
        void CacheContentControls(string templatePath, List<Models.ContentControlData> controls);

        /// <summary>
        /// 清除指定模板的缓存
        /// </summary>
        /// <param name="templatePath">模板文件路径</param>
        void InvalidateCache(string templatePath);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        void ClearAllCache();

        /// <summary>
        /// 清除过期缓存
        /// </summary>
        void ClearExpiredCache();
    }
}