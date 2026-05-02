---
phase: execute
phase_name: M004-l08k3s
project: DocuFiller
generated: 2026-04-23T23:14:57Z
counts:
  decisions: 2
  lessons: 3
  patterns: 2
  surprises: 2
missing_artifacts: []
---

### Decisions

- **Extended T01 scope to include T02 work (MainWindow cleanup)** when the update system proved deeply integrated into MainWindowViewModel constructor params, fields, commands, methods, and properties — the project would not compile without removing those references. This avoided a broken intermediate state.
  Source: S01-SUMMARY.md/What Happened

- **Created standalone `Models/DataStatistics.cs`** to replace the class previously nested in `IDataParser.cs`, which was being deleted. This avoided breaking test consumers that referenced DataStatistics.
  Source: S02-SUMMARY.md/What Happened

### Lessons

- **Solution files (.sln) must be cleaned when removing projects** — not just .csproj exclusion entries. MSB3202 build errors occur if deleted project references remain in the .sln file.
  Source: S02-SUMMARY.md/Deviations

- **Worktree git operations can corrupt files** — MainWindowViewModel.cs had duplicate content with garbled bytes appended after the closing brace (likely from worktree git operations). Always verify file integrity after worktree setup.
  Source: S01-SUMMARY.md/What Happened

- **Dead code consumers may hide in unexpected places** — Tools/E2ETest/Program.cs had an IDataParser registration not listed in any task plan. Removed to keep build passing (Tools directory was deleted in T03 anyway).
  Source: S02-SUMMARY.md/Deviations

### Patterns

- **Feature removal cascade in MVVM apps**: delete service files → clean DI registrations → clean consuming ViewModels → clean Views → clean code-behind → verify build. Tight coupling to MainWindow may require expanding scope beyond the feature's own files.
  Source: S01-SUMMARY.md/Patterns Established

- **Data source removal pattern**: delete interface + implementation, remove from DI, remove processing branches from DocumentProcessorService, simplify enums (DataFileType), update file dialogs (.xlsx only), update drop hints in XAML.
  Source: S02-SUMMARY.md/Patterns Established

### Surprises

- **Build warnings reduced from 54 to 0** as a side effect of feature removal — the removed update/JSON/converter code was the source of many warnings. Feature cleanup simultaneously improved build hygiene.
  Source: S03-SUMMARY.md/What Happened

- **IDataParser grep false positives** — searching for "IDataParser" matches the active "IExcelDataParser" service. Always use word boundary (`\b`) in grep patterns or filter results to exclude active services.
  Source: S02-SUMMARY.md/Verification
