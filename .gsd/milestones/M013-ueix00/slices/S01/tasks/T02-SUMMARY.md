---
id: T02
parent: S01
milestone: M013-ueix00
key_files:
  - Tests/UpdateServiceTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-02T12:20:11.714Z
blocker_discovered: false
---

# T02: Add path verification tests for new PersistentConfigPath under %USERPROFILE%\.docx_replacer\update-config.json

**Add path verification tests for new PersistentConfigPath under %USERPROFILE%\.docx_replacer\update-config.json**

## What Happened

Added three new tests to `UpdateServiceTests.cs` verifying the persistent config path migration done in T01:

1. **GetPersistentConfigPath_returns_user_profile_path** — verifies the public static method returns a path under `%USERPROFILE%\.docx_replacer\update-config.json` with correct directory structure.

2. **EnsurePersistentConfigSync_creates_directory_and_file** — verifies that when `UpdateService` is constructed with a temp path, `EnsurePersistentConfigSync` auto-creates the directory and writes a valid JSON config file with the correct UpdateUrl and Channel values.

3. **ReadPersistentConfig_reads_from_persistent_path** — verifies that when a pre-existing config file exists at the injected path, the constructor reads it and the persisted values (custom HTTP URL + beta channel) override the IConfiguration defaults (empty URL + stable channel).

The `UpdateSettingsViewModelTests` were already passing (11 tests) — the ViewModel's `ReadPersistentConfig()` calls `UpdateService.GetPersistentConfigPath()` which returns a real path, but no file exists there in CI, so it falls through to IConfiguration as before. No changes needed to ViewModel tests.

Full test suite: 217 tests pass (190 unit + 27 E2E), 0 failures.

## Verification

`dotnet test` passes all 217 tests (190 unit + 27 E2E) with 0 failures. The 3 new UpdateService tests specifically verify path calculation, directory/file auto-creation, and config file reading.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter "UpdateService|UpdateSettingsViewModel"` | 0 | ✅ pass | 5000ms |
| 2 | `dotnet test` | 0 | ✅ pass | 15000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Tests/UpdateServiceTests.cs`
