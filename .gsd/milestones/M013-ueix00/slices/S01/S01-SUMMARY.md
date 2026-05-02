---
id: S01
parent: M013-ueix00
milestone: M013-ueix00
provides:
  - ["UpdateService.GetPersistentConfigPath() — public static method returning %USERPROFILE%\\.docx_replacer\\update-config.json", "UpdateSettingsViewModel.ReadPersistentConfig() — refactored to use shared GetPersistentConfigPath()", "Auto-directory creation for ~/.docx_replacer/ on first write", "3 new tests covering path calculation, directory/file creation, and config reading"]
requires:
  []
affects:
  []
key_files:
  - ["Services/UpdateService.cs", "ViewModels/UpdateSettingsViewModel.cs", "Tests/UpdateServiceTests.cs"]
key_decisions:
  - ["Added internal constructor overload for test injection of PersistentConfigPath to avoid polluting real user config directory during tests"]
patterns_established:
  - ["Public static method for shared path computation (GetPersistentConfigPath) — single source of truth consumed by both service and ViewModel", "Internal constructor overload pattern for test injection when constructor has side effects that run before property override is possible"]
observability_surfaces:
  - ["Logger outputs PersistentConfigPath on service construction — shows actual config path being used", "Directory creation failure logged as Warning", "Config file read/write failure logged as Debug/Warning with fallback to appsettings.json"]
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-02T12:21:00.401Z
blocker_discovered: false
---

# S01: 配置路径迁移到 ~/.docx_replacer/

**Migrated persistent config path from Velopack install dir to %USERPROFILE%\.docx_replacer\update-config.json, shared by UpdateService and UpdateSettingsViewModel with auto-directory creation and full test coverage**

## What Happened

Changed `UpdateService.GetPersistentConfigPath()` from a private instance method that conditionally detected Velopack install structure (parent dir + Update.exe check) to a public static method that unconditionally returns `%USERPROFILE%\.docx_replacer\update-config.json`. This eliminates the root cause of the recurring bug where Velopack installation/update overwrites the config file.

Added `Directory.CreateDirectory()` calls in both `EnsurePersistentConfigSync()` and `PersistToAppSettings()` to auto-create `~/.docx_replacer/` on first write.

Refactored `UpdateSettingsViewModel.ReadPersistentConfig()` to call `UpdateService.GetPersistentConfigPath()` instead of duplicating path detection logic.

Added an internal constructor overload for test injection of temp PersistentConfigPath to avoid polluting the real user config directory during test runs.

Added 3 new tests: path format verification, directory/file auto-creation, and config file reading with value override. Full suite: 244 tests pass (217 unit + 27 E2E), 0 failures, 0 build errors.

## Verification

`dotnet build` — 0 errors, 95 warnings (pre-existing). `dotnet test` — 244 tests pass (217 unit + 27 E2E), 0 failures. Verified GetPersistentConfigPath() is public static returning Path.Combine(UserProfile, ".docx_replacer", "update-config.json"). Verified UpdateSettingsViewModel.ReadPersistentConfig() calls UpdateService.GetPersistentConfigPath(). Verified directory auto-creation in both write paths. 3 new path-specific tests all pass.

## Requirements Advanced

- R056 — Changed GetPersistentConfigPath() to unconditionally return %USERPROFILE%\.docx_replacer\update-config.json; removed Velopack install dir dependency; both GUI (UpdateSettingsViewModel) and service (UpdateService) share the same path

## Requirements Validated

- R056 — dotnet build 0 errors; dotnet test 244 pass; GetPersistentConfigPath() returns ~/.docx_replacer/update-config.json unconditionally; both UpdateService and UpdateSettingsViewModel use same path; no Update.exe dependency

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Added an internal constructor overload UpdateService(logger, configuration, persistentConfigPath) not in the original plan. Necessary because the path migration from conditional (Velopack-only) to unconditional means tests that previously got null now get a real path, causing cross-test pollution. The internal constructor allows temp path injection without changing the public API.

## Known Limitations

No migration logic for existing config files at the old Velopack install directory — users who already have config there will need to re-enter settings once.

## Follow-ups

None.

## Files Created/Modified

- `Services/UpdateService.cs` — GetPersistentConfigPath() changed to public static returning %USERPROFILE%\.docx_replacer\update-config.json; added Directory.CreateDirectory() in write paths; added internal constructor for test injection
- `ViewModels/UpdateSettingsViewModel.cs` — ReadPersistentConfig() refactored to call UpdateService.GetPersistentConfigPath() instead of duplicating Velopack detection logic
- `Tests/UpdateServiceTests.cs` — Added CreateTestService/CleanupTestService helpers for temp path injection; added 3 new tests for path calculation, directory creation, and config reading
