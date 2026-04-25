@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller Build Script
REM Single entry point for build operations
REM ========================================

REM Parameter parsing
set MODE=%1
set CHANNEL=%2

if "%MODE%"=="" (
    set MODE=standalone
) else if "%MODE%"=="--standalone" (
    set MODE=standalone
) else if "%MODE%"=="--help" (
    call :SHOW_HELP
    exit /b 0
) else (
    echo Error: Unknown parameter %MODE%
    echo.
    call :SHOW_HELP
    exit /b 1
)

REM Validate channel if provided
if not "%CHANNEL%"=="" (
    if not "%CHANNEL%"=="stable" (
        if not "%CHANNEL%"=="beta" (
            echo Error: Invalid channel '%CHANNEL%'. Must be 'stable' or 'beta'.
            exit /b 1
        )
    )
)

REM Execute build
call "%~dp0build-internal.bat" %MODE% %CHANNEL%

endlocal
exit /b %errorlevel%

REM ========================================
REM Function: SHOW_HELP
REM ========================================
:SHOW_HELP
echo DocuFiller Build Script
echo.
echo Usage:
echo   build.bat [mode] [channel]
echo.
echo Modes:
echo   (default)       - Build (standalone mode)
echo   --standalone    - Build (standalone mode)
echo   --help          - Show this help
echo.
echo Channels:
echo   stable          - Upload to stable channel after build
echo   beta            - Upload to beta channel after build
echo   (omit)          - Build only, no upload
echo.
echo Environment variables for upload:
echo   UPDATE_SERVER_URL   - Base URL of the update server
echo   UPDATE_SERVER_TOKEN - Bearer token for authentication
echo.
echo Examples:
echo   build.bat                    - Build only
echo   build.bat --standalone       - Build only (explicit^)
echo   build.bat --standalone beta  - Build and upload to beta channel
echo   build.bat --standalone stable - Build and upload to stable channel
echo.
exit /b 0
