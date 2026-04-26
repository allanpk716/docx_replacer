# GSD context snapshot (2026-04-24T00:14:52.831Z)

## Top project memories
- [MEM005] (gotcha) ExcelDataParserService.ParseExcelFileAsync throws NullReferenceException when worksheet.Dimension is null (completely empty worksheet). Only ValidateExcelFileAsync guards against this. Tests document this as current behavior rather than a fix — it's a pre-existing issue not introduced by the 3-column feature.
- [MEM007] (gotcha) When verifying build on Windows, the DocuFiller.csproj has a custom MSBuild target that errors if External/update-client.exe is missing. This is a pre-existing check unrelated to code changes — filter for "error CS" or "error MC" to detect real compilation errors.
- [MEM023] (environment) GSD worktrees are at .gsd/worktrees/M003-g1w88x/ — all file operations must use this as the working directory, not the repo root.
- [MEM026] (gotcha) Windows worktree 环境中没有 grep 命令。所有验证命令必须使用 PowerShell 替代（Select-String、Measure-Object）。任务计划中的 grep 验证命令会在 Windows 上失败。
- [MEM031] (convention) DocuFiller 项目规范：每次完成功能改进或开发后，必须执行 `dotnet build` 确认编译通过，再进行 commit 和 push。即使本次修改不涉及代码变更（如文档更新），也要编译确认，因为 git merge/stash 操作可能引入意外的文件损坏（M003 merge 后 MainWindowViewModel.cs 末尾出现垃圾数据就是例子）。
- [MEM034] (pattern) 新项目使用 GSD 且需要追踪 .gsd/ 规划文档时，必须在 .gitignore 中添加 GSD_RUNTIME_PATTERNS（来自 gsd-2/src/resources/extensions/gsd/gitignore.ts），确保 gsd.db、STATE.md、journal/ 等运行时文件不被 git 追踪。否则在 Windows 上会导致 milestone merge 时 SQLite WAL 锁与 git 文件操作冲突（gsd-build/gsd-2#4718）。如果不需要追踪规划文档，使用 GSD 默认的 .gsd 整体忽略即可。

## Recent gsd_exec runs
- [704413e6-5705-46a8-9d40-faf3484a958f] python exit:0 — Check actu
…[truncated]
