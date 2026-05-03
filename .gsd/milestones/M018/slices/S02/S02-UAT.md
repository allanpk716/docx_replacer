# S02: E2E 便携版更新测试 — UAT

**Milestone:** M018
**Written:** 2026-05-03T09:32:38.126Z

# UAT: S02 — E2E 便携版更新测试

## Preconditions
- Windows 10/11 with git-bash installed
- vpk CLI installed (`dotnet tool install -g vpk`)
- Python 3 available on PATH
- Go compiler available on PATH
- No processes on ports 8081 or 19081

## Test Case 1: Local HTTP Portable E2E Script — Syntax & Dry-Run

**Steps:**
1. Open CMD terminal
2. Run `cmd /c scripts\e2e-portable-update-test.bat --dry-run`
3. Verify: exit code 0, output contains "[E2E-PORTABLE]" tags, final line shows "OVERALL" summary

**Expected:** All phases print in dry-run mode, no errors, script exits cleanly.

## Test Case 2: Go Server Portable E2E Script — Syntax Check

**Steps:**
1. Open git-bash terminal
2. Run `bash -n scripts/e2e-portable-go-update-test.sh`
3. Verify: no syntax errors reported

**Expected:** Exit code 0, no output (clean syntax).

## Test Case 3: Full Local HTTP Portable Update E2E (Live)

**Steps:**
1. Ensure no process on port 8081 (`netstat -ano | findstr 8081`)
2. Open CMD terminal as administrator
3. Run `scripts\e2e-portable-update-test.bat`
4. Wait for all phases to complete (approx 5-10 minutes)
5. Check final summary output

**Expected:**
- v1.0.0 and v1.1.0 build and pack successfully
- e2e-serve.py starts and serves v1.1.0 artifacts
- `DocuFiller.exe update --yes` detects new version 1.1.0
- Download progress indicators appear in output
- Version upgrade verified (file version or log parsing)
- Summary shows PASS for key checkpoints
- Cleanup restores original csproj version and update-config.json

## Test Case 4: Full Go Server Portable Update E2E (Live)

**Steps:**
1. Ensure no process on port 19081
2. Open git-bash terminal
3. Run `bash scripts/e2e-portable-go-update-test.sh`
4. Wait for all phases to complete (approx 5-10 minutes)
5. Check final PASS/FAIL summary

**Expected:**
- Go server compiles and starts, responds to health checks
- v1.1.0 uploaded to stable channel, GET /stable/releases.win.json returns 1.1.0
- Portable v1.0.0 extracted, DocuFiller.exe and Update.exe present
- `DocuFiller.exe update --yes` completes with update indicators
- Summary shows PASS for key checkpoints
- Cleanup kills Go server, restores all files

## Test Case 5: Test Guide Documentation Completeness

**Steps:**
1. Open `docs/plans/e2e-update-test-guide.md`
2. Verify sections exist: "Automated Portable Update Tests", "Prerequisites for Portable Testing", "Portable Update via Local HTTP", "Portable Update via Go Server"
3. Verify troubleshooting table has portable-specific entries (port conflicts, ApplyUpdatesAndRestart, update-config.json path)

**Expected:** All 4 sections present, troubleshooting entries cover common portable issues.

## Edge Cases
- **Port conflict:** If port 8081 or 19081 is in use, scripts should detect and report failure early
- **vpk not installed:** Prerequisites check should fail gracefully with clear message
- **Interrupted build:** If dotnet build fails mid-version, csproj version must be restored on cleanup path
- **ApplyUpdatesAndRestart timeout:** If update process doesn't exit within timeout, script should still report findings from captured output
