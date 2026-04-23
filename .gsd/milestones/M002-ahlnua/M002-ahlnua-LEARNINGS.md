---
phase: M002-ahlnua
phase_name: "代码质量清理"
project: DocuFiller
generated: "2026-04-23T08:34:00.000Z"
counts:
  decisions: 3
  lessons: 4
  patterns: 3
  surprises: 0
missing_artifacts: []
---

### Decisions

- **Used Microsoft.Win32.OpenFolderDialog (native .NET 8) instead of Windows API Code Pack or FolderBrowserDialog** — matched the existing ConverterWindowViewModel pattern; no extra NuGet packages needed, clean API with FolderName property for initial path and result.
  Source: S02-SUMMARY.md/key_decisions

- **App.xaml.cs global exception handler Debug.WriteLine intentionally preserved** — global exception handler is a last-resort diagnostic path where Debug.WriteLine is appropriate; removing it would lose crash diagnostics in debug builds.
  Source: S01-SUMMARY.md/key_decisions

- **Hardcoded URL default value preserved in UISettings class for backward compatibility** — keeping the default value in the property initializer ensures the app works even if appsettings.json is missing the KeywordEditorUrl entry.
  Source: S01-SUMMARY.md/key_decisions

### Lessons

- **Constructor injection for ILogger<T> and IOptions<T> works well in WPF code-behind files** — not just ViewModels; MainWindow.xaml.cs and CleanupWindow.xaml.cs both gained constructor DI without issues. The pattern scales naturally from MVVM to code-behind.
  Source: S01-SUMMARY.md/patterns_established

- **Upgrading from string interpolation to structured logging template parameters is cheap** — existing _logger calls using $"..." were upgraded to _logger.LogInformation("...", args) in the same pass as Console.WriteLine removal, with zero behavioral change but better log analysis capability.
  Source: S01-SUMMARY.md/narrative

- **Grep-based exit code checking is a reliable zero-cost verification method** — using `grep; echo "EXIT:$?"` and checking for exit code 1 (no matches) confirmed code hygiene without needing test infrastructure or custom linting tools.
  Source: S01-SUMMARY.md/verification, S02-SUMMARY.md/verification

- **Original plan undercounted Console.WriteLine calls** — the plan missed 2 Console.WriteLine calls in MainWindow.xaml.cs DataFileDropBorder_Drop handler. Always grep-scan before planning to get accurate counts.
  Source: S01-SUMMARY.md/deviations

### Patterns

- **Constructor injection for ILogger<T> and IOptions<T> in WPF code-behind files** — when adding logging to code-behind, inject via constructor and assign to private _logger field, mirroring ViewModel DI pattern.
  Source: S01-SUMMARY.md/patterns_established

- **Structured logging with template parameters instead of string interpolation** — use `_logger.LogInformation("Message {Parameter}", value)` rather than `_logger.LogInformation($"Message {value}")` for searchability and structured log analysis.
  Source: S01-SUMMARY.md/patterns_established

- **Folder selection uses OpenFolderDialog with ILogger logging** — LogInformation on successful selection, LogDebug on null/empty early-return; use dialog.FolderName for both initial path and result retrieval.
  Source: S02-SUMMARY.md/patterns_established

### Surprises

(None — this was a straightforward cleanup milestone with no unexpected challenges.)
