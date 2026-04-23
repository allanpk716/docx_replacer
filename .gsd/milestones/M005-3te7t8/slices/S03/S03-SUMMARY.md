---
id: S03
parent: M005-3te7t8
milestone: M005-3te7t8
provides:
  - ["37 个 CLI 单元测试覆盖路由、参数验证、输出格式", "CLAUDE.md 和 README.md 包含完整 CLI 使用文档"]
requires:
  []
affects:
  []
key_files:
  - ["Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs", "Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs", "Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs", "Tests/DocuFiller.Tests.csproj", "CLAUDE.md", "README.md"]
key_decisions:
  - (none)
patterns_established:
  - ["xUnit 单元测试中 Console.SetOut 需要 DisableTestParallelization 防止并行干扰", "CLI 命令验证测试使用 Stub 服务（NotImplementedException）满足 DI 构造函数需求，无需完整 Mock", "Windows 环境验证用 findstr 替代 grep"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-23T16:40:46.468Z
blocker_discovered: false
---

# S03: CLI 测试 + 文档更新

**新增 37 个 CLI 单元测试（CliRunner 路由、命令参数验证、JsonlOutput 格式），更新 CLAUDE.md 和 README.md 添加完整 CLI 使用文档，全部 108 个测试通过**

## What Happened

S03 是 M005（CLI 接口）的最后一个切片，聚焦于测试覆盖和文档完善。

**T01 — CLI 单元测试（37 个）**：
- JsonlOutputTests（10 个）：验证 WriteResult/WriteError/WriteSummary 的 JSON envelope 结构（type/status/timestamp/data 字段）、单行 JSON 输出、ISO 8601 时间戳、null code 处理、WriteRaw 直通。
- CliRunnerTests（13 个）：验证路由逻辑——空参数返回 -1（GUI 模式）、--help/-h 输出全局帮助 JSON、--version/-v 输出版本 JSON、未知命令产生 UNKNOWN_COMMAND 错误、三个子命令 --help 输出子命令帮助、DI 分发到正确 ICliCommand 实现、未注册命令产生 COMMAND_NOT_IMPLEMENTED 错误。
- CommandValidationTests（14 个）：验证三个命令的参数校验——FillCommand 缺少 template/data/output → MISSING_ARGUMENT、不存在的文件 → FILE_NOT_FOUND；CleanupCommand 缺少 input → MISSING_ARGUMENT；InspectCommand 缺少 template → MISSING_ARGUMENT。所有验证失败退出码为 1。

关键技术决策：
- 使用 `[assembly: CollectionBehavior(DisableTestParallelization = true)]` 防止 xUnit 并行执行时 Console.SetOut 互相干扰。
- 创建 StubDocumentProcessor/StubExcelDataParser/StubCleanupService（均抛 NotImplementedException）满足构造函数注入需求。
- 使用 NullLogger<T> 避免 LoggerFactory 开销。

**T02 — 文档更新**：
- CLAUDE.md：添加 CLI/GUI 双模式说明、6 个 CLI 组件到服务层表、新增「CLI 接口」章节（子命令用法、JSONL envelope schema、输出类型、错误码、使用示例）、Cli/ 目录说明。
- README.md：主要功能增加 CLI 接口说明、新增「CLI 使用方法」章节（三个子命令参数和示例、JSONL 格式说明）、编译运行章节增加 CLI 调用方式。

**验证结果**：全部 108 个测试通过（含 37 个新增 CLI 测试）。CLAUDE.md 包含 CliRunner 和 JSONL 关键字，README.md 包含 CLI 和 fill --template 关键字。

## Verification

全部验证通过：
1. dotnet test --verbosity minimal: 108 passed, 0 failed, 0 skipped
2. CLAUDE.md 包含 CliRunner（服务层架构表 + 文件结构说明）
3. CLAUDE.md 包含 JSONL（CLI 接口章节 + JSONL envelope schema）
4. README.md 包含 CLI（主要功能 + CLI 使用方法章节）
5. README.md 包含 fill --template（fill 子命令示例）

初始验证中 grep 命令在 Windows 上不可用导致误报，使用 findstr 重新验证全部通过。

## Requirements Advanced

- R021 — CLAUDE.md 和 README.md 已添加 CLI 架构说明、子命令文档、JSONL 格式和错误码说明

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
