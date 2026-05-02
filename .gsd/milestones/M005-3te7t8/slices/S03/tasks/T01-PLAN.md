---
estimated_steps: 29
estimated_files: 4
skills_used: []
---

# T01: 编写 CLI 单元测试

为 CliRunner、三个子命令（fill/cleanup/inspect）和 JsonlOutput 编写单元测试。

测试范围：
1. **CliRunner 路由测试**（CliRunnerTests.cs）：
   - 空参数 → 返回 -1（GUI 模式）
   - --help → 输出 JSONL 全局帮助
   - --version → 输出版本 JSONL
   - 未知子命令 → UNKNOWN_COMMAND 错误 JSONL
   - fill --help → 输出 fill 子命令帮助 JSONL
   - 子命令分发到正确的 ICliCommand 实现

2. **命令参数验证测试**（CommandValidationTests.cs）：
   - FillCommand：缺少 --template → MISSING_ARGUMENT
   - FillCommand：缺少 --data → MISSING_ARGUMENT
   - FillCommand：缺少 --output → MISSING_ARGUMENT
   - FillCommand：不存在模板文件 → FILE_NOT_FOUND
   - FillCommand：不存在数据文件 → FILE_NOT_FOUND
   - CleanupCommand：缺少 --input → MISSING_ARGUMENT
   - CleanupCommand：不存在输入文件 → FILE_NOT_FOUND
   - InspectCommand：缺少 --template → MISSING_ARGUMENT
   - InspectCommand：不存在模板文件 → FILE_NOT_FOUND

3. **JsonlOutput 格式测试**（JsonlOutputTests.cs）：
   - WriteResult 产生包含 type/status/timestamp/data 字段的 JSON
   - WriteError 产生 status=error 的 JSON envelope
   - WriteSummary 产生 status=success 的 JSON envelope
   - 每行输出为单行合法 JSON

实现要点：
- 测试项目通过 `<Compile Include>` 直接编译源文件，需在 csproj 中添加 CLI 文件引用：`<Compile Include="..\Cli\**\*.cs" Link="Cli\%(RecursiveDir)%(Filename).cs" />`
- 为命令创建简单的 stub 实现来满足构造函数注入需求（参数验证测试不会调用实际服务方法）
- 使用 `Console.SetOut(new StringWriter())` 捕获控制台输出
- 使用 `System.Text.Json.JsonDocument` 解析验证 JSONL 输出格式

## Inputs

- `Cli/CliRunner.cs`
- `Cli/JsonlOutput.cs`
- `Cli/ConsoleHelper.cs`
- `Cli/Commands/FillCommand.cs`
- `Cli/Commands/CleanupCommand.cs`
- `Cli/Commands/InspectCommand.cs`
- `Tests/DocuFiller.Tests/DocuFiller.Tests.csproj`

## Expected Output

- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
- `Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs`
- `Tests/DocuFiller.Tests/DocuFiller.Tests.csproj`

## Verification

dotnet test --filter "CliRunnerTests|CommandValidationTests|JsonlOutputTests" --verbosity normal
