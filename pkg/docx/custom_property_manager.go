package docx

import (
	"encoding/json"
	"encoding/xml"
	"fmt"
	"strings"
	"time"
)

// CustomPropertyManager 管理基于自定义文档属性的替换追踪
type CustomPropertyManager struct {
	enabled bool
}

// ReplacementRecord 替换记录结构
type ReplacementRecord struct {
	Keyword     string    `json:"keyword"`
	Original    string    `json:"original"`
	Replacement string    `json:"replacement"`
	Timestamp   time.Time `json:"timestamp"`
	Version     int       `json:"version"`
}

// ReplacementHistory 替换历史记录
type ReplacementHistory struct {
	Records []ReplacementRecord `json:"records"`
}

// CustomProperties Word自定义属性XML结构
type CustomProperties struct {
	XMLName    xml.Name         `xml:"Properties"`
	Namespace  string           `xml:"xmlns,attr"`
	VTNamespace string          `xml:"xmlns:vt,attr"`
	Properties []CustomProperty `xml:"property"`
}

// CustomProperty 单个自定义属性
type CustomProperty struct {
	FmtID    string        `xml:"fmtid,attr"`
	PID      string        `xml:"pid,attr"`
	Name     string        `xml:"name,attr"`
	Lpwstr   VTLpwstr      `xml:"lpwstr"`
	Value    string        `xml:"-"` // 用于存储实际值
}

// VTLpwstr 表示vt:lpwstr元素
type VTLpwstr struct {
	XMLName xml.Name `xml:"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes lpwstr"`
	Value   string   `xml:",chardata"`
}

// NewCustomPropertyManager 创建新的自定义属性管理器
func NewCustomPropertyManager() *CustomPropertyManager {
	return &CustomPropertyManager{
		enabled: true,
	}
}

// IsEnabled 检查是否启用追踪
func (cpm *CustomPropertyManager) IsEnabled() bool {
	return cpm.enabled
}

// SetEnabled 设置启用状态
func (cpm *CustomPropertyManager) SetEnabled(enabled bool) {
	cpm.enabled = enabled
}

// ParseCustomProperties 解析自定义属性XML
func (cpm *CustomPropertyManager) ParseCustomProperties(xmlContent string) (*CustomProperties, error) {
	if xmlContent == "" {
		// 创建默认的自定义属性结构
		return &CustomProperties{
			Namespace:   "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties",
			VTNamespace: "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes",
			Properties:  []CustomProperty{},
		}, nil
	}

	var props CustomProperties
	err := xml.Unmarshal([]byte(xmlContent), &props)
	if err != nil {
		return nil, fmt.Errorf("解析自定义属性XML失败: %v", err)
	}

	// 将Lpwstr.Value复制到Value字段
	for i := range props.Properties {
		props.Properties[i].Value = props.Properties[i].Lpwstr.Value
	}

	return &props, nil
}

// GetReplacementHistory 获取替换历史记录
func (cpm *CustomPropertyManager) GetReplacementHistory(props *CustomProperties) (*ReplacementHistory, error) {
	if !cpm.enabled {
		return &ReplacementHistory{Records: []ReplacementRecord{}}, nil
	}

	// 查找替换历史属性
	for _, prop := range props.Properties {
		if prop.Name == "DocxReplacerHistory" {
			var history ReplacementHistory
			err := json.Unmarshal([]byte(prop.Value), &history)
			if err != nil {
				return nil, fmt.Errorf("解析替换历史失败: %v", err)
			}
			return &history, nil
		}
	}

	// 如果没有找到历史记录，返回空记录
	return &ReplacementHistory{Records: []ReplacementRecord{}}, nil
}

// AddReplacementRecord 添加替换记录
func (cpm *CustomPropertyManager) AddReplacementRecord(props *CustomProperties, keyword, original, replacement string) error {
	if !cpm.enabled {
		return nil
	}

	// 获取现有历史记录
	history, err := cpm.GetReplacementHistory(props)
	if err != nil {
		return err
	}

	// 检查是否已存在相同关键词的记录，如果存在则更新版本号
	version := 1
	for i, record := range history.Records {
		if record.Keyword == keyword {
			version = record.Version + 1
			// 更新现有记录
			history.Records[i] = ReplacementRecord{
				Keyword:     keyword,
				Original:    original,
				Replacement: replacement,
				Timestamp:   time.Now(),
				Version:     version,
			}
			return cpm.updateHistoryProperty(props, history)
		}
	}

	// 添加新记录
	newRecord := ReplacementRecord{
		Keyword:     keyword,
		Original:    original,
		Replacement: replacement,
		Timestamp:   time.Now(),
		Version:     version,
	}
	history.Records = append(history.Records, newRecord)

	return cpm.updateHistoryProperty(props, history)
}

// updateHistoryProperty 更新历史属性
func (cpm *CustomPropertyManager) updateHistoryProperty(props *CustomProperties, history *ReplacementHistory) error {
	historyJSON, err := json.Marshal(history)
	if err != nil {
		return fmt.Errorf("序列化替换历史失败: %v", err)
	}

	fmt.Printf("[DEBUG] updateHistoryProperty: 序列化历史记录，长度=%d, 记录数=%d\n", len(historyJSON), len(history.Records))

	// 查找并更新现有属性
	for i, prop := range props.Properties {
		if prop.Name == "DocxReplacerHistory" {
			fmt.Printf("[DEBUG] updateHistoryProperty: 更新现有历史属性\n")
			props.Properties[i].Value = string(historyJSON)
			return nil
		}
	}

	// 如果不存在，添加新属性
	newPID := cpm.getNextPID(props)
	fmt.Printf("[DEBUG] updateHistoryProperty: 添加新历史属性，PID=%d\n", newPID)
	newProperty := CustomProperty{
		FmtID: "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}",
		PID:   fmt.Sprintf("%d", newPID),
		Name:  "DocxReplacerHistory",
		Value: string(historyJSON),
	}
	props.Properties = append(props.Properties, newProperty)
	fmt.Printf("[DEBUG] updateHistoryProperty: 属性添加完成，当前属性总数=%d\n", len(props.Properties))

	return nil
}

// getNextPID 获取下一个可用的PID
func (cpm *CustomPropertyManager) getNextPID(props *CustomProperties) int {
	maxPID := 1
	for _, prop := range props.Properties {
		var pid int
		fmt.Sscanf(prop.PID, "%d", &pid)
		if pid > maxPID {
			maxPID = pid
		}
	}
	return maxPID + 1
}

// GenerateCustomPropertiesXML 生成自定义属性XML
func (cpm *CustomPropertyManager) GenerateCustomPropertiesXML(props *CustomProperties) (string, error) {
	fmt.Printf("[DEBUG] GenerateCustomPropertiesXML: 属性数量=%d\n", len(props.Properties))
	for i, prop := range props.Properties {
		fmt.Printf("[DEBUG] 属性[%d]: Name=%s, Value=%s\n", i, prop.Name, prop.Value)
	}
	
	// 设置命名空间
	if props.Namespace == "" {
		props.Namespace = "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties"
	}
	if props.VTNamespace == "" {
		props.VTNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"
	}

	// 在生成XML前，确保Lpwstr字段正确设置
	for i := range props.Properties {
		props.Properties[i].Lpwstr = VTLpwstr{
			Value: props.Properties[i].Value,
		}
	}

	xmlData, err := xml.MarshalIndent(props, "", "  ")
	if err != nil {
		return "", fmt.Errorf("生成自定义属性XML失败: %v", err)
	}

	// 添加XML声明
	xmlContent := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>` + "\n" + string(xmlData)
	return xmlContent, nil
}

// GetReplacementByKeyword 根据关键词获取替换记录
func (cpm *CustomPropertyManager) GetReplacementByKeyword(props *CustomProperties, keyword string) (*ReplacementRecord, error) {
	history, err := cpm.GetReplacementHistory(props)
	if err != nil {
		return nil, err
	}

	for _, record := range history.Records {
		if record.Keyword == keyword {
			return &record, nil
		}
	}

	return nil, nil // 未找到记录
}

// GetReplacementByOriginal 根据原始关键词获取替换记录
func (cpm *CustomPropertyManager) GetReplacementByOriginal(props *CustomProperties, original string) (*ReplacementRecord, error) {
	history, err := cpm.GetReplacementHistory(props)
	if err != nil {
		return nil, err
	}

	for _, record := range history.Records {
		if record.Original == original {
			return &record, nil
		}
	}

	return nil, nil // 未找到记录
}

// CleanupOrphanedRecords 清理孤立的记录（在文档中不再存在的关键词）
func (cpm *CustomPropertyManager) CleanupOrphanedRecords(props *CustomProperties, existingKeywords []string) error {
	if !cpm.enabled {
		return nil
	}

	history, err := cpm.GetReplacementHistory(props)
	if err != nil {
		return err
	}

	// 创建关键词映射
	keywordMap := make(map[string]bool)
	for _, keyword := range existingKeywords {
		keywordMap[keyword] = true
	}

	// 过滤掉不存在的关键词记录
	var filteredRecords []ReplacementRecord
	for _, record := range history.Records {
		if keywordMap[record.Keyword] {
			filteredRecords = append(filteredRecords, record)
		}
	}

	// 如果记录数量发生变化，更新历史
	if len(filteredRecords) != len(history.Records) {
		history.Records = filteredRecords
		return cpm.updateHistoryProperty(props, history)
	}

	return nil
}

// GetRecordCount 获取记录数量
func (cpm *CustomPropertyManager) GetRecordCount(props *CustomProperties) int {
	history, err := cpm.GetReplacementHistory(props)
	if err != nil {
		return 0
	}
	return len(history.Records)
}

// HasReplacements 检查是否有替换记录
func (cpm *CustomPropertyManager) HasReplacements(props *CustomProperties) bool {
	return cpm.GetRecordCount(props) > 0
}

// GetKeywords 获取所有已替换的关键词
func (cpm *CustomPropertyManager) GetKeywords(props *CustomProperties) []string {
	history, err := cpm.GetReplacementHistory(props)
	if err != nil {
		return []string{}
	}

	var keywords []string
	for _, record := range history.Records {
		keywords = append(keywords, record.Keyword)
	}
	return keywords
}

// HasReplaced 检查关键词是否已经被替换过
func (cpm *CustomPropertyManager) HasReplaced(props *CustomProperties, keyword, replacement string) bool {
	if props == nil {
		return false
	}

	for _, prop := range props.Properties {
		if prop.Name == cpm.getPropertyName(keyword) {
			return prop.Value == replacement
		}
	}
	return false
}

// AddReplacement 添加替换记录到自定义属性
func (cpm *CustomPropertyManager) AddReplacement(props *CustomProperties, keyword, original, replacement string) error {
	if !cpm.enabled {
		return nil
	}

	propertyName := cpm.getPropertyName(keyword)
	fmt.Printf("[DEBUG] AddReplacement: keyword=%s, propertyName=%s, replacement=%s\n", keyword, propertyName, replacement)
	
	// 查找并更新现有属性
	for i, prop := range props.Properties {
		if prop.Name == propertyName {
			props.Properties[i].Value = replacement
			props.Properties[i].Lpwstr = VTLpwstr{Value: replacement}
			return nil
		}
	}

	// 如果不存在，添加新属性
	newPID := cpm.getNextPID(props)
	newProperty := CustomProperty{
		FmtID: "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}",
		PID:   fmt.Sprintf("%d", newPID),
		Name:  propertyName,
		Value: replacement,
		Lpwstr: VTLpwstr{Value: replacement},
	}
	props.Properties = append(props.Properties, newProperty)

	return nil
}

// GetLastValue 获取关键词的最后替换值
func (cpm *CustomPropertyManager) GetLastValue(keyword string) string {
	// 这个方法需要在调用时传入CustomProperties实例
	// 为了保持接口一致性，这里返回空字符串
	// 实际使用时应该调用GetLastValueFromProps
	return ""
}

// GetLastValueFromProps 从自定义属性中获取关键词的最后替换值
func (cpm *CustomPropertyManager) GetLastValueFromProps(props *CustomProperties, keyword string) string {
	if props == nil {
		return ""
	}

	for _, prop := range props.Properties {
		if prop.Name == cpm.getPropertyName(keyword) {
			return prop.Value
		}
	}
	return ""
}

// getPropertyName 获取关键词对应的属性名
func (cpm *CustomPropertyManager) getPropertyName(keyword string) string {
	return "DocxReplacer_" + strings.ReplaceAll(keyword, " ", "_")
}