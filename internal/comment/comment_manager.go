package comment

import (
	"fmt"
	"regexp"
	"strconv"
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

// ParseComment 解析注释内容，提取替换信息
func (cm *CommentManager) ParseComment(commentText string) (*ReplacementComment, error) {
	if !cm.enabled {
		return nil, fmt.Errorf("注释追踪未启用")
	}
	
	// 匹配注释格式
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

// AddOrUpdateComment 添加或更新注释
func (cm *CommentManager) AddOrUpdateComment(keyword, value string) {
	if !cm.enabled {
		return
	}
	
	if existing, exists := cm.comments[keyword]; exists {
		// 更新现有注释
		existing.LastValue = value
		existing.ReplaceCount++
		existing.LastModified = time.Now()
	} else {
		// 添加新注释
		cm.comments[keyword] = &ReplacementComment{
			OriginalKeyword: keyword,
			LastValue:      value,
			ReplaceCount:   1,
			LastModified:   time.Now(),
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

// GetCommentCount 获取注释数量
func (cm *CommentManager) GetCommentCount() int {
	if !cm.enabled {
		return 0
	}
	return len(cm.comments)
}

// GetComments 获取所有注释
func (cm *CommentManager) GetComments() []*ReplacementComment {
	if !cm.enabled {
		return nil
	}
	
	comments := make([]*ReplacementComment, 0, len(cm.comments))
	for _, comment := range cm.comments {
		comments = append(comments, comment)
	}
	return comments
}

// ParseComments 解析文档中的注释
func (cm *CommentManager) ParseComments(xmlContent string) error {
	if !cm.enabled {
		return nil
	}
	
	// 查找所有注释
	commentPattern := fmt.Sprintf(`<!-- %s:[^>]+ -->`, cm.config.CommentFormat)
	re, err := regexp.Compile(commentPattern)
	if err != nil {
		return fmt.Errorf("编译注释正则表达式失败: %w", err)
	}
	
	matches := re.FindAllString(xmlContent, -1)
	for _, match := range matches {
		comment, err := cm.ParseComment(match)
		if err == nil {
			cm.comments[comment.OriginalKeyword] = comment
		}
	}
	
	return nil
}

// CleanupOrphanedComments 清理孤立注释
func (cm *CommentManager) CleanupOrphanedComments(xmlContent string, activeKeywords []string) string {
	if !cm.enabled || !cm.config.CleanupOrphanedComments {
		return xmlContent
	}
	
	// 创建活跃关键词映射
	activeMap := make(map[string]bool)
	for _, keyword := range activeKeywords {
		activeMap[keyword] = true
	}
	
	// 移除不活跃的注释
	for keyword := range cm.comments {
		if !activeMap[keyword] {
			delete(cm.comments, keyword)
		}
	}
	
	return xmlContent
}

// GenerateCommentXML 生成XML格式的注释
func (cm *CommentManager) GenerateCommentXML(keyword, replacement string) string {
	if !cm.enabled {
		return ""
	}
	
	// 生成XML注释格式
	commentText := cm.GenerateComment(keyword, replacement, 1)
	if commentText == "" {
		return ""
	}
	
	// 包装为XML注释
	return fmt.Sprintf("<!-- %s -->", commentText[5:len(commentText)-4]) // 移除原有的<!-- -->
}