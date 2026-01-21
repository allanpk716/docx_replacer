# 版本管理与自动化发布系统设计

**目标**: 基于 Git Tag 的单一数据源版本管理系统，实现构建时自动同步版本和一键发布。

**创建日期**: 2025-01-21

---

## 1. 版本数据流与文件结构

### 版本数据流
```
Git Tag → 构建脚本 → DocuFiller.csproj + update-config.yaml
```

### 文件结构
```
docx_replacer/
├── External/
│   ├── update-client.exe              # 手动放置，不提交
│   ├── update-config.yaml             # 构建时生成，不提交
│   ├── update-config.yaml.template    # 模板文件，提交到 git
│   ├── update-config.local.yaml       # 用户配置，不提交
│   └── .gitignore                     # 忽略规则
├── DocuFiller.csproj                  # 构建时版本被更新
├── scripts/
│   ├── build.bat                      # 修改：添加版本同步和文件复制
│   ├── release.bat                    # 修改：增强 tag 验证，使用 update-publisher
│   ├── sync-version.bat               # 新增：版本同步脚本
│   └── config/
│       ├── release-config.bat         # 修改：使用 update-publisher 配置
│       └── release-config.bat.example # 示例配置
├── Utils/
│   └── VersionHelper.cs               # 新增：版本读取工具类
└── docs/
    └── VERSION_MANAGEMENT.md          # 新增：版本管理文档
```

### 版本规则

**有 tag**（如 v1.0.0, v1.0.0-beta01）：
- DocuFiller.csproj: `<Version>1.0.0</Version>` 或 `<Version>1.0.0-beta01</Version>`
- update-config.yaml: `current_version: "1.0.0"` 或 `current_version: "1.0.0-beta01"`

**无 tag**：
- DocuFiller.csproj: `<Version>1.0.0-dev.abc1234</Version>`
- update-config.yaml: `current_version: "1.0.0-dev.abc1234"`
- 其中 abc1234 是当前 commit 的短 hash（8位）

---

## 2. 构建时版本同步机制

### scripts/sync-version.bat

版本同步核心脚本，每次构建时执行：

```bat
@echo off
setlocal enabledelayedexpansion

REM 获取当前 git tag
git describe --tags --abbrev=0 2>.giterr >.gittag
set /p TAG=<.gittag 2>nul
del .gittag .giterr 2>nul

REM 获取短 commit hash
git rev-parse --short HEAD 2>.giterr >.githash
set /p HASH=<.githash 2>nul
del .githash .giterr 2>nul

REM 决定版本号
if defined TAG (
    REM 有 tag：移除 'v' 前缀
    set VERSION=!TAG:~1!
) else (
    REM 无 tag：使用开发版本
    set VERSION=1.0.0-dev.!HASH!
)

REM 更新 DocuFiller.csproj
powershell -Command "(gc DocuFiller.csproj) -replace '<Version>[^<]+</Version>', '<Version>!VERSION!</Version>' | Out-File -encoding UTF8 DocuFiller.csproj"

REM 生成 update-config.yaml
copy External\update-config.yaml.template External\update-config.yaml
powershell -Command "(gc External\update-config.yaml) -replace '%%VERSION%%', '!VERSION!' | Out-File -encoding UTF8 External\update-config.yaml"

echo Version synchronized: !VERSION!
endlocal
```

### DocuFiller.csproj 修改

添加 PreBuild 目标，在每次构建前同步版本：

```xml
<Target Name="SynchronizeVersion" BeforeTargets="CoreBuild">
  <Exec Command="call $(ProjectDir)scripts\sync-version.bat" />
</Target>
```

### External/update-config.yaml.template

模板文件（提交到 git）：

```yaml
# Update Client Configuration
# 此文件由构建过程自动生成
# 手动配置请修改 update-config.local.yaml

# Application information
app:
  id: "docu-filler"
  name: "DocuFiller"
  current_version: "%%VERSION%%"

# Update server configuration (会被 update-config.local.yaml 覆盖)
server:
  url: "http://192.168.1.100:8080"
```

---

## 3. 增强的 release.bat 验证逻辑

### Tag 验证逻辑

```bat
REM 验证最新 commit 是否有 tag
git describe --tags --abbrev=0 HEAD 2>nul | findstr /i "^v" >nul
if errorlevel 1 (
    echo Error: Latest commit has no tag!
    echo.
    echo Please create and push a tag before releasing:
    echo   git tag v1.0.0
    echo   git push origin v1.0.0
    echo.
    echo For beta releases:
    echo   git tag v1.0.0-beta01
    echo   git push origin v1.0.0-beta01
    exit /b 1
)

REM 获取最新 tag
for /f "tokens=*" %%t in ('git describe --tags --abbrev=0 HEAD') do set LATEST_TAG=%%t

REM 验证 tag 格式
echo !LATEST_TAG! | findstr /i "^v[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*" >nul
if errorlevel 1 (
    echo Error: Invalid tag format: !LATEST_TAG!
    echo Expected format: vX.Y.Z or vX.Y.Z-betaNN
    exit /b 1
)

REM 检测通道
echo !LATEST_TAG! | findstr /i "-beta[0-9][0-9]*$" >nul
if errorlevel 1 (
    set CHANNEL=stable
    set VERSION=!LATEST_TAG:~1!
) else (
    set CHANNEL=beta
    REM 移除 'v' 前缀和 '-betaXX' 后缀
    set VERSION=!LATEST_TAG:~1,-6!
)
```

### 发布流程

1. 验证 tag 存在
2. 运行 build.bat（内部会触发 sync-version.bat）
3. 上传到 update server（使用 update-publisher.exe）

---

## 4. 用户配置管理

### External/.gitignore

```
# Auto-generated files (do not commit)
update-config.yaml

# User-specific configuration (do not commit)
update-config.local.yaml

# Template (tracked by git)
!update-config.yaml.template
```

### External/update-config.local.yaml.example

```yaml
# Update Client Local Configuration
# 复制此文件为 update-config.local.yaml 并修改为你的配置
# 此文件会覆盖 update-config.yaml 中的设置

# Update server configuration
server:
  url: "http://your-update-server:8080"

# Optional: Authentication token (if server requires)
# auth:
#   token: "your-token-here"

# Optional: Update check interval (in hours)
# check:
#   interval: 24
```

### UpdateClientService 修改

读取配置时合并：

```csharp
private UpdateConfig LoadConfig()
{
    var baseConfig = LoadYaml("update-config.yaml");
    var localConfig = LoadYamlIfExists("update-config.local.yaml");

    if (localConfig != null)
    {
        // 合并配置：localConfig 覆盖 baseConfig
        baseConfig.Server.Url = localConfig.Server.Url ?? baseConfig.Server.Url;
        // ... 其他字段
    }

    return baseConfig;
}
```

### 配置优先级

1. `update-config.local.yaml`（用户配置，最高优先级）
2. `update-config.yaml`（构建生成，包含版本）
3. `update-config.yaml.template`（模板，默认值）

---

## 5. 版本显示与运行时读取

### Utils/VersionHelper.cs

```csharp
using System.Reflection;

namespace DocuFiller.Utils
{
    public static class VersionHelper
    {
        /// <summary>
        /// 获取当前应用程序版本
        /// </summary>
        public static string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version?.ToString() ?? "1.0.0.0";

            // 返回主版本.次版本.修订号（去掉构建号）
            var parts = version.Split('.');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            return version;
        }

        /// <summary>
        /// 获取完整的版本信息（包含构建号）
        /// </summary>
        public static string GetFullVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        }

        /// <summary>
        /// 判断是否为开发版本
        /// </summary>
        public static bool IsDevelopmentVersion()
        {
            var version = GetCurrentVersion();
            return version.Contains("-dev.") || version.Contains("-dev");
        }

        /// <summary>
        /// 获取更新通道（从版本号推断）
        /// </summary>
        public static string GetChannel()
        {
            var version = GetCurrentVersion();

            if (version.Contains("-beta")) return "beta";
            if (version.Contains("-alpha")) return "alpha";
            if (version.Contains("-dev")) return "dev";

            return "stable";
        }
    }
}
```

### 在 MainWindowViewModel 中使用

```csharp
// 替换现有的硬编码版本获取
var currentVersion = VersionHelper.GetCurrentVersion();
var channel = VersionHelper.GetChannel();

_logger.LogInformation("当前版本: {Version}, 通道: {Channel}", currentVersion, channel);

var updateInfo = await _updateService.CheckForUpdateAsync(currentVersion, channel);
```

### 窗口标题显示版本

```csharp
// MainWindow.xaml.cs 或 MainWindowViewModel
Title = $"DocuFiller v{VersionHelper.GetCurrentVersion()}";

// 如果是开发版本，添加标识
if (VersionHelper.IsDevelopmentVersion())
{
    Title += " (开发版)";
}
```

---

## 6. update-publisher 集成与打包修复

### 问题分析

1. **打包问题**：`External/update-client.exe` 和 `External/update-config.yaml` 没有被打包进 zip
   - 原因：`dotnet publish -o output` 只会复制 csproj 中 `<CopyToOutputDirectory>` 指定的文件到 `bin/` 目录

2. **上传工具变更**：从 `upload-admin.exe` 改为 `update-publisher.exe`

### 修改后的 scripts/build.bat

```bat
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DocuFiller Build Script
echo ========================================

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM Build and publish (会触发 sync-version.bat)
echo Building...
dotnet publish "%PROJECT_ROOT%\DocuFiller.csproj" -c Release -r win-x64 --self-contained -o "%SCRIPT_DIR%build\publish" -p:PublishSingleFile=false

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

REM Read version from built csproj (sync-version.bat 已更新)
for /f "tokens=2 delims=<> " %%v in ('type "%PROJECT_ROOT%\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
    set VERSION=%%v
)

echo Package version: !VERSION!

REM Copy External files to publish output
echo Copying update client files...
copy "%PROJECT_ROOT%\External\update-client.exe" "%SCRIPT_DIR%build\publish\"
copy "%PROJECT_ROOT%\External\update-config.yaml" "%SCRIPT_DIR%build\publish\"

REM Create zip package
echo Packaging...
cd "%SCRIPT_DIR%build\publish"
tar -a -cf "..\docufiller-!VERSION!.zip" *
cd "%SCRIPT_DIR%.."

REM Clean publish directory
rmdir /s /q "%SCRIPT_DIR%build\publish"

echo ========================================
echo Build completed successfully!
echo Output: build\docufiller-!VERSION!.zip
echo Version: !VERSION!
echo ========================================

endlocal
```

### 修改后的 scripts/release.bat 上传部分

```bat
REM 替换原来的 upload-admin.exe 调用

echo.
echo ========================================
echo Step 2: Uploading to Update Server
echo ========================================
echo Server: !UPDATE_SERVER_URL!
echo Program: docufiller
echo Channel: !CHANNEL!
echo Version: !VERSION!
echo File: !BUILD_FILE!
echo ========================================
echo.

REM Upload using update-publisher.exe
"!UPDATE_PUBLISHER_PATH!" upload ^
  --server !UPDATE_SERVER_URL! ^
  --token !UPDATE_TOKEN! ^
  --program-id docufiller ^
  --channel !CHANNEL! ^
  --version !VERSION! ^
  --file "!BUILD_FILE!" ^
  --notes "Release !VERSION!"

if errorlevel 1 (
    echo.
    echo ========================================
    echo UPLOAD FAILED!
    echo ========================================
    echo.
    echo The build file is available at: !BUILD_FILE!
    echo To retry manually:
    echo   "!UPDATE_PUBLISHER_PATH!" upload --server !UPDATE_SERVER_URL! --token %%UPDATE_TOKEN%% --program-id docufiller --channel !CHANNEL! --version !VERSION! --file "!BUILD_FILE!"
    echo.
    exit /b 1
)
```

### scripts/config/release-config.bat 配置

```bat
REM Update Server Configuration
set UPDATE_SERVER_URL=http://192.168.1.100:8080
set UPDATE_TOKEN=your-api-token-here

REM Update Publisher Path
set UPDATE_PUBLISHER_PATH=C:\Path\To\update-publisher.exe
```

### 发布包内容验证

```
publish/
├── DocuFiller.exe           # 主程序
├── *.dll                    # 依赖库
├── update-client.exe        # 更新客户端（必须）
├── update-config.yaml       # 配置文件（必须）
├── appsettings.json         # 应用配置
├── App.config               # 应用配置
├── Examples/                # 示例文件
├── Templates/               # 模板文件
└── ...其他运行时文件
```

---

## 7. 完整发布流程

### 发布流程图

```
创建 tag (git tag vX.Y.Z)
    ↓
运行 release.bat
    ↓
验证最新 commit 有 tag
    ↓
解析 tag 确定通道和版本
    ↓
运行 build.bat
    ↓
触发 sync-version.bat
    ↓
更新 DocuFiller.csproj 版本
    ↓
生成 update-config.yaml
    ↓
执行 dotnet publish
    ↓
复制 External 文件到发布目录
    ↓
打包成 zip
    ↓
调用 update-publisher.exe upload
    ↓
发布完成
```

### 命令示例

**正式版本发布：**
```bash
# 1. 创建 tag
git tag v1.0.0
git push origin v1.0.0

# 2. 运行发布脚本
cd scripts
release.bat
```

**Beta 版本发布：**
```bash
git tag v1.0.0-beta01
git push origin v1.0.0-beta01
cd scripts
release.bat
```

### 错误处理

- ❌ 无 tag → 提示创建 tag 后退出
- ❌ tag 格式错误 → 提示正确格式后退出
- ❌ 构建失败 → 显示错误，不执行上传
- ❌ 上传失败 → 保留构建文件，可手动重试

---

## 8. 文件清单

### 新增文件

| 文件路径 | 说明 |
|---------|------|
| `scripts/sync-version.bat` | 版本同步核心脚本 |
| `External/update-config.yaml.template` | 配置文件模板 |
| `External/update-config.local.yaml.example` | 用户配置示例 |
| `External/.gitignore` | External 目录忽略规则 |
| `Utils/VersionHelper.cs` | 版本读取工具类 |
| `docs/VERSION_MANAGEMENT.md` | 版本管理文档 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `DocuFiller.csproj` | 添加 PreBuild 目标触发版本同步 |
| `scripts/build.bat` | 添加 External 文件复制到发布包 |
| `scripts/release.bat` | 增强 tag 验证，改用 update-publisher |
| `scripts/config/release-config.bat` | 更新为 update-publisher 配置 |
| `ViewModels/Update/UpdateBannerViewModel.cs` | 使用 VersionHelper 获取版本 |
| `Services/Update/UpdateClientService.cs` | 支持配置文件合并 |

---

## 9. 测试场景

### 场景 1：无 tag 开发构建
```bash
# 当前 commit 无 tag
dotnet build
# 预期：版本为 1.0.0-dev.abc1234
```

### 场景 2：有 tag 发布构建
```bash
git tag v1.0.0
dotnet build
# 预期：版本为 1.0.0
```

### 场景 3：Beta 版本
```bash
git tag v1.0.0-beta01
dotnet build
# 预期：版本为 1.0.0-beta01，通道识别为 beta
```

### 场景 4：发布验证
```bash
# 无 tag 时发布
scripts\release.bat
# 预期：退出并提示创建 tag

# 有 tag 时发布
git tag v1.0.0
scripts\release.bat
# 预期：成功发布到 stable 通道
```

### 场景 5：打包验证
```bash
scripts\build.bat
# 验证 build\docufiller-1.0.0.zip 包含：
# - DocuFiller.exe
# - update-client.exe
# - update-config.yaml
```

---

## 10. 核心设计决策总结

1. ✅ Git Tag 为单一版本数据源
2. ✅ 构建时自动同步版本到 csproj 和 update-config.yaml
3. ✅ 无 tag 时使用 commit hash 格式（1.0.0-dev.abc1234）
4. ✅ update-config.yaml 使用模板 + 占位符方式生成
5. ✅ 用户配置通过 update-config.local.yaml 管理
6. ✅ release.bat 强制要求 tag 存在
7. ✅ 根据 tag 后缀自动识别发布通道
8. ✅ 使用 update-publisher.exe 替代 upload-admin.exe
9. ✅ 修复打包问题，确保 External 文件被包含

### 技术要点

- MSBuild PreBuild 目标触发版本同步
- PowerShell 处理文件内容替换
- Git 命令获取 tag 和 commit hash
- YAML 配置文件合并机制
- update-publisher 命令行参数配置

---

**设计完成，准备进入实施阶段。**
