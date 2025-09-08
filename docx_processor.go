package main

import (
	"fmt"
	"log"
	"strings"

	"github.com/nguyenthenguyen/docx"
)

// DocxProcessor 处理docx文件的结构体
type DocxProcessor struct {
	reader           *docx.ReplaceDocx
	editable         *docx.Docx
	replacementCount map[string]int
}

// NewDocxProcessor 创建新的DocxProcessor实例
func NewDocxProcessor(filePath string) (*DocxProcessor, error) {
	reader, err := docx.ReadDocxFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("打开docx文件失败: %v", err)
	}

	editable := reader.Editable()

	return &DocxProcessor{
		reader:           reader,
		editable:         editable,
		replacementCount: make(map[string]int),
	}, nil
}

// ReplaceKeywords 替换文档中的关键词
func (dp *DocxProcessor) ReplaceKeywords(replacements map[string]string, verbose bool) error {
	if dp.editable == nil {
		return fmt.Errorf("文档未初始化")
	}

	// 初始化替换计数器
	dp.replacementCount = make(map[string]int)

	// 使用nguyenthenguyen/docx库进行替换
	for oldText, replacement := range replacements {
		// 获取替换前的内容以计算替换次数
		content := dp.editable.GetContent()
		count := 0
		for i := 0; i < len(content); {
			index := strings.Index(content[i:], oldText)
			if index == -1 {
				break
			}
			count++
			i += index + len(oldText)
		}

		if count > 0 {
			dp.replacementCount[oldText] = count
			// 执行替换（-1表示替换所有匹配项）
			wrappedOldText := "#" + oldText + "#"
			err := dp.editable.Replace(wrappedOldText, replacement, -1)
			if err != nil {
				return fmt.Errorf("替换文本失败: %v", err)
			}
			// 同时替换页眉和页脚
			err = dp.editable.ReplaceHeader(wrappedOldText, replacement)
			if err != nil {
				log.Printf("替换页眉失败: %v", err)
			}
			err = dp.editable.ReplaceFooter(wrappedOldText, replacement)
			if err != nil {
				log.Printf("替换页脚失败: %v", err)
			}

			if verbose {
				log.Printf("替换 '%s' -> '%s' (%d次)", oldText, replacement, count)
			}
		}
	}

	return nil
}

// SaveAs 保存文档到指定路径
func (dp *DocxProcessor) SaveAs(filePath string) error {
	if dp.editable == nil {
		return fmt.Errorf("文档未初始化")
	}

	return dp.editable.WriteToFile(filePath)
}

// Close 关闭文档
func (dp *DocxProcessor) Close() error {
	if dp.reader != nil {
		err := dp.reader.Close()
		dp.reader = nil
		dp.editable = nil
		return err
	}
	return nil
}

// GetReplacementCount 获取关键词替换次数统计
func (dp *DocxProcessor) GetReplacementCount() map[string]int {
	return dp.replacementCount
}
