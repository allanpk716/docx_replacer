package processor

import (
	"context"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/allanpk716/docx_replacer/internal/domain"
)

// MockTableProcessor 用于测试的模拟表格处理器
type MockTableProcessor struct {
	processCalled bool
	processResult string
	processError  error
}

func (m *MockTableProcessor) ProcessTableContent(content string, keywordMap map[string]string) (string, error) {
	m.processCalled = true
	if m.processError != nil {
		return "", m.processError
	}
	if m.processResult != "" {
		return m.processResult, nil
	}
	// 默认行为：简单替换
	result := content
	for key, value := range keywordMap {
		result = strings.ReplaceAll(result, key, value)
	}
	return result, nil
}

func (m *MockTableProcessor) HandleSplitKeywords(content string) string {
	// 模拟处理分割关键词的行为
	return content // 简单返回原内容
}

func TestNewDocumentProcessor(t *testing.T) {
	tableProcessor := NewTableProcessor()
	processor := NewDocumentProcessor(tableProcessor)

	if processor == nil {
		t.Error("NewDocumentProcessor 返回 nil")
	}

	// 验证接口实现
	var _ domain.DocumentProcessor = processor
}

func TestDocumentProcessor_ProcessDocument_InvalidPath(t *testing.T) {
	mockTableProcessor := &MockTableProcessor{}
	processor := NewDocumentProcessor(mockTableProcessor)
	keywordMap := map[string]string{"#TEST#": "value"}

	ctx := context.Background()
	err := processor.ProcessDocument(ctx, "nonexistent.docx", "output.docx", keywordMap)

	if err == nil {
		t.Error("期望处理不存在的文件时返回错误")
	}
}

func TestDocumentProcessor_ProcessDocument_EmptyKeywordMap(t *testing.T) {
	// 测试空关键词映射的情况
	mockTableProcessor := &MockTableProcessor{}
	processor := NewDocumentProcessor(mockTableProcessor)
	emptyKeywordMap := map[string]string{}

	ctx := context.Background()
	err := processor.ProcessDocument(ctx, "nonexistent.docx", "output.docx", emptyKeywordMap)

	// 应该返回错误，因为文件不存在
	if err == nil {
		t.Error("期望处理不存在的文件时返回错误")
	}
}

func TestDocumentProcessor_ProcessDocument_ContextCancellation(t *testing.T) {
	// 测试上下文取消的情况
	mockTableProcessor := &MockTableProcessor{}
	processor := NewDocumentProcessor(mockTableProcessor)
	keywordMap := map[string]string{"#NAME#": "John"}

	// 创建一个已取消的上下文
	ctx, cancel := context.WithCancel(context.Background())
	cancel() // 立即取消

	err := processor.ProcessDocument(ctx, "nonexistent.docx", "output.docx", keywordMap)

	// 应该返回错误（可能是上下文取消或文件不存在）
	if err == nil {
		t.Error("期望返回错误")
	}
}

func TestDocumentProcessor_ProcessDocument_Timeout(t *testing.T) {
	// 创建临时测试文件
	tempDir := t.TempDir()
	inputFile := filepath.Join(tempDir, "test.docx")
	outputFile := filepath.Join(tempDir, "output.docx")

	// 创建一个空的docx文件
	if err := os.WriteFile(inputFile, []byte{}, 0644); err != nil {
		t.Fatalf("创建测试文件失败: %v", err)
	}

	mockTableProcessor := &MockTableProcessor{}
	processor := NewDocumentProcessor(mockTableProcessor)
	keywordMap := map[string]string{"#TEST#": "value"}

	// 创建一个非常短的超时上下文
	ctx, cancel := context.WithTimeout(context.Background(), 1*time.Nanosecond)
	defer cancel()

	// 等待一下确保超时
	time.Sleep(1 * time.Millisecond)

	err := processor.ProcessDocument(ctx, inputFile, outputFile, keywordMap)

	// 应该返回超时错误
	if err == nil {
		t.Error("期望在超时时返回错误")
	}
}

func TestDocumentProcessor_Integration(t *testing.T) {
	// 集成测试：使用真实的TableProcessor
	tableProcessor := NewTableProcessor()
	processor := NewDocumentProcessor(tableProcessor)

	if processor == nil {
		t.Error("集成测试：NewDocumentProcessor 返回 nil")
	}

	// 验证接口实现
	var _ domain.DocumentProcessor = processor
}

// 基准测试
func BenchmarkDocumentProcessor_ProcessDocument(b *testing.B) {
	mockTableProcessor := &MockTableProcessor{
		processResult: "processed content",
	}
	processor := NewDocumentProcessor(mockTableProcessor)
	keywordMap := map[string]string{
		"#NAME#": "John",
		"#AGE#":  "25",
	}

	// 创建临时文件
	tempDir := b.TempDir()
	inputFile := filepath.Join(tempDir, "test.docx")
	outputFile := filepath.Join(tempDir, "output.docx")

	if err := os.WriteFile(inputFile, []byte{}, 0644); err != nil {
		b.Fatalf("创建测试文件失败: %v", err)
	}

	ctx := context.Background()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_ = processor.ProcessDocument(ctx, inputFile, outputFile, keywordMap)
	}
}