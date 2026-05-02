# S04: S04: 端到端更新验证 — UAT

**Milestone:** M007-wpaxa3
**Written:** 2026-04-24T06:40:56.833Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice produces automation scripts and documentation, not runtime behavior. The actual live E2E verification (installing on clean Windows, updating, confirming config preservation) is a manual step documented in the test guide. Verification here confirms the artifacts exist, are syntactically valid, and the build pipeline remains healthy.

## Preconditions

- Windows environment with .NET 8 SDK installed
- Python 3.x installed
- Velopack CLI (vpk) installed for running the E2E script
- Git available (used by build-internal.bat)

## Smoke Test

1. Run `python scripts/e2e-serve.py --help` — expect usage output with --port and --directory options
2. Verify `scripts/e2e-update-test.bat` exists and contains [E2E] echo tags

## Test Cases

### 1. E2E test script prerequisite checks

1. Open a terminal and run `scripts/e2e-update-test.bat`
2. If vpk or python is missing, script should print clear install instructions and exit with error
3. **Expected:** Prerequisites detected; script proceeds to build phases

### 2. E2E HTTP server serves Velopack artifacts

1. Run `python scripts/e2e-serve.py --port 8080 --directory e2e-test/v1.1.0/`
2. From another terminal, run `curl http://localhost:8080/releases.win.json` (or open in browser)
3. **Expected:** Server logs `[E2E-SERVE] GET /releases.win.json` and returns the JSON feed

### 3. Full E2E update flow (manual, requires clean Windows)

1. Run `scripts/e2e-update-test.bat` — it builds v1.0.0 and v1.1.0, starts the HTTP server
2. Install v1.0.0 Setup.exe from the e2e-test directory
3. Launch DocuFiller, verify it shows version 1.0.0 in status bar
4. Click "Check for Updates" — should detect v1.1.0 available
5. Confirm the update dialog, wait for download and restart
6. After restart, verify app shows version 1.1.0
7. **Expected:** Version updated from 1.0.0 to 1.1.0 successfully

### 4. User config preservation after upgrade

1. Before updating (at v1.0.0), modify appsettings.json (e.g., change Output directory)
2. Complete the update flow to v1.1.0
3. After restart, check that appsettings.json still has the custom Output directory
4. Check that Logs/ directory and its contents still exist
5. **Expected:** User configuration preserved unchanged after upgrade

## Edge Cases

### Script error recovery

1. Kill the Python HTTP server mid-test (Ctrl+C in server window)
2. Press any key in the BAT script window to proceed to cleanup
3. **Expected:** Script cleans up and restores original csproj version

### BAT file no Chinese characters

1. Run `python -c "import sys; data=open('scripts/e2e-update-test.bat','r',encoding='utf-8').read(); chars=[c for c in data if '\u4e00'<=c<='\u9fff']; sys.exit(1 if chars else 0)"`
2. **Expected:** Exit code 0 (no Chinese characters found)

## Failure Signals

- e2e-update-test.bat fails to find vpk or python → prerequisite not installed
- Build errors during version compilation → csproj modification issue or .NET SDK problem
- HTTP server fails to start → port 8080 already in use or directory doesn't exist
- Velopack update check fails → releases.win.json format incorrect or not served properly

## Not Proven By This UAT

- Actual live install→update→restart flow on a clean Windows machine (requires human tester with vpk CLI)
- Velopack delta update mechanism (full package replacement only tested)
- Network error handling during update download
- Windows UAC elevation during install

## Notes for Tester

- The E2E script temporarily modifies DocuFiller.csproj Version property — if the script crashes unexpectedly, manually restore the version
- The Python HTTP server runs in a separate window — keep it visible to observe update traffic
- Port 8080 must be available; change it in the BAT script if needed
- This is a self-contained verification slice — no runtime services or background processes to monitor
