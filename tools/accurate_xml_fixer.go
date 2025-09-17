package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// AccurateXMLFixer 准确XML修复器
type AccurateXMLFixer struct {
	filePath string
}

// TagInfo 标签信息
type TagInfo struct {
	Name     string
	Position int
	IsOpen   bool
	FullTag  string
}

// NewAccurateXMLFixer 创建准确XML修复器
func NewAccurateXMLFixer(filePath string) *AccurateXMLFixer {
	return &AccurateXMLFixer{
		filePath: filePath,
	}
}

// FixXMLStructure 修复XML结构
func (axf *AccurateXMLFixer) FixXMLStructure(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(axf.filePath)
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
			fixedContent, err := axf.fixDocumentXML(file)
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
			if err := axf.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Println("准确XML结构修复完成!")
	return nil
}

// fixDocumentXML 修复document.xml文件
func (axf *AccurateXMLFixer) fixDocumentXML(file *zip.File) (string, error) {
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

	// 准确修复XML标签
	xmlContent = axf.accurateFixXMLTags(xmlContent)

	return xmlContent, nil
}

// accurateFixXMLTags 准确修复XML标签
func (axf *AccurateXMLFixer) accurateFixXMLTags(content string) string {
	// 需要修复的具体标签（精确匹配）
	tags := []string{"w:p", "w:r", "w:t"}

	for _, tag := range tags {
		content = axf.fixSpecificTag(content, tag)
	}

	return content
}

// fixSpecificTag 修复特定标签
func (axf *AccurateXMLFixer) fixSpecificTag(content, tag string) string {
	// 查找所有该标签的开始和结束标签
	tags := axf.findExactTags(content, tag)

	// 使用栈来匹配标签
	stack := []*TagInfo{}

	for _, tagInfo := range tags {
		if tagInfo.IsOpen {
			// 开始标签，压入栈
			stack = append(stack, tagInfo)
		} else {
			// 结束标签，尝试匹配栈顶
			if len(stack) > 0 && stack[len(stack)-1].Name == tagInfo.Name {
				// 匹配成功，弹出栈顶
				stack = stack[:len(stack)-1]
			}
		}
	}

	// 栈中剩余的都是未匹配的开始标签
	if len(stack) > 0 {
		fmt.Printf("标签 <%s>: 发现 %d 个未匹配的开始标签\n", tag, len(stack))
		
		// 添加对应数量的结束标签
		closeTags := strings.Repeat(fmt.Sprintf("</%s>", tag), len(stack))

		// 在</w:body>之前添加结束标签
		bodyEndPos := strings.LastIndex(content, "</w:body>")
		if bodyEndPos != -1 {
			content = content[:bodyEndPos] + closeTags + content[bodyEndPos:]
			fmt.Printf("已在</w:body>之前添加 %d 个 </%s> 标签\n", len(stack), tag)
		} else {
			// 如果找不到</w:body>，在</w:document>之前添加
			docEndPos := strings.LastIndex(content, "</w:document>")
			if docEndPos != -1 {
				content = content[:docEndPos] + closeTags + content[docEndPos:]
				fmt.Printf("已在</w:document>之前添加 %d 个 </%s> 标签\n", len(stack), tag)
			}
		}
	} else {
		fmt.Printf("标签 <%s>: XML结构正确\n", tag)
	}

	return content
}

// findExactTags 精确查找指定标签
func (axf *AccurateXMLFixer) findExactTags(content, tag string) []*TagInfo {
	tags := []*TagInfo{}

	// 精确匹配开始标签（不包括自闭合标签）
	// 使用单词边界确保精确匹配
	openPattern := fmt.Sprintf(`<%s(\s[^>]*)?[^/]>`, regexp.QuoteMeta(tag))
	openRe := regexp.MustCompile(openPattern)
	openMatches := openRe.FindAllStringIndex(content, -1)

	for _, match := range openMatches {
		tagText := content[match[0]:match[1]]
		// 确保不是自闭合标签
		if !strings.HasSuffix(strings.TrimSpace(tagText), "/>") {
			tags = append(tags, &TagInfo{
				Name:     tag,
				Position: match[0],
				IsOpen:   true,
				FullTag:  tagText,
			})
		}
	}

	// 精确匹配结束标签
	closePattern := fmt.Sprintf(`</%s>`, regexp.QuoteMeta(tag))
	closeRe := regexp.MustCompile(closePattern)
	closeMatches := closeRe.FindAllStringIndex(content, -1)

	for _, match := range closeMatches {
		tagText := content[match[0]:match[1]]
		tags = append(tags, &TagInfo{
			Name:     tag,
			Position: match[0],
			IsOpen:   false,
			FullTag:  tagText,
		})
	}

	// 按位置排序
	for i := 0; i < len(tags)-1; i++ {
		for j := i + 1; j < len(tags); j++ {
			if tags[i].Position > tags[j].Position {
				tags[i], tags[j] = tags[j], tags[i]
			}
		}
	}

	return tags
}

// copyFile 复制文件到ZIP
func (axf *AccurateXMLFixer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
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
		fmt.Println("用法: go run accurate_xml_fixer.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	fixer := NewAccurateXMLFixer(inputFile)
	if err := fixer.FixXMLStructure(outputFile); err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("准确修复完成!")
}