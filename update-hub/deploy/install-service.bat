@echo off
setlocal enabledelayedexpansion

:: ============================================================
:: install-service.bat — Register UpdateHub as a Windows service
:: via NSSM (Non-Sucking Service Manager).
:: Must be run as Administrator.
:: ============================================================

set SERVICE_NAME=UpdateHub
set NSSM=nssm

:: --- Check for admin privileges ---
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] This script must be run as Administrator.
    echo Right-click and select "Run as administrator".
    exit /b 1
)

:: --- Check NSSM is available ---
where %NSSM% >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] nssm not found in PATH.
    echo Download from https://nssm.cc/download and add to PATH.
    exit /b 1
)

:: --- Resolve AppDirectory (directory where this script lives) ---
set APP_DIR=%~dp0..
set EXE_PATH=%APP_DIR%\update-hub.exe

if not exist "%EXE_PATH%" (
    echo [ERROR] update-hub.exe not found at: %EXE_PATH%
    echo Copy the executable next to the deploy\ folder before installing.
    exit /b 1
)

:: --- Prompt for token (API bearer token) ---
set /p TOKEN="[PROMPT] Enter API bearer token (leave empty to disable auth): "

:: --- Prompt for password (Web UI login) ---
set /p PASSWORD="[PROMPT] Enter Web UI admin password (leave empty to disable login): "

:: --- Build arguments ---
set ARGS=-port 30001 -data-dir "%APP_DIR%\data"
if not "%TOKEN%"=="" (
    set ARGS=!ARGS! -token %TOKEN%
)
if not "%PASSWORD%"=="" (
    set ARGS=!ARGS! -password %PASSWORD%
)

:: --- Install the service ---
echo.
echo [INFO] Installing service '%SERVICE_NAME%' ...
%NSSM% install %SERVICE_NAME% "%EXE_PATH%"
if %errorlevel% neq 0 (
    echo [ERROR] Failed to install service. It may already exist.
    echo Run uninstall-service.bat first, then retry.
    exit /b 1
)

:: --- Configure service parameters ---
echo [INFO] Configuring service parameters ...

%NSSM% set %SERVICE_NAME% AppDirectory "%APP_DIR%"
%NSSM% set %SERVICE_NAME% AppParameters %ARGS%
%NSSM% set %SERVICE_NAME% DisplayName "Update Hub - Auto-Update Server"
%NSSM% set %SERVICE_NAME% Description "Serves Velopack-compatible auto-update feeds for internal applications."
%NSSM% set %SERVICE_NAME% Start SERVICE_AUTO_START

:: --- Restart policy ---
%NSSM% set %SERVICE_NAME% AppExit Default Restart
%NSSM% set %SERVICE_NAME% AppRestartDelay 5000

:: --- Log rotation (stdout + stderr) ---
set LOG_DIR=%APP_DIR%\logs
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

%NSSM% set %SERVICE_NAME% AppStdout "%LOG_DIR%\update-hub.out.log"
%NSSM% set %SERVICE_NAME% AppStderr "%LOG_DIR%\update-hub.err.log"
%NSSM% set %SERVICE_NAME% AppRotateFiles 1
%NSSM% set %SERVICE_NAME% AppRotateBytes 10485760
%NSSM% set %SERVICE_NAME% AppRotateBacklogCopies 5

echo.
echo [OK] Service '%SERVICE_NAME%' installed and configured.
echo.
echo   Executable : %EXE_PATH%
echo   Data dir   : %APP_DIR%\data
echo   Logs       : %LOG_DIR%\
echo.
echo Run start-service.bat (or: nssm start %SERVICE_NAME%) to start.
endlocal
