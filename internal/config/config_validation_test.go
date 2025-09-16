package config

import (
	"testing"
)

func TestConfigManager_ValidateConfig(t *testing.T) {
	tests := []struct {
		name    string
		config  *Config
		wantErr bool
	}{
		{
			name: "valid config",
			config: &Config{
				ProjectName: "Test Project",
				Keywords: []Keyword{
					{Key: "NAME", Value: "John", SourceFile: "test.xlsx"},
				},
			},
			wantErr: false,
		},
		{
			name: "empty project name",
			config: &Config{
				ProjectName: "",
				Keywords: []Keyword{
					{Key: "NAME", Value: "John", SourceFile: "test.xlsx"},
				},
			},
			wantErr: true,
		},
		{
			name: "empty keywords",
			config: &Config{
				ProjectName: "Test Project",
				Keywords: []Keyword{},
			},
			wantErr: true,
		},
		{
			name: "empty key",
			config: &Config{
				ProjectName: "Test Project",
				Keywords: []Keyword{
					{Key: "", Value: "John", SourceFile: "test.xlsx"},
				},
			},
			wantErr: true,
		},
		{
			name: "empty value",
			config: &Config{
				ProjectName: "Test Project",
				Keywords: []Keyword{
					{Key: "NAME", Value: "", SourceFile: "test.xlsx"},
				},
			},
			wantErr: true,
		},
		{
			name: "duplicate keys",
			config: &Config{
				ProjectName: "Test Project",
				Keywords: []Keyword{
					{Key: "NAME", Value: "John", SourceFile: "test.xlsx"},
					{Key: "NAME", Value: "Jane", SourceFile: "test.xlsx"},
				},
			},
			wantErr: true,
		},
	}

	manager := NewConfigManager()
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			err := manager.ValidateConfig(tt.config)
			if tt.wantErr {
				if err == nil {
					t.Errorf("期望出现错误，但没有错误")
				}
			} else {
				if err != nil {
					t.Errorf("不期望出现错误，但出现了错误: %v", err)
				}
			}
		})
	}
}