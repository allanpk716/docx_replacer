# DocuFiller 更新服务器部署指南

本文档记录如何将 DocuFiller 的 Go 更新服务器部署到 Windows Server，以及后续的配置、测试和运维操作。

## 环境变量说明

所有环境变量统一以 `UPDATE_SERVER_` 前缀命名，在项目根目录 `.env` 文件中配置（该文件已被 gitignore）：

| 环境变量 | 说明 | 示例 |
|----------|------|------|
| `UPDATE_SERVER_HOST` | 更新服务器的 IP 或主机名 | `172.18.200.47` |
| `UPDATE_SERVER_USER` | 服务器 SSH 登录用户名 | `Administrator` |
| `UPDATE_SERVER_PASSWORD` | 服务器 SSH 登录密码 | `your-password` |
| `UPDATE_SERVER_SSH_PORT` | SSH 端口 | `30000` |
| `UPDATE_SERVER_PORT` | 更新服务 HTTP 端口 | `30001` |
| `UPDATE_SERVER_API_TOKEN` | API 鉴权 Bearer Token | `your-secret-token` |

### .env 文件示例

```
# Update Server Connection
UPDATE_SERVER_HOST=172.18.200.47
UPDATE_SERVER_USER=Administrator
UPDATE_SERVER_PASSWORD=your-password
UPDATE_SERVER_SSH_PORT=30000
UPDATE_SERVER_PORT=30001
UPDATE_SERVER_API_TOKEN=your-secret-token
```

## 前置条件

### 服务器端（Windows Server）

- Windows Server 2019 或更高版本
- 已安装 OpenSSH Server（参见下方 SSH 配置章节）
- Python 脚本运行依赖：`pip install paramiko`（在部署机器上安装）
- 防火墙放行 SSH 端口和更新服务端口

### 部署端（本机）

- Go 1.22+（交叉编译）
- Python 3（运行部署和验证脚本）
- `paramiko` 库：`pip install paramiko`

## 更新服务器架构

```
update-server/           # Go HTTP 服务源码
├── main.go              # 入口：端口、数据目录、token 配置
├── go.mod
├── handler/             # HTTP 处理器
│   ├── api.go           # API 路由分发
│   ├── upload.go        # POST /api/channels/{channel}/releases
│   ├── list.go          # GET  /api/channels/{channel}/releases
│   ├── promote.go       # POST /api/channels/stable/promote?from=beta&version=x.x.x
│   └── static.go        # GET /{channel}/releases.win.json, /{channel}/*.nupkg
├── middleware/
│   └── auth.go          # Bearer Token 鉴权（上传/promote 需认证，GET 公开）
├── storage/
│   ├── store.go         # 文件系统存储（releases.win.json + .nupkg）
│   └── cleanup.go       # 旧版本自动清理（保留最近 10 个版本）
└── model/
    └── release.go       # Velopack ReleaseFeed 数据模型
```

### API 端点

| 方法 | 路径 | 鉴权 | 说明 |
|------|------|------|------|
| GET | `/{channel}/releases.win.json` | 无 | Velopack 客户端获取版本信息 |
| GET | `/{channel}/*.nupkg` | 无 | 下载更新包 |
| GET | `/api/channels/{channel}/releases` | 无 | 列出通道内所有版本 |
| POST | `/api/channels/{channel}/releases` | Bearer Token | 上传 .nupkg 和 releases.win.json |
| POST | `/api/channels/{channel}/promote` | Bearer Token | 将版本从源通道提升到目标通道 |

支持的通道：`stable`、`beta`

## 部署步骤

### 1. 编译

在本地交叉编译 Windows amd64 二进制：

```bash
cd update-server
GOOS=windows GOARCH=amd64 go build -o docufiller-update-server.exe .
```

### 2. 上传到服务器

将编译产物上传到服务器：

```bash
scp -P <SSH_PORT> docufiller-update-server.exe <USER>@<HOST>:C:/WorkSpace/update-server/
```

或使用 Python 脚本（读取 .env 配置）：

```python
import paramiko, os

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect(os.environ["UPDATE_SERVER_HOST"],
    port=int(os.environ["UPDATE_SERVER_SSH_PORT"]),
    username=os.environ["UPDATE_SERVER_USER"],
    password=os.environ["UPDATE_SERVER_PASSWORD"],
    timeout=15)

sftp = client.open_sftp()
sftp.put("docufiller-update-server.exe",
         "C:/WorkSpace/update-server/docufiller-update-server.exe")
sftp.close()
client.close()
```

### 3. 安装为 Windows 服务

在服务器上执行（需管理员权限）：

```powershell
# 创建目录
New-Item -Path "C:\WorkSpace\update-server\data\stable" -ItemType Directory -Force
New-Item -Path "C:\WorkSpace\update-server\data\beta" -ItemType Directory -Force
New-Item -Path "C:\WorkSpace\update-server\logs" -ItemType Directory -Force

# 下载 NSSM（如果服务器没有）
# nssm 用于将命令行程序注册为 Windows 服务
Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile "$env:TEMP\nssm.zip"
Expand-Archive -Path "$env:TEMP\nssm.zip" -DestinationPath "$env:TEMP\nssm" -Force
Copy-Item "$env:TEMP\nssm\nssm-2.24\win64\nssm.exe" "C:\WorkSpace\update-server\nssm.exe"

# 注册服务（替换 <TOKEN> 为你的 API Token）
$port = 30001
$token = "<TOKEN>"
$dataDir = "C:\WorkSpace\update-server\data"

nssm install DocuFillerUpdateServer "C:\WorkSpace\update-server\docufiller-update-server.exe"
nssm set DocuFillerUpdateServer AppParameters "-port $port -token $token -data-dir \`"$dataDir\`""
nssm set DocuFillerUpdateServer AppDirectory "C:\WorkSpace\update-server"
nssm set DocuFillerUpdateServer DisplayName "DocuFiller Update Server"
nssm set DocuFillerUpdateServer Description "DocuFiller auto-update server for Velopack"
nssm set DocuFillerUpdateServer Start SERVICE_AUTO_START
nssm set DocuFillerUpdateServer ObjectName LocalSystem
nssm set DocuFillerUpdateServer AppStdout "C:\WorkSpace\update-server\logs\stdout.log"
nssm set DocuFillerUpdateServer AppStderr "C:\WorkSpace\update-server\logs\stderr.log"
nssm set DocuFillerUpdateServer AppRotateFiles 1
nssm set DocuFillerUpdateServer AppRotateBytes 10485760

# 启动服务
nssm start DocuFillerUpdateServer

# 验证
nssm status DocuFillerUpdateServer
netstat -ano | findstr ":$port" | findstr LISTENING
```

### 4. 防火墙放行

```powershell
New-NetFirewallRule -Name "DocuFiller Update Server" -DisplayName "DocuFiller Update Server" `
    -Enabled True -Direction Inbound -Protocol TCP -LocalPort <HTTP_PORT> -Action Allow
```

## 验证部署

使用项目自带的验证脚本：

```bash
cd <project-root>
python scripts/post_reboot_test.py
```

该脚本会检查：
- 服务状态是否为 SERVICE_RUNNING
- HTTP 端口是否在监听
- Stable 和 Beta 通道的 releases.win.json 是否可访问
- 版本列表 API 是否正常

或手动用 curl 验证：

```bash
# 检查版本信息（应返回 200 或 404，404 表示还没上传 release）
curl -s "http://<HOST>:<HTTP_PORT>/stable/releases.win.json"

# 上传测试（需 Token）
curl -X POST "http://<HOST>:<HTTP_PORT>/api/channels/beta/releases" \
  -H "Authorization: Bearer <TOKEN>" \
  -F "releases=@releases.win.json;filename=releases.win.json" \
  -F "file=@DocuFiller-1.0.0-full.nupkg"

# Promote beta -> stable
curl -X POST "http://<HOST>:<HTTP_PORT>/api/channels/stable/promote?from=beta&version=1.0.0" \
  -H "Authorization: Bearer <TOKEN>"
```

## 服务管理

```powershell
# 查看状态
nssm status DocuFillerUpdateServer

# 重启
nssm restart DocuFillerUpdateServer

# 停止
nssm stop DocuFillerUpdateServer

# 启动
nssm start DocuFillerUpdateServer

# 查看日志
type "C:\WorkSpace\update-server\logs\stderr.log"
```

## 客户端配置

在 DocuFiller 的 `appsettings.json` 中配置：

```json
{
  "Update": {
    "UpdateUrl": "http://<HOST>:<HTTP_PORT>",
    "Channel": "stable"
  }
}
```

- `UpdateUrl`：更新服务器地址（不含通道路径和末尾斜杠）
- `Channel`：`stable` 或 `beta`

## 发布流程

使用构建脚本自动打包并上传：

```bash
# 构建 + 上传到 beta 通道
scripts\build.bat --standalone beta

# 构建 + 上传到 stable 通道
scripts\build.bat --standalone stable
```

脚本依赖的环境变量：

| 环境变量 | 说明 |
|----------|------|
| `UPDATE_SERVER_HOST` | 更新服务器 IP |
| `UPDATE_SERVER_PORT` | 更新服务器 HTTP 端口 |
| `UPDATE_SERVER_API_TOKEN` | API 鉴权 Token |

### 推荐发布流程

1. 先发布到 `beta` 通道测试
2. 测试通过后，使用 promote API 将版本提升到 `stable`：

```bash
curl -X POST "http://<HOST>:<HTTP_PORT>/api/channels/stable/promote?from=beta&version=<VERSION>" \
  -H "Authorization: Bearer <TOKEN>"
```

## Windows Server 2019 SSH 配置参考

如果服务器没有联网能力（无法通过 Windows Update 安装 OpenSSH），需要手动安装：

1. 从 [Win32-OpenSSH GitHub Releases](https://github.com/PowerShell/Win32-OpenSSH/releases) 下载 `OpenSSH-Win64.zip`
2. 将 zip 拷贝到服务器并解压
3. 以管理员身份运行安装脚本：

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
& "C:\path\to\OpenSSH-Win64\install-sshd.ps1"
```

4. 修改端口和启用密码登录：

```powershell
$port = 30000
$cfg = Get-Content "$env:ProgramData\ssh\sshd_config" -Raw
$cfg = $cfg -replace '#Port 22', "Port $port"
$cfg = $cfg -replace '(?m)^Port\s+\d+', "Port $port"
$cfg = $cfg -replace '#PasswordAuthentication yes', "PasswordAuthentication yes"
$cfg | Set-Content "$env:ProgramData\ssh\sshd_config" -Encoding ASCII

# 防火墙
New-NetFirewallRule -Name "OpenSSH-Server-In-TCP" -DisplayName "OpenSSH Server" `
    -Enabled True -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow

# 设置开机自启
Set-Service -Name sshd -StartupType Automatic
Restart-Service sshd
```

## 当前部署信息

| 项目 | 值 |
|------|-----|
| 服务器 | `172.18.200.47` |
| SSH 端口 | `30000` |
| 更新服务端口 | `30001` |
| 服务安装路径 | `C:\WorkSpace\update-server` |
| 数据目录 | `C:\WorkSpace\update-server\data` |
| 日志目录 | `C:\WorkSpace\update-server\logs` |
| 服务名称 | `DocuFillerUpdateServer` |
| 管理工具 | NSSM (`nssm.exe`) |
