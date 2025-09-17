package main

import (
	"fmt"
	"os"

	"github.com/allanpk716/docx_replacer/pkg/docx"
)

func main() {
	if len(os.Args) < 3 {
		fmt.Println("Usage: go run extract_text.go <input_docx> <output_txt>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	// 创建增强XML处理器
	processor := docx.NewEnhancedXMLProcessorWithCustomProps(inputFile)

	// 提取文本内容
	textContent, err := processor.ExtractTextContent()
	if err != nil {
		fmt.Printf("提取文本内容失败: %v\n", err)
		os.Exit(1)
	}

	// 保存到文件
	err = os.WriteFile(outputFile, []byte(textContent), 0644)
	if err != nil {
		fmt.Printf("保存文本文件失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("文本内容已提取并保存到: %s\n", outputFile)
	fmt.Printf("文本长度: %d 字符\n", len(textContent))
}