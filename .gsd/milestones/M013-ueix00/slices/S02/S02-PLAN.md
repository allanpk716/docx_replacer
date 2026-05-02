# S02: 测试更新与验证

**Goal:** 验证 S01 路径迁移后的完整回归安全性和新增边界测试覆盖：所有 244 个现有测试继续通过，新增测试覆盖持久化配置的边界情况（JSON 格式错误、缺失字段、空文件等），验证 UpdateService 和 UpdateSettingsViewModel 使用相同路径。
**Demo:** 所有现有测试通过，新增路径逻辑有测试覆盖

## Must-Haves

- dotnet build 0 errors, dotnet test 244+ 全部通过（无回归）
- 新增 UpdateService 边界测试：JSON 格式错误、缺失字段、空文件
- 验证 UpdateSettingsViewModel.ReadPersistentConfig() 与 UpdateService.GetPersistentConfigPath() 路径一致

## Proof Level

- This slice proves: contract

## Integration Closure

Upstream surfaces consumed: UpdateService.GetPersistentConfigPath() (public static), UpdateSettingsViewModel.ReadPersistentConfig() (private static calling GetPersistentConfigPath). New wiring: none — this slice only adds test coverage. What remains: nothing — this is the final slice in M013.

## Verification

- Not provided.

## Tasks

- [x] **T01: 新增 UpdateService 持久化配置边界测试** `est:30m`
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
  - Files: `Tests/UpdateServiceTests.cs`
  - Verify: dotnet test --filter UpdateServiceTests --nologo -v q

- [x] **T02: 验证 UpdateSettingsViewModel 路径一致性和全量回归** `est:20m`
  1. 在 Tests/UpdateSettingsViewModelTests.cs 中添加路径一致性测试：验证 UpdateService.GetPersistentConfigPath() 返回的路径与 UpdateSettingsViewModel 内部使用的路径相同（通过检查 UpdateSettingsViewModel.ReadPersistentConfig 的调用目标是 UpdateService.GetPersistentConfigPath）

由于 UpdateSettingsViewModel.ReadPersistentConfig() 是 private static 且直接调用 UpdateService.GetPersistentConfigPath()（无注入点），无法在不污染真实目录的情况下进行集成测试。因此采用以下策略：
- 添加编译时保证测试：用反射或代码分析确认 ViewModel 的 ReadPersistentConfig 方法调用了 UpdateService.GetPersistentConfigPath()
- 或者更实际的做法：验证 ViewModel 构造函数在无持久化配置文件时的行为与之前一致（已有测试覆盖）

2. 运行完整 dotnet test 确认 244+ 测试全部通过（0 failures）
3. 确认 dotnet build 0 errors

如果反射验证过于复杂，可以简化为：确认 GetPersistentConfigPath() 返回固定路径，ViewModel 的源码中直接调用此方法（代码审查确认），无需额外测试。在此情况下，本任务主要是全量回归验证。

验证 R056 的完整测试覆盖：路径一致性、配置读写、异常处理。
  - Files: `Tests/UpdateSettingsViewModelTests.cs`
  - Verify: dotnet test --nologo -v q && dotnet build --nologo -v q

## Files Likely Touched

- Tests/UpdateServiceTests.cs
- Tests/UpdateSettingsViewModelTests.cs
