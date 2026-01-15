@echo off
setlocal enabledelayedexpansion

REM Check parameters
if "%1"=="" (
    echo Usage: publish.bat [stable^|beta] [version]
    echo Example: publish.bat stable 1.0.0
    echo.
    echo If version is not specified, it will be read from DocuFiller.csproj
    exit /b 1
)

set CHANNEL=%1
set VERSION=%2

REM Load configuration
if exist "%~dp0config\publish-config.bat" (
    call "%~dp0config\publish-config.bat"
) else (
    echo Error: Configuration file not found: config\publish-config.bat
    echo Please create the configuration file first.
    exit /b 1
)

REM If version not specified, read from csproj
if "!VERSION!"=="" (
    for /f "tokens=2 delims=<> " %%v in ('type "%~dp0..\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
        set VERSION=%%v
    )
)

if "!VERSION!"=="" (
    echo Error: Cannot determine version. Please specify version parameter or check DocuFiller.csproj
    exit /b 1
)

REM Check if build file exists
set BUILD_FILE=%~dp0build\docufiller-!VERSION!.zip
if not exist "!BUILD_FILE!" (
    echo Error: Build file not found!
    echo Expected: !BUILD_FILE!
    echo.
    echo Please run build.bat first to create the package.
    exit /b 1
)

echo ========================================
echo Publishing DocuFiller Release
echo ========================================
echo Channel: !CHANNEL!
echo Version: !VERSION!
echo Server: !UPDATE_SERVER_URL!
echo File: !BUILD_FILE!
echo ========================================

REM Check curl availability
where curl >nul 2>&1
if errorlevel 1 (
    echo Error: curl command not found!
    echo Please install curl or use Windows 10+ which includes curl.
    exit /b 1
)

REM Call API to upload package
echo.
echo Uploading package to server...
curl -X POST "!UPDATE_SERVER_URL!/api/version/upload" ^
  -H "Authorization: Bearer !UPDATE_SERVER_TOKEN!" ^
  -F "channel=!CHANNEL!" ^
  -F "version=!VERSION!" ^
  -F "file=@!BUILD_FILE!" ^
  -F "mandatory=false" ^
  -F "notes=Release version !VERSION!"

echo.
echo ========================================
if errorlevel 1 (
    echo Publish failed! Please check the error message above.
    exit /b 1
) else (
    echo Publish completed successfully!
    echo Version !VERSION! (!CHANNEL!) has been uploaded.
)
echo ========================================

endlocal
