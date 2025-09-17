package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// NestedXMLFixer 嵌套XML修复器
type NestedXMLFixer struct {
	filePath string
}

// TagInfo 标签信息
type TagInfo struct {
	Name     string
	Position int
	IsOpen   bool
	IsSelfClosing bool
}

// NewNestedXMLFixer 创建嵌套XML修复器
func NewNestedXMLFixer(filePath string) *NestedXMLFixer {
	return &NestedXMLFixer{
		filePath: filePath,
	}
}

// FixXMLStructure 修复XML结构
func (nxf *NestedXMLFixer) FixXMLStructure(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(nxf.filePath)
	if err != nil {
		return fmt.Errorf("打开DOCX文件失败: %v", err)
	}
	defer reader.Close()

	// 创建输出文件
	outputFile, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("创建输出文件失败: %v", err)
	}
	defer outputFile.Close()

	// 创建ZIP写入器
	zipWriter := zip.NewWriter(outputFile)
	defer zipWriter.Close()

	// 处理每个文件
	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			// 修复document.xml文件
			fixedContent, err := nxf.fixDocumentXML(file)
			if err != nil {
				return fmt.Errorf("修复document.xml失败: %v", err)
			}

			// 写入修复后的内容
			writer, err := zipWriter.Create(file.Name)
			if err != nil {
				return fmt.Errorf("创建文件失败: %v", err)
			}
			_, err = writer.Write([]byte(fixedContent))
			if err != nil {
				return fmt.Errorf("写入文件失败: %v", err)
			}
		} else {
			// 复制其他文件
			if err := nxf.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Println("嵌套XML结构修复完成!")
	return nil
}

// fixDocumentXML 修复document.xml文件
func (nxf *NestedXMLFixer) fixDocumentXML(file *zip.File) (string, error) {
	fileReader, err := file.Open()
	if err != nil {
		return "", err
	}
	defer fileReader.Close()

	content, err := io.ReadAll(fileReader)
	if err != nil {
		return "", err
	}

	xmlContent := string(content)

	// 使用嵌套感知的方式修复XML标签
	xmlContent = nxf.fixNestedXMLTags(xmlContent)

	return xmlContent, nil
}

// fixNestedXMLTags 修复嵌套XML标签
func (nxf *NestedXMLFixer) fixNestedXMLTags(content string) string {
	// 找到</w:body>的位置
	bodyEndPos := strings.LastIndex(content, "</w:body>")
	if bodyEndPos == -1 {
		fmt.Println("未找到</w:body>标签")
		return content
	}

	// 分析从文档开始到</w:body>之前的所有标签
	bodyContent := content[:bodyEndPos]
	afterBody := content[bodyEndPos:]

	// 解析所有标签
	tags := nxf.parseAllTags(bodyContent)

	// 使用栈来跟踪未闭合的标签
	stack := make([]string, 0)
	for _, tag := range tags {
		if tag.IsSelfClosing {
			continue
		}

		if tag.IsOpen {
			// 开始标签，压入栈
			stack = append(stack, tag.Name)
		} else {
			// 结束标签，从栈中弹出
			if len(stack) > 0 && stack[len(stack)-1] == tag.Name {
				stack = stack[:len(stack)-1]
			}
		}
	}

	// 栈中剩余的就是未闭合的标签，需要按照LIFO顺序添加结束标签
	if len(stack) > 0 {
		fmt.Printf("发现 %d 个未闭合的标签: %v\n", len(stack), stack)

		// 按照栈的顺序（LIFO）添加结束标签
		closeTags := ""
		for i := len(stack) - 1; i >= 0; i-- {
			closeTags += fmt.Sprintf("</%s>", stack[i])
		}

		fmt.Printf("添加结束标签: %s\n", closeTags)
		return bodyContent + closeTags + afterBody
	}

	fmt.Println("所有标签都已正确闭合")
	return content
}

// parseAllTags 解析所有标签
func (nxf *NestedXMLFixer) parseAllTags(content string) []TagInfo {
	tags := make([]TagInfo, 0)

	// 匹配所有XML标签的正则表达式
	tagPattern := `<(/?)([^\s/>]+)[^>]*(/?)>`
	re := regexp.MustCompile(tagPattern)

	matches := re.FindAllStringSubmatch(content, -1)
	for _, match := range matches {
		if len(match) >= 4 {
			isClosing := match[1] == "/"
			tagName := match[2]
			isSelfClosing := match[3] == "/"

			// 只处理Word相关的标签
			if strings.HasPrefix(tagName, "w:") {
				tags = append(tags, TagInfo{
					Name:          tagName,
					IsOpen:        !isClosing,
					IsSelfClosing: isSelfClosing,
				})
			}
		}
	}

	return tags
}

// copyFile 复制文件到ZIP
func (nxf *NestedXMLFixer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
	fileReader, err := file.Open()
	if err != nil {
		return err
	}
	defer fileReader.Close()

	writer, err := zipWriter.Create(file.Name)
	if err != nil {
		return err
	}

	_, err = io.Copy(writer, fileReader)
	return err
}

func main() {
	if len(os.Args) != 3 {
		fmt.Println("用法: go run nested_xml_fixer.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	fixer := NewNestedXMLFixer(inputFile)
	if err := fixer.FixXMLStructure(outputFile); err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("嵌套修复完成!")
}