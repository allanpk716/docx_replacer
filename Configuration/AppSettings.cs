namespace DocuFiller.Configuration
{
    /// <summary>
    /// 应用程序配置设置
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 日志配置
        /// </summary>
        public LoggingSettings Logging { get; set; } = new();

        /// <summary>
        /// 文件处理配置
        /// </summary>
        public FileProcessingSettings FileProcessing { get; set; } = new();

        /// <summary>
        /// 性能配置
        /// </summary>
        public PerformanceSettings Performance { get; set; } = new();

        /// <summary>
        /// UI配置
        /// </summary>
        public UISettings UI { get; set; } = new();
    }

    /// <summary>
    /// 日志配置设置
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// 日志保留天数
        /// </summary>
        public int LogRetentionDays { get; set; } = 30;

        /// <summary>
        /// 日志文件路径模板
        /// </summary>
        public string LogFilePath { get; set; } = "Logs\\DocuFiller_{Date}.log";

        /// <summary>
        /// 是否启用控制台日志
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;
    }

    /// <summary>
    /// 文件处理配置设置
    /// </summary>
    public class FileProcessingSettings
    {
        /// <summary>
        /// 最大文件大小（字节）
        /// </summary>
        public long MaxFileSize { get; set; } = 104857600; // 100MB

        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        public List<string> SupportedExtensions { get; set; } = new() { ".docx", ".dotx" };

        /// <summary>
        /// 默认输出目录
        /// </summary>
        public string DefaultOutputDirectory { get; set; } = "Output";

        /// <summary>
        /// 是否启用文件备份
        /// </summary>
        public bool EnableFileBackup { get; set; } = true;
    }

    /// <summary>
    /// 性能配置设置
    /// </summary>
    public class PerformanceSettings
    {
        /// <summary>
        /// 最大并发处理数
        /// </summary>
        public int MaxConcurrentProcessing { get; set; } = 4;

        /// <summary>
        /// 处理超时时间（毫秒）
        /// </summary>
        public int ProcessingTimeout { get; set; } = 300000; // 5分钟

        /// <summary>
        /// 是否启用模板验证缓存
        /// </summary>
        public bool EnableTemplateCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;
    }

    /// <summary>
    /// UI配置设置
    /// </summary>
    public class UISettings
    {
        /// <summary>
        /// 是否自动保存设置
        /// </summary>
        public bool AutoSaveSettings { get; set; } = true;

        /// <summary>
        /// 是否显示详细进度信息
        /// </summary>
        public bool ShowProgressDetails { get; set; } = true;

        /// <summary>
        /// 退出前是否确认
        /// </summary>
        public bool ConfirmBeforeExit { get; set; } = true;

        /// <summary>
        /// 窗口默认宽度
        /// </summary>
        public int WindowWidth { get; set; } = 1000;

        /// <summary>
        /// 窗口默认高度
        /// </summary>
        public int WindowHeight { get; set; } = 700;
    }
}