# Update Hub — Windows Server Deployment Guide

This directory contains scripts for deploying Update Hub as a Windows service using **NSSM** (Non-Sucking Service Manager) on Windows Server 2019+.

## Prerequisites

1. **NSSM** installed and on `PATH`.  
   Download from <https://nssm.cc/download> — place `nssm.exe` in a directory listed in the system PATH (e.g. `C:\Tools\`).

2. **update-hub.exe** compiled and copied to the server.  
   The executable should sit next to the `deploy\` folder:

   ```
   C:\UpdateHub\
   ├── update-hub.exe
   ├── deploy\
   │   ├── install-service.bat
   │   ├── uninstall-service.bat
   │   ├── start-service.bat
   │   └── stop-service.bat
   └── data\          (created automatically)
   ```

## Configuration

Configuration is handled through command-line arguments passed to `update-hub.exe`. The install script will prompt for:

| Argument | Default | Description |
|---|---|---|
| `-port` | `30001` | HTTP listen port |
| `-data-dir` | `.\data` | Directory for release artifacts and SQLite DB |
| `-token` | *(empty)* | Bearer token for API auth. Empty = auth disabled |
| `-password` | *(empty)* | Admin password for Web UI. Empty = login disabled |
| `-migrate-app-id` | `docufiller` | App ID for old-format directory migration. Empty = skip |

To change arguments after installation, edit the service configuration:

```bat
nssm set UpdateHub AppParameters "-port 30001 -data-dir C:\UpdateHub\data -token YOUR_TOKEN -password YOUR_PASSWORD"
nssm restart UpdateHub
```

## Install Steps

1. **Open an elevated command prompt** (Run as Administrator).
2. Navigate to the deploy directory:
   ```bat
   cd C:\UpdateHub\deploy
   ```
3. Run the install script:
   ```bat
   install-service.bat
   ```
4. Enter the token and password when prompted (or press Enter to leave them empty).
5. Start the service:
   ```bat
   start-service.bat
   ```

## Verification

After starting the service, open a browser and navigate to:

```
http://<server-ip>:30001
```

You should see the Update Hub Web UI with the application list. If you configured a password, you'll be prompted to log in.

To verify via command line:

```bat
curl http://localhost:30001/api/apps
```

## Data Migration

On first start with the default `-migrate-app-id docufiller` flag, the server automatically detects and migrates old DocuFiller data:

- **Old layout:** `data/stable/`, `data/beta/`
- **New layout:** `data/docufiller/stable/`, `data/docufiller/beta/`

Migration is atomic per-channel (uses `os.Rename`). After migration, the SQLite metadata database (`data/update-hub.db`) is synchronized to reflect the current filesystem state.

Check migration status in the service log:

```bat
type ..\logs\update-hub.out.log | findstr migration
```

## Updating the Service

To deploy a new version of update-hub.exe:

```bat
cd C:\UpdateHub\deploy

:: 1. Stop the service
stop-service.bat

:: 2. Replace the executable
copy /Y \\path\to\new\update-hub.exe ..\update-hub.exe

:: 3. Start the service
start-service.bat
```

## Logs and Log Rotation

Logs are written to the `logs\` directory next to the executable:

| File | Content |
|---|---|
| `logs\update-hub.out.log` | Standard output (structured JSON events) |
| `logs\update-hub.err.log` | Standard error |

**Rotation settings** (configured by install-service.bat):

- **Max file size:** 10 MB per log file
- **Retained copies:** 5 rotated files per log stream
- **Total max disk usage:** ~100 MB (2 streams × 5 copies × 10 MB)

To view live logs:

```bat
:: PowerShell
Get-Content ..\logs\update-hub.out.log -Wait -Tail 50
```

## Uninstall

```bat
cd C:\UpdateHub\deploy
uninstall-service.bat
```

This stops the service and removes it from the Windows service registry. Data files in `data\` and log files are **not** deleted.
