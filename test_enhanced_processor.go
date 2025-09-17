package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"os"
	"path/filepath"

	"github.com/allanpk716/docx_replacer/pkg/docx"
)

// Config 配置结构
type Config struct {
	ProjectName string    `json:"project_name"`
	Keywords    []Keyword `json:"keywords"`
}

// Keyword 关键词结构
type Keyword struct {
	Key        string `json:"key"`
	Value      string `json:"value"`
	SourceFile string `json:"source_file"`
}

func main() {
	if len(os.Args) != 4 {
		fmt.Println("使用方法: go run test_enhanced_processor.go <输入docx文件> <配置json文件> <输出docx文件>")
		fmt.Println("示例: go run test_enhanced_processor.go input.docx config.json output.docx")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	configFile := os.Args[2]
	outputFile := os.Args[3]

	fmt.Println("=== 增强Word兼容处理器测试 ===")
	fmt.Printf("输入文件: %s\n", inputFile)
	fmt.Printf("配置文件: %s\n", configFile)
	fmt.Printf("输出文件: %s\n", outputFile)
	fmt.Println()

	// 检查输入文件是否存在
	if _, err := os.Stat(inputFile); os.IsNotExist(err) {
		log.Fatalf("输入文件不存在: %s", inputFile)
	}

	// 检查配置文件是否存在
	if _, err := os.Stat(configFile); os.IsNotExist(err) {
		log.Fatalf("配置文件不存在: %s", configFile)
	}

	// 读取配置文件
	fmt.Println("正在加载配置文件...")
	configData, err := ioutil.ReadFile(configFile)
	if err != nil {
		log.Fatalf("读取配置文件失败: %v", err)
	}

	var config Config
	if err := json.Unmarshal(configData, &config); err != nil {
		log.Fatalf("解析配置文件失败: %v", err)
	}

	fmt.Printf("项目名称: %s\n", config.ProjectName)
	fmt.Printf("关键词数量: %d\n", len(config.Keywords))

	// 转换关键词为map格式
	replacements := make(map[string]string)
	for _, keyword := range config.Keywords {
		replacements[keyword.Key] = keyword.Value
		fmt.Printf("  '%s' -> '%s'\n", keyword.Key, keyword.Value)
	}
	fmt.Println()

	// 创建增强的Word兼容处理器
	fmt.Println("创建增强Word兼容处理器...")
	processor := docx.NewEnhancedWordCompatibleProcessor(replacements)
	fmt.Println(processor.GetProcessorInfo())
	fmt.Println()

	// 确保输出目录存在
	outputDir := filepath.Dir(outputFile)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		log.Fatalf("创建输出目录失败: %v", err)
	}

	// 执行替换
	fmt.Println("开始执行关键词替换...")
	if err := processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile); err != nil {
		log.Fatalf("替换失败: %v", err)
	}

	fmt.Println()
	fmt.Println("=== 处理完成 ===")
	fmt.Printf("输出文件已生成: %s\n", outputFile)
	fmt.Println("请使用Microsoft Word打开输出文件检查显示效果。")
	fmt.Println()
	fmt.Println("增强处理器特点:")
	fmt.Println("- 只在<w:t>标签内进行精确文本替换")
	fmt.Println("- 保持所有XML结构和属性不变")
	fmt.Println("- 保持原始文件的压缩格式和元数据")
	fmt.Println("- 确保XML安全性，自动转义特殊字符")
	fmt.Println("- 最大化Word兼容性")
}