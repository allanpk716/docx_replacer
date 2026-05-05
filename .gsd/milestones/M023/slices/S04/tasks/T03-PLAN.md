---
estimated_steps: 13
estimated_files: 5
skills_used: []
---

# T03: Create NSSM deployment scripts and deployment README

Create Windows batch scripts for NSSM (Non-Sucking Service Manager) service management on Windows Server 2019. Also create a deployment README with step-by-step instructions.

Scripts to create in `deploy/` directory:
1. `install-service.bat` — Registers update-hub.exe as a Windows service via NSSM. Sets service name to "UpdateHub", configures AppDirectory, AppExit restart delay, log rotation (10MB per file, keep 5). Prompts for token and password.
2. `uninstall-service.bat` — Stops and removes the NSSM service.
3. `start-service.bat` / `stop-service.bat` — Simple wrappers for `nssm start/stop UpdateHub`.

README (`deploy/README.md`):
1. Prerequisites (NSSM installed, .exe copied to server)
2. Configuration (editing start-service.bat args)
3. Install steps (run install-service.bat as admin)
4. Verification (open browser to http://server:30001)
5. Data migration (automatic on first start)
6. Updating the service (stop → replace exe → start)
7. Log location and rotation

## Inputs

- None specified.

## Expected Output

- `update-hub/deploy/install-service.bat`
- `update-hub/deploy/uninstall-service.bat`
- `update-hub/deploy/start-service.bat`
- `update-hub/deploy/stop-service.bat`
- `update-hub/deploy/README.md`

## Verification

powershell -Command "Get-ChildItem deploy/*.bat | ForEach-Object { Write-Host $_.Name }" && test -f deploy/README.md
