---
id: T02
parent: S04
milestone: M009-q7p4iu
key_files:
  - Cli/CliRunner.cs
key_decisions:
  - Post-command reminder only fires for exitCode==0 AND non-update subcommands, avoiding double output when user explicitly runs update
duration: 
verification_result: passed
completed_at: 2026-04-26T11:28:49.792Z
blocker_discovered: false
---

# T02: Add post-command update reminder hook in CliRunner that conditionally appends type=update JSONL after successful non-update subcommands

**Add post-command update reminder hook in CliRunner that conditionally appends type=update JSONL after successful non-update subcommands**

## What Happened

Added `TryAppendUpdateReminderAsync` method to `CliRunner` and a post-command hook in `RunAsync`. After any successful (exitCode==0) non-update subcommand (inspect, fill, cleanup), the hook attempts to check for updates via `IUpdateService` (resolved lazily from `IServiceProvider`). Only when an update is available does it emit a `JsonlOutput.WriteUpdate` line with `reminder=true`, `latestVersion`, and a user-facing message. If already up-to-date, nothing is output. If `IUpdateService` is not registered (null) or the check throws, the hook silently skips — never affecting the original command's exit code. Added `using` imports for `DocuFiller.Services.Interfaces` and `Microsoft.Extensions.Logging` (the latter for consistency with other CLI commands, though not directly used here).

## Verification

Verified by `dotnet build -c Release` — 0 errors, 0 new warnings. All must-haves confirmed: (1) post-command hook only runs when exitCode==0 and subcommand is not "update", (2) only outputs JSONL when updateInfo is not null (new version available), (3) check exceptions are caught and silently skipped, (4) IUpdateService resolved via GetService with null check, (5) build passes with zero CS/MC errors.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -c Release` | 0 | ✅ pass | 2500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Cli/CliRunner.cs`
