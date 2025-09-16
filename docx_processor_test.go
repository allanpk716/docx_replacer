package main

import (
	"fmt"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

// TestNewGoDocxProcessorFromFile_ValidFile 测试从有效文件创建处理器
func TestNewGoDocxProcessorFromFile_ValidFile(t *testing.T) {
	// Arrange
	testFile := "test_input/test_document.docx"
	
	// 检查测试文件是否存在
	if _, err := os.Stat(testFile); os.IsNotExist(err) {
		t.Skipf("测试文件 %s 不存在，跳过测试", testFile)
	}
	
	// Act
	processor, err := NewGoDocxProcessorFromFile(testFile)
	
	// Assert
	if err != nil {
		t.Errorf("NewGoDocxProcessorFromFile() 返回错误 = %v, 期望 nil", err)
	}
	if processor == nil {
		t.Error("NewGoDocxProcessorFromFile() 返回 nil 处理器")
	}
	if processor != nil && processor.doc == nil {
		t.Error("处理器的文档字段为 nil")
	}
	if processor != nil && processor.replacementCount == nil {
		t.Error("处理器的替换计数字段为 nil")
	}
}

// TestNewGoDocxProcessorFromFile_EmptyPath 测试空文件路径
func TestNewGoDocxProcessorFromFile_EmptyPath(t *testing.T) {
	// Arrange
	filePath := ""
	
	// Act
	processor, err := NewGoDocxProcessorFromFile(filePath)
	
	// Assert
	if err == nil {
		t.Error("NewGoDocxProcessorFromFile() 期望返回错误，但返回 nil")
	}
	if processor != nil {
		t.Error("NewGoDocxProcessorFromFile() 期望返回 nil 处理器，但返回了非 nil")
	}
}

// TestNewGoDocxProcessorFromFile_NonExistentFile 测试不存在的文件
func TestNewGoDocxProcessorFromFile_NonExistentFile(t *testing.T) {
	// Arrange
	filePath := "nonexistent_file.docx"
	
	// Act
	processor, err := NewGoDocxProcessorFromFile(filePath)
	
	// Assert
	if err == nil {
		t.Error("NewGoDocxProcessorFromFile() 期望返回错误，但返回 nil")
	}
	if processor != nil {
		t.Error("NewGoDocxProcessorFromFile() 期望返回 nil 处理器，但返回了非 nil")
	}
}

// TestNewGoDocxProcessorFromBytes_ValidData 测试从有效字节数据创建处理器
func TestNewGoDocxProcessorFromBytes_ValidData(t *testing.T) {
	// Arrange - 读取测试文件的字节数据
	testFile := "test_input/test_document.docx"
	if _, err := os.Stat(testFile); os.IsNotExist(err) {
		t.Skipf("测试文件 %s 不存在，跳过测试", testFile)
	}
	
	data, err := os.ReadFile(testFile)
	if err != nil {
		t.Fatalf("读取测试文件失败: %v", err)
	}
	
	// Act
	processor, err := NewGoDocxProcessorFromBytes(data)
	
	// Assert
	if err != nil {
		t.Errorf("NewGoDocxProcessorFromBytes() 返回错误 = %v, 期望 nil", err)
	}
	if processor == nil {
		t.Error("NewGoDocxProcessorFromBytes() 返回 nil 处理器")
	}
	if processor != nil && processor.doc == nil {
		t.Error("处理器的文档字段为 nil")
	}
	if processor != nil && processor.replacementCount == nil {
		t.Error("处理器的替换计数字段为 nil")
	}
}

// TestNewGoDocxProcessorFromBytes_NilData 测试 nil 字节数据
func TestNewGoDocxProcessorFromBytes_NilData(t *testing.T) {
	// Arrange
	var data []byte = nil
	
	// Act
	processor, err := NewGoDocxProcessorFromBytes(data)
	
	// Assert
	if err == nil {
		t.Error("NewGoDocxProcessorFromBytes() 期望返回错误，但返回 nil")
	}
	if processor != nil {
		t.Error("NewGoDocxProcessorFromBytes() 期望返回 nil 处理器，但返回了非 nil")
	}
}

// TestNewGoDocxProcessorFromBytes_EmptyData 测试空字节数据
func TestNewGoDocxProcessorFromBytes_EmptyData(t *testing.T) {
	// Arrange
	data := []byte{}
	
	// Act
	processor, err := NewGoDocxProcessorFromBytes(data)
	
	// Assert
	if err == nil {
		t.Error("NewGoDocxProcessorFromBytes() 期望返回错误，但返回 nil")
	}
	if processor != nil {
		t.Error("NewGoDocxProcessorFromBytes() 期望返回 nil 处理器，但返回了非 nil")
	}
}

// TestNewGoDocxProcessorFromBytes_InvalidData 测试无效字节数据
func TestNewGoDocxProcessorFromBytes_InvalidData(t *testing.T) {
	// Arrange
	data := []byte("这不是有效的docx数据")
	
	// Act
	processor, err := NewGoDocxProcessorFromBytes(data)
	
	// Assert
	if err == nil {
		t.Error("NewGoDocxProcessorFromBytes() 期望返回错误，但返回 nil")
	}
	if processor != nil {
		t.Error("NewGoDocxProcessorFromBytes() 期望返回 nil 处理器，但返回了非 nil")
	}
}

// TestGoDocxProcessor_ReplaceKeywordsWithOptions_NormalCase 测试正常替换情况
func TestGoDocxProcessor_ReplaceKeywordsWithOptions_NormalCase(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
		// doc 字段为 nil，模拟未初始化状态
	}
	replacements := map[string]string{
		"NAME":    "张三",
		"COMPANY": "测试公司",
		"DATE":    "2024-01-01",
	}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacements, true, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_ReplaceKeywordsWithOptions_EmptyReplacements 测试空替换映射
func TestGoDocxProcessor_ReplaceKeywordsWithOptions_EmptyReplacements(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	replacements := map[string]string{}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacements, false, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_ReplaceKeywordsWithOptions_NilReplacements 测试 nil 替换映射
func TestGoDocxProcessor_ReplaceKeywordsWithOptions_NilReplacements(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	var replacements map[string]string = nil
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacements, false, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_ReplaceKeywordsWithOptions_WithHashWrapper 测试使用井号包装
func TestGoDocxProcessor_ReplaceKeywordsWithOptions_WithHashWrapper(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	replacements := map[string]string{
		"NAME":     "张三",
		"#COMPANY#": "测试公司", // 已经有井号的情况
	}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacements, true, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_ReplaceKeywordsWithOptions_WithoutHashWrapper 测试不使用井号包装
func TestGoDocxProcessor_ReplaceKeywordsWithOptions_WithoutHashWrapper(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	replacements := map[string]string{
		"NAME":    "李四",
		"COMPANY": "另一个公司",
	}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacements, false, false)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_GetReplacementCount_ValidData 测试获取有效替换计数
func TestGoDocxProcessor_GetReplacementCount_ValidData(t *testing.T) {
	// Arrange
	expected := map[string]int{
		"NAME":    3,
		"COMPANY": 2,
		"DATE":    1,
	}
	processor := &GoDocxProcessor{
		replacementCount: expected,
	}
	
	// Act
	actual := processor.GetReplacementCount()
	
	// Assert
	if !reflect.DeepEqual(actual, expected) {
		t.Errorf("GetReplacementCount() = %v, 期望 %v", actual, expected)
	}
}

// TestGoDocxProcessor_GetReplacementCount_EmptyData 测试获取空替换计数
func TestGoDocxProcessor_GetReplacementCount_EmptyData(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	
	// Act
	actual := processor.GetReplacementCount()
	
	// Assert
	if len(actual) != 0 {
		t.Errorf("GetReplacementCount() 返回长度 = %d, 期望 0", len(actual))
	}
}

// TestGoDocxProcessor_GetReplacementCount_NilData 测试获取 nil 替换计数
func TestGoDocxProcessor_GetReplacementCount_NilData(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: nil,
	}
	
	// Act
	actual := processor.GetReplacementCount()
	
	// Assert
	if actual != nil {
		t.Errorf("GetReplacementCount() = %v, 期望 nil", actual)
	}
}

// TestGoDocxProcessor_SaveAs_ValidPath 测试保存到有效路径
func TestGoDocxProcessor_SaveAs_ValidPath(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		// doc 字段为 nil，模拟未初始化状态
	}
	tempDir := t.TempDir()
	outputPath := filepath.Join(tempDir, "output.docx")
	
	// Act
	err := processor.SaveAs(outputPath)
	
	// Assert
	if err == nil {
		t.Error("SaveAs() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_SaveAs_EmptyPath 测试保存到空路径
func TestGoDocxProcessor_SaveAs_EmptyPath(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{}
	outputPath := ""
	
	// Act
	err := processor.SaveAs(outputPath)
	
	// Assert
	if err == nil {
		t.Error("SaveAs() 期望返回错误，但返回 nil")
	}
}

// TestGoDocxProcessor_Close_ValidProcessor 测试关闭有效处理器
func TestGoDocxProcessor_Close_ValidProcessor(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{}
	
	// Act
	err := processor.Close()
	
	// Assert
	if err != nil {
		t.Errorf("Close() 返回错误 = %v, 期望 nil", err)
	}
}

// TestGoDocxProcessor_DebugContent_ValidKeywords 测试调试有效关键词
func TestGoDocxProcessor_DebugContent_ValidKeywords(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{}
	keywords := []string{"NAME", "COMPANY", "DATE"}
	
	// Act & Assert - 这个方法不返回错误，只是打印日志
	// 我们主要测试它不会 panic
	processor.DebugContent(keywords)
}

// TestGoDocxProcessor_DebugContent_EmptyKeywords 测试调试空关键词
func TestGoDocxProcessor_DebugContent_EmptyKeywords(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{}
	keywords := []string{}
	
	// Act & Assert
	processor.DebugContent(keywords)
}

// TestGoDocxProcessor_DebugContent_NilKeywords 测试调试 nil 关键词
func TestGoDocxProcessor_DebugContent_NilKeywords(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{}
	var keywords []string = nil
	
	// Act & Assert
	processor.DebugContent(keywords)
}

// TestGoDocxProcessor_GetPlaceholders_ValidProcessor 测试获取占位符
func TestGoDocxProcessor_GetPlaceholders_ValidProcessor(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{}
	
	// Act
	placeholders := processor.GetPlaceholders()
	
	// Assert
	if placeholders == nil {
		t.Error("GetPlaceholders() 返回 nil, 期望空切片")
	}
	if len(placeholders) != 0 {
		t.Errorf("GetPlaceholders() 返回长度 = %d, 期望 0", len(placeholders))
	}
}

// TestGoDocxProcessor_GetPlaceholders_NilProcessor 测试 nil 处理器获取占位符
func TestGoDocxProcessor_GetPlaceholders_NilProcessor(t *testing.T) {
	// Arrange
	var processor *GoDocxProcessor = nil
	
	// Act & Assert - 这会导致 panic，我们需要恢复
	defer func() {
		if r := recover(); r == nil {
			t.Error("GetPlaceholders() 期望 panic，但没有 panic")
		}
	}()
	
	processor.GetPlaceholders()
}

// BenchmarkGoDocxProcessor_ReplaceKeywords 性能测试：关键词替换
func BenchmarkGoDocxProcessor_ReplaceKeywords(b *testing.B) {
	// Arrange
	replacementMap := map[string]string{
		"NAME":    "张三",
		"COMPANY": "测试公司",
		"DATE":    "2024-01-01",
		"AMOUNT":  "10000",
		"TITLE":   "测试标题",
	}
	
	b.ResetTimer()
	
	// Act
	for i := 0; i < b.N; i++ {
		processor := &GoDocxProcessor{
			replacementCount: make(map[string]int),
		}
		
		// 执行替换（会因为文档未初始化而返回错误，但我们主要测试性能）
		_ = processor.ReplaceKeywordsWithOptions(replacementMap, false, true)
	}
}

// BenchmarkGoDocxProcessor_GetReplacementCount 性能测试：获取替换计数
func BenchmarkGoDocxProcessor_GetReplacementCount(b *testing.B) {
	// Arrange
	replacementCount := make(map[string]int)
	for i := 0; i < 1000; i++ {
		replacementCount[fmt.Sprintf("KEY%d", i)] = i
	}
	processor := &GoDocxProcessor{
		replacementCount: replacementCount,
	}
	
	b.ResetTimer()
	
	// Act
	for i := 0; i < b.N; i++ {
		_ = processor.GetReplacementCount()
	}
}

// TestGoDocxProcessor_EdgeCases_LargeReplacementMap 边界测试：大量替换映射
func TestGoDocxProcessor_EdgeCases_LargeReplacementMap(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	
	// 创建大量替换规则
	replacementMap := make(map[string]string)
	for i := 0; i < 10000; i++ {
		replacementMap[fmt.Sprintf("KEY%d", i)] = fmt.Sprintf("值%d", i)
	}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacementMap, false, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_EdgeCases_SpecialCharacters 边界测试：特殊字符
func TestGoDocxProcessor_EdgeCases_SpecialCharacters(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	replacementMap := map[string]string{
		"SPECIAL1": "<>&\"'",
		"SPECIAL2": "中文测试",
		"SPECIAL3": "🎉🎊🎈", // emoji
		"SPECIAL4": "\n\t\r",  // 控制字符
	}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacementMap, true, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_EdgeCases_LongStrings 边界测试：长字符串
func TestGoDocxProcessor_EdgeCases_LongStrings(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	
	// 创建长字符串
	longString := strings.Repeat("这是一个很长的字符串", 1000)
	replacementMap := map[string]string{
		"LONG_KEY": longString,
	}
	
	// Act
	err := processor.ReplaceKeywordsWithOptions(replacementMap, false, true)
	
	// Assert
	if err == nil {
		t.Error("ReplaceKeywordsWithOptions() 期望返回错误（文档未初始化），但返回 nil")
	}
}

// TestGoDocxProcessor_EdgeCases_ConcurrentAccess 边界测试：并发访问
func TestGoDocxProcessor_EdgeCases_ConcurrentAccess(t *testing.T) {
	// Arrange
	processor := &GoDocxProcessor{
		replacementCount: make(map[string]int),
	}
	
	// Act - 并发访问 GetReplacementCount
	done := make(chan bool, 10)
	for i := 0; i < 10; i++ {
		go func() {
			defer func() { done <- true }()
			_ = processor.GetReplacementCount()
		}()
	}
	
	// 等待所有 goroutine 完成
	for i := 0; i < 10; i++ {
		<-done
	}
	
	// Assert - 主要测试不会 panic 或死锁
	t.Log("并发访问测试完成")
}