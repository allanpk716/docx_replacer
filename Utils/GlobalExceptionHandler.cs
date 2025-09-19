using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Utils
{
    /// <summary>
    /// 全局异常处理器
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static ILogger<App> _logger;
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DocuFiller", "Logs");
        
        /// <summary>
        /// 初始化全局异常处理
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public static void Initialize(ILogger<App> logger)
        {
            _logger = logger;
            
            // 确保日志目录存在
            Directory.CreateDirectory(LogDirectory);
            
            // 订阅全局异常事件
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        
        /// <summary>
        /// 处理应用程序域未处理异常
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            LogException(exception, "应用程序域未处理异常");
            
            if (e.IsTerminating)
            {
                ShowCriticalErrorDialog(exception);
            }
        }
        
        /// <summary>
        /// 处理UI线程未处理异常
        /// </summary>
        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "UI线程未处理异常");
            
            // 显示错误对话框
            ShowErrorDialog(e.Exception);
            
            // 标记异常已处理，防止应用程序崩溃
            e.Handled = true;
        }
        
        /// <summary>
        /// 处理任务未观察异常
        /// </summary>
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "任务未观察异常");
            
            // 标记异常已观察，防止应用程序崩溃
            e.SetObserved();
        }
        
        /// <summary>
        /// 记录异常信息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="context">异常上下文</param>
        public static void LogException(Exception exception, string context = "")
        {
            if (exception == null) return;
            
            try
            {
                // 使用结构化日志记录
                _logger?.LogError(exception, "[{Context}] 发生异常: {Message}", context, exception.Message);
                
                // 同时写入文件日志
                WriteToFileLog(exception, context);
            }
            catch
            {
                // 如果日志记录失败，至少尝试写入文件
                WriteToFileLog(exception, context);
            }
        }
        
        /// <summary>
        /// 写入文件日志
        /// </summary>
        private static void WriteToFileLog(Exception exception, string context)
        {
            try
            {
                var logFileName = $"error_{DateTime.Now:yyyyMMdd}.log";
                var logFilePath = Path.Combine(LogDirectory, logFileName);
                
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{context}]");
                logEntry.AppendLine($"异常类型: {exception.GetType().FullName}");
                logEntry.AppendLine($"异常消息: {exception.Message}");
                logEntry.AppendLine($"堆栈跟踪: {exception.StackTrace}");
                
                // 记录内部异常
                var innerException = exception.InnerException;
                var level = 1;
                while (innerException != null && level <= 5) // 最多记录5层内部异常
                {
                    logEntry.AppendLine($"内部异常 {level}: {innerException.GetType().FullName}");
                    logEntry.AppendLine($"内部异常消息 {level}: {innerException.Message}");
                    innerException = innerException.InnerException;
                    level++;
                }
                
                logEntry.AppendLine(new string('-', 80));
                
                File.AppendAllText(logFilePath, logEntry.ToString(), Encoding.UTF8);
            }
            catch
            {
                // 如果文件写入也失败，就没有其他办法了
            }
        }
        
        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private static void ShowErrorDialog(Exception exception)
        {
            try
            {
                var message = $"应用程序发生错误：\n\n{exception.Message}\n\n" +
                             $"错误详情已记录到日志文件中。\n" +
                             $"日志位置: {LogDirectory}\n\n" +
                             $"是否继续运行程序？";
                
                var result = MessageBox.Show(
                    message,
                    "应用程序错误",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);
                
                if (result == MessageBoxResult.No)
                {
                    Application.Current.Shutdown(1);
                }
            }
            catch
            {
                // 如果连错误对话框都显示不了，直接退出
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// 显示严重错误对话框
        /// </summary>
        private static void ShowCriticalErrorDialog(Exception exception)
        {
            try
            {
                var message = $"应用程序发生严重错误，即将退出：\n\n{exception?.Message}\n\n" +
                             $"错误详情已记录到日志文件中。\n" +
                             $"日志位置: {LogDirectory}";
                
                MessageBox.Show(
                    message,
                    "严重错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
            }
            catch
            {
                // 忽略对话框显示错误
            }
            finally
            {
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// 获取日志目录路径
        /// </summary>
        /// <returns>日志目录路径</returns>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }
        
        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        public static void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(LogDirectory)) return;
                
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(LogDirectory, "*.log");
                
                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "清理旧日志文件时发生错误");
            }
        }
    }
}