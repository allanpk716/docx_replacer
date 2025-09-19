using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using DocuFiller.Models;

namespace DocuFiller.Test
{
    /// <summary>
    /// 测试文档替换功能
    /// </summary>
    public class TestReplacement
    {
        public static async Task Main(string[] args)
        {
            // 设置依赖注入
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddTransient<IFileService, FileService>();
            services.AddTransient<IDataParser, DataParserService>();
            services.AddTransient<IProgressReporter, ProgressReporterService>();
            services.AddTransient<IDocumentProcessor, DocumentProcessorService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<TestReplacement>>();
            var documentProcessor = serviceProvider.GetRequiredService<IDocumentProcessor>();
            var dataParser = serviceProvider.GetRequiredService<IDataParser>();
            
            try
            {
                Console.WriteLine("开始测试文档替换功能...");
                
                // 测试文件路径
                var templatePath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\1.2.申请表-test.docx";
                var dataPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\1.json";
                var outputPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_output.docx";
                
                Console.WriteLine($"模板文件: {templatePath}");
                Console.WriteLine($"数据文件: {dataPath}");
                Console.WriteLine($"输出文件: {outputPath}");
                
                // 验证文件存在
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"错误: 模板文件不存在 - {templatePath}");
                    return;
                }
                
                if (!File.Exists(dataPath))
                {
                    Console.WriteLine($"错误: 数据文件不存在 - {dataPath}");
                    return;
                }
                
                // 解析数据文件
                Console.WriteLine("\n解析数据文件...");
                var dataList = await dataParser.ParseJsonFileAsync(dataPath);
                
                if (dataList == null || dataList.Count == 0)
                {
                    Console.WriteLine("错误: 数据文件解析失败或为空");
                    return;
                }
                
                Console.WriteLine($"解析到 {dataList.Count} 条数据记录");
                
                // 打印解析后的数据
                for (int i = 0; i < dataList.Count; i++)
                {
                    Console.WriteLine($"\n数据记录 {i + 1}:");
                    foreach (var kvp in dataList[i])
                    {
                        Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
                    }
                }
                
                // 获取内容控件信息
                Console.WriteLine("\n获取模板文件中的内容控件...");
                var contentControls = await documentProcessor.GetContentControlsAsync(templatePath);
                
                Console.WriteLine($"找到 {contentControls.Count} 个内容控件:");
                foreach (var control in contentControls)
                {
                    Console.WriteLine($"  标签: {control.Tag}, 标题: {control.Title}");
                }
                
                // 处理文档
                Console.WriteLine("\n开始处理文档...");
                var success = await documentProcessor.ProcessSingleDocumentAsync(templatePath, outputPath, dataList[0]);
                
                if (success)
                {
                    Console.WriteLine($"\n成功! 输出文件已生成: {outputPath}");
                    Console.WriteLine("请检查输出文件中的内容控件是否被正确替换。");
                }
                else
                {
                    Console.WriteLine("\n失败: 文档处理失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}