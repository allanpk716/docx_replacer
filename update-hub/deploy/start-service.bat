@echo off
setlocal

:: ============================================================
:: start-service.bat — Start the UpdateHub service.
:: ============================================================

set SERVICE_NAME=UpdateHub

echo [INFO] Starting service '%SERVICE_NAME%' ...
nssm start %SERVICE_NAME%
if %errorlevel% neq 0 (
    echo [ERROR] Failed to start service. Check logs in the logs\ directory.
    exit /b 1
)

echo [OK] Service '%SERVICE_NAME%' started.
echo        Web UI: http://localhost:30001
endlocal
