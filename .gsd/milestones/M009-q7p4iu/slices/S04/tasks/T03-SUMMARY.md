---
id: T03
parent: S04
milestone: M009-q7p4iu
key_files:
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
key_decisions:
  - Used SemanticVersion.Parse() instead of System.Version for VelopackAsset.Version (Velopack requires NuGet.Versioning.SemanticVersion)
  - Created separate StubUpdateService in each test file to keep test classes self-contained and avoid shared mutable state
duration: 
verification_result: passed
completed_at: 2026-04-26T11:35:48.368Z
blocker_discovered: false
---

# T03: Add unit tests for UpdateCommand routing, JSONL output format, --yes mode, and post-command update reminder logic

**Add unit tests for UpdateCommand routing, JSONL output format, --yes mode, and post-command update reminder logic**

## What Happened

Created comprehensive unit tests for the UpdateCommand CLI feature and post-command update reminder hook.

**UpdateCommandTests.cs** (new file, 6 tests):
- `Update_Help_OutputsUpdateCommandHelp`: Verifies `update --help` outputs command help with type=command and name=update
- `Update_DispatchesToCorrectHandler`: Verifies `update` subcommand routes to UpdateCommand (not UNKNOWN_COMMAND or COMMAND_NOT_IMPLEMENTED)
- `Update_NoYes_OutputsVersionInfo`: Verifies version check outputs JSONL with type=update, hasUpdate=true, currentVersion, latestVersion, and a summary line with "发现新版本"
- `Update_NoYes_NoUpdate_OutputsAlreadyUpToDate`: Verifies no-update scenario outputs hasUpdate=false and "当前已是最新版本" summary
- `Update_WithYes_Portable_OutputsError`: Verifies IsInstalled=false + --yes outputs PORTABLE_NOT_SUPPORTED error
- `Update_WithYes_NoUpdate_ReturnsSuccess`: Verifies --yes with no update outputs "当前已是最新版本" with exit code 0

**CliRunnerTests.cs** (added 3 tests + helper classes):
- `PostCommand_UpdateAvailable_AppendsUpdateLine`: Successful command + update available → append type=update JSONL with reminder=true
- `PostCommand_NoUpdate_NoExtraLine`: Successful command + no update → no update-type lines in output
- `PostCommand_FailedCommand_NoUpdateLine`: Failed command → no update reminder appended even when update available

Key implementation details:
- Used Velopack's `VelopackAsset` and `UpdateInfo` types with `SemanticVersion.Parse()` for realistic stub data
- Created `StubUpdateService` implementing `IUpdateService` in each test file (UpdateCommandTests and CliRunnerTests) to keep test classes self-contained
- Followed existing test patterns: StringWriter capture, DI container creation, JSONL line parsing

## Verification

Ran `dotnet test --filter "UpdateCommand|PostCommand"` — all 9 new tests passed. Ran full test suite `dotnet test` — all 154 tests passed (9 new + 145 existing, zero regressions).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Tests/DocuFiller.Tests.csproj --filter "UpdateCommand|PostCommand"` | 0 | ✅ pass | 2370ms |
| 2 | `dotnet test Tests/DocuFiller.Tests.csproj` | 0 | ✅ pass | 12300ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
