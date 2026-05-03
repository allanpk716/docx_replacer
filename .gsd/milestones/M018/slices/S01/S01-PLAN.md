# S01: 便携版更新解锁

**Goal:** 移除所有便携版更新阻断逻辑，让便携版 GUI 状态栏显示正常更新状态（不是"不支持自动更新"），CLI `update --yes` 能走完整更新流程。IUpdateService 新增 IsPortable 属性供下游区分运行模式。
**Demo:** 便携版 GUI 状态栏显示正常更新状态；便携版 CLI update --yes 完成检查→下载→应用更新流程

## Must-Haves

- UpdateStatus.PortableVersion 枚举值已移除
- MainWindowViewModel.InitializeUpdateStatusAsync 中无 IsInstalled 守卫阻断
- UpdateCommand.ExecuteAsync 中无 PORTABLE_NOT_SUPPORTED 错误返回
- IUpdateService 接口新增 IsPortable 属性
- UpdateService 实现 IsPortable（读取 Velopack UpdateManager.IsPortable）
- dotnet build 编译通过
- dotnet test 全部通过
- 所有 IUpdateService 测试 stub 已添加 IsPortable 属性

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: Velopack UpdateManager.IsPortable (SDK 原生属性), 现有 UpdateService.CheckForUpdatesAsync / DownloadUpdatesAsync / ApplyUpdatesAndRestart（无改动）
- New wiring introduced in this slice: IUpdateService.IsPortable 属性供 UI/CLI 区分运行模式，不阻断任何流程
- What remains before the milestone is truly usable end-to-end: S02 E2E 测试脚本验证便携版完整更新链路（本地 HTTP + 内网 Go 服务器）

## Verification

- Not provided.

## Tasks

- [x] **T01: Add IsPortable to IUpdateService and implement in UpdateService** `est:15m`
  在 IUpdateService 接口新增 IsPortable 属性（R002），在 UpdateService 中使用 Velopack UpdateManager.IsPortable 实现。同时更新所有实现 IUpdateService 的测试 stub 类以包含新属性。

### Steps
1. 在 `Services/Interfaces/IUpdateService.cs` 的接口定义中，在 `IsInstalled` 属性下方新增 `IsPortable` 属性，XML 注释说明："当前应用是否为便携版运行（解压自 Portable.zip）"。同时将 `IsInstalled` 的注释改为"当前应用是否为安装版（信息属性，不用于流程阻断）"。
2. 在 `Services/UpdateService.cs` 中：
   - 新增 `private readonly bool _isPortable;` 私有字段
   - 在构造函数的 `IsInstalled` 检测代码块中，使用同一个 `tempManager` 检测 `tempManager.IsPortable` 并赋值给 `_isPortable`
   - 在构造函数日志行中追加 `IsPortable: {_isPortable}`
   - 新增 `public bool IsPortable => _isPortable;` 属性实现
3. 更新 `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` 中的 `StubUpdateService`：添加 `public bool IsPortable => false;`
4. 更新 `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs` 中的 `StubUpdateService`：添加 `public bool IsPortable => false;`
5. 更新 `Tests/UpdateServiceTests.cs`：无 stub 需要修改（IsInstalled 测试直接测 UpdateService 实例），可选新增 IsPortable 测试

### Must-Haves
- [ ] IUpdateService 接口包含 IsPortable 属性
- [ ] UpdateService.IsPortable 从 Velopack UpdateManager.IsPortable 读取
- [ ] 所有实现 IUpdateService 的测试 stub 编译通过

### Verification
- `dotnet build` 编译通过
- `grep -n "IsPortable" Services/Interfaces/IUpdateService.cs` 输出包含接口属性定义

### Inputs
- `Services/Interfaces/IUpdateService.cs` — 当前接口定义，需新增 IsPortable
- `Services/UpdateService.cs` — 当前实现，需新增 IsPortable 实现
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — 测试 stub 实现 IUpdateService
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs` — 测试 stub 实现 IUpdateService

### Expected Output
- `Services/Interfaces/IUpdateService.cs` — 新增 IsPortable 属性定义
- `Services/UpdateService.cs` — 新增 IsPortable 属性实现（读取 Velopack IsPortable）
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs` — StubUpdateService 新增 IsPortable
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs` — StubUpdateService 新增 IsPortable
  - Files: `Services/Interfaces/IUpdateService.cs`, `Services/UpdateService.cs`, `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`, `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
  - Verify: dotnet build && grep -q "IsPortable" Services/Interfaces/IUpdateService.cs && echo PASS

- [x] **T02: Remove portable version update blocking in GUI, CLI, and tests** `est:30m`
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
  - Files: `ViewModels/MainWindowViewModel.cs`, `Cli/Commands/UpdateCommand.cs`, `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
  - Verify: dotnet build && dotnet test && ! grep -q "PortableVersion" ViewModels/MainWindowViewModel.cs && ! grep -q "PORTABLE_NOT_SUPPORTED" Cli/Commands/UpdateCommand.cs && echo PASS

## Files Likely Touched

- Services/Interfaces/IUpdateService.cs
- Services/UpdateService.cs
- Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
- Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
- ViewModels/MainWindowViewModel.cs
- Cli/Commands/UpdateCommand.cs
