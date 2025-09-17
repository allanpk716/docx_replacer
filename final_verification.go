package main

import (
	"fmt"
	"log"
	"os"
	"path/filepath"

	"github.com/allanpk716/docx_replacer/pkg/docx"
)

func main() {
	// 设置测试文件路径
	inputFile := "input/1.2.申请表.docx"
	outputFile := "out1/final_test_result.docx"

	// 检查输入文件是否存在
	if _, err := os.Stat(inputFile); os.IsNotExist(err) {
		fmt.Printf("输入文件不存在: %s\n", inputFile)
		return
	}

	// 创建输出目录
	outputDir := filepath.Dir(outputFile)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		log.Fatalf("创建输出目录失败: %v", err)
	}

	fmt.Printf("输入文件: %s\n", inputFile)
	fmt.Printf("输出文件: %s\n", outputFile)

	// 定义关键词映射
	replacements := map[string]string{
		"#产品名称#":   "D-二聚体测定试剂盒（胶乳免疫比浊法）",
		"#预期用途#":   "用于体外定量检测人血浆中D-二聚体的含量",
		"#主要组成成分#": "试剂R1、试剂R2、校准品、质控品",
		"#结构及组成#":  "adsasdadsa", // 这是表格中的关键词
	}

	fmt.Printf("关键词数量: %d\n", len(replacements))

	// 使用增强Word兼容处理器
	fmt.Println("开始处理文件:", inputFile)
	fmt.Println("使用增强Word兼容模式...")
	processor := docx.NewEnhancedWordCompatibleProcessor(replacements)

	// 执行替换
	if err := processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile); err != nil {
		log.Fatalf("处理失败: %v", err)
	}

	fmt.Printf("文件处理完成: %s\n", outputFile)
	fmt.Println("\n=== 测试完成 ===")
	fmt.Println("请用Word打开输出文件，验证以下内容:")
	fmt.Println("1. 正文中的 #产品名称# 是否被正确替换")
	fmt.Println("2. 表格中的 #结构及组成# 是否被正确替换")
	fmt.Println("3. 其他关键词是否都被正确替换")
	fmt.Println("4. 文档格式是否保持完整")
}