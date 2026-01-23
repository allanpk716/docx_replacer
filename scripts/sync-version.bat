@echo off
setlocal enabledelayedexpansion

REM Get script directory
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM Get current git tag
git describe --tags --abbrev=0 2>.giterr >.gittag
set /p TAG=<.gittag 2>nul
del .gittag .giterr 2>nul

REM Get short commit hash
git rev-parse --short HEAD 2>.giterr >.githash
set /p HASH=<.githash 2>nul
del .githash .giterr 2>nul

REM Determine version
if defined TAG (
    REM Has tag: remove 'v' prefix
    set VERSION=!TAG:~1!
) else (
    REM No tag: use dev version
    set VERSION=1.0.0-dev.!HASH!
)

echo Synchronizing version: !VERSION!

REM Update DocuFiller.csproj
set "PS_CMD=%PROJECT_ROOT%\DocuFiller.csproj"
powershell -Command "$content = Get-Content '%PS_CMD%'; $newVersion = '%VERSION%'; $content = $content -replace '<Version>[^<]+</Version>', ('<Version>' + $newVersion + '</Version>'); Set-Content '%PS_CMD%' $content -Encoding UTF8"

REM Update update-client.config.yaml (only the current_version field)
if exist "%PROJECT_ROOT%\External\update-client.config.yaml" (
    powershell -Command "$content = [System.IO.File]::ReadAllText('%PROJECT_ROOT%\External\update-client.config.yaml'); $newVersion = '%VERSION%'; $content = $content -replace 'current_version: \S+', ('current_version: ''' + $newVersion + ''''); [System.IO.File]::WriteAllText('%PROJECT_ROOT%\External\update-client.config.yaml', $content, [System.Text.Encoding]::UTF8)"
)

echo Version synchronized: !VERSION!

endlocal
