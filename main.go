package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"path/filepath"
)

const (
	version = "1.0.0"
)

func main() {
	// 定义命令行参数
	var (
		configPath  = flag.String("config", "config.json", "配置文件路径")
		inputPath   = flag.String("input", "data\\2. 综述资料", "输入文件路径（单文件模式）或输入文件夹路径（批量模式）")
		outputPath  = flag.String("output", "out", "输出文件路径（单文件模式）或输出文件夹路径（批量模式）")
		batchMode   = flag.Bool("batch", true, "批量处理模式")
		showVersion = flag.Bool("version", false, "显示版本信息")
		verbose     = flag.Bool("verbose", false, "显示详细信息")
	)

	flag.Usage = func() {
		fmt.Fprintf(os.Stderr, "docx_replacer - DOCX文档关键词替换工具\n\n")
		fmt.Fprintf(os.Stderr, "用法: %s [选项]\n\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "选项:\n")
		flag.PrintDefaults()
		fmt.Fprintf(os.Stderr, "示例:\n")
		fmt.Fprintf(os.Stderr, "  单文件模式: %s -config=config.json -input=input.docx -output=output.docx\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "  批量模式: %s -config=config.json -input=./input -output=./output -batch\n", os.Args[0])
	}

	flag.Parse()

	// 显示版本信息
	if *showVersion {
		fmt.Printf("docx_replacer version %s\n", version)
		return
	}

	// 检查配置文件是否存在
	if _, err := os.Stat(*configPath); os.IsNotExist(err) {
		log.Fatalf("配置文件不存在: %s\n使用 -generate-config 生成示例配置文件", *configPath)
	}

	// 验证命令行参数
	if *inputPath == "" || *outputPath == "" {
		log.Fatalf("必须指定输入路径(-input)和输出路径(-output)")
	}

	// 加载配置
	config, err := LoadConfig(*configPath)
	if err != nil {
		log.Fatalf("加载配置失败: %v", err)
	}

	if *verbose {
		fmt.Printf("配置加载成功:\n")
		if *batchMode {
			fmt.Printf("  模式: 批量处理\n")
			fmt.Printf("  输入文件夹: %s\n", *inputPath)
			fmt.Printf("  输出文件夹: %s\n", *outputPath)
		} else {
			fmt.Printf("  模式: 单文件处理\n")
			fmt.Printf("  输入文件: %s\n", *inputPath)
			fmt.Printf("  输出文件: %s\n", *outputPath)
		}
		fmt.Printf("  关键词数量: %d\n", len(config.Keywords))
		replacementMap := config.GetReplacementMap()
		fmt.Printf("  总替换规则数量: %d\n", len(replacementMap))
	}

	// 根据模式选择处理方式
	if *batchMode {
		// 批量处理模式
		batchProcessor := NewBatchProcessor(config, *inputPath, *outputPath, *verbose)
		if err := batchProcessor.ProcessBatch(); err != nil {
			log.Fatalf("批量处理失败: %v", err)
		}
	} else {
		// 单文件模式
		if err := processSingleFile(config, *inputPath, *outputPath, *verbose); err != nil {
			log.Fatalf("处理文档失败: %v", err)
		}
		fmt.Println("文档处理完成!")
	}
}

// processSingleFile 处理单个文档替换
func processSingleFile(config *Config, inputPath, outputPath string, verbose bool) error {
	// 检查输入文件是否存在
	if _, err := os.Stat(inputPath); os.IsNotExist(err) {
		return fmt.Errorf("输入文件不存在: %s", inputPath)
	}

	// 创建输出目录（如果不存在）
	outputDir := filepath.Dir(outputPath)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出目录失败: %v", err)
	}
	// 打开docx文件
	processor, err := NewDocxProcessor(inputPath)
	if err != nil {
		return fmt.Errorf("打开文档失败: %w", err)
	}
	defer processor.Close()

	// 获取所有替换映射
	replacementMap := config.GetReplacementMap()

	// 执行替换
	if err := processor.ReplaceKeywords(replacementMap, verbose); err != nil {
		return fmt.Errorf("替换关键词失败: %w", err)
	}

	if verbose {
		fmt.Println("\n关键词替换完成")
	}

	// 保存文档
	if err := processor.SaveAs(outputPath); err != nil {
		return fmt.Errorf("保存文档失败: %w", err)
	}

	if verbose {
		fmt.Printf("文档已保存到: %s\n", outputPath)
	}

	return nil
}
