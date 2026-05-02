---
id: T01
parent: S01
milestone: M011-ns0oo0
key_files:
  - ViewModels/UpdateSettingsViewModel.cs
  - Tests/UpdateSettingsViewModelTests.cs
  - Tests/DocuFiller.Tests.csproj
  - Tests/UpdateServiceTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-30T06:04:48.797Z
blocker_discovered: false
---

# T01: 修复 UpdateSettingsViewModel 构造函数从 IConfiguration 直接读取 UpdateUrl/Channel 原始值，替换脆弱的 EffectiveUpdateUrl 剥离逻辑

**修复 UpdateSettingsViewModel 构造函数从 IConfiguration 直接读取 UpdateUrl/Channel 原始值，替换脆弱的 EffectiveUpdateUrl 剥离逻辑**

## What Happened

UpdateSettingsViewModel 构造函数原本从 IUpdateService.EffectiveUpdateUrl 剥离通道路径后缀（如 "/stable/"）来恢复用户输入的原始 URL，这种剥离逻辑在尾部斜杠、大小写等边界条件下容易出错。按照决策 D033，将数据源改为直接从 IConfiguration["Update:UpdateUrl"] 读取原始值。

具体变更：
1. UpdateSettingsViewModel 构造函数新增 IConfiguration 参数
2. 从 configuration["Update:UpdateUrl"] 直接读取原始 URL（空值返回空字符串，非空值 Trim）
3. 从 configuration["Update:Channel"] 读取通道，为空时 fallback 到 _updateService.Channel
4. 删除旧的 EffectiveUpdateUrl 剥离逻辑（约 20 行 if/else + EndsWith/Substring 代码）
5. App.xaml.cs 无需修改（IConfiguration 已在 DI 中注册为 Singleton，UpdateSettingsViewModel 已注册为 Transient，DI 自动解析新参数）
6. UpdateSettingsWindow.xaml.cs 无需修改（通过 ServiceProvider.GetRequiredService 解析，DI 自动注入）

测试覆盖（11 个测试全部通过）：
- HTTP URL 正确回显原始值
- GitHub 模式空 URL 回显空字符串
- GitHub 模式 null URL 回显空字符串
- Channel 从 IConfiguration 正确读取
- Channel 为空时 fallback 到 service channel
- Channel 为 null 时 fallback 到 service channel
- SourceTypeDisplay 正确显示 HTTP/GitHub
- URL 和 Channel 含空格时 Trim
- Channels 集合包含 stable 和 beta
- null IConfiguration 不抛异常

附带修复：Tests/DocuFiller.Tests.csproj 添加 UseWPF=true 支持 RelayCommand（WPF 类型），并修复 UpdateServiceTests.cs 缺少 using System.IO 导致的编译错误。

## Verification

1. dotnet test --filter "FullyQualifiedName~UpdateSettingsViewModelTests" — 11 tests passed
2. dotnet test --no-restore — 全部 176 tests passed (0 failed)
3. 代码审查确认构造函数逻辑正确：直接读 IConfiguration，不再依赖 EffectiveUpdateUrl 剥离

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter "FullyQualifiedName~UpdateSettingsViewModelTests"` | 0 | ✅ pass | 3300ms |
| 2 | `dotnet test --no-restore --verbosity minimal` | 0 | ✅ pass | 16000ms |

## Deviations

1. 计划未提及需要修改 Tests/DocuFiller.Tests.csproj（添加 UseWPF=true、Moq 包、ViewModels Compile Include 链接），但测试编译需要这些变更
2. 计划未提及需要修复 Tests/UpdateServiceTests.cs 的 using System.IO 缺失问题（UseWPF=true 引入后导致 System.IO 类型冲突）

## Known Issues

None.

## Files Created/Modified

- `ViewModels/UpdateSettingsViewModel.cs`
- `Tests/UpdateSettingsViewModelTests.cs`
- `Tests/DocuFiller.Tests.csproj`
- `Tests/UpdateServiceTests.cs`
