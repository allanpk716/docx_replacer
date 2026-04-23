---
estimated_steps: 11
estimated_files: 1
skills_used: []
---

# T04: 实现 --help JSONL 输出（全局 + 子命令级别）

在 CliRunner 中实现 --help 输出，格式为 JSONL：

```jsonl
{"type":"help","name":"DocuFiller","version":"1.0.0","description":"Word文档批量填充工具"}
{"type":"command","name":"fill","description":"使用Excel数据批量填充Word模板","usage":"DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> [options]","options":[{"name":"--template","required":true,"description":"模板文件路径"},{"name":"--data","required":true,"description":"Excel数据文件路径"},{"name":"--output","required":true,"description":"输出目录"},{"name":"--folder","required":false,"description":"文件夹批量模式"},{"name":"--overwrite","required":false,"description":"覆盖已存在文件"}]}
{"type":"command","name":"cleanup","description":"清理Word文档中的批注和内容控件","usage":"DocuFiller.exe cleanup --input <path> [options]","options":[{"name":"--input","required":true,"description":"文件或文件夹路径"},{"name":"--output","required":false,"description":"输出目录"},{"name":"--folder","required":false,"description":"文件夹批量模式"}]}
{"type":"command","name":"inspect","description":"查询模板中的内容控件列表","usage":"DocuFiller.exe inspect --template <path>","options":[{"name":"--template","required":true,"description":"模板文件路径"}]}
{"type":"examples","items":["DocuFiller.exe inspect --template report.docx","DocuFiller.exe fill --template report.docx --data input.xlsx --output ./output","DocuFiller.exe cleanup --input ./docs"]}
```

全局 --help/-h 或无子命令时输出完整帮助。
子命令 --help 输出该子命令的帮助行。
--version 输出版本行。

## Inputs

- `Cli/JsonlOutput.cs`

## Expected Output

- `修改后的 Cli/CliRunner.cs`

## Verification

执行 DocuFiller.exe --help 输出完整 JSONL 帮助文档，包含 fill/cleanup/inspect 三个子命令的参数和使用示例
