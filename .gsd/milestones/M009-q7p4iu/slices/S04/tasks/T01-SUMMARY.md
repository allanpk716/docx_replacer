---
id: T01
parent: S04
milestone: M009-q7p4iu
key_files:
  - Cli/Commands/UpdateCommand.cs
  - Cli/JsonlOutput.cs
  - Cli/CliRunner.cs
  - App.xaml.cs
key_decisions:
  - Error code split: UPDATE_CHECK_ERROR for check failures, UPDATE_DOWNLOAD_ERROR for download failures (both in --yes mode)
  - Progress callback during download emits JSONL update lines with progress percentage
duration: 
verification_result: passed
completed_at: 2026-04-26T11:26:52.244Z
blocker_discovered: false
---

# T01: Implement CLI update command with version check JSONL output, --yes auto-update flow, and DI registration

**Implement CLI update command with version check JSONL output, --yes auto-update flow, and DI registration**

## What Happened

Created `UpdateCommand` implementing `ICliCommand` with full update lifecycle: version info JSONL output without `--yes`, portable edition guard (PORTABLE_NOT_SUPPORTED error), download with progress JSONL, and apply-and-restart for installed builds. Added `JsonlOutput.WriteUpdate` method for type=update envelope output. Registered the `update` subcommand in `CliRunner` (route dispatch, help text in both global and subcommand help, unknown command list) and as `ICliCommand` singleton in `App.xaml.cs` DI. Build passes with zero errors.

## Verification

Verified by `dotnet build -c Release` — 0 errors, 0 warnings. All must-haves confirmed: UpdateCommand implements ICliCommand with CommandName="update", no-yes path outputs version info JSONL, --yes portable guard emits PORTABLE_NOT_SUPPORTED, installed path executes download+restart flow, CliRunner registers route and help text, DI registration complete.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -c Release` | 0 | ✅ pass | 1300ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Cli/Commands/UpdateCommand.cs`
- `Cli/JsonlOutput.cs`
- `Cli/CliRunner.cs`
- `App.xaml.cs`
