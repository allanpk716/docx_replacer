@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller Build Internal Script
REM This script contains the core build logic
REM and should not be called directly.
REM ========================================

set MODE=%1
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set PACKAGE_PATH=

REM Validate mode
if "%MODE%"=="" (
    echo Error: Mode not specified
    exit /b 1
)

if not "%MODE%"=="standalone" if not "%MODE%"=="publish" (
    echo Error: Invalid mode '%MODE%'
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
echo Channel: !CHANNEL!
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

call :COPY_EXTERNAL_FILES

call :CREATE_PACKAGE
if errorlevel 1 (
    echo Error: Package creation failed
    exit /b 1
)

REM ========================================
REM Publish if needed
REM ========================================
if "!MODE!"=="publish" (
    call :PUBLISH_TO_SERVER
    if errorlevel 1 (
        echo.
        echo Publish failed, but build succeeded.
        echo Package: !PACKAGE_PATH!
        exit /b 1
    )
)

echo ========================================
echo Build completed successfully!
echo Package: !PACKAGE_PATH!
echo Version: !VERSION!
echo Channel: !CHANNEL!
echo ========================================

endlocal
exit /b 0

REM ========================================
REM Function: GET_VERSION
REM ========================================
:GET_VERSION
REM Try Git Tag first
for /f "tokens=* delims= " %%t in ('git describe --tags --exact-match 2^>nul') do (
    set GIT_TAG=%%t
    set VERSION=%%t
)

REM If no tag, read from csproj and add -dev suffix
if "!VERSION!"=="" (
    for /f "tokens=2 delims=<> " %%v in ('type "!PROJECT_ROOT!\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
        set VERSION=%%v-dev
    )
)

REM Detect channel from tag
if defined GIT_TAG (
    echo !GIT_TAG! | findstr /i "beta" >nul && set CHANNEL=beta
    echo !GIT_TAG! | findstr /i "alpha" >nul && set CHANNEL=alpha
    if not defined CHANNEL set CHANNEL=stable
) else (
    set CHANNEL=dev
)

echo Detecting version...
echo   Version: !VERSION!
echo   Channel: !CHANNEL!
exit /b 0

REM ========================================
REM Function: CLEAN_BUILD
REM ========================================
:CLEAN_BUILD
echo.
echo Cleaning old build output...
if exist "%SCRIPT_DIR%build" rmdir /s /q "%SCRIPT_DIR%build"
mkdir "%SCRIPT_DIR%build\temp"

echo Building project...
dotnet publish "!PROJECT_ROOT!\DocuFiller.csproj" -c Release -r win-x64 --self-contained -o "%SCRIPT_DIR%build\temp" -p:PublishSingleFile=false -p:PublishReadyToRun=false
exit /b %errorlevel%

REM ========================================
REM Function: COPY_EXTERNAL_FILES
REM ========================================
:COPY_EXTERNAL_FILES
echo.
echo Copying External files...
if exist "!PROJECT_ROOT!\External\update-client.exe" (
    copy "!PROJECT_ROOT!\External\update-client.exe" "%SCRIPT_DIR%build\temp\" >nul
    echo   - update-client.exe
) else (
    echo Warning: update-client.exe not found
)

if exist "!PROJECT_ROOT!\External\update-client.config.yaml" (
    copy "!PROJECT_ROOT!\External\update-client.config.yaml" "%SCRIPT_DIR%build\temp\" >nul
    echo   - update-client.config.yaml
) else (
    echo Warning: update-client.config.yaml not found
)
exit /b 0

REM ========================================
REM Function: CREATE_PACKAGE
REM ========================================
:CREATE_PACKAGE
echo.
echo Creating package...
set PACKAGE_PATH=%SCRIPT_DIR%build\docufiller-!VERSION!.zip
cd "%SCRIPT_DIR%build\temp"
tar -a -cf "!PACKAGE_PATH!" *
cd "%SCRIPT_DIR%.."
rmdir /s /q "%SCRIPT_DIR%build\temp"
echo Package created: !PACKAGE_PATH!
exit /b 0

REM ========================================
REM Function: PUBLISH_TO_SERVER
REM ========================================
:PUBLISH_TO_SERVER
echo.
echo ========================================
echo Publishing to Update Server
echo ========================================

REM Load publish config
if exist "%SCRIPT_DIR%config\publish-config.bat" (
    call "%SCRIPT_DIR%config\publish-config.bat"
) else (
    echo Error: config\publish-config.bat not found
    exit /b 1
)

REM Check publish-client.exe
set PUBLISHER=!PROJECT_ROOT!\External\publish-client.exe
if not exist "!PUBLISHER!" (
    echo Error: publish-client.exe not found at !PUBLISHER!
    exit /b 1
)

REM Get release notes from commits
call :GET_RELEASE_NOTES
set NOTES=!RELEASE_NOTES!

REM Ask for mandatory flag
echo.
choice /C YN /M "Mark as mandatory update"
if errorlevel 2 (
    set MANDATORY_FLAG=
) else (
    set MANDATORY_FLAG=--mandatory
)

echo.
echo Server: !UPDATE_SERVER_URL!
echo Channel: !CHANNEL!
echo Version: !VERSION!
echo File: !PACKAGE_PATH!
echo Notes: !NOTES!
echo.

REM Upload using publish-client.exe
"!PUBLISHER!" upload ^
  --server !UPDATE_SERVER_URL! ^
  --token !UPDATE_SERVER_TOKEN! ^
  --program-id !PROGRAM_ID! ^
  --channel !CHANNEL! ^
  --version !VERSION! ^
  --file "!PACKAGE_PATH!" ^
  --notes "!NOTES!" ^
  !MANDATORY_FLAG!

if errorlevel 1 (
    echo.
    echo Publish failed!
    exit /b 1
)

echo.
echo Publish completed successfully!
exit /b 0

REM ========================================
REM Function: GET_RELEASE_NOTES
REM ========================================
:GET_RELEASE_NOTES
set RELEASE_NOTES=

REM Get commits since last tag
if defined GIT_TAG (
    for /f "usebackq delims=" %%c in (`git log !GIT_TAG!..HEAD --oneline --format="%%s" 2^>nul`) do (
        set RELEASE_NOTES=!RELEASE_NOTES!%%c`n
    )
) else (
    REM Get recent commits
    for /f "usebackq delims=" %%c in (`git log -5 --oneline --format="%%s" 2^>nul`) do (
        set RELEASE_NOTES=!RELEASE_NOTES!%%c`n
    )
)

REM If no commits, use default
if "!RELEASE_NOTES!"=="" (
    set RELEASE_NOTES=Release version !VERSION!
)

echo Release Notes: !RELEASE_NOTES!
exit /b 0
