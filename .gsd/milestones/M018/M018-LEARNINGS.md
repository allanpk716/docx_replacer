---
phase: completion
phase_name: M018 Completion
project: DocuFiller
generated: "2026-05-03T09:35:00Z"
counts:
  decisions: 3
  lessons: 4
  patterns: 3
  surprises: 1
missing_artifacts: []
---

# M018 Learnings

## Decisions

- **D043/D044/D045: 推翻 D029，便携版享有与安装版完全一致的自动更新能力** — Velopack SDK 原生支持便携版自更新（IsPortable 属性、Portable.zip 包含 Update.exe），应用层面的 IsInstalled 守卫是过度限制而非技术必要性。选择完全移除阻断逻辑而非添加便携版特定分支，保持代码路径统一。替代方案：为便携版添加独立更新路径（增加维护成本，无技术收益）。
  Source: S01-SUMMARY.md/What Happened

- **可选构造函数委托模式替代 Mocking 框架** — 通过为 UpdateSettingsViewModel 添加 `Func<(string?, string?)>? readPersistentConfig` 可选参数实现测试隔离，而非引入 Moq/NSubstitute 等框架。选择此方案因为：项目现有测试风格为手写 stub，保持一致性；该委托仅隔离一个静态方法，完整 Mock 框架过重。
  Source: S01-SUMMARY.md/Deviations

- **E2E 脚本端口隔离策略** — 便携版 E2E 脚本使用独立端口（8081/19081）避免与现有安装版 E2E 脚本（8080/19080）冲突。选择硬编码端口差异而非动态端口分配，因为并行执行不是当前需求，固定端口更利于调试和日志追踪。
  Source: S02-SUMMARY.md/What Happened

## Lessons

- **Velopack 便携版自更新是 SDK 原生能力，不需要应用层特殊处理** — 之前 D029 假设便携版不支持自动更新，但 Velopack 的 `UpdateManager.IsPortable` 和 Portable.zip 中的 Update.exe 明确支持。在添加限制性守卫前应先验证底层框架的实际能力。
  Source: S01-SUMMARY.md/What Happened

- **预存在的测试失败应在 slice 执行中顺带修复** — S01 发现 6 个 UpdateSettingsViewModelTests 失败（读取真实文件系统配置），通过添加可选委托参数修复。这些失败虽非本 slice 计划范围，但 `dotnet test` 全通过是质量底线，必须处理。
  Source: S01-SUMMARY.md/Deviations

- **ApplyUpdatesAndRestart 会导致进程退出，E2E 脚本需要后台进程 + 超时机制** — Velopack 的 ApplyUpdatesAndRestart 调用会终止当前进程并启动更新后的新版本。E2E 脚本必须在后台运行被测程序并设置超时，而非同步等待返回码。
  Source: S02-SUMMARY.md/Patterns established

- **多策略版本验证提高 E2E 测试可靠性** — 仅依赖 JSONL 日志解析可能遗漏更新失败场景。组合使用日志输出检查 + exe 文件版本信息读取的双重验证策略，显著提高测试可信度。
  Source: S02-SUMMARY.md/Patterns established

## Patterns

- **可选构造函数委托实现文件系统依赖测试隔离** — 对依赖静态文件系统方法的服务，在构造函数中注入 `Func<T>?` 可选委托。测试传入 no-op 委托，生产代码 null 值回退到静态方法。零框架依赖，向后兼容。
  Source: S01-SUMMARY.md/Deviations

- **便携版更新配置路径：`%USERPROFILE%\.docx_replacer\update-config.json`** — 便携版没有固定安装目录，Velopack 的 AppUserDir 也不可靠。使用用户主目录下的固定路径作为更新源配置，独立于便携版的解压位置。
  Source: S02-SUMMARY.md/Patterns established

- **E2E 脚本 PASS/FAIL 标准输出格式** — 每个断言输出 `[PASS]` 或 `[FAIL] description`，脚本结尾输出 `=== OVERALL: PASS/FAIL ===`。便于 grep 快速定位失败点，也适合 CI 管道解析。
  Source: S02-SUMMARY.md/What Happened

## Surprises

- **移除便携版更新限制只需删除代码，无需添加新逻辑** — 原以为可能需要为便携版添加独立的更新处理分支，但实际上只需删除 IsInstalled 守卫即可。便携版和安装版走完全相同的 Velopack 更新管道，零额外代码。
  Source: S01-SUMMARY.md/Key decisions
