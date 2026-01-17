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
