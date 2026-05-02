---
estimated_steps: 6
estimated_files: 1
skills_used: []
---

# T01: 修改 UpdateService 核心：去掉 ExplicitChannel，修正回退逻辑

修改 UpdateService.cs 的三个核心方法，解决 feed 文件名不匹配问题。步骤：
1. 新增 _baseUrl 字段，在构造函数中存储不含通道的基础 URL（如 http://server/）
2. CreateUpdateManager() 和 CreateUpdateManagerForChannel() 去掉 ExplicitChannel 和 AllowVersionDowngrade
3. CheckForUpdatesAsync() 的回退逻辑：HTTP 模式用 _baseUrl + targetChannel 创建新 SimpleWebSource；GitHub 模式跳过回退
4. ReloadSource() 中同步维护 _baseUrl
5. 确保 GitHub 模式和 HTTP 模式都使用 OS 默认 channel（win）查找 releases.win.json

## Inputs

- `Services/UpdateService.cs`

## Expected Output

- `Services/UpdateService.cs`

## Verification

dotnet build 无错误，grep -n ExplicitChannel Services/UpdateService.cs 无结果
