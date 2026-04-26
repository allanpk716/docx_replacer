# Windows Server OpenSSH 离线安装指南

适用于无法联网下载 Windows 组件的 Windows Server 2019 / 2022 环境。

## 前置条件

- Windows Server 2019 或更高版本
- 管理员权限（以管理员身份运行 CMD 或 PowerShell）
- `OpenSSH-Win64.zip` 安装包（见下方下载方式）

## 安装步骤

### 1. 下载 OpenSSH 安装包

在有网络的机器上下载，然后拷贝到目标服务器：

**下载地址**：https://github.com/PowerShell/Win32-OpenSSH/releases/latest

**直接链接**（v9.5.0）：
```
https://github.com/PowerShell/Win32-OpenSSH/releases/download/v9.5.0.0p1-Beta/OpenSSH-Win64.zip
```

### 2. 运行安装脚本

将 `OpenSSH-Win64.zip` 和 `install-ssh.bat` 放到服务器上**同一目录**（或 zip 放在 `C:\WorkSpace` 等常见位置，脚本会自动搜索），然后以管理员身份运行：

```cmd
REM 基本用法（默认端口 22）
install-ssh.bat

REM 指定端口
install-ssh.bat 30000
```

脚本会自动完成以下操作：

1. **检查**是否已安装 OpenSSH，已安装则询问是否重装
2. **搜索** `OpenSSH-Win64.zip`（当前目录 → C:\WorkSpace → D:\ → E:\ → F:\）
3. **解压**安装包到临时目录
4. **安装** sshd 服务（使用官方 install-sshd.ps1，失败则手动安装）
5. **配置**端口和密码认证
6. **添加**防火墙入站规则
7. **设置**开机自启动并启动服务
8. **验证**服务状态、端口监听和防火墙规则

### 3. 验证安装

脚本执行完成后会显示验证结果：

```
========================================
Verification
========================================
  Service status: RUNNING
  Startup type:  AUTO_START
  Port 30000:     LISTENING
  Firewall rule:  OK
========================================
Installation complete!

  SSH Port:  30000
  Connect:   ssh -p 30000 Administrator@<server-ip>
  Config:    C:\ProgramData\ssh\sshd_config
========================================
```

### 4. 外层防火墙

如果是云服务器或虚拟机，还需要在**云控制台安全组**或**虚拟化平台**（如 Proxmox）中放行对应端口的入站规则。

**Proxmox 端口映射示例**：

```bash
# 映射端口范围 30000-30010 到虚拟机
iptables -t nat -A PREROUTING -i vmbr0 -p tcp -m multiport \
  --dports 30000:30010 -j DNAT --to-destination 10.88.88.11:30000-30010
```

## 手动安装（不使用脚本）

如果不想用脚本，可以手动执行以下步骤：

### 步骤 1：解压

```powershell
Expand-Archive -Path "C:\path\to\OpenSSH-Win64.zip" -DestinationPath "C:\path\to\OpenSSH" -Force
```

### 步骤 2：安装服务

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
& "C:\path\to\OpenSSH\OpenSSH-Win64\install-sshd.ps1"
```

> `install-sshd.ps1` 会自动设置密钥权限、注册服务。如果手动安装失败，它会自动修复权限问题。

### 步骤 3：修改端口

```powershell
$port = 30000
$cfg = Get-Content "$env:ProgramData\ssh\sshd_config" -Raw
$cfg = $cfg -replace '#Port 22', "Port $port"
$cfg = $cfg -replace '(?m)^Port\s+\d+', "Port $port"
$cfg = $cfg -replace '#PasswordAuthentication yes', "PasswordAuthentication yes"
$cfg | Set-Content "$env:ProgramData\ssh\sshd_config" -Encoding ASCII
```

### 步骤 4：防火墙

```powershell
Remove-NetFirewallRule -Name "OpenSSH-Server-In-TCP" -ErrorAction SilentlyContinue
New-NetFirewallRule -Name "OpenSSH-Server-In-TCP" -DisplayName "OpenSSH Server" `
    -Enabled True -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow
```

> 如果 `New-NetFirewallRule` 报错参数绑定异常，用 `netsh` 替代：
> ```powershell
> netsh advfirewall firewall add rule name="OpenSSH-Server-In-TCP" dir=in action=allow protocol=TCP localport=$port
> ```

### 步骤 5：设置自启动并启动

```powershell
Set-Service -Name sshd -StartupType Automatic
Restart-Service sshd
```

### 步骤 6：验证

```powershell
Get-Service sshd | Select-Object Name, Status, StartType
netstat -ano | findstr ":30000" | findstr LISTENING
```

## 常见问题

### Q: sshd 服务启动失败（错误 1067）

**原因**：密钥文件权限不正确。OpenSSH 要求私钥文件只有 `SYSTEM` 和 `Administrators` 可访问。

**解决**：使用 `install-sshd.ps1` 安装，它会自动修复权限：

```powershell
# 先删除有问题的服务
sc.exe delete sshd

# 重新用官方脚本安装（它会自动修复权限）
Set-ExecutionPolicy Bypass -Scope Process -Force
& "C:\path\to\OpenSSH\OpenSSH-Win64\install-sshd.ps1"
```

### Q: 重启服务器后 SSH 连不上

**原因**：服务启动类型被设为 Manual，重启后不会自动启动。

**解决**：

```powershell
Set-Service -Name sshd -StartupType Automatic
Get-Service sshd | Select-Object Name, StartType
```

### Q: 外部无法连接但服务器本地正常

**原因**：外部防火墙（云安全组 / Proxmox 端口映射 / 服务器防火墙）未放行端口。

**排查步骤**：

```powershell
# 1. 确认服务在监听
netstat -ano | findstr ":你的端口" | findstr LISTENING

# 2. 确认服务器防火墙放行
netsh advfirewall firewall show rule name="OpenSSH-Server-In-TCP"

# 3. 检查外层防火墙（云控制台 / Proxmox）
```

### Q: `Add-WindowsCapability` 或 `dism` 安装时卡住

**原因**：服务器无法连接 Windows Update 服务器。

**解决**：使用本文档的离线安装方式，直接拷贝 zip 安装包。

## 卸载

```powershell
Stop-Service sshd
sc.exe delete sshd
Remove-Item -Path "$env:ProgramData\ssh" -Recurse -Force
Remove-Item -Path "$env:ProgramFiles\OpenSSH" -Recurse -Force
Remove-NetFirewallRule -Name "OpenSSH-Server-In-TCP" -ErrorAction SilentlyContinue
```

## 配置文件位置

| 文件 | 路径 |
|------|------|
| sshd 配置 | `C:\ProgramData\ssh\sshd_config` |
| 主机密钥 | `C:\ProgramData\ssh\ssh_host_*` |
| 服务日志 | `C:\ProgramData\ssh\logs\` |
| 二进制文件 | `C:\Program Files\OpenSSH\` |
