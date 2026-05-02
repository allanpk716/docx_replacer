---
estimated_steps: 1
estimated_files: 1
skills_used: []
---

# T01: MainWindowViewModel 新增更新状态属性和自动检查逻辑

在 MainWindowViewModel 中新增更新状态判定属性（UpdateStatusMessage、UpdateStatusBrush、HasUpdateStatus、UpdateStatusClickCommand）和启动时自动检查更新逻辑。定义 UpdateStatus 枚举（None/PortableVersion/UpdateAvailable/UpToDate/Checking/Error）封装状态判定，构造函数末尾调用 InitializeUpdateStatusAsync 初始化常驻提示状态。

## Inputs

- `ViewModels/MainWindowViewModel.cs — 现有 ViewModel，包含 CheckUpdateAsync 方法和 IUpdateService? 依赖`
- `Services/Interfaces/IUpdateService.cs — S02 提供的 IsInstalled/CheckForUpdatesAsync/IsUpdateUrlConfigured 接口`
- `Services/UpdateService.cs — S02 实现类，构造时缓存 IsInstalled 和选择 IUpdateSource`

## Expected Output

- `ViewModels/MainWindowViewModel.cs — 新增 UpdateStatus 枚举、6 个属性、InitializeUpdateStatusAsync 方法、UpdateStatusClickCommand 命令`

## Verification

dotnet build -c Release 2>&1 | findstr /C:"error CS" /C:"Build succeeded"
