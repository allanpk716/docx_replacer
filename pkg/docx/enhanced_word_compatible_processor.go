package docx

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// min 返回两个整数中的较小值
func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}

// EnhancedWordCompatibleProcessor 增强的Word兼容处理器
type EnhancedWordCompatibleProcessor struct {
	replacements map[string]string
	customPropertyManager *CustomPropertyManager
	customProperties *CustomProperties
}

// NewEnhancedWordCompatibleProcessor 创建新的增强Word兼容处理器
func NewEnhancedWordCompatibleProcessor(replacements map[string]string) *EnhancedWordCompatibleProcessor {
	return &EnhancedWordCompatibleProcessor{
		replacements: replacements,
		customPropertyManager: NewCustomPropertyManager(),
		customProperties: nil,
	}
}

// ProcessDocument 处理Word文档
func (p *EnhancedWordCompatibleProcessor) ProcessDocument(inputPath, outputPath string, replacements map[string]string) error {
	p.replacements = replacements

	// 打开输入文件
	reader, err := zip.OpenReader(inputPath)
	if err != nil {
		return fmt.Errorf("打开输入文件失败: %v", err)
	}
	defer reader.Close()

	// 预加载自定义属性
	err = p.preloadCustomProperties(reader)
	if err != nil {
		fmt.Printf("警告：预加载自定义属性失败: %v\n", err)
	}

	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("创建输出文件失败: %v", err)
	}
	defer outputFile.Close()

	// 创建zip写入器
	writer := zip.NewWriter(outputFile)
	defer writer.Close()

	// 检查是否存在自定义属性文件
	hasCustomPropsFile := false
	for _, file := range reader.File {
		if file.Name == "docProps/custom.xml" {
			hasCustomPropsFile = true
			break
		}
	}

	// 处理每个文件
	for _, file := range reader.File {
		if err := p.processFile(file, writer); err != nil {
			return fmt.Errorf("处理文件 %s 失败: %v", file.Name, err)
		}
	}

	fmt.Printf("[DEBUG] 检查自定义属性文件创建条件：hasCustomPropsFile=%v, customProperties!=nil=%v\n", hasCustomPropsFile, p.customProperties != nil)
	if p.customProperties != nil {
		fmt.Printf("[DEBUG] 自定义属性对象存在，属性数量：%d\n", len(p.customProperties.Properties))
	}
	
	// 如果原文档没有自定义属性文件，但我们有自定义属性对象，需要创建一个
	if !hasCustomPropsFile && p.customProperties != nil {
		fmt.Printf("[DEBUG] 原文档没有自定义属性文件，创建新的自定义属性文件...（当前属性数量：%d）\n", len(p.customProperties.Properties))
		err = p.createCustomPropertiesFile(writer)
		if err != nil {
			fmt.Printf("警告：创建自定义属性文件失败: %v\n", err)
		} else {
			fmt.Println("[DEBUG] 自定义属性文件创建成功")
		}
	} else {
		fmt.Printf("[DEBUG] 不满足创建自定义属性文件的条件\n")
	}

	fmt.Printf("自定义属性追踪已启用，替换历史将保存到文档属性中\n")
	fmt.Printf("文件处理完成: %s\n", outputPath)
	return nil
}

// ReplaceKeywordsWithWordCompatibility 使用Word兼容模式替换关键词
func (p *EnhancedWordCompatibleProcessor) ReplaceKeywordsWithWordCompatibility(inputPath, outputPath string) error {
	return p.ProcessDocument(inputPath, outputPath, p.replacements)
}

// preloadCustomProperties 预加载自定义属性
func (p *EnhancedWordCompatibleProcessor) preloadCustomProperties(reader *zip.ReadCloser) error {
	// 查找自定义属性文件
	for _, file := range reader.File {
		if file.Name == "docProps/custom.xml" {
			// 打开文件
			src, err := file.Open()
			if err != nil {
				return fmt.Errorf("打开自定义属性文件失败: %v", err)
			}
			defer src.Close()

			// 读取内容
			content, err := io.ReadAll(src)
			if err != nil {
				return fmt.Errorf("读取自定义属性文件失败: %v", err)
			}

			fmt.Printf("[DEBUG] 自定义属性文件内容长度: %d\n", len(content))
			fmt.Printf("[DEBUG] 自定义属性文件内容前500字符: %s\n", string(content[:min(len(content), 500)]))

			// 解析自定义属性
			if len(content) > 0 {
				customProps, err := p.customPropertyManager.ParseCustomProperties(string(content))
				if err != nil {
					fmt.Printf("警告：解析现有自定义属性失败: %v，将创建新的属性文件\n", err)
					customProps = &CustomProperties{
						Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
						VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
						Properties:  []CustomProperty{},
					}
				}
				p.customProperties = customProps
				fmt.Printf("[DEBUG] 预加载自定义属性成功，包含 %d 个属性\n", len(customProps.Properties))
				for i, prop := range customProps.Properties {
					fmt.Printf("[DEBUG] 属性[%d]: Name=%s, Value=%s\n", i, prop.Name, prop.Value)
				}
				
				// 检查替换历史
				history, err := p.customPropertyManager.GetReplacementHistory(p.customProperties)
				if err != nil {
					fmt.Printf("[DEBUG] 获取替换历史失败: %v\n", err)
				} else {
					fmt.Printf("[DEBUG] 发现 %d 条替换历史记录\n", len(history.Records))
					for i, record := range history.Records {
						fmt.Printf("[DEBUG] 历史记录[%d]: Keyword=%s, Original=%s, Replacement=%s\n", i, record.Keyword, record.Original, record.Replacement)
					}
				}
			} else {
				// 如果文件为空，创建默认结构
				p.customProperties = &CustomProperties{
					Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
					VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
					Properties:  []CustomProperty{},
				}
				fmt.Printf("[DEBUG] 创建默认自定义属性结构\n")
			}
			return nil
		}
	}

	// 如果没有找到自定义属性文件，创建默认结构
	p.customProperties = &CustomProperties{
		Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
		VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
		Properties:  []CustomProperty{},
	}
	fmt.Printf("[DEBUG] 未找到自定义属性文件，创建默认结构\n")
	return nil
}

// processFile 处理单个文件
func (p *EnhancedWordCompatibleProcessor) processFile(file *zip.File, writer *zip.Writer) error {
	switch file.Name {
	case "word/document.xml":
		return p.processDocumentXML(file, writer)
	case "docProps/custom.xml":
		// 打开原始自定义属性文件
		src, err := file.Open()
		if err != nil {
			return fmt.Errorf("打开自定义属性文件失败: %v", err)
		}
		defer src.Close()
		
		dst, err := writer.Create(file.Name)
		if err != nil {
			return err
		}
		fmt.Printf("[DEBUG] 处理现有的自定义属性文件\n")
		return p.processCustomPropertiesXML(src, dst)
	case "[Content_Types].xml":
		return p.processContentTypesXML(file, writer)
	case "_rels/.rels":
		return p.processRelsXML(file, writer)
	default:
		// 复制其他文件
		return p.copyFile(file, writer)
	}
}

// replaceInDocumentXML 在document.xml中进行精确替换
func (p *EnhancedWordCompatibleProcessor) replaceInDocumentXML(src io.Reader, dst io.Writer) error {
	// 读取整个文件内容
	content, err := io.ReadAll(src)
	if err != nil {
		return err
	}

	xmlContent := string(content)

	// 确保自定义属性已初始化
	if p.customProperties == nil {
		p.customProperties = &CustomProperties{
			Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
			VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
			Properties:  []CustomProperty{},
		}
		fmt.Printf("[DEBUG] 在replaceInDocumentXML中初始化自定义属性\n")
	}
	
	// 读取现有的自定义属性（如果存在）
	customProps := p.customProperties

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

		// 对文本内容进行替换，传递自定义属性
		replacedText := p.replaceTextContent(textContent, customProps)

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
	fmt.Printf("自定义属性追踪已启用，替换历史将保存到文档属性中\n")

	// 写入结果，保持原始字节格式
	_, err = dst.Write([]byte(replacedContent))
	return err
}

// replaceTextContent 替换文本内容，支持自定义属性追踪
func (p *EnhancedWordCompatibleProcessor) replaceTextContent(text string, customProps *CustomProperties) string {
	replacedText := text
	
	// 首先检查是否有历史记录，如果有，需要先恢复原始关键词
	history, err := p.customPropertyManager.GetReplacementHistory(customProps)
	if err == nil && len(history.Records) > 0 {
		fmt.Printf("[DEBUG] 发现 %d 条替换历史记录\n", len(history.Records))
		// 先恢复所有已替换的关键词到原始状态
		for _, record := range history.Records {
			if strings.Contains(replacedText, record.Replacement) {
				fmt.Printf("[DEBUG] 恢复关键词: '%s' -> '%s'\n", record.Replacement, record.Original)
				replacedText = strings.ReplaceAll(replacedText, record.Replacement, record.Original)
			}
		}
	}
	
	for oldText, newText := range p.replacements {
		// 跳过空关键词，避免错误替换
		if oldText == "" {
			continue
		}
		
		// 构造带#号的关键词（用于匹配文档中的实际关键词）
		// 如果关键词已经包含#号，则直接使用；否则添加#号
		var keywordWithHash string
		if strings.HasPrefix(oldText, "#") && strings.HasSuffix(oldText, "#") {
			keywordWithHash = oldText
		} else {
			keywordWithHash = "#" + strings.TrimSpace(oldText) + "#"
		}
		
		fmt.Printf("[DEBUG] 处理关键词: '%s' -> '%s'\n", oldText, keywordWithHash)
		
		// 首先尝试匹配带#号的关键词
		if strings.Contains(replacedText, keywordWithHash) {
			fmt.Printf("[DEBUG] 找到关键词进行替换: '%s' -> '%s'\n", keywordWithHash, newText)
			
			// 获取原始关键词（从替换历史记录中获取）
			var originalKeyword string
			record, err := p.customPropertyManager.GetReplacementByKeyword(customProps, keywordWithHash)
			if err == nil && record != nil {
				// 如果找到历史记录，使用原始值
				originalKeyword = record.Original
				fmt.Printf("[DEBUG] 使用历史记录中的原始值: '%s'\n", originalKeyword)
			} else {
				// 如果没有历史记录，使用带#号的关键词作为原始值
				originalKeyword = keywordWithHash
				fmt.Printf("[DEBUG] 使用当前值作为原始值: '%s'\n", originalKeyword)
			}
			
			// 记录替换到自定义属性（使用原始关键词作为key）
			err = p.customPropertyManager.AddReplacementRecord(customProps, originalKeyword, originalKeyword, newText)
			if err != nil {
				fmt.Printf("警告：添加替换记录失败: %v\n", err)
			} else {
				fmt.Printf("[DEBUG] 记录替换历史: '%s' -> '%s'\n", originalKeyword, newText)
			}
			
			// 执行替换
			replacedText = strings.ReplaceAll(replacedText, keywordWithHash, newText)
		} else {
			// 如果没有找到带#号的关键词，尝试通过替换历史记录查找
			// 检查是否有以这个关键词为原始值的替换记录
			// 先尝试用关键词查找
			fmt.Printf("[DEBUG] 尝试通过替换历史查找关键词: '%s'\n", keywordWithHash)
			record, err := p.customPropertyManager.GetReplacementByKeyword(customProps, keywordWithHash)
			if err != nil || record == nil {
				// 如果用关键词找不到，尝试用原始值查找（去掉#号）
				originalKey := strings.Trim(keywordWithHash, "#")
				fmt.Printf("[DEBUG] 尝试用原始关键词查找: '%s'\n", originalKey)
				record, err = p.customPropertyManager.GetReplacementByOriginal(customProps, originalKey)
			}
			if err == nil && record != nil {
				// 找到了替换历史，说明这个关键词已经被替换过了
				fmt.Printf("[DEBUG] 找到替换历史记录: 原始='%s', 当前替换值='%s'\n", record.Original, record.Replacement)
				// 现在需要替换当前的替换值
				if strings.Contains(replacedText, record.Replacement) {
					fmt.Printf("[DEBUG] 通过替换历史找到关键词: '%s' (当前值: '%s') -> '%s'\n", keywordWithHash, record.Replacement, newText)
					
					// 更新替换记录（保持原始关键词不变，只更新替换值）
					err = p.customPropertyManager.AddReplacementRecord(customProps, record.Keyword, record.Original, newText)
					if err != nil {
						fmt.Printf("警告：更新替换记录失败: %v\n", err)
					} else {
						fmt.Printf("[DEBUG] 更新替换历史: '%s' (原始: '%s') -> '%s'\n", record.Keyword, record.Original, newText)
					}
					
					// 执行替换（替换当前的替换值）
					replacedText = strings.ReplaceAll(replacedText, record.Replacement, newText)
				} else {
					fmt.Printf("[DEBUG] 替换历史中的值未在文本中找到: '%s'\n", record.Replacement)
					fmt.Printf("[DEBUG] 当前文本片段: '%s'\n", replacedText[:min(len(replacedText), 200)])
				}
			} else {
				if err != nil {
					fmt.Printf("[DEBUG] 查找替换历史时出错: %v\n", err)
				} else {
					fmt.Printf("[DEBUG] 未找到关键词的替换历史: '%s'\n", keywordWithHash)
				}
			}
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
	return "Enhanced Word Compatible Processor v2.2 - 支持跨标签关键词重组和自定义属性追踪，确保Word完全兼容"
}

// readCustomProperties 读取自定义属性
func (p *EnhancedWordCompatibleProcessor) readCustomProperties() *CustomProperties {
	if p.customProperties == nil {
		// 创建默认的自定义属性结构，实际的加载会在processCustomPropertiesXML中进行
		p.customProperties = &CustomProperties{
			Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
			VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
			Properties:  []CustomProperty{},
		}
		fmt.Printf("[DEBUG] 创建默认自定义属性结构\n")
	}
	return p.customProperties
}

// processCustomPropertiesXML 处理自定义属性XML文件
func (p *EnhancedWordCompatibleProcessor) processCustomPropertiesXML(src io.Reader, dst io.Writer) error {
	// 自定义属性已经在预加载阶段处理了，这里直接生成更新后的XML
	if p.customProperties == nil {
		// 如果预加载失败，创建默认结构
		p.customProperties = &CustomProperties{
			Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
			VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
			Properties:  []CustomProperty{},
		}
		fmt.Printf("[DEBUG] 预加载失败，创建默认自定义属性结构\n")
	} else {
		fmt.Printf("[DEBUG] 使用预加载的自定义属性，包含 %d 个属性\n", len(p.customProperties.Properties))
		for i, prop := range p.customProperties.Properties {
			fmt.Printf("[DEBUG] 保存前属性[%d]: Name=%s, Value=%s\n", i, prop.Name, prop.Value)
		}
	}

	// 生成更新后的自定义属性XML
	updatedXML, err := p.customPropertyManager.GenerateCustomPropertiesXML(p.customProperties)
	if err != nil {
		return fmt.Errorf("生成自定义属性XML失败: %v", err)
	}

	fmt.Printf("[DEBUG] 保存自定义属性XML，长度: %d\n", len(updatedXML))

	// 写入更新后的内容
	_, err = dst.Write([]byte(updatedXML))
	return err
}

// createCustomPropertiesFile 创建新的自定义属性文件
func (p *EnhancedWordCompatibleProcessor) createCustomPropertiesFile(writer *zip.Writer) error {
	// 创建自定义属性文件
	header := &zip.FileHeader{
		Name:   "docProps/custom.xml",
		Method: zip.Deflate,
	}

	// 创建文件
	dst, err := writer.CreateHeader(header)
	if err != nil {
		return err
	}

	// 生成自定义属性XML
	updatedXML, err := p.customPropertyManager.GenerateCustomPropertiesXML(p.customProperties)
	if err != nil {
		return fmt.Errorf("生成自定义属性XML失败: %v", err)
	}

	fmt.Printf("[DEBUG] 创建新的自定义属性文件，长度: %d\n", len(updatedXML))

	// 写入内容
	_, err = dst.Write([]byte(updatedXML))
	return err
}

// processContentTypesXML 处理[Content_Types].xml文件
func (p *EnhancedWordCompatibleProcessor) processContentTypesXML(file *zip.File, writer *zip.Writer) error {
	// 读取原始内容
	src, err := file.Open()
	if err != nil {
		return err
	}
	defer src.Close()

	content, err := io.ReadAll(src)
	if err != nil {
		return err
	}

	contentStr := string(content)

	// 检查是否已经包含自定义属性的内容类型
	if !strings.Contains(contentStr, "docProps/custom.xml") {
		// 添加自定义属性的内容类型
		customPropsOverride := `  <Override PartName="/docProps/custom.xml" ContentType="application/vnd.openxmlformats-officedocument.custom-properties+xml"/>`
		
		// 在</Types>之前插入
		contentStr = strings.Replace(contentStr, "</Types>", customPropsOverride+"\n</Types>", 1)
		fmt.Printf("[DEBUG] 添加自定义属性到Content Types\n")
	}

	// 创建输出文件
	header := &zip.FileHeader{
		Name:     file.Name,
		Method:   file.Method,
		Modified: file.Modified,
	}

	dst, err := writer.CreateHeader(header)
	if err != nil {
		return err
	}

	// 写入更新后的内容
	_, err = dst.Write([]byte(contentStr))
	return err
}

// processRelsXML 处理_rels/.rels文件
func (p *EnhancedWordCompatibleProcessor) processRelsXML(file *zip.File, writer *zip.Writer) error {
	// 读取原始内容
	src, err := file.Open()
	if err != nil {
		return err
	}
	defer src.Close()

	content, err := io.ReadAll(src)
	if err != nil {
		return err
	}

	contentStr := string(content)

	// 检查是否已经包含自定义属性的关系
	if !strings.Contains(contentStr, "docProps/custom.xml") {
		// 添加自定义属性的关系
		customPropsRel := `  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/custom-properties" Target="docProps/custom.xml"/>`
		
		// 在</Relationships>之前插入
		contentStr = strings.Replace(contentStr, "</Relationships>", customPropsRel+"\n</Relationships>", 1)
		fmt.Printf("[DEBUG] 添加自定义属性关系到_rels/.rels\n")
	}

	// 创建输出文件
	header := &zip.FileHeader{
		Name:     file.Name,
		Method:   file.Method,
		Modified: file.Modified,
	}

	dst, err := writer.CreateHeader(header)
	if err != nil {
		return err
	}

	// 写入更新后的内容
	_, err = dst.Write([]byte(contentStr))
	return err
}

// copyFile 复制文件
func (p *EnhancedWordCompatibleProcessor) copyFile(file *zip.File, writer *zip.Writer) error {
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

	// 复制内容
	_, err = io.Copy(dst, src)
	return err
}

// processDocumentXML 处理document.xml文件
func (p *EnhancedWordCompatibleProcessor) processDocumentXML(file *zip.File, writer *zip.Writer) error {
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

	fmt.Println("处理document.xml...")
	return p.replaceInDocumentXML(src, dst)
}