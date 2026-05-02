# S02: S02: 测试更新与验证 — UAT

**Milestone:** M013-ueix00
**Written:** 2026-05-02T12:30:06.294Z

# UAT: S02 测试更新与验证

## 前置条件

- 开发环境已安装 .NET 8 SDK
- 工作目录为项目根目录

## 测试用例

### TC-01: 全量回归测试通过

1. 运行 `dotnet build --nologo -v q`
2. 预期：0 errors, 0 warnings
3. 运行 `dotnet test --nologo -v q`
4. 预期：249 tests pass, 0 fail, 0 skip

### TC-02: UpdateService 边界测试覆盖

1. 运行 `dotnet test --filter UpdateServiceTests --nologo -v q`
2. 预期：29 tests pass
3. 确认以下 5 个新测试存在：
   - `ReadPersistentConfig_malformed_json_falls_back_to_appsettings`
   - `ReadPersistentConfig_missing_UpdateUrl_field_falls_back_to_appsettings`
   - `ReadPersistentConfig_missing_Channel_field_defaults_to_stable`
   - `ReadPersistentConfig_empty_file_does_not_crash`
   - `EnsurePersistentConfigSync_does_not_overwrite_existing_file`

### TC-03: 路径一致性保证

1. 打开 `ViewModels/UpdateSettingsViewModel.cs`
2. 定位 `ReadPersistentConfig` 方法
3. 确认该方法调用 `UpdateService.GetPersistentConfigPath()`
4. 预期：两个类使用完全相同的路径计算逻辑
