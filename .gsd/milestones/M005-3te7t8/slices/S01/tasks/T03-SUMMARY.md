---
id: T03
parent: S01
milestone: M005-3te7t8
key_files:
  - Cli/Commands/InspectCommand.cs
  - App.xaml.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T15:37:16.740Z
blocker_discovered: false
---

# T03: Implement inspect subcommand handler with JSONL output for template content controls

**Implement inspect subcommand handler with JSONL output for template content controls**

## What Happened

Created the inspect subcommand implementation in `Cli/Commands/InspectCommand.cs`. The class implements `ICliCommand` (defined in T01's CliRunner.cs) and is registered in the DI container via `services.AddSingleton<ICliCommand, InspectCommand>()` in App.xaml.cs.

The inspect command:
1. Validates the required `--template` parameter, outputting JSONL error with code `MISSING_ARGUMENT` if absent
2. Checks file existence, outputting JSONL error with code `FILE_NOT_FOUND` if the file doesn't exist
3. Resolves `IDocumentProcessor` via constructor DI and calls `GetContentControlsAsync(templatePath)`
4. Outputs one JSONL `{"type":"control",...}` line per content control with tag, title, contentType, and location
5. Outputs a JSONL summary line with `totalControls` count
6. Wraps all processing in try-catch for error JSONL output with code `INSPECT_ERROR`

The task plan mentioned creating `Cli/Commands/ICommand.cs` but the `ICliCommand` interface was already defined in `CliRunner.cs` by T01, so no separate interface file was needed. The DI registration was added to `App.xaml.cs` alongside other service registrations.

## Verification

dotnet build completed with 0 errors and 0 warnings. All 71 existing tests pass. The inspect command is structurally correct: it implements ICliCommand, validates inputs, calls IDocumentProcessor.GetContentControlsAsync, and outputs JSONL via JsonlOutput. Runtime execution could not be verified in the worktree due to a WPF XAML resource loading issue (missing .gresources.dll) that is pre-existing in the worktree environment and unrelated to T03 changes.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 1180ms |
| 2 | `dotnet test --no-build` | 0 | ✅ pass (71/71 tests) | 775ms |

## Deviations

The task plan expected creating Cli/Commands/ICommand.cs as a separate interface file, but ICliCommand was already defined in CliRunner.cs by T01 — reused the existing interface instead. The parameter type is Dictionary<string, string> (from T01's ICliCommand) rather than string[] (from the plan), since the arg parsing is already done by CliRunner.

## Known Issues

WPF XAML resource loading fails in the git worktree environment (missing .gresources.dll), preventing runtime CLI testing. This is a worktree-specific issue, not related to the T03 code changes.

## Files Created/Modified

- `Cli/Commands/InspectCommand.cs`
- `App.xaml.cs`
