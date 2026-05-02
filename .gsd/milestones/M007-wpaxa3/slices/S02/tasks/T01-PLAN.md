---
estimated_steps: 1
estimated_files: 4
skills_used: []
---

# T01: 实现 UpdateService 并注册到 DI 容器

创建 `Services/UpdateService.cs`，实现 `IUpdateService` 接口，封装 Velopack `UpdateManager` 的检查更新、下载更新、应用更新并重启三个核心方法。从 `IConfiguration` 读取 `Update:UpdateUrl` 配置节点，空字符串时 `IsUpdateUrlConfigured` 返回 false。在 `App.xaml.cs` 的 `BuildServiceProvider` 方法中注册为 Singleton 服务。所有异常向上传播，由 ViewModel 层处理用户提示。

## Inputs

- `Services/Interfaces/IUpdateService.cs`

## Expected Output

- `Services/UpdateService.cs`
- `App.xaml.cs`

## Verification

dotnet build && dotnet test

## Observability Impact

Signals added: ILogger<UpdateService> logs at Information level for check/start/download/apply lifecycle; Warning for unconfigured URL; Error for exceptions during update operations.
