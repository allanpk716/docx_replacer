---
id: T02
parent: S02
milestone: M005-3te7t8
key_files:
  - Cli/Commands/CleanupCommand.cs
  - App.xaml.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T16:15:39.812Z
blocker_discovered: false
---

# T02: 实现 CleanupCommand 类并注册到 DI 容器，支持 cleanup 子命令的参数验证、输入路径检查、文件夹/单文件模式、输出目录指定及 JSONL 格式输出

**实现 CleanupCommand 类并注册到 DI 容器，支持 cleanup 子命令的参数验证、输入路径检查、文件夹/单文件模式、输出目录指定及 JSONL 格式输出**

## What Happened

创建了 `Cli/Commands/CleanupCommand.cs`，实现 ICliCommand 接口，CommandName = "cleanup"。构造函数注入 IDocumentCleanupService 和 ILogger。ExecuteAsync 流程：验证 --input 必需参数 → 确定 folder/singleFile 模式（基于 --folder 标志或路径是否为目录）→ 验证路径存在性 → 根据 --output 是否指定选择对应 CleanupAsync 重载（指定输出目录用 fileItem+outputDirectory 重载，否则单文件原地用 filePath 重载，文件夹原地用 fileItem 重载）→ 成功输出 result（commentsRemoved、controlsUnwrapped、outputPath）+ summary → 失败输出 CLEANUP_ERROR。在 App.xaml.cs 添加了 `services.AddSingleton<ICliCommand, CleanupCommand>()` DI 注册。修复了 nullable 引用警告（outputDir 使用 null-forgiving operator，因为 hasOutput 守卫保证非 null）。

## Verification

dotnet build 编译成功（0 error，0 warning）。dotnet test 全部 71 个测试通过（0 失败）。代码遵循 FillCommand 的结构和命名风格，所有用户输出通过 JsonlOutput 辅助类。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2200ms |
| 2 | `dotnet test` | 0 | ✅ pass | 761ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Cli/Commands/CleanupCommand.cs`
- `App.xaml.cs`
