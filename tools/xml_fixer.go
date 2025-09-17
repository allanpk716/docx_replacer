package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

func main() {
	if len(os.Args) != 3 {
		fmt.Println("使用方法: go run xml_fixer.go <输入docx文件> <输出docx文件>")
		os.Exit(1)
	}

	inputPath := os.Args[1]
	outputPath := os.Args[2]

	err := fixDocxXML(inputPath, outputPath)
	if err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("XML结构修复完成")
}

func fixDocxXML(inputPath, outputPath string) error {
	// 读取DOCX文件
	reader, err := zip.OpenReader(inputPath)
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

		// 修复document.xml文件
		if file.Name == "word/document.xml" {
			fmt.Println("正在修复document.xml...")
			fixedXML := fixXMLStructureAdvanced(string(content))
			content = []byte(fixedXML)
			fmt.Println("document.xml修复完成")
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

	return nil
}

func fixXMLStructureAdvanced(xmlContent string) string {
	fmt.Println("开始高级XML结构修复...")

	// 提取文本内容并重建结构
	textContent := extractAllText(xmlContent)
	fmt.Printf("提取到文本内容长度: %d 字符\n", len(textContent))

	// 重建完整的XML结构
	fixedXML := rebuildCompleteXML(xmlContent, textContent)

	fmt.Println("高级XML结构修复完成")
	return fixedXML
}

func extractAllText(xmlContent string) string {
	// 提取所有<w:t>标签中的文本内容
	re := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
	matches := re.FindAllStringSubmatch(xmlContent, -1)

	var textParts []string
	for _, match := range matches {
		if len(match) > 1 && strings.TrimSpace(match[1]) != "" {
			textParts = append(textParts, match[1])
		}
	}

	return strings.Join(textParts, "")
}

func rebuildCompleteXML(originalXML, textContent string) string {
	// 提取XML头部和命名空间
	headerMatch := regexp.MustCompile(`<\?xml[^>]*>\s*<w:document[^>]*>`).FindString(originalXML)
	if headerMatch == "" {
		headerMatch = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">`
	}

	// 提取尾部
	footerMatch := regexp.MustCompile(`</w:document>\s*$`).FindString(originalXML)
	if footerMatch == "" {
		footerMatch = "</w:document>"
	}

	// 构建新的XML结构
	var xmlBuilder strings.Builder

	// 写入头部
	xmlBuilder.WriteString(headerMatch)
	xmlBuilder.WriteString("<w:body>")

	// 将文本内容分段并构建段落
	paragraphs := splitTextIntoParagraphs(textContent)
	for _, paragraph := range paragraphs {
		if strings.TrimSpace(paragraph) != "" {
			xmlBuilder.WriteString("<w:p>")
			xmlBuilder.WriteString("<w:r>")
			xmlBuilder.WriteString("<w:t>")
			xmlBuilder.WriteString(escapeXML(paragraph))
			xmlBuilder.WriteString("</w:t>")
			xmlBuilder.WriteString("</w:r>")
			xmlBuilder.WriteString("</w:p>")
		}
	}

	// 添加文档设置（从原始XML中提取或使用默认值）
	sectPr := extractSectPr(originalXML)
	if sectPr != "" {
		xmlBuilder.WriteString(sectPr)
	} else {
		// 默认的文档设置
		xmlBuilder.WriteString(`<w:sectPr>`)
		xmlBuilder.WriteString(`<w:pgSz w:w="11906" w:h="16838"/>`)
		xmlBuilder.WriteString(`<w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/>`)
		xmlBuilder.WriteString(`<w:cols w:space="708"/>`)
		xmlBuilder.WriteString(`<w:docGrid w:linePitch="360"/>`)
		xmlBuilder.WriteString(`</w:sectPr>`)
	}

	xmlBuilder.WriteString("</w:body>")
	xmlBuilder.WriteString(footerMatch)

	return xmlBuilder.String()
}

func splitTextIntoParagraphs(text string) []string {
	// 按换行符分割文本
	lines := strings.Split(text, "\n")
	var paragraphs []string

	for _, line := range lines {
		trimmed := strings.TrimSpace(line)
		if trimmed != "" {
			paragraphs = append(paragraphs, trimmed)
		}
	}

	// 如果没有段落，将整个文本作为一个段落
	if len(paragraphs) == 0 && strings.TrimSpace(text) != "" {
		paragraphs = append(paragraphs, strings.TrimSpace(text))
	}

	// 如果文本太长，按句号分割
	var finalParagraphs []string
	for _, para := range paragraphs {
		if len(para) > 1000 {
			sentences := strings.Split(para, "。")
			for _, sentence := range sentences {
				if strings.TrimSpace(sentence) != "" {
					finalParagraphs = append(finalParagraphs, strings.TrimSpace(sentence)+"。")
				}
			}
		} else {
			finalParagraphs = append(finalParagraphs, para)
		}
	}

	return finalParagraphs
}

func extractSectPr(xmlContent string) string {
	// 提取<w:sectPr>部分
	re := regexp.MustCompile(`<w:sectPr[^>]*>.*?</w:sectPr>`)
	match := re.FindString(xmlContent)
	return match
}

func escapeXML(text string) string {
	replacer := strings.NewReplacer(
		"&", "&amp;",
		"<", "&lt;",
		">", "&gt;",
		"\"", "&quot;",
		"'", "&apos;",
	)
	return replacer.Replace(text)
}