# S02: fill + cleanup 子命令

**Goal:** 实现 fill 和 cleanup 两个 CLI 子命令，复用 S01 建立的 CliRunner/ICliCommand/JsonlOutput 框架，通过 DI 调用现有的 IDocumentProcessor 和 IDocumentCleanupService 服务，所有输出使用 JSONL 格式
**Demo:** 执行 DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> 成功生成填充文档；执行 DocuFiller.exe cleanup --input <path> 成功清理文档；三个子命令均通过 --help 输出 JSONL 参数说明

## Must-Haves

- DocuFiller.exe fill --template <path> --data <xlsx> --output <dir> 成功生成填充文档并输出 JSONL 结果
- DocuFiller.exe fill --help 输出 fill 子命令 JSONL 参数说明
- DocuFiller.exe cleanup --input <path> 成功清理文档并输出 JSONL 结果
- DocuFiller.exe cleanup --help 输出 cleanup 子命令 JSONL 参数说明
- 缺少必需参数时输出错误 JSONL + exit code 1
- dotnet build 编译通过，dotnet test 全部通过
- 无参数启动时 WPF GUI 正常工作（不受影响）

## Proof Level

- This slice proves: integration — 两个子命令都通过 DI 调用现有服务，需要验证端到端 JSONL 输出

## Integration Closure

- Upstream surfaces consumed: ICliCommand 接口（S01 定义）、JsonlOutput（S01 定义）、CliRunner 分发框架（S01 定义）、IDocumentProcessor.ProcessDocumentsAsync + ProcessDocumentWithFormattedDataAsync、IExcelDataParser.ParseExcelFileAsync + ValidateExcelFileAsync、IDocumentCleanupService.CleanupAsync（三个重载）
- New wiring introduced: FillCommand 和 CleanupCommand 注册到 DI 容器（App.xaml.cs BuildServiceProvider），CliRunner 已有 fill/cleanup 分发路由
- What remains before milestone is truly usable end-to-end: S03 的 CLI 测试覆盖和文档更新

## Verification

- Signals added: fill 子命令输出 progress/result/summary 三种 JSONL 类型（处理进度、单文件结果、汇总统计）；cleanup 子命令输出 result/summary 两种 JSONL 类型
- How a future agent inspects: 执行子命令检查 JSONL 输出的 status 字段和 data 内容
- Failure state exposed: 错误 JSONL 包含 code（MISSING_ARGUMENT/FILE_NOT_FOUND/FILL_ERROR/CLEANUP_ERROR）和 message 字段

## Tasks

- [x] **T01: 实现 FillCommand + DI 注册** `est:45m`
  创建 FillCommand 类实现 ICliCommand 接口，处理 fill 子命令的参数解析、验证和文档填充流程。

## Context

S01 建立的框架：
- ICliCommand 接口在 `Cli/CliRunner.cs` 中定义，要求实现 CommandName 和 ExecuteAsync(Dictionary<string,string>) 方法
- CliRunner 已有 fill 的 switch case 路由和 help 文本
- InspectCommand (`Cli/Commands/InspectCommand.cs`) 是参考实现

fill 子命令参数（已在 CliRunner help 中定义）：
- --template (必需): Word 模板文件路径
- --data (必需): Excel 数据文件路径
- --output (必需): 输出目录
- --overwrite (可选): 覆盖已存在文件

## Steps

1. 创建 `Cli/Commands/FillCommand.cs`，实现 ICliCommand 接口，CommandName = "fill"
2. 构造函数注入 IDocumentProcessor、IExcelDataParser、ILogger<FillCommand>
3. ExecuteAsync 实现流程：
   a. 验证必需参数（--template, --data, --output），缺少时用 JsonlOutput.WriteError 输出错误并返回 1
   b. 验证 --template 文件存在，不存在用 JsonlOutput.WriteError 输出 FILE_NOT_FOUND
   c. 验证 --data 文件存在，不存在用 JsonlOutput.WriteError 输出 FILE_NOT_FOUND
   d. 创建输出目录（如果不存在）
   e. 用 IExcelDataParser.ValidateExcelFileAsync 验证 Excel 文件，无效时输出错误
   f. 构建 ProcessRequest 并调用 IDocumentProcessor.ProcessDocumentsAsync
   g. 输出处理结果：成功时 JsonlOutput.WriteResult 输出每个生成文件路径，最后 JsonlOutput.WriteSummary 输出汇总
   h. 失败时 JsonlOutput.WriteError 输出错误信息
4. 在 `App.xaml.cs` 的 BuildServiceProvider 方法中添加 FillCommand 的 DI 注册：`services.AddSingleton<ICliCommand, FillCommand>();`
5. 确保代码编译通过

## Constraints
- 所有用户可见输出必须通过 JsonlOutput 辅助类
- 不要直接 Console.WriteLine（help 文本除外，那是 CliRunner 的职责）
- 不要 try-catch 吞掉异常 — 让外层 CliRunner 的全局 catch 处理
- 遵循 InspectCommand 的代码结构和命名风格
- 日志用 ILogger，不要用 Console.Write

## Must-Haves
- [ ] FillCommand 类存在且实现 ICliCommand
- [ ] --template/--data/--output 必需参数验证
- [ ] 文件存在性验证
- [ ] 调用 IDocumentProcessor.ProcessDocumentsAsync 执行填充
- [ ] JSONL 格式输出（result + summary 或 error）
- [ ] App.xaml.cs DI 注册 FillCommand
- [ ] dotnet build 通过

## Verification
- `dotnet build` 编译成功
- 手动验证 help 输出：`dotnet run -- fill --help` 输出 fill 子命令 JSONL 参数说明
- 手动验证缺少参数：`dotnet run -- fill` 输出 MISSING_ARGUMENT 错误 JSONL + exit code 1

## Inputs
- `Cli/CliRunner.cs` — ICliCommand 接口定义和 fill 分发路由
- `Cli/Commands/InspectCommand.cs` — 参考实现模式
- `Cli/JsonlOutput.cs` — JSONL 输出辅助类
- `Services/Interfaces/IDocumentProcessor.cs` — ProcessDocumentsAsync 签名
- `Services/Interfaces/IExcelDataParser.cs` — ParseExcelFileAsync/ValidateExcelFileAsync 签名
- `Models/ProcessRequest.cs` — ProcessRequest 数据模型
- `App.xaml.cs` — DI 注册位置

## Expected Output
- `Cli/Commands/FillCommand.cs` — fill 子命令处理器（新建）
- `App.xaml.cs` — 添加 FillCommand DI 注册（修改）
  - Files: `Cli/Commands/FillCommand.cs`, `Cli/Commands/InspectCommand.cs`, `Cli/JsonlOutput.cs`, `Cli/CliRunner.cs`, `Services/Interfaces/IDocumentProcessor.cs`, `Services/Interfaces/IExcelDataParser.cs`, `Models/ProcessRequest.cs`, `App.xaml.cs`
  - Verify: dotnet build

- [x] **T02: 实现 CleanupCommand + DI 注册** `est:45m`
  创建 CleanupCommand 类实现 ICliCommand 接口，处理 cleanup 子命令的参数解析、验证和文档清理流程。

## Context

S01 建立的框架同 T01。CleanupCommand 复用 T01 创建的 FillCommand 模式。

cleanup 子命令参数（已在 CliRunner help 中定义）：
- --input (必需): 文件或文件夹路径
- --output (可选): 输出目录（不指定时原地清理）
- --folder (可选): 文件夹批量模式标志

IDocumentCleanupService 提供三个 CleanupAsync 重载：
1. CleanupAsync(string filePath) — 单文件原地清理
2. CleanupAsync(CleanupFileItem fileItem) — 单文件项清理
3. CleanupAsync(CleanupFileItem fileItem, string outputDirectory) — 指定输出目录

当指定了 --output 时使用第 3 个重载，否则使用第 1 个重载。
当 --folder 标志存在或 --input 是目录时，设置 CleanupFileItem.InputType = InputSourceType.Folder。

## Steps

1. 创建 `Cli/Commands/CleanupCommand.cs`，实现 ICliCommand 接口，CommandName = "cleanup"
2. 构造函数注入 IDocumentCleanupService、ILogger<CleanupCommand>
3. ExecuteAsync 实现流程：
   a. 验证必需参数（--input），缺少时用 JsonlOutput.WriteError 输出错误并返回 1
   b. 确定输入类型：如果 --input 是目录路径或指定了 --folder 标志，则 InputType = Folder，否则 SingleFile
   c. 验证输入路径存在（文件或目录）
   d. 构建 CleanupFileItem（设置 FilePath、FileName、InputType）
   e. 如果指定了 --output：调用 CleanupAsync(fileItem, outputDirectory)
   f. 如果未指定 --output：调用 CleanupAsync(filePath)（原地清理）
   g. 成功时 JsonlOutput.WriteResult 输出清理详情（commentsRemoved、controlsUnwrapped、outputPath），然后 JsonlOutput.WriteSummary 汇总
   h. 失败时 JsonlOutput.WriteError 输出错误信息
4. 在 `App.xaml.cs` 的 BuildServiceProvider 方法中添加 CleanupCommand 的 DI 注册：`services.AddSingleton<ICliCommand, CleanupCommand>();`
5. 确保代码编译通过

## Constraints
- 所有用户可见输出必须通过 JsonlOutput 辅助类
- 不要直接 Console.WriteLine
- 遵循 FillCommand/InspectCommand 的代码结构和命名风格
- 日志用 ILogger，不要用 Console.Write
- 清理服务是 Transient 注册的（每次使用需要重新获取）

## Must-Haves
- [ ] CleanupCommand 类存在且实现 ICliCommand
- [ ] --input 必需参数验证
- [ ] 输入路径存在性验证（文件和目录两种模式）
- [ ] 调用 IDocumentCleanupService.CleanupAsync 执行清理
- [ ] 支持 --output 指定输出目录
- [ ] 支持 --folder 标志处理文件夹模式
- [ ] JSONL 格式输出（result + summary 或 error）
- [ ] App.xaml.cs DI 注册 CleanupCommand
- [ ] dotnet build 通过

## Verification
- `dotnet build` 编译成功
- `dotnet test` 全部通过
- 手动验证 help 输出：`dotnet run -- cleanup --help` 输出 cleanup 子命令 JSONL 参数说明
- 手动验证缺少参数：`dotnet run -- cleanup` 输出 MISSING_ARGUMENT 错误 JSONL + exit code 1

## Inputs
- `Cli/CliRunner.cs` — ICliCommand 接口定义和 cleanup 分发路由
- `Cli/Commands/InspectCommand.cs` — 参考实现模式
- `Cli/Commands/FillCommand.cs` — T01 创建的同类参考
- `Cli/JsonlOutput.cs` — JSONL 输出辅助类
- `Services/Interfaces/IDocumentCleanupService.cs` — CleanupAsync 签名
- `Models/CleanupFileItem.cs` — CleanupFileItem 数据模型
- `Models/InputSourceType.cs` — InputSourceType 枚举
- `App.xaml.cs` — DI 注册位置

## Expected Output
- `Cli/Commands/CleanupCommand.cs` — cleanup 子命令处理器（新建）
- `App.xaml.cs` — 添加 CleanupCommand DI 注册（修改）
  - Files: `Cli/Commands/CleanupCommand.cs`, `Cli/Commands/InspectCommand.cs`, `Cli/Commands/FillCommand.cs`, `Cli/JsonlOutput.cs`, `Cli/CliRunner.cs`, `Services/Interfaces/IDocumentCleanupService.cs`, `Models/CleanupFileItem.cs`, `Models/InputSourceType.cs`, `App.xaml.cs`
  - Verify: dotnet build && dotnet test

## Files Likely Touched

- Cli/Commands/FillCommand.cs
- Cli/Commands/InspectCommand.cs
- Cli/JsonlOutput.cs
- Cli/CliRunner.cs
- Services/Interfaces/IDocumentProcessor.cs
- Services/Interfaces/IExcelDataParser.cs
- Models/ProcessRequest.cs
- App.xaml.cs
- Cli/Commands/CleanupCommand.cs
- Services/Interfaces/IDocumentCleanupService.cs
- Models/CleanupFileItem.cs
- Models/InputSourceType.cs
