---
id: T01
parent: S01
milestone: M014-jkpyu6
key_files:
  - Services/UpdateService.cs
key_decisions:
  - 去掉 ExplicitChannel 让 Velopack 使用 OS 默认 channel (win) 查找 releases.win.json，而非硬编码的 stable
  - HTTP 模式回退使用 _baseUrl + targetChannel 重建 SimpleWebSource，GitHub 模式跳过回退
duration: 
verification_result: passed
completed_at: 2026-05-02T15:53:58.072Z
blocker_discovered: false
---

# T01: 去掉 UpdateService 的 ExplicitChannel 和 AllowVersionDowngrade，修正 HTTP 模式回退逻辑，让 Velopack 使用 OS 默认 channel 查找 releases.win.json

**去掉 UpdateService 的 ExplicitChannel 和 AllowVersionDowngrade，修正 HTTP 模式回退逻辑，让 Velopack 使用 OS 默认 channel 查找 releases.win.json**

## What Happened

修改 UpdateService.cs 解决 feed 文件名不匹配问题。核心变更：

1. **新增 _baseUrl 字段**：在构造函数和 ReloadSource 中存储不含通道后缀的基础 URL（如 http://server/），供回退逻辑重建 SimpleWebSource 使用。

2. **去掉 ExplicitChannel 和 AllowVersionDowngrade**：CreateUpdateManager() 和 CreateUpdateManagerForChannel() 不再设置这两个 UpdateOptions 属性。之前 ExplicitChannel="stable" 导致 Velopack 查找 releases.stable.json，而服务器实际文件名是 releases.win.json。去掉后 Velopack 使用 OS 默认 channel（win），正确匹配 releases.win.json。

3. **修正 CheckForUpdatesAsync 回退逻辑**：原逻辑对所有源类型都尝试回退到 stable 通道。修改后：
   - HTTP 模式：用 _baseUrl + "stable/" 创建新 SimpleWebSource，再创建 UpdateManager 检查
   - GitHub 模式：跳过回退（GitHub Releases 没有通道目录分离，无需回退）

4. **ReloadSource 同步维护 _baseUrl**：热重载时同步更新 _baseUrl，确保回退逻辑在配置变更后仍能正确重建 URL。

CreateUpdateManagerForChannel 方法保留但内部不再使用 ExplicitChannel 参数，作为回退逻辑的内联替代方案留作备用。

## Verification

1. dotnet build 成功（0 错误，95 个预存在警告）
2. grep -n ExplicitChannel Services/UpdateService.cs 只在注释中出现，无代码引用
3. grep -n AllowVersionDowngrade Services/UpdateService.cs 无结果

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --nologo -v q` | 0 | ✅ pass | 8170ms |
| 2 | `grep -n ExplicitChannel Services/UpdateService.cs` | 0 | ✅ pass (comments only) | 200ms |
| 3 | `grep -n AllowVersionDowngrade Services/UpdateService.cs` | 1 | ✅ pass (no results) | 200ms |

## Deviations

CreateUpdateManagerForChannel 方法保留了空壳（接收 channel 参数但不使用），因为原计划没有提到要删除这个方法签名。不影响功能，后续可清理。

## Known Issues

None.

## Files Created/Modified

- `Services/UpdateService.cs`
