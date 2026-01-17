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

REM ========================================
REM Check Git Availability
REM ========================================

where git.exe >nul 2>&1
if errorlevel 1 (
    echo Error: git.exe not found in PATH
    echo Please install Git or ensure it's in your PATH
    exit /b 1
)

REM ========================================
REM Detect Git Tag
REM ========================================

REM Try to get tag from command line parameter
set TAG_FROM_PARAM=%1
set CHANNEL_FROM_PARAM=%2

REM If parameters provided, use them
if not "%TAG_FROM_PARAM%"=="" (
    set TAG_TO_USE=%TAG_FROM_PARAM%
    if "%CHANNEL_FROM_PARAM%"=="" (
        echo Error: When specifying version, you must also specify channel
        echo Usage: release.bat [stable^|beta] [version]
        echo Example: release.bat stable 1.0.0
        exit /b 1
    )
    set USER_DEFINED_CHANNEL=%CHANNEL_FROM_PARAM%

    REM Validate channel parameter
    if /i not "!CHANNEL_FROM_PARAM!"=="stable" (
        if /i not "!CHANNEL_FROM_PARAM!"=="beta" (
            echo Error: Invalid channel "!CHANNEL_FROM_PARAM!"
            echo Channel must be either "stable" or "beta"
            echo Usage: release.bat [stable^|beta] [version]
            echo Example: release.bat stable 1.0.0
            exit /b 1
        )
    )

    goto :TagDetected
)

REM Otherwise, try to get current git tag
echo Detecting git tag...
for /f "delims=" %%t in ('git.exe describe --tags --abbrev=0 2^>nul') do (
    set CURRENT_TAG=%%t
)

if "!CURRENT_TAG!"=="" (
    echo Error: No git tag found.
    echo.
    echo Please create and push a tag first, or specify version manually:
    echo.
    echo Using git tag:
    echo   git tag v1.0.0
    echo   git push origin v1.0.0
    echo.
    echo Using parameters:
    echo   release.bat stable 1.0.0
    exit /b 1
)

set TAG_TO_USE=!CURRENT_TAG!
echo Found tag: !TAG_TO_USE!

:TagDetected
