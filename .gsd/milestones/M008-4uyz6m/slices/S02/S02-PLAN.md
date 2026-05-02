# S02: 客户端通道支持

**Goal:** 修改 UpdateService 支持从 appsettings.json 读取 Channel 配置（stable/beta），构造 `{UpdateUrl}/{Channel}/` 的更新源 URL 传给 Velopack UpdateManager。Channel 为空时默认 stable，保持向后兼容。
**Demo:** 修改 appsettings.json Channel=beta，启动 DocuFiller，检查更新时请求 {UpdateUrl}/beta/releases.win.json

## Must-Haves

- ```
- appsettings.json 中 Update 节包含 Channel 字段
- Channel 为空时 UpdateService 默认使用 "stable"
- Channel="beta" 时 UpdateManager URL 为 {UpdateUrl}/beta/
- Channel="stable" 时 UpdateManager URL 为 {UpdateUrl}/stable/
- UpdateUrl 为空时 IsUpdateUrlConfigured 返回 false（行为不变）
- dotnet build 0 errors
- dotnet test 全部通过（现有 162 + 新增通道测试）
- ```

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream consumed: S01 的静态文件服务 /{channel}/releases.win.json 和 /{channel}/*.nupkg\n- New wiring: appsettings.json Channel → UpdateService URL → Velopack UpdateManager\n- Remaining before e2e: S03 发布脚本改造、S04 端到端验证

## Verification

- Not provided.

## Tasks

- [x] **T01: Add Channel config and modify UpdateService URL construction** `est:30m`
  为 UpdateService 添加通道（Channel）支持。在 appsettings.json 的 Update 节添加 Channel 字段，修改 UpdateService 从配置读取 Channel 并构造通道感知的更新 URL。Channel 为空时默认 "stable"，确保 UpdateManager 请求 `http://server:port/stable/releases.win.json`（默认）或 `http://server:port/beta/releases.win.json`（beta）。
  - Files: `appsettings.json`, `Services/UpdateService.cs`, `Services/Interfaces/IUpdateService.cs`
  - Verify: dotnet build 0 errors, dotnet test all pass

- [x] **T02: Add unit tests for channel URL construction** `est:30m`
  为 UpdateService 的通道 URL 构造逻辑添加单元测试。验证各种配置组合下 URL 的正确构造：默认通道、显式 beta、Channel 键缺失、UpdateUrl 为空、末尾斜杠处理。将 UpdateService.cs 添加到测试项目编译引用。
  - Files: `Tests/UpdateServiceTests.cs`, `Tests/DocuFiller.Tests.csproj`
  - Verify: dotnet test --filter UpdateServiceTests passes

## Files Likely Touched

- appsettings.json
- Services/UpdateService.cs
- Services/Interfaces/IUpdateService.cs
- Tests/UpdateServiceTests.cs
- Tests/DocuFiller.Tests.csproj
