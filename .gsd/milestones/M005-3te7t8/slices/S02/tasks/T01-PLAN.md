---
estimated_steps: 54
estimated_files: 8
skills_used: []
---

# T01: 实现 FillCommand + DI 注册

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

## Inputs

- `Cli/CliRunner.cs`
- `Cli/Commands/InspectCommand.cs`
- `Cli/JsonlOutput.cs`
- `Services/Interfaces/IDocumentProcessor.cs`
- `Services/Interfaces/IExcelDataParser.cs`
- `Models/ProcessRequest.cs`
- `App.xaml.cs`

## Expected Output

- `Cli/Commands/FillCommand.cs`
- `App.xaml.cs`

## Verification

dotnet build
