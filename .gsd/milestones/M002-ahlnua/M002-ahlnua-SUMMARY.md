---
id: M002-ahlnua
title: "代码质量清理"
status: complete
completed_at: 2026-04-23T08:37:45.012Z
key_decisions:
  - Used Microsoft.Win32.OpenFolderDialog (native .NET 8) instead of Windows API Code Pack or FolderBrowserDialog, matching the existing ConverterWindowViewModel pattern
  - App.xaml.cs global exception handler Debug.WriteLine intentionally preserved as the sole allowed Debug.WriteLine in production code
  - Hardcoded URL default value preserved in UISettings class for backward compatibility when appsettings.json entry is missing
key_files:
  - ViewModels/MainWindowViewModel.cs
  - ViewModels/JsonEditorViewModel.cs
  - MainWindow.xaml.cs
  - DocuFiller/Views/CleanupWindow.xaml.cs
  - Configuration/AppSettings.cs
  - appsettings.json
lessons_learned:
  - Native OpenFolderDialog (.NET 8) is the cleanest folder-selection approach for WPF apps — no extra NuGet packages needed, matches existing OpenFileDialog patterns
  - Constructor injection for ILogger<T> and IOptions<T> works well in WPF code-behind files, not just ViewModels — the pattern scales naturally
  - Structured logging with template parameters (not string interpolation) should be the default from the start — upgrading existing calls is cheap and improves log analysis
  - Grep-based verification (with exit code checking) is a reliable zero-cost way to confirm code hygiene — no test infrastructure needed
---

# M002-ahlnua: 代码质量清理

**Replaced all debug logging with structured ILogger calls, migrated hardcoded URL to appsettings.json config, and replaced folder-selection hacks with native OpenFolderDialog — 71 tests pass**

## What Happened

This milestone cleaned up three categories of code quality debt in the DocuFiller production codebase.

**S01 (调试日志统一和硬编码清理)** removed all Console.WriteLine calls from ViewModels (17 duplicates from MainWindowViewModel, 1 from JsonEditorViewModel) and Debug.WriteLine calls from code-behind files (5 from MainWindow.xaml.cs, 2 from CleanupWindow.xaml.cs), replacing them with structured ILogger calls using template parameters. It also migrated the hardcoded keyword editor URL (http://192.168.200.23:32200/) to an appsettings.json UISettings.KeywordEditorUrl configuration property with IOptions<UISettings> injection. The App.xaml.cs global exception handler Debug.WriteLine was intentionally preserved.

**S02 (文件夹选择对话框替换)** replaced three OpenFileDialog-based folder-selection methods (BrowseOutput, BrowseTemplateFolder, BrowseCleanupOutput) with Microsoft.Win32.OpenFolderDialog, matching the existing ConverterWindowViewModel pattern. Each method uses dialog.FolderName for initial path setting and result retrieval, with ILogger calls for success and early-return logging.

Both slices compiled cleanly and all 71 existing tests passed with zero regressions throughout. The changes span 6 files with 97 insertions and 73 deletions.

## Success Criteria Results

- ✅ **生产代码中零 Console.WriteLine 残留（Tools/ 除外）**: grep -rn "Console.WriteLine" across all .cs files (excluding Tools/, obj/, bin/, App.xaml.cs) returned zero matches (exit code 1).
- ✅ **生产代码中零 System.Diagnostics.Debug.WriteLine 残留（App.xaml.cs 全局异常处理除外）**: grep -rn "Debug.WriteLine" across all .cs files (excluding Tools/, obj/, bin/, App.xaml.cs) returned zero matches (exit code 1).
- ✅ **关键词编辑器 URL 从硬编码移至 appsettings.json 的 UISettings.KeywordEditorUrl 配置项**: KeywordEditorUrl confirmed present in both Configuration/AppSettings.cs (line 145) and appsettings.json (line 27) with matching default value.
- ✅ **BrowseOutput、BrowseTemplateFolder、BrowseCleanupOutput 三个方法使用真正的文件夹选择对话框**: grep confirmed 3 OpenFolderDialog uses (all three folder selectors) and 2 remaining OpenFileDialog uses (legitimate file selectors BrowseTemplate, BrowseData).
- ✅ **dotnet test 全部通过，零回归**: dotnet test --no-restore: 71 passed, 0 failed, 0 skipped (774ms).

## Definition of Done Results

- ✅ All slices marked complete in DB: S01 (2/2 tasks done), S02 (1/1 tasks done)
- ✅ S01-SUMMARY.md exists at .gsd/milestones/M002-ahlnua/slices/S01/S01-SUMMARY.md
- ✅ S02-SUMMARY.md exists at .gsd/milestones/M002-ahlnua/slices/S02/S02-SUMMARY.md
- ✅ Cross-slice integration verified: S02 built on S01's clean ViewModel baseline with no conflicts
- ✅ 6 non-.gsd files modified (97 insertions, 73 deletions) — code changes confirmed via git diff

## Requirement Outcomes

No requirements changed status during this milestone. This was a code quality cleanup milestone that did not add new capabilities or modify existing requirement contracts. R004 (test suite integrity) was re-validated: 71 tests pass after replacing three folder-selection methods, zero regressions.

## Deviations

Minor deviations from plan: (1) S01/T01 also upgraded existing _logger calls from string interpolation to structured logging template parameters beyond the original scope; (2) S01/T02 found 2 additional Console.WriteLine calls in MainWindow.xaml.cs DataFileDropBorder_Drop handler not counted in the original plan — these were also replaced.

## Follow-ups

None.
