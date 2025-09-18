package config

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// SaveConfig 保存配置到文件
func (ecm *EnhancedConfigManager) SaveConfig(config *EnhancedConfig, filePath string) error {
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
	if config.ProcessingConfig != nil && config.ProcessingConfig.BackupOriginal {
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

// GetEnabledKeywords 获取启用的关键词映射
func (ecm *EnhancedConfigManager) GetEnabledKeywords(config *EnhancedConfig) map[string]string {
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
			// 检查 key 是否已经包含 # 号，如果已经包含则直接使用，否则添加 # 号
			var formattedKey string
			if strings.HasPrefix(keyword.Key, "#") && strings.HasSuffix(keyword.Key, "#") {
				formattedKey = keyword.Key
			} else {
				formattedKey = fmt.Sprintf("#%s#", keyword.Key)
			}
			keywordMap[formattedKey] = keyword.Value
		}
	}
	
	return keywordMap
}

// GetKeywordsByCategory 按类别获取关键词
func (ecm *EnhancedConfigManager) GetKeywordsByCategory(config *EnhancedConfig, category string) []EnhancedKeyword {
	if config == nil {
		return nil
	}
	
	var result []EnhancedKeyword
	for _, keyword := range config.Keywords {
		if keyword.Category == category {
			result = append(result, keyword)
		}
	}
	
	return result
}

// AddKeyword 添加关键词
func (ecm *EnhancedConfigManager) AddKeyword(config *EnhancedConfig, keyword EnhancedKeyword) error {
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
func (ecm *EnhancedConfigManager) UpdateKeyword(config *EnhancedConfig, key string, newKeyword EnhancedKeyword) error {
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
func (ecm *EnhancedConfigManager) RemoveKeyword(config *EnhancedConfig, key string) error {
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

// GenerateTemplate 生成配置模板
func (ecm *EnhancedConfigManager) GenerateTemplate(templateType string) (*EnhancedConfig, error) {
	switch templateType {
	case "basic":
		return ecm.generateBasicTemplate(), nil
	case "advanced":
		return ecm.generateAdvancedTemplate(), nil

	default:
		return nil, fmt.Errorf("未知的模板类型: %s", templateType)
	}
}

// generateBasicTemplate 生成基础模板
func (ecm *EnhancedConfigManager) generateBasicTemplate() *EnhancedConfig {
	enabled := true
	now := time.Now()
	
	return &EnhancedConfig{
		ProjectName: "示例项目",
		Version:     "2.0",
		CreatedAt:   &now,
		Keywords: []EnhancedKeyword{
			{Key: "产品名称", Value: "示例产品", Enabled: &enabled, Category: "基础信息"},
			{Key: "公司名称", Value: "示例公司", Enabled: &enabled, Category: "基础信息"},
			{Key: "版本号", Value: "v1.0", Enabled: &enabled, Category: "版本信息"},
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

// generateAdvancedTemplate 生成高级模板
func (ecm *EnhancedConfigManager) generateAdvancedTemplate() *EnhancedConfig {
	config := ecm.generateBasicTemplate()
	
	// 启用更多功能
	config.ProcessingConfig.EnableDetailedLogging = true
	config.ProcessingConfig.MaxConcurrentFiles = 3
	config.ProcessingConfig.BackupOriginal = true
	
	return config
}