# S02: UpdateService 多源支持 + 便携版检测 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26T11:08:58.102Z

# S02: UpdateService 多源支持 + 便携版检测 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: S02 是纯服务层改动，没有 GUI 或运行时 UI 需要验证。所有行为通过构造函数参数和属性暴露，可通过单元测试完整验证。

## Preconditions

- .NET 8 SDK 已安装
- 项目可编译（dotnet restore 已完成）

## Smoke Test

运行 `dotnet test --filter "UpdateService"` — 全部 10 个测试通过即确认核心功能正常。

## Test Cases

### 1. UpdateUrl 为空时走 GitHub 源

1. 构造 UpdateService 实例，UpdateUrl 为空字符串
2. 读取 `service.UpdateSourceType`
3. **Expected:** 返回 `"GitHub"`

### 2. UpdateUrl 非空时走 HTTP 源

1. 构造 UpdateService 实例，UpdateUrl 为 `"http://server/updates"`
2. 读取 `service.UpdateSourceType`
3. **Expected:** 返回 `"HTTP"`

### 3. IsUpdateUrlConfigured 始终为 true

1. 构造 UpdateService 实例，UpdateUrl 为空
2. 读取 `service.IsUpdateUrlConfigured`
3. **Expected:** 返回 `true`（GitHub Releases 作为备选源始终可用）

### 4. 便携版 IsInstalled 检测

1. 在测试环境（非安装版）构造 UpdateService 实例
2. 读取 `service.IsInstalled`
3. **Expected:** 返回 `false`

### 5. 空配置默认 stable 通道

1. 构造 UpdateService 实例，UpdateUrl 为空，Channel 为空
2. 读取 `service.Channel`
3. **Expected:** 返回 `"stable"`

### 6. URL 有值时优先走 HTTP

1. 构造 UpdateService 实例，UpdateUrl 为 `"http://server/updates"`
2. 读取 `service.UpdateSourceType`
3. **Expected:** 返回 `"HTTP"`（即使 GitHub 也可用，有 URL 时优先 HTTP）

## Edge Cases

### UpdateUrl 为空且 Channel 有值

1. 构造 UpdateService，UpdateUrl 为空，Channel 为 "beta"
2. 读取 `service.UpdateSourceType` 和 `service.Channel`
3. **Expected:** UpdateSourceType 为 "GitHub"，Channel 为 "beta"（HTTP 源不可用时仍使用配置的通道）

## Failure Signals

- `dotnet test --filter "UpdateService"` 有任何测试失败
- `dotnet build -c Release` 有编译错误
- IsUpdateUrlConfigured 返回 false（应为始终 true）

## Not Proven By This UAT

- 真实网络环境下的 GithubSource 连接和版本检查（需要真实 GitHub API 访问）
- 真实安装版环境下的 IsInstalled = true 检测（测试环境均为非安装版）
- GUI 和 CLI 对新属性的实际消费（S03/S04 职责）

## Notes for Tester

- 这是纯服务层改动，没有 UI 变化
- Velopack UpdateManager 未实现 IDisposable（0.0.1298），代码中使用普通变量而非 using var
- IsInstalled 检测失败时默认返回 false 并记录警告日志，不会抛异常
