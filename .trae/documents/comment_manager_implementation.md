# 注释管理器实现方案

## 1. 核心数据结构

### CommentManager 结构体
```go
package docx

import (
	"encoding/xml"
	"fmt"
	"regexp"
	"strconv"
	"strings"
	"time"
)

// CommentManager 管理DOCX文档中的替换注释
type CommentManager struct {
	comments map[string]*ReplacementComment
	enabled  bool
	config   *CommentConfig
}

// ReplacementComment 表示一个替换注释
type ReplacementComment struct {
	OriginalKeyword string    `xml:"original_keyword"`
	LastValue      string    `xml:"last_value"`
	ReplaceCount   int       `xml:"replace_count"`
	LastModified   time.Time `xml:"last_modified"`
	Position       CommentPosition
}

// CommentPosition 注释在文档中的位置信息
type CommentPosition struct {
	NodeID   string `xml:"node_id"`
	StartPos int    `xml:"start_pos"`
	EndPos   int    `xml:"end_pos"`
}

// CommentConfig 注释追踪配置
type CommentConfig struct {
	EnableCommentTracking   bool   `json:"enable_comment_tracking"`
	CleanupOrphanedComments bool   `json:"cleanup_orphaned_comments"`
	CommentFormat          string `json:"comment_format"`
	MaxCommentHistory      int    `json:"max_comment_history"`
}
```

## 2. 注释管理器方法实现

### 初始化和配置
```go
// NewCommentManager 创建新的注释管理器
func NewCommentManager(config *CommentConfig) *CommentManager {
	if config == nil {
		config = &CommentConfig{
			EnableCommentTracking:   false,
			CleanupOrphanedComments: false,
			CommentFormat:          "DOCX_REPLACER_ORIGINAL",
			MaxCommentHistory:      10,
		}
	}
	
	return &CommentManager{
		comments: make(map[string]*ReplacementComment),
		enabled:  config.EnableCommentTracking,
		config:   config,
	}
}

// IsEnabled 检查注释追踪是否启用
func (cm *CommentManager) IsEnabled() bool {
	return cm.enabled
}
```

### 注释解析和生成
```go
// ParseComment 解析注释内容，提取替换信息
func (cm *CommentManager) ParseComment(commentText string) (*ReplacementComment, error) {
	if !cm.enabled {
		return nil, fmt.Errorf("注释追踪未启用")
	}
	
	// 匹配注释格式: <!-- DOCX_REPLACER_ORIGINAL:#关键词# LAST_VALUE:值 REPLACE_COUNT:次数 LAST_MODIFIED:时间 -->
	pattern := fmt.Sprintf(`<!-- %s:(#[^#]+#)\s*LAST_VALUE:([^\s]*)\s*REPLACE_COUNT:(\d+)\s*LAST_MODIFIED:([^\s]+)\s*-->`, 
		cm.config.CommentFormat)
	
	re, err := regexp.Compile(pattern)
	if err != nil {
		return nil, fmt.Errorf("编译注释正则表达式失败: %w", err)
	}
	
	matches := re.FindStringSubmatch(commentText)
	if len(matches) != 5 {
		return nil, fmt.Errorf("注释格式不匹配")
	}
	
	replaceCount, err := strconv.Atoi(matches[3])
	if err != nil {
		return nil, fmt.Errorf("解析替换次数失败: %w", err)
	}
	
	lastModified, err := time.Parse(time.RFC3339, matches[4])
	if err != nil {
		return nil, fmt.Errorf("解析修改时间失败: %w", err)
	}
	
	return &ReplacementComment{
		OriginalKeyword: matches[1],
		LastValue:      matches[2],
		ReplaceCount:   replaceCount,
		LastModified:   lastModified,
	}, nil
}

// GenerateComment 生成注释内容
func (cm *CommentManager) GenerateComment(keyword, lastValue string, replaceCount int) string {
	if !cm.enabled {
		return ""
	}
	
	timestamp := time.Now().Format(time.RFC3339)
	return fmt.Sprintf("<!-- %s:%s LAST_VALUE:%s REPLACE_COUNT:%d LAST_MODIFIED:%s -->",
		cm.config.CommentFormat, keyword, lastValue, replaceCount, timestamp)
}
```

### 注释管理操作
```go
// AddComment 添加或更新注释
func (cm *CommentManager) AddComment(keyword, value string, position CommentPosition) {
	if !cm.enabled {
		return
	}
	
	if existing, exists := cm.comments[keyword]; exists {
		// 更新现有注释
		existing.LastValue = value
		existing.ReplaceCount++
		existing.LastModified = time.Now()
		existing.Position = position
	} else {
		// 添加新注释
		cm.comments[keyword] = &ReplacementComment{
			OriginalKeyword: keyword,
			LastValue:      value,
			ReplaceCount:   1,
			LastModified:   time.Now(),
			Position:       position,
		}
	}
}

// GetComment 获取指定关键词的注释信息
func (cm *CommentManager) GetComment(keyword string) (*ReplacementComment, bool) {
	if !cm.enabled {
		return nil, false
	}
	
	comment, exists := cm.comments[keyword]
	return comment, exists
}

// RemoveComment 移除指定关键词的注释
func (cm *CommentManager) RemoveComment(keyword string) {
	if !cm.enabled {
		return
	}
	
	delete(cm.comments, keyword)
}

// GetAllComments 获取所有注释
func (cm *CommentManager) GetAllComments() map[string]*ReplacementComment {
	if !cm.enabled {
		return nil
	}
	
	return cm.comments
}
```

## 3. XML文档注释处理

### 扫描文档中的注释
```go
// ScanDocumentComments 扫描文档中的所有替换注释
func (cm *CommentManager) ScanDocumentComments(xmlContent string) error {
	if !cm.enabled {
		return nil
	}
	
	// 清空现有注释
	cm.comments = make(map[string]*ReplacementComment)
	
	// 查找所有注释
	commentPattern := fmt.Sprintf(`<!-- %s:[^>]+ -->`, cm.config.CommentFormat)
	re, err := regexp.Compile(commentPattern)
	if err != nil {
		return fmt.Errorf("编译注释搜索正则表达式失败: %w", err)
	}
	
	matches := re.FindAllString(xmlContent, -1)
	for _, match := range matches {
		comment, err := cm.ParseComment(match)
		if err != nil {
			// 跳过无法解析的注释
			continue
		}
		
		cm.comments[comment.OriginalKeyword] = comment
	}
	
	return nil
}

// InjectComments 将注释注入到XML内容中
func (cm *CommentManager) InjectComments(xmlContent string) string {
	if !cm.enabled {
		return xmlContent
	}
	
	// 为每个注释找到合适的注入位置
	for keyword, comment := range cm.comments {
		commentText := cm.GenerateComment(keyword, comment.LastValue, comment.ReplaceCount)
		
		// 在替换内容后插入注释
		// 这里需要根据具体的XML结构来确定注入位置
		// 简化实现：在包含替换内容的<w:t>标签后插入
		pattern := fmt.Sprintf(`(<w:t[^>]*>%s</w:t>)`, regexp.QuoteMeta(comment.LastValue))
		re, err := regexp.Compile(pattern)
		if err != nil {
			continue
		}
		
		replacement := fmt.Sprintf("$1%s", commentText)
		xmlContent = re.ReplaceAllString(xmlContent, replacement)
	}
	
	return xmlContent
}
```

## 4. 清理和维护

### 清理孤立注释
```go
// CleanupOrphanedComments 清理孤立的注释
func (cm *CommentManager) CleanupOrphanedComments(xmlContent string, activeKeywords map[string]string) string {
	if !cm.enabled || !cm.config.CleanupOrphanedComments {
		return xmlContent
	}
	
	// 查找所有注释
	commentPattern := fmt.Sprintf(`<!-- %s:[^>]+ -->`, cm.config.CommentFormat)
	re, err := regexp.Compile(commentPattern)
	if err != nil {
		return xmlContent
	}
	
	matches := re.FindAllString(xmlContent, -1)
	for _, match := range matches {
		comment, err := cm.ParseComment(match)
		if err != nil {
			continue
		}
		
		// 如果关键词不在活跃列表中，移除注释
		if _, exists := activeKeywords[comment.OriginalKeyword]; !exists {
			xmlContent = strings.ReplaceAll(xmlContent, match, "")
		}
	}
	
	return xmlContent
}

// GetStatistics 获取注释统计信息
func (cm *CommentManager) GetStatistics() map[string]interface{} {
	if !cm.enabled {
		return map[string]interface{}{
			"enabled": false,
		}
	}
	
	totalComments := len(cm.comments)
	totalReplacements := 0
	for _, comment := range cm.comments {
		totalReplacements += comment.ReplaceCount
	}
	
	return map[string]interface{}{
		"enabled":            true,
		"total_comments":     totalComments,
		"total_replacements": totalReplacements,
		"config":            cm.config,
	}
}
```

## 5. 使用示例

```go
// 使用示例
func ExampleUsage() {
	// 创建配置
	config := &CommentConfig{
		EnableCommentTracking:   true,
		CleanupOrphanedComments: true,
		CommentFormat:          "DOCX_REPLACER_ORIGINAL",
		MaxCommentHistory:      10,
	}
	
	// 创建注释管理器
	cm := NewCommentManager(config)
	
	// 扫描现有注释
	xmlContent := "..."
	cm.ScanDocumentComments(xmlContent)
	
	// 检查是否存在注释
	if comment, exists := cm.GetComment("#产品名称#"); exists {
		fmt.Printf("找到注释: %s -> %s (替换%d次)\n", 
			comment.OriginalKeyword, comment.LastValue, comment.ReplaceCount)
	}
	
	// 添加新注释
	position := CommentPosition{NodeID: "node1", StartPos: 100, EndPos: 120}
	cm.AddComment("#产品名称#", "iPhone15", position)
	
	// 注入注释到XML
	updatedXML := cm.InjectComments(xmlContent)
	
	// 获取统计信息
	stats := cm.GetStatistics()
	fmt.Printf("注释统计: %+v\n", stats)
}
```