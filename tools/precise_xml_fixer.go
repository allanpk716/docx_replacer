package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// PreciseXMLFixer 精确XML修复器
type PreciseXMLFixer struct {
	filePath string
}

// TagStack 标签栈
type TagStack struct {
	Name     string
	Position int
	Content  string
}

// NewPreciseXMLFixer 创建精确XML修复器
func NewPreciseXMLFixer(filePath string) *PreciseXMLFixer {
	return &PreciseXMLFixer{
		filePath: filePath,
	}
}

// FixXMLStructure 修复XML结构
func (pxf *PreciseXMLFixer) FixXMLStructure(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(pxf.filePath)
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
			fixedContent, err := pxf.fixDocumentXML(file)
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
			if err := pxf.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Println("精确XML结构修复完成!")
	return nil
}

// fixDocumentXML 修复document.xml文件
func (pxf *PreciseXMLFixer) fixDocumentXML(file *zip.File) (string, error) {
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

	// 精确修复XML标签
	xmlContent = pxf.preciseFixXMLTags(xmlContent)

	return xmlContent, nil
}

// preciseFixXMLTags 精确修复XML标签
func (pxf *PreciseXMLFixer) preciseFixXMLTags(content string) string {
	// 使用栈来跟踪未闭合的标签
	stack := []*TagStack{}

	// 查找所有Word标签
	tagPattern := `<(/?)(w:[a-zA-Z]+)([^>]*)>`
	tagRe := regexp.MustCompile(tagPattern)
	matches := tagRe.FindAllStringSubmatch(content, -1)
	matchIndices := tagRe.FindAllStringIndex(content, -1)

	for i, match := range matches {
		isClosing := match[1] == "/"
		tagName := match[2]
		tagAttrs := match[3]
		position := matchIndices[i][0]
		fullTag := match[0]

		// 跳过自闭合标签
		if strings.HasSuffix(tagAttrs, "/") {
			continue
		}

		if isClosing {
			// 结束标签，尝试匹配栈顶
			if len(stack) > 0 && stack[len(stack)-1].Name == tagName {
				// 匹配成功，弹出栈顶
				stack = stack[:len(stack)-1]
			} else {
				fmt.Printf("发现不匹配的结束标签: %s 在位置 %d\n", fullTag, position)
			}
		} else {
			// 开始标签，压入栈
			stack = append(stack, &TagStack{
				Name:     tagName,
				Position: position,
				Content:  fullTag,
			})
		}
	}

	// 栈中剩余的都是未闭合的标签
	if len(stack) > 0 {
		fmt.Printf("发现 %d 个未闭合的标签\n", len(stack))
		
		// 按照栈的逆序（后进先出）添加结束标签
		closeTags := ""
		for i := len(stack) - 1; i >= 0; i-- {
			closeTags += fmt.Sprintf("</%s>", stack[i].Name)
			fmt.Printf("需要闭合标签: %s\n", stack[i].Name)
		}

		// 在</w:body>之前添加结束标签
		bodyEndPos := strings.LastIndex(content, "</w:body>")
		if bodyEndPos != -1 {
			content = content[:bodyEndPos] + closeTags + content[bodyEndPos:]
			fmt.Printf("已在</w:body>之前添加结束标签\n")
		} else {
			// 如果找不到</w:body>，在</w:document>之前添加
			docEndPos := strings.LastIndex(content, "</w:document>")
			if docEndPos != -1 {
				content = content[:docEndPos] + closeTags + content[docEndPos:]
				fmt.Printf("已在</w:document>之前添加结束标签\n")
			}
		}
	} else {
		fmt.Println("XML结构已经正确，无需修复")
	}

	return content
}

// copyFile 复制文件到ZIP
func (pxf *PreciseXMLFixer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
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
		fmt.Println("用法: go run precise_xml_fixer.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	fixer := NewPreciseXMLFixer(inputFile)
	if err := fixer.FixXMLStructure(outputFile); err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("精确修复完成!")
}