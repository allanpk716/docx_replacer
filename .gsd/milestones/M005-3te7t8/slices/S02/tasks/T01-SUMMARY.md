---
id: T01
parent: S02
milestone: M005-3te7t8
key_files:
  - Cli/Commands/FillCommand.cs
  - App.xaml.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T16:12:20.703Z
blocker_discovered: false
---

# T01: 实现 FillCommand 类并注册到 DI 容器，支持 fill 子命令的参数验证、文件存在性检查、Excel 验证、文档填充及 JSONL 格式输出

**实现 FillCommand 类并注册到 DI 容器，支持 fill 子命令的参数验证、文件存在性检查、Excel 验证、文档填充及 JSONL 格式输出**

## What Happened

创建了 `Cli/Commands/FillCommand.cs`，实现 ICliCommand 接口，CommandName = "fill"。构造函数注入 IDocumentProcessor、IExcelDataParser 和 ILogger。ExecuteAsync 实现了完整的参数验证流程：检查 --template/--data/--output 必需参数（缺少时输出 MISSING_ARGUMENT 错误），验证文件存在性（不存在时输出 FILE_NOT_FOUND 错误），自动创建输出目录，调用 IExcelDataParser.ValidateExcelFileAsync 验证 Excel 文件，然后构建 ProcessRequest 调用 IDocumentProcessor.ProcessDocumentsAsync 执行填充。进度通过 ProgressUpdated 事件输出 progress 类型的 JSONL 行，完成后输出每个生成文件的 result 行和汇总 summary 行。

在 App.xaml.cs 的 BuildServiceProvider 方法中添加了 `services.AddSingleton<ICliCommand, FillCommand>()` DI 注册。

构建通过（0 错误 0 警告）。手动验证了三个场景：(1) `fill --help` 正确输出 fill 子命令的 JSONL 参数说明；(2) `fill` 无参数时输出 MISSING_ARGUMENT 错误 JSONL 并返回 exit code 1；(3) `fill --template nonexistent.docx --data test.xlsx --output ./out` 输出 FILE_NOT_FOUND 错误并返回 exit code 1。

## Verification

dotnet build 编译成功（0 错误 0 警告）。
手动验证 fill --help：输出 JSONL 格式的 fill 子命令参数说明，包含 --template/--data/--output/--folder/--overwrite 选项。
手动验证 fill 无参数：输出 {"type":"error","data":{"message":"缺少必需参数: --template <path>","code":"MISSING_ARGUMENT"}} 并 exit code 1。
手动验证 fill 文件不存在：输出 {"type":"error","data":{"message":"模板文件不存在: nonexistent.docx","code":"FILE_NOT_FOUND"}} 并 exit code 1。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 3140ms |
| 2 | `DocuFiller.exe fill --help` | 0 | ✅ pass | 1500ms |
| 3 | `DocuFiller.exe fill (no args)` | 1 | ✅ pass | 1500ms |
| 4 | `DocuFiller.exe fill --template nonexistent.docx --data test.xlsx --output ./out` | 1 | ✅ pass | 1500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Cli/Commands/FillCommand.cs`
- `App.xaml.cs`
