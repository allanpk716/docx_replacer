---
estimated_steps: 11
estimated_files: 1
skills_used: []
---

# T01: 新增 UpdateService 持久化配置边界测试

在 Tests/UpdateServiceTests.cs 中添加边界测试，覆盖持久化配置路径逻辑的异常和边界场景：

1. 配置文件包含 malformed JSON（如 "{invalid"）时 ReadPersistentConfig 返回 null fallback，不崩溃
2. 配置文件 JSON 中缺少 UpdateUrl 字段时，url 为 null，构造函数 fallback 到 appsettings.json
3. 配置文件 JSON 中缺少 Channel 字段时，channel 为 null，构造函数默认 stable
4. 配置文件为空文件（0 bytes）时不崩溃
5. EnsurePersistentConfigSync 在目录已存在且文件已存在时不覆盖已有内容

所有测试使用 CreateTestService/CleanupTestService 辅助方法注入临时路径，避免污染真实用户目录。

注意事项：
- 测试中不要修改 Services/UpdateService.cs，只添加测试
- 使用已有的 CreateTestService 和 CleanupTestService 辅助方法
- 对于需要预写配置文件的测试，直接在临时目录中创建文件

## Inputs

- `Services/UpdateService.cs`
- `Tests/UpdateServiceTests.cs`

## Expected Output

- `Tests/UpdateServiceTests.cs`

## Verification

dotnet test --filter UpdateServiceTests --nologo -v q
