package cmd

import (
	"context"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"

	"github.com/allanpk716/docx_replacer/internal/domain"
)

// ExecuteProcessing 执行处理逻辑
func ExecuteProcessing(ctx context.Context, docProcessor domain.DocumentProcessor, args *CommandLineArgs, keywordMap map[string]string) error {
	if args.InputFile != "" {
		// 单文件处理
		return ProcessSingleFile(ctx, docProcessor, args.InputFile, args.OutputFile, keywordMap)
	} else {
		// 批量处理
		return ProcessBatchFiles(ctx, docProcessor, args.InputDir, args.OutputDir, keywordMap)
	}
}

// ProcessSingleFile 处理单个文件
func ProcessSingleFile(ctx context.Context, docProcessor domain.DocumentProcessor, inputFile, outputFile string, keywordMap map[string]string) error {
	log.Printf("处理文件: %s -> %s", inputFile, outputFile)

	if err := docProcessor.ProcessDocument(ctx, inputFile, outputFile, keywordMap); err != nil {
		return fmt.Errorf("处理文件失败: %w", err)
	}

	log.Printf("文件处理完成: %s", outputFile)
	return nil
}

// ProcessBatchFiles 批量处理文件
func ProcessBatchFiles(ctx context.Context, docProcessor domain.DocumentProcessor, inputDir, outputDir string, keywordMap map[string]string) error {
	// 创建输出目录
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		return fmt.Errorf("创建输出目录失败: %w", err)
	}

	// 查找所有 DOCX 文件
	docxFiles, err := FindDocxFiles(inputDir)
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

		if err := docProcessor.ProcessDocument(ctx, inputFile, outputFile, keywordMap); err != nil {
			log.Printf("处理文件失败 %s: %v", inputFile, err)
			continue
		}

		log.Printf("文件处理完成: %s", outputFile)
	}

	log.Printf("批量处理完成，共处理 %d 个文件", len(docxFiles))
	return nil
}

// FindDocxFiles 查找目录中的所有 DOCX 文件
func FindDocxFiles(dir string) ([]string, error) {
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