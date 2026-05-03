# DocuFiller E2E Update Test Guide

This guide covers the full end-to-end verification of the Velopack auto-update flow for DocuFiller. It validates four scenarios: Setup.exe installation, Portable.zip execution, version upgrade via auto-update, and user config preservation after upgrade.

## Prerequisites

Before starting, ensure the following tools are installed:

| Tool      | Install Command / URL                                      |
|-----------|-------------------------------------------------------------|
| .NET 8 SDK | https://dotnet.microsoft.com/download/dotnet/8.0           |
| vpk       | `dotnet tool install -g vpk`                               |
| Python 3  | https://www.python.org/downloads/                           |
| Git       | https://git-scm.com/downloads/win                          |

Verify all tools are available:

```bat
vpk --version
python --version
dotnet --version
```

---

## Automated Environment Setup

Run the provided automation script to build both versions and start the update server:

```bat
cd scripts
e2e-update-test.bat
```

This script will:
1. Build v1.0.0 and v1.1.0 from the current source
2. Package both with Velopack (vpk pack)
3. Copy `DocuFiller-Setup-v1.0.0.exe` to the `e2e-test` root
4. Start `e2e-serve.py` on port 8080 serving the v1.1.0 artifacts
5. Print manual test instructions

The HTTP server window will show `[E2E-SERVE] GET ...` entries each time Velopack checks for updates or downloads a package — use this to observe update traffic in real time.

### Manual Server Start (Alternative)

If you need to start the server separately:

```bat
python scripts/e2e-serve.py --port 8080 --directory e2e-test\v1.1.0
```

---

## Test Scenarios

### Scenario 1: Setup.exe Installs and Runs Correctly

**Purpose:** Verify that the Velopack Setup.exe installer works correctly for a clean install.

**Prerequisites:**
- `e2e-update-test.bat` completed successfully (Phase 2 OK)
- No existing DocuFiller installation on the test machine

**Procedure:**

1. Navigate to `e2e-test\` in File Explorer
2. Double-click `DocuFiller-Setup-v1.0.0.exe`
3. Follow the installer wizard (accept defaults)
4. After installation completes, the app should launch automatically
5. Verify the application window appears
6. Close the application

**Expected Result:**
- Installer completes without errors
- Application launches and displays the main window
- Application is registered in Start Menu and Add/Remove Programs

**Pass/Fail Criteria:**

| Check | Pass Condition |
|-------|---------------|
| Installer completes | No error dialogs during installation |
| App launches | Main window visible after install |
| Start Menu entry | "DocuFiller" appears in Start Menu |
| Uninstall entry | DocuFiller visible in Settings > Apps |

---

### Scenario 2: Portable.zip Extracts and Runs Correctly

**Purpose:** Verify that the Portable.zip distribution works without installation.

**Prerequisites:**
- `e2e-update-test.bat` completed successfully (Phase 2 OK)

**Procedure:**

1. Navigate to `e2e-test\v1.0.0\` in File Explorer
2. Find `DocuFiller-Portable.zip`
3. Extract the ZIP to a new folder (e.g., `e2e-test\portable-test\`)
4. Open the extracted folder
5. Double-click `DocuFiller.exe`
6. Verify the application window appears
7. Close the application

**Expected Result:**
- ZIP extracts without errors
- Application runs directly from the extracted folder
- No installation or registry changes required

**Pass/Fail Criteria:**

| Check | Pass Condition |
|-------|---------------|
| ZIP extraction | No CRC or corruption errors |
| App launches | Main window visible after double-click |
| No install needed | App runs from any folder location |
| Single executable | Only DocuFiller.exe and supporting files in folder |

---

### Scenario 3: Update from Old Version to New Version

**Purpose:** Verify the complete auto-update flow: check → download → apply → restart.

**Prerequisites:**
- Scenario 1 completed (v1.0.0 installed via Setup.exe)
- HTTP update server running (`e2e-serve.py` on port 8080)

**Procedure:**

1. **Configure the update URL:**
   - Open the installed app's `appsettings.json`
   - Default path: `%LOCALAPPDATA%\DocuFiller\appsettings.json`
   - Set `Update.UpdateUrl` to `http://localhost:8080/`
   - Save the file

   Example `appsettings.json`:
   ```json
   {
     "Update": {
       "UpdateUrl": "http://localhost:8080/"
     }
   }
   ```

2. **Launch the installed v1.0.0 application:**
   - Start from Start Menu or desktop shortcut
   - Verify the app shows version 1.0.0 (check title bar or About dialog)

3. **Trigger update check:**
   - Click "Check for Updates" (or the app checks automatically on startup)
   - Observe the HTTP server console — you should see:
     ```
     [E2E-SERVE] GET /releases.win.json from 127.0.0.1
     ```

4. **Confirm update available:**
   - The app should display a notification: "Update available: v1.1.0"

5. **Download and apply update:**
   - Click "Download and Install" (or equivalent update button)
   - Observe the HTTP server console — you should see:
     ```
     [E2E-SERVE] GET /DocuFiller-1.1.0-full.nupkg from 127.0.0.1
     ```
   - A progress bar or download indicator should appear

6. **Verify restart:**
   - After download completes, the app should restart automatically
   - Verify the restarted app shows version 1.1.0

**Expected Result:**
- Update check finds v1.1.0
- Download completes without errors
- Application restarts automatically as v1.1.0
- HTTP server log shows both the feed request and the nupkg download

**Pass/Fail Criteria:**

| Check | Pass Condition |
|-------|---------------|
| Update check | HTTP server logs `GET /releases.win.json` |
| Update found | App shows "Update available" notification |
| Download | HTTP server logs `GET /DocuFiller-1.1.0-full.nupkg` |
| Download completes | No error during download |
| Restart | App restarts automatically after download |
| Version upgraded | Restarted app shows version 1.1.0 |
| No crash | Application is fully functional after upgrade |

---

### Scenario 4: User Config Files Preserved After Upgrade

**Purpose:** Verify that user configuration and data files survive the upgrade process.

**Prerequisites:**
- Scenario 3 completed (app upgraded from v1.0.0 to v1.1.0)
- Before upgrading, create test data (see pre-upgrade steps below)

**Pre-Upgrade Data Setup (do this while v1.0.0 is running):**

1. Open a template file in DocuFiller v1.0.0
2. Fill in some data and generate an output document
3. Note the output file location (typically in the `Output` directory)
4. Close the application
5. Record the contents of `appsettings.json` from the install directory
6. Check that the `Logs` directory contains log entries

**Post-Upgrade Verification:**

After the upgrade to v1.1.0 completes:

1. **Check appsettings.json:**
   - Open `%LOCALAPPDATA%\DocuFiller\appsettings.json`
   - Verify the `Update.UpdateUrl` is still `http://localhost:8080/`
   - Verify all other custom settings are preserved

2. **Check Output directory:**
   - Navigate to the output directory used before the upgrade
   - Verify the previously generated output files still exist
   - Verify files are unchanged (same size, same content)

3. **Check Logs directory:**
   - Navigate to `%LOCALAPPDATA%\DocuFiller\Logs\`
   - Verify log files from before the upgrade still exist
   - Verify new log entries are being appended (upgrade + restart logs)

4. **Check application functionality:**
   - Open the same template file used before the upgrade
   - Verify all settings and recent files are preserved
   - Generate a new output document to confirm the app works fully

**Expected Result:**
- All user configuration files are preserved
- Output files from before the upgrade are intact
- Log history is maintained
- The upgraded application functions normally with preserved settings

**Pass/Fail Criteria:**

| Check | Pass Condition |
|-------|---------------|
| appsettings.json preserved | UpdateUrl and custom settings unchanged |
| Output files preserved | Pre-upgrade output files exist and are unchanged |
| Logs preserved | Historical log files still present |
| New logs written | Post-upgrade log entries appear |
| App functional | Can open templates and generate output after upgrade |
| Settings retained | UI preferences and configured paths still correct |

---

## Automated Portable Update Tests

The following automated scripts validate the portable app self-update flow end-to-end. Unlike the manual scenarios above, these scripts build both versions, configure the update source, trigger the update, and verify the version upgrade automatically.

### Prerequisites for Portable Testing

| Requirement | Details |
|-------------|---------|
| Portable extraction | The scripts extract `DocuFiller-Portable.zip` automatically — no manual extraction needed |
| update-config.json | Created automatically by the scripts at `%USERPROFILE%\.docx_replacer\update-config.json` (existing file is backed up and restored on cleanup) |
| Port availability | Local HTTP test uses port 8081; Go server test uses port 19081 — ensure neither is in use |
| Additional tools (Go test only) | `go` (to build the update server binary), `curl` (to upload releases to the server) |

### Portable Update via Local HTTP

**Script:** `scripts/e2e-portable-update-test.bat`

**Purpose:** Verify the complete portable self-update chain against a local Python HTTP server (same model as the manual Scenario 3, but fully automated).

**What it does:**
1. Checks prerequisites (vpk, python, dotnet)
2. Builds v1.0.0 and v1.1.0 from source, packs both with Velopack
3. Extracts the v1.0.0 Portable.zip to a clean directory
4. Creates `update-config.json` pointing to `http://localhost:8081/`
5. Starts `e2e-serve.py` serving v1.1.0 artifacts on port 8081
6. Runs `DocuFiller.exe update --yes` with a 30s timeout
7. Verifies the version upgrade succeeded (exe file version check)
8. Cleans up: stops HTTP server, restores config, restores csproj version

**Usage:**
```bat
cd scripts
e2e-portable-update-test.bat
```

**Dry-run mode** (syntax validation only, no builds):
```bat
e2e-portable-update-test.bat --dry-run
```

**Output tags:** All output lines are tagged `[E2E-PORTABLE]` for easy filtering. The final summary shows PASS/PARTIAL PASS/FAIL per check.

**Key design notes:**
- Uses port **8081** (not 8080) to avoid conflict with the existing `e2e-update-test.bat`
- Handles `ApplyUpdatesAndRestart` by running the update process in background with `start /b` and polling for process exit
- No Chinese characters in the script per project BAT convention

**Pass/Fail Criteria:**

| Check | Pass Condition |
|-------|---------------|
| Build & pack v1.0.0 | Velopack packaging completes without errors |
| Build & pack v1.1.0 | Velopack packaging completes without errors |
| Portable extraction | DocuFiller.exe and Update.exe present after extraction |
| HTTP server started | `curl http://localhost:8081/releases.win.json` returns valid feed |
| Update applied | DocuFiller.exe file version upgraded to 1.1.0, or JSONL log shows download+apply indicators |
| Cleanup | HTTP server stopped, update-config.json restored, csproj version restored |

---

### Portable Update via Go Server

**Script:** `scripts/e2e-portable-go-update-test.sh` (requires bash — use Git Bash on Windows)

**Purpose:** Verify the complete portable self-update chain against the internal Go update server, proving the full API round-trip: upload release → server stores → client checks → client downloads → client applies.

**What it does:**
1. Builds the Go update server binary from `update-server/` source
2. Starts the server on port 19081 with a test token and temp data directory
3. Builds v1.0.0 and v1.1.0 from C# source, packs with Velopack
4. Uploads v1.1.0 artifacts (.nupkg + releases.win.json) to the server's `stable` channel via authenticated curl POST
5. Verifies `GET /stable/releases.win.json` contains version 1.1.0
6. Extracts v1.0.0 Portable.zip to a clean directory
7. Creates `update-config.json` pointing to `http://localhost:19081/` with channel `stable`
8. Runs `DocuFiller.exe update --yes` with a 60s timeout
9. Parses output for update-found / download / apply indicators
10. Cleans up via `trap EXIT`: kills server, restores config, restores csproj version, removes temp dirs

**Usage:**
```bash
bash scripts/e2e-portable-go-update-test.sh
```

**Output tags:** All output lines are tagged `[E2E-GO]`. The final summary shows PASS/FAIL counts and tails the server log for diagnostics.

**Key design notes:**
- Uses port **19081** to avoid conflict with `e2e-dual-channel-test.sh` (19080) and the local HTTP test (8081)
- Reuses the `upload_version` helper pattern from `e2e-dual-channel-test.sh` with proper `releases.win.json` multipart filename
- Handles `ApplyUpdatesAndRestart` via background process with `kill -0` polling (git-bash compatible)
- Uses `sed -i` for csproj version manipulation, with PowerShell fallback for zip extraction if `unzip` is unavailable

**Pass/Fail Criteria:**

| Check | Pass Condition |
|-------|---------------|
| Go server build | `go build` completes without errors |
| Go server started | Health check returns 200 within retry limit |
| Build & pack v1.0.0 + v1.1.0 | Velopack packaging completes without errors |
| Release upload | curl POST to `/api/channels/stable/releases` returns 200 |
| Feed verification | `GET /stable/releases.win.json` contains version 1.1.0 |
| Portable extraction | DocuFiller.exe and Update.exe present |
| Update applied | JSONL log shows download+apply, or exe file version upgraded to 1.1.0 |
| Cleanup | Server killed, config restored, csproj version restored |

---

## Cleanup

After testing:

1. Close the HTTP server (press Ctrl+C in the server window, or the test script closes it on exit)
2. Uninstall DocuFiller: Settings > Apps > DocuFiller > Uninstall
3. Remove test artifacts:
   ```bat
   rmdir /s /q e2e-test
   ```
4. The `e2e-update-test.bat` script restores the original csproj version automatically

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| "vpk not found" | Velopack CLI not installed | `dotnet tool install -g vpk` |
| HTTP server fails to start | Port 8080 in use | Change `--port` in both e2e-update-test.bat and appsettings.json |
| No update detected | appsettings.json UpdateUrl not set | Set `Update.UpdateUrl` to `http://localhost:8080/` |
| Update download fails | Server not running or wrong directory | Restart e2e-serve.py, verify it serves v1.1.0 directory |
| App does not restart after update | Velopack hooks not registered | Ensure Setup.exe was used for initial install (not Portable) |
| Config lost after update | Installed via Portable.zip | Portable mode does not use Velopack install hooks; use Setup.exe |
| Portable update script "port in use" | Another test using the same port | Kill stale processes or change the port constant in the script |
| e2e-portable-go-update-test.sh fails "go not found" | Go toolchain not installed | Install Go 1.21+ from https://go.dev/dl/ |
| Go server upload returns 401 | Test token mismatch | Verify the token constant in the script matches the server's --token flag |
| Portable update exits immediately | ApplyUpdatesAndRestart killed the process | Expected behavior — the script handles this via background process + timeout + multi-strategy verification |
| update-config.json not found by app | Wrong config directory path | Ensure `%USERPROFILE%\.docx_replacer\update-config.json` exists; scripts create it automatically |
| unzip fails in bash script | unzip not available in Git Bash | Script falls back to PowerShell extraction automatically |

## Artifact Locations

| Artifact | Path |
|----------|------|
| v1.0.0 installer | `e2e-test\DocuFiller-Setup-v1.0.0.exe` |
| v1.0.0 portable | `e2e-test\v1.0.0\DocuFiller-Portable.zip` |
| v1.1.0 artifacts | `e2e-test\v1.1.0\` (served via HTTP) |
| Update feed | `e2e-test\v1.1.0\releases.win.json` |
| Update package | `e2e-test\v1.1.0\DocuFiller-1.1.0-full.nupkg` |
| Portable HTTP test dir | `e2e-portable-test\` (created by e2e-portable-update-test.bat) |
| Portable Go test dir | `e2e-portable-go-test\` (created by e2e-portable-go-update-test.sh) |
