# S01: TextBox 拖放事件改为 Preview 隧道

**Goal:** 将模板 TextBox 和数据 TextBox 的拖放事件从冒泡路由（Drop/DragOver/DragEnter/DragLeave）改为 Preview 隧道路由（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave），使 TextBox 能正确接收外部文件拖放，绕过 TextBox 内置的拖放拦截。
**Demo:** 从资源管理器拖 .docx 到模板 TextBox → 蓝色高亮 → 路径填入；拖 .xlsx 到数据 TextBox → 蓝色高亮 → 路径填入 + 数据预览

## Must-Haves

- Not provided.

## Proof Level

- This slice proves: Not provided.

## Integration Closure

Not provided.

## Verification

- Not provided.

## Tasks

- [x] **T01: 将 TextBox 拖放事件改为 Preview 隧道路由** `est:20m`
  将 MainWindow.xaml 中模板 TextBox (TemplatePathTextBox) 和数据 TextBox (DataPathTextBox) 的 4 个冒泡拖放事件属性改为 Preview 隧道版本，并在 MainWindow.xaml.cs 中重命名对应的事件处理方法。清理区域 (CleanupDropZoneBorder) 的冒泡事件不变。

**XAML 改动（MainWindow.xaml）：**
- TemplatePathTextBox: `Drop=` → `PreviewDrop=`, `DragOver=` → `PreviewDragOver=`, `DragEnter=` → `PreviewDragEnter=`, `DragLeave=` → `PreviewDragLeave=`
- DataPathTextBox: 同上
- CleanupDropZoneBorder: 保持不变（Border 无内置拖放拦截）

**Code-behind 改动（MainWindow.xaml.cs）：**
- 重命名模板 TextBox 事件处理器：`TemplatePathTextBox_Drop` → `TemplatePathTextBox_PreviewDrop`, `TemplatePathTextBox_DragEnter` → `TemplatePathTextBox_PreviewDragEnter`, `TemplatePathTextBox_DragLeave` → `TemplatePathTextBox_PreviewDragLeave`, `TemplatePathTextBox_DragOver` → `TemplatePathTextBox_PreviewDragOver`
- 重命名数据 TextBox 事件处理器：`DataPathTextBox_Drop` → `DataPathTextBox_PreviewDrop`, `DataPathTextBox_DragEnter` → `DataPathTextBox_PreviewDragEnter`, `DataPathTextBox_DragLeave` → `DataPathTextBox_PreviewDragLeave`, `DataPathTextBox_DragOver` → `DataPathTextBox_PreviewDragOver`
- 清理区域事件处理器和所有其他代码不变

**约束：**
- 方法签名不变（仍是 DragEventHandler / DragEventArgs）
- e.Handled = true 保持不变，确保隧道事件阻止 TextBox 内置处理
- 不修改清理区域 (CleanupDropZoneBorder_*) 的任何事件
  - Files: `MainWindow.xaml`, `MainWindow.xaml.cs`
  - Verify: dotnet build

## Files Likely Touched

- MainWindow.xaml
- MainWindow.xaml.cs
