package cmd

import (
	"flag"
	"fmt"
	"path/filepath"
	"strings"
)

// CommandLineArgs 命令行参数结构
type CommandLineArgs struct {
	ConfigFile  string
	InputFile   string
	OutputFile  string
	InputDir    string
	OutputDir   string
	ShowVersion bool
	ShowHelp    bool
	Verbose     bool
}

// ParseCommandLineArgs 解析命令行参数
func ParseCommandLineArgs() *CommandLineArgs {
	args := &CommandLineArgs{}

	flag.StringVar(&args.ConfigFile, "config", "config.json", "配置文件路径")
	flag.StringVar(&args.InputFile, "input", "", "输入 DOCX 文件路径")
	flag.StringVar(&args.OutputFile, "output", "", "输出 DOCX 文件路径")
	flag.StringVar(&args.InputDir, "input-dir", "", "输入目录路径（批量处理）")
	flag.StringVar(&args.OutputDir, "output-dir", "", "输出目录路径（批量处理）")
	flag.BoolVar(&args.ShowVersion, "version", false, "显示版本信息")
	flag.BoolVar(&args.ShowHelp, "help", false, "显示帮助信息")
	flag.BoolVar(&args.Verbose, "verbose", false, "详细输出")

	flag.Parse()

	return args
}

// ValidateArgs 验证命令行参数
func ValidateArgs(args *CommandLineArgs) error {
	if args.ConfigFile == "" {
		return fmt.Errorf("配置文件路径不能为空")
	}

	// 检查是单文件处理还是批量处理
	hasSingleFile := args.InputFile != "" || args.OutputFile != ""
	hasBatchMode := args.InputDir != "" || args.OutputDir != ""

	if !hasSingleFile && !hasBatchMode {
		return fmt.Errorf("必须指定输入文件或输入目录")
	}

	if hasSingleFile && hasBatchMode {
		return fmt.Errorf("不能同时指定单文件和批量处理模式")
	}

	if hasSingleFile {
		if args.InputFile == "" {
			return fmt.Errorf("单文件模式下必须指定输入文件")
		}
		if args.OutputFile == "" {
			// 自动生成输出文件名
			args.OutputFile = GenerateOutputFileName(args.InputFile)
		}
	}

	if hasBatchMode {
		if args.InputDir == "" {
			return fmt.Errorf("批量模式下必须指定输入目录")
		}
		if args.OutputDir == "" {
			// 自动生成输出目录名
			args.OutputDir = args.InputDir + "_processed"
		}
	}

	return nil
}

// GenerateOutputFileName 生成输出文件名
func GenerateOutputFileName(inputFile string) string {
	ext := filepath.Ext(inputFile)
	base := strings.TrimSuffix(inputFile, ext)
	return base + "_processed" + ext
}