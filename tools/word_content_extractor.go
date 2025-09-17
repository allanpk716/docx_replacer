package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// WordContentExtractor Word内容提取器
type WordContentExtractor struct {
	filePath string
}

// NewWordContentExtractor 创建Word内容提取器
func NewWordContentExtractor(filePath string) *WordContentExtractor {
	return &WordContentExtractor{
		filePath: filePath,
	}
}

// ExtractAndRebuildContent 提取内容并重建文档
func (wce *WordContentExtractor) ExtractAndRebuildContent(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(wce.filePath)
	if err != nil {
		return fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()

	// 提取文本内容
	textContent, err := wce.extractTextContent(reader)
	if err != nil {
		return fmt.Errorf("提取文本内容失败: %v", err)
	}

	fmt.Printf("提取到的文本内容长度: %d 字符\n", len(textContent))
	fmt.Printf("文本内容预览: %s...\n", textContent[:min(200, len(textContent))])

	// 重建DOCX文件
	err = wce.rebuildDocx(reader, textContent, outputPath)
	if err != nil {
		return fmt.Errorf("重建DOCX文件失败: %v", err)
	}

	fmt.Println("Word内容提取和重建完成!")
	return nil
}

// extractTextContent 提取文本内容
func (wce *WordContentExtractor) extractTextContent(reader *zip.ReadCloser) (string, error) {
	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			fileReader, err := file.Open()
			if err != nil {
				return "", err
			}
			defer fileReader.Close()

			content, err := io.ReadAll(fileReader)
			if err != nil {
				return "", err
			}

			xmlContent := string(content)
			return wce.extractTextFromXML(xmlContent), nil
		}
	}
	return "", fmt.Errorf("未找到document.xml文件")
}

// extractTextFromXML 从XML中提取文本
func (wce *WordContentExtractor) extractTextFromXML(xmlContent string) string {
	// 提取所有<w:t>标签中的文本内容
	textPattern := `<w:t[^>]*>([^<]*)</w:t>`
	re := regexp.MustCompile(textPattern)
	matches := re.FindAllStringSubmatch(xmlContent, -1)

	var textParts []string
	for _, match := range matches {
		if len(match) > 1 {
			text := strings.TrimSpace(match[1])
			if text != "" {
				textParts = append(textParts, text)
			}
		}
	}

	// 也尝试提取不完整的文本标签中的内容
	incompletePattern := `<w:t[^>]*>([^<]+)`
	re2 := regexp.MustCompile(incompletePattern)
	matches2 := re2.FindAllStringSubmatch(xmlContent, -1)

	for _, match := range matches2 {
		if len(match) > 1 {
			text := strings.TrimSpace(match[1])
			if text != "" && !contains(textParts, text) {
				textParts = append(textParts, text)
			}
		}
	}

	fmt.Printf("提取到 %d 个文本片段\n", len(textParts))
	return strings.Join(textParts, " ")
}

// contains 检查字符串数组是否包含指定字符串
func contains(arr []string, str string) bool {
	for _, s := range arr {
		if s == str {
			return true
		}
	}
	return false
}

// rebuildDocx 重建DOCX文件
func (wce *WordContentExtractor) rebuildDocx(reader *zip.ReadCloser, textContent, outputPath string) error {
	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return err
	}
	defer outputFile.Close()

	// 创建ZIP写入器
	zipWriter := zip.NewWriter(outputFile)
	defer zipWriter.Close()

	// 复制所有文件，但替换document.xml
	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			// 创建新的document.xml
			newDocumentXML := wce.createCleanDocumentXML(textContent)
			writer, err := zipWriter.Create(file.Name)
			if err != nil {
				return err
			}
			_, err = writer.Write([]byte(newDocumentXML))
			if err != nil {
				return err
			}
		} else {
			// 复制其他文件
			if err := wce.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	return nil
}

// createCleanDocumentXML 创建干净的document.xml
func (wce *WordContentExtractor) createCleanDocumentXML(textContent string) string {
	// 创建一个标准的Word文档XML结构
	documentXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:body>
`

	// 将文本内容分段处理
	paragraphs := strings.Split(textContent, "\n")
	if len(paragraphs) == 1 {
		// 如果没有换行符，按句号分段
		paragraphs = strings.Split(textContent, "。")
	}

	for _, paragraph := range paragraphs {
		paragraph = strings.TrimSpace(paragraph)
		if paragraph != "" {
			// 为每个段落创建标准的Word XML结构
			documentXML += fmt.Sprintf(`<w:p>
<w:r>
<w:t>%s</w:t>
</w:r>
</w:p>
`, wce.escapeXML(paragraph))
		}
	}

	documentXML += `</w:body>
</w:document>`

	return documentXML
}

// escapeXML 转义XML特殊字符
func (wce *WordContentExtractor) escapeXML(text string) string {
	text = strings.ReplaceAll(text, "&", "&amp;")
	text = strings.ReplaceAll(text, "<", "&lt;")
	text = strings.ReplaceAll(text, ">", "&gt;")
	text = strings.ReplaceAll(text, "\"", "&quot;")
	text = strings.ReplaceAll(text, "'", "&apos;")
	return text
}

// copyFile 复制文件到ZIP
func (wce *WordContentExtractor) copyFile(file *zip.File, zipWriter *zip.Writer) error {
	fileReader, err := file.Open()
	if err != nil {
		return err
	}
	defer fileReader.Close()

	writer, err := zipWriter.Create(file.Name)
	if err != nil {
		return err
	}

	_, err = io.Copy(writer, fileReader)
	return err
}

// min 返回两个整数中的较小值
func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}

func main() {
	if len(os.Args) != 3 {
		fmt.Println("用法: go run word_content_extractor.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	extractor := NewWordContentExtractor(inputFile)
	if err := extractor.ExtractAndRebuildContent(outputFile); err != nil {
		fmt.Printf("提取和重建失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("内容提取和重建完成!")
}