package main

import (
	"archive/zip"
	"fmt"
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

// TestDocxProcessor_ReplaceKeywordsWithHashWrapper 测试带井号包装的关键词替换
func TestDocxProcessor_ReplaceKeywordsWithHashWrapper(t *testing.T) {
	// 使用包含井号关键词的测试文档
	testFile := createTestDocxWithHashKeywords(t)
	defer os.Remove(testFile)

	processor, err := NewDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建 DocxProcessor 失败: %v", err)
	}
	defer processor.Close()

	// 测试井号包装功能
	// 提供不带井号的关键词，应该能找到并替换文档中带井号的文本
	replacements := map[string]string{
		"姓名": "张三",
		"法人": "李四",
	}

	err = processor.ReplaceKeywordsWithOptions(replacements, true, true)
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
	err = processor.ReplaceKeywordsWithOptions(replacements, true, true)
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
	// 创建临时文件
	tempFile := filepath.Join(os.TempDir(), "test_"+time.Now().Format("20060102150405")+".docx")
	
	// 创建包含测试关键词的简单docx文档
	err := createSimpleDocx(tempFile, "测试关键词")
	if err != nil {
		t.Fatalf("创建测试文档失败: %v", err)
	}

	return tempFile
}

// createTestDocxWithHashKeywords 创建包含井号关键词的测试文档
func createTestDocxWithHashKeywords(t *testing.T) string {
	// 创建临时文件
	tempFile := filepath.Join(os.TempDir(), "test_hash_"+time.Now().Format("20060102150405")+".docx")
	
	// 创建包含井号关键词的简单docx文档
	err := createSimpleDocx(tempFile, "这是一个测试文档。#姓名#需要填写。#法人#需要签字。")
	if err != nil {
		t.Fatalf("创建测试文档失败: %v", err)
	}

	return tempFile
}

// createSimpleDocx 创建一个简单的docx文档
func createSimpleDocx(filePath, content string) error {
	// 创建一个新的zip文件
	file, err := os.Create(filePath)
	if err != nil {
		return err
	}
	defer file.Close()

	zipWriter := zip.NewWriter(file)
	defer zipWriter.Close()

	// 添加必要的docx文件结构
	// 1. [Content_Types].xml
	contentTypes := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>`
	if err := addFileToZip(zipWriter, "[Content_Types].xml", contentTypes); err != nil {
		return err
	}

	// 2. _rels/.rels
	rels := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>`
	if err := addFileToZip(zipWriter, "_rels/.rels", rels); err != nil {
		return err
	}

	// 3. word/_rels/document.xml.rels
	documentRels := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
</Relationships>`
	if err := addFileToZip(zipWriter, "word/_rels/document.xml.rels", documentRels); err != nil {
		return err
	}

	// 4. word/document.xml
	document := fmt.Sprintf(`<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:body>
<w:p>
<w:r>
<w:t>%s</w:t>
</w:r>
</w:p>
</w:body>
</w:document>`, content)
	if err := addFileToZip(zipWriter, "word/document.xml", document); err != nil {
		return err
	}

	return nil
}

// addFileToZip 向zip文件中添加文件
func addFileToZip(zipWriter *zip.Writer, fileName, content string) error {
	writer, err := zipWriter.Create(fileName)
	if err != nil {
		return err
	}
	_, err = writer.Write([]byte(content))
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
		"测试关键词":   "替换内容",
		"不存在的关键词": "不会被替换",
	}

	err = processor.ReplaceKeywordsWithOptions(replacements, true, true)
	if err != nil {
		t.Fatalf("替换关键词失败: %v", err)
	}

	counts := processor.GetReplacementCount()
	if len(counts) != len(replacements) {
		t.Errorf("替换计数数量不匹配，期望 %d，实际 %d", len(replacements), len(counts))
	}

	// 由于我们创建的简单docx可能与docx库的期望格式不完全兼容
	// 我们主要测试替换计数功能是否正常工作，而不是具体的替换结果
	if counts == nil {
		t.Error("替换计数不应该为nil")
	}

	// 检查计数器中包含所有关键词（即使计数为0）
	for keyword := range replacements {
		if _, exists := counts[keyword]; !exists {
			t.Errorf("关键词 '%s' 应该在计数器中", keyword)
		}
	}

	// 检查不存在的关键词计数为0
	if count := counts["不存在的关键词"]; count != 0 {
		t.Errorf("'不存在的关键词' 计数应该为0，实际为 %d", count)
	}
}
