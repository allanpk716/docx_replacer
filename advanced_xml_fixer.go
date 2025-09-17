package main

import (
	"archive/zip"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// AdvancedXMLFixer 高级XML修复器
type AdvancedXMLFixer struct {
	replaceMap map[string]string
}

// NewAdvancedXMLFixer 创建新的高级XML修复器
func NewAdvancedXMLFixer(replaceMap map[string]string) *AdvancedXMLFixer {
	return &AdvancedXMLFixer{
		replaceMap: replaceMap,
	}
}

// FixAndReplaceDocx 修复并替换DOCX文件
func (fixer *AdvancedXMLFixer) FixAndReplaceDocx(inputPath, outputPath string) error {
	fmt.Printf("开始处理文件: %s\n", inputPath)

	// 读取原始DOCX文件
	reader, err := zip.OpenReader(inputPath)
	if err != nil {
		return fmt.Errorf("无法打开DOCX文件: %v", err)
	}
	defer reader.Close()

	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("无法创建输出文件: %v", err)
	}
	defer outputFile.Close()

	// 创建ZIP写入器
	writer := zip.NewWriter(outputFile)
	defer writer.Close()

	// 处理每个文件
	for _, file := range reader.File {
		if err := fixer.processZipFile(file, writer); err != nil {
			return fmt.Errorf("处理文件 %s 时出错: %v", file.Name, err)
		}
	}

	fmt.Printf("文件处理完成: %s\n", outputPath)
	return nil
}

// processZipFile 处理ZIP文件中的单个文件
func (fixer *AdvancedXMLFixer) processZipFile(file *zip.File, writer *zip.Writer) error {
	// 读取文件内容
	rc, err := file.Open()
	if err != nil {
		return err
	}
	defer rc.Close()

	content, err := io.ReadAll(rc)
	if err != nil {
		return err
	}

	// 创建输出文件头
	header := &zip.FileHeader{
		Name:   file.Name,
		Method: zip.Deflate,
	}
	header.SetMode(file.Mode())

	w, err := writer.CreateHeader(header)
	if err != nil {
		return err
	}

	// 如果是document.xml，进行特殊处理
	if file.Name == "word/document.xml" {
		fixedContent := fixer.fixDocumentXML(string(content))
		_, err = w.Write([]byte(fixedContent))
	} else {
		_, err = w.Write(content)
	}

	return err
}

// fixDocumentXML 修复document.xml文件
func (fixer *AdvancedXMLFixer) fixDocumentXML(content string) string {
	fmt.Println("开始修复document.xml...")

	// 1. 先进行关键词替换
	fixedContent := fixer.replaceKeywords(content)

	// 2. 修复XML标签匹配问题
	fixedContent = fixer.fixXMLTagMatching(fixedContent)

	// 3. 修复段落结构
	fixedContent = fixer.fixParagraphStructure(fixedContent)

	// 4. 验证XML结构
	fixer.validateXMLStructure(fixedContent)

	return fixedContent
}

// replaceKeywords 替换关键词
func (fixer *AdvancedXMLFixer) replaceKeywords(content string) string {
	for oldText, newText := range fixer.replaceMap {
		// 使用更精确的替换策略
		content = fixer.safeReplaceInXML(content, oldText, newText)
		fmt.Printf("替换关键词: %s -> %s\n", oldText, newText)
	}
	return content
}

// safeReplaceInXML 安全地在XML中替换文本
func (fixer *AdvancedXMLFixer) safeReplaceInXML(content, oldText, newText string) string {
	// 匹配<w:t>标签内的文本，支持跨标签的文本
	pattern := `(<w:t[^>]*>)([^<]*` + regexp.QuoteMeta(oldText) + `[^<]*)(</w:t>)`
	re := regexp.MustCompile(pattern)

	return re.ReplaceAllStringFunc(content, func(match string) string {
		submatches := re.FindStringSubmatch(match)
		if len(submatches) == 4 {
			openTag := submatches[1]
			textContent := submatches[2]
			closeTag := submatches[3]
			
			// 替换文本内容
			newTextContent := strings.ReplaceAll(textContent, oldText, newText)
			
			// 转义XML特殊字符
			newTextContent = fixer.escapeXML(newTextContent)
			
			return openTag + newTextContent + closeTag
		}
		return match
	})
}

// fixXMLTagMatching 修复XML标签匹配问题
func (fixer *AdvancedXMLFixer) fixXMLTagMatching(content string) string {
	fmt.Println("修复XML标签匹配...")

	// 修复自闭合标签
	content = fixer.fixSelfClosingTags(content)

	// 修复不匹配的段落标签
	content = fixer.fixParagraphTags(content)

	// 修复不匹配的运行标签
	content = fixer.fixRunTags(content)

	// 修复不匹配的文本标签
	content = fixer.fixTextTags(content)

	// 修复不匹配的表格单元格标签
	content = fixer.fixTableCellTags(content)

	return content
}

// fixSelfClosingTags 修复自闭合标签
func (fixer *AdvancedXMLFixer) fixSelfClosingTags(content string) string {
	// 将不正确的自闭合标签转换为正确的格式
	patterns := []struct {
		pattern string
		replace string
	}{
		{`<w:br></w:br>`, `<w:br/>`},
		{`<w:tab></w:tab>`, `<w:tab/>`},
		{`<w:cr></w:cr>`, `<w:cr/>`},
	}

	for _, p := range patterns {
		content = strings.ReplaceAll(content, p.pattern, p.replace)
	}

	return content
}

// fixParagraphTags 修复段落标签
func (fixer *AdvancedXMLFixer) fixParagraphTags(content string) string {
	// 简化的段落标签修复策略
	// 查找所有<w:p>开始标签
	pOpenPattern := regexp.MustCompile(`<w:p[^>]*>`)
	pClosePattern := regexp.MustCompile(`</w:p>`)
	
	openMatches := pOpenPattern.FindAllStringIndex(content, -1)
	closeMatches := pClosePattern.FindAllStringIndex(content, -1)
	
	// 如果开始标签多于结束标签，在末尾添加缺失的结束标签
	if len(openMatches) > len(closeMatches) {
		missing := len(openMatches) - len(closeMatches)
		for i := 0; i < missing; i++ {
			content += "</w:p>"
		}
	}
	
	return content
}

// fixRunTags 修复运行标签
func (fixer *AdvancedXMLFixer) fixRunTags(content string) string {
	// 简化的运行标签修复策略
	rOpenPattern := regexp.MustCompile(`<w:r[^>]*>`)
	rClosePattern := regexp.MustCompile(`</w:r>`)
	
	openMatches := rOpenPattern.FindAllStringIndex(content, -1)
	closeMatches := rClosePattern.FindAllStringIndex(content, -1)
	
	// 如果开始标签多于结束标签，在末尾添加缺失的结束标签
	if len(openMatches) > len(closeMatches) {
		missing := len(openMatches) - len(closeMatches)
		for i := 0; i < missing; i++ {
			content += "</w:r>"
		}
	}
	
	return content
}

// fixTextTags 修复文本标签
func (fixer *AdvancedXMLFixer) fixTextTags(content string) string {
	// 简化的文本标签修复策略
	tOpenPattern := regexp.MustCompile(`<w:t[^>]*>`)
	tClosePattern := regexp.MustCompile(`</w:t>`)
	
	openMatches := tOpenPattern.FindAllStringIndex(content, -1)
	closeMatches := tClosePattern.FindAllStringIndex(content, -1)
	
	// 如果开始标签多于结束标签，在末尾添加缺失的结束标签
	if len(openMatches) > len(closeMatches) {
		missing := len(openMatches) - len(closeMatches)
		for i := 0; i < missing; i++ {
			content += "</w:t>"
		}
	}
	
	return content
}

// fixTableCellTags 修复表格单元格标签
func (fixer *AdvancedXMLFixer) fixTableCellTags(content string) string {
	// 简化的表格单元格标签修复策略
	tcOpenPattern := regexp.MustCompile(`<w:tc[^>]*>`)
	tcClosePattern := regexp.MustCompile(`</w:tc>`)
	
	openMatches := tcOpenPattern.FindAllStringIndex(content, -1)
	closeMatches := tcClosePattern.FindAllStringIndex(content, -1)
	
	// 如果开始标签多于结束标签，在末尾添加缺失的结束标签
	if len(openMatches) > len(closeMatches) {
		missing := len(openMatches) - len(closeMatches)
		for i := 0; i < missing; i++ {
			content += "</w:tc>"
		}
	}
	
	return content
}

// fixParagraphStructure 修复段落结构
func (fixer *AdvancedXMLFixer) fixParagraphStructure(content string) string {
	fmt.Println("修复段落结构...")

	// 确保每个<w:t>都在<w:r>内
	content = fixer.ensureTextInRuns(content)

	// 确保每个<w:r>都在<w:p>内
	content = fixer.ensureRunsInParagraphs(content)

	return content
}

// ensureTextInRuns 确保文本标签在运行标签内
func (fixer *AdvancedXMLFixer) ensureTextInRuns(content string) string {
	// 简化策略：查找孤立的<w:t>标签并包装在<w:r>内
	// 匹配不在<w:r>...</w:r>内的<w:t>标签
	pattern := regexp.MustCompile(`<w:t[^>]*>[^<]*</w:t>`)
	
	return pattern.ReplaceAllStringFunc(content, func(match string) string {
		// 检查这个<w:t>标签前面是否有未闭合的<w:r>标签
		// 简单策略：如果没有在<w:r>内，就包装它
		if !fixer.isTextInRun(content, match) {
			return "<w:r>" + match + "</w:r>"
		}
		return match
	})
}

// isTextInRun 检查文本是否已经在运行标签内
func (fixer *AdvancedXMLFixer) isTextInRun(content, textMatch string) bool {
	// 简单检查：如果文本前面有<w:r>且后面有</w:r>，认为已经在运行内
	index := strings.Index(content, textMatch)
	if index == -1 {
		return false
	}
	
	// 检查前面是否有<w:r>
	before := content[:index]
	lastROpen := strings.LastIndex(before, "<w:r")
	lastRClose := strings.LastIndex(before, "</w:r>")
	
	// 如果最近的<w:r>在最近的</w:r>之后，说明在运行内
	return lastROpen > lastRClose
}

// ensureRunsInParagraphs 确保运行标签在段落标签内
func (fixer *AdvancedXMLFixer) ensureRunsInParagraphs(content string) string {
	// 简化策略：查找孤立的<w:r>标签并包装在<w:p>内
	pattern := regexp.MustCompile(`<w:r[^>]*>.*?</w:r>`)
	
	return pattern.ReplaceAllStringFunc(content, func(match string) string {
		// 检查这个<w:r>标签是否在<w:p>内
		if !fixer.isRunInParagraph(content, match) {
			return "<w:p>" + match + "</w:p>"
		}
		return match
	})
}

// isRunInParagraph 检查运行是否已经在段落标签内
func (fixer *AdvancedXMLFixer) isRunInParagraph(content, runMatch string) bool {
	// 简单检查：如果运行前面有<w:p>且后面有</w:p>，认为已经在段落内
	index := strings.Index(content, runMatch)
	if index == -1 {
		return false
	}
	
	// 检查前面是否有<w:p>
	before := content[:index]
	lastPOpen := strings.LastIndex(before, "<w:p")
	lastPClose := strings.LastIndex(before, "</w:p>")
	
	// 如果最近的<w:p>在最近的</w:p>之后，说明在段落内
	return lastPOpen > lastPClose
}

// validateXMLStructure 验证XML结构
func (fixer *AdvancedXMLFixer) validateXMLStructure(content string) {
	fmt.Println("验证XML结构...")

	// 检查关键标签的匹配情况
	tags := []string{"w:p", "w:r", "w:t", "w:tbl", "w:tr", "w:tc"}

	for _, tag := range tags {
		openCount := strings.Count(content, "<"+tag+">")
		openCount += strings.Count(content, "<"+tag+" ")
		closeCount := strings.Count(content, "</"+tag+">")

		if openCount == closeCount {
			fmt.Printf("✓ %s: 开始标签 %d, 结束标签 %d (匹配)\n", tag, openCount, closeCount)
		} else {
			fmt.Printf("❌ %s: 开始标签 %d, 结束标签 %d (不匹配，差异: %d)\n", tag, openCount, closeCount, openCount-closeCount)
		}
	}
}

// escapeXML 转义XML特殊字符
func (fixer *AdvancedXMLFixer) escapeXML(text string) string {
	replacements := map[string]string{
		"&":  "&amp;",
		"<":  "&lt;",
		">":  "&gt;",
		"\"":"&quot;",
		"'":  "&apos;",
	}

	for old, new := range replacements {
		text = strings.ReplaceAll(text, old, new)
	}

	return text
}

// 配置结构
type Keyword struct {
	Key        string `json:"key"`
	Value      string `json:"value"`
	SourceFile string `json:"source_file"`
}

type Config struct {
	ProjectName string    `json:"project_name"`
	Keywords    []Keyword `json:"keywords"`
}

func main() {
	if len(os.Args) < 4 {
		fmt.Println("用法: go run advanced_xml_fixer.go <输入docx文件> <配置json文件> <输出docx文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	configFile := os.Args[2]
	outputFile := os.Args[3]

	// 读取配置文件
	configData, err := os.ReadFile(configFile)
	if err != nil {
		fmt.Printf("读取配置文件失败: %v\n", err)
		os.Exit(1)
	}

	var config Config
	if err := json.Unmarshal(configData, &config); err != nil {
		fmt.Printf("解析配置文件失败: %v\n", err)
		os.Exit(1)
	}

	// 转换关键词格式
	replaceMap := make(map[string]string)
	for _, keyword := range config.Keywords {
		replaceMap[keyword.Key] = keyword.Value
	}

	// 创建修复器
	fixer := NewAdvancedXMLFixer(replaceMap)

	// 执行修复和替换
	if err := fixer.FixAndReplaceDocx(inputFile, outputFile); err != nil {
		fmt.Printf("处理失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("✓ 文件处理完成，请用Word检查显示效果")
}