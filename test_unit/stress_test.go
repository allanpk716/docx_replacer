package main

import (
	"path/filepath"
	"strings"
	"testing"

	docx "github.com/allanpk716/docx_replacer/pkg/docx"
)

// TestStressScenario 压力测试：多个关键词的连续替换
func TestStressScenario(t *testing.T) {
	// 创建包含多个关键词的复杂文档
	complexXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t>产品</w:t>
			</w:r>
			<w:r>
				<w:t>名称</w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t> 和 </w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t>版本</w:t>
			</w:r>
			<w:r>
				<w:t>号</w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
		</w:p>
		<w:p>
			<w:r>
				<w:t>描述：#产品名称# 的版本是 #版本号#</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`

	tempDir := t.TempDir()
	inputFile := filepath.Join(tempDir, "stress_test.docx")

	// 创建测试文件
	err := createTestDocx(inputFile, complexXML)
	if err != nil {
		t.Fatalf("创建测试文件失败: %v", err)
	}

	// 第一轮替换
	firstReplacements := map[string]string{
		"#产品名称#": "超级软件",
		"#版本号#":   "v2.0",
	}

	processor1 := docx.NewEnhancedWordCompatibleProcessor(firstReplacements)
	firstOutput := filepath.Join(tempDir, "first_output.docx")

	err = processor1.ReplaceKeywordsWithWordCompatibility(inputFile, firstOutput)
	if err != nil {
		t.Fatalf("第一轮替换失败: %v", err)
	}

	firstResult, err := readDocxContent(firstOutput)
	if err != nil {
		t.Fatalf("读取第一轮结果失败: %v", err)
	}

	t.Logf("第一轮替换结果:\n%s", firstResult)

	// 验证第一轮替换
	if !strings.Contains(firstResult, "超级软件") {
		t.Error("第一轮替换失败：不包含超级软件")
	}
	if !strings.Contains(firstResult, "v2.0") {
		t.Error("第一轮替换失败：不包含v2.0")
	}

	// 第二轮替换
	secondReplacements := map[string]string{
		"超级软件": "终极应用",
		"v2.0":  "版本3.0",
	}

	processor2 := docx.NewEnhancedWordCompatibleProcessor(secondReplacements)
	secondOutput := filepath.Join(tempDir, "second_output.docx")

	err = processor2.ReplaceKeywordsWithWordCompatibility(firstOutput, secondOutput)
	if err != nil {
		t.Fatalf("第二轮替换失败: %v", err)
	}

	secondResult, err := readDocxContent(secondOutput)
	if err != nil {
		t.Fatalf("读取第二轮结果失败: %v", err)
	}

	t.Logf("第二轮替换结果:\n%s", secondResult)

	// 验证第二轮替换
	if strings.Contains(secondResult, "超级软件") {
		t.Error("第二轮替换失败：仍然包含超级软件")
	}
	if strings.Contains(secondResult, "v2.0") {
		t.Error("第二轮替换失败：仍然包含v2.0")
	}
	if !strings.Contains(secondResult, "终极应用") {
		t.Error("第二轮替换失败：不包含终极应用")
	}
	if !strings.Contains(secondResult, "版本3.0") {
		t.Error("第二轮替换失败：不包含版本3.0")
	}

	// 第三轮替换
	thirdReplacements := map[string]string{
		"终极应用": "最终产品",
		"版本3.0": "最新版",
	}

	processor3 := docx.NewEnhancedWordCompatibleProcessor(thirdReplacements)
	thirdOutput := filepath.Join(tempDir, "third_output.docx")

	err = processor3.ReplaceKeywordsWithWordCompatibility(secondOutput, thirdOutput)
	if err != nil {
		t.Fatalf("第三轮替换失败: %v", err)
	}

	thirdResult, err := readDocxContent(thirdOutput)
	if err != nil {
		t.Fatalf("读取第三轮结果失败: %v", err)
	}

	t.Logf("第三轮替换结果:\n%s", thirdResult)

	// 验证第三轮替换
	if strings.Contains(thirdResult, "终极应用") {
		t.Error("第三轮替换失败：仍然包含终极应用")
	}
	if strings.Contains(thirdResult, "版本3.0") {
		t.Error("第三轮替换失败：仍然包含版本3.0")
	}
	if !strings.Contains(thirdResult, "最终产品") {
		t.Error("第三轮替换失败：不包含最终产品")
	}
	if !strings.Contains(thirdResult, "最新版") {
		t.Error("第三轮替换失败：不包含最新版")
	}
}

// TestMixedContentReplacement 测试混合内容的替换
func TestMixedContentReplacement(t *testing.T) {
	// 创建包含中英文混合、特殊字符的文档
	mixedXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t>Company</w:t>
			</w:r>
			<w:r>
				<w:t>Name</w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t> (公司) &amp; </w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
			<w:r>
				<w:t>数字</w:t>
			</w:r>
			<w:r>
				<w:t>123</w:t>
			</w:r>
			<w:r>
				<w:t>#</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>`

	tempDir := t.TempDir()
	inputFile := filepath.Join(tempDir, "mixed_test.docx")

	// 创建测试文件
	err := createTestDocx(inputFile, mixedXML)
	if err != nil {
		t.Fatalf("创建测试文件失败: %v", err)
	}

	// 第一轮替换
	firstReplacements := map[string]string{
		"#CompanyName#": "TechCorp技术公司",
		"#数字123#":      "编号456",
	}

	processor1 := docx.NewEnhancedWordCompatibleProcessor(firstReplacements)
	firstOutput := filepath.Join(tempDir, "mixed_first_output.docx")

	err = processor1.ReplaceKeywordsWithWordCompatibility(inputFile, firstOutput)
	if err != nil {
		t.Fatalf("第一轮替换失败: %v", err)
	}

	firstResult, err := readDocxContent(firstOutput)
	if err != nil {
		t.Fatalf("读取第一轮结果失败: %v", err)
	}

	t.Logf("混合内容第一轮替换结果:\n%s", firstResult)

	// 第二轮替换
	secondReplacements := map[string]string{
		"TechCorp技术公司": "SuperTech超级科技",
		"编号456":         "ID789",
	}

	processor2 := docx.NewEnhancedWordCompatibleProcessor(secondReplacements)
	secondOutput := filepath.Join(tempDir, "mixed_second_output.docx")

	err = processor2.ReplaceKeywordsWithWordCompatibility(firstOutput, secondOutput)
	if err != nil {
		t.Fatalf("第二轮替换失败: %v", err)
	}

	secondResult, err := readDocxContent(secondOutput)
	if err != nil {
		t.Fatalf("读取第二轮结果失败: %v", err)
	}

	t.Logf("混合内容第二轮替换结果:\n%s", secondResult)

	// 验证替换结果
	if !strings.Contains(secondResult, "SuperTech超级科技") {
		t.Error("混合内容替换失败：不包含SuperTech超级科技")
	}
	if !strings.Contains(secondResult, "ID789") {
		t.Error("混合内容替换失败：不包含ID789")
	}
	if strings.Contains(secondResult, "TechCorp技术公司") {
		t.Error("混合内容替换失败：仍然包含TechCorp技术公司")
	}
	if strings.Contains(secondResult, "编号456") {
		t.Error("混合内容替换失败：仍然包含编号456")
	}
}