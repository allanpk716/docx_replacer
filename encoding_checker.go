package main

import (
	"archive/zip"
	"fmt"
	"io"
	"os"
	"unicode/utf8"
)

// checkEncoding 检查文件编码和BOM
func checkEncoding(docxPath string) error {
	fmt.Printf("检查文件编码: %s\n", docxPath)

	// 提取document.xml的原始字节
	xmlBytes, err := extractDocumentXMLBytes(docxPath)
	if err != nil {
		return err
	}

	fmt.Printf("文件大小: %d 字节\n", len(xmlBytes))

	// 检查BOM
	checkBOM(xmlBytes)

	// 检查UTF-8编码有效性
	checkUTF8Validity(xmlBytes)

	// 检查前100个字节的内容
	checkFirstBytes(xmlBytes, 100)

	// 检查最后100个字节的内容
	checkLastBytes(xmlBytes, 100)

	return nil
}

// extractDocumentXMLBytes 提取document.xml的原始字节
func extractDocumentXMLBytes(docxPath string) ([]byte, error) {
	reader, err := zip.OpenReader(docxPath)
	if err != nil {
		return nil, err
	}
	defer reader.Close()

	for _, file := range reader.File {
		if file.Name == "word/document.xml" {
			f, err := file.Open()
			if err != nil {
				return nil, err
			}
			defer f.Close()

			return io.ReadAll(f)
		}
	}

	return nil, fmt.Errorf("未找到document.xml")
}

// checkBOM 检查字节顺序标记
func checkBOM(data []byte) {
	fmt.Println("\nBOM检查:")

	if len(data) >= 3 {
		// UTF-8 BOM: EF BB BF
		if data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF {
			fmt.Println("✓ 检测到UTF-8 BOM")
			return
		}
	}

	if len(data) >= 2 {
		// UTF-16 BE BOM: FE FF
		if data[0] == 0xFE && data[1] == 0xFF {
			fmt.Println("✓ 检测到UTF-16 BE BOM")
			return
		}
		// UTF-16 LE BOM: FF FE
		if data[0] == 0xFF && data[1] == 0xFE {
			fmt.Println("✓ 检测到UTF-16 LE BOM")
			return
		}
	}

	fmt.Println("❌ 未检测到BOM")
}

// checkUTF8Validity 检查UTF-8编码有效性
func checkUTF8Validity(data []byte) {
	fmt.Println("\nUTF-8编码检查:")

	if utf8.Valid(data) {
		fmt.Println("✓ UTF-8编码有效")
	} else {
		fmt.Println("❌ UTF-8编码无效")
		
		// 查找第一个无效字节的位置
		for i, b := range data {
			if !utf8.ValidString(string(data[i:i+1])) {
				fmt.Printf("第一个无效字节位置: %d, 值: 0x%02X\n", i, b)
				break
			}
		}
	}
}

// checkFirstBytes 检查前N个字节
func checkFirstBytes(data []byte, n int) {
	fmt.Printf("\n前%d个字节:\n", n)

	if len(data) < n {
		n = len(data)
	}

	// 十六进制显示
	fmt.Print("十六进制: ")
	for i := 0; i < n; i++ {
		fmt.Printf("%02X ", data[i])
		if (i+1)%16 == 0 {
			fmt.Print("\n          ")
		}
	}
	fmt.Println()

	// ASCII显示（可打印字符）
	fmt.Print("ASCII: ")
	for i := 0; i < n; i++ {
		if data[i] >= 32 && data[i] <= 126 {
			fmt.Printf("%c", data[i])
		} else {
			fmt.Print(".")
		}
	}
	fmt.Println()
}

// checkLastBytes 检查最后N个字节
func checkLastBytes(data []byte, n int) {
	fmt.Printf("\n最后%d个字节:\n", n)

	start := len(data) - n
	if start < 0 {
		start = 0
		n = len(data)
	}

	// 十六进制显示
	fmt.Print("十六进制: ")
	for i := start; i < len(data); i++ {
		fmt.Printf("%02X ", data[i])
		if (i-start+1)%16 == 0 {
			fmt.Print("\n          ")
		}
	}
	fmt.Println()

	// ASCII显示（可打印字符）
	fmt.Print("ASCII: ")
	for i := start; i < len(data); i++ {
		if data[i] >= 32 && data[i] <= 126 {
			fmt.Printf("%c", data[i])
		} else {
			fmt.Print(".")
		}
	}
	fmt.Println()
}

// compareEncodings 对比两个文件的编码
func compareEncodings(file1, file2 string) error {
	fmt.Printf("\n=== 对比编码差异 ===\n")
	fmt.Printf("文件1: %s\n", file1)
	fmt.Printf("文件2: %s\n", file2)

	// 提取两个文件的字节
	data1, err := extractDocumentXMLBytes(file1)
	if err != nil {
		return fmt.Errorf("读取文件1失败: %v", err)
	}

	data2, err := extractDocumentXMLBytes(file2)
	if err != nil {
		return fmt.Errorf("读取文件2失败: %v", err)
	}

	fmt.Printf("\n大小对比:\n")
	fmt.Printf("文件1: %d 字节\n", len(data1))
	fmt.Printf("文件2: %d 字节\n", len(data2))
	fmt.Printf("差异: %d 字节\n", len(data2)-len(data1))

	// 查找第一个不同的字节
	minLen := len(data1)
	if len(data2) < minLen {
		minLen = len(data2)
	}

	for i := 0; i < minLen; i++ {
		if data1[i] != data2[i] {
			fmt.Printf("\n第一个差异位置: %d\n", i)
			fmt.Printf("文件1: 0x%02X ('%c')\n", data1[i], printableChar(data1[i]))
			fmt.Printf("文件2: 0x%02X ('%c')\n", data2[i], printableChar(data2[i]))
			
			// 显示差异周围的上下文
			start := i - 10
			if start < 0 {
				start = 0
			}
			end := i + 10
			if end > minLen {
				end = minLen
			}
			
			fmt.Printf("\n上下文 (位置 %d-%d):\n", start, end-1)
			fmt.Print("文件1: ")
			for j := start; j < end; j++ {
				fmt.Printf("%02X ", data1[j])
			}
			fmt.Print("\n文件2: ")
			for j := start; j < end; j++ {
				fmt.Printf("%02X ", data2[j])
			}
			fmt.Println()
			break
		}
	}

	if len(data1) == len(data2) {
		fmt.Println("\n✓ 文件内容完全相同")
	} else {
		fmt.Printf("\n❌ 文件大小不同，额外内容在较长的文件中\n")
	}

	return nil
}

// printableChar 返回可打印字符或点
func printableChar(b byte) rune {
	if b >= 32 && b <= 126 {
		return rune(b)
	}
	return '.'
}

func main() {
	if len(os.Args) < 2 {
		fmt.Println("用法:")
		fmt.Println("  检查单个文件: go run encoding_checker.go <docx文件>")
		fmt.Println("  对比两个文件: go run encoding_checker.go <文件1> <文件2>")
		os.Exit(1)
	}

	if len(os.Args) == 2 {
		// 检查单个文件
		if err := checkEncoding(os.Args[1]); err != nil {
			fmt.Printf("检查失败: %v\n", err)
			os.Exit(1)
		}
	} else if len(os.Args) == 3 {
		// 对比两个文件
		if err := compareEncodings(os.Args[1], os.Args[2]); err != nil {
			fmt.Printf("对比失败: %v\n", err)
			os.Exit(1)
		}
	}

	fmt.Println("\n检查完成")
}