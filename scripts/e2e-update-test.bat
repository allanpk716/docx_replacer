@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller E2E Update Test Script
REM
REM Builds two versions (1.0.0 and 1.1.0),
REM packages with vpk, starts a local HTTP
REM server serving the new version's feed,
REM and prints tester instructions.
REM ========================================

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set E2E_DIR=%PROJECT_ROOT%\e2e-test
set CSproj=%PROJECT_ROOT%\DocuFiller.csproj

REM ========================================
REM Phase 0: Prerequisites
REM ========================================
echo [E2E] ========================================
echo [E2E] DocuFiller E2E Update Test
echo [E2E] ========================================
echo.

echo [E2E] Checking prerequisites...

where vpk >nul 2>&1
if errorlevel 1 (
    echo [E2E] FAILED: vpk not found.
    echo [E2E] Install with: dotnet tool install -g vpk
    goto :ERROR_EXIT
)
echo [E2E]   vpk: OK

where python >nul 2>&1
if errorlevel 1 (
    echo [E2E] FAILED: python not found.
    echo [E2E] Install Python 3 from https://www.python.org/downloads/
    goto :ERROR_EXIT
)
echo [E2E]   python: OK

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [E2E] FAILED: dotnet not found.
    echo [E2E] Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    goto :ERROR_EXIT
)
echo [E2E]   dotnet: OK

echo [E2E] Prerequisites OK
echo.

REM ========================================
REM Phase 1: Prepare output directories
REM ========================================
echo [E2E] ========================================
echo [E2E] Phase 1: Prepare directories
echo [E2E] ========================================

if exist "%E2E_DIR%" rmdir /s /q "%E2E_DIR%"
mkdir "%E2E_DIR%\v1.0.0"
mkdir "%E2E_DIR%\v1.1.0"
echo [E2E] Directories ready: e2e-test\v1.0.0, e2e-test\v1.1.0
echo.

REM ========================================
REM Save original csproj version
REM ========================================
for /f "tokens=2 delims=<> " %%v in ('type "%CSproj%" ^| findstr /i "<Version>"') do (
    set ORIGINAL_VERSION=%%v
)
echo [E2E] Original csproj version: %ORIGINAL_VERSION%

REM ========================================
REM Phase 2: Build v1.0.0 (old version)
REM ========================================
echo.
echo [E2E] ========================================
echo [E2E] Phase 2: Build v1.0.0 (old version)
echo [E2E] ========================================

call :SET_CSPROJ_VERSION "1.0.0"
if errorlevel 1 goto :ERROR_EXIT

call :BUILD_AND_PACK "1.0.0" "%E2E_DIR%\v1.0.0"
if errorlevel 1 goto :ERROR_EXIT

echo [E2E] Phase 2 SUCCESS: v1.0.0 artifacts created
echo.

REM ========================================
REM Phase 3: Build v1.1.0 (new version)
REM ========================================
echo.
echo [E2E] ========================================
echo [E2E] Phase 3: Build v1.1.0 (new version)
echo [E2E] ========================================

call :SET_CSPROJ_VERSION "1.1.0"
if errorlevel 1 goto :ERROR_EXIT

call :BUILD_AND_PACK "1.1.0" "%E2E_DIR%\v1.1.0"
if errorlevel 1 goto :ERROR_EXIT

echo [E2E] Phase 3 SUCCESS: v1.1.0 artifacts created
echo.

REM ========================================
REM Phase 4: Copy installer
REM ========================================
echo.
echo [E2E] ========================================
echo [E2E] Phase 4: Copy installer to e2e-test root
echo [E2E] ========================================

copy /y "%E2E_DIR%\v1.0.0\DocuFiller-Setup.exe" "%E2E_DIR%\DocuFiller-Setup-v1.0.0.exe" >nul
if errorlevel 1 (
    echo [E2E] FAILED: Could not find DocuFiller-Setup.exe in v1.0.0 output
    goto :ERROR_EXIT
)
echo [E2E] Copied: e2e-test\DocuFiller-Setup-v1.0.0.exe
echo [E2E] Phase 4 SUCCESS
echo.

REM ========================================
REM Phase 5: Restore original version
REM ========================================
call :SET_CSPROJ_VERSION "%ORIGINAL_VERSION%"
echo [E2E] Restored csproj version to %ORIGINAL_VERSION%
echo.

REM ========================================
REM Phase 6: Start HTTP server
REM ========================================
echo.
echo [E2E] ========================================
echo [E2E] Phase 5: Start HTTP update server
echo [E2E] ========================================

echo [E2E] Starting e2e-serve.py on port 8080...
echo [E2E] Serving from: %E2E_DIR%\v1.1.0
echo.

start "E2E Update Server" python "%SCRIPT_DIR%e2e-serve.py" --port 8080 --directory "%E2E_DIR%\v1.1.0"

REM Give server a moment to start
timeout /t 2 /nobreak >nul

echo [E2E] HTTP server started on http://localhost:8080/
echo.

REM ========================================
REM Phase 7: Print tester instructions
REM ========================================
echo.
echo [E2E] ========================================
echo [E2E] MANUAL TEST INSTRUCTIONS
echo [E2E] ========================================
echo.
echo [E2E] Update URL: http://localhost:8080/
echo.
echo [E2E] Step 1: Install old version
echo [E2E]   Run: e2e-test\DocuFiller-Setup-v1.0.0.exe
echo [E2E]   Expected: DocuFiller 1.0.0 installs and launches
echo [E2E]   PASS: App window shows, version is 1.0.0
echo.
echo [E2E] Step 2: Create user data (for config preservation test)
echo [E2E]   In the app: open a template, fill some data, generate output
echo [E2E]   Note the Output directory location
echo.
echo [E2E] Step 3: Configure update URL
echo [E2E]   Edit the installed app's appsettings.json
echo [E2E]   Set Update.UpdateUrl to: http://localhost:8080/
echo [E2E]   (Default install path: %%LOCALAPPDATA%%\DocuFiller\)
echo.
echo [E2E] Step 4: Trigger update check
echo [E2E]   Restart the app, then click "Check for Updates"
echo [E2E]   Expected: Update found (v1.1.0 available)
echo [E2E]   PASS: Update notification appears
echo.
echo [E2E] Step 5: Apply update
echo [E2E]   Click "Download and Install" or "Update"
echo [E2E]   Expected: App downloads update, restarts
echo [E2E]   Watch the HTTP server console for GET requests
echo [E2E]   PASS: App restarts as version 1.1.0
echo.
echo [E2E] Step 6: Verify config preservation
echo [E2E]   Check that appsettings.json is preserved
echo [E2E]   Check that Output/ directory still has your test files
echo [E2E]   Check that Logs/ directory exists and has entries
echo [E2E]   PASS: All user data preserved after upgrade
echo.
echo [E2E] Step 7: Verify Portable.zip
echo [E2E]   Extract: e2e-test\v1.0.0\DocuFiller-Portable.zip
echo [E2E]   Run DocuFiller.exe from extracted folder
echo [E2E]   PASS: App runs without installation
echo.
echo [E2E] ========================================
echo [E2E] ARTIFACTS SUMMARY
echo [E2E] ========================================
echo [E2E] Old version (installer):
dir /b "%E2E_DIR%\DocuFiller-Setup-v1.0.0.exe" 2>nul
echo [E2E] Old version (full):
dir /b "%E2E_DIR%\v1.0.0\*" 2>nul
echo [E2E] New version (served via HTTP):
dir /b "%E2E_DIR%\v1.1.0\*" 2>nul
echo.
echo [E2E] Press any key to stop the HTTP server and exit...
pause >nul

REM ========================================
REM Cleanup
REM ========================================
echo.
echo [E2E] Stopping HTTP server...
taskkill /fi "WINDOWTITLE eq E2E Update Server" >nul 2>&1

echo [E2E] ========================================
echo [E2E] E2E test session ended
echo [E2E] ========================================
echo [E2E] NOTE: e2e-test artifacts preserved for inspection.
echo [E2E]       Run 'rmdir /s /q e2e-test' to clean up.

endlocal
exit /b 0

REM ========================================
REM Function: SET_CSPROJ_VERSION
REM Args: %~1 = version string
REM ========================================
:SET_CSPROJ_VERSION
set NEW_VER=%~1
powershell -Command "$content = Get-Content '%CSproj%'; $content = $content -replace '<Version>[^<]+</Version>', ('<Version>' + '%NEW_VER%' + '</Version>'); Set-Content '%CSproj%' $content -Encoding UTF8"
if errorlevel 1 (
    echo [E2E] FAILED: Could not update csproj version to %NEW_VER%
    exit /b 1
)
echo [E2E]   csproj version set to %NEW_VER%
exit /b 0

REM ========================================
REM Function: BUILD_AND_PACK
REM Args: %~1 = version, %~2 = output dir
REM ========================================
:BUILD_AND_PACK
set VER=%~1
set OUTDIR=%~2
set PUBLISH_DIR=%SCRIPT_DIR%build\publish

echo [E2E]   Building %VER%...

REM Clean previous build output
if exist "%SCRIPT_DIR%build" rmdir /s /q "%SCRIPT_DIR%build"
mkdir "%SCRIPT_DIR%build\publish"

REM Publish (single file, self-contained)
dotnet publish "%CSproj%" -c Release -r win-x64 --self-contained -o "%PUBLISH_DIR%" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
    echo [E2E] FAILED: dotnet publish failed for %VER%
    exit /b 1
)
echo [E2E]   Publish OK

REM Ensure output dir exists
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

REM vpk pack
vpk pack --packId DocuFiller --packVersion %VER% --packDir "%PUBLISH_DIR%" --mainExe DocuFiller.exe --outputDir "%OUTDIR%"
if errorlevel 1 (
    echo [E2E] FAILED: vpk pack failed for %VER%
    exit /b 1
)
echo [E2E]   vpk pack OK -> %OUTDIR%

REM Clean intermediate publish dir
if exist "%SCRIPT_DIR%build" rmdir /s /q "%SCRIPT_DIR%build"

exit /b 0

REM ========================================
REM Error exit handler
REM ========================================
:ERROR_EXIT
REM Restore original version on error
if defined ORIGINAL_VERSION (
    call :SET_CSPROJ_VERSION "%ORIGINAL_VERSION%"
    echo [E2E] Restored csproj version to %ORIGINAL_VERSION% after error
)
echo [E2E] ========================================
echo [E2E] E2E test FAILED - see errors above
echo [E2E] ========================================
endlocal
exit /b 1
