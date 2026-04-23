---
id: S02
parent: M005-3te7t8
milestone: M005-3te7t8
provides:
  - ["FillCommand: fill 子命令实现，调用 IDocumentProcessor.ProcessDocumentsAsync + IExcelDataParser.ValidateExcelFileAsync", "CleanupCommand: cleanup 子命令实现，调用 IDocumentCleanupService.CleanupAsync（三个重载）", "DI 注册: FillCommand 和 CleanupCommand 已注册到 DI 容器", "JSONL 输出格式: fill 输出 progress/result/summary 三种类型，cleanup 输出 result/summary 两种类型"]
requires:
  []
affects:
  []
key_files:
  - ["Cli/Commands/FillCommand.cs", "Cli/Commands/CleanupCommand.cs", "App.xaml.cs"]
key_decisions:
  - ["FillCommand 通过 ProgressUpdated 事件输出进度 JSONL 行", "CleanupCommand 根据 --folder 标志和路径类型自动判断单文件/文件夹模式", "CleanupCommand 支持 --output 指定输出目录（使用 fileItem+outputDir 重载）和原地清理（使用 filePath 或 fileItem 重载）", "CleanupCommand 修复 nullable 引用警告（outputDir 使用 null-forgiving operator）"]
patterns_established:
  - ["ICliCommand 命令模式：每个子命令是独立类，实现 ICliCommand 接口，构造函数注入服务，通过 DI 注册为 singleton", "JSONL 错误输出约定：JsonlOutput.WriteError(code, message)，已知 code 包括 MISSING_ARGUMENT/FILE_NOT_FOUND/FILL_ERROR/CLEANUP_ERROR", "JSONL 结果输出约定：JsonlOutput.WriteResult(type, data) + JsonlOutput.WriteSummary(data)"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-23T16:17:32.029Z
blocker_discovered: false
---

# S02: fill + cleanup 子命令

**实现 fill 和 cleanup 两个 CLI 子命令，通过 DI 调用 IDocumentProcessor 和 IDocumentCleanupService，所有输出使用 JSONL 格式**

## What Happened

## 概述

S02 在 S01 建立的 CliRunner/ICliCommand/JsonlOutput 框架上，实现了 fill 和 cleanup 两个 CLI 子命令。两个命令均通过 DI 调用现有服务层，所有输出严格遵循 JSONL 格式。

## T01: FillCommand

创建了 `Cli/Commands/FillCommand.cs`，实现 ICliCommand 接口（CommandName = "fill"）。构造函数注入 IDocumentProcessor、IExcelDataParser 和 ILogger。ExecuteAsync 实现完整流程：必需参数验证（--template/--data/--output）→ 文件存在性检查 → 自动创建输出目录 → Excel 文件验证 → 构建 ProcessRequest 调用 ProcessDocumentsAsync → 进度事件输出 progress JSONL → 每个生成文件输出 result JSONL → 汇总输出 summary JSONL。错误时输出对应 code（MISSING_ARGUMENT/FILE_NOT_FOUND/FILL_ERROR）的错误 JSONL 并返回 exit code 1。

## T02: CleanupCommand

创建了 `Cli/Commands/CleanupCommand.cs`，实现 ICliCommand 接口（CommandName = "cleanup"）。构造函数注入 IDocumentCleanupService 和 ILogger。支持三种模式：(1) 单文件原地清理（CleanupAsync(filePath)）；(2) 文件夹原地清理（CleanupAsync(fileItem)）；(3) 指定输出目录清理（CleanupAsync(fileItem, outputDir)）。根据 --folder 标志或输入路径是否为目录自动判断模式。成功时输出 result（commentsRemoved、controlsUnwrapped、outputPath）+ summary，失败时输出 CLEANUP_ERROR。

## DI 注册

在 App.xaml.cs BuildServiceProvider 中添加了两个 singleton 注册：
- `services.AddSingleton<ICliCommand, FillCommand>()`
- `services.AddSingleton<ICliCommand, CleanupCommand>()`

## 验证结果

- `dotnet build`: 0 错误 0 警告
- `dotnet test`: 71 个测试全部通过
- `fill --help`: 正确输出 JSONL 格式参数说明
- `cleanup --help`: 正确输出 JSONL 格式参数说明
- `fill`（无参数）: MISSING_ARGUMENT 错误 JSONL + exit code 1
- `fill --template nonexistent.docx ...`: FILE_NOT_FOUND 错误 JSONL + exit code 1
- `cleanup`（无参数）: MISSING_ARGUMENT 错误 JSONL + exit code 1
- `cleanup --input nonexistent.docx`: FILE_NOT_FOUND 错误 JSONL + exit code 1
- `--help`: 全局帮助输出包含三个子命令（fill、cleanup、inspect）

## Verification

- dotnet build: 0 errors, 0 warnings
- dotnet test: 71 tests passed, 0 failed, 0 skipped
- DocuFiller.exe fill --help: 输出 JSONL 格式的 fill 子命令参数说明，exit code 0
- DocuFiller.exe cleanup --help: 输出 JSONL 格式的 cleanup 子命令参数说明，exit code 0
- DocuFiller.exe fill（无参数）: MISSING_ARGUMENT 错误 JSONL + exit code 1
- DocuFiller.exe cleanup（无参数）: MISSING_ARGUMENT 错误 JSONL + exit code 1
- DocuFiller.exe fill --template nonexistent.docx --data test.xlsx --output ./out: FILE_NOT_FOUND 错误 JSONL + exit code 1
- DocuFiller.exe cleanup --input nonexistent.docx: FILE_NOT_FOUND 错误 JSONL + exit code 1
- DocuFiller.exe --help: 全局帮助输出包含三个子命令的 JSONL 文档

## Requirements Advanced

- R012 — fill 和 cleanup 子命令实现完成，JSONL 输出格式验证通过，端到端测试路径可用

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
