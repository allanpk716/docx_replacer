# Update Server 简化重构设计

## 概述

将 update-server 从"需要手动配置"转变为"开箱即用"的自动更新服务，通过 Web 管理界面简化服务器管理和客户端工具配置。

## 核心目标

1. **开箱即用**：首次启动时通过 Web 向导完成初始化
2. **统一管理**：通过 Web 界面管理程序、版本、Token
3. **简化分发**：一键下载配置好的客户端工具
4. **端到端加密**：每个程序独立的加密密钥

## 系统架构

### 组件关系

```
┌─────────────────────────────────────────────────────────────┐
│                    update-server                            │
│              (中央更新服务器 - Go + Gin)                     │
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ Web 管理界面 │  │   API 服务   │  │  文件存储    │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
                           ↑                ↓
                    管理员使用           存储加密文件
                           │
        ┌──────────────────┼──────────────────┐
        ↓                  ↓                  ↓
┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│ Program A     │  │ Program B     │  │ Program C     │
│               │  │               │  │               │
│ ┌───────────┐ │  │ ┌───────────┐ │  │ ┌───────────┐ │
│ │publish-cli│ │  │ │publish-cli│ │  │ │publish-cli│ │
│ └───────────┘ │  │ └───────────┘ │  │ └───────────┘ │
│ ┌───────────┐ │  │ ┌───────────┐ │  │ ┌───────────┐ │
│ │update-cli │ │  │ │update-cli │ │  │ │update-cli │ │
│ └───────────┘ │  │ └───────────┘ │  │ └───────────┘ │
└───────────────┘  └───────────────┘  └───────────────┘
```

### 三个独立组件

1. **update-server**：运行在服务器上，托管所有项目的更新
2. **publish-client.exe**：分发给项目开发者，用于上传更新
3. **update-client.exe**：集成到项目中，用于检查和下载更新

## 认证与加密

### Token 认证

| Token 类型 | 用途 | 生成方式 |
|-----------|------|---------|
| Upload Token | 上传特定程序的版本 | Web 界面自动生成 |
| Download Token | 下载特定程序的版本 | Web 界面自动生成 |

Token 通过 HTTP Header 传递：
```http
Authorization: Bearer <token-string>
```

### 端到端加密

1. **创建程序时**：生成独立的 32 字节 Encryption Key
2. **publish-client**：上传前使用密钥加密文件
3. **服务器**：只存储加密文件，无法解密
4. **update-client**：下载后使用密钥解密文件

```yaml
# 加密流程
DocuFiller:
  publish-client → [AES-256-GCM加密] → 服务器存储 → update-client → [解密]

AnotherApp:
  publish-client → [AES-256-GCM加密] → 服务器存储 → update-client → [解密]
```

## Web 管理界面

### 页面结构

| 页面 | 功能 |
|-----|------|
| 初始化向导 | 首次启动配置（管理员账号、服务器 URL） |
| 登录页面 | 管理员认证 |
| 仪表盘 | 系统概览统计 |
| 程序管理 | 创建、查看、删除程序 |
| 程序详情 | Token 管理、版本列表、下载客户端工具 |
| 系统设置 | 修改服务器配置 |

### 程序详情页

```
┌──────────────────────────────────────────────┐
│ 程序详情 - DocuFiller                         │
├──────────────────────────────────────────────┤
│ 基本信息                                      │
│ Program ID: docufiller                        │
│ 创建时间: 2024-01-15                          │
│ 总下载: 456                                   │
├──────────────────────────────────────────────┤
│ 下载客户端工具                                │
│ [下载发布端] [下载更新端]                     │
├──────────────────────────────────────────────┤
│ Token 管理                                    │
│ Upload Token:   ul_xxx...  [重新生成]        │
│ Download Token: dl_xxx...  [重新生成]        │
│ Encryption Key: key...   [重新生成]          │
├──────────────────────────────────────────────┤
│ 版本列表                                      │
│ v1.2.0 (stable)  2024-01-15  123  [删除]     │
│ v1.1.0 (stable)  2024-01-10  333  [删除]     │
└──────────────────────────────────────────────┘
```

## 客户端工具分发

### 下载流程

1. 管理员将 `publish-client.exe` 和 `update-client.exe` 放到服务器的 `clients/` 目录
2. 在程序详情页点击"下载发布端"或"下载更新端"
3. 服务器动态生成配置文件（填充服务器 URL、Program ID、Token、密钥）
4. 打包成 zip 提供下载

### 配置文件格式

**publish-config.yaml**：
```yaml
server: "https://update.example.com"
programId: "docufiller"
uploadToken: "ul_xxxxxxxxxxxxx"
encryption:
  enabled: true
  key: "base64密钥"
file: "./app-v1.0.0.zip"
version: "1.0.0"
channel: "stable"
changelog: "更新说明"
```

**update-config.yaml**：
```yaml
server: "https://update.example.com"
programId: "docufiller"
downloadToken: "dl_xxxxxxxxxxxxx"
encryption:
  enabled: true
  key: "base64密钥"
check:
  channel: "stable"
  autoDownload: true
download:
  outputPath: "./updates"
```

## 数据模型

### programs 表
```sql
CREATE TABLE programs (
  id INTEGER PRIMARY KEY,
  program_id TEXT UNIQUE NOT NULL,
  name TEXT NOT NULL,
  description TEXT,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### encryption_keys 表
```sql
CREATE TABLE encryption_keys (
  id INTEGER PRIMARY KEY,
  program_id TEXT UNIQUE NOT NULL,
  key_data BLOB NOT NULL,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (program_id) REFERENCES programs(program_id)
);
```

### tokens 表
```sql
CREATE TABLE tokens (
  id INTEGER PRIMARY KEY,
  token_hash TEXT UNIQUE NOT NULL,
  program_id TEXT NOT NULL,
  token_type TEXT NOT NULL, -- 'upload' or 'download'
  expires_at DATETIME,
  is_active BOOLEAN DEFAULT 1,
  FOREIGN KEY (program_id) REFERENCES programs(program_id)
);
```

### versions 表
```sql
CREATE TABLE versions (
  id INTEGER PRIMARY KEY,
  program_id TEXT NOT NULL,
  version TEXT NOT NULL,
  channel TEXT NOT NULL, -- 'stable' or 'beta'
  file_path TEXT NOT NULL,
  file_size INTEGER,
  file_hash TEXT,
  changelog TEXT,
  download_count INTEGER DEFAULT 0,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (program_id) REFERENCES programs(program_id)
);
```

### admin_users 表
```sql
CREATE TABLE admin_users (
  id INTEGER PRIMARY KEY,
  username TEXT UNIQUE NOT NULL,
  password_hash TEXT NOT NULL,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

## 部署流程

### 旧流程（复杂）
```
配置 config.yaml → 运行 gen-token → 手动配置 Token → 运行服务器
```

### 新流程（简单）
```
解压 → 运行服务器 → 浏览器打开 → 3 步配置 → 开始使用
```

## 使用流程

### 1. 部署服务器
```bash
# 解压并运行
unzip update-server.zip
cd update-server
./update-server.exe
# 浏览器自动打开 http://localhost:8080/setup
```

### 2. 创建程序
```
登录管理后台 → 程序管理 → 创建新程序 → 填写 Program ID
```

### 3. 发布更新
```bash
# 下载 docufiller-publish-client.zip
# 解压并编辑配置
# 运行 publish-client.exe
```

### 4. 集成更新
```bash
# 下载 docufiller-update-client.zip
# 集成到项目
# 启动时运行 update-client.exe --check
```

## 实现计划

### 阶段一：核心重构
- 数据库模型调整
- 初始化流程实现
- 客户端工具目录支持

### 阶段二：Web 管理界面
- 初始化向导
- 程序管理功能
- Token 管理功能
- 动态打包下载

### 阶段三：客户端工具调整
- publish-client 加密功能
- update-client 解密功能

### 阶段四：文档和测试
- 编写使用文档
- 编写架构文档
- 测试完整流程
