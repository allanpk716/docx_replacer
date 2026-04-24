# S02: 更新服务 + 状态栏 UI

**Goal:** 主窗口底部状态栏显示当前版本号和"检查更新"按钮，点击后调用 UpdateService 检测版本，有新版本弹出自定义确认对话框，无新版本显示"已是最新"。更新源未配置时按钮灰显。
**Demo:** 主窗口底部状态栏显示当前版本号，点击检查更新可连接更新源检测版本，有新版本显示确认对话框，无新版本显示已是最新

## Must-Haves

- ## Must-Haves
- UpdateService 实现 IUpdateService 全部 4 个成员（CheckForUpdatesAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart, IsUpdateUrlConfigured）
- UpdateService 在 App.xaml.cs DI 容器中注册为 Singleton
- MainWindow 底部有状态栏显示 VersionHelper.GetCurrentVersion() 版本号
- 状态栏有"检查更新"按钮，绑定到 ViewModel 的 CheckUpdateCommand
- IsUpdateUrlConfigured 为 false 时按钮灰显（CanCheckUpdate = false）
- 检查更新成功后：有新版本弹出自定义确认对话框，无新版本提示"已是最新版本"
- dotnet build 0 errors, dotnet test 162 tests pass
- ## Verification
- `dotnet build` — 0 errors
- `dotnet test` — 162 tests pass (0 failures)
- `grep -c "CheckUpdateCommand" ViewModels/MainWindowViewModel.cs` returns >= 1
- `grep -c "IUpdateService" App.xaml.cs` returns >= 1 (DI registration)
- `grep -c "StatusBar" MainWindow.xaml` returns >= 1
- `test -f Services/UpdateService.cs` returns 0
- ## Proof Level
- This slice proves: integration
- Real runtime required: no (Velopack UpdateManager needs real app install context — S04 validates end-to-end)
- Human/UAT required: no
- ## Threat Surface
- **Abuse**: UpdateUrl from appsettings.json points to internal HTTP server. If config file is tampered, could redirect to malicious server. Low risk — internal tool, config file not user-editable at runtime.
- **Data exposure**: No PII or secrets transmitted. Version check sends only current version info.
- **Input trust**: No user input reaches filesystem or database through the update path. Velopack validates package signatures.
- ## Requirement Impact
- **Requirements touched**: R027 (all tests pass)
- **Re-verify**: dotnet test full suite after all changes
- **Decisions revisited**: none
- ## Observability / Diagnostics
- Runtime signals: ILogger<UpdateService> logs update check lifecycle (check start, result found/not-found, download progress, apply-and-restart); ViewModel logs command execution
- Inspection surfaces: Log files in Logs/ directory show update check attempts and results
- Failure visibility: Error-level logs with exception details for network failures, malformed responses, Velopack errors
- Redaction constraints: UpdateUrl may contain internal server name — no secrets involved
- ## Integration Closure
- Upstream surfaces consumed: `Services/Interfaces/IUpdateService.cs` (interface contract from S01), `Program.cs` (VelopackApp initialization from S01), `appsettings.json` Update:UpdateUrl config (from S01)
- New wiring introduced in this slice: UpdateService registered in DI, IUpdateService injected into MainWindowViewModel, status bar UI bound to ViewModel
- What remains before the milestone is truly usable end-to-end: S03 (publish pipeline produces Velopack packages) and S04 (end-to-end install→update verification on clean Windows)

## Proof Level

- This slice proves: Not provided.

## Integration Closure

Not provided.

## Verification

- Not provided.

## Tasks

- [x] **T01: 实现 UpdateService 并注册到 DI 容器** `est:45m`
  创建 `Services/UpdateService.cs`，实现 `IUpdateService` 接口，封装 Velopack `UpdateManager` 的检查更新、下载更新、应用更新并重启三个核心方法。从 `IConfiguration` 读取 `Update:UpdateUrl` 配置节点，空字符串时 `IsUpdateUrlConfigured` 返回 false。在 `App.xaml.cs` 的 `BuildServiceProvider` 方法中注册为 Singleton 服务。所有异常向上传播，由 ViewModel 层处理用户提示。
  - Files: `Services/UpdateService.cs`, `Services/Interfaces/IUpdateService.cs`, `App.xaml.cs`, `appsettings.json`
  - Verify: dotnet build && dotnet test

- [x] **T02: MainWindow 底部状态栏 UI + 更新检查流程** `est:1h`
  在 `MainWindow.xaml` 底部添加状态栏，显示 `VersionHelper.GetCurrentVersion()` 版本号和"检查更新"按钮。在 `MainWindowViewModel` 中注入 `IUpdateService`，添加 `CurrentVersion` 属性、`CheckUpdateCommand` 命令、`IsCheckingUpdate`/`CanCheckUpdate` 属性。点击检查更新时异步调用 `IUpdateService.CheckForUpdatesAsync()`，根据结果显示自定义 MessageBox 对话框：有新版本→显示版本号并确认下载+重启，无新版本→显示"已是最新版本"，异常→显示错误信息。更新源未配置时 `CanCheckUpdate` 为 false，按钮灰显。
  - Files: `MainWindow.xaml`, `ViewModels/MainWindowViewModel.cs`, `Utils/VersionHelper.cs`
  - Verify: dotnet build && dotnet test

## Files Likely Touched

- Services/UpdateService.cs
- Services/Interfaces/IUpdateService.cs
- App.xaml.cs
- appsettings.json
- MainWindow.xaml
- ViewModels/MainWindowViewModel.cs
- Utils/VersionHelper.cs
