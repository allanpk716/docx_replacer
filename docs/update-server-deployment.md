# DocuFiller 更新服务器部署指南

> **2026-05 更新**：旧的单应用更新服务器（update-server）已替换为通用平台 **update-hub**。
> update-hub 源码独立仓库：`https://github.com/allanpk716/update-hub`（私有）

## 当前部署信息

| 项目 | 值 |
|------|-----|
| 服务器 | `172.18.200.47` |
| SSH 端口 | `30000` |
| 更新服务端口 | `30001` |
| 服务名称 | `UpdateHub`（NSSM Windows 服务） |
| 安装路径 | `C:\WorkSpace\update-server\` |
| 二进制 | `C:\WorkSpace\update-server\update-hub.exe` |
| 数据目录 | `C:\WorkSpace\update-server\data\` |
| 日志目录 | `C:\WorkSpace\update-server\logs\` |
| NSSM 路径 | `C:\WorkSpace\update-server\nssm.exe` |
| 数据库 | `C:\WorkSpace\update-server\data\update-hub.db`（SQLite） |

### 认证配置

| 参数 | 说明 |
|------|------|
| `-token` | API Bearer Token，用于 curl 上传 / promote / delete 认证 |
| `-password` | Web UI 登录密码 |
| 当前设置 | token 和 password 使用同一个密钥，值存储在 DocuFiller 项目 `.env` 文件的 `UPDATE_SERVER_API_TOKEN` 字段 |

### 修改密码或 Token

SSH 到服务器后执行：

```bat
C:\WorkSpace\update-server\nssm.exe set UpdateHub AppParameters "-port 30001 -data-dir "C:\WorkSpace\update-server\data" -token <TOKEN> -password <PASSWORD> -migrate-app-id docufiller"
C:\WorkSpace\update-server\nssm.exe restart UpdateHub
```

### DocuFiller 客户端配置

在 `appsettings.json` 中：

```json
{
  "Update": {
    "UpdateUrl": "http://172.18.200.47:30001/docufiller",
    "Channel": "stable"
  }
}
```

URL 规则：`http://<host>:<port>/<appId>`，通道由 `Channel` 字段选择（stable / beta）。不要在 URL 末尾加通道路径，客户端会自动拼接。

### 数据目录结构（迁移后）

```
C:\WorkSpace\update-server\data\
├── docufiller\
│   ├── stable\          ← 16 个版本，~1.1GB
│   │   ├── releases.win.json
│   │   └── DocuFiller-*.nupkg
│   └── beta\            ← 4 个版本，~222MB
│       ├── releases.win.json
│       └── DocuFiller-*.nupkg
└── update-hub.db        ← SQLite 元数据
```

旧格式 `data\stable\` 和 `data\beta\` 已在首次启动时自动迁移到 `data\docufiller\stable\` 和 `data\docufiller\beta\`。

## 服务管理

```bat
:: 查看状态
C:\WorkSpace\update-server\nssm.exe status UpdateHub

:: 重启
C:\WorkSpace\update-server\nssm.exe restart UpdateHub

:: 停止
C:\WorkSpace\update-server\nssm.exe stop UpdateHub

:: 启动
C:\WorkSpace\update-server\nssm.exe start UpdateHub

:: 查看日志
type C:\WorkSpace\update-server\logs\update-hub.out.log
type C:\WorkSpace\update-server\logs\update-hub.err.log
```

## 更新 update-hub 二进制

```bat
:: 1. 停止服务
C:\WorkSpace\update-server\nssm.exe stop UpdateHub

:: 2. 替换二进制（通过 SFTP 或 SCP 上传新的 update-hub.exe）
:: 本地编译：cd update-hub && npm run build --prefix web && go build -o update-hub.exe .
:: 然后上传：scp -P 30000 update-hub.exe Administrator@172.18.200.47:C:/WorkSpace/update-server/update-hub.exe

:: 3. 启动服务
C:\WorkSpace\update-server\nssm.exe start UpdateHub
```

## 发布新版本

使用 `build-internal.bat`（已配置 `.env` 中的 `UPDATE_SERVER_API_TOKEN`）：

```bat
:: 上传到 beta 通道测试
scripts\build-internal.bat beta

:: 通过 Web UI (http://172.18.200.47:30001) 或 API promote 到 stable
curl -X POST "http://172.18.200.47:30001/api/apps/docufiller/channels/stable/promote" ^
  -H "Authorization: Bearer <TOKEN>" ^
  -H "Content-Type: application/json" ^
  -d "{\"sourceChannel\":\"beta\",\"version\":\"1.x.x\"}"
```

## Web 管理界面

浏览器访问 `http://172.18.200.47:30001/`，使用 password 登录后可：
- 查看应用列表和通道
- 查看版本列表（含备注）
- 上传新版本（带 release notes）
- Promote（beta → stable）
- 删除旧版本

## 环境变量（.env 文件）

DocuFiller 项目根目录 `.env` 文件（已 gitignore）：

| 环境变量 | 说明 |
|----------|------|
| `UPDATE_SERVER_HOST` | 服务器 IP |
| `UPDATE_SERVER_SSH_PORT` | SSH 端口（30000） |
| `UPDATE_SERVER_USER` | SSH 用户名 |
| `UPDATE_SERVER_PASSWORD` | SSH 密码 |
| `UPDATE_SERVER_PORT` | 更新服务 HTTP 端口（30001） |
| `UPDATE_SERVER_API_TOKEN` | API Token + Web UI 密码 |

## SSH 连接

```bash
ssh -p 30000 Administrator@172.18.200.47
```

也可通过 Python paramiko 脚本自动化部署（读取 `.env` 配置）。
