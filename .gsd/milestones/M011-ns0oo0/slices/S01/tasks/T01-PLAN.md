---
estimated_steps: 13
estimated_files: 3
skills_used: []
---

# T01: 修复 UpdateSettingsViewModel 从 IConfiguration 读取 URL 原始值并添加单元测试

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

## Inputs

- `ViewModels/UpdateSettingsViewModel.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `App.xaml.cs`
- `appsettings.json`

## Expected Output

- `ViewModels/UpdateSettingsViewModel.cs`
- `Tests/UpdateSettingsViewModelTests.cs`

## Verification

dotnet test --filter "FullyQualifiedName~UpdateSettingsViewModelTests" && dotnet test --no-restore --verbosity minimal
