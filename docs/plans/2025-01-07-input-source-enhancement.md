# 输入源增强功能实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 扩展模板输入方式，支持单个文件拖拽、文件夹多层遍历和文件夹浏览按钮

**架构:** 通过添加 InputSourceType 枚举统一处理输入源类型，修改拖拽事件处理以同时支持文件和文件夹，添加文件夹浏览命令

**技术栈:** WPF, C#/.NET 8, MVVM, OpenXML

---

## Task 1: 创建 InputSourceType 枚举

**Files:**
- Create: `Models/InputSourceType.cs`

**Step 1: 创建枚举文件**

创建 `Models/InputSourceType.cs`，内容如下：

```csharp
namespace DocuFiller.Models
{
    /// <summary>
    /// 输入源类型枚举
    /// </summary>
    public enum InputSourceType
    {
        /// <summary>
        /// 未选择
        /// </summary>
        None,

        /// <summary>
        /// 单个文件
        /// </summary>
        SingleFile,

        /// <summary>
        /// 文件夹（包含子文件夹）
        /// </summary>
        Folder
    }
}
```

**Step 2: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 3: 提交**

```bash
git add Models/InputSourceType.cs
git commit -m "feat(input): 添加 InputSourceType 枚举

定义三种输入源类型：None、SingleFile、Folder

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 2: MainWindowViewModel 添加新属性

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 添加私有字段**

在 `MainWindowViewModel` 类的私有字段区域（约第 30 行后）添加：

```csharp
// 输入源类型相关
private InputSourceType _inputSourceType = InputSourceType.None;
private Models.FileInfo? _singleFileInfo;
```

**Step 2: 添加公共属性**

在 `#region 属性` 区域末尾（约第 190 行前）添加：

```csharp
/// <summary>
/// 输入源类型
/// </summary>
public InputSourceType InputSourceType
{
    get => _inputSourceType;
    set
    {
        if (SetProperty(ref _inputSourceType, value))
        {
            OnPropertyChanged(nameof(CanStartProcess));
            OnPropertyChanged(nameof(DisplayMode));
        }
    }
}

/// <summary>
/// 显示模式（用于 UI 绑定）
/// </summary>
public string DisplayMode => InputSourceType switch
{
    InputSourceType.SingleFile => "单文件模式",
    InputSourceType.Folder => "文件夹模式（含子文件夹）",
    _ => "未选择"
};

/// <summary>
/// 单个文件信息（当选择单个文件时使用）
/// </summary>
public Models.FileInfo? SingleFileInfo
{
    get => _singleFileInfo;
    set => SetProperty(ref _singleFileInfo, value);
}
```

**Step 3: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 4: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat(viewModel): 添加输入源类型相关属性

- 添加 InputSourceType 属性
- 添加 DisplayMode 属性用于 UI 绑定
- 添加 SingleFileInfo 属性存储单文件信息

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 3: 添加文件夹浏览命令

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 声明命令**

在 `#region 命令` 区域（约第 210 行）添加：

```csharp
public ICommand BrowseTemplateFolderCommand { get; private set; } = null!;
```

**Step 2: 初始化命令**

在 `InitializeCommands()` 方法中（约第 233 行后）添加：

```csharp
BrowseTemplateFolderCommand = new RelayCommand(BrowseTemplateFolder);
```

**Step 3: 实现命令处理方法**

在 `#region 私有方法` 区域末尾（约第 488 行后）添加：

```csharp
/// <summary>
/// 浏览并选择文件夹
/// </summary>
private void BrowseTemplateFolder()
{
    var dialog = new System.Windows.Forms.FolderBrowserDialog
    {
        Description = "选择包含模板文件的文件夹",
        ShowNewFolderButton = false,
        SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
    };

    var result = dialog.ShowDialog();
    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
    {
        // 异步处理文件夹扫描
        Task.Run(async () =>
        {
            await HandleFolderDropAsync(dialog.SelectedPath);
        });
    }
}
```

**Step 4: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 5: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat(viewModel): 添加浏览文件夹命令

- 添加 BrowseTemplateFolderCommand 命令
- 实现 BrowseTemplateFolder 方法使用 FolderBrowserDialog
- 异步处理文件夹扫描避免 UI 阻塞

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 4: 实现 HandleSingleFileDropAsync 方法

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 添加 IsDocxFile 辅助方法**

在 `#region 私有方法` 区域末尾（约第 488 行后）添加：

```csharp
/// <summary>
/// 检查是否为 docx 文件
/// </summary>
/// <param name="filePath">文件路径</param>
/// <returns>是否为 docx 文件</returns>
private bool IsDocxFile(string filePath)
{
    if (string.IsNullOrEmpty(filePath))
        return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return extension == ".docx" || extension == ".dotx";
}
```

**Step 2: 实现 HandleSingleFileDropAsync 方法**

在 `#region 文件夹拖拽处理方法` 区域开头（约第 490 行后）添加：

```csharp
/// <summary>
/// 处理单个文件拖拽
/// </summary>
/// <param name="filePath">文件路径</param>
public async Task HandleSingleFileDropAsync(string filePath)
{
    try
    {
        _logger.LogInformation("开始处理单个文件拖拽: {FilePath}", filePath);
        ProgressMessage = "加载模板文件...";

        if (!IsDocxFile(filePath))
        {
            MessageBox.Show(
                "请选择 .docx 或 .dotx 格式的文件！",
                "文件格式错误",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var fileInfo = new System.IO.FileInfo(filePath);
        var docFileInfo = new Models.FileInfo
        {
            Name = fileInfo.Name,
            FullPath = fileInfo.FullName,
            Size = fileInfo.Length,
            CreationTime = fileInfo.CreationTime,
            LastModified = fileInfo.LastWriteTime,
            Extension = fileInfo.Extension,
            IsReadOnly = fileInfo.IsReadOnly,
            DirectoryPath = fileInfo.DirectoryName ?? string.Empty,
            RelativePath = fileInfo.Name,
            RelativeDirectoryPath = string.Empty
        };

        SingleFileInfo = docFileInfo;
        TemplatePath = filePath;
        TemplateFolderPath = null;
        FolderStructure = null;

        TemplateFiles.Clear();
        TemplateFiles.Add(docFileInfo);

        InputSourceType = InputSourceType.SingleFile;
        IsFolderMode = false;

        ProgressMessage = $"已加载模板: {fileInfo.Name}";
        FoundDocxFilesCount = "1";

        _logger.LogInformation("单文件加载完成: {FilePath}", filePath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理单文件拖拽时发生错误");
        ProgressMessage = "文件加载失败";
        MessageBox.Show(
            $"加载文件时发生错误：{ex.Message}",
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
```

**Step 3: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 4: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat(viewModel): 实现单个文件拖拽处理

- 添加 HandleSingleFileDropAsync 方法
- 添加 IsDocxFile 辅助方法
- 支持拖拽单个 .docx/.dotx 文件
- 设置 InputSourceType 为 SingleFile

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 5: 修改 HandleFolderDropAsync 设置 InputSourceType

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 找到 HandleFolderDropAsync 方法**

找到 `HandleFolderDropAsync` 方法（约第 496 行）

**Step 2: 在方法末尾设置 InputSourceType**

在方法末尾 `ProgressMessage = $"找到 {folderStructure.TotalDocxCount} 个模板文件";` 这行之前（约第 535 行）添加：

```csharp
// 设置输入源类型
InputSourceType = InputSourceType.Folder;
IsFolderMode = true;
```

**Step 3: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 4: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat(viewModel): 设置文件夹模式的 InputSourceType

在 HandleFolderDropAsync 中设置 InputSourceType 为 Folder

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 6: 更新 CanStartProcess 逻辑

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 找到 CanStartProcess 属性**

找到 `CanStartProcess` 属性（约第 191 行）

**Step 2: 替换属性实现**

将整个 `CanStartProcess` 属性替换为：

```csharp
public bool CanStartProcess => !IsProcessing &&
    !string.IsNullOrEmpty(DataPath) &&
    InputSourceType != InputSourceType.None &&
    ((InputSourceType == InputSourceType.SingleFile && SingleFileInfo != null) ||
     (InputSourceType == InputSourceType.Folder && FolderStructure != null && !FolderStructure.IsEmpty));
```

**Step 3: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 4: 提交**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "refactor(viewModel): 更新 CanStartProcess 逻辑

基于 InputSourceType 判断是否可以开始处理

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 7: MainWindow.xaml.cs 添加辅助方法

**Files:**
- Modify: `MainWindow.xaml.cs`

**Step 1: 找到文件末尾的 #endregion**

找到 `#endregion 文件夹拖拽事件处理` 后的结束位置（约第 365 行后）

**Step 2: 添加辅助方法**

在文件末尾的最后一个 `}` 之前添加：

```csharp
/// <summary>
/// 检查是否为 docx 文件
/// </summary>
private bool IsDocxFile(string filePath)
{
    if (string.IsNullOrEmpty(filePath))
        return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return extension == ".docx" || extension == ".dotx";
}

/// <summary>
/// 恢复边框样式
/// </summary>
private void RestoreBorderStyle(System.Windows.Controls.Border? border)
{
    if (border != null)
    {
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
        border.BorderThickness = new Thickness(2);
        border.Background = Brushes.Transparent;
    }
}

/// <summary>
/// 更新边框样式
/// </summary>
private void UpdateBorderStyle(System.Windows.Controls.Border? border, bool isActive)
{
    if (border != null && isActive)
    {
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
        border.BorderThickness = new Thickness(3);
        border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
    }
}

/// <summary>
/// 更新提示文本
/// </summary>
private void UpdateHintText(string text)
{
    if (TemplateDropHint != null)
    {
        TemplateDropHint.Text = text;
    }
}
```

**Step 3: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 4: 提交**

```bash
git add MainWindow.xaml.cs
git commit -m "feat(view): 添加拖拽辅助方法

- IsDocxFile: 检查文件类型
- RestoreBorderStyle: 恢复边框样式
- UpdateBorderStyle: 更新边框样式
- UpdateHintText: 更新提示文本

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 8: 修改拖拽事件处理统一支持文件和文件夹

**Files:**
- Modify: `MainWindow.xaml.cs`
- Modify: `MainWindow.xaml`

**Step 1: 修改 XAML 中的元素名称**

在 `MainWindow.xaml` 中找到 `TemplateFolderDropBorder` 元素（约第 86 行）：

将：
```xaml
<Border Grid.Row="1" x:Name="TemplateFolderDropBorder"
```

改为：
```xaml
<Border Grid.Row="1" x:Name="TemplateDropBorder"
```

将：
```xaml
<TextBlock x:Name="TemplateFolderDropHint"
```

改为：
```xaml
<TextBlock x:Name="TemplateDropHint"
```

**Step 2: 修改事件处理方法名称**

在 XAML 中将事件绑定更新为：
- `TemplateFolderDropBorder_Drop` → `TemplateDropBorder_Drop`
- `TemplateFolderDropBorder_DragEnter` → `TemplateDropBorder_DragEnter`
- `TemplateFolderDropBorder_DragLeave` → `TemplateDropBorder_DragLeave`
- `TemplateFolderDropBorder_DragOver` → `TemplateDropBorder_DragOver`

**Step 3: 在 MainWindow.xaml.cs 中重命名事件处理方法**

找到 `TemplateFolderDropBorder_Drop` 方法（约第 322 行），替换为：

```csharp
/// <summary>
/// 模板文件/文件夹拖拽放置事件（统一处理）
/// </summary>
private async void TemplateDropBorder_Drop(object sender, DragEventArgs e)
{
    try
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var path = files[0];

                if (DataContext is MainWindowViewModel viewModel)
                {
                    // 判断是文件还是文件夹
                    if (File.Exists(path) && IsDocxFile(path))
                    {
                        // 单个文件处理
                        await viewModel.HandleSingleFileDropAsync(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        // 文件夹处理（包含子文件夹）
                        await viewModel.HandleFolderDropAsync(path);
                    }
                    else
                    {
                        MessageBox.Show(
                            "请拖拽 .docx/.dotx 文件或包含 .docx 文件的文件夹！",
                            "文件类型错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"处理拖拽时发生错误：{ex.Message}",
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
    finally
    {
        RestoreBorderStyle(sender as System.Windows.Controls.Border);
        UpdateHintText("拖拽单个 docx 文件或包含 docx 文件的文件夹到此处");
    }
}
```

**Step 4: 替换 DragEnter 事件**

找到 `TemplateFolderDropBorder_DragEnter` 方法（约第 254 行），替换为：

```csharp
/// <summary>
/// 拖拽进入事件
/// </summary>
private void TemplateDropBorder_DragEnter(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files != null && files.Length > 0)
        {
            var path = files[0];
            bool isValid = false;
            string hintText = string.Empty;

            if (File.Exists(path) && IsDocxFile(path))
            {
                isValid = true;
                hintText = $"可处理文件: {Path.GetFileName(path)}";
            }
            else if (Directory.Exists(path))
            {
                isValid = true;
                hintText = $"可处理文件夹: {Path.GetFileName(path)} (包含子文件夹)";
            }

            if (isValid)
            {
                e.Effects = DragDropEffects.Copy;
                UpdateBorderStyle(sender as System.Windows.Controls.Border, true);
                UpdateHintText(hintText);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
    e.Handled = true;
}
```

**Step 5: 替换 DragOver 事件**

找到 `TemplateFolderDropBorder_DragOver` 方法（约第 298 行），替换为：

```csharp
/// <summary>
/// 拖拽悬停事件
/// </summary>
private void TemplateDropBorder_DragOver(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files != null && files.Length > 0)
        {
            var path = files[0];
            if ((File.Exists(path) && IsDocxFile(path)) || Directory.Exists(path))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
    else
    {
        e.Effects = DragDropEffects.None;
    }
    e.Handled = true;
}
```

**Step 6: 替换 DragLeave 事件**

找到 `TemplateFolderDropBorder_DragLeave` 方法（约第 284 行），替换为：

```csharp
/// <summary>
/// 拖拽离开事件
/// </summary>
private void TemplateDropBorder_DragLeave(object sender, DragEventArgs e)
{
    RestoreBorderStyle(sender as System.Windows.Controls.Border);
    UpdateHintText("拖拽单个 docx 文件或包含 docx 文件的文件夹到此处");
}
```

**Step 7: 更新 XAML 中的事件绑定**

确保 XAML 中的事件处理器名称与代码后置中的方法名称一致。

**Step 8: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 9: 提交**

```bash
git add MainWindow.xaml.cs MainWindow.xaml
git commit -m "feat(view): 统一拖拽处理支持文件和文件夹

- 重命名 TemplateFolderDropBorder 为 TemplateDropBorder
- 修改拖拽事件处理方法支持文件和文件夹
- 添加动态提示文本显示拖拽内容类型
- 添加辅助方法处理边框样式和提示更新

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 9: 添加浏览文件夹按钮到 UI

**Files:**
- Modify: `MainWindow.xaml`

**Step 1: 找到模板选择区域**

在 `MainWindow.xaml` 中找到模板选择区域的 `<TextBlock Text="模板选择:" ... />` （约第 67 行）

**Step 2: 添加按钮行**

在模板选择区域的顶部添加按钮行。找到模板输入的 `<Grid>` 元素，在第一行添加：

```xaml
<!-- 输入方式选择按钮 -->
<StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10" HorizontalAlignment="Left">
    <Button Content="浏览单文件" Command="{Binding BrowseTemplateCommand}"
            Width="100" Height="30" Margin="0,0,10,0"
            Style="{StaticResource ModernButtonStyle}"/>
    <Button Content="浏览文件夹" Command="{Binding BrowseTemplateFolderCommand}"
            Width="100" Height="30"
            Style="{StaticResource ModernButtonStyle}"/>
</StackPanel>
```

**Step 3: 调整现有行的 Grid.Row**

确保模板输入区域的 `<Grid>` 有两行：
```xaml
<Grid Grid.Row="1">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- 按钮行在这里 -->

    <!-- 现有的拖拽区域移到 Row="1" -->
</Grid>
```

**Step 4: 更新提示文本**

找到拖拽区域的提示文本（约第 97 行）：

将：
```xaml
<TextBlock x:Name="TemplateDropHint"
           Text="拖拽包含docx文件的文件夹到此处进行批量处理"
```

改为：
```xaml
<TextBlock x:Name="TemplateDropHint"
           Text="拖拽单个 docx 文件或包含 docx 文件的文件夹到此处"
```

**Step 5: 验证编译**

Run: `dotnet build --no-restore`
Expected: 0 errors, 0 warnings

**Step 6: 提交**

```bash
git add MainWindow.xaml
git commit -m "feat(ui): 添加浏览文件夹按钮

- 添加浏览单文件和浏览文件夹按钮
- 更新拖拽区域提示文本
- 调整布局以容纳新按钮

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 10: 验证和测试

**Files:**
- All modified files

**Step 1: 完整构建**

Run: `dotnet build`
Expected: 0 errors, 0 warnings

**Step 2: 手动测试清单**

运行应用程序并验证以下功能：

- [ ] 点击"浏览单文件"按钮，选择一个 .docx 文件，确认文件被加载
- [ ] 点击"浏览文件夹"按钮，选择包含 .docx 文件的文件夹，确认文件被扫描
- [ ] 拖拽单个 .docx 文件到模板区域，确认文件被加载
- [ ] 拖拽文件夹到模板区域，确认文件被扫描
- [ ] 拖拽包含子文件夹的文件夹（3-5 层），确认所有子文件夹中的文件都被找到
- [ ] 拖拽非 .docx 文件，验证显示错误提示
- [ ] 拖拽空文件夹，验证显示提示信息
- [ ] 在单文件模式和文件夹模式之间切换，验证 UI 正确更新
- [ ] 点击"开始处理"按钮，验证在两种模式下都能正常处理

**Step 3: 最终提交**

```bash
git add -A
git commit -m "feat(input): 完成输入源增强功能实现

实现功能：
- 支持单个 .docx/.dotx 文件拖拽
- 支持文件夹拖拽（包含子文件夹递归扫描）
- 添加浏览文件夹按钮
- 统一的拖拽处理机制
- 动态提示文本显示

测试通过：
- 单文件拖拽和浏览
- 文件夹拖拽和浏览
- 多层文件夹递归扫描
- 边界情况处理

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## 测试检查清单

完成所有任务后，验证以下测试场景：

| 场景 | 输入 | 预期结果 |
|------|------|---------|
| 单文件浏览 | 点击"浏览单文件"选择 .docx 文件 | 文件被加载，FoundDocxFilesCount = "1" |
| 文件夹浏览 | 点击"浏览文件夹"选择文件夹 | 扫描所有子文件夹，显示文件总数 |
| 单文件拖拽 | 拖拽单个 .docx 文件 | 文件被加载，InputSourceType = SingleFile |
| 文件夹拖拽 | 拖拽文件夹 | 扫描完成，InputSourceType = Folder |
| 多层嵌套 | 拖拽包含 3-5 层子文件夹的文件夹 | 找到所有子文件夹中的文件 |
| 非法文件 | 拖拽 .pdf 文件 | 显示错误提示 |
| 空文件夹 | 拖拽不包含 .docx 的文件夹 | 显示"没有找到文件"提示 |
| 模式切换 | 先拖拽文件，再拖拽文件夹 | 模式正确切换，UI 更新 |

---

## 实施完成标志

当所有任务完成后：
- [ ] 所有 10 个任务已完成
- [ ] 所有测试场景验证通过
- [ ] 代码已提交到 feature/input-source-enhancement 分支
- [ ] 构建成功（0 errors, 0 warnings）
