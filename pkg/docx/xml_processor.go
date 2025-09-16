package docx

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// XMLProcessor 基于ZIP文件结构的DOCX处理器
type XMLProcessor struct {
	filePath string
}

// NewXMLProcessor 创建新的XML处理器
func NewXMLProcessor(filePath string) *XMLProcessor {
	return &XMLProcessor{
		filePath: filePath,
	}
}

// ReplaceKeywords 替换DOCX文档中的关键词
func (xp *XMLProcessor) ReplaceKeywords(replacements map[string]string, outputPath string) error {
	// 读取DOCX文件
	reader, err := zip.OpenReader(xp.filePath)
	if err != nil {
		return fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()

	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("创建输出文件失败: %v", err)
	}
	defer outputFile.Close()

	// 创建ZIP写入器
	zipWriter := zip.NewWriter(outputFile)
	defer zipWriter.Close()

	replacementCount := 0

	// 遍历ZIP文件中的所有文件
	for _, file := range reader.File {
		// 读取文件内容
		fileReader, err := file.Open()
		if err != nil {
			return fmt.Errorf("打开文件 %s 失败: %v", file.Name, err)
		}

		content, err := io.ReadAll(fileReader)
		fileReader.Close()
		if err != nil {
			return fmt.Errorf("读取文件 %s 失败: %v", file.Name, err)
		}

		// 如果是document.xml文件，进行关键词替换
		if file.Name == "word/document.xml" {
			originalContent := string(content)
			modifiedContent := originalContent

			// 执行关键词替换
			for keyword, replacement := range replacements {
				// 处理可能被XML标签分割的关键词
				modifiedContent, replacementCount = xp.replaceKeywordInXML(modifiedContent, keyword, replacement, replacementCount)
			}

			content = []byte(modifiedContent)
			fmt.Printf("在document.xml中完成 %d 次关键词替换\n", replacementCount)
		}

		// 写入到新的ZIP文件
		writer, err := zipWriter.CreateHeader(&file.FileHeader)
		if err != nil {
			return fmt.Errorf("创建ZIP文件头失败: %v", err)
		}

		_, err = writer.Write(content)
		if err != nil {
			return fmt.Errorf("写入文件内容失败: %v", err)
		}
	}

	fmt.Printf("文档处理完成，总共替换了 %d 个关键词\n", replacementCount)
	return nil
}

// replaceKeywordInXML 在XML内容中替换关键词，处理被XML标签分割的情况
func (xp *XMLProcessor) replaceKeywordInXML(content, keyword, replacement string, currentCount int) (string, int) {
	replacementCount := currentCount
	
	// 直接替换完整的关键词
	if strings.Contains(content, keyword) {
		count := strings.Count(content, keyword)
		content = strings.ReplaceAll(content, keyword, replacement)
		replacementCount += count
		fmt.Printf("直接替换 '%s' -> '%s' (%d次)\n", keyword, replacement, count)
	}
	
	// 处理被XML标签分割的关键词
	// 例如: #产<w:t>品</w:t>名称# 或 #产品<w:br/>名称#
	content, additionalCount := xp.handleSplitKeyword(content, keyword, replacement)
	replacementCount += additionalCount
	
	return content, replacementCount
}

// handleSplitKeyword 处理被XML标签分割的关键词
func (xp *XMLProcessor) handleSplitKeyword(content, keyword, replacement string) (string, int) {
	replacementCount := 0
	
	// 移除关键词中的#符号来构建搜索模式
	cleanKeyword := strings.Trim(keyword, "#")
	if cleanKeyword == "" {
		return content, 0
	}
	
	// 构建正则表达式来匹配被XML标签分割的关键词
	// 匹配模式: #...可能的XML标签...关键词...可能的XML标签...#
	pattern := fmt.Sprintf(`#[^#]*?%s[^#]*?#`, regexp.QuoteMeta(cleanKeyword))
	re, err := regexp.Compile(pattern)
	if err != nil {
		return content, 0
	}
	
	// 查找所有匹配项
	matches := re.FindAllString(content, -1)
	for _, match := range matches {
		// 检查匹配项是否包含完整的关键词字符
		if xp.containsKeywordChars(match, cleanKeyword) {
			// 替换整个匹配项
			content = strings.Replace(content, match, replacement, 1)
			replacementCount++
			fmt.Printf("处理分割关键词 '%s' -> '%s'\n", match, replacement)
		}
	}
	
	return content, replacementCount
}

// containsKeywordChars 检查文本是否包含关键词的所有字符
func (xp *XMLProcessor) containsKeywordChars(text, keyword string) bool {
	// 移除XML标签，只保留纯文本
	cleanText := xp.removeXMLTags(text)
	
	// 检查是否包含完整的关键词
	return strings.Contains(cleanText, keyword)
}

// removeXMLTags 移除XML标签
func (xp *XMLProcessor) removeXMLTags(text string) string {
	// 简单的XML标签移除
	re := regexp.MustCompile(`<[^>]*>`)
	return re.ReplaceAllString(text, "")
}

// ExtractTextContent 提取DOCX文档的纯文本内容（用于调试）
func (xp *XMLProcessor) ExtractTextContent() (string, error) {
	reader, err := zip.OpenReader(xp.filePath)
	if err != nil {
		return "", fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()

	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			fileReader, err := file.Open()
			if err != nil {
				return "", fmt.Errorf("打开document.xml失败: %v", err)
			}
			defer fileReader.Close()

			content, err := io.ReadAll(fileReader)
			if err != nil {
				return "", fmt.Errorf("读取document.xml失败: %v", err)
			}

			// 提取文本内容
			textContent := xp.extractTextFromXML(string(content))
			return textContent, nil
		}
	}

	return "", fmt.Errorf("未找到document.xml文件")
}

// extractTextFromXML 从XML中提取文本内容
func (xp *XMLProcessor) extractTextFromXML(xmlContent string) string {
	// 使用正则表达式提取<w:t>标签中的文本
	re := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
	matches := re.FindAllStringSubmatch(xmlContent, -1)
	
	var textParts []string
	for _, match := range matches {
		if len(match) > 1 {
			textParts = append(textParts, match[1])
		}
	}
	
	return strings.Join(textParts, "")
}