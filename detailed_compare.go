package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"strings"
)

// compareFiles 对比两个docx文件的详细差异
func compareFiles(originalPath, modifiedPath string) error {
	fmt.Printf("对比文件:\n原始: %s\n修改: %s\n\n", originalPath, modifiedPath)

	// 提取两个文件的document.xml
	originalXML, err := extractDocumentXML(originalPath)
	if err != nil {
		return fmt.Errorf("提取原始文件XML失败: %v", err)
	}

	modifiedXML, err := extractDocumentXML(modifiedPath)
	if err != nil {
		return fmt.Errorf("提取修改文件XML失败: %v", err)
	}

	// 基本信息对比
	fmt.Printf("文件大小对比:\n")
	fmt.Printf("原始文件: %d 字符\n", len(originalXML))
	fmt.Printf("修改文件: %d 字符\n", len(modifiedXML))
	fmt.Printf("差异: %d 字符\n\n", len(modifiedXML)-len(originalXML))

	// 检查XML声明和编码
	compareXMLDeclarations(originalXML, modifiedXML)

	// 检查命名空间声明
	compareNamespaces(originalXML, modifiedXML)

	// 查找具体的文本差异
	findTextDifferences(originalXML, modifiedXML)

	// 检查文件结构完整性
	compareFileStructure(originalPath, modifiedPath)

	return nil
}

// extractDocumentXML 提取document.xml内容
func extractDocumentXML(docxPath string) (string, error) {
	reader, err := zip.OpenReader(docxPath)
	if err != nil {
		return "", err
	}
	defer reader.Close()

	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			f, err := file.Open()
			if err != nil {
				return "", err
			}
			defer f.Close()

			content, err := io.ReadAll(f)
			if err != nil {
				return "", err
			}

			return string(content), nil
		}
	}

	return "", fmt.Errorf("未找到document.xml")
}

// compareXMLDeclarations 对比XML声明
func compareXMLDeclarations(original, modified string) {
	fmt.Println("XML声明对比:")

	// 提取XML声明
	originalDecl := ""
	modifiedDecl := ""

	if strings.HasPrefix(original, "<?xml") {
		if end := strings.Index(original, "?>"); end != -1 {
			originalDecl = original[:end+2]
		}
	}

	if strings.HasPrefix(modified, "<?xml") {
		if end := strings.Index(modified, "?>"); end != -1 {
			modifiedDecl = modified[:end+2]
		}
	}

	fmt.Printf("原始: %s\n", originalDecl)
	fmt.Printf("修改: %s\n", modifiedDecl)

	if originalDecl == modifiedDecl {
		fmt.Println("✓ XML声明一致\n")
	} else {
		fmt.Println("❌ XML声明不一致\n")
	}
}

// compareNamespaces 对比命名空间声明
func compareNamespaces(original, modified string) {
	fmt.Println("命名空间对比:")

	// 查找根元素的命名空间声明
	originalNS := extractNamespaces(original)
	modifiedNS := extractNamespaces(modified)

	fmt.Printf("原始文件命名空间数量: %d\n", len(originalNS))
	fmt.Printf("修改文件命名空间数量: %d\n", len(modifiedNS))

	// 检查缺失的命名空间
	for ns := range originalNS {
		if _, exists := modifiedNS[ns]; !exists {
			fmt.Printf("❌ 缺失命名空间: %s\n", ns)
		}
	}

	// 检查新增的命名空间
	for ns := range modifiedNS {
		if _, exists := originalNS[ns]; !exists {
			fmt.Printf("+ 新增命名空间: %s\n", ns)
		}
	}

	fmt.Println()
}

// extractNamespaces 提取命名空间声明
func extractNamespaces(xmlContent string) map[string]string {
	namespaces := make(map[string]string)

	// 查找根元素
	start := strings.Index(xmlContent, "<w:document")
	if start == -1 {
		return namespaces
	}

	// 查找根元素结束
	end := strings.Index(xmlContent[start:], ">")
	if end == -1 {
		return namespaces
	}

	rootElement := xmlContent[start : start+end+1]

	// 提取xmlns声明
	lines := strings.Split(rootElement, " ")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if strings.HasPrefix(line, "xmlns") {
			parts := strings.SplitN(line, "=", 2)
			if len(parts) == 2 {
				key := parts[0]
				value := strings.Trim(parts[1], `"`)
				namespaces[key] = value
			}
		}
	}

	return namespaces
}

// findTextDifferences 查找文本差异
func findTextDifferences(original, modified string) {
	fmt.Println("文本差异分析:")

	// 查找所有替换的文本
	replacements := []struct {
		old string
		new string
	}{
		{"产品名称", "D-二聚体测定试剂盒（胶乳免疫比浊法）"},
		{"结构及组成", "adsasdadsa"},
	}

	for _, repl := range replacements {
		originalCount := strings.Count(original, repl.old)
		modifiedCount := strings.Count(modified, repl.new)
		remainingOld := strings.Count(modified, repl.old)

		fmt.Printf("替换: '%s' -> '%s'\n", repl.old, repl.new)
		fmt.Printf("  原始文件中'%s'出现次数: %d\n", repl.old, originalCount)
		fmt.Printf("  修改文件中'%s'出现次数: %d\n", repl.new, modifiedCount)
		fmt.Printf("  修改文件中剩余'%s'次数: %d\n", repl.old, remainingOld)
		if remainingOld > 0 {
			fmt.Printf("  ⚠️ 仍有未替换的文本\n")
		}
		fmt.Println()
	}
}

// compareFileStructure 对比文件结构
func compareFileStructure(originalPath, modifiedPath string) {
	fmt.Println("文件结构对比:")

	originalFiles, err := listZipFiles(originalPath)
	if err != nil {
		fmt.Printf("读取原始文件结构失败: %v\n", err)
		return
	}

	modifiedFiles, err := listZipFiles(modifiedPath)
	if err != nil {
		fmt.Printf("读取修改文件结构失败: %v\n", err)
		return
	}

	fmt.Printf("原始文件包含 %d 个文件\n", len(originalFiles))
	fmt.Printf("修改文件包含 %d 个文件\n", len(modifiedFiles))

	// 检查缺失的文件
	for _, file := range originalFiles {
		found := false
		for _, mFile := range modifiedFiles {
			if file == mFile {
				found = true
				break
			}
		}
		if !found {
			fmt.Printf("❌ 缺失文件: %s\n", file)
		}
	}

	fmt.Println()
}

// listZipFiles 列出zip文件中的所有文件
func listZipFiles(zipPath string) ([]string, error) {
	reader, err := zip.OpenReader(zipPath)
	if err != nil {
		return nil, err
	}
	defer reader.Close()

	var files []string
	for _, file := range reader.File {
		files = append(files, file.Name)
	}

	return files, nil
}

func main() {
	if len(os.Args) != 3 {
		fmt.Println("用法: go run detailed_compare.go <原始docx文件> <修改后docx文件>")
		os.Exit(1)
	}

	originalPath := os.Args[1]
	modifiedPath := os.Args[2]

	if err := compareFiles(originalPath, modifiedPath); err != nil {
		fmt.Printf("对比失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("对比完成")
}