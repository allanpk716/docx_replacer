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

// Keyword 关键词结构
type Keyword struct {
	Key        string `json:"key"`
	Value      string `json:"value"`
	SourceFile string `json:"source_file"`
}

// Config 配置结构
type Config struct {
	ProjectName string    `json:"project_name"`
	Keywords    []Keyword `json:"keywords"`
}

// ConservativeReplacer 保守的替换器
type ConservativeReplacer struct {
	replacements map[string]string
}

// NewConservativeReplacer 创建新的保守替换器
func NewConservativeReplacer(replacements map[string]string) *ConservativeReplacer {
	return &ConservativeReplacer{
		replacements: replacements,
	}
}

// ReplaceInDocx 在docx文件中进行保守替换
func (cr *ConservativeReplacer) ReplaceInDocx(inputPath, outputPath string) error {
	fmt.Printf("开始处理文件: %s\n", inputPath)

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
		if err := cr.processFile(file, writer); err != nil {
			return fmt.Errorf("处理文件 %s 失败: %v", file.Name, err)
		}
	}

	fmt.Printf("文件处理完成: %s\n", outputPath)
	return nil
}

// processFile 处理单个文件
func (cr *ConservativeReplacer) processFile(file *zip.File, writer *zip.Writer) error {
	// 打开源文件
	src, err := file.Open()
	if err != nil {
		return err
	}
	defer src.Close()

	// 创建目标文件
	dst, err := writer.Create(file.Name)
	if err != nil {
		return err
	}

	// 如果是document.xml，进行文本替换
	if file.Name == "word/document.xml" {
		fmt.Println("处理document.xml...")
		return cr.replaceInDocumentXML(src, dst)
	}

	// 其他文件直接复制
	_, err = io.Copy(dst, src)
	return err
}

// replaceInDocumentXML 在document.xml中进行替换
func (cr *ConservativeReplacer) replaceInDocumentXML(src io.Reader, dst io.Writer) error {
	// 读取整个文件内容
	content, err := io.ReadAll(src)
	if err != nil {
		return err
	}

	xmlContent := string(content)

	// 使用正则表达式匹配<w:t>标签内的内容
	// 这个正则表达式会匹配 <w:t>内容</w:t> 或 <w:t xml:space="preserve">内容</w:t>
	textTagRegex := regexp.MustCompile(`(<w:t[^>]*>)([^<]*)(</w:t>)`)

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
		replacedText := cr.replaceText(textContent)

		// 如果文本发生了变化，记录替换
		if replacedText != textContent {
			fmt.Printf("替换文本: '%s' -> '%s'\n", textContent, replacedText)
		}

		return openingTag + replacedText + closingTag
	}

	// 执行替换
	replacedContent := textTagRegex.ReplaceAllStringFunc(xmlContent, replaceFunc)

	// 写入结果
	_, err = dst.Write([]byte(replacedContent))
	return err
}

// replaceText 替换文本内容
func (cr *ConservativeReplacer) replaceText(text string) string {
	replacedText := text
	for oldText, newText := range cr.replacements {
		replacedText = strings.ReplaceAll(replacedText, oldText, newText)
	}
	return replacedText
}

// escapeXML 转义XML特殊字符
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

func main() {
	if len(os.Args) != 4 {
		fmt.Println("用法: go run conservative_replacer.go <输入docx文件> <配置json文件> <输出docx文件>")
		os.Exit(1)
	}

	inputPath := os.Args[1]
	configPath := os.Args[2]
	outputPath := os.Args[3]

	// 读取配置文件
	configData, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Printf("读取配置文件失败: %v\n", err)
		os.Exit(1)
	}

	var config Config
	if err := json.Unmarshal(configData, &config); err != nil {
		fmt.Printf("解析配置文件失败: %v\n", err)
		os.Exit(1)
	}

	// 构建替换映射
	replacements := make(map[string]string)
	for _, keyword := range config.Keywords {
		replacements[keyword.Key] = keyword.Value
		fmt.Printf("加载替换规则: %s -> %s\n", keyword.Key, keyword.Value)
	}

	// 创建保守替换器
	replacer := NewConservativeReplacer(replacements)

	// 执行替换
	if err := replacer.ReplaceInDocx(inputPath, outputPath); err != nil {
		fmt.Printf("处理失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("✓ 文件处理完成，请用Word检查显示效果")
}