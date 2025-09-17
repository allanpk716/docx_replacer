package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// WordStructureFixer Word结构修复器
type WordStructureFixer struct {
	filePath string
}

// NewWordStructureFixer 创建Word结构修复器
func NewWordStructureFixer(filePath string) *WordStructureFixer {
	return &WordStructureFixer{
		filePath: filePath,
	}
}

// FixXMLStructure 修复XML结构
func (wsf *WordStructureFixer) FixXMLStructure(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(wsf.filePath)
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
			fixedContent, err := wsf.fixDocumentXML(file)
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
			if err := wsf.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Println("Word结构修复完成!")
	return nil
}

// fixDocumentXML 修复document.xml文件
func (wsf *WordStructureFixer) fixDocumentXML(file *zip.File) (string, error) {
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

	// 使用Word特定的修复逻辑
	xmlContent = wsf.fixWordSpecificTags(xmlContent)

	return xmlContent, nil
}

// fixWordSpecificTags 修复Word特定标签
func (wsf *WordStructureFixer) fixWordSpecificTags(content string) string {
	// 首先分析当前的标签情况
	wsf.analyzeTagCounts(content)

	// 找到</w:body>的位置
	bodyEndPos := strings.LastIndex(content, "</w:body>")
	if bodyEndPos == -1 {
		fmt.Println("未找到</w:body>标签")
		return content
	}

	// 只修复最基本的文本相关标签：w:t, w:r, w:p
	// 按照Word文档的层次结构：p包含r，r包含t

	bodyContent := content[:bodyEndPos]
	afterBody := content[bodyEndPos:]

	// 统计各种标签的数量
	tCounts := wsf.countTags(bodyContent, "w:t")
	rCounts := wsf.countTags(bodyContent, "w:r")
	pCounts := wsf.countTags(bodyContent, "w:p")

	fmt.Printf("当前标签统计:\n")
	fmt.Printf("w:t - 开始: %d, 结束: %d, 自闭合: %d\n", tCounts.Open, tCounts.Close, tCounts.SelfClosing)
	fmt.Printf("w:r - 开始: %d, 结束: %d, 自闭合: %d\n", rCounts.Open, rCounts.Close, rCounts.SelfClosing)
	fmt.Printf("w:p - 开始: %d, 结束: %d, 自闭合: %d\n", pCounts.Open, pCounts.Close, pCounts.SelfClosing)

	// 计算需要添加的结束标签数量
	tNeed := (tCounts.Open - tCounts.SelfClosing) - tCounts.Close
	rNeed := (rCounts.Open - rCounts.SelfClosing) - rCounts.Close
	pNeed := (pCounts.Open - pCounts.SelfClosing) - pCounts.Close

	fmt.Printf("需要添加的结束标签:\n")
	fmt.Printf("</w:t>: %d 个\n", tNeed)
	fmt.Printf("</w:r>: %d 个\n", rNeed)
	fmt.Printf("</w:p>: %d 个\n", pNeed)

	// 按照正确的嵌套顺序添加结束标签：先t，再r，最后p
	closeTags := ""
	if tNeed > 0 {
		closeTags += strings.Repeat("</w:t>", tNeed)
	}
	if rNeed > 0 {
		closeTags += strings.Repeat("</w:r>", rNeed)
	}
	if pNeed > 0 {
		closeTags += strings.Repeat("</w:p>", pNeed)
	}

	if closeTags != "" {
		fmt.Printf("在</w:body>前添加: %s\n", closeTags)
		return bodyContent + closeTags + afterBody
	}

	fmt.Println("所有标签都已正确闭合")
	return content
}

// TagCounts 标签计数
type TagCounts struct {
	Open        int
	Close       int
	SelfClosing int
}

// countTags 统计标签数量
func (wsf *WordStructureFixer) countTags(content, tagName string) TagCounts {
	counts := TagCounts{}

	// 统计开始标签（包括自闭合）
	openPattern := fmt.Sprintf("<%s[^>]*>", tagName)
	openRe := regexp.MustCompile(openPattern)
	openMatches := openRe.FindAllString(content, -1)
	counts.Open = len(openMatches)

	// 统计结束标签
	closePattern := fmt.Sprintf("</%s>", tagName)
	closeRe := regexp.MustCompile(closePattern)
	closeMatches := closeRe.FindAllString(content, -1)
	counts.Close = len(closeMatches)

	// 统计自闭合标签
	selfClosingPattern := fmt.Sprintf("<%s[^>]*/>", tagName)
	selfClosingRe := regexp.MustCompile(selfClosingPattern)
	selfClosingMatches := selfClosingRe.FindAllString(content, -1)
	counts.SelfClosing = len(selfClosingMatches)

	return counts
}

// analyzeTagCounts 分析标签计数
func (wsf *WordStructureFixer) analyzeTagCounts(content string) {
	fmt.Println("\n=== 标签分析 ===")

	// 分析主要的Word标签
	tags := []string{"w:document", "w:body", "w:p", "w:r", "w:t", "w:pPr", "w:rPr"}

	for _, tag := range tags {
		counts := wsf.countTags(content, tag)
		balance := (counts.Open - counts.SelfClosing) - counts.Close
		fmt.Printf("%s: 开始=%d, 结束=%d, 自闭合=%d, 平衡=%d\n", 
			tag, counts.Open, counts.Close, counts.SelfClosing, balance)
	}

	fmt.Println("=== 分析完成 ===\n")
}

// copyFile 复制文件到ZIP
func (wsf *WordStructureFixer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
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
		fmt.Println("用法: go run word_structure_fixer.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	fixer := NewWordStructureFixer(inputFile)
	if err := fixer.FixXMLStructure(outputFile); err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("Word结构修复完成!")
}