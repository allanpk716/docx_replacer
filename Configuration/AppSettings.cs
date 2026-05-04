namespace DocuFiller.Configuration
{
    /// <summary>
    /// 性能配置设置
    /// </summary>
    public class PerformanceSettings
    {
        /// <summary>
        /// 是否启用模板验证缓存
        /// </summary>
        public bool EnableTemplateCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;
    }
}
