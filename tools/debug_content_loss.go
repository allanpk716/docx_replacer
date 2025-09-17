package main

import (
	"fmt"
	"log"
	"os"

	"github.com/allanpk716/docx_replacer/internal/config"
	"github.com/allanpk716/docx_replacer/pkg/docx"
)

func main() {
	// 文件路径
	inputFile := "input/1.2.申请表.docx"
	json1File := "1.json"
	json2File := "2.json"
	output1File := "temp_debug/output1.docx"
	output2File := "temp_debug/output2.docx"

	// 确保输出目录存在
	os.MkdirAll("temp_debug", 0755)

	fmt.Println("=== 调试DOCX内容丢失问题 ===")

	// 1. 提取原始文档内容
	fmt.Println("\n1. 提取原始文档内容...")
	originalContent, err := extractDocumentContent(inputFile)
	if err != nil {
		log.Fatalf("提取原始文档内容失败: %v", err)
	}
	fmt.Printf("原始文档字符数: %d\n", len(originalContent))
	fmt.Printf("原始文档前200字符: %s\n", truncateString(originalContent, 200))

	// 2. 使用1.json进行替换
	fmt.Println("\n2. 使用1.json进行替换...")
	err = performReplacement(inputFile, json1File, output1File)
	if err != nil {
		log.Fatalf("使用1.json替换失败: %v", err)
	}

	content1, err := extractDocumentContent(output1File)
	if err != nil {
		log.Fatalf("提取替换后文档1内容失败: %v", err)
	}
	fmt.Printf("替换后文档1字符数: %d\n", len(content1))
	fmt.Printf("替换后文档1前200字符: %s\n", truncateString(content1, 200))

	// 3. 使用2.json进行替换
	fmt.Println("\n3. 使用2.json进行替换...")
	err = performReplacement(inputFile, json2File, output2File)
	if err != nil {
		log.Fatalf("使用2.json替换失败: %v", err)
	}

	content2, err := extractDocumentContent(output2File)
	if err != nil {
		log.Fatalf("提取替换后文档2内容失败: %v", err)
	}
	fmt.Printf("替换后文档2字符数: %d\n", len(content2))
	fmt.Printf("替换后文档2前200字符: %s\n", truncateString(content2, 200))

	// 4. 对比内容差异
	fmt.Println("\n4. 对比内容差异...")
	compareContents("原始", originalContent, "替换后1", content1)
	compareContents("原始", originalContent, "替换后2", content2)
	compareContents("替换后1", content1, "替换后2", content2)

	// 5. 保存内容到文件以便详细分析
	saveContentToFile("temp_debug/original_content.txt", originalContent)
	saveContentToFile("temp_debug/content1.txt", content1)
	saveContentToFile("temp_debug/content2.txt", content2)

	fmt.Println("\n调试完成！内容已保存到temp_debug目录")
}

// extractDocumentContent 提取文档内容
func extractDocumentContent(filePath string) (string, error) {
	processor := docx.NewEnhancedXMLProcessorWithCustomProps(filePath)
	return processor.ExtractTextContent()
}

// performReplacement 执行替换操作
func performReplacement(inputFile, configFile, outputFile string) error {
	// 加载配置
	configManager := config.NewConfigManager()
	cfg, err := configManager.LoadConfig(configFile)
	if err != nil {
		return fmt.Errorf("加载配置文件失败: %v", err)
	}

	// 构建替换映射
	replacements := configManager.GetKeywordMap(cfg)

	// 执行替换
	processor := docx.NewEnhancedXMLProcessorWithCustomProps(inputFile)
	return processor.ReplaceKeywordsWithTracking(replacements, outputFile)
}

// compareContents 对比两个内容的差异
func compareContents(name1, content1, name2, content2 string) {
	len1, len2 := len(content1), len(content2)
	fmt.Printf("%s vs %s: 字符数差异 %d (%d -> %d)\n", name1, name2, len2-len1, len1, len2)

	if len1 > len2 {
		fmt.Printf("⚠️  %s比%s少了%d个字符\n", name2, name1, len1-len2)
	} else if len2 > len1 {
		fmt.Printf("✅ %s比%s多了%d个字符\n", name2, name1, len2-len1)
	} else {
		fmt.Printf("✅ %s和%s字符数相同\n", name1, name2)
	}

	// 检查内容是否完全相同
	if content1 == content2 {
		fmt.Printf("✅ %s和%s内容完全相同\n", name1, name2)
	} else {
		fmt.Printf("⚠️  %s和%s内容不同\n", name1, name2)
		// 找出第一个不同的位置
		minLen := len1
		if len2 < minLen {
			minLen = len2
		}
		for i := 0; i < minLen; i++ {
			if content1[i] != content2[i] {
				fmt.Printf("首个差异位置: %d\n", i)
				start := i - 20
				if start < 0 {
					start = 0
				}
				end := i + 20
				if end > minLen {
					end = minLen
				}
				fmt.Printf("%s[%d:%d]: %s\n", name1, start, end, content1[start:end])
				fmt.Printf("%s[%d:%d]: %s\n", name2, start, end, content2[start:end])
				break
			}
		}
	}
}

// truncateString 截断字符串
func truncateString(s string, maxLen int) string {
	if len(s) <= maxLen {
		return s
	}
	return s[:maxLen] + "..."
}

// saveContentToFile 保存内容到文件
func saveContentToFile(filename, content string) {
	err := os.WriteFile(filename, []byte(content), 0644)
	if err != nil {
		fmt.Printf("保存文件%s失败: %v\n", filename, err)
	} else {
		fmt.Printf("内容已保存到: %s\n", filename)
	}
}