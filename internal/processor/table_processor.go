package processor

import (
	"regexp"
	"strings"

	"github.com/allanpk716/docx_replacer/internal/domain"
	"github.com/allanpk716/docx_replacer/internal/matcher"
)

// tableProcessor 表格处理器实现
type tableProcessor struct {
	keywordMatcher domain.KeywordMatcher
	// XML 标签模式，用于识别被分割的关键词
	xmlTagPattern *regexp.Regexp
	// 关键词模式，用于重组分割的关键词
	keywordPattern *regexp.Regexp
}

// NewTableProcessor 创建新的表格处理器
func NewTableProcessor() domain.TableProcessor {
	return &tableProcessor{
		keywordMatcher: matcher.NewKeywordMatcher(),
		// 匹配 XML 标签的正则表达式
		xmlTagPattern: regexp.MustCompile(`<[^>]*>`),
		// 匹配可能被分割的关键词模式 (#...#)
		keywordPattern: regexp.MustCompile(`#[^#]*#`),
	}
}

// ProcessTableContent 处理表格内容中的关键词替换
func (tp *tableProcessor) ProcessTableContent(content string, keywords map[string]string) (string, error) {
	if content == "" {
		return content, nil
	}

	// 首先处理被分割的关键词
	processedContent := tp.HandleSplitKeywords(content)

	// 然后进行关键词替换
	matches := tp.keywordMatcher.FindMatches(processedContent, keywords)
	result := tp.keywordMatcher.ReplaceMatches(processedContent, matches)

	return result, nil
}

// HandleSplitKeywords 处理被 XML 标签分割的关键词
func (tp *tableProcessor) HandleSplitKeywords(content string) string {
	if content == "" {
		return content
	}

	// 移除所有 XML 标签，获得纯文本
	cleanContent := tp.xmlTagPattern.ReplaceAllString(content, "")

	// 查找可能的关键词模式
	keywordMatches := tp.keywordPattern.FindAllString(cleanContent, -1)
	if len(keywordMatches) == 0 {
		return content
	}

	// 尝试重组分割的关键词
	reconstructed := tp.reconstructSplitKeywords(content, keywordMatches)
	return reconstructed
}

// reconstructSplitKeywords 重组被分割的关键词
func (tp *tableProcessor) reconstructSplitKeywords(originalContent string, potentialKeywords []string) string {
	result := originalContent

	// 对每个潜在的关键词，尝试在原始内容中找到并重组
	for _, keyword := range potentialKeywords {
		if tp.isValidKeyword(keyword) {
			// 创建分割模式，查找被 XML 标签分割的关键词
			splitPattern := tp.createSplitPattern(keyword)
			if splitPattern != nil {
				// 替换分割的关键词为完整的关键词
				result = splitPattern.ReplaceAllString(result, keyword)
			}
		}
	}

	return result
}

// createSplitPattern 为关键词创建分割模式
func (tp *tableProcessor) createSplitPattern(keyword string) *regexp.Regexp {
	if len(keyword) < 3 {
		return nil
	}

	// 移除首尾的 # 符号
	keywordCore := keyword[1 : len(keyword)-1]
	if keywordCore == "" {
		return nil
	}

	// 构建匹配被分割关键词的正则表达式
	// 例如：#AAA# 可能被分割为 #<tag>A</tag>AA# 或 #A<tag>A</tag>A#
	var patternParts []string
	patternParts = append(patternParts, "#")

	// 为关键词的每个字符创建可能被 XML 标签分割的模式
	for i, char := range keywordCore {
		if i > 0 {
			// 在字符之间可能有 XML 标签
			patternParts = append(patternParts, `(?:<[^>]*>)*`)
		}
		patternParts = append(patternParts, regexp.QuoteMeta(string(char)))
	}

	patternParts = append(patternParts, `(?:<[^>]*>)*`)
	patternParts = append(patternParts, "#")

	patternStr := strings.Join(patternParts, "")
	pattern, err := regexp.Compile(patternStr)
	if err != nil {
		return nil
	}

	return pattern
}

// isValidKeyword 检查是否是有效的关键词格式
func (tp *tableProcessor) isValidKeyword(keyword string) bool {
	return matcher.ValidateKeywordFormat(keyword)
}

// ExtractTableKeywords 从表格内容中提取所有可能的关键词
func (tp *tableProcessor) ExtractTableKeywords(content string) []string {
	var keywords []string

	// 先处理分割的关键词
	processedContent := tp.HandleSplitKeywords(content)

	// 提取所有关键词
	matches := tp.keywordPattern.FindAllString(processedContent, -1)
	for _, match := range matches {
		if tp.isValidKeyword(match) {
			keywords = append(keywords, match)
		}
	}

	return keywords
}

// GetTableProcessingStats 获取表格处理统计信息
func (tp *tableProcessor) GetTableProcessingStats(content string, keywords map[string]string) map[string]int {
	stats := make(map[string]int)

	// 处理分割的关键词
	processedContent := tp.HandleSplitKeywords(content)

	// 统计每个关键词的出现次数
	for keyword := range keywords {
		count := strings.Count(processedContent, keyword)
		stats[keyword] = count
	}

	return stats
}

// CleanXMLTags 清理 XML 标签，返回纯文本
func (tp *tableProcessor) CleanXMLTags(content string) string {
	return tp.xmlTagPattern.ReplaceAllString(content, "")
}

// HasSplitKeywords 检查内容是否包含被分割的关键词
func (tp *tableProcessor) HasSplitKeywords(content string) bool {
	// 检查是否有 XML 标签
	hasXMLTags := tp.xmlTagPattern.MatchString(content)
	if !hasXMLTags {
		return false
	}

	// 检查清理后的内容是否有关键词
	cleanContent := tp.CleanXMLTags(content)
	hasKeywords := tp.keywordPattern.MatchString(cleanContent)

	return hasKeywords
}

// DebugSplitKeywords 调试分割关键词的处理过程
func (tp *tableProcessor) DebugSplitKeywords(content string) map[string]interface{} {
	debugInfo := make(map[string]interface{})
	debugInfo["original_content"] = content
	debugInfo["has_xml_tags"] = tp.xmlTagPattern.MatchString(content)
	debugInfo["clean_content"] = tp.CleanXMLTags(content)
	debugInfo["extracted_keywords"] = tp.ExtractTableKeywords(content)
	debugInfo["processed_content"] = tp.HandleSplitKeywords(content)

	return debugInfo
}