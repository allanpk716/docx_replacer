package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

// BatchProcessor 批量处理器
type BatchProcessor struct {
	config       *Config
	inputFolder  string
	outputFolder string
	verbose      bool
}

// NewBatchProcessor 创建新的批量处理器
func NewBatchProcessor(config *Config, inputFolder, outputFolder string, verbose bool) *BatchProcessor {
	return &BatchProcessor{
		config:       config,
		inputFolder:  inputFolder,
		outputFolder: outputFolder,
		verbose:      verbose,
	}
}

// FindDocxFiles 在指定文件夹中查找所有docx文件
func (bp *BatchProcessor) FindDocxFiles(folderPath string) ([]string, error) {
	var docxFiles []string

	err := filepath.Walk(folderPath, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}

		// 检查是否为docx文件
		if !info.IsDir() && strings.ToLower(filepath.Ext(path)) == ".docx" {
			// 排除临时文件（以~$开头的文件）
			if !strings.HasPrefix(info.Name(), "~$") {
				docxFiles = append(docxFiles, path)
			}
		}

		return nil
	})

	if err != nil {
		return nil, fmt.Errorf("扫描文件夹失败: %w", err)
	}

	return docxFiles, nil
}

// ProcessBatch 批量处理文件夹中的所有docx文件
func (bp *BatchProcessor) ProcessBatch() error {
	// 检查输入文件夹是否存在
	if _, err := os.Stat(bp.inputFolder); os.IsNotExist(err) {
		return fmt.Errorf("输入文件夹不存在: %s", bp.inputFolder)
	}

	// 创建输出文件夹（如果不存在）
	if err := os.MkdirAll(bp.outputFolder, 0755); err != nil {
		return fmt.Errorf("创建输出文件夹失败: %w", err)
	}

	// 查找所有docx文件
	docxFiles, err := bp.FindDocxFiles(bp.inputFolder)
	if err != nil {
		return err
	}

	if len(docxFiles) == 0 {
		fmt.Printf("在文件夹 %s 中未找到任何docx文件\n", bp.inputFolder)
		return nil
	}

	if bp.verbose {
		fmt.Printf("找到 %d 个docx文件:\n", len(docxFiles))
		for _, file := range docxFiles {
			fmt.Printf("  %s\n", file)
		}
		fmt.Println()
	}

	// 获取替换映射
	replacementMap := bp.config.GetReplacementMap()

	// 处理每个文件
	successCount := 0
	for i, inputFile := range docxFiles {
		if bp.verbose {
			fmt.Printf("[%d/%d] 处理文件: %s\n", i+1, len(docxFiles), inputFile)
		}

		// 生成输出文件路径
		outputFile, err := bp.generateOutputPath(inputFile)
		if err != nil {
			fmt.Printf("生成输出路径失败: %v\n", err)
			continue
		}

		// 处理单个文件
		if err := bp.processSingleFile(inputFile, outputFile, replacementMap); err != nil {
			fmt.Printf("处理文件失败 %s: %v\n", inputFile, err)
			continue
		}

		successCount++
		if bp.verbose {
			fmt.Printf("  -> 已保存到: %s\n", outputFile)
		}
	}

	fmt.Printf("\n批量处理完成! 成功处理 %d/%d 个文件\n", successCount, len(docxFiles))
	return nil
}

// generateOutputPath 生成输出文件路径
func (bp *BatchProcessor) generateOutputPath(inputFile string) (string, error) {
	// 获取相对于输入文件夹的路径
	relPath, err := filepath.Rel(bp.inputFolder, inputFile)
	if err != nil {
		return "", fmt.Errorf("计算相对路径失败: %w", err)
	}

	// 生成输出文件名（添加"_替换后"后缀）
	dir := filepath.Dir(relPath)
	filename := filepath.Base(relPath)
	ext := filepath.Ext(filename)
	nameWithoutExt := strings.TrimSuffix(filename, ext)
	newFilename := nameWithoutExt + "_替换后" + ext

	// 构建完整的输出路径
	outputPath := filepath.Join(bp.outputFolder, dir, newFilename)

	// 确保输出目录存在
	outputDir := filepath.Dir(outputPath)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return "", fmt.Errorf("创建输出目录失败: %w", err)
	}

	return outputPath, nil
}

// processSingleFile 处理单个文件
func (bp *BatchProcessor) processSingleFile(inputFile, outputFile string, replacementMap map[string]string) error {
	// 打开docx文件
	processor, err := NewDocxProcessor(inputFile)
	if err != nil {
		return fmt.Errorf("打开文档失败: %w", err)
	}
	defer processor.Close()

	// 调试：显示文档内容和关键词分析
	if bp.verbose {
		keywords := make([]string, 0, len(replacementMap))
		for keyword := range replacementMap {
			keywords = append(keywords, keyword)
		}
		processor.DebugContent(keywords)
	}

	// 执行替换
	if err := processor.ReplaceKeywordsWithOptions(replacementMap, bp.verbose, true); err != nil {
		return fmt.Errorf("替换关键词失败: %w", err)
	}

	// 保存文档
	if err := processor.SaveAs(outputFile); err != nil {
		return fmt.Errorf("保存文档失败: %w", err)
	}

	return nil
}
