@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DocuFiller Build Script
echo ========================================

REM Get script directory
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM Read version from DocuFiller.csproj
REM Parse: <Version>1.0.0</Version>
REM Tokens with leading space: "", "Version", "1.0.0", "/Version"
REM So we need token 2 to get "1.0.0"
for /f "tokens=2 delims=<> " %%v in ('type "%PROJECT_ROOT%\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
    set VERSION=%%v
)

if "!VERSION!"=="" (
    echo Error: Cannot read version from DocuFiller.csproj
    echo Please check if Version tag exists in the project file.
    exit /b 1
)

echo Building version: !VERSION!

REM Clean old build output
if exist "%SCRIPT_DIR%build" rmdir /s /q "%SCRIPT_DIR%build"
mkdir "%SCRIPT_DIR%build"

REM Build and publish
echo Building...
dotnet publish "%PROJECT_ROOT%\DocuFiller.csproj" -c Release -r win-x64 --self-contained -o "%SCRIPT_DIR%build\temp" -p:PublishSingleFile=false -p:PublishReadyToRun=false

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

REM Copy External files to build output
echo Copying External files...
if exist "%PROJECT_ROOT%\External\update-client.exe" (
    copy "%PROJECT_ROOT%\External\update-client.exe" "%SCRIPT_DIR%build\temp\" >nul
    echo   - update-client.exe copied
) else (
    echo Warning: update-client.exe not found in External directory
)

if exist "%PROJECT_ROOT%\External\update-client.config.yaml" (
    copy "%PROJECT_ROOT%\External\update-client.config.yaml" "%SCRIPT_DIR%build\temp\" >nul
    echo   - update-client.config.yaml copied
) else (
    echo Warning: update-client.config.yaml not found in External directory
)

REM Create zip package
echo Packaging...
cd "%SCRIPT_DIR%build\temp"
tar -a -cf "..\docufiller-!VERSION!.zip" *
cd "%SCRIPT_DIR%.."

REM Clean temp directory
rmdir /s /q "%SCRIPT_DIR%build\temp"

echo ========================================
echo Build completed successfully!
echo Output: build\docufiller-!VERSION!.zip
echo Version: !VERSION!
echo ========================================

REM Export version for use by other scripts
set VERSION_EXPORT=!VERSION!
endlocal & set VERSION=%VERSION_EXPORT%
