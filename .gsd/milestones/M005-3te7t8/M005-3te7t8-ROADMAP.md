# M005-3te7t8: CLI 接口 - LLM Agent 集成

**Vision:** 为 DocuFiller 新增命令行接口（CLI），让第三方 LLM agent 能无需 GUI 界面直接调用核心功能。CLI 全部使用 JSONL 格式输出（包括 --help），专为机器消费设计。三个子命令覆盖完整工作流：fill（Excel 数据批量填充模板）、cleanup（清理批注和内容控件）、inspect（查询模板控件信息）。无参数时正常启动 WPF GUI，有参数时走 CLI 路径不弹窗。

## Success Criteria

- DocuFiller.exe --help 输出 JSONL 格式的完整帮助文档，包含三个子命令的参数和使用示例
- DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> 成功生成填充后的文档
- DocuFiller.exe inspect --template <path> 输出模板控件列表 JSONL
- DocuFiller.exe cleanup --input <path> 成功清理文档
- dotnet test 全部通过
- 无参数启动时 WPF GUI 正常工作

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: dotnet build 成功后，执行 DocuFiller.exe --help 输出 JSONL 格式帮助文档；执行 DocuFiller.exe inspect --template <path> 输出模板控件列表 JSONL；无参数启动 GUI 正常

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 执行 DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> 成功生成填充文档；执行 DocuFiller.exe cleanup --input <path> 成功清理文档；三个子命令均通过 --help 输出 JSONL 参数说明

- [x] **S03: S03** `risk:low` `depends:[]`
  > After this: dotnet test 全部通过（含新增 CLI 测试）；CLAUDE.md 和 README.md 包含 CLI 使用说明和 JSONL 输出格式文档

## Boundary Map

## 边界图

```
外部调用者 (LLM Agent / cmd / PowerShell)
    │
    ▼
DocuFiller.exe [command] [options]
    │
    ├── (无参数) ──▶ App.OnStartup ──▶ GUI 路径 ──▶ MainWindow
    │
    └── (有参数) ──▶ App.OnStartup ──▶ CLI 路径
                         │
                         ├── CliRunner (参数解析 + 分发)
                         │    ├── --help ──▶ JSONL 帮助输出
                         │    ├── inspect ──▶ InspectCommand ──▶ IDocumentProcessor.GetContentControlsAsync()
                         │    ├── fill ──▶ FillCommand ──▶ IDocumentProcessor.ProcessDocumentsAsync() / ProcessFolderAsync()
                         │    │                                    + IExcelDataParser.ParseExcelFileAsync()
                         │    └── cleanup ──▶ CleanupCommand ──▶ IDocumentCleanupService.CleanupAsync()
                         │
                         └── ConsoleHelper (AttachConsole + FreeConsole)
```

### 跨切片边界

- **S01 → S02**: S01 建立 CliRunner 框架、JsonlOutput 格式、ConsoleHelper P/Invoke。S02 在此框架上添加 FillCommand 和 CleanupCommand。
- **S02 → S03**: S01+S02 的所有 CLI 代码是 S03 测试的测试对象。
- **S01 内部边界**: ConsoleHelper 和 JsonlOutput 是独立工具类；InspectCommand 通过 DI 容器调用 IDocumentProcessor。
