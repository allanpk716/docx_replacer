---
estimated_steps: 24
estimated_files: 2
skills_used: []
---

# T02: 添加 TemplateCacheService 单元测试

为 TemplateCacheService 创建基础单元测试文件，覆盖缓存 CRUD、过期清除、缓存禁用、Dispose 后异常等核心行为。需要在测试 csproj 中添加 TemplateCacheService.cs 和 Configuration/AppSettings.cs 的链接引用，以及 Microsoft.Extensions.Options NuGet 包。

**Steps:**
1. 编辑 `Tests/DocuFiller.Tests.csproj`：
   - 添加 `<PackageReference Include="Microsoft.Extensions.Options" Version="10.0.1" />`
   - 添加 `<Compile Include="..\Services\TemplateCacheService.cs" Link="Services\TemplateCacheService.cs" />`
   - 添加 `<Compile Include="..\Configuration\AppSettings.cs" Link="Configuration\AppSettings.cs" />`
2. 创建 `Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs`：
   - 构造函数：使用 `Mock<ILogger<TemplateCacheService>>` 和 `Mock<IOptionsMonitor<PerformanceSettings>>`（设置 `EnableTemplateCache=true`, `CacheExpirationMinutes=30`）
   - **测试用例：**
     - `CacheValidationResult_ThenGet_ReturnsCachedValue` — 缓存后能取回
     - `GetCachedValidationResult_NotCached_ReturnsNull` — 未缓存返回 null
     - `GetCachedValidationResult_Expired_ReturnsNull` — 过期返回 null（设置 CacheExpirationMinutes=0，等待后过期）
     - `CacheContentControls_ThenGet_ReturnsCachedList` — 内容控件缓存/取回
     - `InvalidateCache_RemovesCachedItem` — 指定路径缓存清除
     - `ClearAllCache_RemovesAllItems` — 全量清除
     - `Dispose_ThenAccess_ThrowsObjectDisposedException` — Dispose 后操作抛异常
     - `CacheDisabled_ReturnsNull` — EnableTemplateCache=false 时返回 null
     - `NullOrEmptyTemplatePath_ReturnsNull` — 空路径返回 null
3. 运行 `dotnet test --filter TemplateCacheServiceTests` 确认所有测试通过

**Must-haves:**
- [ ] TemplateCacheServiceTests.cs 包含至少 8 个测试用例
- [ ] 测试 csproj 正确链接 TemplateCacheService.cs 和 AppSettings.cs
- [ ] dotnet test --filter TemplateCacheServiceTests 全部通过

**Notes:** `IOptionsMonitor<T>.CurrentValue` 的 mock 需要设置 `Setup(x => x.CurrentValue).Returns(settings)`

## Inputs

- `Services/TemplateCacheService.cs`
- `Configuration/AppSettings.cs`
- `Services/Interfaces/ITemplateCacheService.cs`
- `Models/ContentControlData.cs`
- `Utils/ValidationHelper.cs`
- `Tests/DocuFiller.Tests.csproj`

## Expected Output

- `Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs`
- `Tests/DocuFiller.Tests.csproj`

## Verification

cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M020 && dotnet test --filter TemplateCacheServiceTests --verbosity normal 2>&1 | tail -15
