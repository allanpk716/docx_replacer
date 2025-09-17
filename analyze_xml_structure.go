package main

import (
	"archive/zip"
	"fmt"
	"io"
	"log"
	"os"
	"regexp"
	"strings"
)

func main() {
	// 分析原始文档和处理后文档的XML结构
	originalFile := "C:\\WorkSpace\\Go2Hell\\src\\github.com\\allanpk716\\docx_replacer\\input\\1.2.申请表.docx"
	processedFile := "C:\\WorkSpace\\Go2Hell\\src\\github.com\\allanpk716\\docx_replacer\\output_test.docx"

	fmt.Println("=== 分析原始文档XML结构 ===")
	originalXML, err := extractDocumentXML(originalFile)
	if err != nil {
		log.Fatalf("提取原始文档XML失败: %v", err)
	}

	fmt.Println("=== 分析处理后文档XML结构 ===")
	processedXML, err := extractDocumentXML(processedFile)
	if err != nil {
		log.Fatalf("提取处理后文档XML失败: %v", err)
	}

	// 保存XML内容到文件以便详细分析
	if err := os.WriteFile("original_document.xml", []byte(originalXML), 0644); err != nil {
		log.Printf("保存原始XML失败: %v", err)
	} else {
		fmt.Println("✓ 原始XML已保存到 original_document.xml")
	}

	if err := os.WriteFile("processed_document.xml", []byte(processedXML), 0644); err != nil {
		log.Printf("保存处理后XML失败: %v", err)
	} else {
		fmt.Println("✓ 处理后XML已保存到 processed_document.xml")
	}

	// 分析XML结构
	fmt.Println("\n=== XML结构分析 ===")
	analyzeXMLStructure("原始文档", originalXML)
	analyzeXMLStructure("处理后文档", processedXML)

	// 检查XML标签匹配
	fmt.Println("\n=== XML标签匹配检查 ===")
	checkXMLTagMatching("原始文档", originalXML)
	checkXMLTagMatching("处理后文档", processedXML)

	// 检查命名空间
	fmt.Println("\n=== 命名空间分析 ===")
	analyzeNamespaces("原始文档", originalXML)
	analyzeNamespaces("处理后文档", processedXML)
}

// extractDocumentXML 从DOCX文件中提取document.xml内容
func extractDocumentXML(filePath string) (string, error) {
	reader, err := zip.OpenReader(filePath)
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

			return string(content), nil
		}
	}

	return "", fmt.Errorf("未找到document.xml文件")
}

// analyzeXMLStructure 分析XML结构
func analyzeXMLStructure(name, xmlContent string) {
	fmt.Printf("\n--- %s ---\n", name)
	fmt.Printf("XML长度: %d 字符\n", len(xmlContent))

	// 统计主要标签数量
	tags := []string{"w:document", "w:body", "w:p", "w:r", "w:t", "w:sectPr", "w:tbl", "w:tr", "w:tc"}
	for _, tag := range tags {
		count := countTags(xmlContent, tag)
		fmt.Printf("%s 标签数量: %d\n", tag, count)
	}

	// 检查是否有XML声明
	if strings.HasPrefix(xmlContent, "<?xml") {
		fmt.Println("✓ 包含XML声明")
	} else {
		fmt.Println("❌ 缺少XML声明")
	}
}

// countTags 统计指定标签的数量
func countTags(xmlContent, tagName string) int {
	// 统计开始标签
	pattern := fmt.Sprintf(`<%s[\s>]`, regexp.QuoteMeta(tagName))
	re := regexp.MustCompile(pattern)
	matches := re.FindAllString(xmlContent, -1)
	return len(matches)
}

// checkXMLTagMatching 检查XML标签匹配
func checkXMLTagMatching(name, xmlContent string) {
	fmt.Printf("\n--- %s 标签匹配检查 ---\n", name)

	tags := []string{"w:p", "w:r", "w:t", "w:tbl", "w:tr", "w:tc"}
	for _, tag := range tags {
		openCount := countOpenTags(xmlContent, tag)
		closeCount := countCloseTags(xmlContent, tag)
		if openCount == closeCount {
			fmt.Printf("✓ %s: 开始标签 %d, 结束标签 %d (匹配)\n", tag, openCount, closeCount)
		} else {
			fmt.Printf("❌ %s: 开始标签 %d, 结束标签 %d (不匹配，差异: %d)\n", tag, openCount, closeCount, openCount-closeCount)
		}
	}
}

// countOpenTags 统计开始标签数量
func countOpenTags(xmlContent, tagName string) int {
	pattern := fmt.Sprintf(`<%s[\s>]`, regexp.QuoteMeta(tagName))
	re := regexp.MustCompile(pattern)
	matches := re.FindAllString(xmlContent, -1)
	return len(matches)
}

// countCloseTags 统计结束标签数量
func countCloseTags(xmlContent, tagName string) int {
	pattern := fmt.Sprintf(`</%s>`, regexp.QuoteMeta(tagName))
	re := regexp.MustCompile(pattern)
	matches := re.FindAllString(xmlContent, -1)
	return len(matches)
}

// analyzeNamespaces 分析命名空间
func analyzeNamespaces(name, xmlContent string) {
	fmt.Printf("\n--- %s 命名空间分析 ---\n", name)

	// 查找命名空间声明
	nsPattern := regexp.MustCompile(`xmlns:([^=]+)="([^"]+)"`)
	matches := nsPattern.FindAllStringSubmatch(xmlContent, -1)

	if len(matches) > 0 {
		fmt.Println("命名空间声明:")
		for _, match := range matches {
			if len(match) >= 3 {
				fmt.Printf("  %s -> %s\n", match[1], match[2])
			}
		}
	} else {
		fmt.Println("❌ 未找到命名空间声明")
	}

	// 检查默认命名空间
	defaultNsPattern := regexp.MustCompile(`xmlns="([^"]+)"`)
	defaultMatches := defaultNsPattern.FindAllStringSubmatch(xmlContent, -1)
	if len(defaultMatches) > 0 {
		fmt.Println("默认命名空间:")
		for _, match := range defaultMatches {
			if len(match) >= 2 {
				fmt.Printf("  %s\n", match[1])
			}
		}
	}
}