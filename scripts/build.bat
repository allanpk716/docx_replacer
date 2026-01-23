@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller Build Script
REM Single entry point for all build operations
REM ========================================

REM Parameter parsing
set MODE=%1

REM Mode detection
if "%MODE%"=="" (
    call :AUTO_DETECT_MODE
) else if "%MODE%"=="--standalone" (
    set MODE=standalone
) else if "%MODE%"=="--publish" (
    set MODE=publish
) else if "%MODE%"=="--help" (
    call :SHOW_HELP
    exit /b 0
) else (
    echo Error: Unknown parameter %MODE%
    echo.
    call :SHOW_HELP
    exit /b 1
)

REM Execute mode
if "%MODE%"=="standalone" (
    call "%~dp0build-internal.bat" standalone
) else if "%MODE%"=="publish" (
    call "%~dp0build-internal.bat" publish
)

endlocal
exit /b %errorlevel%

REM ========================================
REM Function: AUTO_DETECT_MODE
REM ========================================
:AUTO_DETECT_MODE
REM Check for Git Tag
for /f "tokens=* delims= " %%t in ('git describe --tags --exact-match 2^>nul') do (
    set GIT_TAG=%%t
)

if defined GIT_TAG (
    echo.
    echo Git Tag detected: !GIT_TAG!
    echo.
    choice /C YN /M "Publish to update server"
    if errorlevel 2 (
        set MODE=standalone
    ) else (
        set MODE=publish
    )
) else (
    echo No Git Tag, building for local testing
    set MODE=standalone
)
exit /b 0

REM ========================================
REM Function: SHOW_HELP
REM ========================================
:SHOW_HELP
echo DocuFiller Build Script
echo.
echo Usage:
echo   build.bat              - Auto detect mode
echo   build.bat --standalone - Force standalone build
echo   build.bat --publish    - Force publish to server
echo   build.bat --help       - Show this help
echo.
echo Mode Detection:
echo   If Git Tag exists: Prompt to publish or standalone build
echo   If no Git Tag: Standalone build for local testing
echo.
echo Examples:
echo   build.bat
echo   build.bat --standalone
echo   build.bat --publish
echo.
exit /b 0
