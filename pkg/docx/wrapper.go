package docx

import (
	"fmt"
	"io"
	"os"

	"github.com/gomutex/godocx"
	"github.com/gomutex/godocx/docx"
)

// DocxWrapper 包装 gomutex/godocx 库
type DocxWrapper struct {
	doc      *docx.RootDoc
	filePath string
	modified bool
}

// OpenDocument 打开DOCX文档
func (dw *DocxWrapper) OpenDocument(filePath string) error {
	doc, err := godocx.OpenDocument(filePath)
	if err != nil {
		return fmt.Errorf("打开文档失败: %v", err)
	}

	dw.doc = doc
	dw.filePath = filePath
	dw.modified = false
	return nil
}

// SaveDocument 保存文档
func (dw *DocxWrapper) SaveDocument(outputPath string) error {
	if dw.doc == nil {
		return fmt.Errorf("文档未打开")
	}

	// 如果没有修改，直接复制原文件
	if !dw.modified {
		return dw.copyOriginalFile(outputPath)
	}

	// 保存修改后的文档
	err := dw.doc.SaveTo(outputPath)
	if err != nil {
		return fmt.Errorf("保存文档失败: %v", err)
	}

	return nil
}

// copyOriginalFile 复制原始文件
func (dw *DocxWrapper) copyOriginalFile(outputPath string) error {
	sourceFile, err := os.Open(dw.filePath)
	if err != nil {
		return fmt.Errorf("打开源文件失败: %v", err)
	}
	defer sourceFile.Close()

	destFile, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("创建目标文件失败: %v", err)
	}
	defer destFile.Close()

	_, err = io.Copy(destFile, sourceFile)
	if err != nil {
		return fmt.Errorf("复制文件失败: %v", err)
	}

	return nil
}

// GetParagraphs 获取所有段落
// 注意：gomutex/godocx库目前不提供直接访问现有段落的API
// 这是一个临时实现，返回空切片
func (dw *DocxWrapper) GetParagraphs() []interface{} {
	// TODO: gomutex/godocx库暂不支持读取现有文档的段落内容
	// 需要等待库的更新或使用其他方法
	return []interface{}{}
}

// ReplaceParagraphText 替换段落文本
// 注意：由于无法获取现有段落，此方法暂时无法实现
func (dw *DocxWrapper) ReplaceParagraphText(paragraph interface{}, oldText, newText string) bool {
	// TODO: 等待gomutex/godocx库支持段落文本替换
	return false
}

// GetTables 获取所有表格
// 注意：gomutex/godocx库目前不提供直接访问现有表格的API
func (dw *DocxWrapper) GetTables() []interface{} {
	// TODO: gomutex/godocx库暂不支持读取现有文档的表格内容
	return []interface{}{}
}

// ReplaceTableText 替换表格文本
func (dw *DocxWrapper) ReplaceTableText(table interface{}, oldText, newText string) bool {
	// TODO: 等待gomutex/godocx库支持表格文本替换
	return false
}

// GetTableCellText 获取表格单元格文本
func (dw *DocxWrapper) GetTableCellText(tableIndex, rowIndex, cellIndex int) string {
	// TODO: gomutex/godocx库暂不支持读取现有表格单元格内容
	return ""
}

// IsModified 检查文档是否已修改
func (dw *DocxWrapper) IsModified() bool {
	return dw.modified
}

// Close 关闭文档
func (dw *DocxWrapper) Close() error {
	if dw.doc != nil {
		return dw.doc.Close()
	}
	return nil
}