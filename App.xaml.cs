using System;
using System.Configuration;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.ViewModels;
using DocuFiller.Utils;

namespace DocuFiller
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        private ILogger<App> _logger;
        
        /// <summary>
        /// 获取服务提供程序
        /// </summary>
        public ServiceProvider ServiceProvider => _serviceProvider;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 配置服务容器
                ConfigureServices();
                
                // 获取日志记录器
                _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                _logger.LogInformation("应用程序启动");
                
                // 配置全局异常处理
                ConfigureGlobalExceptionHandling();
                
                // 清理旧日志文件
                CleanupOldLogs();
                
                // 创建并显示主窗口
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.LogInformation("应用程序退出");
                _serviceProvider?.Dispose();
            }
            catch (Exception ex)
            {
                // 记录退出时的异常，但不阻止程序退出
                System.Diagnostics.Debug.WriteLine($"应用程序退出时发生异常: {ex.Message}");
            }
            
            base.OnExit(e);
        }
        
        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // 配置日志记录
            var loggerFactory = LoggerConfiguration.CreateLoggerFactory();
            services.AddSingleton(loggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            
            // 注册服务接口和实现
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDataParser, DataParserService>();
            services.AddSingleton<IProgressReporter, ProgressReporterService>();
            services.AddSingleton<IDocumentProcessor, DocumentProcessorService>();
            services.AddSingleton<IFileScanner, FileScannerService>();
            services.AddSingleton<IDirectoryManager, DirectoryManagerService>();
            services.AddSingleton<OpenXmlDocumentHandler>();
            
            // 注册ViewModels
            services.AddTransient<MainWindowViewModel>();
            
            // 注册主窗口
            services.AddTransient<MainWindow>();
            
            _serviceProvider = services.BuildServiceProvider();
        }
        
        /// <summary>
        /// 配置全局异常处理
        /// </summary>
        private void ConfigureGlobalExceptionHandling()
        {
            GlobalExceptionHandler.Initialize(_logger);
            
            // 订阅应用程序域未处理异常事件
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                _logger?.LogCritical(exception, "应用程序域发生未处理异常");
                
                if (e.IsTerminating)
                {
                    MessageBox.Show(
                        $"应用程序遇到严重错误，即将退出:\n{exception?.Message}", 
                        "严重错误", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            };
            
            // 订阅调度程序未处理异常事件
            Dispatcher.UnhandledException += (sender, e) =>
            {
                _logger?.LogError(e.Exception, "调度程序发生未处理异常");
                
                var result = MessageBox.Show(
                    $"应用程序遇到错误:\n{e.Exception.Message}\n\n是否继续运行？", 
                    "错误", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    e.Handled = true;
                }
            };
        }
        
        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        private void CleanupOldLogs()
        {
            try
            {
                var retentionDays = GetConfigValue("LogRetentionDays", 30);
                LoggerConfiguration.CleanupOldLogs(retentionDays);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "清理旧日志文件时发生异常");
            }
        }
        
        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        private T GetConfigValue<T>(string key, T defaultValue)
        {
            try
            {
                var value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(value))
                    return defaultValue;
                    
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public T GetService<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        
        /// <summary>
        /// 获取当前应用程序实例
        /// </summary>
        /// <returns>应用程序实例</returns>
        public static new App Current => (App)Application.Current;
    }
}