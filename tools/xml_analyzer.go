package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// XMLAnalyzer XML分析器
type XMLAnalyzer struct {
	filePath string
}

// NewXMLAnalyzer 创建XML分析器
func NewXMLAnalyzer(filePath string) *XMLAnalyzer {
	return &XMLAnalyzer{
		filePath: filePath,
	}
}

// AnalyzeXMLStructure 分析XML结构
func (xa *XMLAnalyzer) AnalyzeXMLStructure() error {
	// 打开DOCX文件
	reader, err := zip.OpenReader(xa.filePath)
	if err != nil {
		return fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()

	// 查找document.xml文件
	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			return xa.analyzeDocumentXML(file)
		}
	}

	return fmt.Errorf("未找到document.xml文件")
}

// analyzeDocumentXML 分析document.xml文件
func (xa *XMLAnalyzer) analyzeDocumentXML(file *zip.File) error {
	fileReader, err := file.Open()
	if err != nil {
		return err
	}
	defer fileReader.Close()

	content, err := io.ReadAll(fileReader)
	if err != nil {
		return err
	}

	xmlContent := string(content)

	fmt.Println("=== XML结构分析报告 ===")
	fmt.Printf("文件: %s\n", xa.filePath)
	fmt.Printf("XML内容长度: %d 字符\n", len(xmlContent))
	fmt.Println()

	// 分析标签统计
	xa.analyzeTagCounts(xmlContent)

	// 显示XML开头和结尾
	xa.showXMLStructure(xmlContent)

	// 查找可能的问题区域
	xa.findProblemAreas(xmlContent)

	return nil
}

// analyzeTagCounts 分析标签数量
func (xa *XMLAnalyzer) analyzeTagCounts(content string) {
	fmt.Println("=== 标签统计 ===")

	tags := []string{"w:document", "w:body", "w:p", "w:r", "w:t"}

	for _, tag := range tags {
		// 统计开始标签（排除自闭合标签）
		openPattern := fmt.Sprintf(`<%s[^>]*[^/]>`, regexp.QuoteMeta(tag))
		openRe := regexp.MustCompile(openPattern)
		openMatches := openRe.FindAllString(content, -1)
		openCount := len(openMatches)

		// 统计结束标签
		closePattern := fmt.Sprintf(`</%s>`, regexp.QuoteMeta(tag))
		closeRe := regexp.MustCompile(closePattern)
		closeMatches := closeRe.FindAllString(content, -1)
		closeCount := len(closeMatches)

		// 统计自闭合标签
		selfClosePattern := fmt.Sprintf(`<%s[^>]*/\s*>`, regexp.QuoteMeta(tag))
		selfCloseRe := regexp.MustCompile(selfClosePattern)
		selfCloseMatches := selfCloseRe.FindAllString(content, -1)
		selfCloseCount := len(selfCloseMatches)

		fmt.Printf("标签 <%s>: 开始=%d, 结束=%d, 自闭合=%d, 差值=%d\n", 
			tag, openCount, closeCount, selfCloseCount, openCount-closeCount)

		// 显示前几个匹配的标签示例
		if openCount > 0 {
			fmt.Printf("  开始标签示例: %s\n", openMatches[0])
		}
		if closeCount > 0 {
			fmt.Printf("  结束标签示例: %s\n", closeMatches[0])
		}
		if selfCloseCount > 0 {
			fmt.Printf("  自闭合标签示例: %s\n", selfCloseMatches[0])
		}
		fmt.Println()
	}
}

// showXMLStructure 显示XML结构
func (xa *XMLAnalyzer) showXMLStructure(content string) {
	fmt.Println("=== XML结构 ===")

	// 显示前500个字符
	fmt.Println("XML开头:")
	if len(content) > 500 {
		fmt.Println(content[:500] + "...")
	} else {
		fmt.Println(content)
	}
	fmt.Println()

	// 显示后500个字符
	fmt.Println("XML结尾:")
	if len(content) > 500 {
		fmt.Println("..." + content[len(content)-500:])
	}
	fmt.Println()
}

// findProblemAreas 查找问题区域
func (xa *XMLAnalyzer) findProblemAreas(content string) {
	fmt.Println("=== 问题区域分析 ===")

	// 查找可能的标签不匹配
	problems := []string{}

	// 检查是否有未闭合的标签模式
	patterns := []string{
		`<w:p[^>]*>[^<]*$`,     // w:p标签后没有内容或结束标签
		`<w:r[^>]*>[^<]*$`,     // w:r标签后没有内容或结束标签
		`<w:t[^>]*>[^<]*$`,     // w:t标签后没有内容或结束标签
		`</w:[a-z]+>\s*</w:body>`, // 在body结束前的最后几个标签
	}

	for i, pattern := range patterns {
		re := regexp.MustCompile(pattern)
		matches := re.FindAllString(content, 10) // 最多找10个匹配
		if len(matches) > 0 {
			fmt.Printf("模式 %d 匹配 %d 次:\n", i+1, len(matches))
			for j, match := range matches {
				if j < 3 { // 只显示前3个
					fmt.Printf("  %s\n", strings.TrimSpace(match))
				}
			}
			fmt.Println()
		}
	}

	// 查找body结束前的内容
	bodyEndPos := strings.LastIndex(content, "</w:body>")
	if bodyEndPos != -1 {
		startPos := bodyEndPos - 200
		if startPos < 0 {
			startPos = 0
		}
		fmt.Println("</w:body>前的内容:")
		fmt.Println(content[startPos:bodyEndPos+9])
		fmt.Println()
	}

	if len(problems) == 0 {
		fmt.Println("未发现明显的问题模式")
	}
}

func main() {
	if len(os.Args) != 2 {
		fmt.Println("用法: go run xml_analyzer.go <DOCX文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]

	analyzer := NewXMLAnalyzer(inputFile)
	if err := analyzer.AnalyzeXMLStructure(); err != nil {
		fmt.Printf("分析失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("=== 分析完成 ===")
}