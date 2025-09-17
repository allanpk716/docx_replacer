package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// SmartXMLFixer 智能XML修复器
type SmartXMLFixer struct {
	filePath string
}

// TagInfo 标签信息
type TagInfo struct {
	Name     string
	Position int
	IsOpen   bool
}

// NewSmartXMLFixer 创建智能XML修复器
func NewSmartXMLFixer(filePath string) *SmartXMLFixer {
	return &SmartXMLFixer{
		filePath: filePath,
	}
}

// FixXMLStructure 修复XML结构
func (sxf *SmartXMLFixer) FixXMLStructure(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(sxf.filePath)
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
			fixedContent, err := sxf.fixDocumentXML(file)
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
			if err := sxf.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Println("智能XML结构修复完成!")
	return nil
}

// fixDocumentXML 修复document.xml文件
func (sxf *SmartXMLFixer) fixDocumentXML(file *zip.File) (string, error) {
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

	// 智能修复XML标签
	xmlContent = sxf.smartFixXMLTags(xmlContent)

	return xmlContent, nil
}

// smartFixXMLTags 智能修复XML标签
func (sxf *SmartXMLFixer) smartFixXMLTags(content string) string {
	// 需要修复的标签列表（按嵌套层级排序）
	tags := []string{"w:t", "w:r", "w:p"}

	for _, tag := range tags {
		content = sxf.smartFixTag(content, tag)
	}

	return content
}

// smartFixTag 智能修复特定标签
func (sxf *SmartXMLFixer) smartFixTag(content, tag string) string {
	// 查找所有标签位置
	tags := sxf.findAllTags(content, tag)

	// 构建标签栈来匹配开始和结束标签
	stack := []*TagInfo{}
	unmatched := []*TagInfo{}

	for _, tagInfo := range tags {
		if tagInfo.IsOpen {
			// 开始标签，压入栈
			stack = append(stack, tagInfo)
		} else {
			// 结束标签，尝试匹配栈顶
			if len(stack) > 0 {
				// 弹出栈顶（匹配成功）
				stack = stack[:len(stack)-1]
			} else {
				// 没有对应的开始标签，记录为多余的结束标签
				fmt.Printf("发现多余的结束标签 </%s> 在位置 %d\n", tag, tagInfo.Position)
			}
		}
	}

	// 栈中剩余的都是未匹配的开始标签
	unmatched = stack

	if len(unmatched) > 0 {
		fmt.Printf("标签 <%s>: 发现 %d 个未匹配的开始标签\n", tag, len(unmatched))
		
		// 在适当位置添加结束标签
		content = sxf.addClosingTags(content, tag, len(unmatched))
	} else {
		fmt.Printf("标签 <%s>: XML结构正确\n", tag)
	}

	return content
}

// findAllTags 查找所有指定标签
func (sxf *SmartXMLFixer) findAllTags(content, tag string) []*TagInfo {
	tags := []*TagInfo{}

	// 查找开始标签（包括自闭合标签）
	openPattern := fmt.Sprintf(`<%s[^>]*>`, regexp.QuoteMeta(tag))
	openRe := regexp.MustCompile(openPattern)
	openMatches := openRe.FindAllStringIndex(content, -1)

	for _, match := range openMatches {
		tagText := content[match[0]:match[1]]
		// 检查是否为自闭合标签
		if !strings.HasSuffix(tagText, "/>") {
			tags = append(tags, &TagInfo{
				Name:     tag,
				Position: match[0],
				IsOpen:   true,
			})
		}
	}

	// 查找结束标签
	closePattern := fmt.Sprintf(`</%s>`, regexp.QuoteMeta(tag))
	closeRe := regexp.MustCompile(closePattern)
	closeMatches := closeRe.FindAllStringIndex(content, -1)

	for _, match := range closeMatches {
		tags = append(tags, &TagInfo{
			Name:     tag,
			Position: match[0],
			IsOpen:   false,
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

// addClosingTags 添加结束标签
func (sxf *SmartXMLFixer) addClosingTags(content, tag string, count int) string {
	closeTags := strings.Repeat(fmt.Sprintf("</%s>", tag), count)

	// 在</w:body>之前添加结束标签
	bodyEndPos := strings.LastIndex(content, "</w:body>")
	if bodyEndPos != -1 {
		content = content[:bodyEndPos] + closeTags + content[bodyEndPos:]
		fmt.Printf("已在</w:body>之前添加 %d 个 </%s> 标签\n", count, tag)
	} else {
		// 如果找不到</w:body>，在</w:document>之前添加
		docEndPos := strings.LastIndex(content, "</w:document>")
		if docEndPos != -1 {
			content = content[:docEndPos] + closeTags + content[docEndPos:]
			fmt.Printf("已在</w:document>之前添加 %d 个 </%s> 标签\n", count, tag)
		}
	}

	return content
}

// copyFile 复制文件到ZIP
func (sxf *SmartXMLFixer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
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
		fmt.Println("用法: go run smart_xml_fixer.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	fixer := NewSmartXMLFixer(inputFile)
	if err := fixer.FixXMLStructure(outputFile); err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("智能修复完成!")
}