package main

import (
	"os"
	"path/filepath"
	"testing"
	"time"
)

// TestDocxProcessor_NewDocxProcessor 测试创建 DocxProcessor
func TestDocxProcessor_NewDocxProcessor(t *testing.T) {
	// 创建临时测试文件
	testFile := createTestDocx(t)
	defer os.Remove(testFile)

	processor, err := NewDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建 DocxProcessor 失败: %v", err)
	}
	defer processor.Close()

	if processor.editable == nil {
		t.Error("editable 字段未初始化")
	}
	if processor.replacementCount == nil {
		t.Error("replacementCount 字段未初始化")
	}
}

// TestDocxProcessor_ReplaceKeywords 测试普通关键词替换
func TestDocxProcessor_ReplaceKeywords(t *testing.T) {
	testFile := createTestDocx(t)
	defer os.Remove(testFile)

	processor, err := NewDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建 DocxProcessor 失败: %v", err)
	}
	defer processor.Close()

	replacements := map[string]string{
		"测试关键词": "替换内容",
		"另一个关键词": "另一个替换内容",
	}

	err = processor.ReplaceKeywords(replacements, false)
	if err != nil {
		t.Fatalf("替换关键词失败: %v", err)
	}

	// 检查替换计数
	counts := processor.GetReplacementCount()
	if len(counts) != len(replacements) {
		t.Errorf("替换计数数量不匹配，期望 %d，实际 %d", len(replacements), len(counts))
	}
}

// TestDocxProcessor_ReplaceKeywordsWithHashWrapper 测试带井号包装的关键词替换
func TestDocxProcessor_ReplaceKeywordsWithHashWrapper(t *testing.T) {
	testFile := createTestDocx(t)
	defer os.Remove(testFile)

	processor, err := NewDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建 DocxProcessor 失败: %v", err)
	}
	defer processor.Close()

	// 第一步：先在文档中添加带井号的关键词（模拟实际文档场景）
	replacements1 := map[string]string{
		"注册申请人": "#姓名#",
		"法定代表人": "#法人#",
	}
	err = processor.ReplaceKeywords(replacements1, true)
	if err != nil {
		t.Fatalf("添加井号关键词失败: %v", err)
	}

	// 第二步：测试井号包装功能
	// 提供不带井号的关键词，应该能找到并替换文档中带井号的文本
	replacements2 := map[string]string{
		"姓名": "张三",
		"法人": "李四",
	}

	err = processor.ReplaceKeywordsWithHashWrapper(replacements2, true)
	if err != nil {
		t.Fatalf("替换带井号关键词失败: %v", err)
	}

	// 检查替换计数
	counts := processor.GetReplacementCount()
	if counts == nil {
		t.Error("替换计数不应该为nil")
	}

	// 验证井号关键词被正确替换
	if counts["姓名"] == 0 {
		t.Error("应该找到并替换 #姓名# 关键词")
	}
	if counts["法人"] == 0 {
		t.Error("应该找到并替换 #法人# 关键词")
	}
}

// TestDocxProcessor_ReplaceKeywordsWithOptions 测试带选项的关键词替换
func TestDocxProcessor_ReplaceKeywordsWithOptions(t *testing.T) {
	tests := []struct {
		name           string
		useHashWrapper bool
		createFunc     func(*testing.T) string
	}{
		{
			name:           "普通关键词替换",
			useHashWrapper: false,
			createFunc:     createTestDocx,
		},
		{
			name:           "井号包装关键词替换",
			useHashWrapper: true,
			createFunc:     createTestDocxWithHashKeywords,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			testFile := tt.createFunc(t)
			defer os.Remove(testFile)

			processor, err := NewDocxProcessor(testFile)
			if err != nil {
				t.Fatalf("创建 DocxProcessor 失败: %v", err)
			}
			defer processor.Close()

			replacements := map[string]string{
				"测试": "成功",
			}

			err = processor.ReplaceKeywordsWithOptions(replacements, false, tt.useHashWrapper)
			if err != nil {
				t.Fatalf("替换关键词失败: %v", err)
			}
		})
	}
}

// TestDocxProcessor_SaveAs 测试保存文档
func TestDocxProcessor_SaveAs(t *testing.T) {
	testFile := createTestDocx(t)
	defer os.Remove(testFile)

	processor, err := NewDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建 DocxProcessor 失败: %v", err)
	}
	defer processor.Close()

	// 执行替换
	replacements := map[string]string{
		"测试关键词": "替换内容",
	}
	err = processor.ReplaceKeywords(replacements, false)
	if err != nil {
		t.Fatalf("替换关键词失败: %v", err)
	}

	// 保存到新文件
	outputFile := filepath.Join(os.TempDir(), "test_output_"+time.Now().Format("20060102150405")+".docx")
	defer os.Remove(outputFile)

	err = processor.SaveAs(outputFile)
	if err != nil {
		t.Fatalf("保存文档失败: %v", err)
	}

	// 检查文件是否存在
	if _, err := os.Stat(outputFile); os.IsNotExist(err) {
		t.Error("输出文件未创建")
	}
}

// createTestDocx 创建测试用的 docx 文件
func createTestDocx(t *testing.T) string {
	// 使用包含测试关键词的测试文档
	testDocPath := "test/test_document.docx"
	if _, err := os.Stat(testDocPath); os.IsNotExist(err) {
		t.Skip("测试文档不存在，跳过测试")
	}
	
	// 复制到临时文件
	tempFile := filepath.Join(os.TempDir(), "test_"+time.Now().Format("20060102150405")+".docx")
	err := copyFile(testDocPath, tempFile)
	if err != nil {
		t.Fatalf("复制测试文档失败: %v", err)
	}
	
	return tempFile
}

// createTestDocxWithHashKeywords 创建包含井号关键词的测试文档
func createTestDocxWithHashKeywords(t *testing.T) string {
	// 使用包含测试关键词的测试文档
	testDocPath := "test/test_document.docx"
	if _, err := os.Stat(testDocPath); os.IsNotExist(err) {
		t.Skip("测试文档不存在，跳过测试")
	}
	
	// 复制到临时文件
	tempFile := filepath.Join(os.TempDir(), "test_hash_"+time.Now().Format("20060102150405")+".docx")
	err := copyFile(testDocPath, tempFile)
	if err != nil {
		t.Fatalf("复制测试文档失败: %v", err)
	}
	
	return tempFile
}

// copyFile 复制文件
func copyFile(src, dst string) error {
	sourceFile, err := os.Open(src)
	if err != nil {
		return err
	}
	defer sourceFile.Close()

	destFile, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer destFile.Close()

	_, err = destFile.ReadFrom(sourceFile)
	return err
}

// TestDocxProcessor_GetReplacementCount 测试获取替换计数
func TestDocxProcessor_GetReplacementCount(t *testing.T) {
	testFile := createTestDocx(t)
	defer os.Remove(testFile)

	processor, err := NewDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建 DocxProcessor 失败: %v", err)
	}
	defer processor.Close()

	replacements := map[string]string{
		"测试关键词": "替换内容",
		"不存在的关键词": "不会被替换",
	}

	err = processor.ReplaceKeywords(replacements, false)
	if err != nil {
		t.Fatalf("替换关键词失败: %v", err)
	}

	counts := processor.GetReplacementCount()
	if len(counts) != len(replacements) {
		t.Errorf("替换计数数量不匹配，期望 %d，实际 %d", len(replacements), len(counts))
	}

	// 检查存在的关键词被替换
	if count, exists := counts["测试关键词"]; !exists || count == 0 {
		t.Error("'测试关键词' 应该被替换")
	}

	// 检查不存在的关键词计数为0
	if count, exists := counts["不存在的关键词"]; !exists || count != 0 {
		t.Error("'不存在的关键词' 计数应该为0")
	}
}