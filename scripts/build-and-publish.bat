@echo off
setlocal enabledelayedexpansion

REM Get parameters
set CHANNEL=%1
if "%CHANNEL%"=="" set CHANNEL=stable

echo ========================================
echo DocuFiller Build and Publish Script
echo ========================================
echo Channel: %CHANNEL%
echo.

REM Read version from csproj for display
for /f "tokens=2 delims=<> " %%v in ('type "%~dp0..\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
    set VERSION=%%v
)

if "!VERSION!"=="" (
    echo Warning: Cannot read version from DocuFiller.csproj
)

echo Starting build process...
echo.

REM Step 1: Build
call "%~dp0build.bat"
if errorlevel 1 (
    echo.
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.

REM Step 2: Publish
echo Starting publish process...
echo.

call "%~dp0publish.bat" %CHANNEL% !VERSION!
if errorlevel 1 (
    echo.
    echo ========================================
    echo PUBLISH FAILED!
    echo ========================================
    echo However, the build was successful.
    echo You can find the package at: build\docufiller-!VERSION!.zip
    echo.
    echo To retry publishing later, run:
    echo   publish.bat %CHANNEL% !VERSION!
    exit /b 1
)

echo.
echo ========================================
echo ALL DONE!
echo ========================================
echo Version: !VERSION!
echo Channel: %CHANNEL%
echo Status: Successfully built and published
echo ========================================

endlocal
