---
estimated_steps: 23
estimated_files: 2
skills_used: []
---

# T02: Update tests and add path verification tests for new config location

Update existing tests to work with the new unconditional PersistentConfigPath, and add new tests verifying the path calculation and file operations.

## Steps

1. Update `Tests/UpdateServiceTests.cs`:
   - Existing tests use `InMemoryConfiguration` only (no PersistentConfigPath in test env). Since GetPersistentConfigPath() now returns a real path under %USERPROFILE%, the `PersistentConfigPath` property will be non-null in tests.
   - For tests that call `ReloadSource()`: the method now writes to the real ~/.docx_replacer/ directory. To avoid polluting the user's real config, set `service.PersistentConfigPath` to a temp file path (same pattern as `AppSettingsPath` injection). Update the following tests to inject a temp path:
     - `ReloadSource_http_changes_source_type_to_HTTP`
     - `ReloadSource_empty_changes_source_type_to_GitHub`
     - `ReloadSource_updates_channel`
     - `ReloadSource_null_url_treated_as_empty`
     - `ReloadSource_null_channel_defaults_to_stable`
     - `ReloadSource_with_trailing_slash_no_double_slash`
     - `ReloadSource_channel_with_whitespace_trimmed`
   - Add new test: `GetPersistentConfigPath_returns_user_profile_path` — verify the static method returns a path ending in `.docx_replacer\update-config.json` and containing the user profile directory.
   - Add new test: `EnsurePersistentConfigSync_creates_directory_and_file` — set PersistentConfigPath to a temp path, verify directory creation and file content.
   - Add new test: `ReadPersistentConfig_reads_from_persistent_path` — create a temp config file, set PersistentConfigPath, verify ReadPersistentConfig returns correct values.

2. Update `Tests/UpdateSettingsViewModelTests.cs`:
   - The ViewModel's `ReadPersistentConfig()` now calls `UpdateService.GetPersistentConfigPath()` which returns a real path. If no file exists there, it returns (null, null) which falls through to IConfiguration — this is the existing behavior for tests, so existing tests should still pass.
   - Verify by running all tests. If any fail due to the new path logic, adjust.

3. Run `dotnet test` to verify all tests pass.

## Constraints
- Tests must not write to the real ~/.docx_replacer/ directory — use temp paths via PersistentConfigPath injection
- All existing tests must continue to pass
- Clean up temp files in test teardown

## Inputs

- `Services/UpdateService.cs`
- `ViewModels/UpdateSettingsViewModel.cs`
- `Tests/UpdateServiceTests.cs`
- `Tests/UpdateSettingsViewModelTests.cs`

## Expected Output

- `Tests/UpdateServiceTests.cs`
- `Tests/UpdateSettingsViewModelTests.cs`

## Verification

dotnet test 2>&1 | tail -20
