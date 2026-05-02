# S01: GithubSource 替换为 SimpleWebSource — UAT

**Milestone:** M015
**Written:** 2026-05-02T16:50:24.865Z

# UAT: S01 — GithubSource 替换为 SimpleWebSource

## 前置条件
- 已编译 DocuFiller（dotnet build 成功）
- GitHub 更新模式已配置（appsettings.json Update:Source 为 "GitHub"）

## 测试用例

### TC1: GitHub 更新模式使用 CDN 直连
1. 启动应用，切换更新源为 "GitHub"
2. 检查日志中 `_updateSource` 类型应为 `SimpleWebSource`
3. 调用 `IUpdateService.EffectiveUpdateUrl` 应返回 `https://github.com/allanpk716/docx_replacer/releases/latest/download/` 而非空字符串
4. **预期**: UpdateSourceType 仍为 "GitHub"，EffectiveUpdateUrl 返回 CDN URL

### TC2: 内网 HTTP 模式不受影响
1. 配置 appsettings.json Update:Source 为 "Internal"，Update:UpdateUrl 为内网地址
2. 启动应用，检查更新
3. **预期**: 内网模式行为与变更前完全一致，使用 SimpleWebSource 访问内网 URL

### TC3: 热重载后源类型正确
1. 启动应用（默认 GitHub 模式）
2. 通过设置界面切换到内网模式，保存
3. 检查状态栏源类型显示
4. 切换回 GitHub 模式，保存
5. **预期**: 两次切换后源类型和 URL 均正确更新

### TC4: 代码无 GithubSource 残留
1. 在项目目录执行 `findstr /s /r "GithubSource" *.cs`
2. **预期**: 无匹配结果

### TC5: 全量测试通过
1. 执行 `dotnet test`
2. **预期**: 所有测试通过，无失败
