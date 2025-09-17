package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// verifyXMLStructure 验证XML结构完整性
func verifyXMLStructure(xmlContent string) {
	fmt.Println("验证XML结构完整性...")

	// 检查关键标签的匹配情况
	tags := []string{"w:p", "w:r", "w:t", "w:tbl", "w:tr", "w:tc"}

	for _, tag := range tags {
		openPattern := fmt.Sprintf(`<%s[^>]*>`, tag)
		closePattern := fmt.Sprintf(`</%s>`, tag)

		openRegex := regexp.MustCompile(openPattern)
		closeRegex := regexp.MustCompile(closePattern)

		openMatches := openRegex.FindAllString(xmlContent, -1)
		closeMatches := closeRegex.FindAllString(xmlContent, -1)

		openCount := len(openMatches)
		closeCount := len(closeMatches)

		if openCount == closeCount {
			fmt.Printf("✓ %s: 开始标签 %d, 结束标签 %d (匹配)\n", tag, openCount, closeCount)
		} else {
			fmt.Printf("❌ %s: 开始标签 %d, 结束标签 %d (不匹配，差异: %d)\n", tag, openCount, closeCount, openCount-closeCount)
		}
	}
}

// extractDocumentXML 提取document.xml内容
func extractDocumentXML(docxPath string) (string, error) {
	reader, err := zip.OpenReader(docxPath)
	if err != nil {
		return "", err
	}
	defer reader.Close()

	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			f, err := file.Open()
			if err != nil {
				return "", err
			}
			defer f.Close()

			content, err := io.ReadAll(f)
			if err != nil {
				return "", err
			}

			return string(content), nil
		}
	}

	return "", fmt.Errorf("未找到document.xml")
}

// analyzeTextTags 分析<w:t>标签的内容
func analyzeTextTags(xmlContent string) {
	fmt.Println("\n分析<w:t>标签内容...")

	textTagRegex := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
	matches := textTagRegex.FindAllStringSubmatch(xmlContent, -1)

	fmt.Printf("找到 %d 个<w:t>标签\n", len(matches))

	// 显示前10个标签的内容
	for i, match := range matches {
		if i >= 10 {
			fmt.Println("... (显示前10个)")
			break
		}
		if len(match) > 1 && strings.TrimSpace(match[1]) != "" {
			fmt.Printf("  [%d]: '%s'\n", i+1, match[1])
		}
	}
}

func main() {
	if len(os.Args) != 2 {
		fmt.Println("用法: go run verify_conservative.go <docx文件路径>")
		os.Exit(1)
	}

	docxPath := os.Args[1]
	fmt.Printf("验证文件: %s\n", docxPath)

	// 提取document.xml
	xmlContent, err := extractDocumentXML(docxPath)
	if err != nil {
		fmt.Printf("提取document.xml失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("document.xml大小: %d 字符\n", len(xmlContent))

	// 验证XML结构
	verifyXMLStructure(xmlContent)

	// 分析文本标签
	analyzeTextTags(xmlContent)

	fmt.Println("\n验证完成")
}