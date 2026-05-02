# S01: 配置路径迁移到 ~/.docx_replacer/

**Goal:** 将 UpdateService 和 UpdateSettingsViewModel 中的持久化配置路径从 Velopack 安装目录迁移到 %USERPROFILE%\.docx_replacer\update-config.json，使配置完全独立于安装/更新生命周期。
**Demo:** UpdateService 和 UpdateSettingsViewModel 都从 ~/.docx_replacer/update-config.json 读写配置，dotnet build 通过

## Must-Haves

- GetPersistentConfigPath() 返回 %USERPROFILE%\.docx_replacer\update-config.json
- UpdateSettingsViewModel.ReadPersistentConfig() 使用相同路径
- 目录不存在时自动创建 ~/.docx_replacer/
- 便携版/开发环境也使用相同路径（不依赖 Update.exe 判断）
- dotnet build 0 errors, dotnet test 全部通过

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: IConfiguration (for fallback config reading), filesystem (for config file read/write)
- New wiring introduced in this slice: GetPersistentConfigPath() becomes a public static method that both UpdateService and UpdateSettingsViewModel call
- What remains before the milestone is truly usable end-to-end: S02 adds test coverage for the new path logic

## Verification

- Signals added/changed: Logger init line already outputs PersistentConfigPath — will now show %USERPROFILE%\.docx_replacer\update-config.json path. Directory auto-creation logged as Information. No new signals needed.
- How a future agent inspects this: Check log output for "持久化配置" line which includes the full path, or read the file at %USERPROFILE%\.docx_replacer\update-config.json
- Failure state exposed: Directory creation failure logged as warning; file read/write failure logged as debug/warning with fallback to appsettings.json

## Tasks

- [x] **T01: Migrate persistent config path to ~/.docx_replacer/ in UpdateService and UpdateSettingsViewModel** `est:30m`
  Change GetPersistentConfigPath() in UpdateService to always return %USERPROFILE%\.docx_replacer\update-config.json instead of detecting Velopack install structure. Extract it as a public static method so UpdateSettingsViewModel can reuse it without duplicating path computation logic. Auto-create ~/.docx_replacer/ directory when writing. Remove the Update.exe existence check entirely — the new path is unconditional for all environments (installed, portable, dev).

## Steps

1. In `Services/UpdateService.cs`:
   - Change `GetPersistentConfigPath()` from `private` to `public static`. Replace the entire body with: `var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); return Path.Combine(userProfile, ".docx_replacer", "update-config.json");`
   - Remove the `Update.exe` existence check — the method now always returns a valid path.
   - Keep the `PersistentConfigPath` internal property for test injection, but change its default initializer from calling the old `GetPersistentConfigPath()` to calling the new static method.
   - In `EnsurePersistentConfigSync()`: add `Directory.CreateDirectory(Path.GetDirectoryName(PersistentConfigPath)!)` before writing the file, so the ~/.docx_replacer/ directory is auto-created.
   - In `PersistToAppSettings()`: add the same `Directory.CreateDirectory()` call before writing to PersistentConfigPath.
   - Update the constructor's log message to reflect the new path location.

2. In `ViewModels/UpdateSettingsViewModel.cs`:
   - Replace the entire `ReadPersistentConfig()` method body. Instead of duplicating the Velopack parent-dir detection logic, call `UpdateService.GetPersistentConfigPath()` to get the path.
   - The new method body: get path from `UpdateService.GetPersistentConfigPath()`, check if file exists, read JSON, parse UpdateUrl and Channel.
   - This eliminates the code duplication between the two classes.

## Constraints
- Do NOT change the file format (still {"UpdateUrl":"...","Channel":"..."})
- Do NOT change the IUpdateService interface
- Do NOT add migration logic for old path
- Keep the existing fallback behavior: persistent config > appsettings.json > defaults
  - Files: `Services/UpdateService.cs`, `ViewModels/UpdateSettingsViewModel.cs`
  - Verify: dotnet build 2>&1 | tail -5

- [x] **T02: Update tests and add path verification tests for new config location** `est:45m`
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
  - Files: `Tests/UpdateServiceTests.cs`, `Tests/UpdateSettingsViewModelTests.cs`
  - Verify: dotnet test 2>&1 | tail -20

## Files Likely Touched

- Services/UpdateService.cs
- ViewModels/UpdateSettingsViewModel.cs
- Tests/UpdateServiceTests.cs
- Tests/UpdateSettingsViewModelTests.cs
