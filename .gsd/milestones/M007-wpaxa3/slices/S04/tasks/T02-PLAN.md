---
estimated_steps: 16
estimated_files: 1
skills_used: []
---

# T02: Verify build pipeline, configuration wiring, and create E2E test guide

Run automated verification of the build pipeline and update service configuration, then create a comprehensive test guide document.

Verification checks:
1. `dotnet build DocuFiller.csproj -c Release` — 0 errors (regression safety)
2. `dotnet test` — all tests pass (regression safety)
3. IUpdateService DI registration exists in App.xaml.cs
4. appsettings.json has Update:UpdateUrl config node
5. UpdateService.cs implements all 4 IUpdateService members
6. Program.cs has VelopackApp.Build().Run() as first line
7. build-internal.bat has PublishSingleFile=true, IncludeNativeLibrariesForSelfExtract=true, vpk pack
8. No Chinese characters in any BAT file

Then create `docs/plans/e2e-update-test-guide.md` covering all 4 R026 verification scenarios:
1. Setup.exe installs and runs correctly
2. Portable.zip extracts and runs correctly
3. Update from old version to new version works (check update → confirm → download → restart)
4. User config files preserved after upgrade (appsettings.json, Logs/, Output/)

Each scenario should have: prerequisites, step-by-step procedure, expected result, pass/fail criteria.

## Inputs

- `App.xaml.cs`
- `appsettings.json`
- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Program.cs`
- `scripts/build-internal.bat`

## Expected Output

- `docs/plans/e2e-update-test-guide.md`

## Verification

bash -c 'dotnet build DocuFiller.csproj -c Release 2>&1 | tail -5' && dotnet test --verbosity minimal 2>&1 | tail -3 && test -f docs/plans/e2e-update-test-guide.md
