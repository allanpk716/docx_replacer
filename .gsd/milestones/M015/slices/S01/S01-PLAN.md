# S01: GithubSource 替换为 SimpleWebSource

**Goal:** 将 UpdateService 中 GitHub 更新模式从 GithubSource（GitHub API，60 次/小时 rate limit）替换为 SimpleWebSource（CDN 直连 https://github.com/allanpk716/docx_replacer/releases/latest/download/，无 rate limit）。内网 HTTP 更新逻辑完全不受影响。
**Demo:** GitHub 更新模式不再受 API rate limit 限制，匿名用户可无限次检查更新

## Must-Haves

- UpdateService 构造函数和 ReloadSource 方法中使用 SimpleWebSource 替代 GithubSource
- GitHub 模式的 EffectiveUpdateUrl 返回 CDN URL 而非空字符串
- UpdateSourceType 仍返回 "GitHub"（语义不变）
- dotnet build 无错误
- dotnet test 全部通过
- grep 确认代码中不再引用 GithubSource 类

## Proof Level

- This slice proves: contract — 通过单元测试验证源类型、URL、热重载行为，构建和全量测试确保无回归

## Integration Closure

无集成边界变化。IUpdateService 接口签名不变，Velopack UpdateManager 的使用方式不变，只是底层 IUpdateSource 从 GithubSource 换为 SimpleWebSource。所有下游消费者（MainWindowViewModel、UpdateSettingsViewModel、CLI 命令）通过 UpdateSourceType/EffectiveUpdateUrl 属性感知变化，无需修改。

## Verification

- 无变化。UpdateService 已有完整的结构化日志，记录源类型、通道、URL。EffectiveUpdateUrl 将从空字符串变为 CDN URL，日志输出更完整。

## Tasks

- [x] **T01: Replace GithubSource with SimpleWebSource CDN URL and update tests** `est:30m`
  将 UpdateService 中两处 GithubSource 替换为 SimpleWebSource，使用 GitHub CDN 直连 URL（/releases/latest/download/），消除 API rate limit。更新相关测试断言和接口文档注释。
  - Files: `Services/UpdateService.cs`, `Services/Interfaces/IUpdateService.cs`, `Tests/UpdateServiceTests.cs`
  - Verify: dotnet build && dotnet test && grep -r "GithubSource" --include="*.cs" . 应返回空

## Files Likely Touched

- Services/UpdateService.cs
- Services/Interfaces/IUpdateService.cs
- Tests/UpdateServiceTests.cs
