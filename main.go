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
		showVersion = flag.Bool("version", false, "显示版本信息")
		verbose     = flag.Bool("verbose", false, "显示详细信息")
	)

	flag.Usage = func() {
		fmt.Fprintf(os.Stderr, "docx_replacer - DOCX文档关键词替换工具\n\n")
		fmt.Fprintf(os.Stderr, "用法: %s [选项]\n\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "选项:\n")
		flag.PrintDefaults()
		fmt.Fprintf(os.Stderr, "\n示例:\n")
		fmt.Fprintf(os.Stderr, "  %s -config=config.json\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "  %s -generate-config\n", os.Args[0])
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

	// 加载配置
	config, err := LoadConfig(*configPath)
	if err != nil {
		log.Fatalf("加载配置失败: %v", err)
	}

	if *verbose {
		fmt.Printf("配置加载成功:\n")
		if config.IsSingleMode() {
			fmt.Printf("  模式: 单文件处理\n")
			fmt.Printf("  输入文件: %s\n", config.InputDocx)
			fmt.Printf("  输出文件: %s\n", config.OutputDocx)
		} else if config.IsBatchMode() {
			fmt.Printf("  模式: 批量处理\n")
			fmt.Printf("  输入文件夹: %s\n", config.InputFolder)
			fmt.Printf("  输出文件夹: %s\n", config.OutputFolder)
		}
		fmt.Printf("  关键词数量: %d\n", len(config.Keywords))
		replacementMap := config.GetReplacementMap()
		fmt.Printf("  总替换规则数量: %d\n", len(replacementMap))
	}

	// 根据配置模式选择处理方式
	if config.IsSingleMode() {
		// 单文件模式
		if err := processSingleFile(config, *verbose); err != nil {
			log.Fatalf("处理文档失败: %v", err)
		}
		fmt.Println("文档处理完成!")
	} else if config.IsBatchMode() {
		// 批量处理模式
		batchProcessor := NewBatchProcessor(config, *verbose)
		if err := batchProcessor.ProcessBatch(); err != nil {
			log.Fatalf("批量处理失败: %v", err)
		}
	} else {
		log.Fatalf("无效的配置模式")
	}
}

// processSingleFile 处理单个文档替换
func processSingleFile(config *Config, verbose bool) error {
	// 检查输入文件是否存在
	if _, err := os.Stat(config.InputDocx); os.IsNotExist(err) {
		return fmt.Errorf("输入文件不存在: %s", config.InputDocx)
	}

	// 创建输出目录（如果不存在）
	outputDir := filepath.Dir(config.OutputDocx)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出目录失败: %v", err)
	}
	// 打开docx文件
	processor, err := NewDocxProcessor(config.InputDocx)
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
	if err := processor.SaveAs(config.OutputDocx); err != nil {
		return fmt.Errorf("保存文档失败: %w", err)
	}

	if verbose {
		fmt.Printf("文档已保存到: %s\n", config.OutputDocx)
	}

	return nil
}
