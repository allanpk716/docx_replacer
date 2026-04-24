@echo off
REM Clean up orphaned GSD stashes left after milestone merge
REM Run this after each milestone completion if you see stash pop warnings
REM
REM Usage: scripts\cleanup-gsd-stash.bat          (dry run - show what would be dropped)
REM        scripts\cleanup-gsd-stash.bat --drop    (actually drop stashes)

setlocal enabledelayedexpansion

set "DROPMODE=%~1"
set "COUNT=0"

for /f %%i in ('git stash list --format="%%gD" 2^>nul') do (
    set /a COUNT+=1
)

if %COUNT%==0 (
    echo No stashes found. Repository is clean.
    exit /b 0
)

echo Found %COUNT% stash entries:
echo.
git stash list
echo.

if "%DROPMODE%"=="--drop" (
    echo Dropping all %COUNT% stash entries...
    for /f %%i in ('git stash list --format="%%gD" 2^>nul') do (
        git stash drop %%i 2>nul
    )
    echo.
    echo Done. Stash is now empty.
) else (
    echo Dry run. Use --drop to actually remove these stashes.
)

endlocal
