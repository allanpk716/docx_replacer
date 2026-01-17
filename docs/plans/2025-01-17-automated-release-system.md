# Automated Release System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 创建本地一键发布系统，通过 Git 标签自动编译 DocuFiller 并发布到远端 update-server

**Architecture:** 本地 BAT 脚本检测 Git 标签，解析渠道类型（stable/beta），调用现有 build.bat 编译，使用 upload-admin.exe 上传到 172.18.200.47:58100

**Tech Stack:** Windows Batch, Git CLI, upload-admin.exe (Go tool)

---

## Task 1: Create release configuration template

**Files:**
- Create: `scripts/config/release-config.bat.example`
- Modify: `.gitignore`

**Step 1: Create configuration template**

Create `scripts/config/release-config.bat.example`:

```bat
@echo off
REM Update Server Configuration for DocuFiller Release
REM This file is a template. Copy to release-config.bat and fill in your values.

REM Update Server URL (your deployed server address)
set UPDATE_SERVER_URL=http://172.18.200.47:58100

REM Upload Token for authentication (must match your server's config)
set UPDATE_TOKEN=change-this-token-in-production

REM Path to upload-admin.exe tool
set UPLOAD_ADMIN_PATH=C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\bin\upload-admin.exe
```

**Step 2: Update .gitignore**

Add to `.gitignore`:

```gitignore
# Release configuration (contains sensitive token)
scripts/config/release-config.bat
```

**Step 3: Commit**

```bash
git add scripts/config/release-config.bat.example .gitignore
git commit -m "feat: add release configuration template"
```

---

## Task 2: Create release script skeleton

**Files:**
- Create: `scripts/release.bat`

**Step 1: Create script with error handling and structure**

Create `scripts/release.bat`:

```bat
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DocuFiller Release Script
echo ========================================
echo.

REM Get script directory
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..

REM ========================================
REM Load Configuration
REM ========================================
if exist "%SCRIPT_DIR%config\release-config.bat" (
    call "%SCRIPT_DIR%config\release-config.bat"
) else (
    echo Error: Configuration file not found!
    echo.
    echo Please create release-config.bat from the template:
    echo   copy scripts\config\release-config.bat.example scripts\config\release-config.bat
    echo Then edit it with your server settings.
    exit /b 1
)

REM Validate required configuration
if "!UPDATE_SERVER_URL!"=="" (
    echo Error: UPDATE_SERVER_URL not set in release-config.bat
    exit /b 1
)

if "!UPDATE_TOKEN!"=="" (
    echo Error: UPDATE_TOKEN not set in release-config.bat
    exit /b 1
)

if "!UPLOAD_ADMIN_PATH!"=="" (
    echo Error: UPLOAD_ADMIN_PATH not set in release-config.bat
    exit /b 1
)

REM Check upload-admin.exe exists
if not exist "!UPLOAD_ADMIN_PATH!" (
    echo Error: upload-admin.exe not found at: !UPLOAD_ADMIN_PATH!
    echo Please check UPLOAD_ADMIN_PATH in release-config.bat
    exit /b 1
)

endlocal
```

**Step 2: Test script execution**

Run: `scripts\release.bat`
Expected: Error about missing git tag (we haven't implemented that yet)

**Step 3: Commit**

```bash
git add scripts/release.bat
git commit -m "feat: add release script skeleton with config loading"
```

---

## Task 3: Implement Git tag detection and parsing

**Files:**
- Modify: `scripts/release.bat`

**Step 1: Add tag detection logic**

In `scripts/release.bat`, after the config validation section, add:

```bat
REM ========================================
REM Detect Git Tag
REM ========================================

REM Try to get tag from command line parameter
set TAG_FROM_PARAM=%1
set CHANNEL_FROM_PARAM=%2

REM If parameters provided, use them
if not "%TAG_FROM_PARAM%"=="" (
    set TAG_TO_USE=%TAG_FROM_PARAM%
    if "%CHANNEL_FROM_PARAM%"=="" (
        echo Error: When specifying version, you must also specify channel
        echo Usage: release.bat [stable^|beta] [version]
        echo Example: release.bat stable 1.0.0
        exit /b 1
    )
    set USER_DEFINED_CHANNEL=%CHANNEL_FROM_PARAM%
    goto :TagDetected
)

REM Otherwise, try to get current git tag
echo Detecting git tag...
for /f "delims=" %%t in ('git describe --tags --abbrev=0 2^>nul') do (
    set CURRENT_TAG=%%t
)

if "!CURRENT_TAG!"=="" (
    echo Error: No git tag found.
    echo.
    echo Please create and push a tag first, or specify version manually:
    echo.
    echo Using git tag:
    echo   git tag v1.0.0
    echo   git push origin v1.0.0
    echo.
    echo Using parameters:
    echo   release.bat stable 1.0.0
    exit /b 1
)

set TAG_TO_USE=!CURRENT_TAG!
echo Found tag: !TAG_TO_USE!

:TagDetected
```

**Step 2: Test tag detection**

Run: `scripts\release.bat`
Expected: Error about no git tag (or displays tag if one exists)

Create a test tag:
```bash
git tag v0.0.1-test
scripts\release.bat
```
Expected: Displays "Found tag: v0.0.1-test"

**Step 3: Commit**

```bash
git add scripts/release.bat
git commit -m "feat: add git tag detection to release script"
```

---

## Task 4: Implement channel and version parsing

**Files:**
- Modify: `scripts/release.bat`

**Step 1: Add parsing logic**

In `scripts/release.bat`, after the tag detection, add:

```bat
REM ========================================
REM Parse Channel and Version
REM ========================================

if defined USER_DEFINED_CHANNEL (
    REM Use provided channel and version
    set CHANNEL=%USER_DEFINED_CHANNEL%
    set VERSION=%TAG_TO_USE%
    echo.
    echo ========================================
    echo Manual Release Mode
    echo ========================================
    echo Channel: !CHANNEL!
    echo Version: !VERSION!
    goto :ParsingComplete
)

REM Auto-detect from git tag
echo.
echo ========================================
echo Parsing tag: !TAG_TO_USE!
echo ========================================

REM Check if tag starts with 'v'
echo !TAG_TO_USE! | findstr /i "^v" >nul
if errorlevel 1 (
    echo Error: Tag must start with 'v' (e.g., v1.0.0 or v1.0.0-beta)
    echo Invalid tag: !TAG_TO_USE!
    exit /b 1
)

REM Check for -beta suffix
echo !TAG_TO_USE! | findstr /i "-beta$" >nul
if errorlevel 1 (
    REM No -beta suffix = stable channel
    set CHANNEL=stable
    set VERSION=!TAG_TO_USE:~1!
    echo Detected: STABLE release
) else (
    REM Has -beta suffix = beta channel
    set CHANNEL=beta
    REM Remove 'v' prefix and '-beta' suffix
    set VERSION=!TAG_TO_USE:~1,-5!
    echo Detected: BETA release
)

echo Version: !VERSION!

:ParsingComplete
echo.

REM Validate version format
echo !VERSION! | findstr /i "^[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*$" >nul
if errorlevel 1 (
    echo Error: Invalid version format: !VERSION!
    echo Expected format: x.y.z (e.g., 1.0.0)
    exit /b 1
)

echo ========================================
echo Release Summary
echo ========================================
echo Channel: !CHANNEL!
echo Version: !VERSION!
echo ========================================
echo.
```

**Step 2: Test parsing**

Test with stable tag:
```bash
git tag v1.0.0-test-stable
git describe --tags --abbrev=0
scripts\release.bat
```
Expected: Shows "Detected: STABLE release", "Version: 1.0.0-test-stable"

Test with beta tag:
```bash
git tag v1.0.0-test-beta
git describe --tags --abbrev=0
scripts\release.bat
```
Expected: Shows "Detected: BETA release", "Version: 1.0.0-test"

**Step 3: Commit**

```bash
git add scripts/release.bat
git commit -m "feat: add channel and version parsing logic"
```

---

## Task 5: Integrate build process

**Files:**
- Modify: `scripts/release.bat`

**Step 1: Add build invocation**

After the parsing section, add:

```bat
REM ========================================
REM Build Project
REM ========================================

echo.
echo ========================================
echo Step 1: Building DocuFiller
echo ========================================

call "%SCRIPT_DIR%build.bat"
if errorlevel 1 (
    echo.
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    echo Release aborted due to build failure.
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================

REM Verify build output exists
set BUILD_FILE=%SCRIPT_DIR%build\docufiller-!VERSION!.zip
if not exist "!BUILD_FILE!" (
    echo Error: Build file not found: !BUILD_FILE!
    echo.
    echo Expected: build\docufiller-!VERSION!.zip
    echo Please check if build.bat completed successfully.
    exit /b 1
)

echo Build output: !BUILD_FILE!
```

**Step 2: Test build integration**

Note: This will run a full build, so it may take a minute
Run: `scripts\release.bat stable 0.0.1`
Expected: Builds project, creates zip file

Verify file exists:
```cmd
dir scripts\build\docufiller-0.0.1.zip
```

**Step 3: Commit**

```bash
git add scripts/release.bat
git commit -m "feat: integrate build process into release script"
```

---

## Task 6: Implement upload to update-server

**Files:**
- Modify: `scripts/release.bat`

**Step 1: Add upload logic**

After the build section, add:

```bat
REM ========================================
REM Upload to Update Server
REM ========================================

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

REM Upload using upload-admin.exe
"!UPLOAD_ADMIN_PATH!" upload ^
  --program-id docufiller ^
  --channel !CHANNEL! ^
  --version !VERSION! ^
  --file "!BUILD_FILE!" ^
  --server !UPDATE_SERVER_URL! ^
  --token !UPDATE_TOKEN!

if errorlevel 1 (
    echo.
    echo ========================================
    echo UPLOAD FAILED!
    echo ========================================
    echo.
    echo The build file is available at: !BUILD_FILE!
    echo You can retry upload manually with:
    echo.
    echo "!UPLOAD_ADMIN_PATH!" upload --program-id docufiller --channel !CHANNEL! --version !VERSION! --file "!BUILD_FILE!" --server !UPDATE_SERVER_URL! --token YOUR_TOKEN
    echo.
    exit /b 1
)

echo.
echo ========================================
echo Release completed successfully!
echo ========================================
echo.
echo Summary:
echo   Program: docufiller
echo   Channel: !CHANNEL!
echo   Version: !VERSION!
echo   Server: !UPDATE_SERVER_URL!
echo.
echo You can verify the release at:
echo   !UPDATE_SERVER_URL!/api/version/latest?channel=!CHANNEL!
echo.
echo ========================================

endlocal
```

**Step 2: Manual testing (requires real server)**

Before running, ensure:
1. `release-config.bat` is created with correct values
2. update-server at 172.18.200.47:58100 is running
3. Token is correct

Test with a dummy version:
```cmd
scripts\release.bat beta 0.0.1-test
```

Expected:
- Builds project
- Uploads to server
- Shows success message

**Step 3: Commit**

```bash
git add scripts/release.bat
git commit -m "feat: add upload to update-server"
```

---

## Task 7: Add version validation against csproj

**Files:**
- Modify: `scripts/release.bat`

**Step 1: Add version consistency check**

After parsing section, add:

```bat
REM ========================================
REM Validate Version Consistency
REM ========================================

echo Checking version consistency with DocuFiller.csproj...

for /f "tokens=2 delims=<> " %%v in ('type "%PROJECT_ROOT%\DocuFiller.csproj" ^| findstr /i "<Version>"') do (
    set CSPROJ_VERSION=%%v
)

if "!CSPROJ_VERSION!"=="" (
    echo Warning: Cannot read version from DocuFiller.csproj
    goto :VersionCheckDone
)

echo.
echo ========================================
echo Version Consistency Check
echo ========================================
echo Git Tag Version: !VERSION!
echo CSPROJ Version:   !CSPROJ_VERSION!
echo ========================================

if "!VERSION!"=="!CSPROJ_VERSION!" (
    echo [OK] Versions match
) else (
    echo.
    echo Warning: Version mismatch detected!
    echo.
    echo The git tag version (!VERSION!) does not match the version
    echo in DocuFiller.csproj (!CSPROJ_VERSION!).
    echo.
    set /p CONTINUE="Continue anyway? (Y/N): "
    if /i not "!CONTINUE!"=="Y" (
        echo Release cancelled.
        exit /b 1
    )
    echo.
)

:VersionCheckDone
echo.
```

**Step 2: Test version validation**

1. Check current version in csproj
2. Create mismatched tag:
```bash
git tag v99.99.99
scripts\release.bat
```
Expected: Warns about version mismatch, prompts to continue

3. Test with matching version:
```bash
# Assume csproj has version 1.0.0
git tag v1.0.0
scripts\release.bat
```
Expected: Shows "[OK] Versions match"

**Step 3: Commit**

```bash
git add scripts/release.bat
git commit -m "feat: add version consistency check"
```

---

## Task 8: Create usage documentation

**Files:**
- Create: `docs/release-workflow.md`

**Step 1: Write comprehensive documentation**

Create `docs/release-workflow.md`:

```markdown
# DocuFiller Release Workflow

本文档描述如何使用自动化脚本发布 DocuFiller 到更新服务器。

## 前置准备

### 1. 配置发布环境

复制配置模板并填写实际值：

```cmd
copy scripts\config\release-config.bat.example scripts\config\release-config.bat
notepad scripts\config\release-config.bat
```

配置文件内容：

```bat
set UPDATE_SERVER_URL=http://172.18.200.47:58100
set UPDATE_TOKEN=你的实际令牌
set UPLOAD_ADMIN_PATH=C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\bin\upload-admin.exe
```

### 2. 确保工具可用

- [ ] update-server 在 172.18.200.47:58100 运行中
- [ ] upload-admin.exe 存在于指定路径
- [ ] Git 已正确配置

## 发布流程

### 方式一：使用 Git 标签（推荐）

#### 发布稳定版

```bash
# 1. 更新 DocuFiller.csproj 中的版本号
# 2. 提交代码
git add .
git commit -m "Release v1.0.0"

# 3. 创建标签
git tag v1.0.0

# 4. 推送到远程
git push origin main
git push origin v1.0.0

# 5. 执行发布
scripts\release.bat
```

#### 发布测试版

```bash
# 1. 提交代码
git add .
git commit -m "Beta release v1.0.1-beta"

# 2. 创建 beta 标签
git tag v1.0.1-beta

# 3. 推送标签
git push origin v1.0.1-beta

# 4. 执行发布（自动识别为 beta）
scripts\release.bat
```

### 方式二：命令行参数

不使用 Git 标签，直接指定渠道和版本：

```cmd
REM 发布稳定版
scripts\release.bat stable 1.0.0

REM 发布测试版
scripts\release.bat beta 1.0.1
```

## 标签命名规则

| 标签格式 | 渠道 | 解析后版本 |
|---------|------|-----------|
| `v1.0.0` | stable | 1.0.0 |
| `v1.0.1-beta` | beta | 1.0.1 |
| `v2.3.4` | stable | 2.3.4 |
| `v2.3.5-beta` | beta | 2.3.5 |

规则：
- 必须以 `v` 开头
- 稳定版：无后缀
- 测试版：以 `-beta` 结尾
- 版本号格式：`x.y.z`

## 输出示例

```
========================================
DocuFiller Release Script
========================================

Found tag: v1.0.0

========================================
Parsing tag: v1.0.0
========================================
Detected: STABLE release
Version: 1.0.0

========================================
Release Summary
========================================
Channel: stable
Version: 1.0.0
========================================

Checking version consistency with DocuFiller.csproj...

========================================
Version Consistency Check
========================================
Git Tag Version: 1.0.0
CSPROJ Version:   1.0.0
========================================
[OK] Versions match

========================================
Step 1: Building DocuFiller
========================================
[Build output...]

========================================
Step 2: Uploading to Update Server
========================================
Server: http://172.18.200.47:58100
Program: docufiller
Channel: stable
Version: 1.0.0
File: C:\...\build\docufiller-1.0.0.zip
========================================

[Upload output...]

========================================
Release completed successfully!
========================================

Summary:
  Program: docufiller
  Channel: stable
  Version: 1.0.0
  Server: http://172.18.200.47:58100

You can verify the release at:
  http://172.18.200.47:58100/api/version/latest?channel=stable
========================================
```

## 故障排查

### 问题：No git tag found

**原因**：未创建 Git 标签

**解决**：
```bash
git tag v1.0.0
git push origin v1.0.0
```

### 问题：Configuration file not found

**原因**：未创建 `release-config.bat`

**解决**：
```cmd
copy scripts\config\release-config.bat.example scripts\config\release-config.bat
# 然后编辑填入实际值
```

### 问题：Version mismatch detected

**原因**：Git 标签版本与 csproj 版本不一致

**解决**：更新 csproj 版本号或创建正确的标签

### 问题：UPLOAD FAILED

**原因**：网络问题、Token 错误、服务器不可用

**解决**：
1. 检查网络连接
2. 验证 Token 是否正确
3. 确认 update-server 正在运行
4. 构建文件会保留，可手动重试上传

### 问题：upload-admin.exe not found

**原因**：配置的路径不正确

**解决**：检查 `release-config.bat` 中的 `UPLOAD_ADMIN_PATH`

## 管理服务器上的版本

使用 upload-admin.exe 管理工具：

### 查看版本列表

```cmd
C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\bin\upload-admin.exe list --program-id docufiller --channel stable
```

### 删除版本

```cmd
C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\bin\upload-admin.exe delete --program-id docufiller --version 1.0.0 --channel stable
```

### 查看最新版本

```cmd
curl http://172.18.200.47:58100/api/version/latest?channel=stable
```

## 安全注意事项

1. **Token 保护**：`release-config.bat` 包含敏感 Token，已加入 `.gitignore`，不要提交到仓库
2. **Token 轮换**：定期更新服务器 Token
3. **访问控制**：确保 update-server 仅在内网可访问
4. **版本验证**：发布前务必验证版本号正确性

## 相关文件

- `scripts/release.bat` - 主发布脚本
- `scripts/build.bat` - 构建脚本
- `scripts/config/release-config.bat.example` - 配置模板
- `DocuFiller.csproj` - 项目版本号定义
```

**Step 2: Commit documentation**

```bash
git add docs/release-workflow.md
git commit -m "docs: add comprehensive release workflow documentation"
```

---

## Task 9: Add README update

**Files:**
- Modify: `README.md` (if exists) or create project README

**Step 1: Update README with release section**

Add to project README:

```markdown
## 发布 Release

DocuFiller 使用自动化脚本发布到更新服务器。详见 [发布流程文档](docs/release-workflow.md)。

快速发布：
```bash
# 稳定版
git tag v1.0.0 && git push origin v1.0.0 && scripts\release.bat

# 测试版
git tag v1.0.1-beta && git push origin v1.0.1-beta && scripts\release.bat
```
```

**Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add release section to README"
```

---

## Task 10: Cleanup test tags

**Step 1: Remove test tags created during development**

```bash
# List all tags
git tag

# Delete test tags locally
git tag -d v0.0.1-test v1.0.0-test-stable v1.0.0-test-beta v99.99.99

# Delete from remote if pushed
git push origin --delete refs/tags/v0.0.1-test
# (repeat for any pushed test tags)
```

**Step 2: Final verification**

```bash
# Verify script syntax
scripts\release.bat

# Verify config template exists
dir scripts\config\release-config.bat.example

# Verify docs exist
dir docs\release-workflow.md
```

---

## Testing Checklist

After implementation, test the following scenarios:

- [ ] **Configuration test**
  - [ ] Script fails gracefully without config file
  - [ ] Script loads config correctly
  - [ ] Script validates all required variables

- [ ] **Tag detection test**
  - [ ] Script fails without tags
  - [ ] Script detects stable tag correctly
  - [ ] Script detects beta tag correctly
  - [ ] Invalid tag format is rejected

- [ ] **Build integration test**
  - [ ] Build executes successfully
  - [ ] Build failure stops release
  - [ ] Output file is verified

- [ ] **Upload test**
  - [ ] Successful upload to server
  - [ ] Upload failure is handled
  - [ ] Retry information is displayed

- [ ] **Version validation test**
  - [ ] Matching versions proceed without prompt
  - [ ] Mismatched versions show warning
  - [ ] User can cancel on mismatch

- [ ] **Documentation test**
  - [ ] Commands in docs work as described
  - [ ] Examples are clear and accurate

---

## Success Criteria

1. ✅ 开发者可以通过单个 Git 标签完成发布
2. ✅ 脚本自动识别 stable/beta 渠道
3. ✅ 版本号与 csproj 一致性验证
4. ✅ 构建失败时停止发布流程
5. ✅ 上传失败时保留文件并显示重试命令
6. ✅ 完整的中文文档
7. ✅ 敏感信息（Token）不提交到仓库

---

## Related Skills

- @superpowers:systematic-debugging - If something doesn't work
- @superpowers:verification-before-completion - Before claiming done
- @elements-of-style:writing-clearly-and-concisely - For documentation

---

**Plan End**

Total estimated implementation time: 2-3 hours
Total tasks: 10
Files created: 3
Files modified: 3
