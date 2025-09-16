package main

import (
	"bufio"
	"fmt"
	"os"
	"strings"
)

const (
	versionNew = "2.0.0-new-lib"
)

// 使用新库的主程序
func main() {
	fmt.Printf("docx_replacer 高级版本 %s\n", versionNew)
	fmt.Println("专门解决表格中关键词被XML标签分割的问题")
	fmt.Println("请按照提示，将相应的文件或文件夹拖拽到窗口中，然后按 Enter 键。")
	fmt.Println("-----------------------------------------------------------------")
	fmt.Println("高级替换模式选择:")
	fmt.Println("1. 高级批量替换模式")
	fmt.Println("2. 高级单文件测试模式")
	fmt.Print("请选择模式 (1/2，直接回车默认为1): ")

	var choice string
	fmt.Scanln(&choice)

	switch choice {
	case "2":
		advancedSingleFileMode()
		return
	default:
		// 继续高级批量模式
	}

	advancedBatchMode()
}





// getDragDropPath 提示用户拖拽文件/文件夹并返回路径
func getDragDropPath(prompt string) string {
	reader := bufio.NewReader(os.Stdin)
	for {
		fmt.Print(prompt)
		path, _ := reader.ReadString('\n')
		path = strings.TrimSpace(path)
		if path != "" {
			// 去除windows拖拽路径时可能带有的双引号
			path = strings.Trim(path, "\"")
			// 检查路径是否存在
			if _, err := os.Stat(path); os.IsNotExist(err) {
				fmt.Printf("错误：路径 '%s' 不存在，请重新输入\n", path)
				continue
			}
			return path
		}
	}
}