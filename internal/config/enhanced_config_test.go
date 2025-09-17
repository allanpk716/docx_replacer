package config

import (
	"os"
	"path/filepath"
	"testing"
)

// TestNewEnhancedConfigManager 测试创建增强配置管理器
func TestNewEnhancedConfigManager(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	if ecm == nil {
		t.Fatal("增强配置管理器创建失败")
	}
	
	if len(ecm.migrationHandlers) == 0 {
		t.Error("迁移处理器未注册")
	}
}

// TestValidateEnhancedConfig 测试配置验证
func TestValidateEnhancedConfig(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	
	// 测试空配置
	err := ecm.ValidateEnhancedConfig(nil)
	if err == nil {
		t.Error("应该拒绝空配置")
	}
	
	// 测试有效配置
	enabled := true
	validConfig := &EnhancedConfig{
		ProjectName: "测试项目",
		Keywords: []EnhancedKeyword{
			{Key: "测试", Value: "值", Enabled: &enabled},
		},
		CommentTracking: &CommentTracking{
			EnableCommentTracking: true,
			CommentFormat:        "TEST_FORMAT",
			MaxCommentHistory:    5,
		},
		ProcessingConfig: &ProcessingConfig{
			MaxConcurrentFiles: 2,
		},
	}
	
	err = ecm.ValidateEnhancedConfig(validConfig)
	if err != nil {
		t.Errorf("有效配置验证失败: %v", err)
	}
	
	// 测试无效的注释追踪配置
	invalidConfig := &EnhancedConfig{
		ProjectName: "测试项目",
		Keywords: []EnhancedKeyword{
			{Key: "测试", Value: "值", Enabled: &enabled},
		},
		CommentTracking: &CommentTracking{
			EnableCommentTracking: true,
			CommentFormat:        "", // 空格式
			MaxCommentHistory:    5,
		},
	}
	
	err = ecm.ValidateEnhancedConfig(invalidConfig)
	if err == nil {
		t.Error("应该拒绝空注释格式")
	}
}

// TestMigrationFromV1ToV2 测试配置迁移
func TestMigrationFromV1ToV2(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	
	// 创建v1.0格式的配置
	config := &EnhancedConfig{
		ProjectName: "测试项目",
		Keywords: []EnhancedKeyword{
			{Key: "测试", Value: "值"},
		},
		// 没有版本号，模拟v1.0
	}
	
	err := ecm.migrateFromV1ToV2(config)
	if err != nil {
		t.Errorf("迁移失败: %v", err)
	}
	
	// 验证迁移结果
	if config.Version != "2.0" {
		t.Errorf("版本号应该是2.0，实际: %s", config.Version)
	}
	
	if config.CommentTracking == nil {
		t.Error("注释追踪配置应该被添加")
	}
	
	if config.ProcessingConfig == nil {
		t.Error("处理配置应该被添加")
	}
	
	// 检查关键词的enabled字段
	for _, keyword := range config.Keywords {
		if keyword.Enabled == nil {
			t.Error("关键词的enabled字段应该被设置")
		}
	}
}

// TestGetEnabledKeywords 测试获取启用的关键词
func TestGetEnabledKeywords(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	
	enabled := true
	disabled := false
	config := &EnhancedConfig{
		Keywords: []EnhancedKeyword{
			{Key: "启用", Value: "值1", Enabled: &enabled},
			{Key: "禁用", Value: "值2", Enabled: &disabled},
			{Key: "默认", Value: "值3"}, // 没有设置enabled，应该默认启用
		},
	}
	
	keywords := ecm.GetEnabledKeywords(config)
	
	if len(keywords) != 2 {
		t.Errorf("应该有2个启用的关键词，实际: %d", len(keywords))
	}
	
	if keywords["#启用#"] != "值1" {
		t.Error("启用的关键词值不正确")
	}
	
	if keywords["#默认#"] != "值3" {
		t.Error("默认启用的关键词值不正确")
	}
	
	if _, exists := keywords["#禁用#"]; exists {
		t.Error("禁用的关键词不应该出现在结果中")
	}
}

// TestKeywordManagement 测试关键词管理
func TestKeywordManagement(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	enabled := true
	
	config := &EnhancedConfig{
		ProjectName: "测试项目",
		Keywords: []EnhancedKeyword{
			{Key: "原始", Value: "值", Enabled: &enabled},
		},
	}
	
	// 测试添加关键词
	newKeyword := EnhancedKeyword{
		Key:      "新增",
		Value:    "新值",
		Category: "测试类别",
	}
	
	err := ecm.AddKeyword(config, newKeyword)
	if err != nil {
		t.Errorf("添加关键词失败: %v", err)
	}
	
	if len(config.Keywords) != 2 {
		t.Errorf("关键词数量应该是2，实际: %d", len(config.Keywords))
	}
	
	// 测试重复添加
	err = ecm.AddKeyword(config, newKeyword)
	if err == nil {
		t.Error("应该拒绝重复的关键词")
	}
	
	// 测试更新关键词
	updatedKeyword := EnhancedKeyword{
		Key:   "新增",
		Value: "更新值",
	}
	
	err = ecm.UpdateKeyword(config, "新增", updatedKeyword)
	if err != nil {
		t.Errorf("更新关键词失败: %v", err)
	}
	
	// 验证更新结果
	for _, keyword := range config.Keywords {
		if keyword.Key == "新增" && keyword.Value != "更新值" {
			t.Error("关键词值未正确更新")
		}
	}
	
	// 测试移除关键词
	err = ecm.RemoveKeyword(config, "新增")
	if err != nil {
		t.Errorf("移除关键词失败: %v", err)
	}
	
	if len(config.Keywords) != 1 {
		t.Errorf("关键词数量应该是1，实际: %d", len(config.Keywords))
	}
}

// TestGenerateTemplate 测试模板生成
func TestGenerateTemplate(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	
	// 测试基础模板
	basicConfig, err := ecm.GenerateTemplate("basic")
	if err != nil {
		t.Errorf("生成基础模板失败: %v", err)
	}
	
	if basicConfig.ProjectName == "" {
		t.Error("基础模板应该有项目名称")
	}
	
	if len(basicConfig.Keywords) == 0 {
		t.Error("基础模板应该有关键词")
	}
	
	// 测试注释追踪模板
	trackingConfig, err := ecm.GenerateTemplate("comment_tracking")
	if err != nil {
		t.Errorf("生成注释追踪模板失败: %v", err)
	}
	
	if !trackingConfig.CommentTracking.EnableCommentTracking {
		t.Error("注释追踪模板应该启用注释追踪")
	}
	
	// 测试未知模板类型
	_, err = ecm.GenerateTemplate("unknown")
	if err == nil {
		t.Error("应该拒绝未知的模板类型")
	}
}

// TestSaveAndLoadConfig 测试配置保存和加载
func TestSaveAndLoadConfig(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	
	// 创建临时文件
	tempDir := t.TempDir()
	configPath := filepath.Join(tempDir, "test_config.json")
	
	// 生成测试配置
	originalConfig, err := ecm.GenerateTemplate("basic")
	if err != nil {
		t.Fatalf("生成测试配置失败: %v", err)
	}
	
	// 保存配置
	err = ecm.SaveConfig(originalConfig, configPath)
	if err != nil {
		t.Errorf("保存配置失败: %v", err)
	}
	
	// 验证文件存在
	if _, err := os.Stat(configPath); os.IsNotExist(err) {
		t.Error("配置文件未创建")
	}
	
	// 加载配置
	loadedConfig, err := ecm.LoadConfigWithMigration(configPath)
	if err != nil {
		t.Errorf("加载配置失败: %v", err)
	}
	
	// 验证加载的配置
	if loadedConfig.ProjectName != originalConfig.ProjectName {
		t.Error("项目名称不匹配")
	}
	
	if len(loadedConfig.Keywords) != len(originalConfig.Keywords) {
		t.Error("关键词数量不匹配")
	}
	
	if loadedConfig.Version != "2.0" {
		t.Error("版本号不正确")
	}
}

// TestGetKeywordsByCategory 测试按类别获取关键词
func TestGetKeywordsByCategory(t *testing.T) {
	ecm := NewEnhancedConfigManager()
	enabled := true
	
	config := &EnhancedConfig{
		Keywords: []EnhancedKeyword{
			{Key: "产品1", Value: "值1", Category: "产品", Enabled: &enabled},
			{Key: "产品2", Value: "值2", Category: "产品", Enabled: &enabled},
			{Key: "公司1", Value: "值3", Category: "公司", Enabled: &enabled},
			{Key: "其他", Value: "值4", Enabled: &enabled},
		},
	}
	
	// 测试获取产品类别的关键词
	productKeywords := ecm.GetKeywordsByCategory(config, "产品")
	if len(productKeywords) != 2 {
		t.Errorf("产品类别应该有2个关键词，实际: %d", len(productKeywords))
	}
	
	// 测试获取不存在的类别
	nonExistentKeywords := ecm.GetKeywordsByCategory(config, "不存在")
	if len(nonExistentKeywords) != 0 {
		t.Errorf("不存在的类别应该返回空列表，实际: %d", len(nonExistentKeywords))
	}
}