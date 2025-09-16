package matcher

import (
	"regexp"
	"sort"
	"strings"

	"github.com/allanpk716/docx_replacer/internal/domain"
)

// keywordMatcher 关键词匹配器实现
type keywordMatcher struct {
	patternCache map[string]*regexp.Regexp
}

// NewKeywordMatcher 创建新的关键词匹配器
func NewKeywordMatcher() domain.KeywordMatcher {
	return &keywordMatcher{
		patternCache: make(map[string]*regexp.Regexp),
	}
}

// FindMatches 在内容中查找所有匹配的关键词
func (km *keywordMatcher) FindMatches(content string, keywords map[string]string) []domain.Match {
	var matches []domain.Match

	// 遍历所有关键词进行匹配
	for keyword, replacement := range keywords {
		// 转义特殊字符，确保精确匹配
		escapedKeyword := regexp.QuoteMeta(keyword)
		pattern := km.getOrCreatePattern(escapedKeyword)

		// 查找所有匹配位置
		indexes := pattern.FindAllStringIndex(content, -1)
		for _, index := range indexes {
			matches = append(matches, domain.Match{
				Keyword:     keyword,
				Replacement: replacement,
				StartPos:    index[0],
				EndPos:      index[1],
			})
		}
	}

	// 按位置排序，从后往前替换避免位置偏移
	sort.Slice(matches, func(i, j int) bool {
		return matches[i].StartPos > matches[j].StartPos
	})

	return matches
}

// ReplaceMatches 根据匹配结果替换内容
func (km *keywordMatcher) ReplaceMatches(content string, matches []domain.Match) string {
	result := content

	// 从后往前替换，避免位置偏移问题
	for _, match := range matches {
		if match.StartPos >= 0 && match.EndPos <= len(result) {
			result = result[:match.StartPos] + match.Replacement + result[match.EndPos:]
		}
	}

	return result
}

// getOrCreatePattern 获取或创建正则表达式模式
func (km *keywordMatcher) getOrCreatePattern(escapedKeyword string) *regexp.Regexp {
	if pattern, exists := km.patternCache[escapedKeyword]; exists {
		return pattern
	}

	// 创建新的正则表达式模式
	pattern := regexp.MustCompile(escapedKeyword)
	km.patternCache[escapedKeyword] = pattern
	return pattern
}

// ReplaceKeywords 直接替换关键词的便捷方法
func (km *keywordMatcher) ReplaceKeywords(content string, keywords map[string]string) string {
	matches := km.FindMatches(content, keywords)
	return km.ReplaceMatches(content, matches)
}

// GetMatchStats 获取匹配统计信息
func (km *keywordMatcher) GetMatchStats(content string, keywords map[string]string) map[string]int {
	stats := make(map[string]int)

	for keyword := range keywords {
		escapedKeyword := regexp.QuoteMeta(keyword)
		pattern := km.getOrCreatePattern(escapedKeyword)
		matches := pattern.FindAllString(content, -1)
		stats[keyword] = len(matches)
	}

	return stats
}

// ValidateKeywordFormat 验证关键词格式是否正确 (#key# 格式)
func ValidateKeywordFormat(keyword string) bool {
	if len(keyword) < 3 {
		return false
	}
	return strings.HasPrefix(keyword, "#") && strings.HasSuffix(keyword, "#")
}

// ExtractKeywordName 从 #key# 格式中提取关键词名称
func ExtractKeywordName(keyword string) string {
	if !ValidateKeywordFormat(keyword) {
		return keyword
	}
	return keyword[1 : len(keyword)-1]
}

// FormatKeyword 将关键词名称格式化为 #key# 格式
func FormatKeyword(keywordName string) string {
	if ValidateKeywordFormat(keywordName) {
		return keywordName
	}
	return "#" + keywordName + "#"
}