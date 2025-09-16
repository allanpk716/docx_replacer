package main

import (
	"context"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"
	"time"

	"github.com/allanpk716/docx_replacer/internal/cmd"
	"github.com/allanpk716/docx_replacer/internal/config"
	"github.com/allanpk716/docx_replacer/pkg/docx"
)

func main() {
	// 解析命令行参数
	args := cmd.ParseCommandLineArgs()

	// 处理版本和帮助信息
	if args.ShowVersion {
		fmt.Printf("%s v%s\n", cmd.AppName, cmd.AppVersion)
		return
	}

	if args.ShowHelp {
		cmd.ShowUsage()
		return
	}

	// 设置日志级别
	if args.Verbose {
		log.SetFlags(log.LstdFlags | log.Lshortfile)
	} else {
		log.SetFlags(log.LstdFlags)
	}

	log.Printf("启动 %s v%s", cmd.AppName, cmd.AppVersion)

	// 验证参数
	if err := cmd.ValidateArgs(args); err != nil {
		log.Fatalf("参数验证失败: %v", err)
	}

	// 加载配置文件
	configManager := config.NewConfigManager()
	cfg, err := configManager.LoadConfig(args.ConfigFile)
	if err != nil {
		log.Fatalf("加载配置文件失败: %v", err)
	}

	log.Printf("成功加载配置文件: %s (项目: %s, 关键词数量: %d)", 
		args.ConfigFile, cfg.ProjectName, len(cfg.Keywords))

	// 获取关键词映射
	keywordMap := configManager.GetKeywordMap(cfg)
	if len(keywordMap) == 0 {
		log.Fatalf("没有找到有效的关键词")
	}

	// 创建上下文
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Minute)
	defer cancel()

	// 使用XML处理器执行处理
	if err := executeXMLProcessing(ctx, args, keywordMap); err != nil {
		log.Fatalf("处理失败: %v", err)
	}

	log.Println("处理完成")
}

// executeXMLProcessing 使用XML处理器执行处理逻辑
func executeXMLProcessing(ctx context.Context, args *cmd.CommandLineArgs, keywordMap map[string]string) error {
	if args.InputFile != "" {
		// 单文件处理
		return processXMLSingleFile(ctx, args.InputFile, args.OutputFile, keywordMap)
	} else {
		// 批量处理
		return processXMLBatchFiles(ctx, args.InputDir, args.OutputDir, keywordMap)
	}
}

// processXMLSingleFile 使用XML处理器处理单个文件
func processXMLSingleFile(ctx context.Context, inputFile, outputFile string, keywordMap map[string]string) error {
	log.Printf("处理文件: %s -> %s", inputFile, outputFile)

	// 检查输入文件是否存在
	if _, err := os.Stat(inputFile); os.IsNotExist(err) {
		return fmt.Errorf("输入文件不存在: %s", inputFile)
	}

	// 确保输出目录存在
	outputDir := filepath.Dir(outputFile)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出目录失败: %w", err)
	}

	// 创建XML处理器并执行替换
	xmlProcessor := docx.NewXMLProcessor(inputFile)
	if err := xmlProcessor.ReplaceKeywords(keywordMap, outputFile); err != nil {
		return fmt.Errorf("处理文件失败: %w", err)
	}

	log.Printf("文件处理完成: %s", outputFile)
	return nil
}

// processXMLBatchFiles 使用XML处理器批量处理文件
func processXMLBatchFiles(ctx context.Context, inputDir, outputDir string, keywordMap map[string]string) error {
	// 创建输出目录
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出目录失败: %w", err)
	}

	// 查找所有 DOCX 文件
	docxFiles, err := findDocxFiles(inputDir)
	if err != nil {
		return fmt.Errorf("查找 DOCX 文件失败: %w", err)
	}

	if len(docxFiles) == 0 {
		return fmt.Errorf("在目录 %s 中没有找到 DOCX 文件", inputDir)
	}

	log.Printf("找到 %d 个 DOCX 文件", len(docxFiles))

	// 处理每个文件
	for i, inputFile := range docxFiles {
		select {
		case <-ctx.Done():
			return ctx.Err()
		default:
		}

		// 生成输出文件路径
		relPath, err := filepath.Rel(inputDir, inputFile)
		if err != nil {
			return fmt.Errorf("计算相对路径失败: %w", err)
		}

		outputFile := filepath.Join(outputDir, relPath)

		// 确保输出文件的目录存在
		outputFileDir := filepath.Dir(outputFile)
		if err := os.MkdirAll(outputFileDir, 0755); err != nil {
			return fmt.Errorf("创建输出文件目录失败: %w", err)
		}

		log.Printf("[%d/%d] 处理文件: %s", i+1, len(docxFiles), inputFile)

		// 创建XML处理器并执行替换
		xmlProcessor := docx.NewXMLProcessor(inputFile)
		if err := xmlProcessor.ReplaceKeywords(keywordMap, outputFile); err != nil {
			log.Printf("处理文件失败 %s: %v", inputFile, err)
			continue
		}

		log.Printf("文件处理完成: %s", outputFile)
	}

	log.Printf("批量处理完成，共处理 %d 个文件", len(docxFiles))
	return nil
}

// findDocxFiles 查找目录中的所有 DOCX 文件
func findDocxFiles(dir string) ([]string, error) {
	var docxFiles []string

	err := filepath.Walk(dir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}

		if !info.IsDir() && strings.ToLower(filepath.Ext(path)) == ".docx" {
			// 排除临时文件
			filename := filepath.Base(path)
			if !strings.HasPrefix(filename, "~$") {
				docxFiles = append(docxFiles, path)
			}
		}

		return nil
	})

	return docxFiles, err
}