---
estimated_steps: 14
estimated_files: 3
skills_used: []
---

# T01: 清理幽灵配置值和废弃的配置类

移除从未被代码引用的幽灵配置：删除 LoggingSettings、FileProcessingSettings、UISettings、AppSettings 四个类，从 PerformanceSettings 中移除 MaxConcurrentProcessing 和 ProcessingTimeout，精简 appsettings.json，清理 App.xaml.cs 中的幽灵 DI 注册。

**Steps:**
1. 编辑 `Configuration/AppSettings.cs`：删除 `AppSettings`、`LoggingSettings`、`FileProcessingSettings`、`UISettings` 四个类，只保留 `PerformanceSettings`（并移除 `MaxConcurrentProcessing` 和 `ProcessingTimeout` 两个属性）
2. 编辑 `appsettings.json`：删除 `Logging`、`FileProcessing`、`UI` 三个 section；从 `Performance` section 中删除 `MaxConcurrentProcessing` 和 `ProcessingTimeout`；只保留 `{"Performance": {"EnableTemplateCache": true, "CacheExpirationMinutes": 30}, "Update": {"UpdateUrl": "", "Channel": ""}}`
3. 编辑 `App.xaml.cs`：删除第 116-120 行的 5 行 DI 注册（`Configure<AppSettings>`、`Configure<LoggingSettings>`、`Configure<FileProcessingSettings>`、`Configure<UISettings>`），只保留 `Configure<PerformanceSettings>`
4. 检查代码中是否还有对已删除类/属性的引用（grep 验证）
5. 运行 `dotnet build` 确认 0 错误
6. 运行 `dotnet test` 确认所有测试通过

**Must-haves:**
- [ ] Configuration/AppSettings.cs 只包含 PerformanceSettings 类（2 个属性）
- [ ] appsettings.json 只有 Performance 和 Update 两个 section
- [ ] App.xaml.cs 只有一行 Configure<PerformanceSettings> DI 注册
- [ ] grep 确认 LoggingSettings/FileProcessingSettings/UISettings/AppSettings 零引用（Configuration/AppSettings.cs 定义除外）
- [ ] dotnet build 0 错误，dotnet test 全部通过

## Inputs

- `Configuration/AppSettings.cs`
- `appsettings.json`
- `App.xaml.cs`

## Expected Output

- `Configuration/AppSettings.cs`
- `appsettings.json`
- `App.xaml.cs`

## Verification

cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M020 && dotnet build 2>&1 | tail -5 && dotnet test --no-build --verbosity minimal 2>&1 | tail -8
