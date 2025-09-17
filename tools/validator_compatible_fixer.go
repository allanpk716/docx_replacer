package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"regexp"
	"strings"
)

// ValidatorCompatibleFixer 与验证器兼容的修复器
type ValidatorCompatibleFixer struct {
	filePath string
}

// NewValidatorCompatibleFixer 创建与验证器兼容的修复器
func NewValidatorCompatibleFixer(filePath string) *ValidatorCompatibleFixer {
	return &ValidatorCompatibleFixer{
		filePath: filePath,
	}
}

// FixXMLStructure 修复XML结构
func (vcf *ValidatorCompatibleFixer) FixXMLStructure(outputPath string) error {
	// 打开原始DOCX文件
	reader, err := zip.OpenReader(vcf.filePath)
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
			fixedContent, err := vcf.fixDocumentXML(file)
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
			if err := vcf.copyFile(file, zipWriter); err != nil {
				return fmt.Errorf("复制文件%s失败: %v", file.Name, err)
			}
		}
	}

	fmt.Println("与验证器兼容的XML结构修复完成!")
	return nil
}

// fixDocumentXML 修复document.xml文件
func (vcf *ValidatorCompatibleFixer) fixDocumentXML(file *zip.File) (string, error) {
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

	// 使用与验证器相同的逻辑修复XML标签
	xmlContent = vcf.fixXMLTagsLikeValidator(xmlContent)

	return xmlContent, nil
}

// fixXMLTagsLikeValidator 使用与验证器相同的逻辑修复XML标签
func (vcf *ValidatorCompatibleFixer) fixXMLTagsLikeValidator(content string) string {
	// 检查常见的Word标签是否正确闭合（与验证器相同的标签列表）
	commonTags := []string{"w:p", "w:r", "w:t", "w:pPr", "w:rPr"}

	for _, tag := range commonTags {
		content = vcf.fixTagLikeValidator(content, tag)
	}

	return content
}

// fixTagLikeValidator 使用与验证器相同的逻辑修复特定标签
func (vcf *ValidatorCompatibleFixer) fixTagLikeValidator(content, tag string) string {
	// 使用与验证器完全相同的正则表达式
	openPattern := fmt.Sprintf("<%s[^>]*>", tag)
	closePattern := fmt.Sprintf("</%s>", tag)

	openRe := regexp.MustCompile(openPattern)
	closeRe := regexp.MustCompile(closePattern)

	openMatches := openRe.FindAllString(content, -1)
	closeMatches := closeRe.FindAllString(content, -1)

	// 过滤自闭合标签（与验证器相同的逻辑）
	selfClosingPattern := fmt.Sprintf("<%s[^>]*/>", tag)
	selfClosingRe := regexp.MustCompile(selfClosingPattern)
	selfClosingMatches := selfClosingRe.FindAllString(content, -1)

	openCount := len(openMatches) - len(selfClosingMatches)
	closeCount := len(closeMatches)

	fmt.Printf("标签 <%s>: 开始标签 %d 个，结束标签 %d 个，自闭合标签 %d 个\n", 
		tag, len(openMatches), closeCount, len(selfClosingMatches))

	if openCount != closeCount {
		missingCount := openCount - closeCount
		if missingCount > 0 {
			fmt.Printf("标签 <%s>: 需要添加 %d 个结束标签\n", tag, missingCount)

			// 添加缺失的结束标签
			closeTags := strings.Repeat(fmt.Sprintf("</%s>", tag), missingCount)

			// 在</w:body>之前添加结束标签
			bodyEndPos := strings.LastIndex(content, "</w:body>")
			if bodyEndPos != -1 {
				content = content[:bodyEndPos] + closeTags + content[bodyEndPos:]
				fmt.Printf("已在</w:body>之前添加 %d 个 </%s> 标签\n", missingCount, tag)
			} else {
				// 如果找不到</w:body>，在</w:document>之前添加
				docEndPos := strings.LastIndex(content, "</w:document>")
				if docEndPos != -1 {
					content = content[:docEndPos] + closeTags + content[docEndPos:]
					fmt.Printf("已在</w:document>之前添加 %d 个 </%s> 标签\n", missingCount, tag)
				}
			}
		} else {
			fmt.Printf("标签 <%s>: 结束标签过多，需要手动检查\n", tag)
		}
	} else {
		fmt.Printf("标签 <%s>: 标签匹配正确\n", tag)
	}

	return content
}

// copyFile 复制文件到ZIP
func (vcf *ValidatorCompatibleFixer) copyFile(file *zip.File, zipWriter *zip.Writer) error {
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
		fmt.Println("用法: go run validator_compatible_fixer.go <输入文件> <输出文件>")
		os.Exit(1)
	}

	inputFile := os.Args[1]
	outputFile := os.Args[2]

	fixer := NewValidatorCompatibleFixer(inputFile)
	if err := fixer.FixXMLStructure(outputFile); err != nil {
		fmt.Printf("修复失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Println("与验证器兼容的修复完成!")
}