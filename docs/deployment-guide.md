# DocuFiller 部署与发布指南

本文档详细说明 DocuFiller 项目的编译、打包、发布流程，以及客户端和服务器的配置要求。

## 目录

- [系统架构](#系统架构)
- [服务器配置与部署](#服务器配置与部署)
- [客户端配置](#客户端配置)
- [发布流程](#发布流程)
- [常见问题](#常见问题)

---

## 系统架构

```
┌─────────────────┐         ┌──────────────────────┐
│  DocuFiller     │         │   更新服务器          │
│  (客户端应用)    │ ──────▶ │  (update-server)    │
│                 │ 检查更新 │                      │
└─────────────────┘         │  - 版本管理           │
                             │  - 文件存储           │
                             │  - 下载服务           │
                             └──────────────────────┘
```

---

## 服务器配置与部署

### 1. 更新服务器部署

更新服务器位于：`C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server`

#### 1.1 首次部署

```bash
# 1. 编译服务器
cd C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server
go build -o bin/update-server.exe main.go

# 2. 生成 Admin Token
go run cmd/gen-token/main.go
# 输出示例:
# Admin Token: 9f986f90cfee9e2ffd6278105dbcd95b94889dee4ea3ea1a24234a45a32b5db3
# Token ID: 1022d95b8439843d2e385fa56b7b3ec90b2a36ab0a3486a98033fade8b782652

# 3. 创建程序记录（使用 Admin Token）
curl -X POST http://your-server:8080/api/programs \
  -H "Authorization: Bearer <ADMIN_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"programId":"docufiller","name":"DocuFiller","description":"Word document template filling tool"}'

# 4. 启动服务器
.\bin\update-server.exe
```

#### 1.2 配置文件

服务器配置文件：`config.yaml`

```yaml
server:
  port: 8080              # 服务器端口
  host: "0.0.0.0"         # 监听地址

database:
  path: "./data/versions.db"  # 数据库文件路径

storage:
  basePath: "./data/packages"  # 文件存储路径
  maxFileSize: 536870912       # 最大文件大小 (512MB)

logger:
  level: "info"           # 日志级别: debug, info, warn, error
  output: "both"          # 输出: console, file, both
  filePath: "./logs/server.log"
```

#### 1.3 重要修复说明

**已修复的问题**：版本删除使用硬删除而非软删除

修改位置：`internal/service/version.go`

```go
// DeleteVersion 删除版本（硬删除，允许重新上传相同版本）
func (s *VersionService) DeleteVersion(programID, channel, version string) error {
	return s.db.Unscoped().Where("program_id = ? AND channel = ? AND version = ?", programID, channel, version).Delete(&models.Version{}).Error
}
```

**注意**：每次部署新服务器时，需要运行清理脚本删除旧的软删除记录：

```bash
go run scripts/cleanup-soft-deleted.go
```

#### 1.4 管理工具

服务器提供的管理工具位于：`bin/upload-admin.exe`

**常用命令**：

```bash
# 列出版本
upload-admin.exe list --program-id docufiller --channel stable

# 删除版本
upload-admin.exe delete --program-id docufiller --version 1.0.0 --channel stable

# 上传版本
upload-admin.exe upload --program-id docufiller --channel stable --version 1.0.0 --file path\to\package.zip
```

---

## 客户端配置

### 2.1 发布配置文件

DocuFiller 发布配置：`scripts\config\release-config.bat`

```bat
@echo off
REM 更新服务器配置
REM 此文件包含敏感凭据，已加入 .gitignore

REM 更新服务器地址
set UPDATE_SERVER_URL=http://172.18.200.47:58100

REM 上传 Token (使用 Admin Token)
set UPDATE_TOKEN=your-admin-token-here

REM upload-admin.exe 工具路径
set UPLOAD_ADMIN_PATH=C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\bin\upload-admin.exe
```

**重要**：
- `UPDATE_SERVER_URL` 需要指向实际部署的更新服务器地址
- `UPDATE_TOKEN` 需要使用服务器生成的有效 Admin Token
- 此文件已加入 `.gitignore`，不要提交到仓库

### 2.2 首次配置步骤

```bash
# 1. 复制配置模板（如果不存在）
copy scripts\config\release-config.bat.example scripts\config\release-config.bat

# 2. 编辑配置文件
notepad scripts\config\release-config.bat

# 3. 填入实际值：
#    - UPDATE_SERVER_URL: 你的更新服务器地址
#    - UPDATE_TOKEN: 从服务器获取的 Admin Token
#    - UPLOAD_ADMIN_PATH: upload-admin.exe 的完整路径
```

---

## 发布流程

### 3.1 版本号管理

DocuFiller 的版本号在 `DocuFiller.csproj` 中定义：

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  ...
</PropertyGroup>
```

**版本号格式**：`x.y.z` (主版本.次版本.修订号)

### 3.2 完整发布步骤

#### 方式一：使用 Git 标签（推荐）

```bash
# 1. 更新版本号
# 编辑 DocuFiller.csproj，修改 <Version> 标签

# 2. 提交代码
git add .
git commit -m "Release v1.0.0"

# 3. 创建标签
# 稳定版：v1.0.0
# 测试版：v1.0.0-beta
git tag v1.0.0

# 4. 推送标签
git push origin main
git push origin v1.0.0

# 5. 执行发布（自动识别标签）
scripts\release.bat
```

#### 方式二：使用命令行参数

```bash
# 稳定版发布
scripts\release.bat stable 1.0.0

# 测试版发布
scripts\release.bat beta 1.0.0
```

### 3.3 发布脚本详解

#### build.bat - 构建脚本

```bash
# 功能：
# 1. 读取 DocuFiller.csproj 中的版本号
# 2. 清理旧的构建输出
# 3. 编译项目 (dotnet publish)
# 4. 打包为 zip 文件

# 输出：scripts\build\docufiller-{VERSION}.zip
```

**使用**：
```bash
scripts\build.bat
```

#### release.bat - 发布脚本

```bash
# 功能：
# 1. 检测 Git 标签或使用命令行参数
# 2. 解析渠道（stable/beta）和版本号
# 3. 验证版本号一致性
# 4. 调用 build.bat 构建
# 5. 调用 upload-admin.exe 上传到更新服务器

# 配置：从 scripts\config\release-config.bat 读取
```

**使用**：
```bash
# 使用 Git 标签
scripts\release.bat

# 或指定参数
scripts\release.bat stable 1.0.0
```

### 3.4 发布流程图

```
┌─────────────────┐
│ 1. 更新版本号    │
│ DocuFiller.csproj│
└────────┬────────┘
         │
┌────────▼────────┐
│ 2. Git 提交     │
│ git commit      │
└────────┬────────┘
         │
┌────────▼────────┐
│ 3. 创建标签     │
│ git tag v1.0.0  │
└────────┬────────┘
         │
┌────────▼────────┐
│ 4. 执行发布     │
│ scripts\release │
└────────┬────────┘
         │
    ┌────▼────┐
    │ 构建    │ ← build.bat
    │ dotnet  │
    │ publish │
    └────┬────┘
         │
    ┌────▼────┐
    │ 打包    │
    │ tar zip │
    └────┬────┘
         │
    ┌────▼────┐
    │ 上传    │ ← upload-admin.exe
    │ HTTP API│
    └────┬────┘
         │
┌────────▼────────┐
│  更新服务器      │
│  存储版本信息    │
└─────────────────┘
```

---

## 常见问题

### Q1: 发布时提示 "invalid token"

**原因**：Token 无效或服务器重启后 Token 已重置

**解决**：
```bash
# 重新生成 Admin Token
cd C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server
go run cmd/gen-token/main.go

# 更新 release-config.bat 中的 UPDATE_TOKEN
```

### Q2: 上传后提示 "UNIQUE constraint failed"

**原因**：数据库中存在软删除的记录

**解决**：
```bash
# 运行清理脚本
cd C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server
go run scripts/cleanup-soft-deleted.go
```

### Q3: 版本号不一致警告

**原因**：Git 标签版本与 DocuFiller.csproj 中的版本不匹配

**解决**：
- 方式 1：更新 DocuFiller.csproj 中的版本号
- 方式 2：创建正确的新标签

### Q4: upload-admin.exe 返回 404

**原因**：使用的是旧版本 upload-admin.exe，API 端点不匹配

**解决**：
```bash
# 重新编译 upload-admin.exe
cd C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\clients\go\tool
go build -o ../../bin/upload-admin.exe .
```

### Q5: 端口被占用

**原因**：8080 端口已被其他进程占用

**解决**：
```bash
# 查找占用进程
netstat -ano | findstr ":8080"

# 终止进程
taskkill /F /PID <进程ID>

# 或修改 config.yaml 中的端口
```

---

## 附录

### A. 文件结构

```
DocuFiller 项目：
├── DocuFiller.csproj          # 版本号定义
├── scripts/
│   ├── build.bat              # 构建脚本
│   ├── release.bat            # 发布脚本
│   ├── build-and-publish.bat  # 构建并发布
│   └── config/
│       ├── release-config.bat.example  # 配置模板
│       └── release-config.bat         # 实际配置（不提交）
└── docs/
    └── deployment-guide.md    # 本文档

更新服务器项目：
├── bin/
│   └── upload-admin.exe       # 管理工具
├── config.yaml                # 服务器配置
├── data/
│   ├── versions.db            # 版本数据库
│   └── packages/              # 文件存储目录
└── scripts/
    ├── cleanup-soft-deleted.go  # 清理脚本
    └── migrate.go             # 数据库迁移
```

### B. API 端点

**公开端点**（无需认证）：
- `GET /api/health` - 健康检查
- `GET /api/programs/:programId/versions/latest?channel=stable` - 获取最新版本
- `GET /api/programs/:programId/versions?channel=stable` - 获取版本列表

**认证端点**（需要 Token）：
- `POST /api/programs/:programId/versions` - 上传版本
- `DELETE /api/programs/:programId/versions/:version?channel=stable` - 删除版本
- `GET /api/programs/:programId/download/:channel/:version` - 下载文件

### C. 环境变量

**release-config.bat 支持的环境变量**：
- `UPDATE_SERVER_URL` - 服务器地址
- `UPDATE_TOKEN` - 认证 Token

**upload-admin.exe 支持的环境变量**：
- `UPDATE_SERVER_URL` - 服务器地址
- `UPDATE_TOKEN` - 认证 Token

---

**文档版本**：1.0
**最后更新**：2026-01-20
**维护者**：Claude Code
