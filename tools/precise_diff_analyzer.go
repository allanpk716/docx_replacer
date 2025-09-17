package main

import (
	"fmt"
	"os"
	"regexp"
	"strings"
)

func main() {
	if len(os.Args) < 4 {
		fmt.Println("Usage: go run precise_diff_analyzer.go <original_file> <replaced_file> <output_report>")
		os.Exit(1)
	}

	originalFile := os.Args[1]
	replacedFile := os.Args[2]
	reportFile := os.Args[3]

	// 读取原始文件
	originalContent, err := os.ReadFile(originalFile)
	if err != nil {
		fmt.Printf("读取原始文件失败: %v\n", err)
		os.Exit(1)
	}

	// 读取替换后文件
	replacedContent, err := os.ReadFile(replacedFile)
	if err != nil {
		fmt.Printf("读取替换后文件失败: %v\n", err)
		os.Exit(1)
	}

	originalText := string(originalContent)
	replacedText := string(replacedContent)

	// 分析差异
	report := analyzePreciseDifferences(originalText, replacedText)

	// 保存报告
	err = os.WriteFile(reportFile, []byte(report), 0644)
	if err != nil {
		fmt.Printf("保存报告失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("精确差异分析报告已保存到: %s\n", reportFile)
}

func analyzePreciseDifferences(original, replaced string) string {
	var report strings.Builder

	report.WriteString("=== DOCX 文档精确差异分析报告 ===\n\n")
	report.WriteString(fmt.Sprintf("原始文档长度: %d 字符\n", len(original)))
	report.WriteString(fmt.Sprintf("替换后文档长度: %d 字符\n", len(replaced)))
	report.WriteString(fmt.Sprintf("长度差异: %+d 字符\n\n", len(replaced)-len(original)))

	// 查找关键词替换
	keywordReplacements := findKeywordReplacements(original, replaced)

	if len(keywordReplacements) == 0 {
		report.WriteString("❌ 未发现任何关键词替换，这可能表明替换功能存在问题\n")
	} else {
		report.WriteString(fmt.Sprintf("✓ 发现 %d 个关键词替换:\n\n", len(keywordReplacements)))
		for i, replacement := range keywordReplacements {
			report.WriteString(fmt.Sprintf("%d. '%s' -> '%s'\n", i+1, replacement.Original, replacement.Replaced))
			report.WriteString(fmt.Sprintf("   位置: %d\n", replacement.Position))
			report.WriteString(fmt.Sprintf("   上下文: ...%s...\n\n", replacement.Context))
		}
	}

	// 检查是否有内容完全丢失
	lostSections := findLostSections(original, replaced)
	if len(lostSections) > 0 {
		report.WriteString("❌ 发现丢失的内容段落:\n")
		for i, section := range lostSections {
			report.WriteString(fmt.Sprintf("%d. 丢失内容 (长度: %d 字符):\n", i+1, len(section)))
			if len(section) > 100 {
				report.WriteString(fmt.Sprintf("   \"%s...%s\"\n\n", section[:50], section[len(section)-50:]))
			} else {
				report.WriteString(fmt.Sprintf("   \"%s\"\n\n", section))
			}
		}
	} else {
		report.WriteString("✓ 未发现内容丢失\n\n")
	}

	// 检查文档结构完整性
	structureCheck := checkDocumentStructure(original, replaced)
	report.WriteString(structureCheck)

	// 总结
	report.WriteString("=== 总结 ===\n")
	if len(lostSections) == 0 && len(keywordReplacements) > 0 {
		report.WriteString("✅ 文档替换功能正常工作\n")
		report.WriteString("✅ 所有原始内容都得到保留\n")
		report.WriteString("✅ 关键词替换成功执行\n")
	} else if len(lostSections) > 0 {
		report.WriteString("❌ 发现内容丢失问题\n")
		report.WriteString("❌ 需要检查替换算法\n")
	} else {
		report.WriteString("⚠️  未发现关键词替换，可能配置有问题\n")
	}

	return report.String()
}

type KeywordReplacement struct {
	Original string
	Replaced string
	Position int
	Context  string
}

func findKeywordReplacements(original, replaced string) []KeywordReplacement {
	var replacements []KeywordReplacement

	// 查找原始文档中的关键词模式 #xxx#
	keywordPattern := regexp.MustCompile(`#[^#]+#`)
	originalKeywords := keywordPattern.FindAllString(original, -1)

	for _, keyword := range originalKeywords {
		// 在原始文档中找到关键词的位置
		origPos := strings.Index(original, keyword)
		if origPos == -1 {
			continue
		}

		// 检查在替换后的文档中相同位置是否还是这个关键词
		if origPos < len(replaced) {
			// 在替换后文档中查找这个位置附近的内容
			// 由于替换可能改变长度，我们需要在一个范围内搜索
			searchStart := max(0, origPos-50)
			searchEnd := min(len(replaced), origPos+len(keyword)+100)
			searchArea := replaced[searchStart:searchEnd]

			// 如果在搜索区域内没有找到原关键词，说明被替换了
			if !strings.Contains(searchArea, keyword) {
				// 尝试找到替换后的内容
				// 通过上下文来定位
				contextBefore := ""
				contextAfter := ""
				if origPos > 20 {
					contextBefore = original[origPos-20 : origPos]
				}
				if origPos+len(keyword)+20 < len(original) {
					contextAfter = original[origPos+len(keyword) : origPos+len(keyword)+20]
				}

				// 在替换后文档中找到相同的上下文
				beforePos := -1
				afterPos := -1
				if contextBefore != "" {
					beforePos = strings.Index(replaced, contextBefore)
				}
				if contextAfter != "" {
					afterPos = strings.Index(replaced, contextAfter)
				}

				replacedContent := "[无法确定]"
				if beforePos != -1 && afterPos != -1 {
					startPos := beforePos + len(contextBefore)
					if startPos < afterPos {
						replacedContent = replaced[startPos:afterPos]
					}
				}

				context := fmt.Sprintf("%s[%s]%s", contextBefore, keyword, contextAfter)

				replacements = append(replacements, KeywordReplacement{
					Original: keyword,
					Replaced: replacedContent,
					Position: origPos,
					Context:  context,
				})
			}
		}
	}

	return replacements
}

func findLostSections(original, replaced string) []string {
	var lostSections []string

	// 将文档分成较大的段落来检查
	originalParagraphs := strings.Split(original, "\n")
	replacedText := replaced

	for _, paragraph := range originalParagraphs {
		paragraph = strings.TrimSpace(paragraph)
		if len(paragraph) > 10 { // 只检查有意义的段落
			// 检查这个段落是否在替换后的文档中存在
			if !strings.Contains(replacedText, paragraph) {
				// 检查是否是关键词替换导致的差异
				hasKeyword := strings.Contains(paragraph, "#")
				if !hasKeyword {
					// 如果段落不包含关键词但仍然丢失，这是真正的丢失
					lostSections = append(lostSections, paragraph)
				}
			}
		}
	}

	return lostSections
}

func checkDocumentStructure(original, replaced string) string {
	var result strings.Builder

	result.WriteString("=== 文档结构完整性检查 ===\n")

	// 检查重要的结构性文本
	structuralElements := []string{
		"广西壮族自治区药品监督管理局",
		"医疗器械注册证核发申请表",
		"桂林优利特医疗电子有限公司",
		"填表说明",
		"保证书",
		"注册申请人",
		"法定代表人",
	}

	for _, element := range structuralElements {
		origCount := strings.Count(original, element)
		replCount := strings.Count(replaced, element)
		if origCount == replCount {
			result.WriteString(fmt.Sprintf("✓ '%s': %d 次 (一致)\n", element, origCount))
		} else {
			result.WriteString(fmt.Sprintf("❌ '%s': 原始 %d 次 -> 替换后 %d 次\n", element, origCount, replCount))
		}
	}

	result.WriteString("\n")
	return result.String()
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}