---
estimated_steps: 5
estimated_files: 3
skills_used: []
---

# T01: Add ReloadSource to IUpdateService + implement in-memory hot-reload + unit tests

**Slice:** S01 — UpdateService 热重载 + appsettings.json 写回
**Milestone:** M010-hpylzg

## Description

在 IUpdateService 接口新增 `ReloadSource(string updateUrl, string channel)` 方法，将 `EffectiveUpdateUrl` 从 internal 提升为接口成员。在 UpdateService 中移除字段 readonly 修饰符，实现 ReloadSource 内存热重载逻辑。编写单元测试验证行为。

## Negative Tests

- **Malformed inputs**: null updateUrl (应视为空，走 GitHub), null channel (应默认 "stable")
- **Boundary conditions**: URL 带尾部斜杠 vs 不带，channel 前后有空格

## Steps

1. **修改 IUpdateService 接口** (`Services/Interfaces/IUpdateService.cs`)
   - 添加 `void ReloadSource(string updateUrl, string channel)` 方法，XML 注释说明：热重载更新源，updateUrl 为空时走 GitHub Releases，非空时走 HTTP。同时持久化到 appsettings.json。
   - 添加 `string EffectiveUpdateUrl { get; }` 属性，XML 注释：当前生效的完整更新源 URL（含通道路径），GitHub 模式返回空字符串。

2. **修改 UpdateService 字段** (`Services/UpdateService.cs`)
   - 移除 `_updateSource`、`_updateUrl`、`_channel`、`_sourceType` 的 `readonly` 修饰符（`_isInstalled` 保持 readonly — 安装状态不变）
   - 将 `EffectiveUpdateUrl` 属性从 `internal` 改为 `public`，实现接口成员

3. **实现 ReloadSource 方法** (`Services/UpdateService.cs`)
   - 方法签名: `public void ReloadSource(string updateUrl, string channel)`
   - 处理 null 参数：`updateUrl ??= ""`，`channel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim()`
   - 记录日志：旧值 → 新值（源类型、URL、通道）
   - 核心逻辑（与构造函数相同的分支）：
     - 如果 updateUrl 非空：`_updateUrl = updateUrl.TrimEnd('/') + "/" + _channel + "/"`，`_updateSource = new SimpleWebSource(_updateUrl)`，`_sourceType = "HTTP"`
     - 如果 updateUrl 为空：`_updateUrl = ""`，`_updateSource = new GithubSource("https://github.com/allanpk716/docx_replacer", accessToken: null, prerelease: false)`，`_sourceType = "GitHub"`
   - 更新 `_channel` 为处理后的值
   - 日志记录完成：Information 级别

4. **添加 usings** (`Services/UpdateService.cs`)
   - 无需新增 using — 已有 `Velopack` 和 `Velopack.Sources`

5. **编写单元测试** (`Tests/UpdateServiceTests.cs`)
   - `ReloadSource_http_changes_source_type_to_HTTP`：构造 GitHub 模式服务 → 调用 ReloadSource("http://server", "stable") → Assert UpdateSourceType == "HTTP", EffectiveUpdateUrl == "http://server/stable/"
   - `ReloadSource_empty_changes_source_type_to_GitHub`：构造 HTTP 模式服务 → 调用 ReloadSource("", "stable") → Assert UpdateSourceType == "GitHub", EffectiveUpdateUrl == ""
   - `ReloadSource_updates_channel`：构造 stable 服务 → ReloadSource("http://server", "beta") → Assert Channel == "beta", EffectiveUpdateUrl.Contains("/beta/")
   - `ReloadSource_null_url_treated_as_empty`：ReloadSource(null, "stable") → Assert UpdateSourceType == "GitHub"
   - `ReloadSource_null_channel_defaults_to_stable`：ReloadSource("http://server", null) → Assert Channel == "stable"
   - `ReloadSource_with_trailing_slash`：ReloadSource("http://server/", "stable") → Assert EffectiveUpdateUrl 不含双斜杠

## Must-Haves

- [ ] IUpdateService 接口包含 ReloadSource 方法和 EffectiveUpdateUrl 属性
- [ ] ReloadSource 正确切换 HTTP/GitHub 源并更新所有字段
- [ ] EffectiveUpdateUrl 从 internal 提升为 public 接口成员
- [ ] 至少 5 个新单元测试覆盖 ReloadSource 内存行为
- [ ] 现有测试全部通过（不破坏 EffectiveUpdateUrl 的使用方式）

## Verification

- `dotnet test --filter "UpdateServiceTests" --verbosity normal` — 所有测试通过
- 构建无错误：`dotnet build`

## Inputs

- `Services/Interfaces/IUpdateService.cs` — 当前接口定义，需要新增方法
- `Services/UpdateService.cs` — 当前实现，字段为 readonly，EffectiveUpdateUrl 为 internal
- `Tests/UpdateServiceTests.cs` — 现有测试，需要扩展

## Expected Output

- `Services/Interfaces/IUpdateService.cs` — 新增 ReloadSource + EffectiveUpdateUrl
- `Services/UpdateService.cs` — 实现 ReloadSource，字段可变，EffectiveUpdateUrl public
- `Tests/UpdateServiceTests.cs` — 新增 ReloadSource 内存行为测试
