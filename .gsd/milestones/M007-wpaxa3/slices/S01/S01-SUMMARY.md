---
id: S01
parent: M007-wpaxa3
milestone: M007-wpaxa3
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["DocuFiller.csproj", "Program.cs", "appsettings.json", "Services/Interfaces/IUpdateService.cs", "App.config", "scripts/build-internal.bat", "scripts/sync-version.bat", "Tests/DocuFiller.Tests.csproj", "Tests/E2ERegression/E2ERegression.csproj"]
key_decisions:
  - ["Velopack NuGet added to test csprojs due to wildcard Compile includes auto-picking up IUpdateService.cs"]
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-24T05:44:21.155Z
blocker_discovered: false
---

# S01: S01

**Added Velopack 0.0.1298 NuGet, bootstrapped VelopackApp in Program.Main(), created IUpdateService interface, cleaned all old update system residuals from config and scripts. All 162 tests pass.**

## What Happened

## What Was Built

**T01 — Velopack integration + interface contract:**
- Added `Velopack` NuGet package v0.0.1298 to DocuFiller.csproj and both test projects (needed because wildcard Compile includes auto-pick up the new IUpdateService.cs which references Velopack.UpdateInfo).
- Bootstrapped `VelopackApp.Build().Run()` as the very first line of `Program.Main()`, before CLI/GUI branching — this ensures Velopack can intercept install/update/restart hooks before any WPF initialization.
- Added `"Update": { "UpdateUrl": "" }` config section to `appsettings.json` (empty default = update not configured; S02 reads this).
- Created `Services/Interfaces/IUpdateService.cs` with 4 members: `CheckForUpdatesAsync(UpdateInfo?)`, `DownloadUpdatesAsync(UpdateInfo, Action<int>?)`, `ApplyUpdatesAndRestart()`, `IsUpdateUrlConfigured` property.

**T02 — Old update system residual cleanup:**
- Removed 3 old update entries (UpdateServerUrl, UpdateChannel, CheckUpdateOnStartup) and their XML comment from App.config.
- Removed COPY_EXTERNAL_FILES, PUBLISH_TO_SERVER, GET_RELEASE_NOTES function blocks from build-internal.bat; simplified mode validation to standalone-only; removed CHANNEL variable references.
- Removed update-client.config.yaml sync block from sync-version.bat.

## Key Decisions
- **Velopack NuGet in test projects**: Required because test csprojs use `<Compile Include="..\..\Services\Interfaces\*.cs" .../>` wildcard which auto-includes IUpdateService.cs referencing Velopack types.

## Deviations
- T01 added Velopack package reference to both test csprojs (not in original plan) — required for compilation.

## Patterns Established
- VelopackApp.Build().Run() must be the first line of Main() — this is the Velopack convention for update hook interception.
- IUpdateService interface follows the same Services/Interfaces/ location convention as all other service interfaces.
- appsettings.json Update:UpdateUrl with empty string = "not configured" pattern.

## Known Issues
None.

## Provides to Downstream Slices
- S02: Program.cs VelopackApp initialization, IUpdateService.cs interface contract, appsettings.json Update:UpdateUrl config node
- S03: DocuFiller.csproj Velopack NuGet reference, Program.cs VelopackApp initialization

## Verification

All slice-level verification checks passed:
1. dotnet build — 0 errors, 92 warnings (all pre-existing nullable)
2. VelopackApp.Build() found exactly once in Program.cs
3. UpdateUrl config node exists in appsettings.json
4. IUpdateService.cs exists with correct interface members
5. 0 old update system references (UpdateServerUrl, UpdateChannel, CheckUpdateOnStartup, COPY_EXTERNAL_FILES, update-client, publish-client) in App.config or any .bat scripts
6. dotnet test — 162 tests pass (135 DocuFiller.Tests + 27 E2ERegression), 0 failures

## Requirements Advanced

- R022 — VelopackApp.Build().Run() initialized, old update residuals cleaned from config and scripts
- R027 — 162 tests pass after Velopack integration and config cleanup

## Requirements Validated

- R022 — Velopack 0.0.1298 in csproj, VelopackApp.Build().Run() first in Main(), App.config/build-internal.bat/sync-version.bat cleaned, grep 0 old references, build 0 errors
- R027 — dotnet test 162/162 pass, 0 failures

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
