package domain

import "context"

// DocumentProcessor 文档处理器接口
type DocumentProcessor interface {
	ProcessDocument(ctx context.Context, inputPath, outputPath string, replacements map[string]string) error
	ValidateDocument(inputPath string) error
}

// KeywordMatcher 关键词匹配器接口
type KeywordMatcher interface {
	FindMatches(content string, keywords map[string]string) []Match
	ReplaceMatches(content string, matches []Match) string
}

// TableProcessor 表格处理器接口
type TableProcessor interface {
	ProcessTableContent(content string, keywords map[string]string) (string, error)
	HandleSplitKeywords(content string) string
}

// Match 表示一个匹配项
type Match struct {
	Keyword     string // 原始关键词 (如 #AAA#)
	Replacement string // 替换值
	StartPos    int    // 开始位置
	EndPos      int    // 结束位置
}

// ProcessResult 处理结果
type ProcessResult struct {
	Success        bool
	ProcessedFiles int
	Replacements   int
	Errors         []error
}

// DocumentInfo 文档信息
type DocumentInfo struct {
	Path     string
	Size     int64
	Modified bool
}

// ReplacementStats 替换统计信息
type ReplacementStats struct {
	Keyword      string
	Occurrences  int
	InTables     int
	InParagraphs int
}