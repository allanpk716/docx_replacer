package main

import (
	"bufio"
	"context"
	"encoding/json"
	"flag"
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
	// 设置日志级别
	log.SetFlags(log.LstdFlags)

	fmt.Printf("=== %s v%s ===\n", cmd.AppName, cmd.AppVersion)
	fmt.Println("欢迎使用 DOCX 关键词替换工具！")
	fmt.Println()

	// 解析命令行参数
	var configFile, inputPath, outputPath string
	flag.StringVar(&configFile, "config", "", "配置文件路径")
	flag.StringVar(&inputPath, "input", "", "输入文件或目录路径")
	flag.StringVar(&outputPath, "output", "", "输出文件或目录路径")
	flag.Parse()

	// 如果没有提供命令行参数，使用交互式模式
	if configFile == "" {
		// 交互式获取配置文件路径
		configFile = getConfigFilePath()
	} else {
		// 验证配置文件
		if _, err := os.Stat(configFile); os.IsNotExist(err) {
			log.Fatalf("配置文件不存在: %s", configFile)
		}
		fmt.Printf("✓ 配置文件路径: %s\n", configFile)
	}
	
	// 加载增强配置文件
	enhancedConfigManager := config.NewEnhancedConfigManager()
	enhancedCfg, err := enhancedConfigManager.LoadConfigWithMigration(configFile)
	if err != nil {
		log.Fatalf("加载配置文件失败: %v", err)
	}
	
	// 加载或创建独立的注释追踪配置文件
	if err := loadOrCreateCommentTrackingConfig(configFile, enhancedCfg); err != nil {
		log.Fatalf("处理注释追踪配置失败: %v", err)
	}

	fmt.Printf("✓ 成功加载配置文件 (项目: %s, 关键词数量: %d)\n", enhancedCfg.ProjectName, len(enhancedCfg.Keywords))
	if enhancedCfg.CommentTracking != nil && enhancedCfg.CommentTracking.EnableCommentTracking {
		fmt.Println("✓ 注释追踪功能已启用")
	}
	fmt.Println()

	// 获取启用的关键词映射
	keywordMap := enhancedConfigManager.GetEnabledKeywords(enhancedCfg)
	if len(keywordMap) == 0 {
		log.Fatalf("没有找到有效的关键词")
	}

	// 获取输入和输出路径
	var isBatchMode bool
	if inputPath == "" || outputPath == "" {
		// 交互式获取输入和输出路径
		inputPath, outputPath, isBatchMode = getInputOutputPaths()
	} else {
		// 验证输入路径
		if _, err := os.Stat(inputPath); os.IsNotExist(err) {
			log.Fatalf("输入路径不存在: %s", inputPath)
		}
		// 判断是否为批量模式
		info, err := os.Stat(inputPath)
		if err != nil {
			log.Fatalf("无法获取输入路径信息: %v", err)
		}
		isBatchMode = info.IsDir()
		fmt.Printf("✓ 输入路径: %s\n", inputPath)
		fmt.Printf("✓ 输出路径: %s\n", outputPath)
		if isBatchMode {
			fmt.Println("检测到输入目录，将进行批量处理")
		} else {
			fmt.Println("检测到输入文件，将进行单文件处理")
		}
	}

	// 创建上下文
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Minute)
	defer cancel()

	// 执行处理
	if err := executeInteractiveProcessing(ctx, inputPath, outputPath, isBatchMode, keywordMap, enhancedCfg); err != nil {
		log.Fatalf("处理失败: %v", err)
	}

	fmt.Println()
	fmt.Println("✓ 处理完成！")
	fmt.Println("按任意键退出...")
	bufio.NewReader(os.Stdin).ReadBytes('\n')
}

// getConfigFilePath 交互式获取配置文件路径
func getConfigFilePath() string {
	fmt.Println("步骤 1/3: 请拖拽配置文件 (config.json) 到此窗口，然后按回车:")
	fmt.Print("配置文件路径: ")
	
	scanner := bufio.NewScanner(os.Stdin)
	for scanner.Scan() {
		input := strings.TrimSpace(scanner.Text())
		if input == "" {
			fmt.Print("请输入配置文件路径: ")
			continue
		}
		
		// 清理路径中的引号
		input = strings.Trim(input, "\"'")
		
		// 检查文件是否存在
		if _, err := os.Stat(input); os.IsNotExist(err) {
			fmt.Printf("❌ 文件不存在: %s\n", input)
			fmt.Print("请重新输入配置文件路径: ")
			continue
		}
		
		// 检查文件扩展名
		if !strings.HasSuffix(strings.ToLower(input), ".json") {
			fmt.Printf("❌ 配置文件必须是 .json 格式\n")
			fmt.Print("请重新输入配置文件路径: ")
			continue
		}
		
		fmt.Printf("✓ 配置文件路径: %s\n", input)
		return input
	}
	
	log.Fatal("读取输入失败")
	return ""
}

// getInputOutputPaths 交互式获取输入和输出路径
func getInputOutputPaths() (string, string, bool) {
	fmt.Println()
	fmt.Println("步骤 2/3: 请拖拽要处理的文件或文件夹到此窗口，然后按回车:")
	fmt.Print("输入路径: ")
	
	scanner := bufio.NewScanner(os.Stdin)
	var inputPath string
	
	for scanner.Scan() {
		input := strings.TrimSpace(scanner.Text())
		if input == "" {
			fmt.Print("请输入要处理的文件或文件夹路径: ")
			continue
		}
		
		// 清理路径中的引号
		input = strings.Trim(input, "\"'")
		
		// 检查路径是否存在
		_, err := os.Stat(input)
		if os.IsNotExist(err) {
			fmt.Printf("❌ 路径不存在: %s\n", input)
			fmt.Print("请重新输入路径: ")
			continue
		}
		
		inputPath = input
		fmt.Printf("✓ 输入路径: %s\n", input)
		break
	}
	
	// 检查是否成功获取输入路径
	if inputPath == "" {
		log.Fatal("未能获取有效的输入路径")
	}
	
	// 判断是文件还是目录
	info, err := os.Stat(inputPath)
	if err != nil {
		log.Fatalf("无法获取路径信息: %v", err)
	}
	isBatchMode := info.IsDir()
	
	if isBatchMode {
		fmt.Printf("检测到输入目录，将进行批量处理\n")
	} else {
		fmt.Printf("检测到输入文件，将进行单文件处理\n")
	}
	
	fmt.Println()
	fmt.Println("步骤 3/3: 请拖拽输出文件夹到此窗口，然后按回车:")
	fmt.Print("输出路径: ")
	
	var outputPath string
	for scanner.Scan() {
		input := strings.TrimSpace(scanner.Text())
		if input == "" {
			fmt.Print("请输入输出路径: ")
			continue
		}
		
		// 清理路径中的引号
		input = strings.Trim(input, "\"'")
		
		// 如果是单文件模式，检查输出路径是否为文件
		if !isBatchMode {
			// 如果输出路径是目录，自动生成文件名
			if info, err := os.Stat(input); err == nil && info.IsDir() {
				filename := filepath.Base(inputPath)
				ext := filepath.Ext(filename)
				base := strings.TrimSuffix(filename, ext)
				input = filepath.Join(input, base+"_processed"+ext)
			}
		}
		
		outputPath = input
		fmt.Printf("✓ 输出路径: %s\n", input)
		break
	}
	
	// 检查是否成功获取输出路径
	if outputPath == "" {
		log.Fatal("未能获取有效的输出路径")
	}
	
	return inputPath, outputPath, isBatchMode
}

// executeInteractiveProcessing 执行交互式处理逻辑
func executeInteractiveProcessing(ctx context.Context, inputPath, outputPath string, isBatchMode bool, keywordMap map[string]string, cfg *config.EnhancedConfig) error {
	fmt.Println()
	fmt.Println("开始处理...")
	
	if isBatchMode {
		return processXMLBatchFiles(ctx, inputPath, outputPath, keywordMap, cfg)
	} else {
		return processXMLSingleFile(ctx, inputPath, outputPath, keywordMap, cfg)
	}
}

// processXMLSingleFile 使用增强XML处理器处理单个文件
func processXMLSingleFile(ctx context.Context, inputFile, outputFile string, keywordMap map[string]string, cfg *config.EnhancedConfig) error {
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

	// 询问用户选择处理模式
	useWordCompatible := askForProcessingMode()
	
	if useWordCompatible {
		// 使用Word兼容处理器
		wordProcessor := docx.NewWordCompatibleProcessor(inputFile)
		if err := wordProcessor.ReplaceKeywordsWithWordCompatibility(keywordMap, outputFile); err != nil {
			return fmt.Errorf("Word兼容处理失败: %w", err)
		}
	} else {
		// 创建增强XML处理器（使用自定义属性追踪）
		enhancedProcessor := docx.NewEnhancedXMLProcessorWithCustomProps(inputFile)
		
		// 执行替换
		if err := enhancedProcessor.ReplaceKeywordsWithTracking(keywordMap, outputFile); err != nil {
			return fmt.Errorf("处理文件失败: %w", err)
		}
	}

	log.Printf("文件处理完成: %s", outputFile)
	return nil
}

// processXMLBatchFiles 使用增强XML处理器批量处理文件
func processXMLBatchFiles(ctx context.Context, inputDir, outputDir string, keywordMap map[string]string, cfg *config.EnhancedConfig) error {
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

	// 询问用户选择处理模式
	useWordCompatible := askForProcessingMode()

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

		if useWordCompatible {
			// 使用Word兼容处理器
			wordProcessor := docx.NewWordCompatibleProcessor(inputFile)
			if err := wordProcessor.ReplaceKeywordsWithWordCompatibility(keywordMap, outputFile); err != nil {
				log.Printf("Word兼容处理失败 %s: %v", inputFile, err)
				continue
			}
		} else {
			// 创建增强XML处理器（使用自定义属性追踪）
			enhancedProcessor := docx.NewEnhancedXMLProcessorWithCustomProps(inputFile)
			
			// 执行替换
			if err := enhancedProcessor.ReplaceKeywordsWithTracking(keywordMap, outputFile); err != nil {
				log.Printf("处理文件失败 %s: %v", inputFile, err)
				continue
			}
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

// askForProcessingMode 询问用户选择处理模式
func askForProcessingMode() bool {
	fmt.Println()
	fmt.Println("请选择处理模式:")
	fmt.Println("1. 标准模式 (保持原有XML结构，可能在Word中显示异常)")
	fmt.Println("2. Word兼容模式 (重建XML结构，确保在Word中正确显示) [推荐]")
	fmt.Print("请输入选择 (1/2，默认为2): ")
	
	scanner := bufio.NewScanner(os.Stdin)
	for scanner.Scan() {
		input := strings.TrimSpace(scanner.Text())
		if input == "" || input == "2" {
			fmt.Println("✓ 已选择Word兼容模式")
			return true
		} else if input == "1" {
			fmt.Println("✓ 已选择标准模式")
			return false
		} else {
			fmt.Print("请输入有效选择 (1/2): ")
			continue
		}
	}
	
	// 默认返回Word兼容模式
	fmt.Println("✓ 默认选择Word兼容模式")
	return true
}

// loadOrCreateCommentTrackingConfig 加载或创建注释追踪配置文件
func loadOrCreateCommentTrackingConfig(configFilePath string, enhancedCfg *config.EnhancedConfig) error {
	// 获取配置文件所在目录
	configDir := filepath.Dir(configFilePath)
	commentConfigPath := filepath.Join(configDir, "docx-replacer-config.json")
	
	// 检查配置文件是否存在
	if _, err := os.Stat(commentConfigPath); os.IsNotExist(err) {
		// 文件不存在，创建默认配置
		defaultConfig := map[string]interface{}{
			"comment_tracking": map[string]interface{}{
				"enable_comment_tracking":   true,
				"comment_format":           "DOCX_REPLACER_ORIGINAL",
				"max_comment_history":      10,
				"cleanup_orphaned_comments": true,
				"auto_backup":              true,
			},
		}
		
		data, err := json.MarshalIndent(defaultConfig, "", "  ")
		if err != nil {
			return fmt.Errorf("序列化默认配置失败: %w", err)
		}
		
		if err := os.WriteFile(commentConfigPath, data, 0644); err != nil {
			return fmt.Errorf("创建配置文件失败: %w", err)
		}
		
		fmt.Printf("✓ 已创建默认注释追踪配置文件: %s\n", commentConfigPath)
	} else if err != nil {
		return fmt.Errorf("检查配置文件失败: %w", err)
	}
	
	// 读取配置文件
	data, err := os.ReadFile(commentConfigPath)
	if err != nil {
		return fmt.Errorf("读取配置文件失败: %w", err)
	}
	
	// 解析配置
	var commentConfig struct {
		CommentTracking *config.CommentTracking `json:"comment_tracking"`
	}
	
	if err := json.Unmarshal(data, &commentConfig); err != nil {
		return fmt.Errorf("解析配置文件失败: %w", err)
	}
	
	// 将配置应用到主配置中
	if commentConfig.CommentTracking != nil {
		enhancedCfg.CommentTracking = commentConfig.CommentTracking
		fmt.Printf("✓ 已加载注释追踪配置文件: %s\n", commentConfigPath)
	}
	
	return nil
}