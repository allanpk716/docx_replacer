---
id: T02
parent: S03
milestone: M005-3te7t8
key_files:
  - CLAUDE.md
  - README.md
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T16:39:27.825Z
blocker_discovered: false
---

# T02: 更新 CLAUDE.md 和 README.md 添加 CLI 使用说明和 JSONL 输出格式文档

**更新 CLAUDE.md 和 README.md 添加 CLI 使用说明和 JSONL 输出格式文档**

## What Happened

在 CLAUDE.md 中添加了 4 处 CLI 相关内容：(1) Architecture Overview 中补充 CLI/GUI 双模式说明；(2) Service Layer Architecture 表中增加 6 个 CLI 组件（CliRunner、JsonlOutput、ConsoleHelper、FillCommand、CleanupCommand、InspectCommand）；(3) 新增「CLI 接口」章节，包含子命令用法、JSONL envelope schema、输出类型、错误码和使用示例；(4) File Structure Notes 中添加 Cli/ 目录和 Program.cs 说明。在 README.md 中添加了 3 处内容：(1) 主要功能中增加 CLI 接口说明（LLM Agent 集成）；(2) 新增「CLI 使用方法」章节，包含 inspect/fill/cleanup 三个子命令的参数说明和 JSONL 输出格式；(3) 编译和运行章节中添加 CLI 调用方式。项目结构中也补充了 Cli/ 目录。

## Verification

dotnet test 通过（108 tests, 0 failures）。grep 验证：CLAUDE.md 包含 CliRunner 和 JSONL 关键字，README.md 包含 CLI 和 fill --template 关键字。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --verbosity minimal` | 0 | ✅ pass | 60000ms |
| 2 | `grep -q CliRunner CLAUDE.md` | 0 | ✅ pass | 100ms |
| 3 | `grep -q JSONL CLAUDE.md` | 0 | ✅ pass | 100ms |
| 4 | `grep -q CLI README.md` | 0 | ✅ pass | 100ms |
| 5 | `grep -q "fill --template" README.md` | 0 | ✅ pass | 100ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `CLAUDE.md`
- `README.md`
