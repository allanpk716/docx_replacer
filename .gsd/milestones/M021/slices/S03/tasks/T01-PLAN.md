---
estimated_steps: 8
estimated_files: 3
skills_used: []
---

# T01: Create FileDragDrop Behavior + ViewModel DropCommands

**Slice:** S03 — DragDropBehavior 提取
**Milestone:** M021

## Description

创建 `Behaviors/FileDragDrop.cs` AttachedProperty 类，封装文件拖放的完整逻辑：文件类型验证、视觉效果反馈、Drop 回调。同时在 FillViewModel 添加 TemplateDropCommand/DataDropCommand，CleanupViewModel 添加 DropFilesCommand。

## Steps

1. 创建 `Behaviors/FileDragDrop.cs`，定义 `FileFilter` 枚举（DocxOrFolder / ExcelFile / DocxFile）和 AttachedProperty 类
2. 实现 `FileDragDrop.IsEnabled` 附加属性，在 PropertyChangedCallback 中注册/注销事件处理器
3. 实现 `FileDragDrop.Filter` 附加属性（FileFilter 枚举类型）
4. 实现 `FileDragDrop.DropCommand` 附加属性（ICommand 类型）
5. 实现事件注册逻辑：检测目标是否为 TextBox → 使用 Preview 事件；否则使用冒泡事件
6. 实现 DragEnter/DragOver/DragLeave/Drop 四个事件处理器，包含文件验证和视觉反馈
7. 在 FillViewModel 添加 `[RelayCommand] private async Task TemplateDrop(string path)` — 判断文件/文件夹，调用已有的 HandleSingleFileDropAsync 或 HandleFolderDropAsync
8. 在 FillViewModel 添加 `[RelayCommand] private void DataDrop(string path)` — 设置 DataPath，调用 PreviewDataCommand
9. 在 CleanupViewModel 添加 `[RelayCommand] private void DropFiles(string[] paths)` — 遍历路径，.docx 文件调 AddFiles，文件夹调 AddFolder

## Must-Haves

- [ ] FileDragDrop.cs 实现 IsEnabled/Filter/DropCommand 三个附加属性
- [ ] FileFilter 枚举支持 DocxOrFolder、ExcelFile、DocxFile
- [ ] TextBox 目标使用 Preview 隧道事件（PreviewDragEnter/PreviewDragLeave/PreviewDragOver/PreviewDrop）
- [ ] 非 TextBox 目标使用冒泡事件（DragEnter/DragLeave/DragOver/Drop）
- [ ] 文件验证逻辑与现有 MainWindow.xaml.cs 一致（DocxOrFolder: .docx/.dotx 文件或文件夹；ExcelFile: 单个 .xlsx；DocxFile: .docx 文件或文件夹）
- [ ] 拖入高亮：TextBox → BorderBrush=#2196F3, Thickness=2, Background=20% #2196F3；Border → BorderBrush=#2196F3, Thickness=3, Background=20% #2196F3
- [ ] 拖出/放下恢复：TextBox → BorderBrush=#BDC3C7, Thickness=1, Background=White；Border → BorderBrush=#CCCCCC, Thickness=2, Background=#F9F9F9
- [ ] FillViewModel.TemplateDropCommand 调用已有 HandleSingleFileDropAsync/HandleFolderDropAsync
- [ ] FillViewModel.DataDropCommand 设置 DataPath 并触发 PreviewDataCommand
- [ ] CleanupViewModel.DropFilesCommand 调用已有 AddFiles/AddFolder

## Verification

- `dotnet build DocuFiller.csproj --no-restore` 返回 0 errors
- `grep -c "FileFilter\|DropCommand\|IsEnabled" Behaviors/FileDragDrop.cs` ≥ 3（三个附加属性存在）
- `grep -c "TemplateDropCommand\|DataDropCommand" ViewModels/FillViewModel.cs` ≥ 2
- `grep -c "DropFilesCommand" DocuFiller/ViewModels/CleanupViewModel.cs` ≥ 1

## Inputs

- `ViewModels/FillViewModel.cs` — 现有 HandleSingleFileDropAsync/HandleFolderDropAsync 方法和 DataPath 属性
- `DocuFiller/ViewModels/CleanupViewModel.cs` — 现有 AddFiles/AddFolder 方法
- `MainWindow.xaml.cs` — 参考现有拖放逻辑和视觉反馈参数

## Expected Output

- `Behaviors/FileDragDrop.cs` — 新建 AttachedProperty Behavior
- `ViewModels/FillViewModel.cs` — 新增 TemplateDropCommand 和 DataDropCommand
- `DocuFiller/ViewModels/CleanupViewModel.cs` — 新增 DropFilesCommand
