# 版本管理文档 (Version Management)

## 概述 (Overview)

DocuFiller 使用 **Git Tag** 作为版本号的单一数据源 (Single Source of Truth)。所有版本相关的文件都会在构建时自动同步，确保整个项目中的版本号保持一致。

## 版本数据流 (Version Data Flow)

```
Git Tag (v1.0.0)
    ↓
sync-version.bat (构建时自动运行)
    ↓
├─→ DocuFiller.csproj (<Version>1.0.0</Version>)
└─→ External/update-config.yaml (current_version: "1.0.0")
```

## 版本命名规范 (Version Naming Convention)

### 正式版本 (Stable Releases)
```
v1.0.0  →  版本号: 1.0.0, 通道: stable
v1.2.3  →  版本号: 1.2.3, 通道: stable
```

### Beta 版本 (Beta Releases)
```
v1.0.0-beta01  →  版本号: 1.0.0-beta01, 通道: beta
v1.2.0-beta    →  版本号: 1.2.0-beta, 通道: beta
```

### 开发版本 (Development Builds)
```
无 tag  →  版本号: 1.0.0-dev.{commit-hash}, 通道: dev
```

## 工作流程 (Workflows)

### 1. 日常开发 (Daily Development)

```bash
# 1. 编写代码
git add .
git commit -m "feat: add new feature"

# 2. 构建项目（版本自动同步为 1.0.0-dev.{hash}）
dotnet build

# 3. 运行测试
dotnet test
```

**版本行为**: 无 tag 时，自动使用 `1.0.0-dev.{commit-hash}` 格式

### 2. 创建发布版本 (Creating a Release)

```bash
# 1. 创建并推送 tag
git tag v1.0.0
git push origin v1.0.0

# 2. 运行发布脚本（自动构建 + 上传）
cd scripts
release.bat

# 或者手动指定版本和通道
release.bat stable 1.0.0
```

**版本行为**: 有 tag 时，使用 tag 版本号（去掉 'v' 前缀）

### 3. 创建 Beta 版本 (Creating a Beta Release)

```bash
# 1. 创建并推送 beta tag
git tag v1.0.0-beta01
git push origin v1.0.0-beta01

# 2. 运行发布脚本
cd scripts
release.bat

# 脚本会自动检测到 -beta 后缀，使用 beta 通道
```

**版本行为**: tag 包含 `-beta` 时，使用 beta 通道

## 构建系统 (Build System)

### 版本同步 (Version Synchronization)

**重要**: 版本同步需要在发布前手动运行，不建议在每次构建时自动同步（避免文件锁定问题）。

运行版本同步脚本：

```bash
# 从项目根目录运行
scripts\sync-version.bat
```

脚本会执行以下操作：

1. **读取 Git Tag**:
   - 有 tag: 使用 tag 版本（如 `v1.0.0` → `1.0.0`）
   - 无 tag: 使用 commit hash（如 `1.0.0-dev.abc1234`）

2. **更新 DocuFiller.csproj**:
   ```xml
   <Version>1.0.0</Version>
   ```

3. **更新 External/update-config.yaml**:
   ```yaml
   program:
     current_version: "1.0.0"
   ```

**注意**: 首次使用前需要从模板创建配置文件：
```bash
cd External
copy update-config.yaml.template update-config.yaml
# 编辑配置文件，设置服务器地址
notepad update-config.yaml
```

## 发布流程 (Release Process)

### 完整发布步骤 (Complete Release Steps)

```bash
# 1. 确保代码已提交
git status
git add .
git commit -m "feat: prepare for v1.0.0 release"

# 2. 创建 tag
git tag v1.0.0
git push origin v1.0.0

# 3. 同步版本号（重要！）
scripts\sync-version.bat

# 4. 验证版本号
grep "<Version>" DocuFiller.csproj
type External\update-config.yaml | findstr current_version

# 5. 运行发布脚本
cd scripts
release.bat

# 脚本会执行：
# - 检测 tag: v1.0.0
# - 解析版本: 1.0.0
# - 解析通道: stable
# - 构建项目: scripts/build.bat
# - 上传到服务器: 使用 update-publisher.exe
```

### 发布脚本参数 (Release Script Parameters)

```bash
# 自动模式（从 git tag 检测）
release.bat

# 手动模式（指定版本和通道）
release.bat stable 1.0.0
release.bat beta 1.0.0-beta01
```

## 运行时版本读取 (Runtime Version Reading)

### C# 代码获取版本 (Getting Version in C#)

```csharp
using DocuFiller.Utils;

// 获取当前版本（去掉构建号）
string version = VersionHelper.GetCurrentVersion(); // "1.0.0"

// 获取完整版本（包含构建号）
string fullVersion = VersionHelper.GetFullVersion(); // "1.0.0.0"

// 检查是否为开发版本
bool isDev = VersionHelper.IsDevelopmentVersion(); // true/false

// 获取更新通道
string channel = VersionHelper.GetChannel(); // "stable", "beta", or "dev"
```

### 版本号解析规则 (Version Parsing Rules)

- **稳定版**: `1.0.0` → channel: `stable`
- **Beta 版**: `1.0.0-beta01` → channel: `beta`
- **开发版**: `1.0.0-dev.abc1234` → channel: `dev`

## 配置文件 (Configuration Files)

### 1. DocuFiller.csproj

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
  </PropertyGroup>
</Project>
```

**说明**: `<Version>` 字段会在每次构建时由 `sync-version.bat` 自动更新

### 2. External/update-config.yaml

```yaml
app:
  id: "docufiller"
  name: "DocuFiller"
  current_version: "1.0.0"  # 自动同步

server:
  url: "http://localhost:8080"

channels:
  - name: "stable"
    description: "Stable releases"
  - name: "beta"
    description: "Beta releases"
  - name: "dev"
    description: "Development builds"
```

**说明**: `current_version` 字段会在每次构建时由 `sync-version.bat` 自动更新

### 3. scripts/config/release-config.bat

```bat
set UPDATE_SERVER_URL=http://localhost:8080
set UPDATE_TOKEN=your-secret-token
set UPDATE_PUBLISHER_PATH=%~dp0..\..\External\update-publisher.exe
set RELEASE_NOTES=Bug fixes and improvements
```

**说明**: 此文件包含敏感信息，已被 `.gitignore` 排除

## 故障排除 (Troubleshooting)

### 1. 版本号不同步 (Version Not Synchronized)

**问题**: 修改代码后版本号没有更新

**解决**:
```bash
# 手动运行版本同步脚本
scripts\sync-version.bat

# 检查 DocuFiller.csproj 中的 <Version> 元素
# 检查 External/update-config.yaml 中的 current_version
```

### 2. Git Tag 检测失败 (Git Tag Not Detected)

**问题**: 构建时版本显示为 `1.0.0-dev.{hash}`，但期望显示 tag 版本

**解决**:
```bash
# 检查是否有 tag
git tag

# 确保标签已推送
git push origin v1.0.0

# 验证标签格式（必须以 'v' 开头）
git describe --tags --abbrev=0
```

### 3. 发布失败 (Release Failed)

**问题**: `release.bat` 执行失败

**检查清单**:
- [ ] Git tag 是否已创建并推送
- [ ] `scripts/config/release-config.bat` 是否已配置
- [ ] `External/update-publisher.exe` 是否存在
- [ ] 服务器地址和 token 是否正确
- [ ] 网络连接是否正常

### 4. External 文件缺失 (External Files Missing)

**问题**: 构建失败，提示缺少 `update-client.exe` 或 `update-config.yaml`

**解决**:
```bash
# 检查 External 目录
dir External

# 如果缺少 update-config.yaml：
cd External
copy update-config.yaml.template update-config.yaml
notepad update-config.yaml

# 然后运行版本同步
cd ..
scripts\sync-version.bat

# 如果缺少 .exe 文件：
# 1. 从 Update Server 管理后台下载
# 2. 或从 update-server 仓库构建
# 3. 放置在 External/ 目录
```

**检查清单**:
- [ ] `External/update-client.exe` 存在
- [ ] `External/update-publisher.exe` 存在
- [ ] `External/update-config.yaml` 存在（从模板创建）
- [ ] `External/update-config.yaml` 已配置服务器地址
- [ ] 已运行 `scripts\sync-version.bat` 同步版本号

## 最佳实践 (Best Practices)

### 1. 版本号管理 (Version Number Management)

- ✅ 使用语义化版本 (Semantic Versioning): `MAJOR.MINOR.PATCH`
- ✅ 正式版本使用 tag: `v1.0.0`
- ✅ Beta 版本使用 tag: `v1.0.0-beta01`
- ✅ 不要手动修改 `DocuFiller.csproj` 中的 `<Version>`
- ✅ 不要手动修改 `External/update-config.yaml` 中的 `current_version`

### 2. 发布流程 (Release Process)

- ✅ 发布前确保所有测试通过
- ✅ 发布前更新 CHANGELOG.md
- ✅ 使用 tag 标记发布版本
- ✅ 推送 tag 到远程仓库
- ✅ **运行 `scripts\sync-version.bat` 同步版本号**
- ✅ 验证版本号已正确更新
- ✅ 运行 `release.bat` 自动构建和上传

### 3. 开发流程 (Development Process)

- ✅ 日常开发不需要创建 tag
- ✅ 构建时自动使用 `1.0.0-dev.{hash}` 格式
- ✅ 提交代码前运行测试
- ✅ 保持工作目录干净

## 相关文档 (Related Documentation)

- [EXTERNAL_SETUP.md](EXTERNAL_SETUP.md) - External 目录说明
- [update-publisher.usage.txt](../External/update-publisher.usage.txt) - 发布工具使用说明
- [Git Tag 文档](https://git-scm.com/docs/git-tag) - Git 官方文档

## 常见问题 (FAQ)

### Q: 为什么使用 Git Tag 作为版本号来源？

**A**: Git Tag 是版本控制的标准实践，具有以下优势：
- 不可变历史记录
- 易于回溯和验证
- 与 CI/CD 流程集成良好
- 避免手动同步错误

### Q: 如何在本地测试发布流程？

**A**:
```bash
# 1. 创建本地测试 tag
git tag v1.0.0-test

# 2. 运行构建脚本
scripts\build.bat

# 3. 检查输出文件
dir scripts\build

# 4. 删除测试 tag
git tag -d v1.0.0-test
```

### Q: 发布后如何回滚？

**A**:
```bash
# 1. 删除远程 tag
git push origin :refs/tags/v1.0.0

# 2. 删除本地 tag
git tag -d v1.0.0

# 3. 在服务器上删除版本（使用 update-publisher）
External\update-publisher.exe delete ^
  --token YOUR_TOKEN ^
  --program-id docufiller ^
  --channel stable ^
  --version 1.0.0
```

### Q: 如何查看当前运行版本？

**A**:
- 应用程序窗口标题栏显示版本号
- 帮助菜单中显示版本信息
- 日志文件中包含版本信息

---

**文档版本**: 1.0.0
**最后更新**: 2025-01-21
