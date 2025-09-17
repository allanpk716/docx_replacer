package main

import (
	"archive/zip"
	"fmt"
	"os"
	"path/filepath"
)

func main() {
	if len(os.Args) != 2 {
		fmt.Println("用法: go run simple_docx_creator.go <输出文件路径>")
		os.Exit(1)
	}

	outputPath := os.Args[1]
	err := createSimpleDocx(outputPath)
	if err != nil {
		fmt.Printf("创建DOCX文件失败: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("成功创建DOCX文件: %s\n", outputPath)
}

func createSimpleDocx(outputPath string) error {
	// 确保输出目录存在
	dir := filepath.Dir(outputPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("创建目录失败: %w", err)
	}

	// 创建ZIP文件
	file, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("创建文件失败: %w", err)
	}
	defer file.Close()

	zipWriter := zip.NewWriter(file)
	defer zipWriter.Close()

	// 添加[Content_Types].xml
	contentTypes := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>`

	if err := addFileToZip(zipWriter, "[Content_Types].xml", contentTypes); err != nil {
		return err
	}

	// 添加_rels/.rels
	rels := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>`

	if err := addFileToZip(zipWriter, "_rels/.rels", rels); err != nil {
		return err
	}

	// 添加word/document.xml
	documentXML := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
	<w:body>
		<w:p>
			<w:r>
				<w:t>这是一个测试文档，包含关键词占位符：</w:t>
			</w:r>
		</w:p>
		<w:p>
			<w:r>
				<w:t>公司名称：#company_name#</w:t>
			</w:r>
		</w:p>
		<w:p>
			<w:r>
				<w:t>地区：#region#</w:t>
			</w:r>
		</w:p>
		<w:sectPr>
			<w:pgSz w:w="11906" w:h="16838"/>
			<w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/>
			<w:cols w:space="708"/>
			<w:docGrid w:linePitch="360"/>
		</w:sectPr>
	</w:body>
</w:document>`

	// 添加word/_rels/document.xml.rels
	documentRels := `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
</Relationships>`

	if err := addFileToZip(zipWriter, "word/document.xml", documentXML); err != nil {
		return err
	}

	// 添加word/_rels/document.xml.rels
	if err := addFileToZip(zipWriter, "word/_rels/document.xml.rels", documentRels); err != nil {
		return err
	}

	return nil
}

func addFileToZip(zipWriter *zip.Writer, filename, content string) error {
	writer, err := zipWriter.Create(filename)
	if err != nil {
		return fmt.Errorf("创建ZIP条目失败 %s: %w", filename, err)
	}

	_, err = writer.Write([]byte(content))
	if err != nil {
		return fmt.Errorf("写入ZIP条目失败 %s: %w", filename, err)
	}

	return nil
}