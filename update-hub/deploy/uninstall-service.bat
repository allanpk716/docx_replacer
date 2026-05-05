@echo off
setlocal

:: ============================================================
:: uninstall-service.bat — Stop and remove the UpdateHub service.
:: Must be run as Administrator.
:: ============================================================

set SERVICE_NAME=UpdateHub

:: --- Check for admin privileges ---
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] This script must be run as Administrator.
    exit /b 1
)

:: --- Stop the service (ignore errors if already stopped) ---
echo [INFO] Stopping service '%SERVICE_NAME%' ...
nssm stop %SERVICE_NAME% >nul 2>&1

:: --- Remove the service ---
echo [INFO] Removing service '%SERVICE_NAME%' ...
nssm remove %SERVICE_NAME% confirm
if %errorlevel% neq 0 (
    echo [ERROR] Failed to remove service. It may not be installed.
    exit /b 1
)

echo [OK] Service '%SERVICE_NAME%' stopped and removed.
endlocal
