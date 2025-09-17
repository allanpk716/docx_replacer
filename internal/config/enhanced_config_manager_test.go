package config

import (
	"encoding/json"
	"os"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestEnhancedConfigManager_LoadConfigWithMigration(t *testing.T) {
	manager := NewEnhancedConfigManager()
	
	// 创建临时配置文件
	tempFile, err := os.CreateTemp("", "test_config_*.json")
	require.NoError(t, err)
	defer os.Remove(tempFile.Name())
	
	// 写入测试配置（v1.0格式）
	testConfig := map[string]interface{}{
		"project_name": "测试项目",
		"keywords": []map[string]interface{}{
			{"key": "name", "value": "测试项目"},
			{"key": "version", "value": "1.0.0"},
		},
	}
	
	configData, err := json.Marshal(testConfig)
	require.NoError(t, err)
	
	err = os.WriteFile(tempFile.Name(), configData, 0644)
	require.NoError(t, err)
	
	// 测试加载配置
	loadedConfig, err := manager.LoadConfigWithMigration(tempFile.Name())
	require.NoError(t, err)
	require.NotNil(t, loadedConfig)
	
	// 验证配置内容
	assert.Equal(t, "2.0", loadedConfig.Version)
	assert.Len(t, loadedConfig.Keywords, 2)
	assert.NotNil(t, loadedConfig.CommentTracking)
	assert.NotNil(t, loadedConfig.ProcessingConfig)
}

func TestEnhancedConfigManager_GetEnabledKeywords(t *testing.T) {
	manager := NewEnhancedConfigManager()
	
	// 创建测试配置
	enabled := true
	disabled := false
	config := &EnhancedConfig{
		ProjectName: "测试项目",
		Keywords: []EnhancedKeyword{
			{Key: "name", Value: "测试项目", Enabled: &enabled},
			{Key: "version", Value: "1.0.0", Enabled: &disabled},
			{Key: "author", Value: "测试作者"}, // 默认启用
		},
	}
	
	// 获取启用的关键词
	keywords := manager.GetEnabledKeywords(config)
	
	// 验证结果
	assert.Len(t, keywords, 2) // 只有2个启用的关键词
	assert.Equal(t, "测试项目", keywords["#name#"])
	assert.Equal(t, "测试作者", keywords["#author#"])
	_, exists := keywords["#version#"]
	assert.False(t, exists) // version应该被禁用
}

func TestEnhancedConfigManager_AddKeyword(t *testing.T) {
	manager := NewEnhancedConfigManager()
	
	config := &EnhancedConfig{
		ProjectName: "测试项目",
		Keywords: []EnhancedKeyword{
			{Key: "existing", Value: "已存在"},
		},
	}
	
	// 测试添加新关键词
	newKeyword := EnhancedKeyword{
		Key:   "new",
		Value: "新关键词",
	}
	
	err := manager.AddKeyword(config, newKeyword)
	assert.NoError(t, err)
	assert.Len(t, config.Keywords, 2)
	
	// 测试添加重复关键词
	duplicateKeyword := EnhancedKeyword{
		Key:   "existing",
		Value: "重复",
	}
	
	err = manager.AddKeyword(config, duplicateKeyword)
	assert.Error(t, err)
	assert.Contains(t, err.Error(), "关键词已存在")
}

func TestEnhancedConfigManager_ValidateEnhancedConfig(t *testing.T) {
	manager := NewEnhancedConfigManager()
	
	tests := []struct {
		name    string
		config  *EnhancedConfig
		wantErr bool
	}{
		{
			name: "有效配置",
			config: &EnhancedConfig{
				ProjectName: "测试项目",
				Version:     "2.0",
				Keywords: []EnhancedKeyword{
					{Key: "name", Value: "测试"},
				},
			},
			wantErr: false,
		},
		{
			name:    "空配置",
			config:  nil,
			wantErr: true,
		},
		{
			name: "空项目名称",
			config: &EnhancedConfig{
				ProjectName: "",
				Keywords: []EnhancedKeyword{
					{Key: "name", Value: "测试"},
				},
			},
			wantErr: true,
		},
		{
			name: "空关键词列表",
			config: &EnhancedConfig{
				ProjectName: "测试项目",
				Keywords:    []EnhancedKeyword{},
			},
			wantErr: true,
		},
	}
	
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			err := manager.ValidateEnhancedConfig(tt.config)
			if tt.wantErr {
				assert.Error(t, err)
			} else {
				assert.NoError(t, err)
			}
		})
	}
}



func TestEnhancedConfigManager_GenerateTemplate(t *testing.T) {
	manager := NewEnhancedConfigManager()
	
	tests := []struct {
		name         string
		templateType string
		wantErr      bool
	}{
		{
			name:         "基础模板",
			templateType: "basic",
			wantErr:      false,
		},
		{
			name:         "高级模板",
			templateType: "advanced",
			wantErr:      false,
		},
		{
			name:         "注释追踪模板",
			templateType: "comment_tracking",
			wantErr:      false,
		},
		{
			name:         "未知模板",
			templateType: "unknown",
			wantErr:      true,
		},
	}
	
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			config, err := manager.GenerateTemplate(tt.templateType)
			
			if tt.wantErr {
				assert.Error(t, err)
				assert.Nil(t, config)
			} else {
				assert.NoError(t, err)
				assert.NotNil(t, config)
				assert.NotEmpty(t, config.ProjectName)
				assert.Equal(t, "2.0", config.Version)
			}
		})
	}
}

// TestEnhancedConfigManager_SaveConfig 测试配置保存
func TestEnhancedConfigManager_SaveConfig(t *testing.T) {
	manager := NewEnhancedConfigManager()
	
	// 创建测试配置
	config := &EnhancedConfig{
		ProjectName: "测试项目",
		Version:     "2.0",
		Keywords: []EnhancedKeyword{
			{Key: "name", Value: "测试项目"},
		},
	}
	
	// 创建临时文件
	tempFile, err := os.CreateTemp("", "test_save_*.json")
	require.NoError(t, err)
	defer os.Remove(tempFile.Name())
	tempFile.Close()
	
	// 保存配置
	err = manager.SaveConfig(config, tempFile.Name())
	assert.NoError(t, err)
	
	// 验证文件存在
	_, err = os.Stat(tempFile.Name())
	assert.NoError(t, err)
	
	// 重新加载并验证
	loadedConfig, err := manager.LoadConfigWithMigration(tempFile.Name())
	assert.NoError(t, err)
	assert.Equal(t, config.ProjectName, loadedConfig.ProjectName)
	assert.Equal(t, config.Version, loadedConfig.Version)
}