---
id: M018
title: "便携版自动更新支持"
status: complete
completed_at: 2026-05-03T09:36:01.007Z
key_decisions:
  - D043: 移除所有便携版更新阻断，便携版和安装版走完全相同的更新代码路径，推翻 D029
  - D044: 推翻 D029，便携版与安装版享有完全相同的自动更新能力
  - D045: Velopack 技术上支持便携版自更新（IsPortable 属性），移除所有基于 IsInstalled 的流程阻断
  - 可选构造函数委托模式：UpdateSettingsViewModel 添加可选 readPersistentConfig 参数实现测试隔离
key_files:
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
  - ViewModels/MainWindowViewModel.cs
  - ViewModels/UpdateSettingsViewModel.cs
  - Cli/Commands/UpdateCommand.cs
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
  - Tests/UpdateSettingsViewModelTests.cs
  - scripts/e2e-portable-update-test.bat
  - scripts/e2e-portable-go-update-test.sh
  - docs/plans/e2e-update-test-guide.md
lessons_learned:
  - Velopack 原生支持便携版自更新——应用层面的 IsInstalled 守卫是过度限制，不是技术必要性
  - E2E 脚本中使用后台进程 + 超时是处理 ApplyUpdatesAndRestart 进程退出的可靠模式
  - 可选构造函数委托是隔离文件系统依赖测试的简洁方案，无需引入 mocking 框架
  - 预存在的测试失败不应在 slice 执行中被忽视——S01 顺带修复了 6 个 UpdateSettingsViewModelTests 失败
---

# M018: 便携版自动更新支持

**移除所有便携版更新阻断逻辑，新增 IUpdateService.IsPortable 属性，便携版享有与安装版完全一致的自动更新能力，并创建 E2E 测试脚本覆盖本地 HTTP 和内网 Go 服务器两种环境**

## What Happened

M018 完成了两个 slice 的工作，彻底移除了 DocuFiller 便携版的自动更新限制。

S01（便携版更新解锁）移除了 GUI 和 CLI 中所有基于 IsInstalled 的便携版更新阻断逻辑。具体改动包括：删除 `UpdateStatus.PortableVersion` 枚举值及其所有 switch 分支，移除 `InitializeUpdateStatusAsync` 中的 IsInstalled 守卫，移除 `UpdateCommand.cs` 中的 PORTABLE_NOT_SUPPORTED 错误码。新增 `IUpdateService.IsPortable` 属性（从 Velopack `UpdateManager.IsPortable` 读取）供下游模式检测使用，同时将 `IsInstalled` 降级为纯信息属性（不用于流程阻断）。此外修复了 6 个预存在的 `UpdateSettingsViewModelTests` 失败，通过为构造函数添加可选的 `readPersistentConfig` 委托实现测试隔离。S01 记录了 D043、D044、D045 三个决策，正式推翻了 D029（"只支持安装版自动更新"）。

S02（E2E 便携版更新测试）创建了两个自动化测试脚本：`e2e-portable-update-test.bat`（本地 HTTP 服务器，端口 8081）和 `e2e-portable-go-update-test.sh`（Go 更新服务器，端口 19081）。两个脚本均自动化完整的便携版更新链路：从源码构建旧版本→vpk 打包→解压 Portable.zip→配置更新源→执行 update --yes→多策略版本验证。S02 还更新了 `e2e-update-test-guide.md`，新增 4 个便携版测试章节和 6 条故障排除条目。

全部 249 个测试通过（222 DocuFiller.Tests + 27 E2ERegression），dotnet build 0 错误。

## Success Criteria Results

| # | 成功标准 | 结果 | 证据 |
|---|---------|------|------|
| 1 | 便携版 GUI 状态栏显示正常更新状态 | ✅ 通过 | `UpdateStatus.PortableVersion` 枚举删除，`IsInstalled` 守卫从 `InitializeUpdateStatusAsync` 移除，grep 确认零残留引用 |
| 2 | 便携版 CLI update --yes 完成完整更新链路 | ✅ 通过 | `IsInstalled` 守卫和 `PORTABLE_NOT_SUPPORTED` 错误从 `UpdateCommand.cs` 移除，测试 `Update_WithYes_Portable_ProceedsNormally` 验证通过 |
| 3 | E2E 脚本覆盖本地 HTTP 和 Go 服务器两种环境 | ✅ 通过 | `e2e-portable-update-test.bat`（17KB，40 个 PASS/FAIL 标记）和 `e2e-portable-go-update-test.sh`（13KB，9 个 PASS/FAIL 标记）均已创建 |
| 4 | 安装版更新行为无回归 | ✅ 通过 | 249/249 测试全部通过（222 + 27），包括现有安装版更新测试 |
| 5 | dotnet build 编译通过 | ✅ 通过 | 0 错误，0 警告 |
| 6 | 决策 D029 已推翻记录 | ✅ 通过 | D043、D044、D045 三个决策已记录在 DECISIONS.md 中，明确推翻 D029 |

## Definition of Done Results

| # | 定义项 | 结果 | 证据 |
|---|--------|------|------|
| 1 | S01 完成 | ✅ 通过 | 2/2 任务完成，S01-SUMMARY.md 已渲染 |
| 2 | S02 完成 | ✅ 通过 | 3/3 任务完成，S02-SUMMARY.md 已渲染 |
| 3 | 分支 diff 包含非 .gsd 文件 | ✅ 通过 | 11 个实现文件（CLI/Services/ViewModels/Tests/Scripts/Docs） |
| 4 | dotnet build 通过 | ✅ 通过 | 0 错误，0 警告 |
| 5 | dotnet test 通过 | ✅ 通过 | 249/249 通过 |
| 6 | 跨 slice 集成正确 | ✅ 通过 | S02 消费 S01 的便携版解锁输出（depends 正确） |

## Requirement Outcomes

| 需求 | 里程碑前状态 | 里程碑后状态 | 证据 |
|------|-------------|-------------|------|
| R001 | validated | validated | 更新验证描述：IsInstalled 守卫从 GUI 和 CLI 移除 |
| R002 | validated | validated | 更新验证描述：IUpdateService.IsPortable 属性实现 |
| R003 | validated | validated | 更新验证描述：PortableVersion 枚举彻底删除 |
| R004 | validated | validated | 更新验证描述：CLI update --yes 便携版正常工作 |
| R026 | validated | validated | 更新验证描述：E2E 便携版脚本覆盖两种环境 |

所有 5 个需求的验证证据已更新，无新增/删除/推迟的需求。

## Deviations

None.

## Follow-ups

None.
