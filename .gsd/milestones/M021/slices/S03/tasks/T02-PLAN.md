---
estimated_steps: 5
estimated_files: 2
skills_used: []
---

# T02: Wire XAML to FileDragDrop Behavior, delete code-behind handlers

**Slice:** S03 — DragDropBehavior 提取
**Milestone:** M021

## Description

更新 MainWindow.xaml：添加 xmlns:behaviors 引用，将 TemplatePathTextBox、DataPathTextBox、CleanupDropZoneBorder 的 12 个拖放事件替换为 FileDragDrop AttachedProperty 绑定。删除 MainWindow.xaml.cs 中 13 个拖放事件处理器（~422 行）和 5 个辅助方法（~40 行），仅保留 Window_PreviewDragOver。

## Steps

1. MainWindow.xaml 添加 `xmlns:behaviors="clr-namespace:DocuFiller.Behaviors"` 到 Window 标签
2. TemplatePathTextBox：删除 AllowDrop + 4 个 Preview* 事件属性，添加 `behaviors:FileDragDrop.IsEnabled="True" behaviors:FileDragDrop.Filter="DocxOrFolder" behaviors:FileDragDrop.DropCommand="{Binding TemplateDropCommand}"`
3. DataPathTextBox：同上处理，Filter="ExcelFile"，DropCommand="{Binding DataDropCommand}"
4. CleanupDropZoneBorder：删除 AllowDrop + 4 个事件属性，添加 AttachedProperty 绑定，Filter="DocxFile"，DropCommand="{Binding DropFilesCommand}"
5. MainWindow.xaml.cs：删除 #region 数据文件拖拽事件处理（行 107-246）、#region 模板文件拖拽事件处理（行 248-387）、#region 辅助方法（行 389-429）、#region 清理功能拖拽事件处理（行 431-528）。删除 IsExcelFile/IsDataFile/IsDocxFile/RestoreBorderStyle/UpdateBorderStyle 辅助方法。保留 Window_PreviewDragOver。

## Must-Haves

- [ ] MainWindow.xaml 添加 behaviors 命名空间
- [ ] TemplatePathTextBox 使用 FileDragDrop AttachedProperty 替代 4 个 Preview* 事件
- [ ] DataPathTextBox 使用 FileDragDrop AttachedProperty 替代 4 个 Preview* 事件
- [ ] CleanupDropZoneBorder 使用 FileDragDrop AttachedProperty 替代 4 个事件
- [ ] MainWindow.xaml.cs 中 4 个 #region 块全部删除
- [ ] MainWindow.xaml.cs ≤ 130 行
- [ ] 仅保留 Window_PreviewDragOver 作为唯一拖放处理器

## Verification

- `dotnet build DocuFiller.csproj --no-restore` → 0 errors
- `dotnet test --no-restore --verbosity minimal` → all tests pass
- `wc -l MainWindow.xaml.cs` → ≤ 130
- `grep -c "private.*void.*Drag\|private.*void.*Drop" MainWindow.xaml.cs` → 1
- `grep -c "behaviors:FileDragDrop" MainWindow.xaml` → 6（3 targets × 2 properties，或 3 个 targets × IsEnabled+Filter+DropCommand = 9）

## Inputs

- `MainWindow.xaml` — 现有拖放事件绑定
- `MainWindow.xaml.cs` — 现有 13 个拖放处理器和辅助方法
- `Behaviors/FileDragDrop.cs` — T01 创建的 Behavior（AttachedProperty 名称和用法）
- `ViewModels/FillViewModel.cs` — T01 添加的 TemplateDropCommand/DataDropCommand
- `DocuFiller/ViewModels/CleanupViewModel.cs` — T01 添加的 DropFilesCommand

## Expected Output

- `MainWindow.xaml` — 事件替换为 AttachedProperty 绑定
- `MainWindow.xaml.cs` — 删除拖放代码，≤ 130 行
