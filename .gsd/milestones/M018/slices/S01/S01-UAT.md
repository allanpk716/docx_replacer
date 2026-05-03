# S01: S01: 便携版更新解锁 — UAT

**Milestone:** M018
**Written:** 2026-05-03T09:21:14.928Z

# S01: 便携版更新解锁 — UAT

**Milestone:** M018
**Written:** 2026-05-03

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a code-level change (removing blocking logic + adding property). Verification is entirely through build/test and code inspection. Runtime testing of the actual update flow requires S02's E2E test infrastructure.

## Preconditions

- .NET 8 SDK installed
- Project builds and all tests pass
- `appsettings.json` exists with Update configuration section

## Smoke Test

```bash
dotnet build && dotnet test
```
All 249 tests pass, 0 build errors.

## Test Cases

### 1. IUpdateService.IsPortable interface contract

1. Open `Services/Interfaces/IUpdateService.cs`
2. Search for `IsPortable`
3. **Expected:** Line 32 contains `bool IsPortable { get; }` with XML doc comment

### 2. UpdateService.IsPortable implementation

1. Open `Services/UpdateService.cs`
2. Search for `_isPortable`
3. **Expected:** Constructor reads `tempManager.IsPortable` and stores in `_isPortable` field; public property `IsPortable => _isPortable` exists; constructor log includes `IsPortable: {_isPortable}`

### 3. No PortableVersion blocking in GUI

1. Open `ViewModels/MainWindowViewModel.cs`
2. Search for `PortableVersion`
3. **Expected:** 0 matches — enum value deleted, all switch branches removed, no IsInstalled guard in InitializeUpdateStatusAsync

### 4. No PORTABLE_NOT_SUPPORTED in CLI

1. Open `Cli/Commands/UpdateCommand.cs`
2. Search for `PORTABLE_NOT_SUPPORTED`
3. **Expected:** 0 matches — IsInstalled guard removed, portable mode enters same download+apply path

### 5. Portable CLI test verifies normal flow

1. Run `dotnet test --filter "Update_WithYes_Portable_ProceedsNormally"`
2. **Expected:** Test passes — verifies exitCode 0, output contains update type, no PORTABLE_NOT_SUPPORTED in output

### 6. UpdateSettingsViewModelTests all pass

1. Run `dotnet test --filter "UpdateSettingsViewModelTests"`
2. **Expected:** All 12 tests pass — no interference from real persistent config file

### 7. Test stubs satisfy new interface

1. Search for `IsPortable` in `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` and `CliRunnerTests.cs`
2. **Expected:** Both contain `public bool IsPortable => false;`

## Edge Cases

### Null IConfiguration with UpdateSettingsViewModel

1. Constructor accepts `null!` for IConfiguration
2. **Expected:** Does not throw; UpdateUrl is empty string, Channel falls back to service channel

### UpdateService constructor catch block

1. If UpdateManager creation fails, `_isPortable` defaults to `false`
2. **Expected:** No NullReferenceException; safe fallback

## Failure Signals

- `dotnet build` returns errors
- `dotnet test` has failures
- `grep` finds residual "PortableVersion" or "PORTABLE_NOT_SUPPORTED" references
- Any IUpdateService implementation missing IsPortable property

## Not Proven By This UAT

- Actual portable version update execution (requires built Portable.zip and update server — covered by S02)
- Actual GUI status bar display on portable build
- Install version regression under real update conditions (covered by S02 E2E)

## Notes for Tester

- The UpdateSettingsViewModel constructor now has an optional 4th parameter (`readPersistentConfig` delegate) — this is a testability improvement, not a functional change. Production code never passes this parameter.
- `IsInstalled` property still exists on IUpdateService but is now a pure information property with no flow-control role.
- Decision D045 records the formal overturning of D029.
