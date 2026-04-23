# S01: 调试日志统一和硬编码清理 — UAT

**Milestone:** M002-ahlnua
**Written:** 2026-04-23T08:14:09.211Z

# S01: 调试日志统一和硬编码清理 — UAT

**Milestone:** M002-ahlnua
**Written:** 2026-04-23

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a code cleanup/refactoring task with no runtime UI changes. Verification is via static analysis (grep) and build/test, which are fully automatable.

## Preconditions

- Worktree at `.gsd/worktrees/M002-ahlnua` contains the modified source files
- .NET 8 SDK is available
- NuGet packages restored

## Smoke Test

Run `dotnet test` — all 71 tests should pass with zero failures.

## Test Cases

### 1. Zero Console.WriteLine in production ViewModels

1. Run `grep -rn "Console\.WriteLine" ViewModels/MainWindowViewModel.cs ViewModels/JsonEditorViewModel.cs`
2. **Expected:** Exit code 1 (no matches)

### 2. Zero Console.WriteLine in code-behind files

1. Run `grep -rn "Console\.WriteLine" MainWindow.xaml.cs DocuFiller/Views/CleanupWindow.xaml.cs`
2. **Expected:** Exit code 1 (no matches)

### 3. Zero Debug.WriteLine in code-behind files

1. Run `grep -rn "System\.Diagnostics\.Debug\.WriteLine" MainWindow.xaml.cs DocuFiller/Views/CleanupWindow.xaml.cs`
2. **Expected:** Exit code 1 (no matches)

### 4. KeywordEditorUrl configuration exists

1. Run `grep -q "KeywordEditorUrl" Configuration/AppSettings.cs && grep -q "KeywordEditorUrl" appsettings.json`
2. **Expected:** Exit code 0, both files contain the property/key

### 5. No hardcoded URL in MainWindow.xaml.cs

1. Run `grep "192.168.200.23" MainWindow.xaml.cs`
2. **Expected:** Exit code 1 (URL no longer hardcoded)

### 6. Build succeeds with no C# errors

1. Run `dotnet build --no-restore 2>&1 | grep -E "error (CS|MC)"`
2. **Expected:** Zero output (no C# compilation errors; pre-existing update-client.exe MSBuild error is acceptable)

### 7. All tests pass

1. Run `dotnet test --no-restore`
2. **Expected:** 71 passed, 0 failed, 0 skipped

## Edge Cases

### App.xaml.cs exemption

1. Run `grep "Debug\.WriteLine" App.xaml.cs`
2. **Expected:** One match in global exception handler (line ~71) — this is intentionally excluded from cleanup

### Tools/ exemption

1. Run `grep -rn "Console\.WriteLine" Tools/`
2. **Expected:** Matches may exist — Tools/ directory is intentionally excluded from cleanup scope

## Failure Signals

- Any Console.WriteLine in ViewModels/MainWindowViewModel.cs or ViewModels/JsonEditorViewModel.cs
- Any Debug.WriteLine in MainWindow.xaml.cs or CleanupWindow.xaml.cs
- Missing KeywordEditorUrl property in UISettings class or appsettings.json
- Hardcoded IP address 192.168.200.23 in MainWindow.xaml.cs
- dotnet test reporting any failures

## Not Proven By This UAT

- Runtime logging output at specific log levels (structured logging works at compile time but runtime output depends on logging provider configuration)
- Application startup and shutdown with new constructor parameters (requires GUI runtime)
- Keyword editor URL actually opens the correct web page (requires network access)

## Notes for Tester

- The build always shows 1 error about missing External/update-client.exe — this is a pre-existing CI check in DocuFiller.csproj, not related to this slice
- App.xaml.cs line ~71 has an intentionally preserved Debug.WriteLine for global exception handler exit logging
- Tools/ directory contains standalone diagnostic utilities that are out of scope for this cleanup
