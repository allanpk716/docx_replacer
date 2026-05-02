# S01: UpdateService 热重载 + appsettings.json 写回

**Goal:** 在 IUpdateService 接口新增 ReloadSource(string updateUrl, string channel) 方法，运行时重建 IUpdateSource。UpdateService 实现该方法：内存中重建 SimpleWebSource/GithubSource，同时将新值持久化到 appsettings.json 的 Update 节。将 EffectiveUpdateUrl 提升为接口成员。
**Demo:** 调用 ReloadSource("http://192.168.1.100:8080", "beta") 后 UpdateSourceType 变为 "HTTP"，EffectiveUpdateUrl 变为 "http://192.168.1.100:8080/beta/"，appsettings.json 中 Update 节已更新

## Must-Haves

- ReloadSource("http://192.168.1.100:8080", "beta") 后 UpdateSourceType 为 "HTTP"，EffectiveUpdateUrl 为 "http://192.168.1.100:8080/beta/"
- ReloadSource("", "stable") 后 UpdateSourceType 为 "GitHub"，EffectiveUpdateUrl 为 ""
- appsettings.json 中 Update:UpdateUrl 和 Update:Channel 节点已更新为新值
- 现有 UpdateServiceTests 全部通过，无回归
- ReloadSource 处理文件写入失败时不影响内存热重载

## Proof Level

- This slice proves: contract — ReloadSource 方法行为通过单元测试验证，边界契约（接口方法签名 + 返回值）通过接口定义保证

## Integration Closure

- New wiring: ReloadSource 修改 UpdateService 内部字段，后续 CreateUpdateManager 自动使用新源
- Persistence: System.Text.Json.Nodes 读写 appsettings.json，IConfiguration reloadOnChange 自动生效
- What remains: S02 消费 ReloadSource + UpdateSourceType + EffectiveUpdateUrl 构建 GUI

## Verification

- ReloadSource 记录结构化日志（Information 级别）：旧值 → 新值的源类型/URL/通道变更
- 文件写入失败记录 Warning 日志但不抛异常（内存热重载仍成功）
- EffectiveUpdateUrl 可在任意时刻检查当前生效的完整更新 URL

## Tasks

- [x] **T01: Add ReloadSource to IUpdateService + implement in-memory hot-reload + unit tests** `est:45m`
  在 IUpdateService 接口新增 ReloadSource(string updateUrl, string channel) 方法，将 EffectiveUpdateUrl 从 internal 提升为接口成员。在 UpdateService 中移除字段 readonly 修饰符，实现 ReloadSource：根据 updateUrl 是否为空重建 SimpleWebSource 或 GithubSource，更新 _updateSource/_updateUrl/_channel/_sourceType 四个字段。编写单元测试验证 ReloadSource 内存行为。
  - Files: `Services/Interfaces/IUpdateService.cs`, `Services/UpdateService.cs`, `Tests/UpdateServiceTests.cs`
  - Verify: dotnet test --filter "UpdateServiceTests" --verbosity normal

- [x] **T02: Add appsettings.json write-back to ReloadSource + persistence tests** `est:30m`
  在 UpdateService 中添加 PersistToAppSettings 私有方法（使用 System.Text.Json.Nodes 读写 appsettings.json），从 ReloadSource 调用。添加 internal AppSettingsPath 属性支持测试替换路径。文件写入失败时记录 Warning 日志但不抛异常。编写集成测试使用临时文件验证持久化行为。
  - Files: `Services/UpdateService.cs`, `Tests/UpdateServiceTests.cs`
  - Verify: dotnet test --filter "UpdateServiceTests" --verbosity normal

## Files Likely Touched

- Services/Interfaces/IUpdateService.cs
- Services/UpdateService.cs
- Tests/UpdateServiceTests.cs
