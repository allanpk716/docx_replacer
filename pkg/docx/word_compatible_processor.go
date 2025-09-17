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

// WordCompatibleProcessor Word兼容的处理器，确保生成的文档能在Word中正确显示
type WordCompatibleProcessor struct {
	filePath string
}

// NewWordCompatibleProcessor 创建新的Word兼容处理器
func NewWordCompatibleProcessor(filePath string) *WordCompatibleProcessor {
	return &WordCompatibleProcessor{
		filePath: filePath,
	}
}

// ReplaceKeywordsWithWordCompatibility 替换关键词并确保Word兼容性
func (wcp *WordCompatibleProcessor) ReplaceKeywordsWithWordCompatibility(replacements map[string]string, outputPath string) error {
	// 读取DOCX文件
	reader, err := zip.OpenReader(wcp.filePath)
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

		// 处理document.xml文件
		if file.Name == "word/document.xml" {
			documentXMLContent = string(content)
			
			// 直接在XML中替换文本内容，保持原始结构
			modifiedXML, count := wcp.replaceTextInXML(documentXMLContent, replacements)
			replacementCount = count
			
			// 输出替换信息
			for keyword, replacement := range replacements {
				if strings.Contains(documentXMLContent, keyword) {
					fmt.Printf("替换关键词: '%s' -> '%s'\n", keyword, replacement)
				}
			}
			
			content = []byte(modifiedXML)
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

// replaceTextInXML 直接在XML中替换文本内容，保持原始结构
func (wcp *WordCompatibleProcessor) replaceTextInXML(xmlContent string, replacements map[string]string) (string, int) {
	replacementCount := 0
	modifiedXML := xmlContent
	
	// 使用正则表达式找到所有<w:t>标签并替换其中的文本（支持多行）
	re := regexp.MustCompile(`(?s)(<w:t[^>]*>)(.*?)(</w:t>)`)
	modifiedXML = re.ReplaceAllStringFunc(modifiedXML, func(match string) string {
		submatches := re.FindStringSubmatch(match)
		if len(submatches) != 4 {
			return match
		}
		
		openingTag := submatches[1]
		textContent := submatches[2]
		closingTag := submatches[3]
		
		// 对文本内容进行关键词替换
		modifiedText := textContent
		hasReplacement := false
		for keyword, replacement := range replacements {
			if strings.Contains(modifiedText, keyword) {
				// 先解码原始文本，再替换，最后重新编码
				decodedText := wcp.unescapeXML(modifiedText)
				decodedText = strings.ReplaceAll(decodedText, keyword, replacement)
				modifiedText = wcp.escapeXML(decodedText)
				replacementCount++
				hasReplacement = true
			}
		}
		
		// 如果没有替换，保持原始文本不变
		if !hasReplacement {
			modifiedText = textContent
		}
		
		return openingTag + modifiedText + closingTag
	})
	
	return modifiedXML, replacementCount
}

// rebuildWordDocument 重建Word文档的XML结构
func (wcp *WordCompatibleProcessor) rebuildWordDocument(textContent string) string {
	// 创建标准的Word文档XML结构
	xmlBuilder := strings.Builder{}
	
	// XML声明和根元素
	xmlBuilder.WriteString(`<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`)
	xmlBuilder.WriteString(`<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">`)
	xmlBuilder.WriteString(`<w:body>`)
	
	// 将文本内容分段处理
	paragraphs := wcp.splitIntoParagraphs(textContent)
	
	for _, paragraph := range paragraphs {
		if strings.TrimSpace(paragraph) != "" {
			xmlBuilder.WriteString(`<w:p>`)
			xmlBuilder.WriteString(`<w:r>`)
			xmlBuilder.WriteString(`<w:t>`)
			// 转义XML特殊字符
			xmlBuilder.WriteString(wcp.escapeXML(paragraph))
			xmlBuilder.WriteString(`</w:t>`)
			xmlBuilder.WriteString(`</w:r>`)
			xmlBuilder.WriteString(`</w:p>`)
		}
	}
	
	// 添加文档网格设置（Word兼容性要求）
	xmlBuilder.WriteString(`<w:sectPr>`)
	xmlBuilder.WriteString(`<w:pgSz w:w="11906" w:h="16838"/>`)
	xmlBuilder.WriteString(`<w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/>`)
	xmlBuilder.WriteString(`<w:cols w:space="708"/>`)
	xmlBuilder.WriteString(`<w:docGrid w:linePitch="360"/>`)
	xmlBuilder.WriteString(`</w:sectPr>`)
	
	// 关闭根元素
	xmlBuilder.WriteString(`</w:body>`)
	xmlBuilder.WriteString(`</w:document>`)
	
	return xmlBuilder.String()
}

// splitIntoParagraphs 将文本分割成段落
func (wcp *WordCompatibleProcessor) splitIntoParagraphs(text string) []string {
	// 按换行符分割
	lines := strings.Split(text, "\n")
	var paragraphs []string
	
	for _, line := range lines {
		trimmed := strings.TrimSpace(line)
		if trimmed != "" {
			paragraphs = append(paragraphs, trimmed)
		}
	}
	
	// 如果没有段落，创建一个包含所有文本的段落
	if len(paragraphs) == 0 && strings.TrimSpace(text) != "" {
		paragraphs = append(paragraphs, strings.TrimSpace(text))
	}
	
	return paragraphs
}

// escapeXML 转义XML特殊字符
func (wcp *WordCompatibleProcessor) escapeXML(text string) string {
	replacer := strings.NewReplacer(
		"&", "&amp;",
		"<", "&lt;",
		">", "&gt;",
		"\"", "&quot;",
		"'", "&apos;",
	)
	return replacer.Replace(text)
}

// unescapeXML 反转义XML特殊字符
func (wcp *WordCompatibleProcessor) unescapeXML(text string) string {
	replacer := strings.NewReplacer(
		"&amp;", "&",
		"&lt;", "<",
		"&gt;", ">",
		"&quot;", "\"",
		"&apos;", "'",
	)
	return replacer.Replace(text)
}