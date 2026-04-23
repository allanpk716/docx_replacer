---
estimated_steps: 60
estimated_files: 9
skills_used: []
---

# T02: 实现 CleanupCommand + DI 注册

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

## Inputs

- `Cli/CliRunner.cs`
- `Cli/Commands/InspectCommand.cs`
- `Cli/Commands/FillCommand.cs`
- `Cli/JsonlOutput.cs`
- `Services/Interfaces/IDocumentCleanupService.cs`
- `Models/CleanupFileItem.cs`
- `Models/InputSourceType.cs`
- `App.xaml.cs`

## Expected Output

- `Cli/Commands/CleanupCommand.cs`
- `App.xaml.cs`

## Verification

dotnet build && dotnet test
