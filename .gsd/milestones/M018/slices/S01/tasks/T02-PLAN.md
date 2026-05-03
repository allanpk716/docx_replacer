---
estimated_steps: 38
estimated_files: 3
skills_used: []
---

# T02: Remove portable version update blocking in GUI, CLI, and tests

移除所有便携版更新阻断逻辑：GUI 状态栏不再显示"不支持自动更新"，CLI `update --yes` 不再返回 PORTABLE_NOT_SUPPORTED 错误，便携版走与安装版完全相同的更新代码路径。

### Steps
1. **MainWindowViewModel.cs — 移除 PortableVersion 枚举值**：
   - 删除 `UpdateStatus.PortableVersion` 枚举成员（约第 30 行）
   - 在 `UpdateStatusMessage` 属性的 switch 表达式中，移除 `UpdateStatus.PortableVersion => "便携版不支持自动更新"` 分支
   - 在 `UpdateStatusBrush` 属性的 switch 表达式中，移除 `UpdateStatus.PortableVersion => Brushes.Gray` 分支

2. **MainWindowViewModel.cs — InitializeUpdateStatusAsync**：
   - 删除整个 `if (!_updateService.IsInstalled)` 代码块（约第 1393-1397 行），让便携版进入正常的更新检查流程

3. **MainWindowViewModel.cs — OnUpdateStatusClickAsync**：
   - 移除 `case UpdateStatus.PortableVersion:` 分支及其 MessageBox.Show 调用（约第 1441-1445 行）

4. **UpdateCommand.cs — 移除 IsInstalled 守卫**：
   - 删除 `if (!_updateService.IsInstalled)` 代码块（约第 66-70 行），便携版直接进入下载流程

5. **UpdateCommandTests.cs — 更新便携版测试**：
   - 将 `Update_WithYes_Portable_OutputsError` 测试改名为 `Update_WithYes_Portable_ProceedsNormally`
   - 修改测试：设置 `IsInstalledValue = false`（便携模式），预期行为应与安装版相同——检查到更新后进入下载流程
   - 预期 exitCode 为 0（而非 1），验证输出包含下载进度信息而非错误
   - 注意：由于 `ApplyUpdatesAndRestart()` 在 stub 中是 no-op，下载完成后不会真正重启，但应能正常返回

6. **dotnet build && dotnet test** 验证全部通过

### Must-Haves
- [ ] UpdateStatus.PortableVersion 枚举值已删除
- [ ] InitializeUpdateStatusAsync 中无 IsInstalled 阻断守卫
- [ ] OnUpdateStatusClickAsync 中无 PortableVersion 分支
- [ ] UpdateCommand.ExecuteAsync 中无 PORTABLE_NOT_SUPPORTED 返回
- [ ] 便携版 CLI 测试验证正常更新流程
- [ ] dotnet build + dotnet test 全部通过

### Verification
- `dotnet build` 编译通过
- `! grep -q "PortableVersion" ViewModels/MainWindowViewModel.cs` 无残留引用
- `! grep -q "PORTABLE_NOT_SUPPORTED" Cli/Commands/UpdateCommand.cs` 无残留错误码
- `dotnet test` 全部通过

### Inputs
- `ViewModels/MainWindowViewModel.cs` — 包含 PortableVersion 枚举和阻断逻辑
- `Cli/Commands/UpdateCommand.cs` — 包含 IsInstalled 守卫和 PORTABLE_NOT_SUPPORTED
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — 包含便携版错误测试

### Expected Output
- `ViewModels/MainWindowViewModel.cs` — 移除所有便携版阻断逻辑
- `Cli/Commands/UpdateCommand.cs` — 移除 IsInstalled 守卫
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — 便携版测试改为验证正常流程

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `Cli/Commands/UpdateCommand.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`

## Expected Output

- `ViewModels/MainWindowViewModel.cs`
- `Cli/Commands/UpdateCommand.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`

## Verification

dotnet build && dotnet test && ! grep -q "PortableVersion" ViewModels/MainWindowViewModel.cs && ! grep -q "PORTABLE_NOT_SUPPORTED" Cli/Commands/UpdateCommand.cs && echo PASS
