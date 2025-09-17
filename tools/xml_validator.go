package main

import (
	"archive/zip"
	"encoding/xml"
	"fmt"
	"io"
	"log"
	"os"
	"regexp"
	"strings"
)

// XMLValidator XML结构验证器
type XMLValidator struct {
	filePath string
}

// NewXMLValidator 创建新的XML验证器
func NewXMLValidator(filePath string) *XMLValidator {
	return &XMLValidator{
		filePath: filePath,
	}
}

// ValidationResult 验证结果
type ValidationResult struct {
	IsValid        bool     `json:"is_valid"`
	Errors         []string `json:"errors"`
	Warnings       []string `json:"warnings"`
	XMLStructure   string   `json:"xml_structure"`
	Namespaces     []string `json:"namespaces"`
	Relationships  []string `json:"relationships"`
	WordCompatible bool     `json:"word_compatible"`
}

// ValidateDocxStructure 验证DOCX文件的XML结构
func (xv *XMLValidator) ValidateDocxStructure() (*ValidationResult, error) {
	result := &ValidationResult{
		IsValid:        true,
		Errors:         []string{},
		Warnings:       []string{},
		Namespaces:     []string{},
		Relationships:  []string{},
		WordCompatible: true,
	}

	// 打开DOCX文件
	reader, err := zip.OpenReader(xv.filePath)
	if err != nil {
		result.IsValid = false
		result.Errors = append(result.Errors, fmt.Sprintf("无法打开DOCX文件: %v", err))
		return result, nil
	}
	defer reader.Close()

	// 验证必需的文件
	requiredFiles := []string{
		"word/document.xml",
		"[Content_Types].xml",
		"_rels/.rels",
		"word/_rels/document.xml.rels",
	}

	fileMap := make(map[string]*zip.File)
	for _, file := range reader.File {
		fileMap[file.Name] = file
	}

	// 检查必需文件
	for _, requiredFile := range requiredFiles {
		if _, exists := fileMap[requiredFile]; !exists {
			result.IsValid = false
			result.WordCompatible = false
			result.Errors = append(result.Errors, fmt.Sprintf("缺少必需文件: %s", requiredFile))
		}
	}

	// 验证document.xml
	if documentFile, exists := fileMap["word/document.xml"]; exists {
		err := xv.validateDocumentXML(documentFile, result)
		if err != nil {
			result.Errors = append(result.Errors, fmt.Sprintf("验证document.xml失败: %v", err))
		}
	}

	// 验证关系文件
	if relsFile, exists := fileMap["word/_rels/document.xml.rels"]; exists {
		err := xv.validateRelationships(relsFile, result)
		if err != nil {
			result.Errors = append(result.Errors, fmt.Sprintf("验证关系文件失败: %v", err))
		}
	}

	// 验证Content Types
	if contentTypesFile, exists := fileMap["[Content_Types].xml"]; exists {
		err := xv.validateContentTypes(contentTypesFile, result)
		if err != nil {
			result.Errors = append(result.Errors, fmt.Sprintf("验证Content Types失败: %v", err))
		}
	}

	// 检查自定义属性文件
	if customPropsFile, exists := fileMap["docProps/custom.xml"]; exists {
		err := xv.validateCustomProperties(customPropsFile, result)
		if err != nil {
			result.Warnings = append(result.Warnings, fmt.Sprintf("验证自定义属性失败: %v", err))
		}
	}

	// 最终判断
	if len(result.Errors) > 0 {
		result.IsValid = false
		result.WordCompatible = false
	}

	return result, nil
}

// validateDocumentXML 验证document.xml的结构
func (xv *XMLValidator) validateDocumentXML(file *zip.File, result *ValidationResult) error {
	rc, err := file.Open()
	if err != nil {
		return err
	}
	defer rc.Close()

	content, err := io.ReadAll(rc)
	if err != nil {
		return err
	}

	xmlContent := string(content)
	result.XMLStructure = "document.xml"

	// 检查XML格式是否有效
	var doc interface{}
	err = xml.Unmarshal(content, &doc)
	if err != nil {
		result.IsValid = false
		result.WordCompatible = false
		result.Errors = append(result.Errors, fmt.Sprintf("document.xml XML格式无效: %v", err))
		return nil
	}

	// 检查必需的命名空间
	requiredNamespaces := []string{
		"xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"",
	}

	for _, ns := range requiredNamespaces {
		if !strings.Contains(xmlContent, ns) {
			result.WordCompatible = false
			result.Errors = append(result.Errors, fmt.Sprintf("缺少必需命名空间: %s", ns))
		} else {
			result.Namespaces = append(result.Namespaces, ns)
		}
	}

	// 检查文档结构
	if !strings.Contains(xmlContent, "<w:document") {
		result.IsValid = false
		result.WordCompatible = false
		result.Errors = append(result.Errors, "缺少根元素 <w:document>")
	}

	if !strings.Contains(xmlContent, "<w:body>") {
		result.IsValid = false
		result.WordCompatible = false
		result.Errors = append(result.Errors, "缺少文档主体 <w:body>")
	}

	// 检查是否有未闭合的标签
	err = xv.checkUnclosedTags(xmlContent, result)
	if err != nil {
		result.Warnings = append(result.Warnings, fmt.Sprintf("标签检查警告: %v", err))
	}

	// 检查是否有损坏的文本标签
	err = xv.checkTextTags(xmlContent, result)
	if err != nil {
		result.Warnings = append(result.Warnings, fmt.Sprintf("文本标签检查警告: %v", err))
	}

	return nil
}

// checkUnclosedTags 检查未闭合的标签
func (xv *XMLValidator) checkUnclosedTags(xmlContent string, result *ValidationResult) error {
	// 检查常见的Word标签是否正确闭合
	commonTags := []string{"w:p", "w:r", "w:t", "w:pPr", "w:rPr"}
	
	for _, tag := range commonTags {
		openPattern := fmt.Sprintf("<%s[^>]*>", tag)
		closePattern := fmt.Sprintf("</%s>", tag)
		
		openRe := regexp.MustCompile(openPattern)
		closeRe := regexp.MustCompile(closePattern)
		
		openMatches := openRe.FindAllString(xmlContent, -1)
		closeMatches := closeRe.FindAllString(xmlContent, -1)
		
		// 过滤自闭合标签
		selfClosingPattern := fmt.Sprintf("<%s[^>]*/>", tag)
		selfClosingRe := regexp.MustCompile(selfClosingPattern)
		selfClosingMatches := selfClosingRe.FindAllString(xmlContent, -1)
		
		openCount := len(openMatches) - len(selfClosingMatches)
		closeCount := len(closeMatches)
		
		if openCount != closeCount {
			result.WordCompatible = false
			result.Errors = append(result.Errors, 
				fmt.Sprintf("标签 <%s> 未正确闭合: 开始标签 %d 个，结束标签 %d 个", 
					tag, openCount, closeCount))
		}
	}
	
	return nil
}

// checkTextTags 检查文本标签的完整性
func (xv *XMLValidator) checkTextTags(xmlContent string, result *ValidationResult) error {
	// 检查 <w:t> 标签是否正确
	textTagPattern := `<w:t[^>]*>([^<]*)</w:t>`
	re := regexp.MustCompile(textTagPattern)
	matches := re.FindAllStringSubmatch(xmlContent, -1)
	
	for _, match := range matches {
		if len(match) > 1 {
			textContent := match[1]
			// 检查文本内容是否包含未转义的特殊字符
			if strings.Contains(textContent, "<") || strings.Contains(textContent, ">") {
				result.Warnings = append(result.Warnings, 
					fmt.Sprintf("文本标签包含未转义的特殊字符: %s", textContent))
			}
		}
	}
	
	return nil
}

// validateRelationships 验证关系文件
func (xv *XMLValidator) validateRelationships(file *zip.File, result *ValidationResult) error {
	rc, err := file.Open()
	if err != nil {
		return err
	}
	defer rc.Close()

	content, err := io.ReadAll(rc)
	if err != nil {
		return err
	}

	// 检查XML格式
	var doc interface{}
	err = xml.Unmarshal(content, &doc)
	if err != nil {
		result.IsValid = false
		result.WordCompatible = false
		result.Errors = append(result.Errors, fmt.Sprintf("关系文件XML格式无效: %v", err))
		return nil
	}

	xmlContent := string(content)
	result.Relationships = append(result.Relationships, "document.xml.rels")

	// 检查关系命名空间
	if !strings.Contains(xmlContent, "http://schemas.openxmlformats.org/package/2006/relationships") {
		result.WordCompatible = false
		result.Errors = append(result.Errors, "关系文件缺少正确的命名空间")
	}

	return nil
}

// validateContentTypes 验证Content Types文件
func (xv *XMLValidator) validateContentTypes(file *zip.File, result *ValidationResult) error {
	rc, err := file.Open()
	if err != nil {
		return err
	}
	defer rc.Close()

	content, err := io.ReadAll(rc)
	if err != nil {
		return err
	}

	// 检查XML格式
	var doc interface{}
	err = xml.Unmarshal(content, &doc)
	if err != nil {
		result.IsValid = false
		result.WordCompatible = false
		result.Errors = append(result.Errors, fmt.Sprintf("Content Types文件XML格式无效: %v", err))
		return nil
	}

	xmlContent := string(content)

	// 检查必需的Content Types
	requiredTypes := []string{
		"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
	}

	for _, contentType := range requiredTypes {
		if !strings.Contains(xmlContent, contentType) {
			result.WordCompatible = false
			result.Errors = append(result.Errors, fmt.Sprintf("缺少必需的Content Type: %s", contentType))
		}
	}

	return nil
}

// validateCustomProperties 验证自定义属性文件
func (xv *XMLValidator) validateCustomProperties(file *zip.File, result *ValidationResult) error {
	rc, err := file.Open()
	if err != nil {
		return err
	}
	defer rc.Close()

	content, err := io.ReadAll(rc)
	if err != nil {
		return err
	}

	// 检查XML格式
	var doc interface{}
	err = xml.Unmarshal(content, &doc)
	if err != nil {
		result.Warnings = append(result.Warnings, fmt.Sprintf("自定义属性XML格式无效: %v", err))
		return nil
	}

	return nil
}

// PrintValidationReport 打印验证报告
func (xv *XMLValidator) PrintValidationReport(result *ValidationResult) {
	fmt.Println("\n=== DOCX XML结构验证报告 ===")
	fmt.Printf("文件: %s\n", xv.filePath)
	fmt.Printf("XML结构有效: %v\n", result.IsValid)
	fmt.Printf("Word兼容: %v\n", result.WordCompatible)

	if len(result.Errors) > 0 {
		fmt.Println("\n错误:")
		for i, err := range result.Errors {
			fmt.Printf("  %d. %s\n", i+1, err)
		}
	}

	if len(result.Warnings) > 0 {
		fmt.Println("\n警告:")
		for i, warning := range result.Warnings {
			fmt.Printf("  %d. %s\n", i+1, warning)
		}
	}

	if len(result.Namespaces) > 0 {
		fmt.Println("\n检测到的命名空间:")
		for i, ns := range result.Namespaces {
			fmt.Printf("  %d. %s\n", i+1, ns)
		}
	}

	if len(result.Relationships) > 0 {
		fmt.Println("\n关系文件:")
		for i, rel := range result.Relationships {
			fmt.Printf("  %d. %s\n", i+1, rel)
		}
	}

	fmt.Println("\n=== 验证完成 ===")
}

// 主函数用于测试
func main() {
	if len(os.Args) < 2 {
		log.Fatal("用法: go run xml_validator.go <docx文件路径>")
	}

	filePath := os.Args[1]
	validator := NewXMLValidator(filePath)

	result, err := validator.ValidateDocxStructure()
	if err != nil {
		log.Fatalf("验证失败: %v", err)
	}

	validator.PrintValidationReport(result)

	// 如果发现问题，退出码为1
	if !result.IsValid || !result.WordCompatible {
		os.Exit(1)
	}
}