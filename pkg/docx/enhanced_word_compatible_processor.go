package docx

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// EnhancedWordCompatibleProcessor 增强的Word兼容处理器
type EnhancedWordCompatibleProcessor struct {
	replacements map[string]string
}

// NewEnhancedWordCompatibleProcessor 创建新的增强Word兼容处理器
func NewEnhancedWordCompatibleProcessor(replacements map[string]string) *EnhancedWordCompatibleProcessor {
	return &EnhancedWordCompatibleProcessor{
		replacements: replacements,
	}
}

// ReplaceKeywordsWithWordCompatibility 使用Word兼容模式替换关键词
func (p *EnhancedWordCompatibleProcessor) ReplaceKeywordsWithWordCompatibility(inputPath, outputPath string) error {
	fmt.Printf("开始处理文件: %s\n", inputPath)
	fmt.Println("使用增强Word兼容模式...")

	// 打开输入文件
	reader, err := zip.OpenReader(inputPath)
	if err != nil {
		return fmt.Errorf("打开输入文件失败: %v", err)
	}
	defer reader.Close()

	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("创建输出文件失败: %v", err)
	}
	defer outputFile.Close()

	// 创建zip写入器
	writer := zip.NewWriter(outputFile)
	defer writer.Close()

	// 处理每个文件
	for _, file := range reader.File {
		if err := p.processFile(file, writer); err != nil {
			return fmt.Errorf("处理文件 %s 失败: %v", file.Name, err)
		}
	}

	fmt.Printf("文件处理完成: %s\n", outputPath)
	return nil
}

// processFile 处理单个文件
func (p *EnhancedWordCompatibleProcessor) processFile(file *zip.File, writer *zip.Writer) error {
	// 创建目标文件，保持原始文件信息
	header := &zip.FileHeader{
		Name:     file.Name,
		Method:   file.Method,
		Modified: file.Modified,
	}

	// 打开源文件
	src, err := file.Open()
	if err != nil {
		return err
	}
	defer src.Close()

	// 创建目标文件
	dst, err := writer.CreateHeader(header)
	if err != nil {
		return err
	}

	// 如果是document.xml，进行精确的文本替换
	if file.Name == "word/document.xml" {
		fmt.Println("处理document.xml...")
		return p.replaceInDocumentXML(src, dst)
	}

	// 其他文件直接复制，保持原始状态
	_, err = io.Copy(dst, src)
	return err
}

// replaceInDocumentXML 在document.xml中进行精确替换
func (p *EnhancedWordCompatibleProcessor) replaceInDocumentXML(src io.Reader, dst io.Writer) error {
	// 读取整个文件内容
	content, err := io.ReadAll(src)
	if err != nil {
		return err
	}

	xmlContent := string(content)

	// 第一步：重组被分割的关键词
	xmlContent = p.reconstructSplitKeywords(xmlContent)

	// 第二步：使用最精确的正则表达式匹配<w:t>标签内的内容
	// 支持带属性的<w:t>标签，如 <w:t xml:space="preserve">
	textTagRegex := regexp.MustCompile(`(<w:t(?:\s[^>]*)?>)([^<]*)(</w:t>)`)

	replacementCount := 0

	// 替换函数
	replaceFunc := func(match string) string {
		submatches := textTagRegex.FindStringSubmatch(match)
		if len(submatches) != 4 {
			return match // 如果匹配不正确，返回原始内容
		}

		openingTag := submatches[1]
		textContent := submatches[2]
		closingTag := submatches[3]

		// 对文本内容进行替换
		replacedText := p.replaceTextContent(textContent)

		// 如果文本发生了变化，记录替换并确保XML安全
		if replacedText != textContent {
			replacementCount++
			fmt.Printf("替换 #%d: '%s' -> '%s'\n", replacementCount, textContent, replacedText)
			
			// 确保替换后的文本是XML安全的
			replacedText = p.ensureXMLSafe(replacedText)
		}

		return openingTag + replacedText + closingTag
	}

	// 执行替换
	replacedContent := textTagRegex.ReplaceAllStringFunc(xmlContent, replaceFunc)

	fmt.Printf("完成 %d 次文本替换\n", replacementCount)

	// 写入结果，保持原始字节格式
	_, err = dst.Write([]byte(replacedContent))
	return err
}

// replaceTextContent 替换文本内容
func (p *EnhancedWordCompatibleProcessor) replaceTextContent(text string) string {
	replacedText := text
	for oldText, newText := range p.replacements {
		// 跳过空关键词，避免错误替换
		if oldText == "" {
			continue
		}
		// 使用精确匹配替换
		if strings.Contains(replacedText, oldText) {
			// 直接替换为新值，不保留原关键词
			replacedText = strings.ReplaceAll(replacedText, oldText, newText)
		}
	}
	return replacedText
}

// ensureXMLSafe 确保文本是XML安全的
func (p *EnhancedWordCompatibleProcessor) ensureXMLSafe(text string) string {
	// 只转义必要的XML特殊字符，保持最小干预
	replacer := strings.NewReplacer(
		"&", "&amp;",   // 必须首先处理&
		"<", "&lt;",    // 转义小于号
		">", "&gt;",    // 转义大于号
	)
	return replacer.Replace(text)
}

// ValidateXMLStructure 验证XML结构（可选的验证步骤）
func (p *EnhancedWordCompatibleProcessor) ValidateXMLStructure(xmlContent string) []string {
	var issues []string

	// 检查关键标签的基本匹配
	criticalTags := []string{"w:document", "w:body"}

	for _, tag := range criticalTags {
		openPattern := fmt.Sprintf(`<%s[^>]*>`, tag)
		closePattern := fmt.Sprintf(`</%s>`, tag)

		openRegex := regexp.MustCompile(openPattern)
		closeRegex := regexp.MustCompile(closePattern)

		openCount := len(openRegex.FindAllString(xmlContent, -1))
		closeCount := len(closeRegex.FindAllString(xmlContent, -1))

		if openCount != closeCount {
			issues = append(issues, fmt.Sprintf("标签 %s 不匹配: 开始 %d, 结束 %d", tag, openCount, closeCount))
		}
	}

	return issues
}

// reconstructSplitKeywords 重组被XML标签分割的关键词
func (p *EnhancedWordCompatibleProcessor) reconstructSplitKeywords(xmlContent string) string {
	// 查找所有关键词模式，处理被分割的情况
	for keyword := range p.replacements {
		if !strings.HasPrefix(keyword, "#") || !strings.HasSuffix(keyword, "#") {
			continue // 只处理以#开头和结尾的关键词
		}

		// 构建更灵活的匹配模式，能够处理关键词被完全分割的情况
		// 匹配从第一个#开始到最后一个#结束的整个区域
		pattern := fmt.Sprintf(
			`<w:t[^>]*>#</w:t>[\s\S]*?<w:t[^>]*>#</w:t>`,
		)
		splitRegex := regexp.MustCompile(pattern)
		
		// 查找所有可能的分割区域
		matches := splitRegex.FindAllString(xmlContent, -1)
		for _, match := range matches {
			// 提取匹配区域中的所有文本内容
			textRegex := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
			textMatches := textRegex.FindAllStringSubmatch(match, -1)
			
			// 拼接所有文本内容
			var fullText strings.Builder
			for _, textMatch := range textMatches {
				if len(textMatch) > 1 {
					fullText.WriteString(textMatch[1])
				}
			}
			
			// 检查拼接后的文本是否包含我们要找的关键词
			if fullText.String() == keyword {
				// 找到匹配的分割关键词，进行重组
				// 提取第一个<w:t>标签的格式
				firstTagRegex := regexp.MustCompile(`<w:t[^>]*>`)
				firstTagMatch := firstTagRegex.FindString(match)
				
				if firstTagMatch != "" {
					// 创建新的完整标签
					newTag := firstTagMatch + keyword + "</w:t>"
					
					fmt.Printf("重组分割关键词: 找到匹配 '%s'\n", keyword)
					fmt.Printf("  -> 替换为: %s\n", newTag)
					
					// 执行替换
					xmlContent = strings.ReplaceAll(xmlContent, match, newTag)
				}
			}
		}
	}

	return xmlContent
}

// GetProcessorInfo 获取处理器信息
func (p *EnhancedWordCompatibleProcessor) GetProcessorInfo() string {
	return "Enhanced Word Compatible Processor v2.1 - 支持跨标签关键词重组，确保Word完全兼容"
}