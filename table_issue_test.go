package main

import (
	"fmt"
	"os"
	"path/filepath"
	"testing"
)

// TestTableKeywordIssue 测试表格中关键词被分割的问题
func TestTableKeywordIssue(t *testing.T) {
	// 创建测试用的docx文件路径
	testFile := filepath.Join("test_input", "test_document.docx")
	
	// 检查测试文件是否存在
	if _, err := os.Stat(testFile); os.IsNotExist(err) {
		t.Skipf("测试文件不存在: %s", testFile)
		return
	}

	// 创建处理器
	processor, err := NewGoDocxProcessorFromFile(testFile)
	if err != nil {
		t.Fatalf("创建处理器失败: %v", err)
	}
	defer processor.Close()

	// 测试替换映射
	replacements := map[string]string{
		"abc": "替换后的内容",
		"测试关键词": "新的测试内容",
	}

	// 执行替换（使用井号包装）
	err = processor.ReplaceKeywordsWithOptions(replacements, true, true)
	if err != nil {
		t.Fatalf("替换失败: %v", err)
	}

	// 保存到临时文件
	tempFile := filepath.Join("test_output", "table_test_output.docx")
	os.MkdirAll(filepath.Dir(tempFile), 0755)
	
	err = processor.SaveAs(tempFile)
	if err != nil {
		t.Fatalf("保存文件失败: %v", err)
	}

	// 验证文件是否创建成功
	if _, err := os.Stat(tempFile); os.IsNotExist(err) {
		t.Fatalf("输出文件未创建: %s", tempFile)
	}

	// 获取替换计数
	count := processor.GetReplacementCount()
	t.Logf("替换计数: %+v", count)

	// 调试内容
	processor.DebugContent([]string{"abc", "测试关键词"})

	fmt.Printf("测试完成，输出文件: %s\n", tempFile)
	fmt.Println("请手动检查输出文件中表格内的关键词是否被正确替换")
}

// TestCreateSampleDocxWithTable 创建包含表格的示例docx文件用于测试
func TestCreateSampleDocxWithTable(t *testing.T) {
	// 这个测试用于手动创建包含表格和分割关键词的测试文件
	// 由于无法程序化创建复杂的docx文件，这里只是一个占位符
	t.Log("请手动创建包含以下内容的docx文件:")
	t.Log("1. 在表格单元格中放置 #abc# 关键词")
	t.Log("2. 在表格单元格中放置 #测试关键词# 关键词")
	t.Log("3. 确保关键词可能被XML标签分割")
	t.Log("4. 保存为 test_input/test_document.docx")
}

// TestAnalyzeDocxStructure 分析docx文件的内部结构
func TestAnalyzeDocxStructure(t *testing.T) {
	testFile := filepath.Join("test_input", "test_document.docx")
	
	if _, err := os.Stat(testFile); os.IsNotExist(err) {
		t.Skipf("测试文件不存在: %s", testFile)
		return
	}

	// 尝试读取docx文件的原始XML内容
	// 这需要我们实现一个更底层的分析工具
	t.Log("开始分析docx文件结构...")
	
	// 创建处理器进行调试
	processor, err := NewGoDocxProcessorFromFile(testFile)
	if err != nil {
		t.Fatalf("创建处理器失败: %v", err)
	}
	defer processor.Close()

	// 调试内容
	processor.DebugContent([]string{"abc", "测试关键词"})
}