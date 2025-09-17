package main

import (
	"path/filepath"
	"strings"
	"testing"

	docx "github.com/allanpk716/docx_replacer/pkg/docx"
)

// TestRealScenario 测试用户报告的真实场景
func TestRealScenario(t *testing.T) {
	// 模拟用户报告的场景：#结构及组成# 替换问题
	testXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t>结构及组成</w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
		</w:p>
		<w:p>
			<w:r>
				<w:t>文档中还有其他内容 #结构及组成# 需要替换</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`

	t.Run("第一次替换测试", func(t *testing.T) {
		// 第一次替换
		firstReplacements := map[string]string{
			"#结构及组成#": "新的结构内容",
		}

		processor1 := docx.NewEnhancedWordCompatibleProcessor(firstReplacements)

		tempDir := t.TempDir()
		inputFile := filepath.Join(tempDir, "real_test.docx")
		firstOutputFile := filepath.Join(tempDir, "first_output.docx")

		// 创建测试文件
		err := createTestDocx(inputFile, testXML)
		if err != nil {
			t.Fatalf("创建测试文件失败: %v", err)
		}

		// 第一次替换
		err = processor1.ReplaceKeywordsWithWordCompatibility(inputFile, firstOutputFile)
		if err != nil {
			t.Fatalf("第一次替换失败: %v", err)
		}

		firstResult, err := readDocxContent(firstOutputFile)
		if err != nil {
			t.Fatalf("读取第一次结果失败: %v", err)
		}

		t.Logf("第一次替换结果:\n%s", firstResult)

		// 验证第一次替换
		if !strings.Contains(firstResult, "新的结构内容") {
			t.Error("第一次替换失败：不包含新的结构内容")
		}
		if strings.Contains(firstResult, "#结构及组成#") {
			t.Error("第一次替换失败：仍然包含原关键词")
		}

		t.Run("第二次替换测试", func(t *testing.T) {
			// 第二次替换
			secondReplacements := map[string]string{
				"新的结构内容": "最终内容",
			}

			processor2 := docx.NewEnhancedWordCompatibleProcessor(secondReplacements)
			secondOutputFile := filepath.Join(tempDir, "second_output.docx")

			// 第二次替换
			err = processor2.ReplaceKeywordsWithWordCompatibility(firstOutputFile, secondOutputFile)
			if err != nil {
				t.Fatalf("第二次替换失败: %v", err)
			}

			secondResult, err := readDocxContent(secondOutputFile)
			if err != nil {
				t.Fatalf("读取第二次结果失败: %v", err)
			}

			t.Logf("第二次替换结果:\n%s", secondResult)

			// 验证第二次替换
			if strings.Contains(secondResult, "新的结构内容") {
				t.Error("第二次替换失败：仍然包含第一次的替换内容")
			}
			if !strings.Contains(secondResult, "最终内容") {
				t.Error("第二次替换失败：不包含最终内容")
			}

			// 验证没有出现用户报告的问题："adsasdadsa#结构及组成#"
			if strings.Contains(secondResult, "adsasdadsa") {
				t.Error("出现了用户报告的问题：包含异常前缀")
			}

			t.Run("第三次替换测试", func(t *testing.T) {
				// 第三次替换，确保连续替换功能完全正常
				thirdReplacements := map[string]string{
					"最终内容": "完全替换",
				}

				processor3 := docx.NewEnhancedWordCompatibleProcessor(thirdReplacements)
				thirdOutputFile := filepath.Join(tempDir, "third_output.docx")

				// 第三次替换
				err = processor3.ReplaceKeywordsWithWordCompatibility(secondOutputFile, thirdOutputFile)
				if err != nil {
					t.Fatalf("第三次替换失败: %v", err)
				}

				thirdResult, err := readDocxContent(thirdOutputFile)
				if err != nil {
					t.Fatalf("读取第三次结果失败: %v", err)
				}

				t.Logf("第三次替换结果:\n%s", thirdResult)

				// 验证第三次替换
				if strings.Contains(thirdResult, "最终内容") {
					t.Error("第三次替换失败：仍然包含第二次的替换内容")
				}
				if !strings.Contains(thirdResult, "完全替换") {
					t.Error("第三次替换失败：不包含完全替换内容")
				}
			})
		})
	})
}

// TestComplexSplitKeyword 测试复杂的分割关键词场景
func TestComplexSplitKeyword(t *testing.T) {
	// 创建更复杂的分割场景
	complexXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:rPr>
					<w:b/>
				</w:rPr>
				<w:t>结构</w:t>
			</w:r>
			<w:r>
				<w:t>及</w:t>
			</w:r>
			<w:r>
				<w:rPr>
					<w:i/>
				</w:rPr>
				<w:t>组成</w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`

	replacements := map[string]string{
		"#结构及组成#": "复杂替换内容",
	}

	processor := docx.NewEnhancedWordCompatibleProcessor(replacements)

	tempDir := t.TempDir()
	inputFile := filepath.Join(tempDir, "complex_test.docx")
	outputFile := filepath.Join(tempDir, "complex_output.docx")

	// 创建测试文件
	err := createTestDocx(inputFile, complexXML)
	if err != nil {
		t.Fatalf("创建测试文件失败: %v", err)
	}

	// 执行替换
	err = processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile)
	if err != nil {
		t.Fatalf("替换失败: %v", err)
	}

	result, err := readDocxContent(outputFile)
	if err != nil {
		t.Fatalf("读取结果失败: %v", err)
	}

	t.Logf("复杂分割关键词替换结果:\n%s", result)

	// 验证替换结果
	if !strings.Contains(result, "复杂替换内容") {
		t.Error("复杂分割关键词替换失败：不包含替换内容")
	}
	if strings.Contains(result, "#结构及组成#") {
		t.Error("复杂分割关键词替换失败：仍然包含原关键词")
	}
}