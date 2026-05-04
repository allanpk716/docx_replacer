---
estimated_steps: 13
estimated_files: 3
skills_used: []
---

# T02: Create FillViewModel with CT.Mvvm

从 MainWindowViewModel 提取关键词替换 Tab 全部业务逻辑到新文件 ViewModels/FillViewModel.cs。这是最大的提取任务（~700行），包含：

**属性**：TemplatePath, DataPath, OutputDirectory, FileInfoText, ProgressMessage, ProgressText, ProgressPercentage, IsProcessing, IsFolderMode, IsFolderDragOver, TemplateFolderPath, FoundDocxFilesCount, FolderStructure, InputSourceType, DisplayMode, DataFileTypeDisplay, CanStartProcess, CanCancelProcess, CanProcessFolder

**集合**：PreviewData, ContentControls, TemplateFiles

**命令**：BrowseTemplateCommand, BrowseDataCommand, BrowseOutputCommand, ValidateTemplateCommand, PreviewDataCommand, StartProcessCommand, CancelProcessCommand, SwitchToSingleModeCommand, SwitchToFolderModeCommand, ProcessFolderCommand, BrowseTemplateFolderCommand

**方法**：BrowseTemplate, BrowseData, BrowseOutput, ValidateTemplateAsync, PreviewDataAsync, StartProcessAsync, CancelProcess, HandleFolderDropAsync, HandleSingleFileDropAsync, HandleTemplateFolderChangedAsync, ProcessFolderAsync, OnProgressUpdated, SubscribeToProgressEvents, BrowseTemplateFolder, FormatFileSize, IsDocxFile

**DI 依赖**：IDocumentProcessor, IFileService, IProgressReporter, IFileScanner, IDirectoryManager, IExcelDataParser, ILogger<FillViewModel>

**CT.Mvvm 转换规则**：
- backing field `_templatePath` + 属性 → `[ObservableProperty] private string _templatePath = string.Empty;`
- `RelayCommand(BrowseTemplate)` → `[RelayCommand] private void BrowseTemplate()` (自动生成 BrowseTemplateCommand)
- CanExecute 条件：`CanStartProcess => !IsProcessing && ...` → `OnIsProcessingChanged` partial 方法中调用 `StartProcessCommand.NotifyCanExecuteChanged()`
- ExitCommand 保留在 MainWindowVM（协调器职责）
- 必须使用完全限定名 CommunityToolkit.Mvvm.ComponentModel.ObservableObject 避免冲突
- 类必须标记为 `partial class`

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/UpdateStatusViewModel.cs`
- `App.xaml.cs`

## Expected Output

- `ViewModels/FillViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.xaml.cs`

## Verification

cd C:/WorkSpace/agent/docx_replacer && dotnet build 2>&1 | Select-Object -Last 5
