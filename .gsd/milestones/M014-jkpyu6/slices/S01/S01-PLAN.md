# S01: 去掉 ExplicitChannel，修复更新检测逻辑

**Goal:** 去掉 ExplicitChannel 使 Velopack 查找正确的 feed 文件名（releases.win.json），修正 HTTP 模式回退逻辑，确保 GitHub 和内网双源更新检测正常工作
**Demo:** 安装版 v1.3.4 启动后通过 GitHub 检测到 v1.4.0，状态栏显示有新版本可用

## Must-Haves

- ExplicitChannel 不再传入 CreateUpdateManager / CreateUpdateManagerForChannel
- 新增 _baseUrl 字段存储不含通道的基础 URL
- HTTP 模式回退时创建新 SimpleWebSource（URL 指向目标通道路径）
- GitHub 模式跳过通道回退（无意义）
- dotnet build 无错误
- dotnet test 全部通过（含现有 UpdateServiceTests）
- 代码中不再有 ExplicitChannel 或 AllowVersionDowngrade 引用

## Proof Level

- This slice proves: command + log verification

## Integration Closure

UpdateService 对外接口（IUpdateService）不变，内部 UpdateManager 创建逻辑修正。依赖 UpdateService 的 MainWindowViewModel 和 UpdateSettingsViewModel 无需改动。

## Verification

- velopack.log 将显示正确的 releases.win.json 查找（而非 releases.stable.json），feed 合并后包含所有历史版本。应用日志中的更新检查结果与实际一致。

## Tasks

- [x] **T01: 修改 UpdateService 核心：去掉 ExplicitChannel，修正回退逻辑** `est:30min`
  修改 UpdateService.cs 的三个核心方法，解决 feed 文件名不匹配问题。步骤：
1. 新增 _baseUrl 字段，在构造函数中存储不含通道的基础 URL（如 http://server/）
2. CreateUpdateManager() 和 CreateUpdateManagerForChannel() 去掉 ExplicitChannel 和 AllowVersionDowngrade
3. CheckForUpdatesAsync() 的回退逻辑：HTTP 模式用 _baseUrl + targetChannel 创建新 SimpleWebSource；GitHub 模式跳过回退
4. ReloadSource() 中同步维护 _baseUrl
5. 确保 GitHub 模式和 HTTP 模式都使用 OS 默认 channel（win）查找 releases.win.json
  - Files: `Services/UpdateService.cs`
  - Verify: dotnet build 无错误，grep -n ExplicitChannel Services/UpdateService.cs 无结果

- [x] **T02: 验证编译和测试，确认无残留 ExplicitChannel 引用** `est:15min`
  编译通过后运行全量测试，验证 UpdateServiceTests 和其他测试全部通过。步骤：
1. dotnet build 确认无编译错误
2. dotnet test 运行全量测试
3. 如果有测试因 ExplicitChannel 移除而失败，更新测试期望值
4. grep 确认代码中无残留的 ExplicitChannel / AllowVersionDowngrade 引用
  - Files: `Services/UpdateService.cs`, `Tests/UpdateServiceTests.cs`
  - Verify: dotnet test 全部通过，无编译错误

## Files Likely Touched

- Services/UpdateService.cs
- Tests/UpdateServiceTests.cs
