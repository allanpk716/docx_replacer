# 增强XML处理器实现方案

## 1. 增强的XML处理器结构

```go
package docx

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
	"time"
)

// EnhancedXMLProcessor 增强的XML处理器，支持注释追踪
type EnhancedXMLProcessor struct {
	*XMLProcessor
	commentManager *CommentManager
	config         *CommentConfig
	result         *ReplacementResult
}

// ReplacementResult 替换结果统计
type ReplacementResult struct {
	Success           bool
	ReplacedCount     int
	CommentBasedCount int
	TraditionalCount  int
	FailedKeywords    []string
	Warnings          []string
	ProcessingTime    time.Duration
}

// NewEnhancedXMLProcessor 创建增强的XML处理器
func NewEnhancedXMLProcessor(filePath string, config *CommentConfig) *EnhancedXMLProcessor {
	return &EnhancedXMLProcessor{
		XMLProcessor:   NewXMLProcessor(filePath),
		commentManager: NewCommentManager(config),
		config:         config,
		result: &ReplacementResult{
			FailedKeywords: make([]string, 0),
			Warnings:       make([]string, 0),
		},
	}
}
```

## 2. 核心替换方法实现

### 主替换方法
```go
// ReplaceKeywordsWithComments 使用注释追踪进行关键词替换
func (exp *EnhancedXMLProcessor) ReplaceKeywordsWithComments(replacements map[string]string, outputPath string) (*ReplacementResult, error) {
	startTime := time.Now()
	defer func() {
		exp.result.ProcessingTime = time.Since(startTime)
	}()
	
	// 读取DOCX文件
	reader, err := zip.OpenReader(exp.filePath)
	if err != nil {
		return exp.result, fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()
	
	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return exp.result, fmt.Errorf("创建输出文件失败: %v", err)
	}
	defer outputFile.Close()
	
	// 创建ZIP写入器
	zipWriter := zip.NewWriter(outputFile)
	defer zipWriter.Close()
	
	// 处理ZIP文件中的所有文件
	for _, file := range reader.File {
		if err := exp.processZipFile(file, zipWriter, replacements); err != nil {
			exp.result.Warnings = append(exp.result.Warnings, 
				fmt.Sprintf("处理文件 %s 时出现警告: %v", file.Name, err))
		}
	}
	
	exp.result.Success = true
	fmt.Printf("文档处理完成，总共替换了 %d 个关键词（注释追踪: %d, 传统匹配: %d）\n", 
		exp.result.ReplacedCount, exp.result.CommentBasedCount, exp.result.TraditionalCount)
	
	return exp.result, nil
}

// processZipFile 处理ZIP文件中的单个文件
func (exp *EnhancedXMLProcessor) processZipFile(file *zip.File, zipWriter *zip.Writer, replacements map[string]string) error {
	// 读取文件内容
	fileReader, err := file.Open()
	if err != nil {
		return fmt.Errorf("打开文件失败: %w", err)
	}
	defer fileReader.Close()
	
	content, err := io.ReadAll(fileReader)
	if err != nil {
		return fmt.Errorf("读取文件失败: %w", err)
	}
	
	// 如果是document.xml文件，进行关键词替换
	if file.Name == "word/document.xml" {
		modifiedContent, err := exp.processDocumentXML(string(content), replacements)
		if err != nil {
			return fmt.Errorf("处理document.xml失败: %w", err)
		}
		content = []byte(modifiedContent)
	}
	
	// 写入到新的ZIP文件
	writer, err := zipWriter.CreateHeader(&file.FileHeader)
	if err != nil {
		return fmt.Errorf("创建ZIP文件头失败: %w", err)
	}
	
	_, err = writer.Write(content)
	if err != nil {
		return fmt.Errorf("写入文件内容失败: %w", err)
	}
	
	return nil
}
```

### 文档XML处理
```go
// processDocumentXML 处理document.xml文件
func (exp *EnhancedXMLProcessor) processDocumentXML(xmlContent string, replacements map[string]string) (string, error) {
	// 1. 扫描现有注释
	if exp.commentManager.IsEnabled() {
		if err := exp.commentManager.ScanDocumentComments(xmlContent); err != nil {
			exp.result.Warnings = append(exp.result.Warnings, 
				fmt.Sprintf("扫描注释失败: %v", err))
		}
	}
	
	// 2. 执行替换
	modifiedContent := xmlContent
	for keyword, replacement := range replacements {
		var replaced bool
		var err error
		
		// 优先尝试基于注释的替换
		if exp.commentManager.IsEnabled() {
			modifiedContent, replaced, err = exp.replaceWithComment(modifiedContent, keyword, replacement)
			if err != nil {
				exp.result.Warnings = append(exp.result.Warnings, 
					fmt.Sprintf("注释替换失败 %s: %v", keyword, err))
			}
			if replaced {
				exp.result.CommentBasedCount++
				exp.result.ReplacedCount++
				continue
			}
		}
		
		// 回退到传统替换
		modifiedContent, replaced = exp.replaceTraditional(modifiedContent, keyword, replacement)
		if replaced {
			exp.result.TraditionalCount++
			exp.result.ReplacedCount++
			
			// 为新替换添加注释
			if exp.commentManager.IsEnabled() {
				position := CommentPosition{NodeID: "auto", StartPos: 0, EndPos: 0}
				exp.commentManager.AddComment(keyword, replacement, position)
			}
		} else {
			exp.result.FailedKeywords = append(exp.result.FailedKeywords, keyword)
		}
	}
	
	// 3. 注入注释到XML
	if exp.commentManager.IsEnabled() {
		modifiedContent = exp.commentManager.InjectComments(modifiedContent)
		
		// 4. 清理孤立注释
		modifiedContent = exp.commentManager.CleanupOrphanedComments(modifiedContent, replacements)
	}
	
	return modifiedContent, nil
}
```

## 3. 替换策略实现

### 基于注释的替换
```go
// replaceWithComment 基于注释进行精确替换
func (exp *EnhancedXMLProcessor) replaceWithComment(content, keyword, replacement string) (string, bool, error) {
	// 获取注释信息
	comment, exists := exp.commentManager.GetComment(keyword)
	if !exists {
		return content, false, nil
	}
	
	// 查找注释在XML中的位置
	commentText := exp.commentManager.GenerateComment(keyword, comment.LastValue, comment.ReplaceCount)
	commentIndex := strings.Index(content, commentText)
	if commentIndex == -1 {
		return content, false, fmt.Errorf("未找到注释: %s", keyword)
	}
	
	// 在注释前查找对应的替换内容
	// 构建搜索模式：查找包含上次替换值的<w:t>标签
	pattern := fmt.Sprintf(`<w:t[^>]*>([^<]*%s[^<]*)</w:t>`, regexp.QuoteMeta(comment.LastValue))
	re, err := regexp.Compile(pattern)
	if err != nil {
		return content, false, fmt.Errorf("编译搜索正则表达式失败: %w", err)
	}
	
	// 在注释位置之前搜索
	searchArea := content[:commentIndex]
	matches := re.FindAllStringSubmatch(searchArea, -1)
	if len(matches) == 0 {
		return content, false, fmt.Errorf("未找到对应的替换内容")
	}
	
	// 使用最后一个匹配项（最接近注释的）
	lastMatch := matches[len(matches)-1]
	if len(lastMatch) < 2 {
		return content, false, fmt.Errorf("匹配格式错误")
	}
	
	// 执行替换
	oldText := lastMatch[0]
	newText := strings.Replace(oldText, comment.LastValue, replacement, 1)
	updatedContent := strings.Replace(content, oldText, newText, 1)
	
	// 更新注释信息
	position := CommentPosition{NodeID: "comment_based", StartPos: commentIndex, EndPos: commentIndex}
	exp.commentManager.AddComment(keyword, replacement, position)
	
	fmt.Printf("基于注释替换 '%s': '%s' -> '%s'\n", keyword, comment.LastValue, replacement)
	return updatedContent, true, nil
}
```

### 传统替换方法
```go
// replaceTraditional 传统的关键词替换
func (exp *EnhancedXMLProcessor) replaceTraditional(content, keyword, replacement string) (string, bool) {
	// 直接替换完整的关键词
	if strings.Contains(content, keyword) {
		count := strings.Count(content, keyword)
		content = strings.ReplaceAll(content, keyword, replacement)
		fmt.Printf("传统替换 '%s' -> '%s' (%d次)\n", keyword, replacement, count)
		return content, true
	}
	
	// 处理被XML标签分割的关键词
	modifiedContent, splitCount := exp.handleSplitKeyword(content, keyword, replacement)
	if splitCount > 0 {
		fmt.Printf("分割关键词替换 '%s' -> '%s' (%d次)\n", keyword, replacement, splitCount)
		return modifiedContent, true
	}
	
	return content, false
}

// handleSplitKeyword 处理被XML标签分割的关键词（继承原有逻辑）
func (exp *EnhancedXMLProcessor) handleSplitKeyword(content, keyword, replacement string) (string, int) {
	replacementCount := 0
	
	// 移除关键词中的#符号来构建搜索模式
	cleanKeyword := strings.Trim(keyword, "#")
	if cleanKeyword == "" {
		return content, 0
	}
	
	// 构建正则表达式来匹配被XML标签分割的关键词
	pattern := fmt.Sprintf(`#[^#]*?%s[^#]*?#`, regexp.QuoteMeta(cleanKeyword))
	re, err := regexp.Compile(pattern)
	if err != nil {
		return content, 0
	}
	
	// 查找所有匹配项
	matches := re.FindAllString(content, -1)
	for _, match := range matches {
		// 检查匹配项是否包含完整的关键词字符
		if exp.containsKeywordChars(match, cleanKeyword) {
			// 替换整个匹配项
			content = strings.Replace(content, match, replacement, 1)
			replacementCount++
		}
	}
	
	return content, replacementCount
}

// containsKeywordChars 检查文本是否包含关键词的所有字符
func (exp *EnhancedXMLProcessor) containsKeywordChars(text, keyword string) bool {
	// 移除XML标签，只保留纯文本
	cleanText := exp.removeXMLTags(text)
	// 检查是否包含完整的关键词
	return strings.Contains(cleanText, keyword)
}

// removeXMLTags 移除XML标签
func (exp *EnhancedXMLProcessor) removeXMLTags(text string) string {
	re := regexp.MustCompile(`<[^>]*>`)
	return re.ReplaceAllString(text, "")
}
```

## 4. 配置和统计方法

### 配置管理
```go
// UpdateConfig 更新配置
func (exp *EnhancedXMLProcessor) UpdateConfig(config *CommentConfig) {
	exp.config = config
	exp.commentManager = NewCommentManager(config)
}

// GetConfig 获取当前配置
func (exp *EnhancedXMLProcessor) GetConfig() *CommentConfig {
	return exp.config
}

// IsCommentTrackingEnabled 检查注释追踪是否启用
func (exp *EnhancedXMLProcessor) IsCommentTrackingEnabled() bool {
	return exp.commentManager.IsEnabled()
}
```

### 统计和调试
```go
// GetReplacementResult 获取替换结果
func (exp *EnhancedXMLProcessor) GetReplacementResult() *ReplacementResult {
	return exp.result
}

// GetCommentStatistics 获取注释统计信息
func (exp *EnhancedXMLProcessor) GetCommentStatistics() map[string]interface{} {
	return exp.commentManager.GetStatistics()
}

// PrintDetailedReport 打印详细报告
func (exp *EnhancedXMLProcessor) PrintDetailedReport() {
	fmt.Println("\n=== 替换详细报告 ===")
	fmt.Printf("处理时间: %v\n", exp.result.ProcessingTime)
	fmt.Printf("总替换次数: %d\n", exp.result.ReplacedCount)
	fmt.Printf("  - 基于注释: %d\n", exp.result.CommentBasedCount)
	fmt.Printf("  - 传统匹配: %d\n", exp.result.TraditionalCount)
	
	if len(exp.result.FailedKeywords) > 0 {
		fmt.Printf("未匹配关键词: %v\n", exp.result.FailedKeywords)
	}
	
	if len(exp.result.Warnings) > 0 {
		fmt.Println("警告信息:")
		for _, warning := range exp.result.Warnings {
			fmt.Printf("  - %s\n", warning)
		}
	}
	
	if exp.commentManager.IsEnabled() {
		stats := exp.commentManager.GetStatistics()
		fmt.Printf("注释统计: %+v\n", stats)
	}
	fmt.Println("========================")
}
```

## 5. 向后兼容性包装

```go
// ReplaceKeywords 向后兼容的替换方法
func (exp *EnhancedXMLProcessor) ReplaceKeywords(replacements map[string]string, outputPath string) error {
	result, err := exp.ReplaceKeywordsWithComments(replacements, outputPath)
	if err != nil {
		return err
	}
	
	if !result.Success {
		return fmt.Errorf("替换操作未成功完成")
	}
	
	return nil
}

// CreateCompatibleProcessor 创建兼容的处理器（不启用注释追踪）
func CreateCompatibleProcessor(filePath string) *EnhancedXMLProcessor {
	config := &CommentConfig{
		EnableCommentTracking: false,
	}
	return NewEnhancedXMLProcessor(filePath, config)
}
```

## 6. 使用示例

```go
// 使用示例
func ExampleEnhancedUsage() {
	// 创建配置
	config := &CommentConfig{
		EnableCommentTracking:   true,
		CleanupOrphanedComments: true,
		CommentFormat:          "DOCX_REPLACER_ORIGINAL",
		MaxCommentHistory:      10,
	}
	
	// 创建增强处理器
	processor := NewEnhancedXMLProcessor("input.docx", config)
	
	// 定义替换映射
	replacements := map[string]string{
		"#产品名称#": "iPhone15",
		"#版本号#":   "v2.0",
		"#公司名称#": "Apple Inc.",
	}
	
	// 执行替换
	result, err := processor.ReplaceKeywordsWithComments(replacements, "output.docx")
	if err != nil {
		fmt.Printf("替换失败: %v\n", err)
		return
	}
	
	// 打印详细报告
	processor.PrintDetailedReport()
	
	// 获取统计信息
	stats := processor.GetCommentStatistics()
	fmt.Printf("注释统计: %+v\n", stats)
}
```