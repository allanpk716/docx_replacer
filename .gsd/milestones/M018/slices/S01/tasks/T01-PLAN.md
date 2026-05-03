---
estimated_steps: 28
estimated_files: 4
skills_used: []
---

# T01: Add IsPortable to IUpdateService and implement in UpdateService

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

## Inputs

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`

## Expected Output

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`

## Verification

dotnet build && grep -q "IsPortable" Services/Interfaces/IUpdateService.cs && echo PASS
