---
id: S01
parent: M002-ahlnua
milestone: M002-ahlnua
provides:
  - ["Clean ILogger logging baseline — zero Console.WriteLine/Debug.WriteLine in production code", "AppSettings.UISettings.KeywordEditorUrl — configurable keyword editor URL in appsettings.json"]
requires:
  []
affects:
  - ["S02"]
key_files:
  - ["ViewModels/MainWindowViewModel.cs", "ViewModels/JsonEditorViewModel.cs", "MainWindow.xaml.cs", "DocuFiller/Views/CleanupWindow.xaml.cs", "Configuration/AppSettings.cs", "appsettings.json"]
key_decisions:
  - ["App.xaml.cs global exception handler Debug.WriteLine intentionally preserved", "Hardcoded URL default value preserved in UISettings class for backward compatibility"]
patterns_established:
  - ["Constructor injection for ILogger<T> and IOptions<T> in WPF code-behind files", "Structured logging with template parameters instead of string interpolation", "Configuration via appsettings.json + IOptions pattern for UI settings"]
observability_surfaces:
  - ["Structured ILogger calls throughout ViewModels and code-behind — grep-friendly by log level and parameters"]
drill_down_paths:
  - [".gsd/milestones/M002-ahlnua/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M002-ahlnua/slices/S01/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-23T08:14:09.211Z
blocker_discovered: false
---

# S01: 调试日志统一和硬编码清理

**Replaced all Console.WriteLine and Debug.WriteLine with structured ILogger calls in production code, migrated hardcoded keyword editor URL to appsettings.json config**

## What Happened

This slice cleaned up debug logging and hardcoded configuration across the DocuFiller codebase.

**T01** removed 17 Console.WriteLine calls from MainWindowViewModel.cs (all were duplicates of existing _logger.LogInformation calls) and 1 from JsonEditorViewModel.cs (replaced with _logger.LogDebug). Also upgraded existing _logger calls from string interpolation to structured logging template parameters.

**T02** added ILogger constructor injection to MainWindow.xaml.cs and CleanupWindow.xaml.cs, replaced 5 Debug.WriteLine calls in MainWindow and 2 in CleanupWindow with _logger.LogDebug calls, and migrated the hardcoded keyword editor URL (http://192.168.200.23:32200/) to an appsettings.json UISettings.KeywordEditorUrl configuration property with an IOptions<UISettings> injection in MainWindow.

All changes compile cleanly (zero C# errors) and all 71 existing tests pass with zero regressions. Broad grep scans confirm zero Console.WriteLine or Debug.WriteLine residue in production code outside the allowed exceptions (App.xaml.cs global exception handler, Tools/ diagnostic utilities).

## Verification

All slice-level verification checks passed:
- grep -rn "Console.WriteLine" across all .cs files (excluding Tools/, obj/, bin/, App.xaml.cs): zero matches (exit code 1)
- grep -rn "System.Diagnostics.Debug.WriteLine" across all .cs files (excluding Tools/, obj/, bin/, App.xaml.cs): zero matches (exit code 1)
- KeywordEditorUrl present in both Configuration/AppSettings.cs and appsettings.json
- dotnet build: zero C# compilation errors (1 pre-existing MSBuild error for missing update-client.exe binary)
- dotnet test: all 71 tests passed, 0 failed, 0 skipped

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Minor deviations from plan: (1) T01 also upgraded existing _logger calls from string interpolation to structured logging template parameters; (2) T02 found 2 additional Console.WriteLine calls in MainWindow.xaml.cs DataFileDropBorder_Drop handler not counted in original plan — these were also replaced with ILogger calls.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
