# S05: 配置清理和测试补充

**Goal:** 清理 appsettings.json 和 Configuration/AppSettings.cs 中从未被代码引用的幽灵配置值，为 FileService 和 TemplateCacheService 补充基础单元测试覆盖。
**Demo:** appsettings.json 只保留实际使用的配置值，FileService 和 TemplateCacheService 有基础单元测试

## Must-Haves

- LoggingSettings、FileProcessingSettings、UISettings、AppSettings 类已从 Configuration/AppSettings.cs 移除
- PerformanceSettings 只保留 EnableTemplateCache 和 CacheExpirationMinutes（MaxConcurrentProcessing、ProcessingTimeout 已移除）
- appsettings.json 只保留 Performance（2 个属性）和 Update（2 个属性）两个 section
- App.xaml.cs 中 4 行幽灵 DI 注册（Configure<AppSettings>、Configure<LoggingSettings>、Configure<FileProcessingSettings>、Configure<UISettings>）已删除
- TemplateCacheService 有基础单元测试：缓存 CRUD、过期清除、Dispose 后 ObjectDisposedException
- FileService 有快乐路径单元测试：FileExists、EnsureDirectoryExists、CopyFileAsync、WriteFileContentAsync、ReadFileContentAsync、DeleteFile、ValidateFileExtension
- dotnet build 0 错误，dotnet test 全部通过

## Proof Level

- This slice proves: contract — 通过单元测试验证配置清理不破坏现有功能，通过新测试证明两个服务的基础行为正确。不需要运行时或集成测试。

## Integration Closure

- Upstream surfaces consumed: PerformanceSettings (by TemplateCacheService), Update section (by UpdateService via IConfiguration)
- New wiring: test csproj 需要新增 TemplateCacheService.cs 和 Configuration/AppSettings.cs 的 Compile Include 链接，以及 Microsoft.Extensions.Options NuGet
- What remains: 无。这是 M020 最后一个 slice。

## Verification

- 清理幽灵配置后，TemplateCacheService 的日志消息将继续通过 ILogger 工作
- 新增测试验证错误路径的 LogError 调用，确保 S01 添加的日志行为不被破坏

## Tasks

- [ ] **T01: 清理幽灵配置值和废弃的配置类** `est:45m`
  移除从未被代码引用的幽灵配置：删除 LoggingSettings、FileProcessingSettings、UISettings、AppSettings 四个类，从 PerformanceSettings 中移除 MaxConcurrentProcessing 和 ProcessingTimeout，精简 appsettings.json，清理 App.xaml.cs 中的幽灵 DI 注册。
  - Files: `Configuration/AppSettings.cs`, `appsettings.json`, `App.xaml.cs`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M020 && dotnet build 2>&1 | tail -5 && dotnet test --no-build --verbosity minimal 2>&1 | tail -8

- [ ] **T02: 添加 TemplateCacheService 单元测试** `est:1h`
  为 TemplateCacheService 创建基础单元测试文件，覆盖缓存 CRUD、过期清除、缓存禁用、Dispose 后异常等核心行为。需要在测试 csproj 中添加 TemplateCacheService.cs 和 Configuration/AppSettings.cs 的链接引用，以及 Microsoft.Extensions.Options NuGet 包。
  - Files: `Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs`, `Tests/DocuFiller.Tests.csproj`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M020 && dotnet test --filter TemplateCacheServiceTests --verbosity normal 2>&1 | tail -15

- [ ] **T03: 添加 FileService 快乐路径单元测试** `est:30m`
  为 FileService 补充快乐路径单元测试，与现有的 4 个错误路径测试形成完整的覆盖。测试放在现有的 `Tests/DocuFiller.Tests/Services/FileServiceTests.cs` 文件中。
  - Files: `Tests/DocuFiller.Tests/Services/FileServiceTests.cs`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M020 && dotnet test --filter FileServiceTests --verbosity normal 2>&1 | tail -15

## Files Likely Touched

- Configuration/AppSettings.cs
- appsettings.json
- App.xaml.cs
- Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs
- Tests/DocuFiller.Tests.csproj
- Tests/DocuFiller.Tests/Services/FileServiceTests.cs
