# 输入源增强功能设计文档

**日期**: 2025-01-07
**状态**: 设计阶段
**作者**: Claude Code

---

## 1. 概述

### 1.1 背景

当前 DocuFiller 应用程序仅支持文件夹拖拽作为模板输入方式，存在以下限制：
- 无法通过拖拽处理单个 .docx 文件
- 缺少通过对话框选择文件夹的选项
- 多层文件夹扫描功能存在但未充分利用

### 1.2 目标

扩展输入源支持，实现：
1. **单个文件拖拽** - 允许用户拖拽单个 .docx 文件到界面
2. **文件夹多层遍历** - 确保文件夹选择或拖拽时，能够递归处理所有子文件夹中的文件
3. **文件夹浏览按钮** - 添加按钮让用户可以通过对话框选择文件夹

### 1.3 范围

**包含**:
- 单个 .docx/.dotx 文件的拖拽支持
- 文件夹拖拽和对话框选择
- 递归扫描子文件夹中的模板文件
- 统一的输入处理机制

**不包含**:
- 输出目录结构的变化
- 数据文件处理方式的修改
- 核心文档处理逻辑的变更

---

## 2. 架构设计

### 2.1 总体架构

```
┌─────────────────────────────────────────────────────────────┐
│                        UI 层 (XAML)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ 浏览单文件  │  │ 浏览文件夹  │  │ 统一拖拽区域        │  │
│  │   按钮      │  │   按钮      │  │ (文件/文件夹)       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   MainWindow.xaml.cs                         │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ TemplateDropBorder_Drop() - 统一拖拽处理             │   │
│  │   ├─ 检测输入类型（文件/文件夹）                      │   │
│  │   ├─ HandleSingleFileDropAsync()                     │   │
│  │   └─ HandleFolderDropAsync()                         │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  MainWindowViewModel                         │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 属性:                                                  │   │
│  │   - InputSourceType (None/SingleFile/Folder)         │   │
│  │   - SingleFileInfo (单文件信息)                      │   │
│  │   - FolderStructure (文件夹结构)                     │   │
│  │   - IsFolderMode (兼容现有逻辑)                      │   │
│  │                                                        │   │
│  │ 命令:                                                  │   │
│  │   - BrowseTemplateFolderCommand (新增)               │   │
│  │                                                        │   │
│  │ 方法:                                                  │   │
│  │   - HandleSingleFileDropAsync() (新增)               │   │
│  │   - HandleFolderDropAsync() (修改)                   │   │
│  │   - CanStartProcess (修改)                           │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Service 层 (无需修改)                      │
│  ┌──────────────────┐  ┌────────────────────────────────┐   │
│  │ FileScannerService│  │ DocumentProcessor             │   │
│  │ - 已支持递归扫描  │  │ - ProcessDocumentsAsync       │   │
│  │ - GetFolderStructure│ │ - ProcessFolderAsync         │   │
│  │   (递归)          │  │   (已支持目录结构保持)        │   │
│  └──────────────────┘  └────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心组件

#### 2.2.1 InputSourceType 枚举

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

#### 2.2.2 数据流

**单文件处理流程**:
```
用户拖拽/选择 .docx 文件
  → InputSourceType = SingleFile
  → SingleFileInfo 被设置
  → TemplateFiles 包含单个文件
  → 用户点击"开始处理"
  → ProcessDocumentsAsync()
```

**文件夹处理流程**:
```
用户拖拽/选择文件夹
  → InputSourceType = Folder
  → FileScannerService.GetFolderStructureAsync() 递归扫描
  → FolderStructure 包含所有子文件夹
  → TemplateFiles 包含所有文件
  → 用户点击"开始处理"
  → ProcessFolderAsync()
```

---

## 3. 详细设计

### 3.1 UI 层修改 (MainWindow.xaml)

**现有布局问题**:
- 模板区域只能拖拽文件夹
- 缺少"浏览文件夹"按钮
- 提示文本仅针对文件夹

**修改方案**:

```xaml
<!-- 模板选择区域 - 修改后 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- 输入方式选择按钮 -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10">
        <Button Content="浏览单文件" Command="{Binding BrowseTemplateCommand}"
                Width="100" Height="30" Margin="0,0,10,0"/>
        <Button Content="浏览文件夹" Command="{Binding BrowseTemplateFolderCommand}"
                Width="100" Height="30"/>
    </StackPanel>

    <!-- 统一拖拽区域（支持文件和文件夹） -->
    <Border Grid.Row="1" x:Name="TemplateDropBorder"
            AllowDrop="True"
            Drop="TemplateDropBorder_Drop"
            DragEnter="TemplateDropBorder_DragEnter"
            DragLeave="TemplateDropBorder_DragLeave"
            DragOver="TemplateDropBorder_DragOver"
            BorderBrush="#BDC3C7" BorderThickness="2"
            CornerRadius="4" Height="80">
        <Grid>
            <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock x:Name="TemplateDropHint"
                           Text="拖拽单个 docx 文件或包含 docx 文件的文件夹到此处"
                           Foreground="#95A5A6"
                           FontStyle="Italic"
                           TextAlignment="Center"
                           FontSize="12"/>
                <TextBlock x:Name="TemplateInfoText"
                           Text="{Binding FoundDocxFilesCount, StringFormat='已找到: {0} 个文件'}"
                           Foreground="#2ECC71"
                           FontWeight="SemiBold"
                           FontSize="14"
                           Margin="0,5,0,0"
                           Visibility="{Binding FoundDocxFilesCount, Converter={StaticResource NullToVisibilityConverter}}"/>
            </StackPanel>
        </Grid>
    </Border>
</Grid>
```

### 3.2 事件处理层修改 (MainWindow.xaml.cs)

**3.2.1 统一的拖拽处理**

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
        RestoreBorderStyle(sender as Border);
    }
}

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
                UpdateBorderStyle(sender as Border, true);
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

/// <summary>
/// 拖拽离开事件
/// </summary>
private void TemplateDropBorder_DragLeave(object sender, DragEventArgs e)
{
    RestoreBorderStyle(sender as Border);
    UpdateHintText("拖拽单个 docx 文件或包含 docx 文件的文件夹到此处");
}

/// <summary>
/// 辅助方法 - 检查是否为 docx 文件
/// </summary>
private bool IsDocxFile(string filePath)
{
    if (string.IsNullOrEmpty(filePath))
        return false;

    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return extension == ".docx" || extension == ".dotx";
}

/// <summary>
/// 辅助方法 - 恢复边框样式
/// </summary>
private void RestoreBorderStyle(Border? border)
{
    if (border != null)
    {
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
        border.BorderThickness = new Thickness(2);
        border.Background = Brushes.Transparent;
    }
}

/// <summary>
/// 辅助方法 - 更新边框样式
/// </summary>
private void UpdateBorderStyle(Border? border, bool isActive)
{
    if (border != null && isActive)
    {
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
        border.BorderThickness = new Thickness(3);
        border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
    }
}

/// <summary>
/// 辅助方法 - 更新提示文本
/// </summary>
private void UpdateHintText(string text)
{
    if (TemplateDropHint != null)
    {
        TemplateDropHint.Text = text;
    }
}
```

**3.2.2 重命名说明**

将现有的事件名称从 `TemplateFolderDropBorder_*` 改为 `TemplateDropBorder_*`：
- `TemplateFolderDropBorder_Drop` → `TemplateDropBorder_Drop`
- `TemplateFolderDropBorder_DragEnter` → `TemplateDropBorder_DragEnter`
- `TemplateFolderDropBorder_DragLeave` → `TemplateDropBorder_DragLeave`
- `TemplateFolderDropBorder_DragOver` → `TemplateDropBorder_DragOver`

对应的 XAML 元素名称也需要更新：
- `TemplateFolderDropBorder` → `TemplateDropBorder`

### 3.3 ViewModel 层修改 (MainWindowViewModel.cs)

**3.3.1 新增属性**

```csharp
/// <summary>
/// 输入源类型
/// </summary>
private InputSourceType _inputSourceType = InputSourceType.None;
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
private Models.FileInfo? _singleFileInfo;
public Models.FileInfo? SingleFileInfo
{
    get => _singleFileInfo;
    set => SetProperty(ref _singleFileInfo, value);
}
```

**3.3.2 新增命令**

```csharp
public ICommand BrowseTemplateFolderCommand { get; private set; } = null!;

private void InitializeCommands()
{
    // ... 现有命令初始化 ...
    BrowseTemplateFolderCommand = new RelayCommand(BrowseTemplateFolder);
}

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

**3.3.3 单个文件处理方法（新增）**

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
```

**3.3.4 修改现有的文件夹处理方法**

在 `HandleFolderDropAsync` 方法末尾添加：

```csharp
public async Task HandleFolderDropAsync(string folderPath)
{
    try
    {
        // ... 现有代码 ...

        TemplateFolderPath = folderPath;
        TemplatePath = folderPath;
        FolderStructure = folderStructure;

        // 新增：设置输入源类型
        InputSourceType = InputSourceType.Folder;
        IsFolderMode = true;

        TemplateFiles.Clear();
        if (FolderStructure != null)
        {
            AddFilesToList(FolderStructure);
        }

        FoundDocxFilesCount = folderStructure.TotalDocxCount.ToString();
        ProgressMessage = $"找到 {folderStructure.TotalDocxCount} 个模板文件";

        _logger.LogInformation("文件夹扫描完成，找到 {Count} 个模板文件", folderStructure.TotalDocxCount);
    }
    catch (Exception ex)
    {
        // ... 现有错误处理 ...
    }
}
```

**3.3.5 修改 CanStartProcess 逻辑**

```csharp
public bool CanStartProcess => !IsProcessing &&
    !string.IsNullOrEmpty(DataPath) &&
    InputSourceType != InputSourceType.None &&
    ((InputSourceType == InputSourceType.SingleFile && SingleFileInfo != null) ||
     (InputSourceType == InputSourceType.Folder && FolderStructure != null && !FolderStructure.IsEmpty));
```

### 3.4 深度限制和性能考虑

**3.4.1 文件夹深度检查**

```csharp
private const int MaxFolderDepth = 10;

/// <summary>
/// 检查文件夹深度是否超过限制
/// </summary>
/// <param name="folder">文件夹结构</param>
/// <param name="currentDepth">当前深度</param>
private void CheckFolderDepth(FolderStructure folder, int currentDepth = 0)
{
    if (currentDepth > MaxFolderDepth)
    {
        _logger.LogWarning("文件夹深度超过限制: {Path}, 深度: {Depth}",
            folder.FullPath, currentDepth);
        throw new InvalidOperationException(
            $"文件夹嵌套过深（超过 {MaxFolderDepth} 层）\n" +
            $"路径: {folder.FullPath}");
    }

    foreach (var subFolder in folder.SubFolders)
    {
        CheckFolderDepth(subFolder, currentDepth + 1);
    }
}
```

在 `HandleFolderDropAsync` 中调用：

```csharp
try
{
    CheckFolderDepth(folderStructure);
}
catch (InvalidOperationException ex)
{
    MessageBox.Show(
        $"{ex.Message}\n\n建议：减少文件夹嵌套层级",
        "文件夹结构过深",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    return;
}
```

**3.4.2 大文件数量警告**

```csharp
private const int LargeFileCountThreshold = 1000;

// 在 HandleFolderDropAsync 中添加
if (folderStructure.TotalDocxCount > LargeFileCountThreshold)
{
    var result = MessageBox.Show(
        $"文件夹中包含 {folderStructure.TotalDocxCount} 个文件，处理可能需要较长时间。\n\n" +
        $"是否继续？",
        "文件数量较多",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result == MessageBoxResult.No)
    {
        ProgressMessage = "已取消";
        return;
    }
}
```

---

## 4. 错误处理和边界情况

### 4.1 错误场景处理

| 场景 | 处理方式 |
|------|---------|
| 拖拽非 .docx/.dotx 文件 | 显示警告："请拖拽 .docx 或 .dotx 格式的文件" |
| 拖拽空文件夹 | 显示提示："文件夹中没有找到 .docx 文件" |
| 拖拽包含大量文件的文件夹 | 显示警告并提供取消选项 |
| 文件被占用或只读 | 在文件信息中标记，处理时跳过并记录日志 |
| 子文件夹过深 | 抛出异常并显示友好提示 |
| 权限不足 | 捕获 UnauthorizedAccessException 并友好提示 |

### 4.2 边界情况清单

- [x] 拖拽快捷方式（.lnk）- 拒绝，提示使用真实路径
- [x] 网络路径（UNC）- 支持但需处理连接超时
- [x] 特殊字符文件名 - 确保路径处理正确
- [x] 临时文件（~$开头）- 已在 FileScannerService 中过滤
- [x] 混合文件类型 - 只识别 .docx/.dotx 文件

### 4.3 异常处理策略

```csharp
// 示例：权限不足处理
try
{
    var files = Directory.GetFiles(folderPath, "*.docx", SearchOption.AllDirectories);
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "访问文件夹权限不足: {FolderPath}", folderPath);
    MessageBox.Show(
        $"无法访问文件夹中的某些文件，可能没有足够的权限。\n\n" +
        $"路径: {folderPath}",
        "权限不足",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    return new List<Models.FileInfo>();
}
```

---

## 5. 测试策略

### 5.1 单元测试

```csharp
[TestClass]
public class MainWindowViewModelTests
{
    [TestMethod]
    public async Task HandleSingleFileDropAsync_WithValidFile_ShouldSetSingleFileMode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testFile = CreateTestDocxFile();

        // Act
        await viewModel.HandleSingleFileDropAsync(testFile);

        // Assert
        Assert.AreEqual(InputSourceType.SingleFile, viewModel.InputSourceType);
        Assert.IsNotNull(viewModel.SingleFileInfo);
        Assert.AreEqual(1, viewModel.TemplateFiles.Count);
    }

    [TestMethod]
    public async Task HandleFolderDropAsync_WithSubfolders_ShouldIncludeAllLevels()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testFolder = CreateTestFolderStructure(
            "root.docx",
            "sub1/file1.docx",
            "sub1/sub2/file2.docx",
            "sub1/sub2/sub3/file3.docx"
        );

        // Act
        await viewModel.HandleFolderDropAsync(testFolder);

        // Assert
        Assert.AreEqual(InputSourceType.Folder, viewModel.InputSourceType);
        Assert.AreEqual(4, viewModel.TemplateFiles.Count);
    }

    [TestMethod]
    public void CheckFolderDepth_ExceedsLimit_ShouldThrowException()
    {
        // Arrange
        var service = new MainWindowViewModel(...);
        var deepFolder = CreateDeepFolderStructure(15); // 超过 MaxFolderDepth

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
            service.CheckFolderDepth(deepFolder));
    }
}
```

### 5.2 集成测试场景

| 测试用例 | 输入 | 预期结果 |
|---------|------|---------|
| 单文件拖拽 | 单个 .docx 文件 | InputSourceType = SingleFile，TemplateFiles.Count = 1 |
| 平铺文件夹 | 包含 5 个 .docx 的文件夹 | 找到 5 个文件 |
| 三层嵌套 | root/sub1/sub2/*.docx | 找到所有子文件夹中的文件 |
| 空文件夹 | 不含 .docx 的文件夹 | 显示"没有找到文件"提示 |
| 混合内容 | 文件夹含 .docx、.pdf、.txt | 只识别 .docx 文件 |
| 超过深度限制 | 嵌套超过 10 层 | 显示警告提示 |
| 大文件数量 | 包含 1500 个 .docx | 显示警告并询问是否继续 |

### 5.3 UI 手动测试清单

- [ ] 拖拽单个文件到模板区域
- [ ] 拖拽文件夹到模板区域
- [ ] 点击"浏览文件夹"按钮选择文件夹
- [ ] 拖拽包含子文件夹的文件夹（3-5 层）
- [ ] 拖拽非 .docx 文件，验证错误提示
- [ ] 拖拽空文件夹，验证提示信息
- [ ] 在文件夹模式和单文件模式之间切换
- [ ] 验证进度显示和文件统计
- [ ] 测试深度限制（嵌套超过 10 层）
- [ ] 测试大文件数量警告

### 5.4 性能测试

```
测试场景：扫描包含 1000 个 .docx 文件的文件夹（分布在多个子文件夹）
预期性能：
- 扫描时间 < 5 秒
- UI 保持响应
- 内存增长合理
```

---

## 6. 实施计划

### 6.1 实施步骤

**阶段 1：模型和枚举定义**
- [ ] 创建 `Models/InputSourceType.cs` 枚举文件
- [ ] 验证编译通过

**阶段 2：ViewModel 修改**
- [ ] 在 `MainWindowViewModel.cs` 添加新属性
- [ ] 添加 `BrowseTemplateFolderCommand` 命令
- [ ] 实现 `HandleSingleFileDropAsync` 方法
- [ ] 修改 `HandleFolderDropAsync` 方法
- [ ] 更新 `CanStartProcess` 逻辑
- [ ] 添加深度检查和大文件警告

**阶段 3：事件处理修改**
- [ ] 重命名 `MainWindow.xaml.cs` 中的拖拽事件
- [ ] 实现 `TemplateDropBorder_Drop` 统一处理
- [ ] 实现 `TemplateDropBorder_DragEnter` 动态提示
- [ ] 添加辅助方法（`IsDocxFile`, `RestoreBorderStyle` 等）
- [ ] 更新 XAML 对应的事件绑定

**阶段 4：UI 更新**
- [ ] 修改 `MainWindow.xaml` 添加"浏览文件夹"按钮
- [ ] 更新拖拽区域提示文本
- [ ] 重命名 XAML 元素（`TemplateFolderDropBorder` → `TemplateDropBorder`）
- [ ] 添加文件统计显示

**阶段 5：测试验证**
- [ ] 执行手动测试清单
- [ ] 验证边界情况处理
- [ ] 性能测试
- [ ] 回归测试（确保现有功能正常）

### 6.2 影响范围

| 组件 | 修改类型 | 优先级 |
|------|---------|--------|
| `Models/InputSourceType.cs` | 新增 | 高 |
| `MainWindowViewModel.cs` | 修改 | 高 |
| `MainWindow.xaml.cs` | 修改 | 高 |
| `MainWindow.xaml` | 修改 | 中 |
| `FileScannerService.cs` | 无需修改 | - |
| `IDocumentProcessor.cs` | 无需修改 | - |

### 6.3 风险评估

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| 破坏现有文件夹拖拽功能 | 高 | 低 | 添加回归测试，逐步验证 |
| UI 复杂度增加 | 中 | 中 | 保持简洁设计，提供清晰提示 |
| 性能问题（大量文件） | 中 | 低 | 添加警告和取消选项 |
| 用户混淆（多种输入方式） | 低 | 中 | 提供清晰的提示文本 |

---

## 7. 后续优化建议

1. **进度显示优化**: 对于大文件夹扫描，显示实时进度（已扫描 X/Y 个文件）
2. **文件预览**: 在文件夹模式下，允许用户预览和取消选择特定文件
3. **最近使用记录**: 记住最近使用的文件夹路径
4. **拖拽多文件**: 支持同时拖拽多个单个文件
5. **配置选项**: 允许用户配置是否包含子文件夹、最大深度等

---

## 8. 附录

### 8.1 相关文件

- `Services/FileScannerService.cs` - 文件扫描服务（已支持递归）
- `Services/Interfaces/IFileScanner.cs` - 文件扫描服务接口
- `ViewModels/MainWindowViewModel.cs` - 主窗口 ViewModel
- `MainWindow.xaml` - 主窗口 UI
- `MainWindow.xaml.cs` - 主窗口代码后置

### 8.2 参考资料

- [WPF 拖放功能概述](https://docs.microsoft.com/zh-cn/dotnet/desktop/wpf/advanced/drag-and-drop-overview)
- [Open XML SDK 文档](https://docs.microsoft.com/zh-cn/office/open-xml/open-xml-sdk)

### 8.3 变更历史

| 日期 | 版本 | 变更说明 | 作者 |
|------|------|---------|------|
| 2025-01-07 | 1.0 | 初始设计文档 | Claude Code |

---

**文档状态**: 待审核
**下一步**: 实现阶段 1 - 创建枚举定义
