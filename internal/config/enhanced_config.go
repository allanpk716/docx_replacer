package config

import (
	"encoding/json"
	"fmt"
	"os"
	"time"
)

// EnhancedKeyword 表示一个增强的关键词配置项
type EnhancedKeyword struct {
	Key        string `json:"key"`
	Value      string `json:"value"`
	SourceFile string `json:"source_file,omitempty"`
	Enabled    *bool  `json:"enabled,omitempty"`
	Category   string `json:"category,omitempty"`
}

// CommentTracking 注释追踪配置
type CommentTracking struct {
	EnableCommentTracking   bool   `json:"enable_comment_tracking"`
	CleanupOrphanedComments bool   `json:"cleanup_orphaned_comments"`
	CommentFormat          string `json:"comment_format"`
	MaxCommentHistory      int    `json:"max_comment_history"`
	AutoBackup             bool   `json:"auto_backup"`
}

// ProcessingConfig 处理配置
type ProcessingConfig struct {
	EnableDetailedLogging bool     `json:"enable_detailed_logging"`
	MaxConcurrentFiles   int      `json:"max_concurrent_files"`
	BackupOriginal       bool     `json:"backup_original"`
	OutputSuffix         string   `json:"output_suffix"`
	ExcludePatterns      []string `json:"exclude_patterns,omitempty"`
}

// EnhancedConfig 表示完整的增强配置文件结构
type EnhancedConfig struct {
	ProjectName      string            `json:"project_name"`
	Keywords         []EnhancedKeyword `json:"keywords"`
	CommentTracking  *CommentTracking  `json:"comment_tracking,omitempty"`
	ProcessingConfig *ProcessingConfig `json:"processing_config,omitempty"`
	Version          string            `json:"version,omitempty"`
	CreatedAt        *time.Time        `json:"created_at,omitempty"`
	UpdatedAt        *time.Time        `json:"updated_at,omitempty"`
}

// MigrationHandler 配置迁移处理器
type MigrationHandler func(*EnhancedConfig) error

// EnhancedConfigManager 增强的配置管理器
type EnhancedConfigManager struct {
	migrationHandlers map[string]MigrationHandler
}

// NewEnhancedConfigManager 创建增强的配置管理器
func NewEnhancedConfigManager() *EnhancedConfigManager {
	ecm := &EnhancedConfigManager{
		migrationHandlers: make(map[string]MigrationHandler),
	}
	
	// 注册迁移处理器
	ecm.registerMigrationHandlers()
	return ecm
}

// registerMigrationHandlers 注册配置迁移处理器
func (ecm *EnhancedConfigManager) registerMigrationHandlers() {
	// v1.0 -> v2.0 迁移
	ecm.migrationHandlers["1.0"] = ecm.migrateFromV1ToV2
}

// LoadConfigWithMigration 加载配置并自动迁移
func (ecm *EnhancedConfigManager) LoadConfigWithMigration(filePath string) (*EnhancedConfig, error) {
	if filePath == "" {
		return nil, fmt.Errorf("配置文件路径不能为空")
	}
	
	// 检查文件是否存在
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		return nil, fmt.Errorf("配置文件不存在: %s", filePath)
	}
	
	// 读取文件内容
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("读取配置文件失败: %w", err)
	}
	
	// 尝试解析为新格式
	var config EnhancedConfig
	if err := json.Unmarshal(data, &config); err != nil {
		return nil, fmt.Errorf("解析配置文件失败: %w", err)
	}
	
	// 检查是否需要迁移
	if config.Version == "" {
		// 假设是v1.0格式，需要迁移
		if err := ecm.migrateFromV1ToV2(&config); err != nil {
			return nil, fmt.Errorf("配置迁移失败: %w", err)
		}
	}
	
	// 设置默认值
	ecm.setDefaultValues(&config)
	
	// 验证配置
	if err := ecm.ValidateEnhancedConfig(&config); err != nil {
		return nil, fmt.Errorf("配置验证失败: %w", err)
	}
	
	// 更新时间戳
	now := time.Now()
	if config.CreatedAt == nil {
		config.CreatedAt = &now
	}
	config.UpdatedAt = &now
	
	return &config, nil
}

// ValidateEnhancedConfig 验证增强配置
func (ecm *EnhancedConfigManager) ValidateEnhancedConfig(config *EnhancedConfig) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}
	
	// 基础验证
	if config.ProjectName == "" {
		return fmt.Errorf("项目名称不能为空")
	}
	
	if len(config.Keywords) == 0 {
		return fmt.Errorf("关键词列表不能为空")
	}
	
	// 检查关键词重复
	keySet := make(map[string]bool)
	for i, keyword := range config.Keywords {
		if keyword.Key == "" {
			return fmt.Errorf("第 %d 个关键词的 key 不能为空", i+1)
		}
		if keyword.Value == "" {
			return fmt.Errorf("第 %d 个关键词的 value 不能为空", i+1)
		}
		if keySet[keyword.Key] {
			return fmt.Errorf("关键词重复: %s", keyword.Key)
		}
		keySet[keyword.Key] = true
	}
	
	// 验证注释追踪配置
	if config.CommentTracking != nil {
		if err := ecm.validateCommentTracking(config.CommentTracking); err != nil {
			return fmt.Errorf("注释追踪配置无效: %w", err)
		}
	}
	
	// 验证处理配置
	if config.ProcessingConfig != nil {
		if err := ecm.validateProcessingConfig(config.ProcessingConfig); err != nil {
			return fmt.Errorf("处理配置无效: %w", err)
		}
	}
	
	return nil
}

// validateCommentTracking 验证注释追踪配置
func (ecm *EnhancedConfigManager) validateCommentTracking(ct *CommentTracking) error {
	if ct.CommentFormat == "" {
		return fmt.Errorf("注释格式不能为空")
	}
	
	if ct.MaxCommentHistory < 1 || ct.MaxCommentHistory > 100 {
		return fmt.Errorf("注释历史记录数量必须在1-100之间")
	}
	
	return nil
}

// validateProcessingConfig 验证处理配置
func (ecm *EnhancedConfigManager) validateProcessingConfig(pc *ProcessingConfig) error {
	if pc.MaxConcurrentFiles < 1 || pc.MaxConcurrentFiles > 50 {
		return fmt.Errorf("最大并发文件数必须在1-50之间")
	}
	
	return nil
}

// migrateFromV1ToV2 从v1.0迁移到v2.0
func (ecm *EnhancedConfigManager) migrateFromV1ToV2(config *EnhancedConfig) error {
	// 设置版本号
	config.Version = "2.0"
	
	// 如果没有注释追踪配置，添加默认配置
	if config.CommentTracking == nil {
		config.CommentTracking = &CommentTracking{
			EnableCommentTracking:   false, // 默认禁用以保持兼容性
			CleanupOrphanedComments: false,
			CommentFormat:          "DOCX_REPLACER_ORIGINAL",
			MaxCommentHistory:      10,
			AutoBackup:             true,
		}
	}
	
	// 如果没有处理配置，添加默认配置
	if config.ProcessingConfig == nil {
		config.ProcessingConfig = &ProcessingConfig{
			EnableDetailedLogging: false,
			MaxConcurrentFiles:   1,
			BackupOriginal:       false,
			OutputSuffix:         "_processed",
			ExcludePatterns:      []string{"~$*", "*.tmp"},
		}
	}
	
	// 为关键词添加默认的enabled字段
	for i := range config.Keywords {
		if config.Keywords[i].Enabled == nil {
			enabled := true
			config.Keywords[i].Enabled = &enabled
		}
	}
	
	fmt.Println("配置已从v1.0迁移到v2.0")
	return nil
}

// setDefaultValues 设置默认值
func (ecm *EnhancedConfigManager) setDefaultValues(config *EnhancedConfig) {
	if config.Version == "" {
		config.Version = "2.0"
	}
	
	// 设置注释追踪默认值
	if config.CommentTracking == nil {
		config.CommentTracking = &CommentTracking{
			EnableCommentTracking:   false,
			CleanupOrphanedComments: false,
			CommentFormat:          "DOCX_REPLACER_ORIGINAL",
			MaxCommentHistory:      10,
			AutoBackup:             true,
		}
	}
	
	// 设置处理配置默认值
	if config.ProcessingConfig == nil {
		config.ProcessingConfig = &ProcessingConfig{
			EnableDetailedLogging: false,
			MaxConcurrentFiles:   1,
			BackupOriginal:       false,
			OutputSuffix:         "_processed",
			ExcludePatterns:      []string{"~$*", "*.tmp"},
		}
	}
}