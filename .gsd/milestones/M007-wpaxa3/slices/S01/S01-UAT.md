# S01: S01 — UAT

**Milestone:** M007-wpaxa3
**Written:** 2026-04-24T05:44:21.155Z

# UAT — S01: Velopack Integration + Old System Cleanup

## Precondition
- Repository at a clean state after T01+T02 merge
- .NET 8 SDK installed
- Windows build environment

## Test Cases

### TC-01: Build succeeds with Velopack
**Steps:**
1. Run `dotnet build --verbosity quiet`
**Expected:** Exit code 0, output contains no error lines

### TC-02: VelopackApp initialized first
**Steps:**
1. Open `Program.cs`
2. Verify `VelopackApp.Build().Run();` is the first executable statement in `Main()`
**Expected:** Present before `if (args.Length > 0)` and any WPF initialization

### TC-03: IUpdateService interface contract exists
**Steps:**
1. Open `Services/Interfaces/IUpdateService.cs`
2. Verify namespace is `DocuFiller.Services.Interfaces`
3. Verify members: `CheckForUpdatesAsync`, `DownloadUpdatesAsync`, `ApplyUpdatesAndRestart`, `IsUpdateUrlConfigured`
**Expected:** All 4 members present with correct signatures

### TC-04: Update config in appsettings.json
**Steps:**
1. Open `appsettings.json`
2. Verify `"Update"` section with `"UpdateUrl": ""` exists
**Expected:** Config node present with empty string default

### TC-05: Old update config removed from App.config
**Steps:**
1. Search App.config for `UpdateServerUrl`, `UpdateChannel`, `CheckUpdateOnStartup`
**Expected:** 0 matches

### TC-06: Old update references removed from scripts
**Steps:**
1. Search `scripts/` for `COPY_EXTERNAL_FILES`, `update-client`, `publish-client`, `PUBLISH_TO_SERVER`
**Expected:** 0 matches in any .bat file

### TC-07: All tests pass
**Steps:**
1. Run `dotnet test --verbosity minimal`
**Expected:** 162 total tests pass (135 + 27), 0 failures, 0 skipped

## Edge Cases
- None for this infrastructure-only slice (no runtime behavior changes)
