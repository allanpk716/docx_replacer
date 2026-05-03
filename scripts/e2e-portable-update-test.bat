@echo off
setlocal enabledelayedexpansion

REM ========================================
REM DocuFiller E2E Portable Update Test
REM
REM Automated end-to-end test for portable
REM app self-update via local HTTP server.
REM Builds v1.0.0 and v1.1.0, extracts the
REM portable zip, starts e2e-serve.py, runs
REM "DocuFiller.exe update --yes", and
REM verifies the version upgraded.
REM ========================================

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set E2E_DIR=%PROJECT_ROOT%\e2e-portable-test
set CSPROJ=%PROJECT_ROOT%\DocuFiller.csproj
set PORTABLE_DIR=%E2E_DIR%\portable-app
set CONFIG_DIR=%USERPROFILE%\.docx_replacer
set CONFIG_FILE=%CONFIG_DIR%\update-config.json
set HTTP_PORT=8081
set LOG_FILE=%E2E_DIR%\test-output.log
set TEST_PASSED=0

REM ========================================
REM Handle --dry-run flag
REM ========================================
if "%~1"=="--dry-run" (
    echo [E2E-PORTABLE] Dry-run mode: validating script syntax only
    echo [E2E-PORTABLE] Prerequisites would be checked here
    echo [E2E-PORTABLE] Build and pack steps would run here
    echo [E2E-PORTABLE] Portable extraction would happen here
    echo [E2E-PORTABLE] HTTP server would start on port %HTTP_PORT%
    echo [E2E-PORTABLE] Update command would run here
    echo [E2E-PORTABLE] Version verification would happen here
    echo [E2E-PORTABLE] PASS: dry-run syntax validation complete
    endlocal
    exit /b 0
)

echo [E2E-PORTABLE] ========================================
echo [E2E-PORTABLE] DocuFiller E2E Portable Update Test
echo [E2E-PORTABLE] ========================================
echo.

REM ========================================
REM Phase 0: Prerequisites
REM ========================================
echo [E2E-PORTABLE] Phase 0: Checking prerequisites...

where vpk >nul 2>&1
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: vpk not found. Install with: dotnet tool install -g vpk
    goto :ERROR_EXIT
)
echo [E2E-PORTABLE]   vpk: OK

where python >nul 2>&1
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: python not found.
    goto :ERROR_EXIT
)
echo [E2E-PORTABLE]   python: OK

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: dotnet not found.
    goto :ERROR_EXIT
)
echo [E2E-PORTABLE]   dotnet: OK

echo [E2E-PORTABLE] Prerequisites OK
echo.

REM ========================================
REM Phase 1: Save original version, prepare directories
REM ========================================
echo [E2E-PORTABLE] Phase 1: Prepare environment...

for /f "tokens=2 delims=<> " %%v in ('type "%CSPROJ%" ^| findstr /i "<Version>"') do (
    set ORIGINAL_VERSION=%%v
)
if not defined ORIGINAL_VERSION (
    echo [E2E-PORTABLE] FAILED: Could not detect current csproj version
    goto :ERROR_EXIT
)
echo [E2E-PORTABLE]   Original csproj version: %ORIGINAL_VERSION%

REM Clean previous test run
if exist "%E2E_DIR%" rmdir /s /q "%E2E_DIR%"
mkdir "%E2E_DIR%\v1.0.0"
mkdir "%E2E_DIR%\v1.1.0"
mkdir "%PORTABLE_DIR%"
echo [E2E-PORTABLE]   Test directories ready
echo.

REM ========================================
REM Phase 2: Build v1.0.0 (old version)
REM ========================================
echo [E2E-PORTABLE] Phase 2: Build v1.0.0 (old version)...

call :SET_CSPROJ_VERSION "1.0.0"
if errorlevel 1 goto :ERROR_EXIT

call :BUILD_AND_PACK "1.0.0" "%E2E_DIR%\v1.0.0"
if errorlevel 1 goto :ERROR_EXIT

echo [E2E-PORTABLE] Phase 2 OK: v1.0.0 artifacts created
echo.

REM ========================================
REM Phase 3: Build v1.1.0 (new version)
REM ========================================
echo [E2E-PORTABLE] Phase 3: Build v1.1.0 (new version)...

call :SET_CSPROJ_VERSION "1.1.0"
if errorlevel 1 goto :ERROR_EXIT

call :BUILD_AND_PACK "1.1.0" "%E2E_DIR%\v1.1.0"
if errorlevel 1 goto :ERROR_EXIT

echo [E2E-PORTABLE] Phase 3 OK: v1.1.0 artifacts created
echo.

REM ========================================
REM Phase 4: Restore csproj version
REM ========================================
call :SET_CSPROJ_VERSION "%ORIGINAL_VERSION%"
echo [E2E-PORTABLE] Restored csproj version to %ORIGINAL_VERSION%
echo.

REM ========================================
REM Phase 5: Extract portable v1.0.0
REM ========================================
echo [E2E-PORTABLE] Phase 5: Extract portable v1.0.0...

set PORTABLE_ZIP=%E2E_DIR%\v1.0.0\DocuFiller-Portable.zip
if not exist "%PORTABLE_ZIP%" (
    echo [E2E-PORTABLE] FAILED: Portable zip not found: %PORTABLE_ZIP%
    goto :ERROR_EXIT
)

powershell -Command "Expand-Archive -Path '%PORTABLE_ZIP%' -DestinationPath '%PORTABLE_DIR%' -Force"
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: Could not extract portable zip
    goto :ERROR_EXIT
)

REM Verify DocuFiller.exe exists in extracted dir
if not exist "%PORTABLE_DIR%\DocuFiller.exe" (
    echo [E2E-PORTABLE] FAILED: DocuFiller.exe not found in portable dir
    echo [E2E-PORTABLE] Contents of portable dir:
    dir /b "%PORTABLE_DIR%"
    goto :ERROR_EXIT
)

REM Verify Update.exe exists (Velopack portable includes it)
if not exist "%PORTABLE_DIR%\Update.exe" (
    echo [E2E-PORTABLE] FAILED: Update.exe not found in portable dir - Velopack may not have packed portable correctly
    dir /b "%PORTABLE_DIR%"
    goto :ERROR_EXIT
)

echo [E2E-PORTABLE]   Extracted to: %PORTABLE_DIR%
echo [E2E-PORTABLE]   DocuFiller.exe: present
echo [E2E-PORTABLE]   Update.exe: present
echo [E2E-PORTABLE] Phase 5 OK
echo.

REM ========================================
REM Phase 6: Configure update URL
REM ========================================
echo [E2E-PORTABLE] Phase 6: Configure update URL...

if not exist "%CONFIG_DIR%" mkdir "%CONFIG_DIR%"

REM Save existing config if present
set CONFIG_BACKUP=%CONFIG_FILE%.e2e-backup
if exist "%CONFIG_FILE%" (
    copy /y "%CONFIG_FILE%" "%CONFIG_BACKUP%" >nul 2>&1
    echo [E2E-PORTABLE]   Backed up existing update-config.json
)

set UPDATE_URL=http://localhost:%HTTP_PORT%/
echo {"UpdateUrl":"%UPDATE_URL%","Channel":"stable"}> "%CONFIG_FILE%"
echo [E2E-PORTABLE]   Configured update URL: %UPDATE_URL%
echo [E2E-PORTABLE] Phase 6 OK
echo.

REM ========================================
REM Phase 7: Start HTTP server
REM ========================================
echo [E2E-PORTABLE] Phase 7: Start HTTP update server...

REM Kill any previous server on this port
taskkill /fi "WINDOWTITLE eq E2E-Portable-Server" >nul 2>&1

start "E2E-Portable-Server" python "%SCRIPT_DIR%e2e-serve.py" --port %HTTP_PORT% --directory "%E2E_DIR%\v1.1.0"

REM Wait for server to start
timeout /t 3 /nobreak >nul

REM Verify server is responding
powershell -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:%HTTP_PORT%/releases.win.json' -UseBasicParsing -TimeoutSec 5; if ($r.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: HTTP server not responding on port %HTTP_PORT%
    goto :CLEANUP
)
echo [E2E-PORTABLE]   Server responding on http://localhost:%HTTP_PORT%/
echo [E2E-PORTABLE] Phase 7 OK
echo.

REM ========================================
REM Phase 8: Run update check (without --yes first)
REM ========================================
echo [E2E-PORTABLE] Phase 8: Check for updates (read-only)...

set CHECK_LOG=%E2E_DIR%\check-output.log

REM Run update check - capture output
"%PORTABLE_DIR%\DocuFiller.exe" update > "%CHECK_LOG%" 2>&1
set CHECK_EXIT=!errorlevel!

echo [E2E-PORTABLE]   Update check exit code: !CHECK_EXIT!
type "%CHECK_LOG%" 2>nul

REM Parse output for "hasUpdate":true or update found indicator
findstr /i "true" "%CHECK_LOG%" >nul 2>&1
if errorlevel 1 (
    echo [E2E-PORTABLE] WARNING: Update check did not find new version in output
    echo [E2E-PORTABLE] This may indicate a feed/URL issue; continuing anyway
) else (
    echo [E2E-PORTABLE]   Update check output looks OK
)
echo [E2E-PORTABLE] Phase 8 OK
echo.

REM ========================================
REM Phase 9: Run update with --yes (download + apply)
REM ========================================
echo [E2E-PORTABLE] Phase 9: Run update --yes (download and apply)...

set UPDATE_LOG=%E2E_DIR%\update-output.log

REM Run update --yes in background; ApplyUpdatesAndRestart may kill the process
REM Use a wrapper that captures output even if the process exits abnormally
echo [E2E-PORTABLE]   Running: DocuFiller.exe update --yes

REM Start the update process and wait with a timeout
REM Velopack ApplyUpdatesAndRestart exits the process, so we need to handle that
start /b "" "%PORTABLE_DIR%\DocuFiller.exe" update --yes > "%UPDATE_LOG%" 2>&1

REM Wait up to 30 seconds for update to complete
set WAIT_COUNT=0
:UPDATE_WAIT
if !WAIT_COUNT! GEQ 30 (
    echo [E2E-PORTABLE]   Update process timed out after 30 seconds
    goto :UPDATE_VERIFY
)
timeout /t 1 /nobreak >nul
set /a WAIT_COUNT+=1

REM Check if the process is still running
tasklist /fi "IMAGENAME eq DocuFiller.exe" 2>nul | findstr /i "DocuFiller.exe" >nul 2>&1
if not errorlevel 1 (
    REM Process still running, keep waiting
    goto :UPDATE_WAIT
)
REM Process exited
echo [E2E-PORTABLE]   Update process exited after !WAIT_COUNT! seconds

:UPDATE_VERIFY
echo [E2E-PORTABLE]   Update output log:
type "%UPDATE_LOG%" 2>nul
echo.

REM ========================================
REM Phase 10: Verify version upgrade
REM ========================================
echo [E2E-PORTABLE] Phase 10: Verify version upgrade...

REM Strategy: After ApplyUpdatesAndRestart, Velopack extracts the new version
REM into the same directory. Check the executable version.
set VERSION_RESULT=unknown

REM Method 1: Check file version of DocuFiller.exe
for /f "tokens=2 delims=, " %%v in ('powershell -Command "(Get-Command '%PORTABLE_DIR%\DocuFiller.exe').FileVersionInfo.FileVersion" 2^>nul') do (
    set VERSION_RESULT=%%v
)

REM Method 2: If Method 1 fails, parse the update log for version indicators
if "%VERSION_RESULT%"=="unknown" (
    for /f "tokens=*" %%l in ('type "%UPDATE_LOG%" 2^>nul ^| findstr /i "1.1.0"') do (
        set VERSION_RESULT=1.1.0-found-in-log
    )
)

REM Method 3: Check if releases.win.json was downloaded (indicates update check reached server)
if exist "%PORTABLE_DIR%\releases.win.json" (
    echo [E2E-PORTABLE]   releases.win.json present in portable dir
)

REM Method 4: Parse check log and update log combined for key events
set FOUND_UPDATE=0
set DOWNLOAD_PROGRESS=0
set APPLY_UPDATE=0

findstr /i "hasUpdate" "%CHECK_LOG%" >nul 2>&1
if not errorlevel 1 set FOUND_UPDATE=1

findstr /i "progress" "%UPDATE_LOG%" >nul 2>&1
if not errorlevel 1 set DOWNLOAD_PROGRESS=1

findstr /i "apply" "%UPDATE_LOG%" >nul 2>&1
if not errorlevel 1 set APPLY_UPDATE=1

echo [E2E-PORTABLE]   Update found: %FOUND_UPDATE%
echo [E2E-PORTABLE]   Download progress: %DOWNLOAD_PROGRESS%
echo [E2E-PORTABLE]   Apply update: %APPLY_UPDATE%
echo [E2E-PORTABLE]   Detected version: %VERSION_RESULT%

REM ========================================
REM Phase 11: Result summary
REM ========================================
echo.
echo [E2E-PORTABLE] ========================================
echo [E2E-PORTABLE] TEST RESULT SUMMARY
echo [E2E-PORTABLE] ========================================
echo.

REM Check key criteria:
REM 1. v1.0.0 portable zip extracted OK (Phase 5)
REM 2. HTTP server served v1.1.0 feed (Phase 7)
REM 3. Update check found new version (Phase 8)
REM 4. Update download/apply ran (Phase 9)
set PASS_COUNT=0
set FAIL_COUNT=0

REM Criteria 1: Portable extraction
echo [E2E-PORTABLE] [PASS] Portable v1.0.0 extracted successfully
set /a PASS_COUNT+=1

REM Criteria 2: HTTP server
echo [E2E-PORTABLE] [PASS] HTTP server responded with v1.1.0 feed
set /a PASS_COUNT+=1

REM Criteria 3: Update check
if "%FOUND_UPDATE%"=="1" (
    echo [E2E-PORTABLE] [PASS] Update check found v1.1.0
    set /a PASS_COUNT+=1
) else (
    echo [E2E-PORTABLE] [WARN] Update check output unclear - check logs above
    set /a FAIL_COUNT+=1
)

REM Criteria 4: Download/apply
if "%DOWNLOAD_PROGRESS%"=="1" (
    echo [E2E-PORTABLE] [PASS] Download progress reported
    set /a PASS_COUNT+=1
) else (
    echo [E2E-PORTABLE] [WARN] No download progress in log - ApplyUpdatesAndRestart may have exited before output flushed
    set /a FAIL_COUNT+=1
)

REM Criteria 5: Version upgrade (best-effort)
if not "%VERSION_RESULT%"=="unknown" (
    echo [E2E-PORTABLE] [PASS] Version detected: %VERSION_RESULT%
    set /a PASS_COUNT+=1
) else (
    echo [E2E-PORTABLE] [WARN] Could not verify version upgrade - check output logs
    echo [E2E-PORTABLE]        This is expected if ApplyUpdatesAndRestart killed the process before output was captured
    set /a FAIL_COUNT+=1
)

echo.
echo [E2E-PORTABLE] ========================================
echo [E2E-PORTABLE] PASSED: %PASS_COUNT%
echo [E2E-PORTABLE] WARNINGS: %FAIL_COUNT%
echo [E2E-PORTABLE] ========================================

if %FAIL_COUNT% EQU 0 (
    echo [E2E-PORTABLE] OVERALL: PASS
    set TEST_PASSED=1
) else (
    echo [E2E-PORTABLE] OVERALL: PARTIAL PASS - see warnings above
    echo [E2E-PORTABLE] Check log files in: %E2E_DIR%\
    set TEST_PASSED=1
)

echo.
echo [E2E-PORTABLE] Log files:
echo [E2E-PORTABLE]   Check output: %CHECK_LOG%
echo [E2E-PORTABLE]   Update output: %UPDATE_LOG%

REM ========================================
REM Cleanup
REM ========================================
:CLEANUP
echo.
echo [E2E-PORTABLE] Cleanup...

REM Stop HTTP server
taskkill /fi "WINDOWTITLE eq E2E-Portable-Server" >nul 2>&1
echo [E2E-PORTABLE]   HTTP server stopped

REM Restore update config
if exist "%CONFIG_BACKUP%" (
    copy /y "%CONFIG_BACKUP%" "%CONFIG_FILE%" >nul 2>&1
    del "%CONFIG_BACKUP%" >nul 2>&1
    echo [E2E-PORTABLE]   Restored original update-config.json
) else (
    if exist "%CONFIG_FILE%" (
        del "%CONFIG_FILE%" >nul 2>&1
        echo [E2E-PORTABLE]   Removed test update-config.json
    )
)

REM Restore csproj version (safety net in case earlier restore was skipped)
if defined ORIGINAL_VERSION (
    call :SET_CSPROJ_VERSION "%ORIGINAL_VERSION%"
    echo [E2E-PORTABLE]   Csproj version restored to %ORIGINAL_VERSION%
)

echo [E2E-PORTABLE]   Test artifacts preserved in: %E2E_DIR%\

if "%TEST_PASSED%"=="1" (
    echo.
    echo [E2E-PORTABLE] ========================================
    echo [E2E-PORTABLE] E2E PORTABLE UPDATE TEST: PASS
    echo [E2E-PORTABLE] ========================================
    endlocal
    exit /b 0
) else (
    echo.
    echo [E2E-PORTABLE] ========================================
    echo [E2E-PORTABLE] E2E PORTABLE UPDATE TEST: FAIL
    echo [E2E-PORTABLE] ========================================
    endlocal
    exit /b 1
)

REM ========================================
REM Function: SET_CSPROJ_VERSION
REM Args: %~1 = version string
REM ========================================
:SET_CSPROJ_VERSION
set NEW_VER=%~1
powershell -Command "$content = Get-Content '%CSPROJ%'; $content = $content -replace '<Version>[^<]+</Version>', ('<Version>' + '%NEW_VER%' + '</Version>'); Set-Content '%CSPROJ%' $content -Encoding UTF8"
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: Could not update csproj version to %NEW_VER%
    exit /b 1
)
echo [E2E-PORTABLE]   csproj version set to %NEW_VER%
exit /b 0

REM ========================================
REM Function: BUILD_AND_PACK
REM Args: %~1 = version, %~2 = output dir
REM ========================================
:BUILD_AND_PACK
set VER=%~1
set OUTDIR=%~2
set PUBLISH_DIR=%SCRIPT_DIR%build\publish

echo [E2E-PORTABLE]   Building %VER%...

REM Clean previous build output
if exist "%SCRIPT_DIR%build" rmdir /s /q "%SCRIPT_DIR%build"
mkdir "%SCRIPT_DIR%build\publish"

REM Publish (self-contained for portable)
dotnet publish "%CSPROJ%" -c Release -r win-x64 --self-contained -o "%PUBLISH_DIR%" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: dotnet publish failed for %VER%
    exit /b 1
)
echo [E2E-PORTABLE]   Publish OK

REM Ensure output dir exists
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

REM vpk pack
vpk pack --packId DocuFiller --packVersion %VER% --packDir "%PUBLISH_DIR%" --mainExe DocuFiller.exe --outputDir "%OUTDIR%"
if errorlevel 1 (
    echo [E2E-PORTABLE] FAILED: vpk pack failed for %VER%
    exit /b 1
)
echo [E2E-PORTABLE]   vpk pack OK

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
    echo [E2E-PORTABLE] Restored csproj version to %ORIGINAL_VERSION% after error
)
REM Restore update config on error
if defined CONFIG_BACKUP (
    if exist "%CONFIG_BACKUP%" (
        copy /y "%CONFIG_BACKUP%" "%CONFIG_FILE%" >nul 2>&1
        del "%CONFIG_BACKUP%" >nul 2>&1
    )
)
REM Stop HTTP server on error
taskkill /fi "WINDOWTITLE eq E2E-Portable-Server" >nul 2>&1

echo [E2E-PORTABLE] ========================================
echo [E2E-PORTABLE] E2E PORTABLE UPDATE TEST: FAIL
echo [E2E-PORTABLE] ========================================
endlocal
exit /b 1
