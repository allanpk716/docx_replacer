---
id: T01
parent: S02
milestone: M007-wpaxa3
key_files:
  - Services/UpdateService.cs
  - App.xaml.cs
key_decisions:
  - ApplyUpdatesAndRestart 使用 UpdateManager.UpdatePendingRestart 获取已下载资产，因为 Velopack 0.0.1298 的 ApplyUpdatesAndRestart 要求传入 VelopackAsset 参数
duration: 
verification_result: passed
completed_at: 2026-04-24T05:53:45.370Z
blocker_discovered: false
---

# T01: 实现 UpdateService 封装 Velopack 更新管理器并注册到 DI 容器

**实现 UpdateService 封装 Velopack 更新管理器并注册到 DI 容器**

## What Happened

创建了 `Services/UpdateService.cs`，实现 `IUpdateService` 接口的三个核心方法：CheckForUpdatesAsync（检查更新）、DownloadUpdatesAsync（下载更新包，支持进度回调）、ApplyUpdatesAndRestart（应用已下载更新并重启）。服务从 IConfiguration 读取 `Update:UpdateUrl` 配置节点，空字符串时 `IsUpdateUrlConfigured` 返回 false。每个方法都创建新的 UpdateManager 实例以避免状态管理问题，ApplyUpdatesAndRestart 使用 `UpdatePendingRestart` 属性获取已下载的待应用更新包。在 `App.xaml.cs` 的 BuildServiceProvider 中注册为 Singleton 服务。所有异常向上传播由 ViewModel 层处理。添加了 ILogger 日志记录：Information 级别记录检查/下载/应用生命周期，Warning 记录未配置 URL，Error 由调用方处理。构建通过 0 错误 0 警告，全部 162 个测试通过。

## Verification

运行 `dotnet build` 构建成功（0 错误 0 警告），`dotnet test` 全部 162 个测试通过（135 单元测试 + 27 E2E 测试）。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2100ms |
| 2 | `dotnet test` | 0 | ✅ pass | 13000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Services/UpdateService.cs`
- `App.xaml.cs`
