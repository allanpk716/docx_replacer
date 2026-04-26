@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller Build Internal Script
REM This script contains the core build logic
REM and should not be called directly.
REM Produces Velopack artifacts:
REM   Setup.exe, Portable.zip, .nupkg, releases.win.json
REM ========================================

set MODE=%1
set CHANNEL=%2
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set PACK_OUTPUT=%SCRIPT_DIR%build

REM Validate mode
if "%MODE%"=="" (
    echo Error: Mode not specified
    exit /b 1
)

if not "%MODE%"=="standalone" (
    echo Error: Invalid mode '%MODE%'. Only 'standalone' is supported.
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

REM ========================================
REM Get Version
REM ========================================
call :GET_VERSION
if "!VERSION!"=="" (
    echo Error: Failed to get version
    exit /b 1
)

echo ========================================
echo Mode: !MODE!
echo Version: !VERSION!
if defined GIT_TAG echo Git Tag: !GIT_TAG!
if defined CHANNEL echo Channel: !CHANNEL!
echo ========================================

REM ========================================
REM Build and Package
REM ========================================
call :CLEAN_BUILD
if errorlevel 1 (
    echo Error: Build failed
    exit /b 1
)

call :VPK_PACK
if errorlevel 1 (
    echo Error: Velopack packaging failed
    exit /b 1
)

REM Upload to update server if channel is specified
if defined CHANNEL (
    call :UPLOAD
    if errorlevel 1 (
        echo Error: Upload failed
        exit /b 1
    )
)

echo ========================================
echo Build completed successfully!
echo Artifacts in: !PACK_OUTPUT!
echo Version: !VERSION!
echo ========================================

endlocal
exit /b 0

REM ========================================
REM Function: GET_VERSION
REM ========================================
:GET_VERSION
REM Try Git Tag first, strip leading 'v' for semver2 compatibility with vpk
for /f "tokens=* delims= " %%t in ('git describe --tags --exact-match 2^>nul') do (
    set GIT_TAG=%%t
    set VERSION=%%t
)

REM Strip leading 'v' from tag (e.g. v1.2.3 -> 1.2.3)
if defined VERSION (
    set "VERSION_NOV=!VERSION!"
    if "!VERSION:~0,1!"=="v" set "VERSION=!VERSION:~1!"
)

REM If no tag, read from csproj and add -dev suffix
if "!GIT_TAG!"=="" (
    for /f "tokens=2 delims=<> " %%v in ('type "!PROJECT_ROOT!\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
        set VERSION=%%v-dev
    )
)

echo [GET_VERSION] Detecting version...
echo [GET_VERSION]   Version: !VERSION!
if defined GIT_TAG echo [GET_VERSION]   Git Tag: !GIT_TAG!
echo [GET_VERSION] SUCCESS
exit /b 0

REM ========================================
REM Function: CLEAN_BUILD
REM ========================================
:CLEAN_BUILD
echo.
echo [CLEAN_BUILD] Cleaning old build output...
if exist "%PACK_OUTPUT%" rmdir /s /q "%PACK_OUTPUT%"
mkdir "%PACK_OUTPUT%\publish"

echo [CLEAN_BUILD] Publishing project (PublishSingleFile=true)...
dotnet publish "!PROJECT_ROOT!\DocuFiller.csproj" -c Release -r win-x64 --self-contained -o "%PACK_OUTPUT%\publish" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
    echo [CLEAN_BUILD] FAILED: dotnet publish returned error
    exit /b 1
)
echo [CLEAN_BUILD] SUCCESS: Publish complete
exit /b 0

REM ========================================
REM Function: VPK_PACK
REM Creates Velopack artifacts: Setup.exe, Portable.zip, .nupkg, releases.win.json
REM ========================================
:VPK_PACK
echo.
echo [VPK_PACK] Checking vpk availability...
where vpk >nul 2>&1
if errorlevel 1 (
    echo [VPK_PACK] ERROR: vpk not found. Install with: dotnet tool install -g vpk
    exit /b 1
)

echo [VPK_PACK] Running vpk pack...
vpk pack --packId DocuFiller --packVersion !VERSION! --packDir "%PACK_OUTPUT%\publish" --mainExe DocuFiller.exe --outputDir "%PACK_OUTPUT%"
if errorlevel 1 (
    echo [VPK_PACK] FAILED: vpk pack returned error
    exit /b 1
)

REM Clean up intermediate publish directory
if exist "%PACK_OUTPUT%\publish" rmdir /s /q "%PACK_OUTPUT%\publish"

echo [VPK_PACK] SUCCESS: Velopack artifacts created
echo Artifacts:
for %%f in ("%PACK_OUTPUT%\DocuFiller*") do echo   %%~nxf
for %%f in ("%PACK_OUTPUT%\releases.win.json") do echo   %%~nxf
exit /b 0

REM ========================================
REM Function: UPLOAD
REM Uploads .nupkg files and releases.win.json to the Go update server
REM Requires UPDATE_SERVER_HOST, UPDATE_SERVER_PORT, and UPDATE_SERVER_API_TOKEN environment variables
REM ========================================
:UPLOAD
echo.
echo [UPLOAD] Starting upload to channel: !CHANNEL!

REM Check required environment variables
if "!UPDATE_SERVER_HOST!"=="" (
    echo [UPLOAD] FAILED: UPDATE_SERVER_HOST environment variable is not set
    echo [UPLOAD] Required environment variables:
    echo [UPLOAD]   UPDATE_SERVER_HOST    - IP or hostname of the update server
    echo [UPLOAD]   UPDATE_SERVER_PORT    - HTTP port of the update server
    echo [UPLOAD]   UPDATE_SERVER_API_TOKEN - Bearer token for authentication
    exit /b 1
)

REM Build URL from host and port (default port 80 if not set)
if "!UPDATE_SERVER_PORT!"=="" (
    set "UPDATE_SERVER_PORT=80"
)
set "UPLOAD_URL=http://!UPDATE_SERVER_HOST!:!UPDATE_SERVER_PORT!/api/channels/!CHANNEL!/releases"

if "!UPDATE_SERVER_API_TOKEN!"=="" (
    echo [UPLOAD] FAILED: UPDATE_SERVER_API_TOKEN environment variable is not set
    echo [UPLOAD] Required environment variables:
    echo [UPLOAD]   UPDATE_SERVER_HOST    - IP or hostname of the update server
    echo [UPLOAD]   UPDATE_SERVER_PORT    - HTTP port of the update server
    echo [UPLOAD]   UPDATE_SERVER_API_TOKEN - Bearer token for authentication
    exit /b 1
)
echo [UPLOAD] Target: !UPLOAD_URL!

REM Upload releases.win.json
set "RELEASES_FILE=%PACK_OUTPUT%\releases.win.json"
if exist "!RELEASES_FILE!" (
    echo [UPLOAD] Uploading releases.win.json...
    for /f "tokens=* delims= " %%h in ('curl -s -o nul -w "%%{http_code}" --max-time 60 -H "Authorization: Bearer !UPDATE_SERVER_API_TOKEN!" -F "file=@!RELEASES_FILE!" "!UPLOAD_URL!" 2^>nul') do set HTTP_STATUS=%%h
    if "!HTTP_STATUS!"=="200" (
        echo [UPLOAD]   releases.win.json - OK (HTTP !HTTP_STATUS!^)
    ) else (
        echo [UPLOAD] FAILED: releases.win.json - HTTP !HTTP_STATUS!
        exit /b 1
    )
) else (
    echo [UPLOAD] WARNING: releases.win.json not found in %PACK_OUTPUT%
)

REM Upload all .nupkg files
set UPLOAD_COUNT=0
for %%f in ("%PACK_OUTPUT%\*.nupkg") do (
    echo [UPLOAD] Uploading %%~nxf...
    for /f "tokens=* delims= " %%h in ('curl -s -o nul -w "%%{http_code}" --max-time 60 -H "Authorization: Bearer !UPDATE_SERVER_API_TOKEN!" -F "file=@%%f" "!UPLOAD_URL!" 2^>nul') do set HTTP_STATUS=%%h
    if "!HTTP_STATUS!"=="200" (
        echo [UPLOAD]   %%~nxf - OK (HTTP !HTTP_STATUS!^)
        set /a UPLOAD_COUNT+=1
    ) else (
        echo [UPLOAD] FAILED: %%~nxf - HTTP !HTTP_STATUS!
        exit /b 1
    )
)

if !UPLOAD_COUNT! equ 0 (
    echo [UPLOAD] WARNING: No .nupkg files found to upload
)

echo [UPLOAD] SUCCESS: !UPLOAD_COUNT! package(s^) uploaded to !CHANNEL! channel
exit /b 0


