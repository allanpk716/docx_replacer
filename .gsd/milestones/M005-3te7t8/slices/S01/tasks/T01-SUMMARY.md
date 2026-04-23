---
id: T01
parent: S01
milestone: M005-3te7t8
key_files:
  - Cli/ConsoleHelper.cs
  - Cli/JsonlOutput.cs
  - Cli/CliRunner.cs
key_decisions:
  - ICliCommand interface placed in CliRunner.cs alongside the dispatcher for co-location
  - CliRunner returns -1 (not 0) for empty args to signal GUI mode to the caller
  - Flag-only args (--flag without value) stored as "true" string in options dictionary
duration: 
verification_result: passed
completed_at: 2026-04-23T15:30:50.392Z
blocker_discovered: false
---

# T01: Create CLI infrastructure: ConsoleHelper (WinExe P/Invoke), JsonlOutput (JSONL envelope serializer), CliRunner (arg parser + command dispatcher with ICliCommand interface)

**Create CLI infrastructure: ConsoleHelper (WinExe P/Invoke), JsonlOutput (JSONL envelope serializer), CliRunner (arg parser + command dispatcher with ICliCommand interface)**

## What Happened

Created three CLI infrastructure classes under the new `Cli/` folder:

1. **ConsoleHelper.cs** — P/Invoke wrapper using `AttachConsole(-1)` to attach to the parent process console when launched from cmd/PowerShell, without allocating a console window when double-clicked. This preserves the WinExe OutputType.

2. **JsonlOutput.cs** — JSONL formatting utility with a unified envelope schema (`{type, status, timestamp, data}`) using System.Text.Json with camelCase serialization. Provides `WriteHelp`, `WriteResult`, `WriteError`, `WriteSummary`, and `WriteRaw` methods. Each line is a self-contained JSON object parseable by agents.

3. **CliRunner.cs** — Hand-written argument parser and command dispatcher. Parses `args[0]` as subcommand name (inspect/fill/cleanup/help), remaining args as `--key value` pairs. Supports `--help`/`-h` (global and per-subcommand), `--version`/`-v`. Returns exit code -1 when no args provided (signals GUI mode). Defines `ICliCommand` interface for subcommand handlers to implement. Dispatches to registered `ICliCommand` implementations via ServiceProvider.

All three files compile cleanly with 0 errors and 0 warnings. The build succeeds end-to-end.

## Verification

`dotnet build` completed successfully with 0 errors and 0 warnings, confirming all three new files (Cli/ConsoleHelper.cs, Cli/JsonlOutput.cs, Cli/CliRunner.cs) compile without issues. No pre-existing build was broken.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2770ms |

## Deviations

None. All files match the task plan exactly.

## Known Issues

None.

## Files Created/Modified

- `Cli/ConsoleHelper.cs`
- `Cli/JsonlOutput.cs`
- `Cli/CliRunner.cs`
