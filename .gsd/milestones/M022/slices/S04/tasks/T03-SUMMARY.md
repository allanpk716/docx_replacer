---
id: T03
parent: S04
milestone: M022
key_files:
  - docs/cross-platform-research/platform-differences.md
key_decisions:
  - DocuFiller 不使用 Windows 注册表，迁移无此障碍
  - System.IO.Path 和 Environment.SpecialFolder 天然跨平台，路径处理无需修改
  - WPF 文件对话框和拖放是需要完整重写的核心平台差异点
duration: 
verification_result: passed
completed_at: 2026-05-04T17:12:03.489Z
blocker_discovered: false
---

# T03: 完成平台差异处理调研，撰写 12 章节 platform-differences.md 报告，覆盖文件对话框、拖放、路径处理、文件系统权限、注册表、进程管理六大差异点

**完成平台差异处理调研，撰写 12 章节 platform-differences.md 报告，覆盖文件对话框、拖放、路径处理、文件系统权限、注册表、进程管理六大差异点**

## What Happened

通过全量扫描 DocuFiller 源码中所有 Windows 专有 API 调用，撰写了完整的跨平台平台差异处理调研报告。报告涵盖 6 大差异领域：

1. **文件对话框** — 分析了 FillViewModel（OpenFileDialog ×2、OpenFolderDialog ×2）和 CleanupViewModel（OpenFolderDialog ×1）的 WPF 文件对话框使用，列出了 Avalonia（IStorageProvider）、Tauri（dialog 插件）、Electron（dialog.showOpenDialog）的替代方案及 API 映射关系。

2. **拖放功能** — 深入分析了 FileDragDrop.cs 的 WPF DependencyProperty + DragDrop 实现，梳理了 Avalonia 拖放 API 的迁移路径（DataFormats.FileDrop → DataFormats.Files、string[] → IStorageItem[]、同步 → 异步）。

3. **路径处理** — 确认 System.IO.Path 天然跨平台，Environment.SpecialFolder 已由 .NET Runtime 映射各平台等效路径。DocuFiller 当前代码无硬编码路径分隔符，路径层面无需修改。

4. **文件系统权限与目录规范** — 对比了 Windows ACL、macOS POSIX、Linux XDG 规范的差异，分析了 macOS App Bundle 结构要求（代码签名、公证）和 Linux 打包格式多样性。

5. **注册表使用** — 确认 DocuFiller 不使用 Windows 注册表。唯一的遗留依赖是 System.Configuration.ConfigurationManager（仅一处调用），T02 已建议迁移。

6. **进程管理** — 确认 Process.Start with UseShellExecute 在 .NET 8 中已跨平台可用。Velopack 内部的进程管理已跨平台。

报告还产出了完整的平台特定代码点清单（10 个必须修改项 + 3 个建议修改项 + 多个无需修改项），以及迁移优先级建议和风险矩阵。

## Verification

验证命令执行结果：
1. `test -f docs/cross-platform-research/platform-differences.md` — 文件存在 ✅
2. `grep -c "^## " docs/cross-platform-research/platform-differences.md` — 返回 13 个章节标题 ✅

报告包含 12 个主要章节（目录 + 12 个内容章节），覆盖了任务计划中要求的全部 6 个调研课题。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `test -f docs/cross-platform-research/platform-differences.md && grep -c "^## " docs/cross-platform-research/platform-differences.md` | 0 | ✅ pass | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `docs/cross-platform-research/platform-differences.md`
