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


