# External 目录说明

## 目录用途

External 目录包含更新系统所需的外部工具和配置文件。

**重要**: 此目录中的 `update-config.yaml` 的 `current_version` 字段会在每次构建时自动更新，无需手动修改。

## 概述

DocuFiller 支持通过外部更新客户端进行自动更新检查。更新客户端是独立的可执行文件，负责与更新服务器通信并通知应用程序有可用更新。

## 文件说明

### 可执行文件

| 文件 | 说明 | 来源 |
|------|------|------|
| update-client.exe | 更新客户端，用于检查和下载更新 | Update Server 发布包 |
| update-publisher.exe | 发布工具，用于上传版本到更新服务器 | Update Server 发布包 |

### 配置文件

| 文件 | 说明 | 是否提交到 git |
|------|------|----------------|
| update-config.yaml | update-client 使用的配置（current_version 会被构建脚本自动更新） | 否（.gitignore） |
| update-client.config.yaml | update-client.exe 的配置示例 | 是 |

### 文档文件

| 文件 | 说明 |
|------|------|
| publish-client.usage.txt | publish-client（已弃用）的使用说明 |
| update-publisher.usage.txt | update-publisher 的使用说明 |

## 获取更新客户端

### 方法一：从发布包获取

1. 下载 DocuFiller 最新发布包
2. 解压后找到 `update-client.exe` 文件
3. 将文件放置在应用程序的 `External/` 目录下

### 方法二：自行构建

如果你有源代码，可以自行构建更新客户端：

```bash
# 进入更新客户端项目目录
cd update-client-project

# 恢复依赖
dotnet restore

# 构建
dotnet build -c Release

# 发布（可选）
dotnet publish -c Release -r win-x64 --self-contained
```

## 文件配置

### 1. update-client.exe

- **位置**: `External/update-client.exe`
- **作用**: 独立的更新检查服务
- **权限**: 需要执行权限

### 2. update-config.yaml

- **位置**: `External/update-config.yaml`
- **作用**: 更新客户端配置文件
- **格式**: YAML
- **示例内容**:

```yaml
# Update configuration for DocuFiller
# This file is used by the update-client.exe to check for updates

# Server configuration
server:
  url: "https://update.example.com/api"
  timeout: 30  # seconds

# Application information
app:
  id: "docu-filler"
  name: "DocuFiller"
  current_version: "1.0.0"

# Update channels
channels:
  - name: "stable"
    description: "Stable releases"
  - name: "beta"
    description: "Beta releases"
  - name: "dev"
    description: "Development builds"

# Check settings
check:
  auto_check: true
  interval: 86400  # 24 hours in seconds
  on_startup: true
  check_on_startup: true
```

### 3. 配置说明

| 配置项 | 说明 | 默认值 | 必需 |
|--------|------|--------|------|
| server.url | 更新服务器 API 地址 | - | 是 |
| server.timeout | 请求超时时间（秒） | 30 | 否 |
| app.id | 应用程序唯一标识 | - | 是 |
| app.name | 应用程序名称 | - | 是 |
| app.current_version | 当前版本号 | - | 是 |
| check.auto_check | 是否自动检查 | true | 否 |
| check.interval | 检查间隔（秒） | 86400 | 否 |
| check.on_startup | 启动时检查 | true | 否 |

## 构建验证

### 1. 检查文件存在

在构建项目之前，确保以下文件存在：

```bash
# 检查更新客户端
if (-not (Test-Path "External/update-client.exe")) {
    Write-Error "缺少 update-client.exe 文件"
    exit 1
}

# 检查配置文件
if (-not (Test-Path "External/update-config.yaml")) {
    Write-Error "缺少 update-config.yaml 文件"
    exit 1
}
```

### 2. 构建项目

```bash
# 恢复依赖
dotnet restore

# 构建
dotnet build

# 发布（可选）
dotnet publish -c Release -r win-x64 --self-contained
```

### 3. 验证更新功能

1. 启动 DocuFiller 应用程序
2. 检查应用程序启动时是否执行更新检查
3. 如果有可用更新，应该看到更新横幅
4. 点击"查看更新"按钮应打开更新详情窗口

## 故障排除

### 1. 更新客户端未找到

**错误**: `System.IO.FileNotFoundException: Could not find file 'External/update-client.exe'`

**解决方案**:
- 确保 `update-client.exe` 文件存在于 `External/` 目录
- 检查文件权限
- 确认文件名拼写正确

### 2. 配置文件格式错误

**错误**: `YamlDotNet.YamlException: (Line 1, Column 1) Unexpected token while parsing a node`

**解决方案**:
- 检查 YAML 文件语法
- 使用 YAML 验证工具
- 确保缩进正确（使用空格，不要使用 Tab）

### 3. 更新检查失败

**错误**: `HttpRequestException: Connection refused`

**解决方案**:
- 检查网络连接
- 验证服务器 URL 是否正确
- 检查防火墙设置
- 查看 `Logs/` 目录下的详细日志

### 4. 更新横幅不显示

**可能原因**:
- 没有可用更新
- 更新服务返回错误
- UI 线程问题

**调试步骤**:
1. 检查日志文件 `Logs/DocuFiller.log`
2. 手动触发更新检查（如果支持）
3. 检查 `UpdateBannerViewModel` 日志输出
4. 验证数据绑定是否正确

### 5. 权限问题

**错误**: `UnauthorizedAccessException: Access to the path is denied`

**解决方案**:
- 以管理员身份运行应用程序
- 检查 `External/` 目录权限
- 确保应用程序有写入日志文件的权限

## 日志记录

应用程序会在以下位置记录更新相关的日志：

```
Logs/DocuFiller.log
```

日志包含以下信息：
- 更新检查时间
- 服务器响应状态
- 错误详情
- 用户操作记录

## 开发调试

### 1. 启用详细日志

在 `App.config` 中添加：

```xml
<add key="LogLevel" value="Debug"/>
<add key="UpdateLogLevel" value="Debug"/>
```

### 2. 模拟更新检查

创建测试配置文件，使用测试服务器：

```yaml
server:
  url: "http://localhost:8080/api"
  timeout: 5
```

### 3. 手触发布版本

测试时可以手动修改 `current_version` 为较低的版本号来触发更新提示。

## 最佳实践

1. **版本控制**: 不要将 `update-client.exe` 提交到版本控制，只在发布包中包含
2. **配置验证**: 应用程序启动时应验证配置文件格式
3. **错误处理**: 实现优雅的错误处理，不影响主要功能
4. **用户体验**: 更新检查不应阻塞应用程序启动
5. **安全性**: 确保更新客户端来源可信

## 支持

如果遇到问题，请检查：
1. 日志文件
2. 网络连接
3. 文件权限
4. 配置文件格式

联系技术支持时，请提供：
- 应用程序版本
- 操作系统信息
- 错误日志
- 重现步骤

---

## 版本自动同步

### 自动版本管理

DocuFiller 使用 Git Tag 作为单一数据源进行版本管理：

1. **Git Tag 决定版本**：
   - 有 tag（如 `v1.0.0`）→ 使用 tag 版本
   - 无 tag → 使用 `1.0.0-dev.{commit-hash}` 格式

2. **构建时自动同步**：
   - 每次运行 `dotnet build` 或 `scripts\build.bat` 时
   - `sync-version.bat` 自动运行
   - 更新 `DocuFiller.csproj` 中的 `<Version>` 元素
   - 更新 `External/update-config.yaml` 中的 `current_version` 字段

3. **手动运行版本同步**：
   ```bash
   scripts\sync-version.bat
   ```

### 版本命名规范

- **正式版本**: `v1.0.0`, `v1.2.3` 等
- **Beta 版本**: `v1.0.0-beta01`, `v1.0.0-beta02` 等
- **Alpha 版本**: `v1.0.0-alpha01` 等

### 发布流程

1. 创建 tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. 运行发布脚本:
   ```bash
   cd scripts
   release.bat
   ```

详见 [docs/VERSION_MANAGEMENT.md](VERSION_MANAGEMENT.md)
