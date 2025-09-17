# 配置扩展实现方案

## 1. 扩展的配置结构

### 更新的Config结构体
```go
package config

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"time"
)

// Config 表示完整的配置文件结构（扩展版）
type Config struct {
	ProjectName      string            `json:"project_name"`
	Keywords         []Keyword         `json:"keywords"`
	CommentTracking  *CommentTracking  `json:"comment_tracking,omitempty"`
	ProcessingConfig *ProcessingConfig `json:"processing_config,omitempty"`
	Version          string            `json:"version,omitempty"`
	CreatedAt        *time.Time        `json:"created_at,omitempty"`
	UpdatedAt        *time.Time        `json:"updated_at,omitempty"`
}

// Keyword 表示一个关键词配置项（扩展版）
type Keyword struct {
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
```

## 2. 配置管理器扩展

### 增强的ConfigManager
```go
// EnhancedConfigManager 增强的配置管理器
type EnhancedConfigManager struct {
	*configManager
	migrationHandlers map[string]MigrationHandler
}

// MigrationHandler 配置迁移处理器
type MigrationHandler func(*Config) error

// NewEnhancedConfigManager 创建增强的配置管理器
func NewEnhancedConfigManager() *EnhancedConfigManager {
	ecm := &EnhancedConfigManager{
		configManager:     &configManager{},
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
	// 可以添加更多版本的迁移处理器
}
```

### 配置加载和验证
```go
// LoadConfigWithMigration 加载配置并自动迁移
func (ecm *EnhancedConfigManager) LoadConfigWithMigration(filePath string) (*Config, error) {
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
	var config Config
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
func (ecm *EnhancedConfigManager) ValidateEnhancedConfig(config *Config) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}
	
	// 基础验证
	if err := ecm.configManager.ValidateConfig(&Config{
		ProjectName: config.ProjectName,
		Keywords:    config.Keywords,
	}); err != nil {
		return err
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
```

## 3. 配置迁移实现

### 版本迁移处理
```go
// migrateFromV1ToV2 从v1.0迁移到v2.0
func (ecm *EnhancedConfigManager) migrateFromV1ToV2(config *Config) error {
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
func (ecm *EnhancedConfigManager) setDefaultValues(config *Config) {
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
```

## 4. 配置保存和备份

### 配置保存
```go
// SaveConfig 保存配置到文件
func (ecm *EnhancedConfigManager) SaveConfig(config *Config, filePath string) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}
	
	// 更新时间戳
	now := time.Now()
	config.UpdatedAt = &now
	
	// 验证配置
	if err := ecm.ValidateEnhancedConfig(config); err != nil {
		return fmt.Errorf("配置验证失败: %w", err)
	}
	
	// 创建备份（如果启用）
	if config.CommentTracking != nil && config.CommentTracking.AutoBackup {
		if err := ecm.createBackup(filePath); err != nil {
			fmt.Printf("创建备份失败: %v\n", err)
		}
	}
	
	// 序列化配置
	data, err := json.MarshalIndent(config, "", "  ")
	if err != nil {
		return fmt.Errorf("序列化配置失败: %w", err)
	}
	
	// 确保目录存在
	dir := filepath.Dir(filePath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("创建目录失败: %w", err)
	}
	
	// 写入文件
	if err := os.WriteFile(filePath, data, 0644); err != nil {
		return fmt.Errorf("写入配置文件失败: %w", err)
	}
	
	return nil
}

// createBackup 创建配置文件备份
func (ecm *EnhancedConfigManager) createBackup(filePath string) error {
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		return nil // 文件不存在，无需备份
	}
	
	// 生成备份文件名
	dir := filepath.Dir(filePath)
	base := filepath.Base(filePath)
	ext := filepath.Ext(base)
	name := strings.TrimSuffix(base, ext)
	timestamp := time.Now().Format("20060102_150405")
	backupPath := filepath.Join(dir, fmt.Sprintf("%s_backup_%s%s", name, timestamp, ext))
	
	// 复制文件
	src, err := os.ReadFile(filePath)
	if err != nil {
		return fmt.Errorf("读取原文件失败: %w", err)
	}
	
	if err := os.WriteFile(backupPath, src, 0644); err != nil {
		return fmt.Errorf("写入备份文件失败: %w", err)
	}
	
	fmt.Printf("配置备份已创建: %s\n", backupPath)
	return nil
}
```

## 5. 配置工具方法

### 关键词管理
```go
// GetEnabledKeywords 获取启用的关键词映射
func (ecm *EnhancedConfigManager) GetEnabledKeywords(config *Config) map[string]string {
	if config == nil {
		return nil
	}
	
	keywordMap := make(map[string]string)
	for _, keyword := range config.Keywords {
		// 检查是否启用（默认为启用）
		enabled := true
		if keyword.Enabled != nil {
			enabled = *keyword.Enabled
		}
		
		if enabled {
			// 将 key 转换为 #key# 格式
			formattedKey := fmt.Sprintf("#%s#", keyword.Key)
			keywordMap[formattedKey] = keyword.Value
		}
	}
	
	return keywordMap
}

// GetKeywordsByCategory 按类别获取关键词
func (ecm *EnhancedConfigManager) GetKeywordsByCategory(config *Config, category string) []Keyword {
	if config == nil {
		return nil
	}
	
	var result []Keyword
	for _, keyword := range config.Keywords {
		if keyword.Category == category {
			result = append(result, keyword)
		}
	}
	
	return result
}

// AddKeyword 添加关键词
func (ecm *EnhancedConfigManager) AddKeyword(config *Config, keyword Keyword) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}
	
	// 检查关键词是否已存在
	for _, existing := range config.Keywords {
		if existing.Key == keyword.Key {
			return fmt.Errorf("关键词已存在: %s", keyword.Key)
		}
	}
	
	// 设置默认值
	if keyword.Enabled == nil {
		enabled := true
		keyword.Enabled = &enabled
	}
	
	config.Keywords = append(config.Keywords, keyword)
	return nil
}

// UpdateKeyword 更新关键词
func (ecm *EnhancedConfigManager) UpdateKeyword(config *Config, key string, newKeyword Keyword) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}
	
	for i, existing := range config.Keywords {
		if existing.Key == key {
			config.Keywords[i] = newKeyword
			return nil
		}
	}
	
	return fmt.Errorf("未找到关键词: %s", key)
}

// RemoveKeyword 移除关键词
func (ecm *EnhancedConfigManager) RemoveKeyword(config *Config, key string) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}
	
	for i, existing := range config.Keywords {
		if existing.Key == key {
			config.Keywords = append(config.Keywords[:i], config.Keywords[i+1:]...)
			return nil
		}
	}
	
	return fmt.Errorf("未找到关键词: %s", key)
}
```

## 6. 配置模板和示例

### 配置模板生成
```go
// GenerateTemplate 生成配置模板
func (ecm *EnhancedConfigManager) GenerateTemplate(templateType string) (*Config, error) {
	switch templateType {
	case "basic":
		return ecm.generateBasicTemplate(), nil
	case "advanced":
		return ecm.generateAdvancedTemplate(), nil
	case "comment_tracking":
		return ecm.generateCommentTrackingTemplate(), nil
	default:
		return nil, fmt.Errorf("未知的模板类型: %s", templateType)
	}
}

// generateBasicTemplate 生成基础模板
func (ecm *EnhancedConfigManager) generateBasicTemplate() *Config {
	enabled := true
	now := time.Now()
	
	return &Config{
		ProjectName: "示例项目",
		Version:     "2.0",
		CreatedAt:   &now,
		Keywords: []Keyword{
			{Key: "产品名称", Value: "示例产品", Enabled: &enabled, Category: "基础信息"},
			{Key: "公司名称", Value: "示例公司", Enabled: &enabled, Category: "基础信息"},
			{Key: "版本号", Value: "v1.0", Enabled: &enabled, Category: "版本信息"},
		},
		CommentTracking: &CommentTracking{
			EnableCommentTracking:   false,
			CleanupOrphanedComments: false,
			CommentFormat:          "DOCX_REPLACER_ORIGINAL",
			MaxCommentHistory:      10,
			AutoBackup:             true,
		},
		ProcessingConfig: &ProcessingConfig{
			EnableDetailedLogging: false,
			MaxConcurrentFiles:   1,
			BackupOriginal:       false,
			OutputSuffix:         "_processed",
			ExcludePatterns:      []string{"~$*", "*.tmp"},
		},
	}
}

// generateCommentTrackingTemplate 生成注释追踪模板
func (ecm *EnhancedConfigManager) generateCommentTrackingTemplate() *Config {
	config := ecm.generateBasicTemplate()
	
	// 启用注释追踪功能
	config.CommentTracking.EnableCommentTracking = true
	config.CommentTracking.CleanupOrphanedComments = true
	config.ProcessingConfig.EnableDetailedLogging = true
	
	return config
}
```

## 7. 使用示例

```go
// 使用示例
func ExampleConfigUsage() {
	// 创建增强配置管理器
	ecm := NewEnhancedConfigManager()
	
	// 加载配置（自动迁移）
	config, err := ecm.LoadConfigWithMigration("config.json")
	if err != nil {
		fmt.Printf("加载配置失败: %v\n", err)
		return
	}
	
	// 启用注释追踪
	config.CommentTracking.EnableCommentTracking = true
	
	// 添加新关键词
	newKeyword := Keyword{
		Key:      "新产品",
		Value:    "iPhone16",
		Category: "产品信息",
	}
	ecm.AddKeyword(config, newKeyword)
	
	// 获取启用的关键词
	keywords := ecm.GetEnabledKeywords(config)
	fmt.Printf("启用的关键词: %+v\n", keywords)
	
	// 保存配置
	if err := ecm.SaveConfig(config, "config.json"); err != nil {
		fmt.Printf("保存配置失败: %v\n", err)
		return
	}
	
	fmt.Println("配置处理完成")
}
```