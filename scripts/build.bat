@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller Build Script
REM Single entry point for build operations
REM ========================================

REM Parameter parsing
set MODE=%1

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

REM Execute build
call "%~dp0build-internal.bat" %MODE%

endlocal
exit /b %errorlevel%

REM ========================================
REM Function: SHOW_HELP
REM ========================================
:SHOW_HELP
echo DocuFiller Build Script
echo.
echo Usage:
echo   build.bat              - Build (standalone mode)
echo   build.bat --standalone - Build (standalone mode)
echo   build.bat --help       - Show this help
echo.
echo Examples:
echo   build.bat
echo   build.bat --standalone
echo.
exit /b 0
