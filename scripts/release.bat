@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DocuFiller Release Script
echo ========================================
echo.

REM Get script directory
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM ========================================
REM Load Configuration
REM ========================================
if exist "%SCRIPT_DIR%config\release-config.bat" (
    call "%SCRIPT_DIR%config\release-config.bat"
) else (
    echo Error: Configuration file not found!
    echo.
    echo Please create release-config.bat from the template:
    echo   copy scripts\config\release-config.bat.example scripts\config\release-config.bat
    echo Then edit it with your server settings.
    exit /b 1
)

REM Validate required configuration
if "!UPDATE_SERVER_URL!"=="" (
    echo Error: UPDATE_SERVER_URL not set in release-config.bat
    exit /b 1
)

if "!UPDATE_TOKEN!"=="" (
    echo Error: UPDATE_TOKEN not set in release-config.bat
    exit /b 1
)

if "!UPLOAD_ADMIN_PATH!"=="" (
    echo Error: UPLOAD_ADMIN_PATH not set in release-config.bat
    exit /b 1
)

REM Check upload-admin.exe exists
if not exist "!UPLOAD_ADMIN_PATH!" (
    echo Error: upload-admin.exe not found at: !UPLOAD_ADMIN_PATH!
    echo Please check UPLOAD_ADMIN_PATH in release-config.bat
    exit /b 1
)

endlocal
