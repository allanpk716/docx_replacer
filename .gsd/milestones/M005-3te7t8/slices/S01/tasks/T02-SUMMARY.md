---
id: T02
parent: S01
milestone: M005-3te7t8
key_files:
  - App.xaml.cs
key_decisions:
  - CLI error path uses Console.WriteLine with inline JSON instead of MessageBox to avoid popups
  - ConsoleHelper init/cleanup delegated to CliRunner.RunAsync internally rather than duplicated at the App level
duration: 
verification_result: passed
completed_at: 2026-04-23T15:33:14.264Z
blocker_discovered: false
---

# T02: Implement CLI/GUI dual-mode fork in App.OnStartup with JSONL error output

**Implement CLI/GUI dual-mode fork in App.OnStartup with JSONL error output**

## What Happened

Modified App.xaml.cs OnStartup to implement CLI/GUI dual-mode branching. When command-line arguments are present, the app enters CLI mode: ConfigureServices is called, a CliRunner is instantiated with the service provider, and RunAsync is invoked synchronously via GetAwaiter().GetResult(). The CLI error path outputs JSONL error lines to console instead of showing MessageBox dialogs, ensuring no popups in CLI mode. When no arguments are provided, the original GUI startup logic runs unchanged. The ConsoleHelper initialization and cleanup are handled internally by CliRunner.RunAsync, avoiding duplicate calls. A defensive goto GuiMode label handles the unlikely case of CliRunner returning -1 (normally only happens with empty args, which won't reach this branch).

## Verification

dotnet build completed successfully with 0 compilation errors. The modified App.xaml.cs compiles cleanly with the new `using DocuFiller.Cli` import and the dual-mode OnStartup method. All pre-existing test warnings are unrelated to this change.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2160ms |

## Deviations

The task plan showed calling ConsoleHelper.Initialize() and Cleanup() directly in App.xaml.cs, but since CliRunner.RunAsync already handles these internally (with idempotent guards), the App-level calls were omitted to avoid redundancy. The plan also showed `new JsonlOutput()` as a parameter to CliRunner, but CliRunner's constructor only takes IServiceProvider — JsonlOutput is a static class used directly by CliRunner internally.

## Known Issues

None.

## Files Created/Modified

- `App.xaml.cs`
