---
estimated_steps: 1
estimated_files: 11
skills_used: []
---

# T02: 移除转换器窗口、KeywordEditorUrl 和工具选项卡

删除 ConverterWindow 视图文件（Views/ConverterWindow.xaml、Views/ConverterWindow.xaml.cs）、ConverterWindowViewModel（ViewModels/ConverterWindowViewModel.cs）、ExcelToWordConverterService（Services/ExcelToWordConverterService.cs）、IExcelToWordConverter 接口（Services/Interfaces/IExcelToWordConverter.cs）。从 App.xaml.cs 移除 IExcelToWordConverter、ConverterWindowViewModel、ConverterWindow 的 DI 注册。从 MainWindowViewModel 移除 OpenConverterCommand 声明、初始化、OpenConverter 方法。从 MainWindow.xaml 移除整个“工具”TabItem（包含 JSON转Excel 转换工具 UI）。从 MainWindow.xaml.cs 移除 ConverterHyperlink_Click 事件处理器。从 appsettings.json 移除 KeywordEditorUrl 配置项。从 Configuration/AppSettings.cs 中 UISettings 类移除 KeywordEditorUrl 属性。从 MainWindow.xaml.cs 移除 KeywordEditorHyperlink_Click 事件处理器和 _uiSettings 字段（如果仅用于 KeywordEditorUrl），同时移除构造函数中 IOptions<UISettings> 参数（如果仅用于 KeywordEditorUrl——需检查是否有其他用途）及其 using 语句。

## Inputs

- `Views/ConverterWindow.xaml`
- `Views/ConverterWindow.xaml.cs`
- `ViewModels/ConverterWindowViewModel.cs`
- `Services/ExcelToWordConverterService.cs`
- `Services/Interfaces/IExcelToWordConverter.cs`
- `App.xaml.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `appsettings.json`
- `Configuration/AppSettings.cs`

## Expected Output

- `App.xaml.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `appsettings.json`
- `Configuration/AppSettings.cs`

## Verification

cd DocuFiller && grep -c "IExcelToWordConverter\|ConverterWindowViewModel\|OpenConverterCommand\|OpenConverter\|KeywordEditor" App.xaml.cs ViewModels/MainWindowViewModel.cs MainWindow.xaml.cs Configuration/AppSettings.cs appsettings.json && echo 'Should be 0'
grep -c "Header=\"工具\"" MainWindow.xaml && echo 'Should be 0 — tools tab removed'
