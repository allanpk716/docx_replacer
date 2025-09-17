package docx

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// TestCustomPropertyTracking 测试自定义属性追踪功能
func TestCustomPropertyTracking(t *testing.T) {
	// 创建临时测试文件
	testDir := t.TempDir()
	inputFile := filepath.Join(testDir, "test_input.docx")
	output1File := filepath.Join(testDir, "test_output1.docx")
	output2File := filepath.Join(testDir, "test_output2.docx")

	// 复制测试文件
	srcFile := "../../input/1.2.申请表.docx"
	err := copyFile(srcFile, inputFile)
	if err != nil {
		t.Skipf("跳过测试，无法找到测试文件: %v", err)
		return
	}

	// 第一次替换
	replacements1 := map[string]string{
		"#产品名称#": "D-二聚体测定试剂盒（胶乳免疫比浊法）",
		"#结构及组成#": "测试组成内容",
	}

	// 创建处理器并执行第一次替换（使用自定义属性追踪）
	processor1 := NewEnhancedXMLProcessorWithCustomProps(inputFile)

	err = processor1.ReplaceKeywordsWithTracking(replacements1, output1File)
	if err != nil {
		t.Fatalf("第一次替换失败: %v", err)
	}

	// 验证第一次替换的输出文件存在
	if _, err := os.Stat(output1File); os.IsNotExist(err) {
		t.Fatalf("第一次替换后输出文件不存在: %s", output1File)
	}

	// 验证第一次替换后的自定义属性
	props1, err := extractCustomProperties(output1File)
	if err != nil {
		t.Fatalf("提取第一次替换后的自定义属性失败: %v", err)
	}

	// 调试：打印所有自定义属性
	t.Logf("第一次替换后的自定义属性:")
	for name, value := range props1 {
		t.Logf("  %s = %s", name, value)
	}

	// 检查是否记录了替换历史
	if !containsProperty(props1, "DocxReplacer_#产品名称#", "D-二聚体测定试剂盒（胶乳免疫比浊法）") {
		t.Errorf("第一次替换后未找到产品名称的追踪记录")
	}
	if !containsProperty(props1, "DocxReplacer_#结构及组成#", "测试组成内容") {
		t.Errorf("第一次替换后未找到结构及组成的追踪记录")
	}

	// 第二次替换（使用不同的值）
	replacements2 := map[string]string{
		"#产品名称#": "D-二聚体测定试剂盒（胶乳免疫比浊法）修改版",
		"#结构及组成#": "测试组成内容修改版",
	}

	// 创建处理器并执行第二次替换（使用自定义属性追踪）
	processor2 := NewEnhancedXMLProcessorWithCustomProps(output1File)
	err = processor2.ReplaceKeywordsWithTracking(replacements2, output2File)
	if err != nil {
		t.Fatalf("第二次替换失败: %v", err)
	}

	// 验证第二次替换的输出文件存在
	if _, err := os.Stat(output2File); os.IsNotExist(err) {
		t.Fatalf("第二次替换后输出文件不存在: %s", output2File)
	}

	// 验证第二次替换后的自定义属性（应该保持第一次的值，因为关键词已被替换）
	props2, err := extractCustomProperties(output2File)
	if err != nil {
		t.Fatalf("提取第二次替换后的自定义属性失败: %v", err)
	}

	// 第二次替换应该不会改变自定义属性，因为关键词已经不存在了
	if !containsProperty(props2, "DocxReplacer_#产品名称#", "D-二聚体测定试剂盒（胶乳免疫比浊法）") {
		t.Errorf("第二次替换后产品名称的追踪记录发生了意外变化")
	}
	if !containsProperty(props2, "DocxReplacer_#结构及组成#", "测试组成内容") {
		t.Errorf("第二次替换后结构及组成的追踪记录发生了意外变化")
	}

	t.Logf("自定义属性追踪功能测试通过")
}

// TestCustomPropertyManager 测试自定义属性管理器
func TestCustomPropertyManager(t *testing.T) {
	manager := NewCustomPropertyManager()

	// 测试解析空的自定义属性
	emptyProps, err := manager.ParseCustomProperties("")
	if err != nil {
		t.Fatalf("解析空自定义属性失败: %v", err)
	}

	// 测试添加替换记录
	manager.AddReplacement(emptyProps, "#测试关键词#", "", "测试值")

	// 测试检查是否已替换
	if !manager.HasReplaced(emptyProps, "#测试关键词#", "测试值") {
		t.Errorf("HasReplaced应该返回true")
	}

	if manager.HasReplaced(emptyProps, "#测试关键词#", "不同的值") {
		t.Errorf("HasReplaced对不同值应该返回false")
	}

	// 测试生成XML
	xmlContent, err := manager.GenerateCustomPropertiesXML(emptyProps)
	if err != nil {
		t.Fatalf("生成自定义属性XML失败: %v", err)
	}

	if !strings.Contains(xmlContent, "DocxReplacer_#测试关键词#") {
		t.Errorf("生成的XML应该包含追踪属性名")
	}

	if !strings.Contains(xmlContent, "测试值") {
		t.Errorf("生成的XML应该包含替换值")
	}

	t.Logf("自定义属性管理器测试通过")
}

// 辅助函数：复制文件
func copyFile(src, dst string) error {
	srcFile, err := os.Open(src)
	if err != nil {
		return err
	}
	defer srcFile.Close()

	dstFile, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer dstFile.Close()

	_, err = srcFile.WriteTo(dstFile)
	return err
}

// 辅助函数：提取自定义属性
func extractCustomProperties(filePath string) (map[string]string, error) {
	processor := NewEnhancedXMLProcessorWithCustomProps(filePath)

	// 读取自定义属性
	customPropsXML, err := processor.readCustomProperties()
	if err != nil {
		return nil, err
	}

	// 解析自定义属性
	manager := NewCustomPropertyManager()
	props, err := manager.ParseCustomProperties(customPropsXML)
	if err != nil {
		return nil, err
	}

	// 转换为映射
	result := make(map[string]string)
	for _, prop := range props.Properties {
		result[prop.Name] = prop.Value
	}

	return result, nil
}

// 辅助函数：检查属性是否存在
func containsProperty(props map[string]string, name, value string) bool {
	if val, exists := props[name]; exists {
		return val == value
	}
	return false
}