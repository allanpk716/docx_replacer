# S03: CLI 测试 + 文档更新

**Goal:** 为 S01+S02 实现的 CLI 代码（CliRunner、三个命令、JsonlOutput）编写单元测试，更新 CLAUDE.md 和 README.md 添加 CLI 使用说明和 JSONL 输出格式文档。dotnet test 全部通过。
**Demo:** dotnet test 全部通过（含新增 CLI 测试）；CLAUDE.md 和 README.md 包含 CLI 使用说明和 JSONL 输出格式文档

## Must-Haves

- `dotnet test` 全部通过（含新增 CLI 测试），0 失败\n- CLAUDE.md 包含 CLI 架构说明、三个子命令文档、JSONL 输出格式、错误码说明\n- README.md 包含 CLI 使用方法章节、子命令参数和示例、JSONL 格式说明

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: CliRunner、ICliCommand、JsonlOutput、ConsoleHelper（S01+S02 产出）\n- New wiring: 测试文件链接到测试项目 csproj\n- What remains: 无 — 这是里程碑最后一个切片，完成后 M005 可验证端到端

## Verification

- None — 纯测试和文档更新，不改变运行时行为。

## Tasks

- [x] **T01: 编写 CLI 单元测试** `est:1h`
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
  - Files: `Tests/DocuFiller.Tests/DocuFiller.Tests.csproj`, `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`, `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`, `Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs`
  - Verify: dotnet test --filter "CliRunnerTests|CommandValidationTests|JsonlOutputTests" --verbosity normal

- [x] **T02: 更新 CLAUDE.md 和 README.md 添加 CLI 文档** `est:30m`
  更新项目文档，添加 CLI 使用说明和 JSONL 输出格式文档。

CLAUDE.md 更新内容：
1. 在 Architecture Overview 中添加 CLI 架构层说明（无参数→GUI，有参数→CLI）
2. 在 Service Layer Architecture 表中添加 CLI 组件说明（CliRunner、JsonlOutput、ConsoleHelper）
3. 添加「CLI 接口」章节，包含：
   - 三个子命令的使用方法和参数说明
   - JSONL 输出格式（envelope schema: type/status/timestamp/data）
   - JSONL 输出类型（help、control、result、progress、summary、error）
   - 错误码说明（MISSING_ARGUMENT、FILE_NOT_FOUND、FILL_ERROR、CLEANUP_ERROR、INSPECT_ERROR、UNKNOWN_COMMAND）
   - 使用示例
4. 在 File Structure Notes 中添加 Cli/ 目录说明

README.md 更新内容：
1. 在「主要功能」中添加 CLI 接口说明（LLM agent 集成）
2. 添加「CLI 使用方法」章节：
   - fill/cleanup/inspect 三个子命令
   - 每个子命令的参数和示例
   - JSONL 输出格式说明
3. 在「编译和运行」章节中添加 CLI 调用方式
  - Files: `CLAUDE.md`, `README.md`
  - Verify: dotnet test --verbosity minimal && grep -q "CliRunner" CLAUDE.md && grep -q "JSONL" CLAUDE.md && grep -q "CLI" README.md && grep -q "fill --template" README.md

## Files Likely Touched

- Tests/DocuFiller.Tests/DocuFiller.Tests.csproj
- Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
- Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs
- Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs
- CLAUDE.md
- README.md
