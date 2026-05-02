---
id: T01
parent: S01
milestone: M007-wpaxa3
key_files:
  - DocuFiller.csproj
  - Program.cs
  - appsettings.json
  - Services/Interfaces/IUpdateService.cs
  - Tests/DocuFiller.Tests.csproj
  - Tests/E2ERegression/E2ERegression.csproj
key_decisions:
  - Added Velopack NuGet to both test projects to satisfy wildcard-compiled IUpdateService.cs that references Velopack.UpdateInfo
duration: 
verification_result: passed
completed_at: 2026-04-24T05:38:55.713Z
blocker_discovered: false
---

# T01: Add Velopack 0.0.1298 NuGet, bootstrap VelopackApp.Build().Run() in Program.Main(), add Update:UpdateUrl config, create IUpdateService interface

**Add Velopack 0.0.1298 NuGet, bootstrap VelopackApp.Build().Run() in Program.Main(), add Update:UpdateUrl config, create IUpdateService interface**

## What Happened

Added Velopack NuGet package (v0.0.1298) to DocuFiller.csproj and both test projects (Tests/DocuFiller.Tests.csproj, Tests/E2ERegression/E2ERegression.csproj). The test projects needed the package because they use wildcard `<Compile Include="..\..\Services\Interfaces\*.cs" .../>` which auto-picks up the new IUpdateService.cs referencing Velopack's UpdateInfo type.

In Program.cs, added `using Velopack;` and placed `VelopackApp.Build().Run();` as the very first line of Main(), before the CLI/GUI branching. This ensures Velopack can intercept install/update/restart hooks before any WPF initialization.

In appsettings.json, added `"Update": { "UpdateUrl": "" }` section after the existing "UI" section. Empty default means "update not configured" — S02's IUpdateService implementation will read this.

Created Services/Interfaces/IUpdateService.cs with 4 members: CheckForUpdatesAsync (returns UpdateInfo?), DownloadUpdatesAsync (accepts UpdateInfo + optional progress callback), ApplyUpdatesAndRestart, and IsUpdateUrlConfigured property. This is the boundary contract for S02's Velopack UpdateManager implementation.

## Verification

dotnet build --verbosity quiet: 0 errors, 92 warnings (all pre-existing CS8602/CS8604 nullable warnings). dotnet test --verbosity minimal: 162 tests pass (135 + 27), 0 failures. grep -c "VelopackApp.Build" Program.cs returns 1. grep -c "UpdateUrl" appsettings.json returns 1. test -f Services/Interfaces/IUpdateService.cs confirms file exists.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --verbosity quiet` | 0 | ✅ pass | 2600ms |
| 2 | `dotnet test --verbosity minimal` | 0 | ✅ pass | 15000ms |
| 3 | `grep -c "VelopackApp.Build" Program.cs` | 0 | ✅ pass | 100ms |
| 4 | `grep -c "UpdateUrl" appsettings.json` | 0 | ✅ pass | 100ms |
| 5 | `test -f Services/Interfaces/IUpdateService.cs` | 0 | ✅ pass | 100ms |

## Deviations

Also added Velopack package reference to both test csprojs (Tests/DocuFiller.Tests.csproj and Tests/E2ERegression/E2ERegression.csproj) — not in original plan but required because these projects use wildcard source includes that pick up IUpdateService.cs which references Velopack types.

## Known Issues

None.

## Files Created/Modified

- `DocuFiller.csproj`
- `Program.cs`
- `appsettings.json`
- `Services/Interfaces/IUpdateService.cs`
- `Tests/DocuFiller.Tests.csproj`
- `Tests/E2ERegression/E2ERegression.csproj`
