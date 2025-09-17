package main

import (
	"encoding/json"
	"fmt"
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
	// 输入文件路径
	inputFile := "C:\\WorkSpace\\Go2Hell\\src\\github.com\\allanpk716\\docx_replacer\\input\\1.2.申请表.docx"
	configFile := "C:\\WorkSpace\\Go2Hell\\src\\github.com\\allanpk716\\docx_replacer\\1.json"
	outputFile := "C:\\WorkSpace\\Go2Hell\\src\\github.com\\allanpk716\\docx_replacer\\output_test.docx"

	// 检查输入文件是否存在
	if _, err := os.Stat(inputFile); os.IsNotExist(err) {
		log.Fatalf("输入文件不存在: %s", inputFile)
	}

	// 读取配置文件
	configData, err := os.ReadFile(configFile)
	if err != nil {
		log.Fatalf("读取配置文件失败: %v", err)
	}

	// 解析配置
	var config Config
	if err := json.Unmarshal(configData, &config); err != nil {
		log.Fatalf("解析配置文件失败: %v", err)
	}

	// 构建替换映射
	replacements := make(map[string]string)
	for _, keyword := range config.Keywords {
		replacements["#"+keyword.Key+"#"] = keyword.Value
	}

	fmt.Printf("项目名称: %s\n", config.ProjectName)
	fmt.Printf("关键词替换映射:\n")
	for key, value := range replacements {
		fmt.Printf("  %s -> %s\n", key, value)
	}
	fmt.Println()

	// 确保输出目录存在
	outputDir := filepath.Dir(outputFile)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		log.Fatalf("创建输出目录失败: %v", err)
	}

	// 创建Word兼容处理器
	wordProcessor := docx.NewWordCompatibleProcessor(inputFile)

	// 执行替换
	fmt.Println("开始使用Word兼容处理器处理文档...")
	if err := wordProcessor.ReplaceKeywordsWithWordCompatibility(replacements, outputFile); err != nil {
		log.Fatalf("Word兼容处理失败: %v", err)
	}

	fmt.Printf("\n处理完成！输出文件: %s\n", outputFile)
	fmt.Println("请用Word打开输出文件检查显示效果。")
}