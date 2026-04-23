---
estimated_steps: 1
estimated_files: 8
skills_used: []
---

# T01: 移除 JSON 数据源管道（IDataParser、DataParserService、DocumentProcessorService JSON 分支、MainWindowViewModel JSON 逻辑）

删除 IDataParser 接口和 DataParserService 实现。从 DocumentProcessorService 构造函数移除 IDataParser 参数、移除 ProcessJsonDataAsync 方法、移除所有 JSON 处理分支（ProcessDocumentsAsync 和 ProcessFolderAsync 中的 else 分支）。从 MainWindowViewModel 移除 IDataParser 字段、构造函数参数、PreviewDataAsync 中的 JSON else 分支、DataFileType.Json 枚举值（保留 DataFileType.Excel），简化 DataFileTypeDisplay 属性。从 App.xaml.cs 移除 IDataParser/DataParserService DI 注册。更新 BrowseData 文件对话框过滤器移除 .json。从 MainWindow.xaml.cs 移除 JSON 文件相关的拖拽验证逻辑（IsJsonFile、IsValidJsonFile 方法，DataFileDropBorder_Drop 中的 JSON 验证分支），更新拖拽提示文本和 IsDataFile 方法仅支持 .xlsx。从 MainWindow.xaml 更新拖拽提示文本从“拖拽 JSON 或 Excel 数据文件到此处”改为仅 Excel。

## Inputs

- `Services/Interfaces/IDataParser.cs`
- `Services/DataParserService.cs`
- `Services/DocumentProcessorService.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`
- `MainWindow.xaml.cs`
- `MainWindow.xaml`
- `Exceptions/DataParsingException.cs`

## Expected Output

- `Services/DocumentProcessorService.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`
- `MainWindow.xaml.cs`
- `MainWindow.xaml`

## Verification

cd DocuFiller && grep -c "IDataParser\|DataParserService\|_dataParser\|ProcessJsonData\|DataFileType\.Json" Services/DocumentProcessorService.cs ViewModels/MainWindowViewModel.cs App.xaml.cs && echo 'Should be 0'
