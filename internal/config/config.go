package config

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

// Keyword 表示一个关键词配置项
type Keyword struct {
	Key        string `json:"key"`
	Value      string `json:"value"`
	SourceFile string `json:"source_file"`
}

// Config 表示完整的配置文件结构
type Config struct {
	ProjectName string    `json:"project_name"`
	Keywords    []Keyword `json:"keywords"`
}

// ConfigManager 配置管理接口
type ConfigManager interface {
	LoadConfig(filePath string) (*Config, error)
	ValidateConfig(config *Config) error
	GetKeywordMap(config *Config) map[string]string
}

// configManager 配置管理器实现
type configManager struct{}

// NewConfigManager 创建新的配置管理器
func NewConfigManager() ConfigManager {
	return &configManager{}
}

// LoadConfig 从文件加载配置
func (cm *configManager) LoadConfig(filePath string) (*Config, error) {
	if filePath == "" {
		return nil, fmt.Errorf("配置文件路径不能为空")
	}

	// 检查文件是否存在
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		return nil, fmt.Errorf("配置文件不存在: %s", filePath)
	}

	// 检查文件扩展名
	if ext := filepath.Ext(filePath); ext != ".json" {
		return nil, fmt.Errorf("配置文件必须是 JSON 格式，当前文件: %s", ext)
	}

	// 读取文件内容
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("读取配置文件失败: %w", err)
	}

	// 解析 JSON
	var config Config
	if err := json.Unmarshal(data, &config); err != nil {
		return nil, fmt.Errorf("解析配置文件失败: %w", err)
	}

	// 验证配置
	if err := cm.ValidateConfig(&config); err != nil {
		return nil, fmt.Errorf("配置验证失败: %w", err)
	}

	return &config, nil
}

// ValidateConfig 验证配置的有效性
func (cm *configManager) ValidateConfig(config *Config) error {
	if config == nil {
		return fmt.Errorf("配置不能为空")
	}

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

	return nil
}

// GetKeywordMap 将关键词列表转换为映射表，key 格式为 #原key#
func (cm *configManager) GetKeywordMap(config *Config) map[string]string {
	if config == nil {
		return nil
	}

	keywordMap := make(map[string]string)
	for _, keyword := range config.Keywords {
		// 检查 key 是否已经包含 # 号，如果已经包含则直接使用，否则添加 # 号
		var formattedKey string
		if strings.HasPrefix(keyword.Key, "#") && strings.HasSuffix(keyword.Key, "#") {
			formattedKey = keyword.Key
		} else {
			formattedKey = fmt.Sprintf("#%s#", keyword.Key)
		}
		keywordMap[formattedKey] = keyword.Value
	}

	return keywordMap
}