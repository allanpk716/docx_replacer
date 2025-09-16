package processor

import (
	"strings"
	"testing"
)

func TestNewTableProcessor(t *testing.T) {
	processor := NewTableProcessor()
	if processor == nil {
		t.Error("NewTableProcessor 返回 nil")
	}
}

func TestTableProcessor_ProcessTableContent(t *testing.T) {
	processor := NewTableProcessor()
	keywordMap := map[string]string{
		"#NAME#": "John",
		"#AGE#":  "25",
		"#CITY#": "Beijing",
	}

	tests := []struct {
		name     string
		content  string
		want     string
		wantRepl int
	}{
		{
			name:     "simple replacement",
			content:  "Hello #NAME#, you are #AGE# years old.",
			want:     "Hello John, you are 25 years old.",
			wantRepl: 2,
		},
		{
			name:     "split keyword in XML",
			content:  "<w:t>#NA</w:t><w:t>ME#</w:t> is a good person.",
			want:     "<w:t>John</w:t> is a good person.",
			wantRepl: 1,
		},
		{
			name:     "multiple split keywords",
			content:  "<w:t>#N</w:t><w:t>A</w:t><w:t>ME#</w:t> is <w:t>#A</w:t><w:t>GE#</w:t> years old.",
			want:     "<w:t>John</w:t> is <w:t>25</w:t> years old.",
			wantRepl: 2,
		},
		{
			name:     "no keywords",
			content:  "<w:t>This is normal text</w:t>",
			want:     "<w:t>This is normal text</w:t>",
			wantRepl: 0,
		},
		{
			name:     "mixed normal and split keywords",
			content:  "#NAME# lives in <w:t>#CI</w:t><w:t>TY#</w:t>",
			want:     "John lives in <w:t>Beijing</w:t>",
			wantRepl: 2,
		},
		{
			name:     "empty content",
			content:  "",
			want:     "",
			wantRepl: 0,
		},
		{
			name:     "partial keyword split",
			content:  "<w:t>#NAM</w:t><w:t>E</w:t> is incomplete",
			want:     "<w:t>#NAM</w:t><w:t>E</w:t> is incomplete",
			wantRepl: 0,
		},
		{
			name:     "keyword across multiple tags",
			content:  "<w:t>#</w:t><w:t>N</w:t><w:t>A</w:t><w:t>M</w:t><w:t>E</w:t><w:t>#</w:t>",
			want:     "<w:t>#</w:t><w:t>N</w:t><w:t>A</w:t><w:t>M</w:t><w:t>E</w:t><w:t>#</w:t>",
			wantRepl: 1,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result, err := processor.ProcessTableContent(tt.content, keywordMap)

			if result != tt.want {
				t.Errorf("ProcessTableContent() result = %v, 期望 %v", result, tt.want)
			}

			if err != nil {
				t.Errorf("ProcessTableContent() error = %v, 期望无错误", err)
			}
		})
	}
}

// 测试处理表格内容的完整流程
func TestTableProcessor_FullProcessing(t *testing.T) {
	processor := NewTableProcessor()
	keywordMap := map[string]string{
		"#NAME#": "John",
		"#AGE#":  "25",
	}

	// 测试正常关键词替换
	content1 := "Hello #NAME#, you are #AGE# years old."
	result1, err1 := processor.ProcessTableContent(content1, keywordMap)
	if err1 != nil {
		t.Errorf("ProcessTableContent() error = %v", err1)
	}
	expected1 := "Hello John, you are 25 years old."
	if result1 != expected1 {
		t.Errorf("ProcessTableContent() = %v, 期望 %v", result1, expected1)
	}

	// 测试分割关键词处理
	content2 := "<w:t>#N</w:t><w:t>AME#</w:t> is <w:t>#A</w:t><w:t>GE#</w:t>"
	result2, err2 := processor.ProcessTableContent(content2, keywordMap)
	if err2 != nil {
		t.Errorf("ProcessTableContent() error = %v", err2)
	}
	// 检查是否包含替换后的值
	if !strings.Contains(result2, "John") || !strings.Contains(result2, "25") {
		t.Errorf("ProcessTableContent() = %v, 期望包含 John 和 25", result2)
	}
}