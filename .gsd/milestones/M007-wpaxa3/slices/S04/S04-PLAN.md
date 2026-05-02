# S04: 端到端更新验证

**Goal:** Verify the complete Velopack auto-update flow end-to-end: build two versions, deploy to local HTTP server, and validate that Setup.exe installs correctly, Portable.zip runs correctly, update from old→new version works, and user config files are preserved after upgrade. Create automation scripts and a test guide so a human tester can execute the full verification.
**Demo:** 在干净 Windows 上安装旧版 → 检查更新 → 升级到新版 → 确认应用正常启动且用户配置文件保留

## Must-Haves

- `scripts/e2e-update-test.bat` and `scripts/e2e-serve.py` exist and pass syntax checks
- `docs/plans/e2e-update-test-guide.md` covers all 4 R026 verification scenarios
- dotnet build 0 errors, dotnet test all pass (regression safety)
- UpdateService DI registration and appsettings.json UpdateUrl wiring verified correct
- No Chinese characters in any BAT file

## Proof Level

- This slice proves: operational

## Integration Closure

Upstream surfaces consumed:
  - S02: Services/UpdateService.cs, IUpdateService DI registration, MainWindow.xaml StatusBar, MainWindowViewModel update commands
  - S03: scripts/build-internal.bat (PublishSingleFile + vpk pack pipeline)
New wiring: scripts/e2e-update-test.bat orchestrates S03 build pipeline + Python HTTP server to simulate update source
What remains: A human tester must run the E2E script on a clean Windows machine with vpk installed to fully validate R026

## Verification

- e2e-update-test.bat uses [PHASE_NAME] SUCCESS/FAILED echo tags matching S03 convention
- Python HTTP server logs each request (GET) to stdout for observing update check/download traffic
- Each verification step in the test guide has explicit pass/fail criteria

## Tasks

- [x] **T01: Create E2E update test automation script and HTTP update server** `est:45m`
  Create `scripts/e2e-update-test.bat` that orchestrates the full end-to-end update verification flow: builds two versions of DocuFiller (old=1.0.0 and new=1.1.0), packages both with vpk, starts a local Python HTTP server serving the new version's Velopack feed, and prints step-by-step instructions for the human tester to manually verify install→update→config-preservation.

Also create `scripts/e2e-serve.py` — a minimal Python HTTP server that serves Velopack releases (releases.win.json + .nupkg files) from a specified directory, logging each request to stdout.

The BAT script must:
1. Check prerequisites (vpk, python) and fail with clear install instructions if missing
2. Build version 1.0.0 by temporarily setting VERSION=1.0.0 (skip git tag requirement)
3. Run vpk pack for 1.0.0, save artifacts to e2e-test/v1.0.0/
4. Build version 1.1.0 with VERSION=1.1.0
5. Run vpk pack for 1.1.0, save artifacts to e2e-test/v1.1.0/
6. Copy v1.0.0 Setup.exe to e2e-test/ as the installer
7. Start e2e-serve.py serving e2e-test/v1.1.0/ on port 8080
8. Print the UpdateUrl (http://localhost:8080/) and manual test instructions
9. Wait for tester to press any key, then clean up

No Chinese characters in BAT files. Use echo tags like [E2E] for observability.
  - Files: `scripts/e2e-update-test.bat`, `scripts/e2e-serve.py`
  - Verify: bash -c 'test -f scripts/e2e-update-test.bat && test -f scripts/e2e-serve.py && python scripts/e2e-serve.py --help'

- [x] **T02: Verify build pipeline, configuration wiring, and create E2E test guide** `est:30m`
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
  - Files: `docs/plans/e2e-update-test-guide.md`
  - Verify: bash -c 'dotnet build DocuFiller.csproj -c Release 2>&1 | tail -5' && dotnet test --verbosity minimal 2>&1 | tail -3 && test -f docs/plans/e2e-update-test-guide.md

## Files Likely Touched

- scripts/e2e-update-test.bat
- scripts/e2e-serve.py
- docs/plans/e2e-update-test-guide.md
