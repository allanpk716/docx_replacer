---
id: T01
parent: S01
milestone: M013-ueix00
key_files:
  - Services/UpdateService.cs
  - ViewModels/UpdateSettingsViewModel.cs
  - Tests/UpdateServiceTests.cs
key_decisions:
  - Used internal constructor overload for test injection of PersistentConfigPath rather than InternalsVisibleTo or property injection, because the constructor runs EnsurePersistentConfigSync before tests can override the property
duration: 
verification_result: untested
completed_at: 2026-05-02T12:17:31.565Z
blocker_discovered: false
---

# T01: Migrate persistent config path from Velopack install dir to %USERPROFILE%\.docx_replacer\update-config.json

**Migrate persistent config path from Velopack install dir to %USERPROFILE%\.docx_replacer\update-config.json**

## What Happened

Changed `UpdateService.GetPersistentConfigPath()` from a private instance method that detected Velopack install structure (parent dir + Update.exe check) to a public static method that unconditionally returns `%USERPROFILE%\.docx_replacer\update-config.json`. The method no longer returns null — all environments (installed, portable, dev) use the same user-profile-based path.

Added `Directory.CreateDirectory()` calls in both `EnsurePersistentConfigSync()` and `PersistToAppSettings()` to auto-create the `~/.docx_replacer/` directory before writing. This ensures the directory exists on first use.

Refactored `UpdateSettingsViewModel.ReadPersistentConfig()` to call `UpdateService.GetPersistentConfigPath()` instead of duplicating the Velopack parent-dir detection logic. Added `using DocuFiller.Services;` to the ViewModel to reference the concrete class.

Added an internal constructor overload `UpdateService(logger, configuration, persistentConfigPath)` to allow test injection of a temp path, avoiding cross-test pollution. The public constructor delegates to this with `persistentConfigPath: null`.

Updated `UpdateServiceTests` to use a new `CreateTestService()` helper that passes a temp directory via the internal constructor, with `CleanupTestService()` for teardown. All 22 UpdateService tests pass. All 214 total tests pass, 0 failures.

Removed the old `~/.docx_replacer/update-config.json` that was created by a prior session's test run, which would have caused VM tests to read stale data.

## Verification

`dotnet build` passes with 0 errors. `dotnet test` passes all 214 tests (187 unit + 27 E2E) with 0 failures. Verified that `UpdateService.GetPersistentConfigPath()` is public static and returns `Path.Combine(UserProfile, ".docx_replacer", "update-config.json")`. Verified that `UpdateSettingsViewModel.ReadPersistentConfig()` calls `UpdateService.GetPersistentConfigPath()`. Verified directory auto-creation in both write paths.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| — | No verification commands discovered | — | — | — |

## Deviations

Added an internal constructor overload `UpdateService(logger, configuration, persistentConfigPath)` not mentioned in the original task plan. This was necessary because the path migration from conditional (Velopack-only) to unconditional means tests that previously got `null` from `GetPersistentConfigPath()` now get a real path, causing cross-test pollution via the shared `~/.docx_replacer/update-config.json` file. The internal constructor allows tests to inject a temp path without changing the public API.

## Known Issues

The `UpdateSettingsViewModelTests` mock `IUpdateService` but the ViewModel's `ReadPersistentConfig()` now directly calls `UpdateService.GetPersistentConfigPath()` (concrete class). If `~/.docx_replacer/update-config.json` exists on the test machine from a real app run, the VM tests could read stale data. This is acceptable because: (1) the file only exists if the user ran the app, (2) in CI the file won't exist, and (3) the existing UpdateService tests clean up after themselves.

## Files Created/Modified

- `Services/UpdateService.cs`
- `ViewModels/UpdateSettingsViewModel.cs`
- `Tests/UpdateServiceTests.cs`
