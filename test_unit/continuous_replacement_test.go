package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"testing"

	docx "github.com/allanpk716/docx_replacer/pkg/docx"
)

// createTestDocx 创建一个包含指定XML内容的测试docx文件
func createTestDocx(filename, documentXML string) error {
	file, err := os.Create(filename)
	if err != nil {
		return err
	}
	defer file.Close()

	w := zip.NewWriter(file)
	defer w.Close()

	// 创建必要的文件结构
	files := map[string]string{
		"[Content_Types].xml": `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
	<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
	<Default Extension="xml" ContentType="application/xml"/>
	<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>`,
		"_rels/.rels": `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
	<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>`,
		"word/_rels/document.xml.rels": `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
</Relationships>`,
		"word/document.xml": documentXML,
	}

	for path, content := range files {
		f, err := w.Create(path)
		if err != nil {
			return err
		}
		_, err = f.Write([]byte(content))
		if err != nil {
			return err
		}
	}

	return nil
}

// readDocxContent 读取docx文件中的document.xml内容
func readDocxContent(filename string) (string, error) {
	r, err := zip.OpenReader(filename)
	if err != nil {
		return "", err
	}
	defer r.Close()

	for _, f := range r.File {
		if f.Name == "word/document.xml" {
			rc, err := f.Open()
			if err != nil {
				return "", err
			}
			defer rc.Close()

			content, err := io.ReadAll(rc)
			if err != nil {
				return "", err
			}
			return string(content), nil
		}
	}
	return "", fmt.Errorf("document.xml not found")
}

// copyFile 复制文件
func copyFile(srcPath, dstPath string) error {
	src, err := os.Open(srcPath)
	if err != nil {
		return err
	}
	defer src.Close()

	dst, err := os.Create(dstPath)
	if err != nil {
		return err
	}
	defer dst.Close()

	_, err = io.Copy(dst, src)
	return err
}

// TestContinuousReplacement 测试连续替换功能
func TestContinuousReplacement(t *testing.T) {
	// 创建测试用的XML内容，模拟Word文档结构
	testXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#结构及组成#</w:t>
			</w:r>
		</w:p>
		<w:p>
			<w:r>
				<w:t>这是一个测试文档，包含关键词 </w:t>
			</w:r>
			<w:r>
				<w:t>#结构及组成#</w:t>
			</w:r>
			<w:r>
				<w:t> 需要被替换。</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`

	// 第一次替换的映射
	firstReplacements := map[string]string{
		"#结构及组成#": "新的结构内容",
	}

	// 第二次替换的映射
	secondReplacements := map[string]string{
		"新的结构内容": "最终内容",
	}

	t.Run("第一次替换测试", func(t *testing.T) {
		processor := docx.NewEnhancedWordCompatibleProcessor(firstReplacements)
		
		// 创建临时文件进行测试
		tempDir := t.TempDir()
		inputFile := filepath.Join(tempDir, "input.docx")
		outputFile := filepath.Join(tempDir, "output.docx")
		
		// 创建一个简单的docx文件用于测试
		err := createTestDocx(inputFile, testXML)
		if err != nil {
			t.Fatalf("创建测试文件失败: %v", err)
		}
		
		err = processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile)
		if err != nil {
			t.Fatalf("第一次替换失败: %v", err)
		}
		
		// 读取替换结果
		firstResult, err := readDocxContent(outputFile)
		if err != nil {
			t.Fatalf("读取第一次替换结果失败: %v", err)
		}
		
		t.Logf("第一次替换结果:\n%s", firstResult)
		
		// 验证第一次替换是否成功
		if strings.Contains(firstResult, "#结构及组成#") {
			t.Error("第一次替换失败：仍然包含原关键词")
		}
		if !strings.Contains(firstResult, "新的结构内容") {
			t.Error("第一次替换失败：不包含新内容")
		}
		
		t.Run("第二次替换测试", func(t *testing.T) {
			processor2 := docx.NewEnhancedWordCompatibleProcessor(secondReplacements)
			
			// 使用第一次替换的结果作为第二次的输入
			secondInputFile := filepath.Join(tempDir, "second_input.docx")
			secondOutputFile := filepath.Join(tempDir, "second_output.docx")
			
			// 复制第一次的输出作为第二次的输入
			err := copyFile(outputFile, secondInputFile)
			if err != nil {
				t.Fatalf("复制文件失败: %v", err)
			}
			
			err = processor2.ReplaceKeywordsWithWordCompatibility(secondInputFile, secondOutputFile)
			if err != nil {
				t.Fatalf("第二次替换失败: %v", err)
			}
			
			// 读取第二次替换结果
			secondResult, err := readDocxContent(secondOutputFile)
			if err != nil {
				t.Fatalf("读取第二次替换结果失败: %v", err)
			}
			t.Logf("第二次替换结果:\n%s", secondResult)
			
			// 验证第二次替换是否成功
			if strings.Contains(secondResult, "新的结构内容") {
				t.Error("第二次替换失败：仍然包含第一次的替换内容")
			}
			if !strings.Contains(secondResult, "最终内容") {
				t.Error("第二次替换失败：不包含最终内容")
			}
		})
	})
}

// TestXMLStructureAfterReplacement 测试替换后XML结构的变化
func TestXMLStructureAfterReplacement(t *testing.T) {
	// 创建包含分割关键词的复杂XML
	splitKeywordXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
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
	</w:body>
</w:document>`

	replacements := map[string]string{
		"#结构及组成#": "替换内容",
	}

	processor := docx.NewEnhancedWordCompatibleProcessor(replacements)
	
	t.Run("分割关键词重组测试", func(t *testing.T) {
		tempDir := t.TempDir()
		inputFile := filepath.Join(tempDir, "split_test.docx")
		outputFile := filepath.Join(tempDir, "split_output.docx")
		
		err := createTestDocx(inputFile, splitKeywordXML)
		if err != nil {
			t.Fatalf("创建测试文件失败: %v", err)
		}
		
		err = processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile)
		if err != nil {
			t.Fatalf("替换失败: %v", err)
		}
		
		result, err := readDocxContent(outputFile)
		if err != nil {
			t.Fatalf("读取结果失败: %v", err)
		}
		t.Logf("分割关键词重组结果:\n%s", result)
		
		// 验证是否正确重组并替换
		if !strings.Contains(result, "替换内容") {
			t.Error("分割关键词重组失败：不包含替换内容")
		}
		
		// 验证XML结构完整性
		if !strings.Contains(result, "<w:document") || !strings.Contains(result, "</w:document>") {
			t.Error("XML结构被破坏")
		}
	})
}

// TestKeywordRecognitionAfterReplacement 测试替换后关键词识别能力
func TestKeywordRecognitionAfterReplacement(t *testing.T) {
	// 创建测试XML
	testXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#测试关键词#</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`

	// 第一次替换
	firstReplacements := map[string]string{
		"#测试关键词#": "#新关键词#",
	}

	// 第二次替换
	secondReplacements := map[string]string{
		"#新关键词#": "最终结果",
	}

	processor1 := docx.NewEnhancedWordCompatibleProcessor(firstReplacements)
	processor2 := docx.NewEnhancedWordCompatibleProcessor(secondReplacements)

	tempDir := t.TempDir()
	inputFile := filepath.Join(tempDir, "keyword_test.docx")
	firstOutputFile := filepath.Join(tempDir, "first_output.docx")
	secondOutputFile := filepath.Join(tempDir, "second_output.docx")

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
	t.Logf("第一次替换后的XML结构:\n%s", firstResult)

	// 分析第一次替换后的XML结构
	t.Run("分析第一次替换后的结构", func(t *testing.T) {
		// 检查是否包含新关键词
		if !strings.Contains(firstResult, "#新关键词#") {
			t.Error("第一次替换后不包含新关键词")
		}
		
		// 检查XML标签结构
		if !strings.Contains(firstResult, "<w:t>") {
			t.Error("第一次替换后缺少w:t标签")
		}
	})

	// 第二次替换
	err = processor2.ReplaceKeywordsWithWordCompatibility(firstOutputFile, secondOutputFile)
	if err != nil {
		t.Fatalf("第二次替换失败: %v", err)
	}

	secondResult, err := readDocxContent(secondOutputFile)
	if err != nil {
		t.Fatalf("读取第二次结果失败: %v", err)
	}
	t.Logf("第二次替换后的XML结构:\n%s", secondResult)

	// 验证第二次替换结果
	t.Run("验证第二次替换结果", func(t *testing.T) {
		if strings.Contains(secondResult, "#新关键词#") {
			t.Error("第二次替换失败：仍然包含中间关键词")
		}
		if !strings.Contains(secondResult, "最终结果") {
			t.Error("第二次替换失败：不包含最终结果")
		}
	})
}

// TestEdgeCases 测试边界情况
func TestEdgeCases(t *testing.T) {
	t.Run("空关键词测试", func(t *testing.T) {
		replacements := map[string]string{
			"": "不应该替换",
		}
		processor := docx.NewEnhancedWordCompatibleProcessor(replacements)
		
		// 创建测试文件
		tempDir := t.TempDir()
		inputFile := filepath.Join(tempDir, "empty_test.docx")
		outputFile := filepath.Join(tempDir, "empty_output.docx")
		
		testXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>测试内容</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`
		
		err := createTestDocx(inputFile, testXML)
		if err != nil {
			t.Fatalf("创建测试文件失败: %v", err)
		}
		
		err = processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile)
		if err != nil {
			t.Fatalf("处理失败: %v", err)
		}
		
		result, err := readDocxContent(outputFile)
		if err != nil {
			t.Fatalf("读取结果失败: %v", err)
		}
		
		if strings.Contains(result, "不应该替换") {
			t.Error("空关键词不应该被替换")
		}
	})

	t.Run("特殊字符测试", func(t *testing.T) {
		replacements := map[string]string{
			"#特殊&字符<>#": "已替换",
		}
		processor := docx.NewEnhancedWordCompatibleProcessor(replacements)
		
		// 创建测试文件
		tempDir := t.TempDir()
		inputFile := filepath.Join(tempDir, "special_test.docx")
		outputFile := filepath.Join(tempDir, "special_output.docx")
		
		testXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#特殊&amp;字符&lt;&gt;#</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`
		
		err := createTestDocx(inputFile, testXML)
		if err != nil {
			t.Fatalf("创建测试文件失败: %v", err)
		}
		
		err = processor.ReplaceKeywordsWithWordCompatibility(inputFile, outputFile)
		if err != nil {
			t.Fatalf("处理失败: %v", err)
		}
		
		result, err := readDocxContent(outputFile)
		if err != nil {
			t.Fatalf("读取结果失败: %v", err)
		}
		
		t.Logf("特殊字符处理结果: %s", result)
	})
}