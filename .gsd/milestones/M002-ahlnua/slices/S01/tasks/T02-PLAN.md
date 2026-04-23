---
estimated_steps: 1
estimated_files: 4
skills_used: []
---

# T02: 为 MainWindow 和 CleanupWindow 添加 ILogger 注入并替换 Debug.WriteLine，迁移硬编码 URL 到配置

MainWindow.xaml.cs 有 5 处 System.Diagnostics.Debug.WriteLine 和 1 处硬编码关键词编辑器 URL。CleanupWindow.xaml.cs 有 2 处 Debug.WriteLine。需要：(1) 为两个 Window 添加 ILogger 构造函数注入；(2) 为 MainWindow 添加 IOptions<UISettings> 注入；(3) 在 UISettings 类中添加 KeywordEditorUrl 属性；(4) 在 appsettings.json 中添加配置值；(5) 将所有 Debug.WriteLine 替换为 _logger.LogDebug 调用；(6) 将硬编码 URL 替换为配置读取。注意 App.xaml.cs:71 的 Debug.WriteLine（全局异常处理退出日志）不在本次清理范围内。

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/JsonEditorViewModel.cs`
- `Configuration/AppSettings.cs`
- `appsettings.json`

## Expected Output

- `Configuration/AppSettings.cs`
- `appsettings.json`
- `MainWindow.xaml.cs`
- `DocuFiller/Views/CleanupWindow.xaml.cs`

## Verification

cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M002-ahlnua" && grep -rn "Console\.WriteLine\|System\.Diagnostics\.Debug\.WriteLine" MainWindow.xaml.cs DocuFiller/Views/CleanupWindow.xaml.cs; grep -q "KeywordEditorUrl" Configuration/AppSettings.cs && grep -q "KeywordEditorUrl" appsettings.json; echo "KeywordEditorUrl check: $?"
