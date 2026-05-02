---
estimated_steps: 18
estimated_files: 2
skills_used: []
---

# T02: 更新 CLAUDE.md 和 README.md 添加 CLI 文档

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

## Inputs

- `CLAUDE.md`
- `README.md`

## Expected Output

- `CLAUDE.md`
- `README.md`

## Verification

dotnet test --verbosity minimal && grep -q "CliRunner" CLAUDE.md && grep -q "JSONL" CLAUDE.md && grep -q "CLI" README.md && grep -q "fill --template" README.md
