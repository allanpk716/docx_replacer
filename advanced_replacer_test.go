package main

import (
	"os"
	"path/filepath"
	"testing"
)

// TestAdvancedReplacer 测试高级替换器
func TestAdvancedReplacer(t *testing.T) {
	// 确保测试输出目录存在
	outputDir := "test_output"
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		t.Fatalf("创建输出目录失败: %v", err)
	}

	// 使用现有的测试文件
	testFile := "test_files/test_template.docx"
	if _, err := os.Stat(testFile); os.IsNotExist(err) {
		t.Skipf("测试文件不存在: %s", testFile)
	}

	// 创建高级处理器
	processor, err := NewAdvancedDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建高级处理器失败: %v", err)
	}
	defer processor.Close()

	// 准备替换数据
	replacements := map[string]string{
		"abc":      "高级替换内容",
		"测试关键词": "新的高级测试内容",
	}

	// 调试原始内容
	processor.DebugContent([]string{"abc", "测试关键词"})

	// 执行高级替换
	err = processor.TableAwareReplace(replacements, true, true)
	if err != nil {
		t.Fatalf("高级替换失败: %v", err)
	}

	// 保存结果
	outputFile := filepath.Join(outputDir, "advanced_test_output.docx")
	err = processor.SaveAs(outputFile)
	if err != nil {
		t.Fatalf("保存文件失败: %v", err)
	}

	// 检查替换计数
	count := processor.GetReplacementCount()
	t.Logf("高级替换计数: %v", count)

	// 验证输出文件存在
	if _, err := os.Stat(outputFile); os.IsNotExist(err) {
		t.Errorf("输出文件不存在: %s", outputFile)
	}

	t.Logf("高级替换测试完成，输出文件: %s", outputFile)
	t.Log("请手动检查输出文件中表格内的关键词是否被正确替换")
}

// TestAdvancedReplacerComparison 对比测试：普通替换 vs 高级替换
func TestAdvancedReplacerComparison(t *testing.T) {
	// 确保测试输出目录存在
	outputDir := "test_output"
	if err := os.MkdirAll(outputDir, 0755); err != nil {
		t.Fatalf("创建输出目录失败: %v", err)
	}

	testFile := "test_files/test_template.docx"
	if _, err := os.Stat(testFile); os.IsNotExist(err) {
		t.Skipf("测试文件不存在: %s", testFile)
	}

	replacements := map[string]string{
		"abc":      "对比测试内容",
		"测试关键词": "新的对比测试内容",
	}

	// 1. 使用普通处理器
	t.Log("=== 普通处理器测试 ===")
	normalProcessor, err := NewGoDocxProcessorFromFile(testFile)
	if err != nil {
		t.Fatalf("创建普通处理器失败: %v", err)
	}
	defer normalProcessor.Close()

	err = normalProcessor.ReplaceKeywordsWithOptions(replacements, true, true)
	if err != nil {
		t.Fatalf("普通替换失败: %v", err)
	}

	normalOutput := filepath.Join(outputDir, "normal_comparison_output.docx")
	err = normalProcessor.SaveAs(normalOutput)
	if err != nil {
		t.Fatalf("保存普通处理器结果失败: %v", err)
	}

	normalCount := normalProcessor.GetReplacementCount()
	t.Logf("普通处理器替换计数: %v", normalCount)

	// 2. 使用高级处理器
	t.Log("=== 高级处理器测试 ===")
	advancedProcessor, err := NewAdvancedDocxProcessor(testFile)
	if err != nil {
		t.Fatalf("创建高级处理器失败: %v", err)
	}
	defer advancedProcessor.Close()

	err = advancedProcessor.TableAwareReplace(replacements, true, true)
	if err != nil {
		t.Fatalf("高级替换失败: %v", err)
	}

	advancedOutput := filepath.Join(outputDir, "advanced_comparison_output.docx")
	err = advancedProcessor.SaveAs(advancedOutput)
	if err != nil {
		t.Fatalf("保存高级处理器结果失败: %v", err)
	}

	advancedCount := advancedProcessor.GetReplacementCount()
	t.Logf("高级处理器替换计数: %v", advancedCount)

	// 3. 对比结果
	t.Log("=== 对比结果 ===")
	for key := range replacements {
		normalC := normalCount[key]
		advancedC := advancedCount[key]
		t.Logf("关键词 '%s': 普通处理器=%d, 高级处理器=%d", key, normalC, advancedC)
		
		if advancedC > normalC {
			t.Logf("✓ 高级处理器找到了更多的 '%s' 关键词 (+%d)", key, advancedC-normalC)
		} else if advancedC == normalC {
			t.Logf("= 两个处理器对 '%s' 的处理结果相同", key)
		} else {
			t.Logf("? 高级处理器对 '%s' 的处理结果较少 (-%d)", key, normalC-advancedC)
		}
	}

	t.Logf("对比测试完成")
	t.Logf("普通处理器输出: %s", normalOutput)
	t.Logf("高级处理器输出: %s", advancedOutput)
	t.Log("请手动对比两个输出文件的差异")
}

// TestCreateTableDocxForAdvanced 创建包含表格的测试文档用于高级测试
func TestCreateTableDocxForAdvanced(t *testing.T) {
	// 这个测试需要手动创建一个包含被分割关键词的docx文件
	// 由于go-docx库的限制，我们无法程序化创建复杂的表格结构
	t.Log("请手动创建一个包含以下内容的docx文件:")
	t.Log("1. 在表格单元格中输入: #abc#")
	t.Log("2. 在表格单元格中输入: #测试关键词#")
	t.Log("3. 保存为 test_files/table_split_test.docx")
	t.Log("4. 然后运行高级替换器测试")
	t.Log("")
	t.Log("注意: 确保在输入关键词时，Word可能会将其分割成多个XML标签")
	t.Log("这通常发生在复制粘贴或者格式化操作之后")
}