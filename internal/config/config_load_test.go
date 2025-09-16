package config

import (
	"os"
	"testing"
)

func TestConfigManager_LoadConfig(t *testing.T) {
	tests := []struct {
		name        string
		configData  string
		wantErr     bool
		wantProject string
		wantKeywords int
	}{
		{
			name: "valid config",
			configData: `{
				"project_name": "Test Project",
				"keywords": [
					{"key": "NAME", "value": "John", "source_file": "test.xlsx"},
					{"key": "AGE", "value": "25", "source_file": "test.xlsx"}
				]
			}`,
			wantErr:      false,
			wantProject:  "Test Project",
			wantKeywords: 2,
		},
		{
			name: "empty project name",
			configData: `{
				"project_name": "",
				"keywords": [{"key": "NAME", "value": "John", "source_file": "test.xlsx"}]
			}`,
			wantErr: true,
		},
		{
			name: "empty keywords",
			configData: `{
				"project_name": "Test Project",
				"keywords": []
			}`,
			wantErr: true,
		},
		{
			name: "invalid json",
			configData: `{
				"project_name": "Test Project",
				"keywords": [
			}`,
			wantErr: true,
		},
		{
			name: "missing key field",
			configData: `{
				"project_name": "Test Project",
				"keywords": [{"value": "John", "source_file": "test.xlsx"}]
			}`,
			wantErr: true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			// 创建临时配置文件
			tmpFile, err := os.CreateTemp("", "config_*.json")
			if err != nil {
				t.Fatalf("创建临时文件失败: %v", err)
			}
			defer os.Remove(tmpFile.Name())

			if _, err := tmpFile.WriteString(tt.configData); err != nil {
				t.Fatalf("写入临时文件失败: %v", err)
			}
			tmpFile.Close()

			// 测试加载配置
			manager := NewConfigManager()
			config, err := manager.LoadConfig(tmpFile.Name())

			if tt.wantErr {
				if err == nil {
					t.Errorf("期望出现错误，但没有错误")
				}
				return
			}

			if err != nil {
				t.Errorf("不期望出现错误，但出现了错误: %v", err)
				return
			}

			if config.ProjectName != tt.wantProject {
				t.Errorf("项目名称 = %v, 期望 %v", config.ProjectName, tt.wantProject)
			}

			if len(config.Keywords) != tt.wantKeywords {
				t.Errorf("关键词数量 = %v, 期望 %v", len(config.Keywords), tt.wantKeywords)
			}
		})
	}
}

func TestConfigManager_LoadConfig_FileNotFound(t *testing.T) {
	manager := NewConfigManager()
	_, err := manager.LoadConfig("nonexistent.json")
	if err == nil {
		t.Errorf("期望文件不存在错误，但没有错误")
	}
}

func TestConfigManager_LoadConfig_InvalidPath(t *testing.T) {
	manager := NewConfigManager()
	_, err := manager.LoadConfig("")
	if err == nil {
		t.Errorf("期望路径无效错误，但没有错误")
	}
}

func TestConfigManager_GetKeywordMap(t *testing.T) {
	config := &Config{
		ProjectName: "Test Project",
		Keywords: []Keyword{
			{Key: "NAME", Value: "John", SourceFile: "test.xlsx"},
			{Key: "AGE", Value: "25", SourceFile: "test.xlsx"},
			{Key: "CITY", Value: "Beijing", SourceFile: "test.xlsx"},
		},
	}

	manager := NewConfigManager()
	keywordMap := manager.GetKeywordMap(config)

	expected := map[string]string{
		"#NAME#": "John",
		"#AGE#":  "25",
		"#CITY#": "Beijing",
	}

	if len(keywordMap) != len(expected) {
		t.Errorf("关键词映射数量 = %v, 期望 %v", len(keywordMap), len(expected))
	}

	for key, expectedValue := range expected {
		if actualValue, exists := keywordMap[key]; !exists {
			t.Errorf("关键词 %s 不存在", key)
		} else if actualValue != expectedValue {
			t.Errorf("关键词 %s 的值 = %v, 期望 %v", key, actualValue, expectedValue)
		}
	}
}