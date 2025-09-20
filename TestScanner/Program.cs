using System;
using System.Threading.Tasks;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建服务容器
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddTransient<FileScannerService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var scanner = serviceProvider.GetService<FileScannerService>();
        
        string testPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data";
        
        Console.WriteLine($"测试扫描路径: {testPath}");
        Console.WriteLine($"路径是否有效: {scanner.IsValidFolder(testPath)}");
        
        try
        {
            Console.WriteLine("开始扫描文件...");
            var files = await scanner.ScanDocxFilesAsync(testPath);
            Console.WriteLine($"扫描到的文件数量: {files.Count}");
            
            foreach (var file in files)
            {
                Console.WriteLine($"文件: {file.Name} - {file.FullPath}");
            }
            
            Console.WriteLine("\n开始获取文件夹结构...");
            var folderStructure = await scanner.GetFolderStructureAsync(testPath);
            Console.WriteLine($"文件夹结构中的文件数量: {folderStructure.TotalDocxCount}");
            Console.WriteLine($"文件夹结构是否为空: {folderStructure.IsEmpty}");
            
            if (folderStructure.DocxFiles != null)
            {
                Console.WriteLine("\n文件夹结构中的文件:");
                foreach (var file in folderStructure.DocxFiles)
                {
                    Console.WriteLine($"  - {file.Name} ({file.FullPath})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"扫描时发生错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}