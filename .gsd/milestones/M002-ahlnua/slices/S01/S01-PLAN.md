# S01: 调试日志统一和硬编码清理

**Goal:** 生产代码中所有调试日志统一使用 ILogger，关键词编辑器 URL 从 appsettings.json 配置读取，grep 扫描确认零残留
**Demo:** 生产代码中所有调试日志使用 ILogger，关键词编辑器 URL 从配置读取，grep 扫描确认零残留

## Must-Haves

- grep -rn "Console\.WriteLine" 排除 Tools/ 目录和 App.xaml.cs 后返回零结果
- grep -rn "System\.Diagnostics\.Debug\.WriteLine" 排除 Tools/ 和 App.xaml.cs 后返回零结果
- appsettings.json 的 UI 段包含 KeywordEditorUrl 配置项
- UISettings 类包含 KeywordEditorUrl 属性
- MainWindow.xaml.cs 从 IOptions<UISettings> 读取 URL 而非硬编码
- dotnet build 编译通过
- dotnet test 全部通过

## Proof Level

- This slice proves: contract — grep 正则验证代码清理结果，dotnet test 验证零回归

## Integration Closure

- Upstream surfaces consumed: 现有 ILogger<T> 注入管道，现有 IOptions<UISettings> Options pattern
- New wiring introduced: MainWindow.xaml.cs 新增 ILogger + IOptions<UISettings> 构造函数注入；CleanupWindow.xaml.cs 新增 ILogger 构造函数注入
- What remains before the milestone is truly usable end-to-end: S02 (文件夹选择对话框替换)

## Verification

- Structured ILogger calls replace raw Console.WriteLine — future agents can grep log output by log level and structured parameters
- Keyword editor URL now configurable — no need to recompile for URL changes

## Tasks

- [x] **T01: 移除 MainWindowViewModel 和 JsonEditorViewModel 中的 Console.WriteLine 调试日志** `est:30m`
  MainWindowViewModel.cs 中约 17 处 Console.WriteLine 与已存在的 _logger.LogInformation 调用完全重复，直接删除即可。JsonEditorViewModel.cs 中有 1 处 Console.WriteLine 应改为 _logger.LogDebug。清理后这两个 ViewModel 文件中不再有 Console.WriteLine 调用。
  - Files: `ViewModels/MainWindowViewModel.cs`, `ViewModels/JsonEditorViewModel.cs`
  - Verify: cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M002-ahlnua" && grep -rn "Console\.WriteLine" ViewModels/MainWindowViewModel.cs ViewModels/JsonEditorViewModel.cs; echo "Exit: $?"

- [ ] **T02: 为 MainWindow 和 CleanupWindow 添加 ILogger 注入并替换 Debug.WriteLine，迁移硬编码 URL 到配置** `est:45m`
  MainWindow.xaml.cs 有 5 处 System.Diagnostics.Debug.WriteLine 和 1 处硬编码关键词编辑器 URL。CleanupWindow.xaml.cs 有 2 处 Debug.WriteLine。需要：(1) 为两个 Window 添加 ILogger 构造函数注入；(2) 为 MainWindow 添加 IOptions<UISettings> 注入；(3) 在 UISettings 类中添加 KeywordEditorUrl 属性；(4) 在 appsettings.json 中添加配置值；(5) 将所有 Debug.WriteLine 替换为 _logger.LogDebug 调用；(6) 将硬编码 URL 替换为配置读取。注意 App.xaml.cs:71 的 Debug.WriteLine（全局异常处理退出日志）不在本次清理范围内。
  - Files: `Configuration/AppSettings.cs`, `appsettings.json`, `MainWindow.xaml.cs`, `DocuFiller/Views/CleanupWindow.xaml.cs`
  - Verify: cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M002-ahlnua" && grep -rn "Console\.WriteLine\|System\.Diagnostics\.Debug\.WriteLine" MainWindow.xaml.cs DocuFiller/Views/CleanupWindow.xaml.cs; grep -q "KeywordEditorUrl" Configuration/AppSettings.cs && grep -q "KeywordEditorUrl" appsettings.json; echo "KeywordEditorUrl check: $?"

## Files Likely Touched

- ViewModels/MainWindowViewModel.cs
- ViewModels/JsonEditorViewModel.cs
- Configuration/AppSettings.cs
- appsettings.json
- MainWindow.xaml.cs
- DocuFiller/Views/CleanupWindow.xaml.cs
