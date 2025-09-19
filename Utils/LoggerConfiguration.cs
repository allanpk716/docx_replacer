using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace DocuFiller.Utils
{
    /// <summary>
    /// 日志配置类
    /// </summary>
    public static class LoggerConfiguration
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string LogFileName = "DocuFiller_{0:yyyyMMdd}.log";
        private static readonly object LockObject = new object();
        
        /// <summary>
        /// 配置日志记录器
        /// </summary>
        /// <returns>日志记录器工厂</returns>
        public static ILoggerFactory CreateLoggerFactory()
        {
            EnsureLogDirectoryExists();
            
            var factory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .AddDebug()
                    .AddProvider(new FileLoggerProvider(GetCurrentLogFilePath()))
                    .SetMinimumLevel(LogLevel.Information);
                    
#if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
#endif
            });
            
            return factory;
        }
        
        /// <summary>
        /// 获取当前日志文件路径
        /// </summary>
        /// <returns>日志文件路径</returns>
        public static string GetCurrentLogFilePath()
        {
            return Path.Combine(LogDirectory, string.Format(LogFileName, DateTime.Now));
        }
        
        /// <summary>
        /// 获取日志目录路径
        /// </summary>
        /// <returns>日志目录路径</returns>
        public static string GetLogDirectoryPath()
        {
            return LogDirectory;
        }
        
        /// <summary>
        /// 确保日志目录存在
        /// </summary>
        private static void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }
        
        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        public static void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;
                    
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(LogDirectory, "DocuFiller_*.log");
                
                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(logFile);
                        }
                        catch
                        {
                            // 忽略删除失败的文件
                        }
                    }
                }
            }
            catch
            {
                // 忽略清理过程中的异常
            }
        }
        
        /// <summary>
        /// 获取日志文件大小
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        /// <returns>文件大小（字节）</returns>
        public static long GetLogFileSize(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return new FileInfo(filePath).Length;
                }
            }
            catch
            {
                // 忽略获取文件大小时的异常
            }
            return 0;
        }
    }
    
    /// <summary>
    /// 文件日志记录器提供程序
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        
        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }
        
        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _filePath, _lock);
        }
        
        public void Dispose()
        {
            // 清理资源
        }
    }
    
    /// <summary>
    /// 文件日志记录器
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _filePath;
        private readonly object _lock;
        
        public FileLogger(string categoryName, string filePath, object lockObject)
        {
            _categoryName = categoryName;
            _filePath = filePath;
            _lock = lockObject;
        }
        
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
                
            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
                return;
                
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception}";
            }
            
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_filePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // 忽略写入日志时的异常，避免影响主程序
                }
            }
        }
    }
}