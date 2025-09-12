package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
)

const (
	version = "1.0.0"
)

func main() {
	fmt.Printf("docx_replacer version %s\n", version)
	fmt.Println("这是一个批量替换 Word 文档中关键字的工具。")
	fmt.Println("请按照提示，将相应的文件或文件夹拖拽到窗口中，然后按 Enter 键。")
	fmt.Println("-----------------------------------------------------------------")

	// 获取配置文件路径
	configPath := getDragDropPath("请拖拽 config.json 配置文件到此处，然后按 Enter：")

	// 获取输入文件夹路径
	inputPath := getDragDropPath("请拖拽包含 Word 文档的输入文件夹到此处，然后按 Enter：")

	// 获取输出文件夹路径
	outputPath := getDragDropPath("请拖拽用于存放结果的输出文件夹到此处，然后按 Enter：")

	// 加载配置
	config, err := LoadConfig(configPath)
	if err != nil {
		log.Fatalf("加载配置失败: %v", err)
	}

	verbose := true // 默认开启详细信息

	if verbose {
		fmt.Printf("配置加载成功:\n")
		fmt.Printf("  模式: 批量处理\n")
		fmt.Printf("  输入文件夹: %s\n", inputPath)
		fmt.Printf("  输出文件夹: %s\n", outputPath)
		fmt.Printf("  关键词数量: %d\n", len(config.Keywords))
		replacementMap := config.GetReplacementMap()
		fmt.Printf("  总替换规则数量: %d\n", len(replacementMap))
	}

	// 批量处理模式
	batchProcessor := NewBatchProcessor(config, inputPath, outputPath, verbose)
	if err := batchProcessor.ProcessBatch(); err != nil {
		log.Fatalf("批量处理失败: %v", err)
	}

	fmt.Println("-----------------------------------------------------------------")
	fmt.Println("所有文档处理完成！")
	fmt.Println("按 Enter 键退出程序...")
	bufio.NewReader(os.Stdin).ReadBytes('\n')
}

// getDragDropPath 提示用户拖拽文件/文件夹并返回路径
func getDragDropPath(prompt string) string {
	reader := bufio.NewReader(os.Stdin)
	for {
		fmt.Print(prompt)
		path, _ := reader.ReadString('\n')
		path = strings.TrimSpace(path)
		if path != "" {
			// 去除windows拖拽路径时可能带有的双引号
			path = strings.Trim(path, "\"")
			// 检查路径是否存在
			if _, err := os.Stat(path); os.IsNotExist(err) {
				fmt.Printf("错误：路径 '%s' 不存在，请重新输入\n", path)
				continue
			}
			return path
		}
	}
}
