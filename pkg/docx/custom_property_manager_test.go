package docx

import (
	"strings"
	"testing"
)

// TestCustomPropertyManager_ParseCustomProperties 测试解析自定义属性
func TestCustomPropertyManager_ParseCustomProperties(t *testing.T) {
	manager := NewCustomPropertyManager()

	// 测试解析空内容
	emptyProps, err := manager.ParseCustomProperties("")
	if err != nil {
		t.Fatalf("解析空自定义属性失败: %v", err)
	}
	if emptyProps == nil {
		t.Fatal("解析空自定义属性应该返回非nil结构")
	}

	// 测试解析有效的XML内容
	validXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/custom-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <property fmtid="{D5CDD505-2E9C-101B-9397-08002B2CF9AE}" pid="2" name="DocxReplacer_#测试#">
    <vt:lpwstr>测试值</vt:lpwstr>
  </property>
</Properties>`

	props, err := manager.ParseCustomProperties(validXML)
	if err != nil {
		t.Fatalf("解析有效XML失败: %v", err)
	}
	if props == nil {
		t.Fatal("解析有效XML应该返回非nil结构")
	}

	// 验证解析结果
	if len(props.Properties) == 0 {
		t.Error("解析后应该包含属性")
	}

	t.Logf("解析自定义属性测试通过")
}

// TestCustomPropertyManager_AddReplacement 测试添加替换记录
func TestCustomPropertyManager_AddReplacement(t *testing.T) {
	manager := NewCustomPropertyManager()

	// 创建空属性结构
	props, err := manager.ParseCustomProperties("")
	if err != nil {
		t.Fatalf("创建空属性结构失败: %v", err)
	}

	// 测试添加新的替换记录
	manager.AddReplacement(props, "#产品名称#", "", "测试产品")

	// 验证属性是否被添加
	found := false
	for _, prop := range props.Properties {
		if prop.Name == "DocxReplacer_#产品名称#" && prop.Value == "测试产品" {
			found = true
			break
		}
	}
	if !found {
		t.Error("添加的替换记录未找到")
	}

	// 测试更新现有的替换记录
	manager.AddReplacement(props, "#产品名称#", "", "更新后的产品")

	// 验证属性是否被更新
	found = false
	for _, prop := range props.Properties {
		if prop.Name == "DocxReplacer_#产品名称#" && prop.Value == "更新后的产品" {
			found = true
			break
		}
	}
	if !found {
		t.Error("更新的替换记录未找到")
	}

	t.Logf("添加替换记录测试通过")
}

// TestCustomPropertyManager_HasReplaced 测试检查是否已替换
func TestCustomPropertyManager_HasReplaced(t *testing.T) {
	manager := NewCustomPropertyManager()

	// 创建空属性结构
	props, err := manager.ParseCustomProperties("")
	if err != nil {
		t.Fatalf("创建空属性结构失败: %v", err)
	}

	// 添加替换记录
	manager.AddReplacement(props, "#测试关键词#", "", "测试值")

	// 测试相同值应该返回true
	if !manager.HasReplaced(props, "#测试关键词#", "测试值") {
		t.Error("相同值的HasReplaced应该返回true")
	}

	// 测试不同值应该返回false
	if manager.HasReplaced(props, "#测试关键词#", "不同的值") {
		t.Error("不同值的HasReplaced应该返回false")
	}

	// 测试不存在的关键词应该返回false
	if manager.HasReplaced(props, "#不存在的关键词#", "任意值") {
		t.Error("不存在关键词的HasReplaced应该返回false")
	}

	t.Logf("检查是否已替换测试通过")
}

// TestCustomPropertyManager_GenerateCustomPropertiesXML 测试生成XML
func TestCustomPropertyManager_GenerateCustomPropertiesXML(t *testing.T) {
	manager := NewCustomPropertyManager()

	// 创建空属性结构
	props, err := manager.ParseCustomProperties("")
	if err != nil {
		t.Fatalf("创建空属性结构失败: %v", err)
	}

	// 添加多个替换记录
	manager.AddReplacement(props, "#产品名称#", "", "测试产品")
	manager.AddReplacement(props, "#结构组成#", "", "测试组成")

	// 生成XML
	xmlContent, err := manager.GenerateCustomPropertiesXML(props)
	if err != nil {
		t.Fatalf("生成XML失败: %v", err)
	}

	// 验证XML内容
	if !strings.Contains(xmlContent, "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>") {
		t.Error("生成的XML应该包含XML声明")
	}

	if !strings.Contains(xmlContent, "DocxReplacer_#产品名称#") {
		t.Error("生成的XML应该包含产品名称属性")
	}

	if !strings.Contains(xmlContent, "测试产品") {
		t.Error("生成的XML应该包含产品名称值")
	}

	if !strings.Contains(xmlContent, "DocxReplacer_#结构组成#") {
		t.Error("生成的XML应该包含结构组成属性")
	}

	if !strings.Contains(xmlContent, "测试组成") {
		t.Error("生成的XML应该包含结构组成值")
	}

	// 验证XML格式正确性
	if !strings.Contains(xmlContent, "<Properties xmlns=\"") {
		t.Error("生成的XML应该包含正确的命名空间")
	}

	t.Logf("生成XML测试通过")
}

// TestCustomPropertyManager_Integration 集成测试
func TestCustomPropertyManager_Integration(t *testing.T) {
	manager := NewCustomPropertyManager()

	// 1. 创建初始属性
	props, err := manager.ParseCustomProperties("")
	if err != nil {
		t.Fatalf("创建初始属性失败: %v", err)
	}

	// 2. 添加第一批替换记录
	manager.AddReplacement(props, "#关键词1#", "", "值1")
	manager.AddReplacement(props, "#关键词2#", "", "值2")

	// 3. 生成XML
	xmlContent1, err := manager.GenerateCustomPropertiesXML(props)
	if err != nil {
		t.Fatalf("第一次生成XML失败: %v", err)
	}

	// 4. 重新解析生成的XML
	props2, err := manager.ParseCustomProperties(xmlContent1)
	if err != nil {
		t.Fatalf("重新解析XML失败: %v", err)
	}

	// 5. 验证解析结果
	if !manager.HasReplaced(props2, "#关键词1#", "值1") {
		t.Error("重新解析后关键词1的记录丢失")
	}

	if !manager.HasReplaced(props2, "#关键词2#", "值2") {
		t.Error("重新解析后关键词2的记录丢失")
	}

	// 6. 添加更多记录
	manager.AddReplacement(props2, "#关键词3#", "", "值3")

	// 7. 再次生成XML
	xmlContent2, err := manager.GenerateCustomPropertiesXML(props2)
	if err != nil {
		t.Fatalf("第二次生成XML失败: %v", err)
	}

	// 8. 验证所有记录都存在
	props3, err := manager.ParseCustomProperties(xmlContent2)
	if err != nil {
		t.Fatalf("第二次重新解析XML失败: %v", err)
	}

	if !manager.HasReplaced(props3, "#关键词1#", "值1") {
		t.Error("最终解析后关键词1的记录丢失")
	}

	if !manager.HasReplaced(props3, "#关键词2#", "值2") {
		t.Error("最终解析后关键词2的记录丢失")
	}

	if !manager.HasReplaced(props3, "#关键词3#", "值3") {
		t.Error("最终解析后关键词3的记录丢失")
	}

	t.Logf("集成测试通过")
}