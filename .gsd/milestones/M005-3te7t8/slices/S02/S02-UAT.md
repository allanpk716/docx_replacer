# S02: fill + cleanup 子命令 — UAT

**Milestone:** M005-3te7t8
**Written:** 2026-04-23T16:17:32.030Z

# UAT: S02 fill + cleanup 子命令

## 前置条件
- 项目已编译：`dotnet build` 成功
- DocuFiller.exe 存在于 `bin/Debug/net8.0-windows/` 目录
- 从 cmd 或 PowerShell 执行命令（非 `dotnet run`）

## 测试用例

### TC-01: fill --help 输出 JSONL 参数说明
```powershell
DocuFiller.exe fill --help
```
**预期**: 输出一行 JSON，type="command"，name="fill"，包含 options 数组（--template/--data/--output/--folder/--overwrite），exit code 0

### TC-02: cleanup --help 输出 JSONL 参数说明
```powershell
DocuFiller.exe cleanup --help
```
**预期**: 输出一行 JSON，type="command"，name="cleanup"，包含 options 数组（--input/--output/--folder），exit code 0

### TC-03: fill 缺少必需参数输出错误 JSONL
```powershell
DocuFiller.exe fill
```
**预期**: 输出 type="error"，code="MISSING_ARGUMENT" 的 JSONL 行，exit code 1

### TC-04: fill 文件不存在输出 FILE_NOT_FOUND
```powershell
DocuFiller.exe fill --template nonexistent.docx --data test.xlsx --output ./out
```
**预期**: 输出 type="error"，code="FILE_NOT_FOUND" 的 JSONL 行，exit code 1

### TC-05: cleanup 缺少必需参数输出错误 JSONL
```powershell
DocuFiller.exe cleanup
```
**预期**: 输出 type="error"，code="MISSING_ARGUMENT" 的 JSONL 行，exit code 1

### TC-06: cleanup 文件不存在输出 FILE_NOT_FOUND
```powershell
DocuFiller.exe cleanup --input nonexistent.docx
```
**预期**: 输出 type="error"，code="FILE_NOT_FOUND" 的 JSONL 行，exit code 1

### TC-07: 全局 --help 包含三个子命令
```powershell
DocuFiller.exe --help
```
**预期**: 输出多行 JSONL，包含 type="help"、三个 type="command"（fill/cleanup/inspect）、type="examples"，exit code 0

### TC-08: dotnet test 全部通过
```powershell
dotnet test --verbosity minimal
```
**预期**: 71 tests passed, 0 failed

### TC-09: fill 端到端填充（需模板+Excel 数据文件）
```powershell
DocuFiller.exe fill --template Templates\template.docx --data data.xlsx --output ./output
```
**预期**: 输出 progress JSONL 行（处理进度）+ result JSONL 行（每个生成文件路径）+ summary JSONL 行（total/success/failed/duration），exit code 0，输出目录中生成填充后的文档

### TC-10: cleanup 端到端清理（需含批注/控件的文档）
```powershell
DocuFiller.exe cleanup --input document_with_comments.docx
```
**预期**: 输出 result JSONL 行（commentsRemoved/controlsUnwrapped/outputPath）+ summary JSONL 行，exit code 0

## 边界场景
- fill --template 无效文件（非 .docx）: 应返回 FILL_ERROR
- cleanup --input 指向目录: 应以文件夹模式处理
- cleanup --input file.docx --output ./cleaned: 应在指定目录生成清理后文件
