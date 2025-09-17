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
	if len(os.Args) != 2 {
		fmt.Println("使用方法: go run xml_structure_analyzer.go <docx文件路径>")
		os.Exit(1)
	}

	docxPath := os.Args[1]

	// 打开DOCX文件
	reader, err := zip.OpenReader(docxPath)
	if err != nil {
		fmt.Printf("打开DOCX文件失败: %v\n", err)
		os.Exit(1)
	}
	defer reader.Close()

	// 查找document.xml文件
	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			fileReader, err := file.Open()
			if err != nil {
				fmt.Printf("打开document.xml失败: %v\n", err)
				os.Exit(1)
			}

			content, err := io.ReadAll(fileReader)
			fileReader.Close()
			if err != nil {
				fmt.Printf("读取document.xml失败: %v\n", err)
				os.Exit(1)
			}

			analyzeXMLStructure(string(content))
			return
		}
	}

	fmt.Println("未找到document.xml文件")
}

func analyzeXMLStructure(xmlContent string) {
	fmt.Println("=== XML结构分析 ===")
	fmt.Printf("XML内容长度: %d 字符\n", len(xmlContent))

	// 分析<w:t>标签的结构
	fmt.Println("\n=== <w:t>标签分析 ===")
	analyzeTextTags(xmlContent)

	// 显示前1000个字符的内容
	fmt.Println("\n=== XML内容预览 (前1000字符) ===")
	if len(xmlContent) > 1000 {
		fmt.Println(xmlContent[:1000])
		fmt.Println("...")
	} else {
		fmt.Println(xmlContent)
	}

	// 查找可能的问题模式
	fmt.Println("\n=== 问题模式检测 ===")
	detectProblems(xmlContent)
}

func analyzeTextTags(xmlContent string) {
	// 查找所有<w:t>标签及其内容
	re := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
	matches := re.FindAllStringSubmatch(xmlContent, -1)

	fmt.Printf("找到 %d 个完整的<w:t>标签\n", len(matches))

	// 显示前10个标签的内容
	for i, match := range matches {
		if i >= 10 {
			fmt.Printf("... 还有 %d 个标签\n", len(matches)-10)
			break
		}
		fmt.Printf("  %d: '%s'\n", i+1, match[1])
	}

	// 查找不完整的<w:t>标签
	openingTags := regexp.MustCompile(`<w:t[^>]*>`).FindAllString(xmlContent, -1)
	closingTags := regexp.MustCompile(`</w:t>`).FindAllString(xmlContent, -1)

	fmt.Printf("\n开始标签 <w:t> 数量: %d\n", len(openingTags))
	fmt.Printf("结束标签 </w:t> 数量: %d\n", len(closingTags))

	if len(openingTags) != len(closingTags) {
		fmt.Printf("⚠️  标签不匹配！差异: %d\n", len(openingTags)-len(closingTags))
	}
}

func detectProblems(xmlContent string) {
	// 检测跨标签的文本分割
	if strings.Contains(xmlContent, "<w:t></w:t>") {
		fmt.Println("⚠️  发现空的<w:t>标签")
	}

	// 检测嵌套的<w:t>标签
	if regexp.MustCompile(`<w:t[^>]*>.*<w:t[^>]*>`).MatchString(xmlContent) {
		fmt.Println("⚠️  可能存在嵌套的<w:t>标签")
	}

	// 检测未闭合的标签
	tagPatterns := []string{"w:p", "w:r", "w:t"}
	for _, tag := range tagPatterns {
		openingPattern := fmt.Sprintf(`<%s[^>]*>`, tag)
		closingPattern := fmt.Sprintf(`</%s>`, tag)

		openingCount := len(regexp.MustCompile(openingPattern).FindAllString(xmlContent, -1))
		closingCount := len(regexp.MustCompile(closingPattern).FindAllString(xmlContent, -1))

		if openingCount != closingCount {
			fmt.Printf("⚠️  标签 <%s> 不匹配: 开始 %d, 结束 %d\n", tag, openingCount, closingCount)
		}
	}

	// 检测关键词是否跨标签分布
	keywords := []string{"公司名称", "产品名称"}
	for _, keyword := range keywords {
		if strings.Contains(xmlContent, keyword) {
			fmt.Printf("✓ 找到关键词: '%s'\n", keyword)
			// 查找关键词周围的XML结构
			index := strings.Index(xmlContent, keyword)
			start := index - 100
			if start < 0 {
				start = 0
			}
			end := index + len(keyword) + 100
			if end > len(xmlContent) {
				end = len(xmlContent)
			}
			fmt.Printf("  上下文: %s\n", xmlContent[start:end])
		}
	}
}