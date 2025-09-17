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

// ReplacementConfig 替换配置
type ReplacementConfig struct {
	Replacements map[string]string `json:"replacements"`
}

// FixedReplacer 修复版替换器
type FixedReplacer struct {
	filePath string
}

// NewFixedReplacer 创建修复版替换器
func NewFixedReplacer(filePath string) *FixedReplacer {
	return &FixedReplacer{
		filePath: filePath,
	}
}

// ReplaceKeywords 替换关键词
func (fr *FixedReplacer) ReplaceKeywords(outputPath, configPath string) error {
	// 读取配置文件
	configData, err := os.ReadFile(configPath)
	if err != nil {
		return fmt.Errorf("读取配置文件失败: %v", err)
	}

	var config ReplacementConfig
	if err := json.Unmarshal(configData, &config); err != nil {
		return fmt.Errorf("解析配置文件失败: %v", err)
	}

	// 打开原始DOCX文件
	reader, err := zip.OpenReader(fr.filePath)
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

	// 处理每个文件
	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			// 处理document.xml文件
			processedContent, count, err := fr.processDocumentXML(file, config.Replacements)
			if err != nil {
				return fmt.Errorf("处理document.xml失败: %v", err)
			}
			replacementCount += count

			// 写入处理后的内容
			writer, err := zipWriter.Create(file.Name)
			if err != nil {
				return fmt.Errorf("创建文件失败: %v", err)
			}
			_, err = writer.Write([]byte(processedContent))
			if err != nil {
				return fmt.Errorf("写入文件失败: %v", err)
			}
		} else {
			// 复制其他文件
			if err := fr.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Printf("文档处理完成，总共替换了 %d 个关键词\n", replacementCount)
	return nil
}

// processDocumentXML 处理document.xml文件
func (fr *FixedReplacer) processDocumentXML(file *zip.File, replacements map[string]string) (string, int, error) {
	fileReader, err := file.Open()
	if err != nil {
		return "", 0, err
	}
	defer fileReader.Close()

	content, err := io.ReadAll(fileReader)
	if err != nil {
		return "", 0, err
	}

	xmlContent := string(content)
	replacementCount := 0

	// 对每个替换项进行处理
	for keyword, replacement := range replacements {
		count := fr.safeReplaceKeyword(xmlContent, keyword, replacement)
		if count > 0 {
			xmlContent = fr.performSafeReplace(xmlContent, keyword, replacement)
			replacementCount += count
			fmt.Printf("替换 '%s' -> '%s' (%d次)\n", keyword, replacement, count)
		}
	}

	return xmlContent, replacementCount, nil
}

// safeReplaceKeyword 安全地计算替换次数
func (fr *FixedReplacer) safeReplaceKeyword(content, keyword, replacement string) int {
	count := 0
	
	// 直接匹配
	count += strings.Count(content, keyword)
	
	// 处理被XML标签分割的情况
	cleanKeyword := strings.Trim(keyword, "#")
	if cleanKeyword != "" && cleanKeyword != keyword {
		// 构建正则表达式匹配被分割的关键词
		pattern := fmt.Sprintf(`#[^#]*?%s[^#]*?#`, regexp.QuoteMeta(cleanKeyword))
		re, err := regexp.Compile(pattern)
		if err == nil {
			matches := re.FindAllString(content, -1)
			for _, match := range matches {
				if fr.containsKeywordChars(match, cleanKeyword) {
					count++
				}
			}
		}
	}
	
	return count
}

// performSafeReplace 执行安全的替换
func (fr *FixedReplacer) performSafeReplace(content, keyword, replacement string) string {
	// 直接替换
	content = strings.ReplaceAll(content, keyword, replacement)
	
	// 处理被XML标签分割的情况
	cleanKeyword := strings.Trim(keyword, "#")
	if cleanKeyword != "" && cleanKeyword != keyword {
		pattern := fmt.Sprintf(`#[^#]*?%s[^#]*?#`, regexp.QuoteMeta(cleanKeyword))
		re, err := regexp.Compile(pattern)
		if err == nil {
			matches := re.FindAllString(content, -1)
			for _, match := range matches {
				if fr.containsKeywordChars(match, cleanKeyword) {
					content = strings.Replace(content, match, replacement, 1)
				}
			}
		}
	}
	
	return content
}

// containsKeywordChars 检查文本是否包含关键词的所有字符
func (fr *FixedReplacer) containsKeywordChars(text, keyword string) bool {
	// 移除XML标签，只保留纯文本
	cleanText := fr.removeXMLTags(text)
	// 检查是否包含完整的关键词
	return strings.Contains(cleanText, keyword)
}

// removeXMLTags 移除XML标签
func (fr *FixedReplacer) removeXMLTags(text string) string {
	re := regexp.MustCompile(`<[^>]*>`)
	return re.ReplaceAllString(text, "")
}

// copyFile 复制文件到ZIP
func (fr *FixedReplacer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
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

func main() {
	if len(os.Args) != 4 {
		fmt.Println("用法: go run fixed_replacer.go <输入文件> <输出文件> <配置文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]
	configFile := os.Args[3]

	replacer := NewFixedReplacer(inputFile)
	if err := replacer.ReplaceKeywords(outputFile, configFile); err != nil {
		fmt.Printf("替换失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("替换完成!")
}