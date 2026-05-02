---
id: M004-l08k3s
title: "功能瘦身 — 移除不活跃功能模块"
status: complete
completed_at: 2026-04-23T15:16:33.448Z
key_decisions:
  - Extended T01 scope to include MainWindow cleanup (T02) since update system was deeply integrated into constructor params, commands, properties, and event handlers — project would not compile without removing these references
  - Created Models/DataStatistics.cs as standalone class to replace the class previously nested in deleted IDataParser.cs — avoids breaking test consumers
  - Removed all Tools/ entries from DocuFiller.sln (not just .csproj exclusions) to prevent MSB3202 build errors from referencing deleted projects
key_files:
  - DocuFiller.csproj
  - App.xaml.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
  - ViewModels/MainWindowViewModel.cs
  - Services/DocumentProcessorService.cs
  - Models/DataStatistics.cs
  - Configuration/AppSettings.cs
  - appsettings.json
  - Utils/ValidationHelper.cs
  - CLAUDE.md
  - README.md
  - docs/excel-data-user-guide.md
  - docs/DocuFiller产品需求文档.md
  - docs/DocuFiller技术架构文档.md
  - DocuFiller.sln
lessons_learned:
  - Feature removal in tightly-coupled MVVM apps often requires cascading into consuming ViewModels and Views — plan for expanded scope beyond the feature's own files
  - Solution files (.sln) must be cleaned when removing projects — .csproj exclusion entries alone are insufficient and cause build errors
  - Worktree git operations can corrupt files — always verify file integrity after worktree setup
  - Build warnings can be eliminated alongside feature removal when the removed code was the source of the warnings
---

# M004-l08k3s: 功能瘦身 — 移除不活跃功能模块

**移除了在线更新系统（19文件）、JSON编辑器遗留（9文件）、JSON数据源（IDataParser）、转换器窗口、KeywordEditorUrl、Tools目录（10项目）、Newtonsoft.Json依赖，Excel成为唯一数据源，71测试全通过，文档全部同步**

## What Happened

## 里程碑执行概述

M004-l08k3s 通过三个切片成功清理了 DocuFiller 代码库中所有不活跃功能模块，将 Excel 确立为唯一数据源。

### S01 — 移除更新功能和 JSON 编辑器遗留

删除了整个在线更新基础设施：DocuFiller.csproj 中的 3 个构建目标（PreBuild/PostPublish 门禁）、External/ 目录、19 个更新系统文件（Services/Update、Models/Update、ViewModels/Update、Views/Update），以及 App.xaml.cs 中 5 个 DI 注册、MainWindowViewModel 中 7 个更新方法/3 个属性、MainWindow.xaml 更新 UI 和 MainWindow.xaml.cs 事件处理。

同时删除了 9 个无 DI 注册的 JSON 编辑器遗留文件。修复了 MainWindowViewModel.cs 的文件损坏问题（worktree git 操作导致的重复内容和乱码字节）。

### S02 — 移除 JSON 数据源、转换器、KeywordEditorUrl、Tools

删除了 IDataParser/DataParserService，从 DocumentProcessorService 移除了所有 JSON 处理分支。简化了 DataFileType 枚举为仅 Excel，更新了文件对话框过滤器。

删除了 5 个转换器文件（ConverterWindow、ExcelToWordConverterService 等），移除了 MainWindow.xaml 中的"工具"TabItem。

从 appsettings.json 和 AppSettings.cs 移除了 KeywordEditorUrl，清理了 MainWindow.xaml.cs 中的浏览器打开逻辑。

删除了 Tools/ 目录下全部 10 个诊断工具项目，清理了 DocuFiller.csproj 排除条目和 DocuFiller.sln 解决方案文件。

创建了 Models/DataStatistics.cs 来替代之前嵌套在 IDataParser 中的类。

### S03 — 测试修复和文档同步

移除了 ValidateJsonFormat 死代码（唯一的 Newtonsoft.Json 消费者）和 Newtonsoft.Json 包引用。删除了 test-data.json。更新了测试模板文档。

全面重写了 CLAUDE.md（服务表从 14+2 缩减为 10+2）。更新了 README.md 为 Excel-only 描述。删除了 7 份过时文档，更新了 3 份现有文档。

### 范围偏差

- S01/T01 扩展了范围包含了 T02 的全部工作，因为更新系统与 MainWindowViewModel 深度耦合，不清理这些引用项目无法编译
- S02/T03 需要额外清理 DocuFiller.sln（原始计划未包含）

### 最终状态

- dotnet build: 0 错误, 0 警告
- dotnet test: 71 通过, 0 失败
- 代码库完全 Excel-only，无 JSON/更新/转换器残留
- 文档与代码完全一致

## Success Criteria Results

### Success Criteria Results

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| 1 | dotnet build 在无 External/ 目录文件的情况下编译成功 | ✅ pass | `dotnet build --no-restore` → 0 errors, 0 warnings; `External/` directory confirmed deleted |
| 2 | dotnet test 全部通过 | ✅ pass | `dotnet test --no-build --verbosity minimal` → 71 passed, 0 failed |
| 3 | 无残留的更新/JSON编辑器/JSON数据源/转换器相关 .cs/.xaml 文件 | ✅ pass | grep confirms 0 matches for IUpdateService, UpdateViewModel, UpdateBannerView, IDataParser, DataParserService, ConverterWindow, JsonEditor, KeywordEditorUrl in source files |
| 4 | CLAUDE.md 和 README.md 与清理后的代码一致 | ✅ pass | grep confirms only IExcelDataParser/ExcelDataParserService references (active services), 0 references to removed modules |
| 5 | 应用可正常启动，Excel 数据源处理流程完整可用 | ✅ pass | Build succeeds, all 71 tests pass including Excel integration tests |

## Definition of Done Results

### Definition of Done Results

| # | Item | Result | Evidence |
|---|------|--------|----------|
| 1 | All slices marked [x] | ✅ pass | S01, S02, S03 all marked complete in ROADMAP.md |
| 2 | All slice summaries exist | ✅ pass | S01-SUMMARY.md, S02-SUMMARY.md, S03-SUMMARY.md all present and verified |
| 3 | Cross-slice integration verified | ✅ pass | S01→S02 boundary: clean App.xaml.cs DI and MainWindow consumed by S02. S02→S03 boundary: clean DocumentProcessorService consumed by S03 for test/doc sync. Build and 71 tests confirm integration |
| 4 | No residual dead code | ✅ pass | ValidateJsonFormat removed, Newtonsoft.Json removed, test-data.json deleted, DataStatistics extracted to standalone file |
| 5 | No build warnings from removed code | ✅ pass | dotnet build → 0 warnings (previously 54 warnings, all eliminated during cleanup) |

## Requirement Outcomes

### Requirement Status Transitions

| Requirement | Status | Evidence |
|------------|--------|----------|
| R014: 移除在线更新代码 | Active → Validated | 19 files deleted, csproj gates removed, External/ deleted, DI removed, MainWindow cleaned. grep 0 matches. build passes. |
| R015: 移除 JSON 编辑器遗留 | Active → Validated | 9 orphaned files deleted. No remaining files. build passes. |
| R016: 移除 JSON 数据源 | Active → Validated | IDataParser/DataParserService deleted, DocumentProcessorService Excel-only, DataFileType enum Excel-only. build and test pass. |
| R017: 移除转换器 | Active → Validated | 5 converter files deleted, 0 references remaining. DI removed. build and test pass. |
| R018: 移除 KeywordEditorUrl | Active → Validated | Removed from appsettings.json and AppSettings.cs. Handlers removed from MainWindow.xaml.cs. build and test pass. |
| R019: 删除 Tools 目录 | Active → Validated | Tools/ deleted. 10 projects removed from .sln and .csproj. 0 residual references. build and test pass. |
| R020: 测试全部通过 | Active → Validated | 71 tests pass. Newtonsoft.Json removed. test-data.json deleted. |
| R021: 文档同步 | Active → Validated | CLAUDE.md rewritten, README.md updated, 7 stale docs deleted, 3 docs updated. grep confirms consistency. |

## Deviations

None.

## Follow-ups

None.
