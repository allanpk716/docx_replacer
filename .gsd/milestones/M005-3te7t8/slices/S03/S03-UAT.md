# S03: CLI 测试 + 文档更新 — UAT

**Milestone:** M005-3te7t8
**Written:** 2026-04-23T16:40:46.469Z

# S03 UAT: CLI 测试 + 文档更新

## 前置条件
- .NET 8 SDK 已安装
- 工作目录: 项目根目录

## 测试用例

### TC-01: 全部测试通过
**步骤**:
1. 运行 `dotnet test --verbosity minimal`
**预期结果**: 显示 "已通过! - 失败: 0，通过: 108"，无失败或跳过

### TC-02: CLI 测试通过
**步骤**:
1. 运行 `dotnet test --filter "CliRunnerTests|CommandValidationTests|JsonlOutputTests" --verbosity normal`
**预期结果**: 37 个 CLI 测试全部通过（CliRunnerTests 13 + CommandValidationTests 14 + JsonlOutputTests 10）

### TC-03: CLAUDE.md 包含 CLI 架构说明
**步骤**:
1. 搜索 CLAUDE.md 中 "CliRunner" 关键字
**预期结果**: 在服务层架构表中找到 CliRunner 条目，在文件结构中找到 CliRunner.cs

### TC-04: CLAUDE.md 包含 JSONL 格式文档
**步骤**:
1. 搜索 CLAUDE.md 中 "JSONL" 关键字
**预期结果**: 找到 JSONL envelope schema 说明、输出类型表、错误码列表

### TC-05: README.md 包含 CLI 使用方法
**步骤**:
1. 搜索 README.md 中 "CLI" 关键字
**预期结果**: 找到 "CLI 使用方法" 章节和 "CLI 接口（LLM Agent 集成）" 功能说明

### TC-06: README.md 包含 fill 子命令示例
**步骤**:
1. 搜索 README.md 中 "fill --template" 关键字
**预期结果**: 找到 fill 子命令的调用示例

### TC-07: 测试文件结构完整
**步骤**:
1. 检查 Tests/DocuFiller.Tests/Cli/ 目录
**预期结果**: 存在 CliRunnerTests.cs、CommandValidationTests.cs、JsonlOutputTests.cs 三个文件

## 边界情况
- 在 Windows 环境下 findstr 替代 grep 进行内容搜索验证
- 测试项目通过 `<Compile Include>` 引用 CLI 源文件，确保与主项目编译一致

