---
estimated_steps: 18
estimated_files: 2
skills_used: []
---

# T01: Migrate persistent config path to ~/.docx_replacer/ in UpdateService and UpdateSettingsViewModel

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

## Inputs

- `Services/UpdateService.cs`
- `ViewModels/UpdateSettingsViewModel.cs`

## Expected Output

- `Services/UpdateService.cs`
- `ViewModels/UpdateSettingsViewModel.cs`

## Verification

dotnet build 2>&1 | tail -5

## Observability Impact

Logger init line already outputs PersistentConfigPath — will now show %USERPROFILE%\.docx_replacer\update-config.json. Directory auto-creation logged as Information. No new signals needed.
