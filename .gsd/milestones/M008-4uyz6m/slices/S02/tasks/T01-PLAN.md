---
estimated_steps: 26
estimated_files: 3
skills_used: []
---

# T01: Add Channel config and modify UpdateService URL construction

为 UpdateService 添加通道（Channel）支持。在 appsettings.json 的 Update 节添加 Channel 字段，修改 UpdateService 从配置读取 Channel 并构造通道感知的更新 URL。Channel 为空时默认 "stable"，确保 UpdateManager 请求 `http://server:port/stable/releases.win.json`（默认）或 `http://server:port/beta/releases.win.json`（beta）。

## Steps

1. 在 appsettings.json 的 "Update" 节添加 "Channel": ""（空字符串，默认 stable）
2. 在 IUpdateService 接口添加 `string Channel { get; }` 只读属性
3. 修改 UpdateService 构造函数：
   a. 读取 configuration["Update:Channel"]，为空或 null 时默认 "stable"
   b. 将 _updateUrl 从纯 URL 改为 "{UpdateUrl}/{Channel}/" 格式
   c. 处理 UpdateUrl 末尾斜杠：确保不会出现双斜杠（用 TrimEnd('/') 后拼接）
   d. 存储解析的 _channel 值供 Channel 属性使用
4. 添加 Channel 属性实现（返回 _channel）
5. 确保当 UpdateUrl 为空时，IsUpdateUrlConfigured 仍返回 false（行为不变，URL 构造只在 IsUpdateUrlConfigured 为 true 时有意义）
6. 运行 dotnet build 确认 0 errors，dotnet test 确认全部通过

## Must-Haves

- [ ] appsettings.json Update 节包含 Channel 字段
- [ ] IUpdateService 接口包含 Channel 属性
- [ ] UpdateService 构造函数读取 Channel 配置
- [ ] Channel 为空/null 时默认 "stable"
- [ ] UpdateManager URL 为 {UpdateUrl}/{Channel}/ 格式
- [ ] UpdateUrl 为空时 IsUpdateUrlConfigured 返回 false（不变）
- [ ] dotnet build 0 errors，dotnet test 全部通过

## Key Implementation Notes

- Velopack UpdateManager(string urlOrPath) 会请求 {urlOrPath}/releases.win.json
- 所以传入 "http://server:port/beta/" → 请求 "http://server:port/beta/releases.win.json"
- 这正好匹配 Go 服务器的 GET /{channel}/releases.win.json 路由
- 不要使用 Velopack 的 ExplicitChannel（它会找 releases.{channel}.json，不匹配我们的服务器路由）
- URL 拼接：先 TrimEnd('/') 去掉 UpdateUrl 末尾斜杠，然后拼 "/" + channel + "/"

## Inputs

- ``appsettings.json` — existing Update:UpdateUrl config`
- ``Services/UpdateService.cs` — current URL construction logic`
- ``Services/Interfaces/IUpdateService.cs` — current interface`

## Expected Output

- ``appsettings.json` — updated with Channel field under Update`
- ``Services/UpdateService.cs` — channel-aware URL construction with default stable`
- ``Services/Interfaces/IUpdateService.cs` — updated with Channel property`

## Verification

dotnet build 0 errors, dotnet test all pass (162 tests, 0 failures)
