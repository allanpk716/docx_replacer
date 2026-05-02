---
estimated_steps: 32
estimated_files: 2
skills_used: []
---

# T02: Add unit tests for channel URL construction

为 UpdateService 的通道 URL 构造逻辑添加单元测试。验证各种配置组合下 URL 的正确构造：默认通道、显式 beta、Channel 键缺失、UpdateUrl 为空、末尾斜杠处理。将 UpdateService.cs 添加到测试项目编译引用。

## Steps

1. 在 Tests/DocuFiller.Tests.csproj 的 ItemGroup 中添加：
   `<Compile Include="..\Services\UpdateService.cs" Link="Services\UpdateService.cs" />`
2. 在 Tests/DocuFiller.Tests.csproj 添加 NuGet 包引用：
   `<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />`
3. 在主项目 DocuFiller.csproj 中添加 InternalsVisibleTo（如果 UpdateService 的 URL 构造使用 internal 方法）：
   `<InternalsVisibleTo Include="DocuFiller.Tests" />`
4. 为了使 URL 可测试，在 UpdateService 中：
   - 添加 internal 属性 `string EffectiveUpdateUrl => _updateUrl;` 返回构造后的完整 URL
   - 这样测试可以验证最终传给 UpdateManager 的 URL
5. 创建 Tests/UpdateServiceTests.cs，包含以下测试用例（使用 xunit）：
   - **Channel_defaults_to_stable_when_empty**: IConfiguration 中 Channel="" → Channel 属性返回 "stable"，EffectiveUpdateUrl 包含 "/stable/"
   - **Channel_explicit_beta**: IConfiguration 中 Channel="beta" → Channel 属性返回 "beta"，EffectiveUpdateUrl 包含 "/beta/"
   - **Channel_missing_key**: IConfiguration 中无 Channel 键 → 默认 "stable"
   - **UpdateUrl_empty_not_configured**: UpdateUrl="" → IsUpdateUrlConfigured 返回 false
   - **UpdateUrl_with_trailing_slash**: UpdateUrl="http://server/" → EffectiveUpdateUrl 为 "http://server/stable/"（不双斜杠）
   - **UpdateUrl_without_trailing_slash**: UpdateUrl="http://server" → EffectiveUpdateUrl 为 "http://server/stable/"
6. 每个测试用 ConfigurationBuilder 或字典构造 mock IConfiguration
7. 使用 LoggerFactory.Create 创建测试用 logger
8. 运行 dotnet test --filter UpdateServiceTests 确认全部通过
9. 运行完整 dotnet test 确认无回归

## Must-Haves

- [ ] UpdateServiceTests.cs 包含 6 个测试用例
- [ ] 测试项目编译包含 UpdateService.cs
- [ ] dotnet test --filter UpdateServiceTests 全部通过
- [ ] 完整 dotnet test 无回归

## Test Infrastructure Notes

- mock IConfiguration: 用 `new ConfigurationBuilder().AddInMemoryCollection(dict).Build()`
- dict 中设置 "Update:UpdateUrl" 和 "Update:Channel"
- mock ILogger: 用 `LoggerFactory.Create(b => { }).CreateLogger<UpdateService>()`
- UpdateService 需要暴露 EffectiveUpdateUrl（internal），配合 InternalsVisibleTo

## Inputs

- ``Services/UpdateService.cs` — channel-aware UpdateService implementation (T01 output)`
- ``Services/Interfaces/IUpdateService.cs` — IUpdateService interface (T01 output)`
- ``Tests/DocuFiller.Tests.csproj` — test project file`

## Expected Output

- ``Tests/UpdateServiceTests.cs` — new test file with 6 channel URL construction tests`
- ``Tests/DocuFiller.Tests.csproj` — updated with UpdateService.cs compile include and config packages`

## Verification

dotnet test --filter UpdateServiceTests passes with 6 tests
