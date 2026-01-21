# 版本管理与自动化发布系统实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 实现基于 Git Tag 的单一数据源版本管理系统，包括构建时自动同步版本和一键发布功能。

**架构:** 使用 Git Tag 作为唯一版本数据源，通过 MSBuild PreBuild 目标触发 BAT 脚本同步版本到 csproj 和 YAML 配置文件，发布脚本验证 tag 并调用 update-publisher 上传。

**技术栈:** Batch 脚本、PowerShell、MSBuild、Git、C# .NET 8

**前置条件:** External 目录已包含：
- `update-client.exe` - 更新客户端
- `update-publisher.exe` - 发布工具
- `update-config.yaml` - 配置文件（将作为模板）
- `update-client.config.yaml` - update-client 配置示例
- `publish-client.usage.txt` - 使用说明
- `update-publisher.usage.txt` - 使用说明

---

## Task 1: 创建版本同步脚本 sync-version.bat

**Files:**
- Create: `scripts/sync-version.bat`

**Step 1: 创建 sync-version.bat 脚本**

创建脚本文件，实现以下逻辑：
- 获取当前 git tag（如果存在）
- 获取短 commit hash
- 根据 tag 决定版本号（有 tag 使用 tag，无 tag 使用 1.0.0-dev.{hash}）
- 更新 DocuFiller.csproj 中的 Version 元素
- 更新 External/update-config.yaml 中的 current_version

```bat
@echo off
setlocal enabledelayedexpansion

REM Get script directory
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM Get current git tag
git describe --tags --abbrev=0 2>.giterr >.gittag
set /p TAG=<.gittag 2>nul
del .gittag .giterr 2>nul

REM Get short commit hash
git rev-parse --short HEAD 2>.giterr >.githash
set /p HASH=<.githash 2>nul
del .githash .giterr 2>nul

REM Determine version
if defined TAG (
    REM Has tag: remove 'v' prefix
    set VERSION=!TAG:~1!
) else (
    REM No tag: use dev version
    set VERSION=1.0.0-dev.!HASH!
)

REM Update DocuFiller.csproj
powershell -Command "(gc '%PROJECT_ROOT%\DocuFiller.csproj') -replace '<Version>[^<]+</Version>', '<Version>!VERSION!</Version>' | Out-File -encoding UTF8 '%PROJECT_ROOT%\DocuFiller.csproj'"

REM Update update-config.yaml (only the current_version field)
if exist "%PROJECT_ROOT%\External\update-config.yaml" (
    powershell -Command "(gc '%PROJECT_ROOT%\External\update-config.yaml') -replace 'current_version: ''[^\']+''', 'current_version: ''!VERSION!''' | Out-File -encoding UTF8 '%PROJECT_ROOT%\External\update-config.yaml'"
)

echo Version synchronized: !VERSION!

endlocal
```

**Step 2: 验证脚本语法**

运行: `scripts\sync-version.bat`
Expected: 输出 "Version synchronized: 1.0.0-dev.xxxxxxxx" 或 tag 版本号

**Step 3: 手动测试版本同步**

运行: `type DocuFiller.csproj | findstr Version`
Expected: 看到 `<Version>1.0.0-dev.xxxxxxx</Version>` 或对应 tag 版本

**Step 4: 提交**

```bash
git add scripts/sync-version.bat
git commit -m "feat: add version synchronization script"
```

---

## Task 2: 配置 External 目录的 git 规则

**Files:**
- Create: `External/.gitignore`

**Step 1: 创建 External/.gitignore**

确保 update-config.yaml（将被修改）不被提交，但保留 exe 和说明文件：

```
# Generated files (do not commit - will be modified by sync-version.bat)
update-config.yaml

# User-specific configuration (do not commit)
update-config.local.yaml

# Keep executable files (they are in External directory)
# update-client.exe - needed for builds
# update-publisher.exe - needed for releases

# Keep documentation files
# *.usage.txt - usage documentation
# *.config.yaml - configuration examples
```

**Step 2: 验证 git 状态**

运行: `git status External`
Expected: 看到只有 update-config.yaml 被忽略，其他文件显示为未跟踪或已修改

**Step 3: 提交**

```bash
git add External/.gitignore
git commit -m "feat: add External directory gitignore rules"
```

---

## Task 3: 创建 External 目录说明文档

**Files:**
- Create: `docs/EXTERNAL_SETUP.md`

**Step 1: 创建 External 目录说明文档**

```markdown
# External 目录说明

## 目录用途

External 目录包含更新系统所需的外部工具和配置文件。

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

## 配置文件格式

### update-config.yaml

此文件由 update-client.exe 读取，包含：

```yaml
server:
  url: "http://your-server:port"  # 更新服务器地址

app:
  id: "docu-filler"                # 程序 ID
  name: "DocuFiller"               # 程序名称
  current_version: "1.0.0"         # 当前版本（构建时自动更新）

channels:
  - name: "stable"
    description: "Stable releases"
  - name: "beta"
    description: "Beta releases"
  - name: "dev"
    description: "Development builds"

check:
  auto_check: true
  interval: 86400  # 24 小时
  on_startup: true
```

### update-client.config.yaml

update-client.exe 的完整配置示例，参考此文件自定义配置。

## 获取更新工具

从 Update Server 下载最新版本：

1. 登录 Update Server 管理后台
2. 进入「下载客户端」页面
3. 下载并解压到 External 目录

## 构建和发布

- **开发构建**: `dotnet build` - 自动同步版本到 update-config.yaml
- **打包发布**: `scripts\build.bat` - 创建包含 update-client.exe 的发布包
- **发布到服务器**: `scripts\release.bat` - 使用 update-publisher.exe 上传
```

**Step 2: 提交**

```bash
git add docs/EXTERNAL_SETUP.md
git commit -m "docs: add External directory documentation"
```

---

## Task 4: 修改 DocuFiller.csproj 添加 PreBuild 目标

**Files:**
- Modify: `DocuFiller.csproj`

**Step 1: 在 PreBuild 目标后添加版本同步目标**

在现有的 `PreBuild` Target 之后添加：

```xml
<Target Name="SynchronizeVersion" BeforeTargets="CoreBuild">
  <PropertyGroup>
    <SyncVersionScript>$(ProjectDir)scripts\sync-version.bat</SyncVersionScript>
  </PropertyGroup>
  <Exec Command="call &quot;$(SyncVersionScript)&quot;" ContinueOnError="false">
    <Output TaskParameter="ExitCode" PropertyName="SyncVersionExitCode" />
  </Exec>
</Target>
```

**Step 2: 验证构建触发版本同步**

运行: `dotnet build -c Debug`
Expected: 构建过程中看到 "Version synchronized: 1.0.0-dev.xxxxxxx"

运行: `type DocuFiller.csproj | findstr Version`
Expected: 版本已更新为 1.0.0-dev.xxxxxxx

**Step 3: 恢复 csproj 版本（准备下一步）**

运行: `git checkout DocuFiller.csproj`
Reason: 恢复原始状态以便下次测试

**Step 4: 提交**

```bash
git add DocuFiller.csproj
git commit -m "feat: add automatic version synchronization on build"
```

---

## Task 5: 创建 VersionHelper.cs 工具类

**Files:**
- Create: `Utils/VersionHelper.cs`

**Step 1: 创建 VersionHelper.cs**

```csharp
using System.Reflection;

namespace DocuFiller.Utils
{
    /// <summary>
    /// 版本信息辅助工具类
    /// </summary>
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

**Step 2: 验证编译**

运行: `dotnet build -c Debug`
Expected: 编译成功，无错误

**Step 3: 提交**

```bash
git add Utils/VersionHelper.cs
git commit -m "feat: add VersionHelper utility class"
```

---

## Task 6: 修改 UpdateViewModel 使用 VersionHelper

**Files:**
- Modify: `ViewModels/Update/UpdateViewModel.cs`

**Step 1: 添加 using 语句**

在文件顶部添加：

```csharp
using DocuFiller.Utils;
```

**Step 2: 移除 GetCurrentVersion 静态方法**

找到现有的 `GetCurrentVersion()` 静态方法（约在第 378-400 行），删除整个方法。

**Step 3: 验证没有其他引用**

运行: `grep -r "UpdateService.GetCurrentVersion" --include="*.cs"`
Expected: 没有结果（或只有已删除的文件）

**Step 4: 提交**

```bash
git add ViewModels/Update/UpdateViewModel.cs
git commit -m "refactor: remove duplicate GetCurrentVersion method"
```

---

## Task 7: 修改 build.bat 添加 External 文件复制

**Files:**
- Modify: `scripts/build.bat`

**Step 1: 替换整个 build.bat 内容**

完全替换为以下内容：

```bat
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DocuFiller Build Script
echo ========================================

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM Build and publish (triggers sync-version.bat via PreBuild target)
echo Building...
dotnet publish "%PROJECT_ROOT%\DocuFiller.csproj" -c Release -r win-x64 --self-contained -o "%SCRIPT_DIR%build\publish" -p:PublishSingleFile=false

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

REM Read version from built csproj (sync-version.bat has updated it)
for /f "tokens=2 delims=<> " %%v in ('type "%PROJECT_ROOT%\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
    set VERSION=%%v
)

if "!VERSION!"=="" (
    echo Error: Cannot read version from DocuFiller.csproj
    exit /b 1
)

echo Package version: !VERSION!

REM Copy External files to publish output
echo.
echo Copying update client files...

if not exist "%PROJECT_ROOT%\External\update-client.exe" (
    echo Error: update-client.exe not found in External directory
    echo Please place update-client.exe in: %PROJECT_ROOT%\External\
    echo See docs/EXTERNAL_SETUP.md for details
    exit /b 1
)

if not exist "%PROJECT_ROOT%\External\update-config.yaml" (
    echo Error: update-config.yaml not found in External directory
    echo Please ensure sync-version.bat generated this file
    exit /b 1
)

copy "%PROJECT_ROOT%\External\update-client.exe" "%SCRIPT_DIR%build\publish\" >nul
copy "%PROJECT_ROOT%\External\update-config.yaml" "%SCRIPT_DIR%build\publish\" >nul

if errorlevel 1 (
    echo Error: Failed to copy External files
    exit /b 1
)

echo Copied: update-client.exe
echo Copied: update-config.yaml

REM Create zip package
echo.
echo Packaging...
cd "%SCRIPT_DIR%build\publish"
tar -a -cf "..\docufiller-!VERSION!.zip" *
cd "%SCRIPT_DIR%.."

if errorlevel 1 (
    echo Error: Failed to create zip package
    exit /b 1
)

REM Clean publish directory
rmdir /s /q "%SCRIPT_DIR%build\publish" 2>nul

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo Output: build\docufiller-!VERSION!.zip
echo Version: !VERSION!
echo ========================================

endlocal
```

**Step 2: 验证脚本语法**

运行: `scripts\build.bat`
Expected: 成功构建并打包

**Step 3: 验证 zip 内容**

运行: `tar -tf scripts\build\docufiller-*.zip | findstr update`
Expected: 看到包含 update-client.exe 和 update-config.yaml

**Step 4: 提交**

```bash
git add scripts/build.bat
git commit -m "feat: add External files copying to build package"
```

---

## Task 8: 修改 release-config.bat 添加 update-publisher 配置

**Files:**
- Create: `scripts/config/release-config.bat.example`
- Modify: `scripts/config/release-config.bat` (如果存在)

**Step 1: 创建配置示例文件**

创建 `scripts/config/release-config.bat.example`：

```bat
REM Update Server Configuration
set UPDATE_SERVER_URL=http://172.18.200.47:58100
set UPDATE_TOKEN=your-api-token-here

REM Update Publisher Path
REM External directory contains update-publisher.exe
set UPDATE_PUBLISHER_PATH=%PROJECT_ROOT%\External\update-publisher.exe
```

**Step 2: 更新现有配置文件（如果存在）**

如果 `scripts/config/release-config.bat` 已存在，更新为使用 External 目录的 update-publisher.exe：

```bat
REM Update Publisher Configuration
REM Point to External directory
set UPDATE_PUBLISHER_PATH=%PROJECT_ROOT%\External\update-publisher.exe
```

**Step 3: 验证配置文件存在**

运行: `if exist scripts\config\release-config.bat (echo Config exists) else (echo Config not found - use example)`
Expected: 根据实际情况输出

**Step 4: 提交**

```bash
git add scripts/config/
git commit -m "feat: add update-publisher configuration using External directory"
```

---

## Task 9: 修改 release.bat 使用 update-publisher

**Files:**
- Modify: `scripts/release.bat`

**Step 1: 找到 upload-admin.exe 调用部分并替换**

在文件中找到调用 `upload-admin.exe` 的部分（约在第 299-319 行），替换为：

```bat
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

REM Validate update-publisher.exe exists
if not exist "!UPDATE_PUBLISHER_PATH!" (
    echo Error: update-publisher.exe not found at: !UPDATE_PUBLISHER_PATH!
    echo Please check UPDATE_PUBLISHER_PATH in release-config.bat
    echo.
    echo Default path should be: %%PROJECT_ROOT%%\External\update-publisher.exe
    echo See External/update-publisher.usage.txt for usage
    exit /b 1
)

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
    echo See External/update-publisher.usage.txt for more information
    exit /b 1
)
```

**Step 2: 移除旧的 UPLOAD_ADMIN_PATH 配置检查**

找到并删除 `UPLOAD_ADMIN_PATH` 相关的验证代码（约在第 43-48 行）。

**Step 3: 验证脚本语法**

运行: `type scripts\release.bat | findstr UPDATE_PUBLISHER`
Expected: 看到新的 update-publisher 引用

**Step 4: 提交**

```bash
git add scripts/release.bat
git commit -m "feat: replace upload-admin with update-publisher"
```

---

## Task 10: 创建版本管理文档

**Files:**
- Create: `docs/VERSION_MANAGEMENT.md`

**Step 1: 创建版本管理文档**

```markdown
# 版本管理规范

## Tag 命名规范

- **正式版本**：`vX.Y.Z`（如 v1.0.0, v1.2.3）
- **测试版本**：`vX.Y.Z-betaNN`（如 v1.0.0-beta01, v1.0.0-beta02）
- **Alpha 版本**：`vX.Y.Z-alphaNN`（可选）

## 发布流程

1. 创建 tag：
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. 运行发布脚本：
   ```bash
   cd scripts
   release.bat
   ```

## 开发构建

- 无 tag 时，版本自动为 `1.0.0-dev.{commit-hash}`
- 有 tag 时，版本使用 tag 版本号

## 配置文件

- `External/update-config.yaml`：update-client 使用的配置，current_version 会被构建脚本自动更新
- 构建时会自动同步版本到此文件

## 示例

### 正式版本发布
```bash
git tag v1.0.0
git push origin v1.0.0
cd scripts
release.bat
```

### Beta 版本发布
```bash
git tag v1.0.0-beta01
git push origin v1.0.0-beta01
cd scripts
release.bat
```

### 开发构建
```bash
# 无 tag，直接构建
dotnet build
# 版本将为 1.0.0-dev.abc1234
# External/update-config.yaml 中的 current_version 会自动更新
```

## External 目录

External 目录包含更新系统所需的工具：
- `update-client.exe` - 更新客户端
- `update-publisher.exe` - 发布工具
- `update-config.yaml` - 配置文件（版本自动更新）
- `update-client.config.yaml` - 配置示例

详见 [docs/EXTERNAL_SETUP.md](EXTERNAL_SETUP.md)
```

**Step 2: 提交**

```bash
git add docs/VERSION_MANAGEMENT.md
git commit -m "docs: add version management documentation"
```

---

## Task 11: 端到端测试 - 无 tag 开发构建

**Files:**
- Test: Manual testing

**Step 1: 确保无 tag**

运行: `git describe --tags --abbrev=0 HEAD 2>&1 | findstr "fatal"`
Expected: 看到 fatal 错误（表示无 tag）

**Step 2: 清理旧构建**

运行: `if exist scripts\build rmdir /s /q scripts\build`

**Step 3: 执行构建**

运行: `scripts\build.bat`
Expected:
- 看到 "Version synchronized: 1.0.0-dev.xxxxxxx"
- 构建成功
- 生成 `scripts\build\docufiller-1.0.0-dev.xxxxxxx.zip`

**Step 4: 验证版本**

运行: `type DocuFiller.csproj | findstr Version`
Expected: 看到 `<Version>1.0.0-dev.xxxxxxx</Version>`

**Step 5: 验证 update-config.yaml 内容**

运行: `type External\update-config.yaml | findstr current_version`
Expected: 看到 `current_version: "1.0.0-dev.xxxxxxx"`

**Step 6: 验证 zip 内容**

运行: `tar -tf scripts\build\docufiller-*.zip | findstr -E "update-client|update-config"`
Expected: 看到两个文件都包含在内

**Step 7: 提交测试结果（文档）**

如果测试通过，无需额外提交（这是验证步骤）

---

## Task 12: 端到端测试 - 有 tag 发布构建

**Files:**
- Test: Manual testing

**Step 1: 创建测试 tag**

运行: `git tag v1.0.0-test`

**Step 2: 清理旧构建和文件**

运行:
```bat
if exist scripts\build rmdir /s /q scripts\build
git checkout DocuFiller.csproj
```

**Step 3: 执行构建**

运行: `scripts\build.bat`
Expected:
- 看到 "Version synchronized: 1.0.0-test"
- 构建成功
- 生成 `scripts\build\docufiller-1.0.0-test.zip`

**Step 4: 验证版本**

运行: `type DocuFiller.csproj | findstr Version`
Expected: 看到 `<Version>1.0.0-test</Version>`

**Step 5: 验证 update-config.yaml 内容**

运行: `type External\update-config.yaml | findstr current_version`
Expected: 看到 `current_version: "1.0.0-test"`

**Step 6: 清理测试 tag**

运行:
```bat
git tag -d v1.0.0-test
git checkout DocuFiller.csproj
```

**Step 7: 提交测试结果（文档）**

如果测试通过，无需额外提交（这是验证步骤）

---

## Task 13: 端到端测试 - release.bat tag 验证

**Files:**
- Test: Manual testing

**Step 1: 测试无 tag 时应失败**

运行:
```bat
git describe --tags --abbrev=0 HEAD 2>&1 | findstr "fatal"
if errorlevel 1 (
    echo No tag exists - good for testing
)
```

运行: `scripts\release.bat 2>&1 | findstr "Error"`
Expected: 看到 "Error: Latest commit has no tag!" 或类似错误

**Step 2: 创建测试 tag 并验证发布流程（不上传）**

运行: `git tag v1.0.0-test`

注意：完整测试需要有效的 UPDATE_PUBLISHER_PATH 和 UPDATE_TOKEN

**Step 3: 清理测试 tag**

运行:
```bat
git tag -d v1.0.0-test
git checkout DocuFiller.csproj
```

**Step 4: 提交测试结果（文档）**

如果测试通过，无需额外提交（这是验证步骤）

---

## 完成检查清单

- [ ] sync-version.bat 正确同步版本到 csproj 和 update-config.yaml
- [ ] External/.gitignore 配置正确
- [ ] External 目录说明文档创建
- [ ] DocuFiller.csproj PreBuild 目标添加
- [ ] VersionHelper.cs 工具类创建
- [ ] UpdateViewModel 使用 VersionHelper
- [ ] build.bat 复制 External 文件
- [ ] release-config.bat 使用 External 目录的 update-publisher
- [ ] release.bat 使用 update-publisher
- [ ] 版本管理文档创建
- [ ] 无 tag 开发构建测试通过
- [ ] 有 tag 发布构建测试通过
- [ ] release.bat tag 验证测试通过

---

**计划完成。准备执行。**
