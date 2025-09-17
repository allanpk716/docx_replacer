package main

import (
	"fmt"
	"os"
	"strings"
)

func main() {
	if len(os.Args) < 4 {
		fmt.Println("Usage: go run diff_analyzer.go <original_file> <replaced_file> <output_report>")
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
	report := analyzeDifferences(originalText, replacedText)

	// 保存报告
	err = os.WriteFile(reportFile, []byte(report), 0644)
	if err != nil {
		fmt.Printf("保存报告失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("差异分析报告已保存到: %s\n", reportFile)
}

func analyzeDifferences(original, replaced string) string {
	var report strings.Builder

	report.WriteString("=== DOCX 文档替换前后差异分析报告 ===\n\n")
	report.WriteString(fmt.Sprintf("原始文档长度: %d 字符\n", len(original)))
	report.WriteString(fmt.Sprintf("替换后文档长度: %d 字符\n", len(replaced)))
	report.WriteString(fmt.Sprintf("长度差异: %+d 字符\n\n", len(replaced)-len(original)))

	// 查找所有差异位置
	differences := findDifferences(original, replaced)

	if len(differences) == 0 {
		report.WriteString("✓ 未发现内容差异，文档内容完全一致\n")
		return report.String()
	}

	report.WriteString(fmt.Sprintf("发现 %d 处差异:\n\n", len(differences)))

	for i, diff := range differences {
		report.WriteString(fmt.Sprintf("=== 差异 %d ===\n", i+1))
		report.WriteString(fmt.Sprintf("位置: %d\n", diff.Position))
		report.WriteString(fmt.Sprintf("类型: %s\n", diff.Type))
		report.WriteString(fmt.Sprintf("原始内容: \"%s\"\n", diff.Original))
		report.WriteString(fmt.Sprintf("替换后内容: \"%s\"\n", diff.Replaced))
		report.WriteString(fmt.Sprintf("上下文: \"%s\"\n\n", diff.Context))
	}

	// 检查是否有内容丢失
	lostContent := findLostContent(original, replaced)
	if len(lostContent) > 0 {
		report.WriteString("=== 丢失的内容 ===\n")
		for _, lost := range lostContent {
			report.WriteString(fmt.Sprintf("丢失内容: \"%s\"\n", lost))
		}
		report.WriteString("\n")
	}

	// 检查新增内容
	newContent := findNewContent(original, replaced)
	if len(newContent) > 0 {
		report.WriteString("=== 新增的内容 ===\n")
		for _, new := range newContent {
			report.WriteString(fmt.Sprintf("新增内容: \"%s\"\n", new))
		}
		report.WriteString("\n")
	}

	return report.String()
}

type Difference struct {
	Position int
	Type     string
	Original string
	Replaced string
	Context  string
}

func findDifferences(original, replaced string) []Difference {
	var differences []Difference

	// 使用最长公共子序列算法找出差异
	origRunes := []rune(original)
	replRunes := []rune(replaced)

	i, j := 0, 0
	for i < len(origRunes) && j < len(replRunes) {
		if origRunes[i] == replRunes[j] {
			i++
			j++
		} else {
			// 找到差异点
			origStart := i
			replStart := j

			// 向前查找相同的部分
			for i < len(origRunes) && j < len(replRunes) && origRunes[i] != replRunes[j] {
				// 尝试在替换文本中找到原始字符
				found := false
				for k := j; k < len(replRunes) && k < j+50; k++ {
					if origRunes[i] == replRunes[k] {
						j = k
						found = true
						break
					}
				}
				if !found {
					// 尝试在原始文本中找到替换字符
					for k := i; k < len(origRunes) && k < i+50; k++ {
						if replRunes[j] == origRunes[k] {
							i = k
							found = true
							break
						}
					}
				}
				if !found {
					i++
					j++
				}
			}

			// 记录差异
			origText := string(origRunes[origStart:i])
			replText := string(replRunes[replStart:j])

			if origText != replText {
				contextStart := max(0, origStart-20)
				contextEnd := min(len(origRunes), origStart+len(origText)+20)
				context := string(origRunes[contextStart:contextEnd])

				diffType := "替换"
				if len(origText) == 0 {
					diffType = "插入"
				} else if len(replText) == 0 {
					diffType = "删除"
				}

				differences = append(differences, Difference{
					Position: origStart,
					Type:     diffType,
					Original: origText,
					Replaced: replText,
					Context:  context,
				})
			}
		}
	}

	return differences
}

func findLostContent(original, replaced string) []string {
	var lost []string

	// 查找在原始文档中存在但在替换后文档中不存在的内容
	origWords := strings.Fields(original)
	replWords := strings.Fields(replaced)

	replWordMap := make(map[string]bool)
	for _, word := range replWords {
		replWordMap[word] = true
	}

	for _, word := range origWords {
		if !replWordMap[word] && len(word) > 2 {
			lost = append(lost, word)
		}
	}

	return lost
}

func findNewContent(original, replaced string) []string {
	var new []string

	// 查找在替换后文档中存在但在原始文档中不存在的内容
	origWords := strings.Fields(original)
	replWords := strings.Fields(replaced)

	origWordMap := make(map[string]bool)
	for _, word := range origWords {
		origWordMap[word] = true
	}

	for _, word := range replWords {
		if !origWordMap[word] && len(word) > 2 {
			new = append(new, word)
		}
	}

	return new
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