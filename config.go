package main

import (
	"encoding/json"
	"fmt"
	"os"
)

// Keyword 表示一个关键词替换项
type Keyword struct {
	// Key 关键词
	Key string `json:"key"`
	// Value 替换值，支持多行字符串
	Value string `json:"value"`
	// SourceFile 来源文件名称描述
	SourceFile string `json:"source_file"`
}

// Config 表示配置文件的结构
type Config struct {
	// InputDocx 输入的docx文件路径（单文件模式）
	InputDocx string `json:"input_docx,omitempty"`
	// OutputDocx 输出的docx文件路径（单文件模式）
	OutputDocx string `json:"output_docx,omitempty"`
	// InputFolder 输入文件夹路径（批量模式）
	InputFolder string `json:"input_folder,omitempty"`
	// OutputFolder 输出文件夹路径（批量模式）
	OutputFolder string `json:"output_folder,omitempty"`
	// Keywords 关键词替换列表
	Keywords []Keyword `json:"keywords"`
}

// LoadConfig 从JSON文件加载配置
func LoadConfig(configPath string) (*Config, error) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		return nil, fmt.Errorf("读取配置文件失败: %w", err)
	}

	var config Config
	if err := json.Unmarshal(data, &config); err != nil {
		return nil, fmt.Errorf("解析配置文件失败: %w", err)
	}

	// 验证配置 - 支持单文件模式和批量模式
	isSingleMode := config.InputDocx != "" && config.OutputDocx != ""
	isBatchMode := config.InputFolder != "" && config.OutputFolder != ""
	
	if !isSingleMode && !isBatchMode {
		return nil, fmt.Errorf("必须指定单文件模式（input_docx + output_docx）或批量模式（input_folder + output_folder）")
	}
	
	if isSingleMode && isBatchMode {
		return nil, fmt.Errorf("不能同时使用单文件模式和批量模式")
	}

	// 验证Keywords字段的完整性
	for i, keyword := range config.Keywords {
		if keyword.Key == "" {
			return nil, fmt.Errorf("第%d个关键词的key不能为空", i+1)
		}
		if keyword.Value == "" {
			return nil, fmt.Errorf("关键词'%s'的value不能为空", keyword.Key)
		}
		if keyword.SourceFile == "" {
			return nil, fmt.Errorf("关键词'%s'的source_file不能为空", keyword.Key)
		}
	}

	return &config, nil
}

// GetReplacementMap 获取所有替换映射，合并Keywords和Replacements
func (c *Config) GetReplacementMap() map[string]string {
	replacementMap := make(map[string]string)

	// 添加Keywords中的映射
	for _, keyword := range c.Keywords {
		replacementMap[keyword.Key] = keyword.Value
	}

	return replacementMap
}

// GetKeywordByKey 根据关键词获取完整的Keyword信息
func (c *Config) GetKeywordByKey(key string) *Keyword {
	for _, keyword := range c.Keywords {
		if keyword.Key == key {
			return &keyword
		}
	}
	return nil
}

// IsBatchMode 判断是否为批量处理模式
func (c *Config) IsBatchMode() bool {
	return c.InputFolder != "" && c.OutputFolder != ""
}

// IsSingleMode 判断是否为单文件处理模式
func (c *Config) IsSingleMode() bool {
	return c.InputDocx != "" && c.OutputDocx != ""
}
