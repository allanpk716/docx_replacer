# DocuFiller 自动更新系统设计文档

**日期**: 2025-01-15
**版本**: 1.0
**状态**: 设计完成

---

## 1. 概述

### 1.1 目标

为 DocuFiller WPF 应用程序实现自动更新功能，支持稳定版和测试版两个渠道，用户可以选择是否更新到最新版本。

### 1.2 核心需求

- 支持稳定版（stable）和测试版（beta）两个发布渠道
- 客户端启动时静默检查更新
- 自动下载更新包，用户确认后安装
- 完整安装包替换方式
- 不支持版本回滚
- 局域网环境部署

### 1.3 技术选型

| 组件 | 技术栈 |
|------|--------|
| 更新服务器 | Go 1.21+ + GORM + Gin |
| 数据库 | SQLite |
| 客户端 | C# WPF (.NET 8) |
| 通信协议 | REST API (HTTP) |
| 日志库 | github.com/WQGroup/logger |

---

## 2. 系统架构

### 2.1 整体架构图

```
┌─────────────────┐
│   发布者        │
│  (开发机)       │
└────────┬────────┘
         │ 脚本发布
         ▼
┌─────────────────────────────────────────┐
│      Go 更新服务器 (Windows Server)       │
│  ┌────────────────────────────────────┐  │
│  │  REST API                          │  │
│  │  - 版本查询                        │  │
│  │  - 版本上传 (需认证)               │  │
│  │  - 文件下载                        │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │  GORM + SQLite                     │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │  文件存储                          │  │
│  │  - stable/                         │  │
│  │  - beta/                           │  │
│  └────────────────────────────────────┘  │
└───────────────────┬─────────────────────┘
                    │ REST API
                    ▼
┌─────────────────────────────────────────┐
│      DocuFiller 客户端 (WPF)              │
│  ┌────────────────────────────────────┐  │
│  │  UpdateService                     │  │
│  │  - 检查更新                        │  │
│  │  - 下载更新                        │  │
│  │  - 安装更新                        │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │  UI 组件                           │  │
│  │  - 更新通知                        │  │
│  │  - 更新窗口                        │  │
│  │  - 下载进度                        │  │
│  └────────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### 2.2 数据流

```
1. 发布流程
   发布者 → build.bat → 编译 WPF 程序
          → publish.bat → 上传到 Go 服务器
          → SQLite + 文件系统

2. 更新检查流程
   DocuFiller 启动
          → UpdateService.CheckForUpdateAsync()
          → GET /api/version/latest
          → 比对版本号
          → 显示通知

3. 下载安装流程
   用户确认更新
          → 后台下载到临时目录
          → 显示下载进度
          → 下载完成，验证哈希
          → 启动安装程序
          → 关闭当前应用
```

---

## 3. Go 更新服务器设计

### 3.1 项目结构

```
docufiller-update-server/
├── main.go                          # 程序入口
├── go.mod                           # 依赖管理
├── config.yaml                      # 配置文件
├── internal/
│   ├── config/
│   │   └── config.go                # 配置加载
│   ├── models/
│   │   └── version.go               # GORM 模型定义
│   ├── database/
│   │   └── gorm.go                  # GORM 初始化
│   ├── handler/
│   │   ├── version.go               # 版本查询处理器
│   │   ├── upload.go                # 上传处理器
│   │   └── download.go              # 下载处理器
│   ├── service/
│   │   ├── version.go               # 版本业务逻辑
│   │   └── storage.go               # 文件存储服务
│   ├── middleware/
│   │   └── auth.go                  # API 认证中间件
│   └── logger/
│       └── logger.go                # 日志初始化
├── data/
│   ├── versions.db                  # SQLite 数据库
│   └── packages/                    # 安装包存储
│       ├── stable/
│       └── beta/
└── logs/                            # 日志文件
```

### 3.2 数据模型

```go
// internal/models/version.go
type Version struct {
    gorm.Model
    Version      string    `gorm:"type:varchar(20);uniqueIndex:idx_version_channel" json:"version"`
    Channel      string    `gorm:"type:varchar(10);uniqueIndex:idx_version_channel" json:"channel"`
    FileName     string    `gorm:"type:varchar(255);not null" json:"fileName"`
    FilePath     string    `gorm:"type:varchar(500);not null" json:"filePath"`
    FileSize     int64     `json:"fileSize"`
    FileHash     string    `gorm:"type:varchar(64);not null" json:"fileHash"`
    ReleaseNotes string    `gorm:"type:text" json:"releaseNotes"`
    PublishDate  time.Time `json:"publishDate"`
    DownloadCount int64    `gorm:"default:0" json:"downloadCount"`
    Mandatory    bool      `gorm:"default:false" json:"mandatory"`
}
```

### 3.3 API 端点

| 方法 | 路径 | 描述 | 认证 |
|------|------|------|------|
| GET | `/api/health` | 健康检查 | 否 |
| GET | `/api/version/latest` | 获取最新版本 | 否 |
| GET | `/api/version/list` | 获取版本列表 | 否 |
| GET | `/api/version/:channel/:version` | 获取版本详情 | 否 |
| POST | `/api/version/upload` | 上传新版本 | 是 |
| GET | `/api/download/:channel/:version` | 下载安装包 | 否 |
| DELETE | `/api/version/:channel/:version` | 删除版本 | 是 |

### 3.4 配置文件

```yaml
# config.yaml
server:
  port: 8080
  host: "0.0.0.0"

database:
  path: "./data/versions.db"

storage:
  basePath: "./data/packages"
  maxFileSize: 536870912  # 512MB

api:
  uploadToken: "your-secret-token-here"
  corsEnable: true

logger:
  level: "info"              # debug, info, warn, error
  output: "both"             # console, file, both
  filePath: "./logs/server.log"
  maxSize: 10485760          # 10MB
  maxBackups: 5
  maxAge: 30                 # 天
  compress: true
```

### 3.5 核心依赖

```go
require (
    github.com/WQGroup/logger v0.0.0          # 日志库
    github.com/gin-gonic/gin v1.9.1           # Web 框架
    gorm.io/gorm v1.25.5                      # ORM
    gorm.io/driver/sqlite v1.25.4             # SQLite 驱动
    gopkg.in/yaml.v3 v3.0.1                   # YAML 配置
)
```

### 3.6 文件存储结构

```
data/packages/
├── stable/
│   ├── 1.0.0/
│   │   ├── docufiller-1.0.0.zip
│   │   └── metadata.json
│   ├── 1.1.0/
│   │   ├── docufiller-1.1.0.zip
│   │   └── metadata.json
│   └── 1.2.0/
│       ├── docufiller-1.2.0.zip
│       └── metadata.json
└── beta/
    ├── 1.3.0-beta1/
    │   ├── docufiller-1.3.0-beta1.zip
    │   └── metadata.json
    └── 1.3.0-beta2/
        ├── docufiller-1.3.0-beta2.zip
        └── metadata.json
```

---

## 4. 发布脚本系统

### 4.1 目录结构

```
scripts/
├── build.bat                       # 编译 WPF 程序
├── publish.bat                     # 发布到更新服务器
├── build-and-publish.bat           # 一键编译+发布
└── config/
    └── publish-config.bat          # 发布配置
```

### 4.2 发布配置

```batch
# scripts/config/publish-config.bat
set UPDATE_SERVER_URL=http://192.168.1.100:8080
set UPDATE_SERVER_TOKEN=your-secret-token-here
set DEFAULT_CHANNEL=stable
```

### 4.3 编译脚本 (build.bat)

```batch
@echo off
echo ========================================
echo DocuFiller Build Script
echo ========================================

REM 从 .csproj 读取版本号
for /f "tokens=2 delims==" %%a in ('findstr /r "^.*Version>.*<" ..\DocuFiller.csproj') do (
    set VERSION_LINE=%%a
    for /f "tokens=2 delims=<>" %%b in ("%%a") do set VERSION=%%b
)

echo Building version: %VERSION%

REM 清理旧的构建输出
if exist "build" rmdir /s /q "build"
mkdir "build"

REM 编译发布
dotnet publish ..\DocuFiller.csproj -c Release -r win-x64 --self-contained -o "build\temp"

REM 打包
cd build\temp
tar -a -cf ..\docufiller-%VERSION%.zip .
cd ..\..

echo ========================================
echo Build completed!
echo Output: build\docufiller-%VERSION%.zip
echo ========================================
```

### 4.4 发布脚本 (publish.bat)

```batch
@echo off
if "%1"=="" (
    echo Usage: publish.bat [stable^|beta] [version]
    exit /b 1
)

set CHANNEL=%1
set VERSION=%2

REM 加载配置
call config\publish-config.bat

REM 检查文件
if not exist "build\docufiller-%VERSION%.zip" (
    echo Error: Build file not found!
    exit /b 1
)

echo Publishing %CHANNEL% version %VERSION%...

REM 调用 API 上传
curl -X POST "%UPDATE_SERVER_URL%/api/version/upload" ^
  -H "Authorization: Bearer %UPDATE_SERVER_TOKEN%" ^
  -F "channel=%CHANNEL%" ^
  -F "version=%VERSION%" ^
  -F "file=@build\docufiller-%VERSION%.zip" ^
  -F "mandatory=false"

echo Publish completed!
```

### 4.5 使用示例

```batch
# 发布稳定版
scripts\build-and-publish.bat stable

# 发布测试版
scripts\build-and-publish.bat beta
```

---

## 5. WPF 客户端更新组件

### 5.1 项目结构

```
DocuFiller/
├── Services/
│   └── Update/
│       ├── IUpdateService.cs        # 更新服务接口
│       ├── UpdateService.cs         # 更新服务实现
│       ├── UpdateChecker.cs         # 版本检查
│       ├── UpdateDownloader.cs      # 下载管理
│       └── UpdateInstaller.cs       # 安装执行
├── ViewModels/
│   └── Update/
│       ├── UpdateViewModel.cs       # 更新窗口 ViewModel
│       └── UpdateNotificationViewModel.cs
├── Views/
│   └── Update/
│       ├── UpdateWindow.xaml        # 更新窗口
│       └── UpdateNotification.xaml  # 更新通知控件
└── Models/
    └── Update/
        ├── VersionInfo.cs           # 版本信息模型
        ├── UpdateConfig.cs          # 更新配置
        └── DownloadProgress.cs      # 下载进度
```

### 5.2 版本信息模型

```csharp
public class VersionInfo
{
    public string Version { get; set; }              // 1.2.0
    public string Channel { get; set; }              // stable/beta
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileHash { get; set; }             // SHA256
    public string ReleaseNotes { get; set; }
    public DateTime PublishDate { get; set; }
    public bool Mandatory { get; set; }
    public string DownloadUrl { get; set; }
    public bool IsDownloaded { get; set; }
}

public class UpdateConfig
{
    public string ServerUrl { get; set; } = "http://192.168.1.100:8080";
    public string Channel { get; set; } = "stable";
    public bool CheckOnStartup { get; set; } = true;
    public bool AutoDownload { get; set; } = true;
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(24);
}
```

### 5.3 更新服务接口

```csharp
public interface IUpdateService
{
    Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel);
    Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress);
    Task<bool> InstallUpdateAsync(string packagePath);
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
}
```

### 5.4 主窗口集成

```csharp
public partial class MainViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;

    protected override async Task OnInitializedAsync()
    {
        // 启动时检查更新
        if (_config.CheckOnStartup)
        {
            await CheckForUpdatesAsync();
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        var currentVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "1.0.0";

        var update = await _updateService.CheckForUpdateAsync(currentVersion, _config.Channel);

        if (update != null)
        {
            ShowUpdateNotification(update);
        }
    }
}
```

### 5.5 客户端配置

```xml
<!-- App.config -->
<appSettings>
  <add key="UpdateServerUrl" value="http://192.168.1.100:8080" />
  <add key="UpdateChannel" value="stable" />
  <add key="CheckUpdateOnStartup" value="true" />
  <add key="AutoDownloadUpdate" value="true" />
</appSettings>
```

---

## 6. 安全性设计

### 6.1 API 认证

上传操作使用 Bearer Token 认证：

```go
func AuthMiddleware() gin.HandlerFunc {
    return func(c *gin.Context) {
        if !strings.Contains(c.Request.URL.Path, "/upload") {
            c.Next()
            return
        }

        authHeader := c.GetHeader("Authorization")
        if authHeader == "" {
            c.JSON(401, gin.H{"error": "Unauthorized"})
            c.Abort()
            return
        }

        token := strings.TrimPrefix(authHeader, "Bearer ")
        if token != uploadToken {
            c.JSON(403, gin.H{"error": "Forbidden"})
            c.Abort()
            return
        }

        c.Next()
    }
}
```

### 6.2 文件完整性验证

下载完成后验证 SHA256 哈希：

```csharp
public async Task<bool> VerifyFileHash(string filePath, string expectedHash)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);

    var hash = await sha256.ComputeHashAsync(stream);
    var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

    return hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
}
```

---

## 7. 部署方案

### 7.1 服务器部署

**Windows 服务器部署**

1. 复制 `docufiller-update-server.exe` 到服务器
2. 复制并编辑 `config.yaml`
3. 创建 Windows 服务（可选）：
   ```batch
   sc create DocuFillerUpdateServer binPath="C:\path\to\exe" start=auto
   sc start DocuFillerUpdateServer
   ```
4. 验证服务运行：访问 `http://server:8080/api/health`

### 7.2 开发机配置

```
开发机
├── DocuFiller/                     # 源代码
├── scripts/                         # 发布脚本
│   ├── build.bat
│   ├── publish.bat
│   └── config/
│       └── publish-config.bat       # 配置服务器地址和 Token
└── build/                           # 编译输出（临时）
```

---

## 8. 测试策略

### 8.1 单元测试

**Go 服务器测试**
```
internal/handler/version_test.go
internal/service/version_test.go
internal/database/gorm_test.go
```

**C# 客户端测试**
```
Services/Update/UpdateServiceTests.cs
Models/Update/VersionInfoTests.cs
ViewModels/Update/UpdateViewModelTests.cs
```

### 8.2 手动测试清单

- [ ] 发布稳定版到服务器
- [ ] 发布测试版到服务器
- [ ] 客户端启动时检测到更新
- [ ] 下载进度显示正确
- [ ] 下载完成后校验文件完整性
- [ ] 安装程序正确启动
- [ ] 更新后版本号正确
- [ ] 强制更新功能正常
- [ ] 拒绝更新后可以下次再提醒
- [ ] 网络断开时处理正确
- [ ] 服务器不可达时提示用户

---

## 9. 监控和日志

### 9.1 日志配置

**服务器端**（使用 `github.com/WQGroup/logger`）

```
logs/
├── server-2024-01-15.log
├── server-2024-01-14.log.gz
└── server-2024-01-13.log.gz
```

**客户端端**

```
Logs/
└── update-2024-01-15.log
```

### 9.2 统计查询

```sql
SELECT
    channel,
    COUNT(*) as download_count,
    SUM(download_count) as total_downloads,
    MAX(publish_date) as latest_publish
FROM versions
GROUP BY channel;
```

---

## 10. 故障排查

| 问题 | 可能原因 | 解决方案 |
|------|----------|----------|
| 无法检测到更新 | 网络不通、URL错误 | 检查服务器地址和防火墙 |
| 下载失败 | 文件不存在、权限问题 | 检查服务器日志和文件权限 |
| 安装失败 | 安装包损坏、权限不足 | 验证文件哈希、以管理员身份运行 |
| 版本号比较错误 | 格式不一致 | 统一使用 Semantic Versioning |

---

## 11. 实现计划

### 阶段一：Go 服务器开发
1. 搭建项目结构
2. 实现数据模型和 GORM 集成
3. 实现 REST API 端点
4. 集成日志系统
5. 测试 API 功能

### 阶段二：发布脚本开发
1. 创建编译脚本
2. 创建发布脚本
3. 测试完整发布流程

### 阶段三：WPF 客户端开发
1. 创建更新服务
2. 实现 UI 组件
3. 集成到主应用
4. 测试更新流程

### 阶段四：集成测试
1. 端到端测试
2. 性能测试
3. 安全性测试

---

## 12. 附录

### 12.1 版本号规范

使用 Semantic Versioning：`MAJOR.MINOR.PATCH`

- MAJOR：不兼容的 API 变更
- MINOR：向后兼容的新功能
- PATCH：向后兼容的问题修复

### 12.2 API 响应示例

**获取最新版本**
```json
GET /api/version/latest?channel=stable

Response:
{
  "version": "1.2.0",
  "channel": "stable",
  "fileName": "docufiller-1.2.0.zip",
  "fileSize": 52428800,
  "fileHash": "a1b2c3d4...",
  "releaseNotes": "修复了xxx问题",
  "publishDate": "2025-01-15T10:00:00Z",
  "mandatory": false,
  "downloadCount": 42
}
```

---

**文档结束**
