---
id: T03
parent: S04
milestone: M023
key_files:
  - update-hub/deploy/install-service.bat
  - update-hub/deploy/uninstall-service.bat
  - update-hub/deploy/start-service.bat
  - update-hub/deploy/stop-service.bat
  - update-hub/deploy/README.md
key_decisions:
  - Used delayed expansion in install-service.bat for dynamic token/password argument assembly
  - Set AppRestartDelay to 5000ms (5 seconds) for quick recovery on crashes
duration: 
verification_result: passed
completed_at: 2026-05-05T07:43:24.655Z
blocker_discovered: false
---

# T03: Create NSSM deployment scripts and deployment README for Windows Server

**Create NSSM deployment scripts and deployment README for Windows Server**

## What Happened

Created 4 Windows batch scripts and a deployment README in `update-hub/deploy/`:

**install-service.bat** — Registers update-hub.exe as a Windows service named "UpdateHub" via NSSM. Checks for admin privileges and NSSM availability, prompts for API token and Web UI password, configures service parameters (AppDirectory, auto-start, restart delay 5s), and sets up log rotation (10MB per file, 5 copies retained) to `logs/` directory.

**uninstall-service.bat** — Stops and removes the NSSM service with confirmation.

**start-service.bat / stop-service.bat** — Simple wrappers around `nssm start/stop UpdateHub` with error handling and status messages.

**README.md** — Comprehensive deployment guide covering prerequisites (NSSM, executable placement), configuration (CLI arguments table), install steps, verification (browser and curl), automatic data migration from old DocuFiller format, update procedure (stop → replace exe → start), and log location/rotation details.

Build and full test suite pass with zero regressions. All 5 expected files created.

## Verification

Verified all 5 files exist in deploy/ directory (install-service.bat, uninstall-service.bat, start-service.bat, stop-service.bat, README.md). Verified build still compiles (`go build -o update-hub.exe .`). Verified full test suite passes (6 packages, 0 failures).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && ls deploy/*.bat deploy/README.md` | 0 | ✅ pass | 500ms |
| 2 | `cd update-hub && GOCACHE="$TEMP/go-cache" go build -o update-hub.exe .` | 0 | ✅ pass | 1318ms |
| 3 | `cd update-hub && GOCACHE="$TEMP/go-cache" go test ./... -count=1` | 0 | ✅ pass | 3581ms |

## Deviations

Adapted the verification command from `powershell -Command ...` to Unix-compatible `ls`/`head`/`test` commands since the executor runs in a Git Bash environment on Windows.

## Known Issues

None.

## Files Created/Modified

- `update-hub/deploy/install-service.bat`
- `update-hub/deploy/uninstall-service.bat`
- `update-hub/deploy/start-service.bat`
- `update-hub/deploy/stop-service.bat`
- `update-hub/deploy/README.md`
