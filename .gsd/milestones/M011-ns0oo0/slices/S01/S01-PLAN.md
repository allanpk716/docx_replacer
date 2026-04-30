# S01: 修复更新设置 URL 回显

**Goal:** 修复 UpdateSettingsViewModel 构造函数中的 URL 回显逻辑：从 IConfiguration 直接读取 Update:UpdateUrl 原始值，替换当前从 EffectiveUpdateUrl 剥离通道路径后缀的脆弱实现。同时为 UpdateSettingsViewModel 添加单元测试覆盖正常回显、GitHub 模式、Channel 默认值等场景。
**Demo:** 打开更新设置窗口，URL 输入框正确显示 appsettings.json 中的 UpdateUrl（如 http://172.18.200.47:30001），Channel 下拉框显示当前通道（如 stable）

## Must-Haves

- UpdateSettingsViewModel.UpdateUrl 正确显示 appsettings.json 中的 Update:UpdateUrl 原始值（如 http://172.18.200.47:30001），不含通道路径后缀
- UpdateSettingsViewModel.Channel 正确显示当前通道（如 stable）
- UpdateSettingsViewModel.SourceTypeDisplay 正确显示 "HTTP" 或 "GitHub"
- UpdateUrl 为空时 UpdateUrl 属性返回空字符串
- 现有 UpdateServiceTests 和全部 192 个测试不受影响

## Proof Level

- This slice proves: contract

## Integration Closure

Upstream surfaces consumed: IUpdateService (Channel, UpdateSourceType, EffectiveUpdateUrl), IConfiguration (Update:UpdateUrl, Update:Channel)
New wiring: UpdateSettingsViewModel 新增 IConfiguration 构造参数
What remains: S02（下载进度弹窗）依赖本 slice 完成后的稳定 UpdateSettings

## Verification

- Not provided.

## Tasks

- [x] **T01: 修复 UpdateSettingsViewModel 从 IConfiguration 读取 URL 原始值并添加单元测试** `est:30m`
  UpdateSettingsViewModel 构造函数当前从 IUpdateService.EffectiveUpdateUrl 剥离通道路径后缀来恢复用户输入的原始 URL。这种剥离逻辑脆弱（尾部斜杠、大小写等边界问题）。决策 D033 确定直接从 IConfiguration 读取 Update:UpdateUrl 原始值。

步骤：
1. 修改 UpdateSettingsViewModel 构造函数，新增 IConfiguration 参数
2. 从 IConfiguration["Update:UpdateUrl"] 读取原始 URL 值赋给 _updateUrl
3. 从 IConfiguration["Update:Channel"] 读取通道，为空时使用 _updateService.Channel 作为 fallback
4. 删除旧的 EffectiveUpdateUrl 剥离逻辑
5. 更新 App.xaml.cs 的 DI 注册（UpdateSettingsViewModel 已注册为 Transient，IConfiguration 已在 DI 中，无需额外注册）
6. 添加 UpdateSettingsViewModelTests.cs 单元测试，覆盖：
   - HTTP URL 模式正确回显原始值
   - GitHub 模式（空 URL）回显空字符串
   - Channel 从 IConfiguration 正确读取
   - Channel 为空时默认 stable
   - SourceTypeDisplay 正确显示
  - Files: `ViewModels/UpdateSettingsViewModel.cs`, `App.xaml.cs`, `Tests/UpdateSettingsViewModelTests.cs`
  - Verify: dotnet test --filter "FullyQualifiedName~UpdateSettingsViewModelTests" && dotnet test --no-restore --verbosity minimal

## Files Likely Touched

- ViewModels/UpdateSettingsViewModel.cs
- App.xaml.cs
- Tests/UpdateSettingsViewModelTests.cs
