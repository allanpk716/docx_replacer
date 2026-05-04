# S05: S05: 配置清理和测试补充 — UAT

**Milestone:** M020
**Written:** 2026-05-04T02:31:07.582Z

# UAT: S05 配置清理和测试补充

## UAT Type
Contract verification — 通过构建和自动化测试验证配置清理不破坏现有功能，新测试证明两个服务的基础行为正确。无需运行时手动验证。

## Preconditions
- .NET 8 SDK 已安装
- 项目位于 M020 worktree 路径
- 依赖已恢复（dotnet restore）

## Test Cases

### TC-01: 幽灵配置类已完全移除
1. 打开 `Configuration/AppSettings.cs`
2. **Expected**: 文件仅包含 `PerformanceSettings` 类，有且仅有 `EnableTemplateCache` 和 `CacheExpirationMinutes` 两个属性
3. **Expected**: 文件中不存在 `AppSettings`、`LoggingSettings`、`FileProcessingSettings`、`UISettings` 类定义
4. 打开 `appsettings.json`
5. **Expected**: 仅包含 `Performance` 和 `Update` 两个顶层 section

### TC-02: 幽灵 DI 注册已清理
1. 搜索 `App.xaml.cs` 中所有 `Configure<` 调用
2. **Expected**: 仅存在 `Configure<PerformanceSettings>` 一行

### TC-03: TemplateCacheService 单元测试通过
1. 运行 `dotnet test --filter "FullyQualifiedName~TemplateCacheServiceTests" --verbosity normal`
2. **Expected**: 11 个测试全部 Passed

### TC-04: FileService 单元测试通过
1. 运行 `dotnet test --filter FileServiceTests --verbosity normal`
2. **Expected**: 17 个测试全部 Passed（4 error-path + 13 happy-path）

### TC-05: 全量构建和测试无回归
1. 运行 `dotnet build`
2. **Expected**: 0 errors
3. 运行 `dotnet test --no-build --verbosity minimal`
4. **Expected**: 所有测试通过，0 failures

## Not Proven By This UAT
- 运行时行为验证（配置值实际被 TemplateCacheService 正确读取）——由 S01 的日志行为变更和现有集成测试间接覆盖
- 性能影响——配置精简对启动时间的影响可忽略不计

