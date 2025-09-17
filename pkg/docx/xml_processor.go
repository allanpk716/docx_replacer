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

// XMLProcessor 基于ZIP文件结构的DOCX处理器
type XMLProcessor struct {
	filePath       string
	commentManager *comment.CommentManager
}

// NewXMLProcessor 创建新的XML处理器
func NewXMLProcessor(filePath string) *XMLProcessor {
	// 启用注释追踪配置
	config := &comment.CommentConfig{
		EnableCommentTracking:   true,
		CleanupOrphanedComments: true,
		CommentFormat:          "DOCX_REPLACER_ORIGINAL",
		MaxCommentHistory:      10,
	}
	
	return &XMLProcessor{
		filePath:       filePath,
		commentManager: comment.NewCommentManager(config),
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
			
			// 解析文档中的隐藏注释
			err := xp.commentManager.ParseComments(originalContent)
			if err != nil {
				fmt.Printf("解析注释失败: %v\n", err)
			}
			
			modifiedContent := originalContent

			// 执行关键词替换，支持多次替换
			for keyword, replacement := range replacements {
				// 处理多次替换逻辑
				modifiedContent, replacementCount = xp.replaceKeywordWithCommentTracking(modifiedContent, keyword, replacement, replacementCount)
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

// replaceKeywordWithCommentTracking 支持注释追踪的关键词替换
func (xp *XMLProcessor) replaceKeywordWithCommentTracking(content, keyword, replacement string, currentCount int) (string, int) {
	replacementCount := currentCount
	
	// 检查是否存在该关键词的替换记录
	if comment, exists := xp.commentManager.GetComment(keyword); exists {
		// 如果找到注释记录，说明这是多次替换
		fmt.Printf("检测到多次替换: %s (上次值: %s) -> %s\n", keyword, comment.LastValue, replacement)
		
		// 替换上次的值为新值
		modifiedContent, count := xp.replaceKeywordInXML(content, comment.LastValue, replacement)
		replacementCount += count
		
		// 更新注释记录
		xp.commentManager.AddOrUpdateComment(keyword, replacement)
		content = modifiedContent
	} else {
		// 首次替换 - 先尝试直接替换关键词
		modifiedContent, count := xp.replaceKeywordInXML(content, keyword, replacement)
		replacementCount += count
		
		// 如果没有找到直接匹配，检查是否有其他关键词的值匹配当前关键词
		if count == 0 {
			// 遍历所有已知的注释，看是否有值匹配当前关键词
			comments := xp.commentManager.GetComments()
			for _, existingComment := range comments {
				if existingComment.LastValue == keyword {
					// 找到匹配，这意味着当前关键词实际上是之前替换的结果
					fmt.Printf("发现关键词匹配已替换的值: %s 匹配 %s 的值\n", keyword, existingComment.OriginalKeyword)
					modifiedContent, count = xp.replaceKeywordInXML(content, keyword, replacement)
					replacementCount += count
					// 更新原始关键词的注释记录
					xp.commentManager.AddOrUpdateComment(existingComment.OriginalKeyword, replacement)
					break
				}
			}
		}
		
		// 添加新的注释记录（仅当这是真正的首次替换）
		if count > 0 && !xp.hasExistingCommentForValue(keyword) {
			xp.commentManager.AddOrUpdateComment(keyword, replacement)
		}
		content = modifiedContent
	}
	
	// 生成并添加隐藏注释到内容中
	if replacementCount > currentCount {
		commentXML := xp.commentManager.GenerateCommentXML(keyword, replacement)
		if commentXML != "" {
			// 在文档末尾添加隐藏注释
			content = xp.addHiddenCommentToXML(content, commentXML)
		}
	}
	
	return content, replacementCount
}

// hasExistingCommentForValue 检查是否已有注释的值匹配给定关键词
func (xp *XMLProcessor) hasExistingCommentForValue(keyword string) bool {
	comments := xp.commentManager.GetComments()
	for _, comment := range comments {
		if comment.LastValue == keyword {
			return true
		}
	}
	return false
}

// replaceKeywordInXML 在XML内容中替换关键词，处理被XML标签分割的情况
func (xp *XMLProcessor) replaceKeywordInXML(content, keyword, replacement string) (string, int) {
	replacementCount := 0
	
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

// addHiddenCommentToXML 在XML文档中添加隐藏注释
func (xp *XMLProcessor) addHiddenCommentToXML(content, commentXML string) string {
	// 查找文档体的结束标签
	bodyEndPattern := `</w:body>`
	if strings.Contains(content, bodyEndPattern) {
		// 在body结束标签前插入隐藏注释
		hiddenComment := fmt.Sprintf(`<w:p><w:pPr><w:rPr><w:vanish/></w:rPr></w:pPr><w:r><w:rPr><w:vanish/></w:rPr><w:t>%s</w:t></w:r></w:p>`, commentXML)
		content = strings.Replace(content, bodyEndPattern, hiddenComment+bodyEndPattern, 1)
	}
	return content
}