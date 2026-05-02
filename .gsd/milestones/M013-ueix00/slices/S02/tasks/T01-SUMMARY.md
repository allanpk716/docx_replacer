---
id: T01
parent: S02
milestone: M013-ueix00
key_files:
  - Tests/UpdateServiceTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-02T12:27:29.877Z
blocker_discovered: false
---

# T01: 新增 5 个 UpdateService 持久化配置边界测试，覆盖 malformed JSON、缺失字段、空文件、文件不覆盖场景

**新增 5 个 UpdateService 持久化配置边界测试，覆盖 malformed JSON、缺失字段、空文件、文件不覆盖场景**

## What Happened

在 Tests/UpdateServiceTests.cs 中添加了 5 个边界测试，覆盖 UpdateService 持久化配置路径逻辑的异常和边界场景：

1. **ReadPersistentConfig_malformed_json_falls_back_to_appsettings** — 配置文件包含非法 JSON（"{invalid"）时，ReadPersistentConfig 捕获异常返回 null fallback，不崩溃，正确使用 appsettings.json 的值。

2. **ReadPersistentConfig_missing_UpdateUrl_field_falls_back_to_appsettings** — JSON 中只有 Channel 没有 UpdateUrl 时，url 为 null，构造函数 fallback 到 IConfiguration 中的 UpdateUrl。

3. **ReadPersistentConfig_missing_Channel_field_defaults_to_stable** — JSON 中只有 UpdateUrl 没有 Channel 时，channel 为 null，IConfiguration 也无 Channel 配置，最终默认 "stable"。修正了初始测试中 IConfiguration 传入 Channel="beta" 导致预期错误的偏差。

4. **ReadPersistentConfig_empty_file_does_not_crash** — 0 字节空文件导致 JsonNode.Parse 抛出异常，被 catch 捕获后 fallback 到 IConfiguration，不崩溃。

5. **EnsurePersistentConfigSync_does_not_overwrite_existing_file** — 预写持久化配置文件后，构造函数中的 EnsurePersistentConfigSync 检测到文件已存在（File.Exists 返回 true）直接 return，不覆盖已有内容。

所有测试使用临时目录注入路径，不污染真实用户目录。29 个 UpdateServiceTests 全部通过（24 个原有 + 5 个新增）。

## Verification

运行 dotnet test --filter UpdateServiceTests 验证所有 29 个测试通过（24 个原有 + 5 个新增边界测试），耗时 164ms，0 失败。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter UpdateServiceTests --nologo -v q` | 0 | ✅ pass | 3000ms |

## Deviations

修正了 "missing Channel" 测试中的 IConfiguration 设置：原计划未明确说明 IConfiguration 中 Channel 的值，实际实现中发现当持久化配置 Channel 缺失时，fallback 到 IConfiguration 的 Channel 值而非直接默认 "stable"。因此将 IConfiguration 中的 Channel 从 "beta" 改为不设置，确保测试真正验证"全链路 fallback 到 stable"的场景。

## Known Issues

None.

## Files Created/Modified

- `Tests/UpdateServiceTests.cs`
