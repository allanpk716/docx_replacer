@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

REM ========================================
REM OpenSSH Server Offline Installer
REM For Windows Server 2019 (no internet required)
REM ========================================

set "SSHPort=%~1"

if "%SSHPort%"=="" (
    echo Usage: install-ssh.bat [port]
    echo Example: install-ssh.bat 30000
    echo.
    echo If no port is specified, default port 22 will be used.
    set /p SSHPort="Enter SSH port [22]: "
    if "!SSHPort!"=="" set "SSHPort=22"
)

REM ========================================
REM Step 1: Check Administrator
REM ========================================
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator.
    echo Right-click PowerShell or CMD and select "Run as administrator".
    pause
    exit /b 1
)

echo ========================================
echo OpenSSH Server Offline Installer
echo Target port: %SSHPort%
echo ========================================

REM ========================================
REM Step 2: Check if already installed
REM ========================================
echo.
echo [1/6] Checking existing OpenSSH installation...
sc.exe query sshd >nul 2>&1
if %errorlevel% equ 0 (
    echo   sshd service already exists.
    for /f "tokens=3" %%s in ('sc.exe query sshd ^| findstr STATE') do (
        set "STATE=%%s"
    )
    echo   Current status: !STATE!
    choice /C YN /M "Do you want to reinstall and reconfigure"
    if errorlevel 2 (
        echo   Skipping installation. Only reconfiguring port.
        goto :CONFIGURE_PORT
    )
)

REM ========================================
REM Step 3: Locate OpenSSH zip
REM ========================================
echo.
echo [2/6] Locating OpenSSH zip file...

set "SSH_ZIP="
if exist "OpenSSH-Win64.zip" (
    set "SSH_ZIP=OpenSSH-Win64.zip"
    echo   Found: OpenSSH-Win64.zip in current directory.
) else (
    for %%d in (C:\WorkSpace D:\ E:\ F:\) do (
        if exist "%%d\OpenSSH-Win64.zip" (
            set "SSH_ZIP=%%d\OpenSSH-Win64.zip"
            echo   Found: !SSH_ZIP!
            goto :FOUND_ZIP
        )
    )
    echo.
    echo   OpenSSH-Win64.zip not found.
    echo.
    echo   Please download it from:
    echo   https://github.com/PowerShell/Win32-OpenSSH/releases/latest
    echo.
    echo   Direct download link:
    echo   https://github.com/PowerShell/Win32-OpenSSH/releases/download/v9.5.0.0p1-Beta/OpenSSH-Win64.zip
    echo.
    echo   Copy the zip to this server, then run this script again.
    pause
    exit /b 1
)
:FOUND_ZIP

REM ========================================
REM Step 4: Extract and install
REM ========================================
echo.
echo [3/6] Extracting OpenSSH...

set "SSH_TEMP=%TEMP%\OpenSSH-Install"
if exist "%SSH_TEMP%" rmdir /s /q "%SSH_TEMP%"
mkdir "%SSH_TEMP%"

powershell -Command "Expand-Archive -Path '%SSH_ZIP%' -DestinationPath '%SSH_TEMP%' -Force"
if %errorlevel% neq 0 (
    echo   ERROR: Failed to extract zip file.
    pause
    exit /b 1
)
echo   Extracted to: %SSH_TEMP%

REM Find the extracted directory (may be OpenSSH-Win64 or nested)
set "SSH_SRC="
if exist "%SSH_TEMP%\OpenSSH-Win64\sshd.exe" (
    set "SSH_SRC=%SSH_TEMP%\OpenSSH-Win64"
) else if exist "%SSH_TEMP%\sshd.exe" (
    set "SSH_SRC=%SSH_TEMP%"
) else (
    echo   ERROR: sshd.exe not found in extracted files.
    pause
    exit /b 1
)
echo   Source: %SSH_SRC%

echo.
echo [4/6] Installing OpenSSH service...

REM Remove existing service if present
sc.exe stop sshd >nul 2>&1
sc.exe delete sshd >nul 2>&1
timeout /t 2 /nobreak >nul

REM Run official install script
powershell -ExecutionPolicy Bypass -File "%SSH_SRC%\install-sshd.ps1"
if %errorlevel% neq 0 (
    echo   WARNING: install-sshd.ps1 returned error.
    echo   Attempting manual installation...
    if not exist "%ProgramFiles%\OpenSSH" mkdir "%ProgramFiles%\OpenSSH"
    xcopy "%SSH_SRC%\*" "%ProgramFiles%\OpenSSH\" /E /I /Y /Q >nul
    powershell -Command "%SSH_SRC%\ssh-keygen.exe -A"
    sc.exe create sshd binPath= "%ProgramFiles%\OpenSSH\sshd.exe" DisplayName= "OpenSSH SSH Server" start= auto
    sc.exe config sshd obj= LocalSystem
)

REM ========================================
REM Step 5: Configure port and security
REM ========================================
:CONFIGURE_PORT
echo.
echo [5/6] Configuring SSH port: %SSHPort%...

set "SSHD_CONFIG=%ProgramData%\ssh\sshd_config"

REM Ensure config directory and file exist
if not exist "%ProgramData%\ssh" mkdir "%ProgramData%\ssh"
if not exist "%SSHD_CONFIG%" (
    if exist "%ProgramFiles%\OpenSSH\sshd_config_default" (
        copy "%ProgramFiles%\OpenSSH\sshd_config_default" "%SSHD_CONFIG%" >nul
    ) else (
        echo Port %SSHPort% > "%SSHD_CONFIG%"
    )
)

REM Read, modify, write config
powershell -Command ^
    "$cfg = Get-Content '%SSHD_CONFIG%' -Raw; ^
    $cfg = $cfg -replace '#Port 22', 'Port %SSHPort%'; ^
    $cfg = $cfg -replace '(?m)^Port\s+\d+', 'Port %SSHPort%'; ^
    $cfg = $cfg -replace '#PasswordAuthentication yes', 'PasswordAuthentication yes'; ^
    $cfg = $cfg -replace '(?m)^PasswordAuthentication\s+no', 'PasswordAuthentication yes'; ^
    $cfg | Set-Content '%SSHD_CONFIG%' -Encoding ASCII"

echo   Port set to: %SSHPort%
echo   Password authentication: enabled

REM ========================================
REM Step 6: Firewall and start
REM ========================================
echo.
echo [6/6] Configuring firewall and starting service...

REM Remove existing SSH firewall rule
powershell -Command "Remove-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' -ErrorAction SilentlyContinue"

REM Add new firewall rule
powershell -Command ^
    "New-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' ^
    -DisplayName 'OpenSSH Server (sshd)' ^
    -Enabled True -Direction Inbound -Protocol TCP ^
    -LocalPort %SSHPort% -Action Allow" >nul 2>&1

if %errorlevel% neq 0 (
    echo   Firewall rule creation via PowerShell failed. Trying netsh...
    netsh advfirewall firewall add rule name="OpenSSH-Server-In-TCP" dir=in action=allow protocol=TCP localport=%SSHPort%
)

echo   Firewall rule added for port %SSHPort%

REM Set auto-start and start service
powershell -Command "Set-Service -Name sshd -StartupType Automatic"
net stop sshd >nul 2>&1
net start sshd

if %errorlevel% neq 0 (
    echo.
    echo   WARNING: Failed to start sshd via net start.
    echo   Trying again in 3 seconds...
    timeout /t 3 /nobreak >nul
    net start sshd
)

REM ========================================
REM Verify
REM ========================================
echo.
echo ========================================
echo Verification
echo ========================================

REM Service status
for /f "tokens=3" %%s in ('sc.exe query sshd ^| findstr STATE') do set "SVC_STATE=%%s"
echo   Service status: !SVC_STATE!

REM Startup type
for /f "tokens=3" %%s in ('sc.exe qc sshd ^| findstr START_TYPE') do set "SVC_START=%%s"
echo   Startup type:  !SVC_START!

REM Port listening
netstat -ano | findstr ":%SSHPort% " | findstr "LISTENING" >nul
if %errorlevel% equ 0 (
    echo   Port %SSHPort%:     LISTENING
) else (
    echo   Port %SSHPort%:     NOT LISTENING - check logs below
)

REM Firewall rule
netsh advfirewall firewall show rule name="OpenSSH-Server-In-TCP" >nul 2>&1
if %errorlevel% equ 0 (
    echo   Firewall rule:  OK
) else (
    echo   Firewall rule:  NOT FOUND - add manually if needed
)

echo.
echo ========================================
echo Installation complete!
echo.
echo   SSH Port:  %SSHPort%
echo   Connect:   ssh -p %SSHPort% Administrator@<server-ip>
echo   Config:    %SSHD_CONFIG%
echo ========================================

REM Cleanup temp files
if exist "%SSH_TEMP%" rmdir /s /q "%SSH_TEMP%" 2>nul

pause
