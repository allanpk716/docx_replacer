@echo off
setlocal

:: ============================================================
:: stop-service.bat — Stop the UpdateHub service.
:: ============================================================

set SERVICE_NAME=UpdateHub

echo [INFO] Stopping service '%SERVICE_NAME%' ...
nssm stop %SERVICE_NAME%
if %errorlevel% neq 0 (
    echo [ERROR] Failed to stop service. It may not be running.
    exit /b 1
)

echo [OK] Service '%SERVICE_NAME%' stopped.
endlocal
