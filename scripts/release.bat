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
git.exe describe --tags --abbrev=0 2>.giterr >.gittag
if exist .gittag (
    set /p CURRENT_TAG=<.gittag
    del .gittag
)
if exist .giterr del .giterr

REM Trim any trailing carriage returns from the tag
if defined CURRENT_TAG (
    for /f "delims=" %%t in ("!CURRENT_TAG!") do set CURRENT_TAG=%%t
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

REM ========================================
REM Parse Channel and Version
REM ========================================

if defined USER_DEFINED_CHANNEL (
    REM Use provided channel and version
    set CHANNEL=%USER_DEFINED_CHANNEL%
    set VERSION=%TAG_TO_USE%
    echo.
    echo ========================================
    echo Manual Release Mode
    echo ========================================
    echo Channel: !CHANNEL!
    echo Version: !VERSION!
    goto :ParsingComplete
)

REM Auto-detect from git tag
echo.
echo ========================================
echo Parsing tag: !TAG_TO_USE!
echo ========================================

REM Check if tag starts with 'v'
echo !TAG_TO_USE! | findstr /i "^v" >nul
if errorlevel 1 (
    echo Error: Tag must start with 'v' (e.g., v1.0.0 or v1.0.0-beta)
    echo Invalid tag: !TAG_TO_USE!
    exit /b 1
)

REM Check for -beta suffix
echo !TAG_TO_USE! | findstr /i "-beta$" >nul
if errorlevel 1 (
    REM No -beta suffix = stable channel
    set CHANNEL=stable
    set VERSION=!TAG_TO_USE:~1!
    echo Detected: STABLE release
) else (
    REM Has -beta suffix = beta channel
    set CHANNEL=beta
    REM Remove 'v' prefix and '-beta' suffix
    set VERSION=!TAG_TO_USE:~1,-5!
    echo Detected: BETA release
)

echo Version: !VERSION!

:ParsingComplete
echo.

REM Validate version format - applies to BOTH manual and auto-detect modes
echo !VERSION! | findstr /i "^[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*$" >nul
if errorlevel 1 (
    echo Error: Invalid version format: !VERSION!
    echo Expected format: x.y.z where x, y, z are numbers
    echo Example: 1.0.0, 2.3.4, 10.20.30
    exit /b 1
)

echo ========================================
echo Release Summary
echo ========================================
echo Channel: !CHANNEL!
echo Version: !VERSION!
echo ========================================
echo.

REM ========================================
REM Build Project
REM ========================================

echo.
echo ========================================
echo Step 1: Building DocuFiller
echo ========================================

REM Check if build.bat exists
if not exist "%SCRIPT_DIR%build.bat" (
    echo.
    echo Error: build.bat not found in scripts directory!
    echo.
    echo Please ensure build.bat exists before running release.
    exit /b 1
)

call "%SCRIPT_DIR%build.bat"
if errorlevel 1 (
    echo.
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    echo Release aborted due to build failure.
    exit /b 1
)

REM Verify build output exists
set BUILD_FILE=%SCRIPT_DIR%build\docufiller-!VERSION!.zip
if not exist "!BUILD_FILE!" (
    echo Error: Build file not found: !BUILD_FILE!
    echo.
    echo Expected: build\docufiller-!VERSION!.zip
    echo Please check if build.bat completed successfully.
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo Build output: !BUILD_FILE!
echo.

REM ========================================
REM Upload to Update Server
REM ========================================

echo.
echo ========================================
echo Step 2: Uploading to Update Server
echo ========================================
echo Server: !UPDATE_SERVER_URL!
echo Program: docufiller
echo Channel: !CHANNEL!
echo Version: !VERSION!
echo File: !BUILD_FILE!
echo ========================================
echo.

REM Upload using upload-admin.exe
"!UPLOAD_ADMIN_PATH!" upload ^
  --program-id docufiller ^
  --channel !CHANNEL! ^
  --version !VERSION! ^
  --file "!BUILD_FILE!" ^
  --server !UPDATE_SERVER_URL! ^
  --token !UPDATE_TOKEN!

if errorlevel 1 (
    echo.
    echo ========================================
    echo UPLOAD FAILED!
    echo ========================================
    echo.
    echo The build file is available at: !BUILD_FILE!
    echo You can retry upload manually with:
    echo.
    echo "!UPLOAD_ADMIN_PATH!" upload --program-id docufiller --channel !CHANNEL! --version !VERSION! --file "!BUILD_FILE!" --server !UPDATE_SERVER_URL! --token YOUR_TOKEN
    echo.
    exit /b 1
)

echo.
echo ========================================
echo Release completed successfully!
echo ========================================
echo.
echo Summary:
echo   Program: docufiller
echo   Channel: !CHANNEL!
echo   Version: !VERSION!
echo   Server: !UPDATE_SERVER_URL!
echo.
echo You can verify the release at:
echo   !UPDATE_SERVER_URL!/api/version/latest?channel=!CHANNEL!
echo.
echo ========================================

endlocal