using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using DocuFiller.Configuration;
using DocuFiller.Models;

namespace DocuFiller.Tools
{
    /// <summary>
    /// 端到端测试程序 - 用于验证表格单元格内容控件修复效果
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DocuFiller 端到端测试 ===");
            Console.WriteLine("测试目标: 验证表格单元格内容控件格式保留修复");
            Console.WriteLine();

            try
            {
                // 配置服务容器
                var serviceProvider = ConfigureServices();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                var processor = serviceProvider.GetRequiredService<DocumentProcessorService>();

                // 测试文件路径
                // 使用绝对路径以确保能找到测试文件
                string projectRoot = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer";
                string testDir = Path.Combine(projectRoot, "test_data", "t1");
                string templateFile = Path.Combine(testDir, "IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx");
                string dataFile = Path.Combine(testDir, "FD68 IVDR.xlsx");
                string outputDir = Path.Combine(testDir, "output");

                var templateArg = GetArg(args, "--template");
                if (!string.IsNullOrWhiteSpace(templateArg))
                {
                    templateFile = templateArg;
                }

                var dataArg = GetArg(args, "--data");
                if (!string.IsNullOrWhiteSpace(dataArg))
                {
                    dataFile = dataArg;
                }

                var outputArg = GetArg(args, "--output");
                if (!string.IsNullOrWhiteSpace(outputArg))
                {
                    outputDir = outputArg;
                }

                // 规范化路径
                templateFile = Path.GetFullPath(templateFile);
                dataFile = Path.GetFullPath(dataFile);
                outputDir = Path.GetFullPath(outputDir);

                Console.WriteLine("测试配置:");
                Console.WriteLine($"  模板文件: {templateFile}");
                Console.WriteLine($"  数据文件: {dataFile}");
                Console.WriteLine($"  输出目录: {outputDir}");
                Console.WriteLine();

                // 检查文件是否存在
                if (!File.Exists(templateFile))
                {
                    Console.WriteLine($"错误: 模板文件不存在: {templateFile}");
                    Console.WriteLine();
                    Console.WriteLine("请确保测试文件存在于以下位置:");
                    Console.WriteLine($"  {testDir}");
                    return;
                }

                if (!File.Exists(dataFile))
                {
                    Console.WriteLine($"错误: 数据文件不存在: {dataFile}");
                    Console.WriteLine();
                    Console.WriteLine("请确保测试文件存在于以下位置:");
                    Console.WriteLine($"  {testDir}");
                    return;
                }

                // 创建输出目录
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"已创建输出目录: {outputDir}");
                    Console.WriteLine();
                }

                Console.WriteLine("开始处理文档...");
                Console.WriteLine();

                // 创建处理请求
                var request = new ProcessRequest
                {
                    TemplateFilePath = templateFile,
                    DataFilePath = dataFile,
                    OutputDirectory = outputDir
                };

                // 处理文档
                var result = await processor.ProcessDocumentsAsync(request);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("=== 处理结果 ===");
                Console.WriteLine($"  成功: {result.IsSuccess}");
                Console.WriteLine($"  消息: {result.Message}");
                Console.WriteLine($"  总记录数: {result.TotalRecords}");
                Console.WriteLine($"  成功记录数: {result.SuccessfulRecords}");
                Console.WriteLine($"  失败记录数: {result.FailedRecords}");

                if (result.GeneratedFiles != null && result.GeneratedFiles.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("生成的文件:");
                    foreach (var file in result.GeneratedFiles)
                    {
                        var sysFileInfo = new System.IO.FileInfo(file);
                        Console.WriteLine($"  - {sysFileInfo.Name} ({sysFileInfo.Length / 1024} KB)");
                        Console.WriteLine($"    路径: {file}");
                    }
                }

                if (!result.IsSuccess)
                {
                    Console.WriteLine();
                    Console.WriteLine("错误详情:");
                    if (result.Errors != null && result.Errors.Count > 0)
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"  - {error}");
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("=== 验证步骤 ===");
                Console.WriteLine("请手动验证以下内容:");
                Console.WriteLine($"  1. 打开输出目录: {outputDir}");
                Console.WriteLine("  2. 打开生成的文档");
                Console.WriteLine("  3. 导航到章节 1.4.3.2 Instrument");
                Console.WriteLine("  4. 检查表格格式是否正常");
                Console.WriteLine("  5. 检查 'Brief Product Description' 列中的内容是否正确替换");
                Console.WriteLine("  6. 检查表格边框、列宽等格式是否保留");
                Console.WriteLine("  7. 检查是否有内容'跑到下一行'");
                Console.WriteLine("  8. 检查富文本格式(上标、下标等)是否保留");
                Console.WriteLine();

                if (result.IsSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ 测试执行成功!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ 测试执行失败!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Console.ResetColor();
            }
        }

        static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 配置设置
            var configuration = BuildConfiguration();
            services.AddSingleton(configuration);

            // 配置选项模式
            services.Configure<AppSettings>(configuration);
            services.Configure<LoggingSettings>(configuration.GetSection("Logging"));
            services.Configure<FileProcessingSettings>(configuration.GetSection("FileProcessing"));
            services.Configure<PerformanceSettings>(configuration.GetSection("Performance"));
            services.Configure<UISettings>(configuration.GetSection("UI"));

            // 配置日志记录
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            services.AddSingleton(loggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // 注册服务接口和实现
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDataParser, DataParserService>();
            services.AddSingleton<IExcelDataParser, ExcelDataParserService>();
            services.AddSingleton<IProgressReporter, ProgressReporterService>();
            services.AddSingleton<IDocumentProcessor, DocumentProcessorService>();
            services.AddSingleton<DocumentProcessorService>();  // 同时注册具体类
            services.AddSingleton<IFileScanner, FileScannerService>();
            services.AddSingleton<IDirectoryManager, DirectoryManagerService>();
            services.AddSingleton<IExcelToWordConverter, ExcelToWordConverterService>();
            services.AddSingleton<ISafeTextReplacer, SafeTextReplacer>();
            services.AddSingleton<ISafeFormattedContentReplacer, SafeFormattedContentReplacer>();

            // 注册内部服务
            services.AddSingleton<ContentControlProcessor>();
            services.AddSingleton<CommentManager>();
            services.AddSingleton<ITemplateCacheService, TemplateCacheService>();

            return services.BuildServiceProvider();
        }

        static IConfiguration BuildConfiguration()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.Production.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        static string? GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
