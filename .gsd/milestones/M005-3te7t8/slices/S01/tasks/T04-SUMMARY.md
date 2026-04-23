---
id: T04
parent: S01
milestone: M005-3te7t8
key_files:
  - Cli/CliRunner.cs
key_decisions:
  - Restructured arg dispatch to check subcommand before global --help to fix subcommand help routing
  - Used flat JSON serialization (WriteJsonLine) instead of envelope-wrapped JsonlOutput.WriteHelp() for help output to match plan spec
duration: 
verification_result: untested
completed_at: 2026-04-23T15:49:45.504Z
blocker_discovered: false
---

# T04: Implement --help JSONL output with flat type-tagged format for global and subcommand levels, plus --version

**Implement --help JSONL output with flat type-tagged format for global and subcommand levels, plus --version**

## What Happened

Rewrote the help output system in CliRunner.cs to match the task plan's specified JSONL format. The previous implementation used JsonlOutput.WriteHelp() which wrapped data in an envelope (type/status/timestamp/data), but the plan requires flat JSON objects with type at the top level.

Key changes:
1. Added `using System.Text.Json;` import for direct serialization
2. Rewrote `WriteGlobalHelp()` to output 5 JSONL lines: help overview, 3 command entries (fill, cleanup, inspect), and examples — matching the exact schema from the task plan
3. Rewrote `WriteSubCommandHelp()` to output a single command JSONL line for the requested subcommand
4. Rewrote `WriteVersion()` to output `{"type":"version","version":"1.0.0"}`
5. Added `WriteJsonLine()` helper for direct JSON serialization without envelope
6. Added `GetVersion()` helper to read assembly version with "1.0.0" fallback
7. Restructured the argument dispatch logic: subcommand detection now happens BEFORE global --help check, fixing a bug where `inspect --help` was incorrectly triggering global help instead of subcommand help

Also fixed the argument parsing order: the original code checked `IsHelp(args)` on the full args array first, which matched subcommand help flags like `inspect --help` as global help. The new logic first checks if args[0] is a subcommand (doesn't start with `-`), then handles subcommand --help within that branch, and only falls through to global --help for non-subcommand cases.

Discovered and documented (MEM047) that WPF apps in git worktrees need `-p:GenerateAssemblyInfo=true` to avoid assembly loading failures during InitializeComponent().

## Verification

Built with `dotnet build -c Debug -p:GenerateAssemblyInfo=true` — 0 errors.

Verified all help output scenarios using PowerShell/cmd execution:
- `DocuFiller.exe --help` → 5 JSONL lines: help + 3 commands (fill, cleanup, inspect) + examples ✅
- `DocuFiller.exe -h` → same global help output ✅
- `DocuFiller.exe inspect --help` → single inspect command JSONL line ✅
- `DocuFiller.exe fill --help` → single fill command JSONL line ✅
- `DocuFiller.exe cleanup --help` → single cleanup command JSONL line ✅
- `DocuFiller.exe --version` → `{"type":"version","version":"1.0.0"}` ✅
- `DocuFiller.exe -v` → same version output ✅

All JSONL output matches the exact format specified in the task plan, with correct type fields, option schemas (name/required/description), and examples.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| — | No verification commands discovered | — | — | — |

## Deviations

None — the JSONL format matches the task plan exactly. Also fixed a pre-existing bug in argument dispatch order where subcommand --help was caught by the global --help check.

## Known Issues

WPF apps in git worktrees require `-p:GenerateAssemblyInfo=true` build flag to avoid FileNotFoundException during InitializeComponent(). This is a worktree-specific issue — the main project directory is unaffected. Captured as MEM047.

## Files Created/Modified

- `Cli/CliRunner.cs`
