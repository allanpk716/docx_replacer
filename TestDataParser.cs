using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DocuFiller.Test
{
    /// <summary>
    /// 简单的数据解析测试
    /// </summary>
    public class TestDataParser
    {
        public static void Main(string[] args)
        {
            var testJsonPath = @"C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\1.json";
            
            Console.WriteLine("开始测试数据解析功能...");
            Console.WriteLine($"测试文件: {testJsonPath}");
            Console.WriteLine();
            
            try
            {
                // 读取JSON文件
                var jsonContent = File.ReadAllText(testJsonPath);
                Console.WriteLine("原始JSON内容:");
                Console.WriteLine(jsonContent);
                Console.WriteLine();
                
                // 解析JSON
                var jsonObject = JObject.Parse(jsonContent);
                
                Console.WriteLine("解析后的结构:");
                
                // 模拟我们修改后的解析逻辑
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name.Equals("keywords", StringComparison.OrdinalIgnoreCase) && property.Value is JArray keywordsArray)
                    {
                        Console.WriteLine($"处理 keywords 数组，包含 {keywordsArray.Count} 个元素:");
                        
                        foreach (var keywordItem in keywordsArray)
                        {
                            if (keywordItem is JObject keywordObj)
                            {
                                var key = keywordObj["key"]?.ToString();
                                var value = keywordObj["value"]?.ToString();
                                
                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    Console.WriteLine($"  键值对: {key} = {value}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  普通属性: {property.Name} = {property.Value}");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("测试完成！数据解析逻辑应该能正确处理 keywords 数组结构。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}