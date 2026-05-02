# S01: 配置路径迁移到 ~/.docx_replacer/ — UAT

**Milestone:** M013-ueix00
**Written:** 2026-05-02T12:21:00.402Z

# S01: 配置路径迁移到 ~/.docx_replacer/ — UAT

**Milestone:** M013-ueix00
**Written:** 2026-05-02

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice changes a file path — the proof is in code inspection (path calculation) and automated tests (build + 244 tests pass). No runtime server or live UI interaction needed.

## Preconditions

- .NET 8 SDK installed
- Project builds successfully (`dotnet build`)
- %USERPROFILE% environment variable is accessible

## Smoke Test

Run `dotnet test` — all 244 tests pass with 0 failures.

## Test Cases

### 1. Config path calculation

1. Inspect `UpdateService.GetPersistentConfigPath()` source code
2. Call the static method in a test or debugger
3. **Expected:** Returns `%USERPROFILE%\.docx_replacer\update-config.json` — path contains user profile directory, ends with `.docx_replacer\update-config.json`, no null return

### 2. ViewModel uses shared path

1. Inspect `UpdateSettingsViewModel.ReadPersistentConfig()` source code
2. Verify it calls `UpdateService.GetPersistentConfigPath()` (not duplicated logic)
3. **Expected:** No path duplication — single source of truth for config path

### 3. Directory auto-creation on write

1. Delete `%USERPROFILE%\.docx_replacer\` if it exists
2. Run `dotnet test --filter "EnsurePersistentConfigSync_creates_directory_and_file"`
3. **Expected:** Test passes — directory created, file written with valid JSON

### 4. Config read overrides defaults

1. Run `dotnet test --filter "ReadPersistentConfig_reads_from_persistent_path"`
2. **Expected:** Test passes — persisted values (custom URL + beta channel) correctly override IConfiguration defaults

### 5. Full test suite regression

1. Run `dotnet test`
2. **Expected:** All 244 tests pass, 0 failures (217 unit + 27 E2E)

## Edge Cases

### No existing config file

1. Ensure `%USERPROFILE%\.docx_replacer\update-config.json` does not exist
2. Construct UpdateService with default IConfiguration
3. **Expected:** Service initializes with defaults from IConfiguration, no error, no exception

### Path with non-Latin characters in UserProfile

1. The path uses `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)` which handles non-ASCII paths
2. **Expected:** Path is correctly computed regardless of username characters

## Failure Signals

- `dotnet build` reports errors in UpdateService.cs or UpdateSettingsViewModel.cs
- `dotnet test` shows any failures in UpdateService tests
- Stale config read from old Velopack install directory (indicates migration incomplete)

## Not Proven By This UAT

- Actual Velopack install/update cycle (requires live Velopack environment)
- GUI save-and-reload round-trip (requires WPF runtime)
- CLI reading from the new path (requires .exe execution, not test runner)
- Migration of existing config from old path to new path (explicitly out of scope)

## Notes for Tester

- The internal constructor overload for test injection is a deviation from the original plan but necessary — it prevents tests from writing to the real user config directory
- No migration logic was added for the old Velopack install directory path — this is intentional per the task constraints
- If `~/.docx_replacer/update-config.json` exists from a real app run, ViewModel tests still pass because they fall through to IConfiguration when no file exists (CI) or read the real file (local dev — acceptable)
