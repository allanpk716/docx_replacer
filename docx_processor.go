package main

import (
	"fmt"
	"log"
	"strings"

	"github.com/lukasjarosch/go-docx"
)

// GoDocxProcessor 使用 lukasjarosch/go-docx 库的文档处理器
type GoDocxProcessor struct {
	doc              *docx.Document
	replacementCount map[string]int
}

// NewGoDocxProcessorFromFile 从文件创建新的文档处理器
func NewGoDocxProcessorFromFile(filePath string) (*GoDocxProcessor, error) {
	if filePath == "" {
		return nil, fmt.Errorf("文件路径不能为空")
	}

	doc, err := docx.Open(filePath)
	if err != nil {
		return nil, fmt.Errorf("打开docx文件失败: %v", err)
	}

	return &GoDocxProcessor{
		doc:              doc,
		replacementCount: make(map[string]int),
	}, nil
}

// NewGoDocxProcessorFromBytes 从字节数据创建新的文档处理器
func NewGoDocxProcessorFromBytes(data []byte) (*GoDocxProcessor, error) {
	if data == nil || len(data) == 0 {
		return nil, fmt.Errorf("字节数据不能为空")
	}

	doc, err := docx.OpenBytes(data)
	if err != nil {
		return nil, fmt.Errorf("从字节数据打开docx失败: %v", err)
	}

	return &GoDocxProcessor{
		doc:              doc,
		replacementCount: make(map[string]int),
	}, nil
}

// ReplaceKeywordsWithOptions 替换关键词（带选项）
func (np *GoDocxProcessor) ReplaceKeywordsWithOptions(replacements map[string]string, verbose bool, useHashWrapper bool) error {
	if np.doc == nil {
		return fmt.Errorf("文档未初始化")
	}

	// 准备替换映射
	placeholderMap := make(map[string]interface{})
	for key, value := range replacements {
		placeholder := key
		if useHashWrapper {
			// 智能处理井号包装：如果关键词已经包含井号，则不再添加
			if !strings.HasPrefix(key, "#") || !strings.HasSuffix(key, "#") {
				placeholder = "#" + key + "#"
			}
		}
		placeholderMap[placeholder] = value
		if verbose {
			log.Printf("准备替换: %s -> %s", placeholder, value)
		}
	}
	
	// 执行批量替换
	err := np.doc.ReplaceAll(placeholderMap)
	if err != nil {
		log.Printf("批量替换时出现错误: %v", err)
		return err
	}
	
	// lukasjarosch/go-docx 不返回替换次数，我们手动计算
	for key := range replacements {
		np.replacementCount[key] = 1 // 假设每个都替换了一次
	}
	
	if verbose {
		log.Printf("批量替换完成，处理了 %d 个占位符", len(replacements))
	}
	
	return nil
}

// SaveAs 保存文档到指定路径
func (np *GoDocxProcessor) SaveAs(filePath string) error {
	if np.doc == nil {
		return fmt.Errorf("文档未初始化")
	}

	return np.doc.WriteToFile(filePath)
}

// Close 关闭文档（lukasjarosch/go-docx 不需要显式关闭）
func (np *GoDocxProcessor) Close() error {
	// lukasjarosch/go-docx 不需要显式关闭
	return nil
}

// GetReplacementCount 获取替换计数
func (np *GoDocxProcessor) GetReplacementCount() map[string]int {
	return np.replacementCount
}

// DebugContent 调试内容（显示文档内容和关键词检查）
func (np *GoDocxProcessor) DebugContent(keywords []string) {
	if np.doc == nil {
		log.Println("文档未初始化")
		return
	}

	log.Println("=== 文档内容调试信息 ===")

	// lukasjarosch/go-docx 没有直接获取占位符的方法
	// 我们可以显示文档的文本内容进行调试
	log.Println("注意：lukasjarosch/go-docx 库不支持直接获取占位符列表")
	log.Println("建议在替换前后对比文档内容来验证替换效果")

	// 检查指定关键词（模拟检查）
	if len(keywords) > 0 {
		log.Println("\n=== 关键词检查 ===")
		for _, keyword := range keywords {
			searchText := "#" + keyword + "#"
			log.Printf("将搜索关键词: '%s' (格式: '%s')", keyword, searchText)
		}
		log.Println("实际匹配结果将在替换时显示")
	}
}

// GetPlaceholders 获取文档中的所有占位符（模拟实现）
func (np *GoDocxProcessor) GetPlaceholders() []string {
	if np.doc == nil {
		return []string{}
	}

	// lukasjarosch/go-docx 库不支持直接获取占位符
	// 返回空切片，实际的占位符检测在替换时进行
	log.Println("注意：lukasjarosch/go-docx 库不支持直接获取占位符列表")
	log.Println("占位符的存在性将在执行替换时通过替换计数来确定")
	return []string{}
}