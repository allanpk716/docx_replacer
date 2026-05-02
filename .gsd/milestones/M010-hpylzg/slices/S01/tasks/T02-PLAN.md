---
estimated_steps: 5
estimated_files: 2
skills_used: []
---

# T02: Add appsettings.json write-back to ReloadSource + persistence tests

**Slice:** S01 — UpdateService 热重载 + appsettings.json 写回
**Milestone:** M010-hpylzg

## Description

在 UpdateService 中添加 appsettings.json 文件持久化逻辑。ReloadSource 更新内存字段后，使用 System.Text.Json.Nodes 将 UpdateUrl 和 Channel 值写回 appsettings.json。添加 internal AppSettingsPath 属性支持测试替换路径。文件写入失败时记录 Warning 日志但不抛异常（内存热重载仍成功）。

## Negative Tests

- **Error paths**: appsettings.json 不存在时写入失败 → 日志 Warning，不抛异常
- **Error paths**: appsettings.json 格式无效 → 日志 Warning，不抛异常
- **Boundary conditions**: 写入空字符串 UpdateUrl → JSON 中值为 ""
- **Malformed inputs**: 并发写入（WPF 单线程 UI，风险低但需 graceful handling）

## Steps

1. **添加 AppSettingsPath 属性** (`Services/UpdateService.cs`)
   - `internal string AppSettingsPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");`
   - 用于测试时替换为临时文件路径

2. **添加 PersistToAppSettings 私有方法** (`Services/UpdateService.cs`)
   - 使用 `System.Text.Json.Nodes` 命名空间（`using System.Text.Json.Nodes;`）
   - 实现：
     ```csharp
     private void PersistToAppSettings(string updateUrl, string channel)
     {
         try
         {
             var path = AppSettingsPath;
             if (!File.Exists(path))
             {
                 _logger.LogWarning("appsettings.json 文件不存在，跳过持久化: {Path}", path);
                 return;
             }
             var json = File.ReadAllText(path);
             var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Failed to parse appsettings.json");
             if (node["Update"] == null)
                 node["Update"] = new JsonObject();
             node["Update"]!["UpdateUrl"] = updateUrl;
             node["Update"]!["Channel"] = channel;
             var options = new JsonWriterOptions { Indented = true };
             File.WriteAllText(path, node.ToJsonString(options));
             _logger.LogInformation("已将更新源配置持久化到 appsettings.json");
         }
         catch (Exception ex)
         {
             _logger.LogWarning(ex, "持久化更新源配置到 appsettings.json 失败，内存热重载已生效");
         }
     }
     ```

3. **在 ReloadSource 方法末尾调用 PersistToAppSettings** (`Services/UpdateService.cs`)
   - 在字段更新和日志记录之后调用 `PersistToAppSettings(updateUrl ?? "", _channel)`
   - 传入的 updateUrl 是原始参数（不含通道路径），channel 是处理后的值

4. **添加必要的 using** (`Services/UpdateService.cs`)
   - `using System.IO;`（可能已通过 ImplicitUsings 包含）
   - `using System.Text.Json.Nodes;`
   - `using System.Text.Json;`（JsonWriterOptions 需要）

5. **编写持久化测试** (`Tests/UpdateServiceTests.cs`)
   - 辅助方法：创建临时 appsettings.json 文件，写入初始 JSON 内容，返回路径
   - `ReloadSource_persists_to_appsettings_json`：
     - 创建临时目录和 appsettings.json
     - 构造 UpdateService（IConfiguration 从 in-memory），设置 AppSettingsPath 为临时文件
     - 调用 ReloadSource("http://192.168.1.100:8080", "beta")
     - 读取临时文件，解析 JSON，Assert Update:UpdateUrl == "http://192.168.1.100:8080", Update:Channel == "beta"
   - `ReloadSource_empty_url_persists_empty_string`：
     - 同上但调用 ReloadSource("", "stable")
     - Assert Update:UpdateUrl == "", Update:Channel == "stable"
   - `ReloadSource_persistence_failure_does_not_throw`：
     - 设置 AppSettingsPath 为不存在的路径（非文件）
     - 调用 ReloadSource — 应不抛异常
     - Assert 内存字段已更新（UpdateSourceType 正确）
   - `ReloadSource_preserves_other_settings`：
     - 创建包含 Logging 等其他配置节的 appsettings.json
     - 调用 ReloadSource
     - Assert 其他配置节未被修改

## Must-Haves

- [ ] PersistToAppSettings 使用 System.Text.Json.Nodes 正确读写 JSON
- [ ] ReloadSource 末尾调用 PersistToAppSettings
- [ ] AppSettingsPath 属性支持测试替换
- [ ] 文件写入失败不抛异常，内存热重载仍然成功
- [ ] appsettings.json 中其他配置节不被破坏
- [ ] 至少 4 个新测试覆盖持久化场景

## Verification

- `dotnet test --filter "UpdateServiceTests" --verbosity normal` — 所有测试通过（含 T01 + T02 新测试）
- `dotnet build` — 构建无错误

## Inputs

- `Services/UpdateService.cs` — T01 完成后的 UpdateService，已有 ReloadSource 方法框架
- `Tests/UpdateServiceTests.cs` — T01 完成后的测试文件，已有 ReloadSource 内存测试

## Expected Output

- `Services/UpdateService.cs` — ReloadSource 包含 PersistToAppSettings 调用
- `Tests/UpdateServiceTests.cs` — 新增持久化测试
