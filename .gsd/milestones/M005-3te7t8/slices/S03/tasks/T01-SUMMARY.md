---
id: T01
parent: S03
milestone: M005-3te7t8
key_files:
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
  - Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs
  - Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs
  - Tests/DocuFiller.Tests.csproj
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T16:35:39.061Z
blocker_discovered: false
---

# T01: Add 37 unit tests for CLI components (CliRunner routing, command validation, JsonlOutput format)

**Add 37 unit tests for CLI components (CliRunner routing, command validation, JsonlOutput format)**

## What Happened

Created three test files for CLI unit testing:

**JsonlOutputTests.cs** (10 tests): Tests WriteResult/WriteError/WriteSummary JSON envelope structure (type, status, timestamp, data fields), single-line JSON output, ISO 8601 timestamps, null code handling, and WriteRaw passthrough.

**CliRunnerTests.cs** (13 tests): Tests CliRunner routing — empty args returns -1 (GUI mode), --help/-h outputs multi-line global help JSON, --version/-v outputs version JSON, unknown commands produce UNKNOWN_COMMAND errors, fill/cleanup/inspect --help outputs subcommand help, dispatch to correct ICliCommand implementations via DI, and unregistered commands produce COMMAND_NOT_IMPLEMENTED errors.

**CommandValidationTests.cs** (14 tests): Tests parameter validation for all three commands — FillCommand missing template/data/output → MISSING_ARGUMENT, nonexistent template/data files → FILE_NOT_FOUND, empty values → MISSING_ARGUMENT; CleanupCommand missing input → MISSING_ARGUMENT, nonexistent file → FILE_NOT_FOUND; InspectCommand missing template → MISSING_ARGUMENT, nonexistent file → FILE_NOT_FOUND. Exit codes verified as 1 for all validation failures.

Key implementation decisions:
- Used `[assembly: CollectionBehavior(DisableTestParallelization = true)]` to prevent Console.SetOut cross-class interference in xUnit parallel execution.
- Created StubDocumentProcessor, StubExcelDataParser, StubCleanupService (all throw NotImplementedException) for command validation tests since validation occurs before service calls.
- Created StubCommand (implements ICliCommand) for CliRunner routing tests with configurable CommandName and ExecuteFn.
- Used NullLogger<T> for command constructors to avoid LoggerFactory setup overhead.
- Added Microsoft.Extensions.DependencyInjection 10.0.1 for ServiceCollection/GetServices<T> in routing tests.
- Fixed Windows \r\n line ending in single-line JSON assertions.

## Verification

All 37 CLI tests pass via `dotnet test --filter "CliRunnerTests|CommandValidationTests|JsonlOutputTests"`. Full test suite (108 tests) passes with 0 errors.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter "CliRunnerTests|CommandValidationTests|JsonlOutputTests" --verbosity normal` | 0 | ✅ pass | 3500ms |
| 2 | `dotnet test --verbosity normal` | 0 | ✅ pass (108 total tests) | 3500ms |

## Deviations

None. All tests follow the task plan exactly.

## Known Issues

None.

## Files Created/Modified

- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
- `Tests/DocuFiller.Tests/Cli/JsonlOutputTests.cs`
- `Tests/DocuFiller.Tests.csproj`
