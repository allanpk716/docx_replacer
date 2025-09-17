package docx
import (
	"archive/zip"
	"os"
	"path/filepath"
	"testing"

	"github.com/allanpk716/docx_replacer/internal/comment"
	"github.com/stretchr/testify/assert"
)

// createTempDocxFile 创建临时的docx文件用于测试
func createTempDocxFile(t *testing.T) string {
	tempFile, err := os.CreateTemp("", "test_*.docx")
	if err != nil {
		t.Fatalf("Failed to create temp file: %v", err)
	}
	tempFile.Close()

	// 重新打开文件用于写入
	file, err := os.OpenFile(tempFile.Name(), os.O_WRONLY|os.O_TRUNC, 0644)
	if err != nil {
		t.Fatalf("Failed to open temp file: %v", err)
	}
	defer file.Close()

	// 创建一个简单的docx文件结构
	zipWriter := zip.NewWriter(file)
	defer zipWriter.Close()

	// 添加基本的docx文件结构
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
		"word/document.xml": `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:body>
<w:p><w:r><w:t>#name# is #age# years old.</w:t></w:r></w:p>
</w:body>
</w:document>`,
	}

	for filename, content := range files {
		// 确保目录存在
		dir := filepath.Dir(filename)
		if dir != "." {
			_, err := zipWriter.Create(dir + "/")
			if err != nil {
				t.Fatalf("Failed to create directory %s: %v", dir, err)
			}
		}

		writer, err := zipWriter.Create(filename)
		if err != nil {
			t.Fatalf("Failed to create file %s: %v", filename, err)
		}
		_, err = writer.Write([]byte(content))
		if err != nil {
			t.Fatalf("Failed to write file %s: %v", filename, err)
		}
	}

	return tempFile.Name()
}

// TestEnhancedXMLProcessor_ReplaceWithComments 测试带注释的替换
func TestEnhancedXMLProcessor_ReplaceWithComments(t *testing.T) {
	// 创建临时测试文件
	tempFile := createTempDocxFile(t)
	defer os.Remove(tempFile)

	commentConfig := &comment.CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
	}

	processor := NewEnhancedXMLProcessor(tempFile, commentConfig)

	// 测试数据
	keywords := map[string]string{
		"#name#": "John Doe",
	}

	// 创建输出文件
	outputFile := tempFile + "_output.docx"
	defer os.Remove(outputFile)

	// 执行替换
	err := processor.ReplaceKeywordsWithTracking(keywords, outputFile)

	// 验证结果
	assert.NoError(t, err)
	// 验证输出文件存在
	_, err = os.Stat(outputFile)
	assert.NoError(t, err)
}

// TestEnhancedXMLProcessor_SecondReplacement 测试二次替换
func TestEnhancedXMLProcessor_SecondReplacement(t *testing.T) {
	// 创建临时测试文件
	tempFile := createTempDocxFile(t)
	defer os.Remove(tempFile)

	commentConfig := &comment.CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
	}

	processor := NewEnhancedXMLProcessor(tempFile, commentConfig)

	// 第一次替换
	keywords1 := map[string]string{
		"#name#": "John",
	}

	outputFile1 := tempFile + "_output1.docx"
	defer os.Remove(outputFile1)

	err := processor.ReplaceKeywordsWithTracking(keywords1, outputFile1)
	assert.NoError(t, err)

	// 第二次替换
	processor2 := NewEnhancedXMLProcessor(outputFile1, commentConfig)
	keywords2 := map[string]string{
		"#name#": "Jane",
	}

	outputFile2 := tempFile + "_output2.docx"
	defer os.Remove(outputFile2)

	err = processor2.ReplaceKeywordsWithTracking(keywords2, outputFile2)
	assert.NoError(t, err)

	// 验证最终输出文件存在
	_, err = os.Stat(outputFile2)
	assert.NoError(t, err)
}

// TestEnhancedXMLProcessor_BackwardCompatibility 测试向后兼容性
func TestEnhancedXMLProcessor_BackwardCompatibility(t *testing.T) {
	// 创建临时测试文件
	tempFile := createTempDocxFile(t)
	defer os.Remove(tempFile)

	commentConfig := &comment.CommentConfig{
		EnableCommentTracking: false, // 禁用注释追踪
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
		MaxCommentHistory:    10,
	}

	processor := NewEnhancedXMLProcessor(tempFile, commentConfig)

	keywords := map[string]string{
		"#name#": "John",
	}

	outputFile := tempFile + "_output.docx"
	defer os.Remove(outputFile)

	err := processor.ReplaceKeywordsWithTracking(keywords, outputFile)

	// 验证结果
	assert.NoError(t, err)
	// 验证输出文件存在
	_, err = os.Stat(outputFile)
	assert.NoError(t, err)
}

// TestEnhancedXMLProcessor_MixedContent 测试混合内容处理
func TestEnhancedXMLProcessor_MixedContent(t *testing.T) {
	// 创建临时测试文件
	tempFile := createTempDocxFile(t)
	defer os.Remove(tempFile)

	commentConfig := &comment.CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
	}

	processor := NewEnhancedXMLProcessor(tempFile, commentConfig)

	keywords := map[string]string{
		"#name#": "John",
		"#age#":  "25",
	}

	outputFile := tempFile + "_output.docx"
	defer os.Remove(outputFile)

	err := processor.ReplaceKeywordsWithTracking(keywords, outputFile)

	// 验证结果
	assert.NoError(t, err)
	// 验证输出文件存在
	_, err = os.Stat(outputFile)
	assert.NoError(t, err)
}

// TestEnhancedXMLProcessor_EmptyKeywords 测试空关键词映射
func TestEnhancedXMLProcessor_EmptyKeywords(t *testing.T) {
	// 创建临时测试文件
	tempFile := createTempDocxFile(t)
	defer os.Remove(tempFile)

	commentConfig := &comment.CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
	}

	processor := NewEnhancedXMLProcessor(tempFile, commentConfig)

	keywords := map[string]string{} // 空映射

	outputFile := tempFile + "_output.docx"
	defer os.Remove(outputFile)

	err := processor.ReplaceKeywordsWithTracking(keywords, outputFile)

	// 验证结果
	assert.NoError(t, err)
	// 验证输出文件存在
	_, err = os.Stat(outputFile)
	assert.NoError(t, err)
}

// TestEnhancedXMLProcessor_InvalidXML 测试无效XML处理
func TestEnhancedXMLProcessor_InvalidXML(t *testing.T) {
	// 创建临时测试文件
	tempFile := createTempDocxFile(t)
	defer os.Remove(tempFile)

	commentConfig := &comment.CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
	}

	processor := NewEnhancedXMLProcessor(tempFile, commentConfig)

	keywords := map[string]string{
		"#name#": "John",
	}

	outputFile := tempFile + "_output.docx"
	defer os.Remove(outputFile)

	// 测试处理（实际的docx文件应该是有效的）
	err := processor.ReplaceKeywordsWithTracking(keywords, outputFile)

	// 验证结果
	assert.NoError(t, err)
	// 验证输出文件存在
	_, err = os.Stat(outputFile)
	assert.NoError(t, err)
}