package docx

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"

	"github.com/allanpk716/docx_replacer/internal/comment"
)

// EnhancedXMLProcessor 增强的XML处理器，支持注释追踪
type EnhancedXMLProcessor struct {
	filePath       string
	commentManager *comment.CommentManager
	enableTracking bool
}

// NewEnhancedXMLProcessor 创建新的增强XML处理器
func NewEnhancedXMLProcessor(filePath string, commentConfig *comment.CommentConfig) *EnhancedXMLProcessor {
	commentManager := comment.NewCommentManager(commentConfig)
	return &EnhancedXMLProcessor{
		filePath:       filePath,
		commentManager: commentManager,
		enableTracking: commentManager.IsEnabled(),
	}
}

// ReplaceKeywordsWithTracking 替换关键词并追踪注释
func (exp *EnhancedXMLProcessor) ReplaceKeywordsWithTracking(replacements map[string]string, outputPath string) error {
	// 读取DOCX文件
	reader, err := zip.OpenReader(exp.filePath)
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
	var documentXMLContent string
	var commentsXMLContent string

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

		// 处理不同类型的文件
		switch file.Name {
		case "word/document.xml":
			documentXMLContent = string(content)
			if exp.enableTracking {
				// 解析现有注释
				exp.commentManager.ParseComments(documentXMLContent)
			}

			// 执行关键词替换
			modifiedContent := documentXMLContent
			for keyword, replacement := range replacements {
				modifiedContent, replacementCount = exp.replaceKeywordWithTracking(
					modifiedContent, keyword, replacement, replacementCount)
			}

			// 如果启用追踪，添加注释
			if exp.enableTracking {
				modifiedContent = exp.addTrackingComments(modifiedContent, replacements)
				// 清理孤立注释
				activeKeywords := make([]string, 0, len(replacements))
				for keyword := range replacements {
					activeKeywords = append(activeKeywords, keyword)
				}
				modifiedContent = exp.commentManager.CleanupOrphanedComments(modifiedContent, activeKeywords)
			}

			content = []byte(modifiedContent)
			fmt.Printf("在document.xml中完成 %d 次关键词替换\n", replacementCount)

		case "word/comments.xml":
			commentsXMLContent = string(content)
			if exp.enableTracking {
				// 处理注释文件
				modifiedCommentsContent := exp.processCommentsXML(commentsXMLContent, replacements)
				content = []byte(modifiedCommentsContent)
			}
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

	// 如果启用追踪且不存在comments.xml，创建一个
	if exp.enableTracking && commentsXMLContent == "" {
		err = exp.createCommentsXML(zipWriter, replacements)
		if err != nil {
			return fmt.Errorf("创建comments.xml失败: %v", err)
		}
	}

	fmt.Printf("文档处理完成，总共替换了 %d 个关键词\n", replacementCount)
	if exp.enableTracking {
		fmt.Printf("注释追踪已启用，管理了 %d 个注释\n", exp.commentManager.GetCommentCount())
	}
	return nil
}

// replaceKeywordWithTracking 替换关键词并更新追踪信息
func (exp *EnhancedXMLProcessor) replaceKeywordWithTracking(content, keyword, replacement string, currentCount int) (string, int) {
	replacementCount := currentCount

	// 检查是否存在该关键词的历史替换记录
	var targetValue string
	if exp.enableTracking {
		if comment, exists := exp.commentManager.GetComment(keyword); exists {
			// 如果存在历史记录，替换上次的值而不是原始关键词
			targetValue = comment.LastValue
			fmt.Printf("发现历史替换记录，将替换 '%s' -> '%s'\n", targetValue, replacement)
		} else {
			// 如果没有历史记录，替换原始关键词
			targetValue = keyword
			fmt.Printf("首次替换 '%s' -> '%s'\n", keyword, replacement)
		}
	} else {
		// 如果未启用追踪，直接替换关键词
		targetValue = keyword
	}

	// 直接替换完整的目标值
	if strings.Contains(content, targetValue) {
		count := strings.Count(content, targetValue)
		content = strings.ReplaceAll(content, targetValue, replacement)
		replacementCount += count
		fmt.Printf("完成替换 '%s' -> '%s' (%d次)\n", targetValue, replacement, count)

		// 更新注释追踪
		if exp.enableTracking {
			exp.commentManager.AddOrUpdateComment(keyword, replacement)
		}
	}

	// 处理被XML标签分割的关键词（仅在首次替换时需要）
	if targetValue == keyword {
		var additionalCount int
		content, additionalCount = exp.handleSplitKeywordWithTracking(content, keyword, replacement)
		replacementCount += additionalCount
	}

	return content, replacementCount
}

// handleSplitKeywordWithTracking 处理被XML标签分割的关键词并追踪
func (exp *EnhancedXMLProcessor) handleSplitKeywordWithTracking(content, keyword, replacement string) (string, int) {
	replacementCount := 0

	// 移除关键词中的#符号来构建搜索模式
	cleanKeyword := strings.Trim(keyword, "#")
	if cleanKeyword == "" {
		return content, 0
	}

	// 构建正则表达式来匹配被XML标签分割的关键词
	// 例如: #产<w:t>品名</w:t><w:t>称# 应该匹配 #产品名称#
	pattern := "#"
	for _, char := range cleanKeyword {
		pattern += fmt.Sprintf("[^#]*?%s", regexp.QuoteMeta(string(char)))
	}
	pattern += "[^#]*?#"

	re, err := regexp.Compile(pattern)
	if err != nil {
		return content, 0
	}

	// 查找所有匹配项
	matches := re.FindAllString(content, -1)
	for _, match := range matches {
		// 检查匹配项是否包含完整的关键词字符
		if exp.containsKeywordChars(match, cleanKeyword) {
			// 替换整个匹配项
			content = strings.Replace(content, match, replacement, 1)
			replacementCount++
			fmt.Printf("处理分割关键词 '%s' -> '%s'\n", match, replacement)

			// 更新注释追踪
			if exp.enableTracking {
				exp.commentManager.AddOrUpdateComment(keyword, replacement)
			}
		}
	}

	return content, replacementCount
}

// containsKeywordChars 检查文本是否包含关键词的所有字符
func (exp *EnhancedXMLProcessor) containsKeywordChars(text, keyword string) bool {
	// 移除XML标签，只保留纯文本
	cleanText := exp.removeXMLTags(text)
	// 检查是否包含完整的关键词
	return strings.Contains(cleanText, keyword)
}

// removeXMLTags 移除XML标签
func (exp *EnhancedXMLProcessor) removeXMLTags(text string) string {
	re := regexp.MustCompile(`<[^>]*>`)
	return re.ReplaceAllString(text, "")
}

// addTrackingComments 添加追踪注释到XML内容
func (exp *EnhancedXMLProcessor) addTrackingComments(content string, replacements map[string]string) string {
	if !exp.enableTracking {
		return content
	}

	// 在文档开始处添加注释
	var commentLines []string
	for keyword, replacement := range replacements {
		commentXML := exp.commentManager.GenerateCommentXML(keyword, replacement)
		if commentXML != "" {
			commentLines = append(commentLines, commentXML)
		}
	}

	if len(commentLines) > 0 {
		// 在<w:document>标签后插入注释
		documentTagPattern := `(<w:document[^>]*>)`
		re, err := regexp.Compile(documentTagPattern)
		if err == nil {
			commentsBlock := strings.Join(commentLines, "\n")
			content = re.ReplaceAllString(content, "$1\n"+commentsBlock)
		}
	}

	return content
}

// processCommentsXML 处理comments.xml文件
func (exp *EnhancedXMLProcessor) processCommentsXML(commentsContent string, replacements map[string]string) string {
	// 这里可以添加对Word原生注释的处理逻辑
	// 目前返回原内容
	return commentsContent
}

// createCommentsXML 创建comments.xml文件
func (exp *EnhancedXMLProcessor) createCommentsXML(zipWriter *zip.Writer, replacements map[string]string) error {
	// 创建基本的comments.xml结构
	commentsXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:comments xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
</w:comments>`

	// 创建comments.xml文件
	writer, err := zipWriter.Create("word/comments.xml")
	if err != nil {
		return err
	}

	_, err = writer.Write([]byte(commentsXML))
	return err
}

// GetCommentManager 获取注释管理器
func (exp *EnhancedXMLProcessor) GetCommentManager() *comment.CommentManager {
	return exp.commentManager
}

// ExtractTextContent 提取DOCX文档的纯文本内容
func (exp *EnhancedXMLProcessor) ExtractTextContent() (string, error) {
	reader, err := zip.OpenReader(exp.filePath)
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
			textContent := exp.extractTextFromXML(string(content))
			return textContent, nil
		}
	}

	return "", fmt.Errorf("未找到document.xml文件")
}

// extractTextFromXML 从XML中提取文本内容
func (exp *EnhancedXMLProcessor) extractTextFromXML(xmlContent string) string {
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