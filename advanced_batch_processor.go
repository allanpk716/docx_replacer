package main

import (
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// AdvancedBatchProcessor 高级批处理器
type AdvancedBatchProcessor struct {
	config    *Config
	inputDir  string
	outputDir string
	verbose   bool
}

// NewAdvancedBatchProcessor 创建高级批处理器
func NewAdvancedBatchProcessor(config *Config, inputDir, outputDir string, verbose bool) *AdvancedBatchProcessor {
	return &AdvancedBatchProcessor{
		config:    config,
		inputDir:  inputDir,
		outputDir: outputDir,
		verbose:   verbose,
	}
}

// ProcessAll 处理所有文件
func (abp *AdvancedBatchProcessor) ProcessAll() error {
	if abp.verbose {
		log.Printf("开始高级批量处理...")
		log.Printf("输入目录: %s", abp.inputDir)
		log.Printf("输出目录: %s", abp.outputDir)
		replacementMap := abp.config.GetReplacementMap()
		log.Printf("配置的替换项数量: %d", len(replacementMap))
	}

	// 确保输出目录存在
	if err := os.MkdirAll(abp.outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出目录失败: %v", err)
	}

	// 查找所有docx文件
	docxFiles, err := abp.findDocxFiles()
	if err != nil {
		return fmt.Errorf("查找docx文件失败: %v", err)
	}

	if len(docxFiles) == 0 {
		log.Println("未找到任何docx文件")
		return nil
	}

	if abp.verbose {
		log.Printf("找到 %d 个docx文件", len(docxFiles))
	}

	// 处理统计
	var (
		processedCount = 0
		errorCount     = 0
		startTime      = time.Now()
	)

	// 逐个处理文件
	for i, inputFile := range docxFiles {
		if abp.verbose {
			log.Printf("[%d/%d] 处理文件: %s", i+1, len(docxFiles), filepath.Base(inputFile))
		}

		err := abp.processFile(inputFile)
		if err != nil {
			log.Printf("处理文件失败 %s: %v", inputFile, err)
			errorCount++
		} else {
			processedCount++
			if abp.verbose {
				log.Printf("文件处理成功: %s", filepath.Base(inputFile))
			}
		}
	}

	// 输出处理结果统计
	duration := time.Since(startTime)
	log.Printf("\n=== 高级批量处理完成 ===")
	log.Printf("总文件数: %d", len(docxFiles))
	log.Printf("成功处理: %d", processedCount)
	log.Printf("处理失败: %d", errorCount)
	log.Printf("处理时间: %v", duration)

	if errorCount > 0 {
		return fmt.Errorf("有 %d 个文件处理失败", errorCount)
	}

	return nil
}

// findDocxFiles 查找所有docx文件
func (abp *AdvancedBatchProcessor) findDocxFiles() ([]string, error) {
	var docxFiles []string

	err := filepath.Walk(abp.inputDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}

		// 跳过目录
		if info.IsDir() {
			return nil
		}

		// 检查文件扩展名
		if strings.ToLower(filepath.Ext(path)) == ".docx" {
			// 跳过临时文件和隐藏文件
			filename := filepath.Base(path)
			if !strings.HasPrefix(filename, "~$") && !strings.HasPrefix(filename, ".") {
				docxFiles = append(docxFiles, path)
			}
		}

		return nil
	})

	return docxFiles, err
}

// processFile 处理单个文件
func (abp *AdvancedBatchProcessor) processFile(inputFile string) error {
	// 创建高级处理器
	processor, err := NewAdvancedDocxProcessor(inputFile)
	if err != nil {
		return fmt.Errorf("创建高级处理器失败: %v", err)
	}
	defer processor.Close()

	// 获取替换映射
	replacementMap := abp.config.GetReplacementMap()
	
	// 如果是详细模式，显示调试信息
	if abp.verbose {
		keywords := make([]string, 0, len(replacementMap))
		for key := range replacementMap {
			keywords = append(keywords, key)
		}
		processor.DebugContent(keywords)
	}

	// 执行高级替换（不自动添加#号，因为配置文件中的关键词已经包含#号）
	err = processor.TableAwareReplace(replacementMap, abp.verbose, false)
	if err != nil {
		return fmt.Errorf("高级替换失败: %v", err)
	}

	// 生成输出文件路径
	outputFile, err := abp.generateOutputPath(inputFile)
	if err != nil {
		return fmt.Errorf("生成输出路径失败: %v", err)
	}

	// 确保输出文件的目录存在
	outputDir := filepath.Dir(outputFile)
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出文件目录失败: %v", err)
	}

	// 保存处理后的文件
	err = processor.SaveAs(outputFile)
	if err != nil {
		return fmt.Errorf("保存文件失败: %v", err)
	}

	// 如果是详细模式，显示替换统计
	if abp.verbose {
		count := processor.GetReplacementCount()
		log.Printf("替换统计:")
		for key, c := range count {
			log.Printf("  '%s': %d 次", key, c)
		}
	}

	return nil
}

// generateOutputPath 生成输出文件路径
func (abp *AdvancedBatchProcessor) generateOutputPath(inputFile string) (string, error) {
	// 计算相对于输入目录的路径
	relPath, err := filepath.Rel(abp.inputDir, inputFile)
	if err != nil {
		return "", err
	}

	// 在输出目录中创建相同的目录结构
	outputFile := filepath.Join(abp.outputDir, relPath)

	// 默认添加 _advanced 后缀
	dir := filepath.Dir(outputFile)
	filename := filepath.Base(outputFile)
	ext := filepath.Ext(filename)
	name := strings.TrimSuffix(filename, ext)
	outputFile = filepath.Join(dir, name+"_advanced"+ext)

	return outputFile, nil
}

// GetProcessingStats 获取处理统计信息
func (abp *AdvancedBatchProcessor) GetProcessingStats() map[string]interface{} {
	return map[string]interface{}{
		"processor_type": "advanced",
		"input_dir":      abp.inputDir,
		"output_dir":     abp.outputDir,
		"replacements":   len(abp.config.GetReplacementMap()),
		"verbose":        abp.verbose,
	}
}