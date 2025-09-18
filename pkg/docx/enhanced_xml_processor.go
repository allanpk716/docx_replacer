package docx

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// EnhancedXMLProcessor 增强的XML处理器，支持自定义属性追踪
type EnhancedXMLProcessor struct {
	filePath        string
	propertyManager *CustomPropertyManager
}

// NewEnhancedXMLProcessor 创建新的增强XML处理器
func NewEnhancedXMLProcessor(filePath string) *EnhancedXMLProcessor {
	propertyManager := NewCustomPropertyManager()
	return &EnhancedXMLProcessor{
		filePath:        filePath,
		propertyManager: propertyManager,
	}
}

// ReplaceKeywordsWithTracking 替换关键词并追踪到自定义属性
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
	var customPropsContent string
	var customProps *CustomProperties

	// 读取现有自定义属性
	customProps = &CustomProperties{}

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
		case "docProps/custom.xml":
			customPropsContent = string(content)
			var err error
			customProps, err = exp.propertyManager.ParseCustomProperties(customPropsContent)
			if err != nil {
				fmt.Printf("解析自定义属性失败: %v\n", err)
				// 创建默认属性结构
				customProps, _ = exp.propertyManager.ParseCustomProperties("")
			}
			// 跳过写入，稍后统一处理
			continue
		case "word/document.xml":
			documentXMLContent = string(content)

			// 执行关键词替换
			modifiedContent := documentXMLContent
			for keyword, replacement := range replacements {
				// 检查是否已经替换过
				if exp.propertyManager.HasReplaced(customProps, keyword, replacement) {
					fmt.Printf("关键词 '%s' 已经被替换过，跳过\n", keyword)
					continue
				}
				modifiedContent, replacementCount = exp.replaceKeywordWithTracking(
					modifiedContent, keyword, replacement, replacementCount, customProps)
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

	// 更新自定义属性XML
	customPropsContent, err = exp.propertyManager.GenerateCustomPropertiesXML(customProps)
	if err != nil {
		return fmt.Errorf("生成自定义属性XML失败: %v", err)
	}
	
	writer, err := zipWriter.Create("docProps/custom.xml")
	if err != nil {
		return fmt.Errorf("创建custom.xml失败: %v", err)
	}
	_, err = writer.Write([]byte(customPropsContent))
	if err != nil {
		return fmt.Errorf("写入custom.xml失败: %v", err)
	}

	fmt.Printf("文档处理完成，总共替换了 %d 个关键词\n", replacementCount)
	fmt.Printf("自定义属性追踪已启用，管理了 %d 个替换记录\n", exp.propertyManager.GetRecordCount(customProps))
	return nil
}

// replaceKeywordWithTracking 替换关键词并添加追踪信息到自定义属性
func (exp *EnhancedXMLProcessor) replaceKeywordWithTracking(content, keyword, replacement string, currentCount int, customProps *CustomProperties) (string, int) {
	replacementCount := currentCount

	// 检查是否已经替换过
	if exp.propertyManager.HasReplaced(customProps, keyword, replacement) {
		fmt.Printf("关键词 '%s' 已经被替换过，跳过\n", keyword)
		return content, replacementCount
	}

	// 检查是否存在该关键词的历史替换记录
	var targetValue string
	if lastValue := exp.propertyManager.GetLastValueFromProps(customProps, keyword); lastValue != "" {
		targetValue = lastValue
		fmt.Printf("发现自定义属性历史记录，将替换 '%s' -> '%s'\n", targetValue, replacement)
	} else {
		targetValue = keyword
		fmt.Printf("首次替换 '%s' -> '%s'\n", keyword, replacement)
	}

	// 直接替换完整的目标值
	if strings.Contains(content, targetValue) {
		count := strings.Count(content, targetValue)
		content = strings.ReplaceAll(content, targetValue, replacement)
		replacementCount += count
		fmt.Printf("完成替换 '%s' -> '%s' (%d次)\n", targetValue, replacement, count)

		// 记录替换信息到自定义属性
		exp.propertyManager.AddReplacement(customProps, keyword, "", replacement)
	} else {
		fmt.Printf("未找到目标值 '%s'，跳过替换\n", targetValue)
	}

	// 处理被XML标签分割的关键词（仅在首次替换时需要）
	if targetValue == keyword {
		var additionalCount int
		content, additionalCount = exp.handleSplitKeywordWithTracking(content, keyword, replacement, customProps)
		replacementCount += additionalCount
	}

	return content, replacementCount
}

// handleSplitKeywordWithTracking 处理被XML标签分割的关键词并追踪
func (exp *EnhancedXMLProcessor) handleSplitKeywordWithTracking(content, keyword, replacement string, customProps *CustomProperties) (string, int) {
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

			// 更新自定义属性追踪
			exp.propertyManager.AddReplacement(customProps, keyword, "", replacement)
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



// readCustomProperties 读取自定义属性XML内容
func (exp *EnhancedXMLProcessor) readCustomProperties() (string, error) {
	reader, err := zip.OpenReader(exp.filePath)
	if err != nil {
		return "", fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()

	for _, file := range reader.File {
		if file.Name == "docProps/custom.xml" {
			rc, err := file.Open()
			if err != nil {
				return "", fmt.Errorf("打开custom.xml失败: %v", err)
			}
			defer rc.Close()

			content, err := io.ReadAll(rc)
			if err != nil {
				return "", fmt.Errorf("读取custom.xml内容失败: %v", err)
			}
			return string(content), nil
		}
	}

	// 如果没有找到custom.xml文件，返回空字符串
	return "", nil
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