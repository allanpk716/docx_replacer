package main

import (
	"fmt"
	"log"
	"regexp"
	"strings"

	"github.com/nguyenthenguyen/docx"
)

// DocxProcessor 处理docx文件的结构体
type DocxProcessor struct {
	reader           *docx.ReplaceDocx
	editable         *docx.Docx
	replacementCount map[string]int
}

// NewDocxProcessor 创建新的DocxProcessor实例
func NewDocxProcessor(filePath string) (*DocxProcessor, error) {
	reader, err := docx.ReadDocxFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("打开docx文件失败: %v", err)
	}

	editable := reader.Editable()

	return &DocxProcessor{
		reader:           reader,
		editable:         editable,
		replacementCount: make(map[string]int),
	}, nil
}

// ReplaceKeywordsWithOptions 带选项的关键词替换方法
func (dp *DocxProcessor) ReplaceKeywordsWithOptions(replacements map[string]string, verbose bool, useHashWrapper bool) error {
	if dp.editable == nil {
		return fmt.Errorf("文档未初始化")
	}

	// 初始化替换计数器
	dp.replacementCount = make(map[string]int)

	// 使用增强的替换方法
	for oldText, replacement := range replacements {
		searchText := oldText
		if useHashWrapper {
			// 智能处理井号包装：如果关键词已经包含井号，则不再添加
			if !strings.HasPrefix(oldText, "#") || !strings.HasSuffix(oldText, "#") {
				searchText = "#" + oldText + "#"
			}
		}

		// 执行增强替换
		count, err := dp.enhancedReplace(searchText, replacement, verbose)
		if err != nil {
			return fmt.Errorf("替换关键词 '%s' 失败: %v", oldText, err)
		}

		dp.replacementCount[oldText] = count

		if verbose {
			if count > 0 {
				log.Printf("替换 '%s' -> '%s' (%d次)", searchText, replacement, count)
			} else {
				log.Printf("未找到关键词 '%s'（搜索: '%s'）", oldText, searchText)
			}
		}
	}

	return nil
}

// enhancedReplace 增强的替换方法，能更好地处理表格等复杂结构
func (dp *DocxProcessor) enhancedReplace(oldText, replacement string, verbose bool) (int, error) {
	// 获取文档内容进行计数
	content := dp.editable.GetContent()
	originalCount := strings.Count(content, oldText)

	if originalCount == 0 {
		return 0, nil
	}

	// 方法1: 使用标准Replace方法
	err := dp.editable.Replace(oldText, replacement, -1)
	if err != nil {
		log.Printf("标准替换失败: %v", err)
	}

	// 方法2: 使用ReplaceRaw方法（直接操作XML内容）
	dp.editable.ReplaceRaw(oldText, replacement, -1)

	// 方法3: 替换页眉和页脚
	err = dp.editable.ReplaceHeader(oldText, replacement)
	if err != nil && verbose {
		log.Printf("替换页眉失败: %v", err)
	}
	err = dp.editable.ReplaceFooter(oldText, replacement)
	if err != nil && verbose {
		log.Printf("替换页脚失败: %v", err)
	}

	// 方法4: 使用正则表达式进行更深层的内容替换
	// 这个方法可以处理被XML标签分割的文本
	err = dp.regexBasedReplace(oldText, replacement)
	if err != nil && verbose {
		log.Printf("正则替换失败: %v", err)
	}

	// 验证替换效果：检查替换后还剩多少个原文本
	afterContent := dp.editable.GetContent()
	remainingCount := strings.Count(afterContent, oldText)
	actualReplacedCount := originalCount - remainingCount

	if verbose && actualReplacedCount != originalCount {
		log.Printf("警告: 期望替换 %d 次，实际替换 %d 次", originalCount, actualReplacedCount)
	}

	return actualReplacedCount, nil
}

// regexBasedReplace 基于正则表达式的替换，处理被XML标签分割的文本
func (dp *DocxProcessor) regexBasedReplace(oldText, replacement string) error {
	// 获取当前文档的完整XML内容
	content := dp.editable.GetContent()

	// 转义特殊字符
	escapedOldText := regexp.QuoteMeta(oldText)

	// 创建一个正则表达式，允许XML标签在关键词中间
	// 这个正则会匹配被<w:t>标签分割的文本
	pattern := strings.ReplaceAll(escapedOldText, "\\ ", "(?:<[^>]*>)*\\s*(?:<[^>]*>)*")
	for i := 0; i < len(oldText); i++ {
		char := string(oldText[i])
		if char != " " {
			escapedChar := regexp.QuoteMeta(char)
			pattern = strings.Replace(pattern, escapedChar, escapedChar+"(?:<[^>]*>)*", 1)
		}
	}

	re, err := regexp.Compile(pattern)
	if err != nil {
		return fmt.Errorf("编译正则表达式失败: %v", err)
	}

	// 如果找到匹配项，使用SetContent更新整个文档
	if re.MatchString(content) {
		newContent := re.ReplaceAllString(content, replacement)
		dp.editable.SetContent(newContent)
	}

	return nil
}

// SaveAs 保存文档到指定路径
func (dp *DocxProcessor) SaveAs(filePath string) error {
	if dp.editable == nil {
		return fmt.Errorf("文档未初始化")
	}

	return dp.editable.WriteToFile(filePath)
}

// Close 关闭文档
func (dp *DocxProcessor) Close() error {
	if dp.reader != nil {
		err := dp.reader.Close()
		dp.reader = nil
		dp.editable = nil
		return err
	}
	return nil
}

// GetReplacementCount 获取关键词替换次数统计
func (dp *DocxProcessor) GetReplacementCount() map[string]int {
	return dp.replacementCount
}

// DebugContent 调试方法：显示文档内容和关键词分析
func (dp *DocxProcessor) DebugContent(keywords []string) {
	if dp.editable == nil {
		log.Println("文档未初始化")
		return
	}

	content := dp.editable.GetContent()
	log.Printf("=== 文档内容调试信息 ===")
	log.Printf("文档总字符数: %d", len(content))

	// 显示前500个字符
	previewLength := 500
	if len(content) < previewLength {
		previewLength = len(content)
	}
	log.Printf("文档内容预览（前%d字符）:", previewLength)
	log.Printf("%s", content[:previewLength])

	// 检查关键词
	log.Printf("=== 关键词分析 ===")
	for _, keyword := range keywords {
		// 检查不带井号的
		plainCount := strings.Count(content, keyword)
		
		// 智能确定搜索文本（与ReplaceKeywordsWithOptions中的逻辑保持一致）
		searchText := keyword
		if !strings.HasPrefix(keyword, "#") || !strings.HasSuffix(keyword, "#") {
			searchText = "#" + keyword + "#"
		}
		searchCount := strings.Count(content, searchText)

		log.Printf("关键词 '%s': 原文=%d次, 实际搜索'%s'=%d次", keyword, plainCount, searchText, searchCount)
	}
	log.Printf("=== 调试信息结束 ===")
}
