package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"
)

// mainAdvanced 高级替换模式的主程序
func mainAdvanced() {
	fmt.Printf("docx_replacer 高级版本 %s\n", versionNew)
	fmt.Println("专门解决表格中关键词被XML标签分割的问题")
	fmt.Println("-----------------------------------------------------------------")
	fmt.Println("高级模式选择:")
	fmt.Println("1. 高级批量替换模式（处理分割关键词）")
	fmt.Println("2. 高级单文件测试模式")
	fmt.Println("3. 对比测试模式（普通 vs 高级）")
	fmt.Println("4. 返回主菜单")
	fmt.Print("请选择模式 (1/2/3/4): ")

	var choice string
	fmt.Scanln(&choice)

	switch choice {
	case "1":
		advancedBatchMode()
	case "2":
		advancedSingleFileMode()
	case "3":
		comparisonMode()
	case "4":
		return
	default:
		fmt.Println("无效选择，返回主菜单")
	}
}

// advancedBatchMode 高级批量替换模式
func advancedBatchMode() {
	fmt.Println("=== 高级批量替换模式 ===")
	fmt.Println("此模式专门处理表格中被XML标签分割的关键词")

	// 获取配置文件路径
	configPath := getDragDropPath("请拖拽 config.json 配置文件到此处，然后按 Enter：")

	// 获取输入文件夹路径
	inputPath := getDragDropPath("请拖拽包含 Word 文档的输入文件夹到此处，然后按 Enter：")

	// 获取输出文件夹路径
	outputPath := getDragDropPath("请拖拽用于存放结果的输出文件夹到此处，然后按 Enter：")

	// 加载配置
	config, err := LoadConfig(configPath)
	if err != nil {
		log.Fatalf("加载配置失败: %v", err)
	}

	verbose := true

	// 创建高级批处理器
	processor := NewAdvancedBatchProcessor(config, inputPath, outputPath, verbose)

	// 执行批量处理
	err = processor.ProcessAll()
	if err != nil {
		log.Fatalf("高级批量处理失败: %v", err)
	}

	fmt.Println("高级批量替换完成！")
	fmt.Println("按 Enter 键继续...")
	bufio.NewReader(os.Stdin).ReadBytes('\n')
}

// advancedSingleFileMode 高级单文件测试模式
func advancedSingleFileMode() {
	fmt.Println("=== 高级单文件测试模式 ===")

	// 获取输入文件路径
	inputPath := getDragDropPath("请拖拽要处理的 Word 文档到此处，然后按 Enter：")

	// 获取配置文件路径
	configPath := getDragDropPath("请拖拽 config.json 配置文件到此处，然后按 Enter：")

	// 加载配置
	config, err := LoadConfig(configPath)
	if err != nil {
		log.Fatalf("加载配置失败: %v", err)
	}

	fmt.Printf("处理文件: %s\n", inputPath)

	// 创建高级处理器
	processor, err := NewAdvancedDocxProcessor(inputPath)
	if err != nil {
		log.Fatalf("创建高级处理器失败: %v", err)
	}
	defer processor.Close()

	// 获取替换映射
	replacementMap := config.GetReplacementMap()
	
	// 调试原始内容
	keywords := make([]string, 0, len(replacementMap))
	for key := range replacementMap {
		keywords = append(keywords, key)
	}
	processor.DebugContent(keywords)

	// 执行高级替换（不自动添加#号，因为配置文件中的关键词已经包含#号）
	err = processor.TableAwareReplace(replacementMap, true, false)
	if err != nil {
		log.Fatalf("高级替换失败: %v", err)
	}

	// 生成输出文件名
	dir := filepath.Dir(inputPath)
	filename := filepath.Base(inputPath)
	ext := filepath.Ext(filename)
	name := strings.TrimSuffix(filename, ext)
	outputFile := filepath.Join(dir, name+"_advanced_output"+ext)

	// 保存结果
	err = processor.SaveAs(outputFile)
	if err != nil {
		log.Fatalf("保存文件失败: %v", err)
	}

	// 显示结果
	count := processor.GetReplacementCount()
	fmt.Printf("\n=== 高级替换结果 ===\n")
	for key, c := range count {
		fmt.Printf("关键词 '%s': 替换了 %d 次\n", key, c)
	}

	fmt.Printf("\n输出文件: %s\n", outputFile)
	fmt.Println("高级单文件处理完成！")
	fmt.Println("按 Enter 键继续...")
	bufio.NewReader(os.Stdin).ReadBytes('\n')
}

// comparisonMode 对比测试模式
func comparisonMode() {
	fmt.Println("=== 对比测试模式 ===")
	fmt.Println("此模式将同时使用普通替换和高级替换处理同一个文件，便于对比效果")

	// 获取输入文件路径
	inputPath := getDragDropPath("请拖拽要对比测试的 Word 文档到此处，然后按 Enter：")

	// 获取配置文件路径
	configPath := getDragDropPath("请拖拽 config.json 配置文件到此处，然后按 Enter：")

	// 加载配置
	config, err := LoadConfig(configPath)
	if err != nil {
		log.Fatalf("加载配置失败: %v", err)
	}

	fmt.Printf("对比测试文件: %s\n", inputPath)

	// 1. 普通处理器测试
	fmt.Println("\n=== 普通处理器测试 ===")
	normalProcessor, err := NewGoDocxProcessorFromFile(inputPath)
	if err != nil {
		log.Fatalf("创建普通处理器失败: %v", err)
	}
	defer normalProcessor.Close()

	replacementMap := config.GetReplacementMap()
	err = normalProcessor.ReplaceKeywordsWithOptions(replacementMap, true, true)
	if err != nil {
		log.Fatalf("普通替换失败: %v", err)
	}

	// 保存普通处理器结果
	dir := filepath.Dir(inputPath)
	filename := filepath.Base(inputPath)
	ext := filepath.Ext(filename)
	name := strings.TrimSuffix(filename, ext)
	normalOutput := filepath.Join(dir, name+"_normal_output"+ext)

	err = normalProcessor.SaveAs(normalOutput)
	if err != nil {
		log.Fatalf("保存普通处理器结果失败: %v", err)
	}

	normalCount := normalProcessor.GetReplacementCount()

	// 2. 高级处理器测试
	fmt.Println("\n=== 高级处理器测试 ===")
	advancedProcessor, err := NewAdvancedDocxProcessor(inputPath)
	if err != nil {
		log.Fatalf("创建高级处理器失败: %v", err)
	}
	defer advancedProcessor.Close()

	// 获取替换映射
	replacementMap2 := config.GetReplacementMap()
	
	// 调试内容
	keywords := make([]string, 0, len(replacementMap2))
	for key := range replacementMap2 {
		keywords = append(keywords, key)
	}
	advancedProcessor.DebugContent(keywords)

	err = advancedProcessor.TableAwareReplace(replacementMap2, true, true)
	if err != nil {
		log.Fatalf("高级替换失败: %v", err)
	}

	// 保存高级处理器结果
	advancedOutput := filepath.Join(dir, name+"_advanced_output"+ext)

	err = advancedProcessor.SaveAs(advancedOutput)
	if err != nil {
		log.Fatalf("保存高级处理器结果失败: %v", err)
	}

	advancedCount := advancedProcessor.GetReplacementCount()

	// 3. 对比结果
	fmt.Println("\n=== 对比结果 ===")
	fmt.Printf("%-20s %-15s %-15s %-10s\n", "关键词", "普通处理器", "高级处理器", "差异")
	fmt.Println(strings.Repeat("-", 70))

	for key := range replacementMap2 {
		normalC := normalCount[key]
		advancedC := advancedCount[key]
		diff := advancedC - normalC

		status := "="
		if diff > 0 {
			status = fmt.Sprintf("+%d", diff)
		} else if diff < 0 {
			status = fmt.Sprintf("%d", diff)
		}

		fmt.Printf("%-20s %-15d %-15d %-10s\n", key, normalC, advancedC, status)
	}

	fmt.Printf("\n输出文件:\n")
	fmt.Printf("普通处理器: %s\n", normalOutput)
	fmt.Printf("高级处理器: %s\n", advancedOutput)

	fmt.Println("\n对比测试完成！请手动检查两个输出文件的差异")
	fmt.Println("按 Enter 键继续...")
	bufio.NewReader(os.Stdin).ReadBytes('\n')
}