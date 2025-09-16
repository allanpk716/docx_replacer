package processor

import (
	"context"
	"fmt"
	"log"

	"github.com/allanpk716/docx_replacer/internal/domain"
	"github.com/allanpk716/docx_replacer/internal/matcher"
	"github.com/allanpk716/docx_replacer/pkg/docx"
)

// documentProcessor 文档处理器实现
type documentProcessor struct {
	keywordMatcher domain.KeywordMatcher
	tableProcessor domain.TableProcessor
}

// NewDocumentProcessor 创建新的文档处理器
func NewDocumentProcessor(tableProcessor domain.TableProcessor) domain.DocumentProcessor {
	return &documentProcessor{
		keywordMatcher: matcher.NewKeywordMatcher(),
		tableProcessor: tableProcessor,
	}
}

// ProcessDocument 处理文档，替换关键词
func (dp *documentProcessor) ProcessDocument(ctx context.Context, inputPath, outputPath string, replacements map[string]string) error {
	// 验证输入参数
	if err := dp.ValidateDocument(inputPath); err != nil {
		return fmt.Errorf("文档验证失败: %w", err)
	}

	if outputPath == "" {
		return fmt.Errorf("输出路径不能为空")
	}

	if len(replacements) == 0 {
		return fmt.Errorf("替换映射不能为空")
	}

	log.Printf("开始处理文档: %s", inputPath)

	// 使用XMLProcessor直接处理DOCX文档
	xmlProcessor := docx.NewXMLProcessor(inputPath)
	if err := xmlProcessor.ReplaceKeywords(replacements, outputPath); err != nil {
		return fmt.Errorf("处理文档失败: %w", err)
	}

	log.Printf("文档处理完成，已保存到: %s", outputPath)
	return nil
}

// ValidateDocument 验证文档是否有效
func (dp *documentProcessor) ValidateDocument(inputPath string) error {
	if inputPath == "" {
		return fmt.Errorf("输入路径不能为空")
	}

	// 创建临时包装器进行验证
	tempWrapper := &docx.DocxWrapper{}
	defer tempWrapper.Close()

	if err := tempWrapper.OpenDocument(inputPath); err != nil {
		return fmt.Errorf("无法打开文档: %w", err)
	}

	return nil
}

// processParagraphs 处理段落中的关键词替换
func (dp *documentProcessor) processParagraphs(ctx context.Context, docxWrapper *docx.DocxWrapper, replacements map[string]string) error {
	select {
	case <-ctx.Done():
		return ctx.Err()
	default:
	}

	// 获取所有段落
	paragraphs := docxWrapper.GetParagraphs()

	log.Printf("处理 %d 个段落", len(paragraphs))

	// 统计替换次数
	replacementCount := 0
	for _, paragraph := range paragraphs {
		// 由于gomutex/godocx库限制，暂时跳过段落处理
		_ = paragraph
	}

	log.Printf("在段落中找到 %d 个关键词匹配", replacementCount)

	return nil
}

// processTables 处理表格中的关键词替换
func (dp *documentProcessor) processTables(ctx context.Context, docxWrapper *docx.DocxWrapper, replacements map[string]string) error {
	select {
	case <-ctx.Done():
		return ctx.Err()
	default:
	}

	// 获取所有表格
	tables := docxWrapper.GetTables()

	log.Printf("处理 %d 个表格", len(tables))

	// 统计表格中的替换次数
	tableReplacementCount := 0
	for i := range tables {
		// 获取表格单元格文本
		cellText := docxWrapper.GetTableCellText(i, 0, 0)
		if cellText == "" {
			continue // 跳过空的表格单元格
		}
		
		// 处理可能被 XML 分割的关键词
		processedText := dp.tableProcessor.HandleSplitKeywords(cellText)
		matches := dp.keywordMatcher.FindMatches(processedText, replacements)
		tableReplacementCount += len(matches)
	}

	log.Printf("在表格中找到 %d 个关键词匹配", tableReplacementCount)

	return nil
}

// GetProcessingStats 获取处理统计信息
func (dp *documentProcessor) GetProcessingStats(inputPath string, replacements map[string]string) (*domain.ProcessResult, error) {
	docxWrapper := &docx.DocxWrapper{}
	defer docxWrapper.Close()

	if err := docxWrapper.OpenDocument(inputPath); err != nil {
		return nil, fmt.Errorf("打开文档失败: %w", err)
	}

	totalReplacements := 0

	// 统计段落中的替换
	paragraphs := docxWrapper.GetParagraphs()
	for _, paragraph := range paragraphs {
		// 由于gomutex/godocx库限制，暂时跳过段落处理
		_ = paragraph
	}

	// 统计表格中的替换
	tables := docxWrapper.GetTables()
	for i := range tables {
		// 获取表格单元格文本
		cellText := docxWrapper.GetTableCellText(i, 0, 0)
		if cellText == "" {
			continue // 跳过空的表格单元格
		}
		
		processedText := dp.tableProcessor.HandleSplitKeywords(cellText)
		matches := dp.keywordMatcher.FindMatches(processedText, replacements)
		totalReplacements += len(matches)
	}

	return &domain.ProcessResult{
		Success:        true,
		ProcessedFiles: 1,
		Replacements:   totalReplacements,
		Errors:         nil,
	}, nil
}