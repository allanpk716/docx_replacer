package main

import (
	"archive/zip"
	"bytes"
	"fmt"
	"io"
	"log"
	"os"
	"regexp"
	"strings"
)

// AdvancedDocxProcessor 高级docx处理器，专门处理表格中被分割的关键词
type AdvancedDocxProcessor struct {
	filePath         string
	zipReader        *zip.ReadCloser
	documentXML      string
	replacementCount map[string]int
}

// NewAdvancedDocxProcessor 创建高级处理器
func NewAdvancedDocxProcessor(filePath string) (*AdvancedDocxProcessor, error) {
	zipReader, err := zip.OpenReader(filePath)
	if err != nil {
		return nil, fmt.Errorf("打开docx文件失败: %v", err)
	}

	// 读取document.xml文件
	documentXML, err := readDocumentXML(zipReader)
	if err != nil {
		zipReader.Close()
		return nil, fmt.Errorf("读取document.xml失败: %v", err)
	}

	return &AdvancedDocxProcessor{
		filePath:         filePath,
		zipReader:        zipReader,
		documentXML:      documentXML,
		replacementCount: make(map[string]int),
	}, nil
}

// readDocumentXML 从zip文件中读取document.xml内容
func readDocumentXML(zipReader *zip.ReadCloser) (string, error) {
	for _, file := range zipReader.File {
		if file.Name == "word/document.xml" {
			rc, err := file.Open()
			if err != nil {
				return "", err
			}
			defer rc.Close()

			content, err := io.ReadAll(rc)
			if err != nil {
				return "", err
			}
			return string(content), nil
		}
	}
	return "", fmt.Errorf("document.xml not found")
}

// TableAwareReplace 表格感知的关键词替换
func (adp *AdvancedDocxProcessor) TableAwareReplace(replacements map[string]string, verbose bool, useHashWrapper bool) error {
	modifiedXML := adp.documentXML
	adp.replacementCount = make(map[string]int)

	for key, value := range replacements {
		placeholder := key
		if useHashWrapper {
			if !strings.HasPrefix(key, "#") || !strings.HasSuffix(key, "#") {
				placeholder = "#" + key + "#"
			}
		}

		if verbose {
			log.Printf("开始处理关键词: %s -> %s", placeholder, value)
		}

		// 使用高级算法处理被分割的关键词
		newXML, count := adp.advancedReplace(modifiedXML, placeholder, value, verbose)
		modifiedXML = newXML
		adp.replacementCount[key] = count

		if verbose {
			log.Printf("关键词 '%s' 替换了 %d 次", placeholder, count)
		}
	}

	adp.documentXML = modifiedXML
	return nil
}

// advancedReplace 高级替换算法，处理被XML标签分割的关键词
func (adp *AdvancedDocxProcessor) advancedReplace(content, placeholder, replacement string, verbose bool) (string, int) {
	count := 0
	result := content

	// 1. 首先尝试直接替换（处理未被分割的情况）
	directCount := strings.Count(result, placeholder)
	if directCount > 0 {
		result = strings.ReplaceAll(result, placeholder, replacement)
		count += directCount
		if verbose {
			log.Printf("直接替换: %d 次", directCount)
		}
	}

	// 2. 处理被XML标签分割的情况
	// 查找表格行 <w:tr>...</w:tr>
	trPattern := regexp.MustCompile(`<w:tr[^>]*>.*?</w:tr>`)
	trMatches := trPattern.FindAllString(result, -1)

	for _, trMatch := range trMatches {
		// 在每个表格行中查找被分割的关键词
		newTrContent, trCount := adp.processSplitKeywords(trMatch, placeholder, replacement, verbose)
		if trCount > 0 {
			result = strings.Replace(result, trMatch, newTrContent, 1)
			count += trCount
		}
	}

	// 3. 处理段落中被分割的情况
	pPattern := regexp.MustCompile(`<w:p[^>]*>.*?</w:p>`)
	pMatches := pPattern.FindAllString(result, -1)

	for _, pMatch := range pMatches {
		newPContent, pCount := adp.processSplitKeywords(pMatch, placeholder, replacement, verbose)
		if pCount > 0 {
			result = strings.Replace(result, pMatch, newPContent, 1)
			count += pCount
		}
	}

	return result, count
}

// processSplitKeywords 处理被分割的关键词
func (adp *AdvancedDocxProcessor) processSplitKeywords(content, placeholder, replacement string, verbose bool) (string, int) {
	count := 0
	result := content

	// 提取所有文本片段
	textPattern := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
	textMatches := textPattern.FindAllStringSubmatch(result, -1)

	if len(textMatches) == 0 {
		return result, 0
	}

	// 重建完整文本
	fullText := ""
	for _, match := range textMatches {
		fullText += match[1]
	}

	if verbose {
		log.Printf("提取的完整文本: '%s'", fullText)
	}

	// 检查是否包含目标关键词
	if !strings.Contains(fullText, placeholder) {
		return result, 0
	}

	if verbose {
		log.Printf("发现被分割的关键词: %s", placeholder)
	}

	// 执行替换
	newFullText := strings.ReplaceAll(fullText, placeholder, replacement)
	count = strings.Count(fullText, placeholder)

	// 重新分配文本到XML标签中
	newResult := adp.redistributeText(result, textMatches, newFullText, verbose)

	return newResult, count
}

// redistributeText 将替换后的文本重新分配到XML标签中
func (adp *AdvancedDocxProcessor) redistributeText(originalContent string, textMatches [][]string, newText string, verbose bool) string {
	if len(textMatches) == 0 || newText == "" {
		return originalContent
	}

	result := originalContent

	// 简单策略：将新文本放在第一个<w:t>标签中，清空其他标签
	for i, match := range textMatches {
		oldTag := match[0]
		if i == 0 {
			// 第一个标签包含所有新文本
			newTag := strings.Replace(oldTag, match[1], newText, 1)
			result = strings.Replace(result, oldTag, newTag, 1)
			if verbose {
				log.Printf("更新第一个标签: %s -> %s", oldTag, newTag)
			}
		} else {
			// 其他标签清空
			newTag := strings.Replace(oldTag, match[1], "", 1)
			result = strings.Replace(result, oldTag, newTag, 1)
			if verbose {
				log.Printf("清空标签: %s -> %s", oldTag, newTag)
			}
		}
	}

	return result
}

// SaveAs 保存修改后的文档
func (adp *AdvancedDocxProcessor) SaveAs(outputPath string) error {
	// 创建新的zip文件
	var buf bytes.Buffer
	zipWriter := zip.NewWriter(&buf)

	// 复制所有文件，除了document.xml
	for _, file := range adp.zipReader.File {
		if file.Name == "word/document.xml" {
			// 写入修改后的document.xml
			w, err := zipWriter.Create(file.Name)
			if err != nil {
				return err
			}
			_, err = w.Write([]byte(adp.documentXML))
			if err != nil {
				return err
			}
		} else {
			// 复制其他文件
			w, err := zipWriter.Create(file.Name)
			if err != nil {
				return err
			}

			rc, err := file.Open()
			if err != nil {
				return err
			}

			_, err = io.Copy(w, rc)
			rc.Close()
			if err != nil {
				return err
			}
		}
	}

	err := zipWriter.Close()
	if err != nil {
		return err
	}

	// 写入文件
	return os.WriteFile(outputPath, buf.Bytes(), 0644)
}

// Close 关闭处理器
func (adp *AdvancedDocxProcessor) Close() error {
	if adp.zipReader != nil {
		return adp.zipReader.Close()
	}
	return nil
}

// GetReplacementCount 获取替换计数
func (adp *AdvancedDocxProcessor) GetReplacementCount() map[string]int {
	return adp.replacementCount
}

// DebugContent 调试内容
func (adp *AdvancedDocxProcessor) DebugContent(keywords []string) {
	log.Println("=== 高级处理器调试信息 ===")
	log.Printf("文档XML长度: %d 字符", len(adp.documentXML))
	
	for _, keyword := range keywords {
		placeholder := "#" + keyword + "#"
		directCount := strings.Count(adp.documentXML, placeholder)
		log.Printf("关键词 '%s' 直接匹配: %d 次", placeholder, directCount)
		
		// 分析可能被分割的情况
		adp.analyzeFragmentedKeyword(keyword)
	}
}

// analyzeFragmentedKeyword 分析被分割的关键词
func (adp *AdvancedDocxProcessor) analyzeFragmentedKeyword(keyword string) {
	placeholder := "#" + keyword + "#"
	
	// 查找包含关键词片段的文本标签
	textPattern := regexp.MustCompile(`<w:t[^>]*>([^<]*)</w:t>`)
	textMatches := textPattern.FindAllStringSubmatch(adp.documentXML, -1)
	
	fragments := make([]string, 0)
	for _, match := range textMatches {
		text := match[1]
		if text != "" && strings.Contains(placeholder, text) {
			fragments = append(fragments, text)
		}
	}
	
	if len(fragments) > 0 {
		log.Printf("关键词 '%s' 可能的片段: %v", placeholder, fragments)
	}
}