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

// ReplaceKeywords 替换文档中的关键词
// ReplaceKeywords 替换关键词（保持向后兼容）
func (dp *DocxProcessor) ReplaceKeywords(replacements map[string]string, verbose bool) error {
	return dp.ReplaceKeywordsWithOptions(replacements, verbose, false)
}

// ReplaceKeywordsWithHashWrapper 使用井号包装的关键词替换
func (dp *DocxProcessor) ReplaceKeywordsWithHashWrapper(replacements map[string]string, verbose bool) error {
	return dp.ReplaceKeywordsWithOptions(replacements, verbose, true)
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
			// 在 oldText 前后添加井号
			searchText = "#" + oldText + "#"
		}

		// 执行增强替换
		count, err := dp.enhancedReplace(searchText, replacement, verbose)
		if err != nil {
			return fmt.Errorf("替换关键词 '%s' 失败: %v", oldText, err)
		}

		dp.replacementCount[oldText] = count

		if verbose {
			if count > 0 {
				log.Printf("替换 '%s' -> '%s' (%d次)", oldText, replacement, count)
			} else {
				log.Printf("未找到关键词 '%s'", oldText)
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

	return originalCount, nil
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
