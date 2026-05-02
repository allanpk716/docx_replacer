---
estimated_steps: 8
estimated_files: 1
skills_used: []
---

# T02: 验证 UpdateSettingsViewModel 路径一致性和全量回归

1. 在 Tests/UpdateSettingsViewModelTests.cs 中添加路径一致性测试：验证 UpdateService.GetPersistentConfigPath() 返回的路径与 UpdateSettingsViewModel 内部使用的路径相同（通过检查 UpdateSettingsViewModel.ReadPersistentConfig 的调用目标是 UpdateService.GetPersistentConfigPath）

由于 UpdateSettingsViewModel.ReadPersistentConfig() 是 private static 且直接调用 UpdateService.GetPersistentConfigPath()（无注入点），无法在不污染真实目录的情况下进行集成测试。因此采用以下策略：
- 添加编译时保证测试：用反射或代码分析确认 ViewModel 的 ReadPersistentConfig 方法调用了 UpdateService.GetPersistentConfigPath()
- 或者更实际的做法：验证 ViewModel 构造函数在无持久化配置文件时的行为与之前一致（已有测试覆盖）

2. 运行完整 dotnet test 确认 244+ 测试全部通过（0 failures）
3. 确认 dotnet build 0 errors

如果反射验证过于复杂，可以简化为：确认 GetPersistentConfigPath() 返回固定路径，ViewModel 的源码中直接调用此方法（代码审查确认），无需额外测试。在此情况下，本任务主要是全量回归验证。

验证 R056 的完整测试覆盖：路径一致性、配置读写、异常处理。

## Inputs

- `ViewModels/UpdateSettingsViewModel.cs`
- `Services/UpdateService.cs`
- `Tests/UpdateSettingsViewModelTests.cs`

## Expected Output

- `Tests/UpdateSettingsViewModelTests.cs`

## Verification

dotnet test --nologo -v q && dotnet build --nologo -v q
