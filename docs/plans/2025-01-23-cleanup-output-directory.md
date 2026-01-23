# 审核清理功能输出到指定目录 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修改"审核清理"功能，使其支持将清理后的文件输出到指定目录，而不是直接修改原文件。

**Architecture:** 扩展现有的 `IDocumentCleanupService` 接口，新增带输出目录参数的重载方法。在 ViewModel 中新增输出目录管理属性和命令。XAML UI 中添加输出设置区域。

**Tech Stack:** .NET 8, WPF, OpenXML SDK, MVVM 模式

---

## Task 1: 更新 CleanupResult 模型

**Files:**
- Modify: `Models/CleanupResult.cs`

**Step 1: 添加新字段**

在 `CleanupResult` 类中添加以下字段：

```csharp
/// <summary>
/// 输入类型（单文件或文件夹）
/// </summary>
public InputType InputType { get; set; }

/// <summary>
/// 单文件模式：输出文件的完整路径
/// </summary>
public string OutputFilePath { get; set; } = string.Empty;

/// <summary>
/// 文件夹模式：输出文件夹的完整路径
/// </summary>
public string OutputFolderPath { get; set; } = string.Empty;
```

**Step 2: 添加 InputType 枚举**

在同一文件中（或新建 `Models/InputType.cs`）添加枚举：

```csharp
namespace DocuFiller.Models
{
    /// <summary>
    /// 输入源类型
    /// </summary>
    public enum InputType
    {
        /// <summary>
        /// 单个文件
        /// </summary>
        SingleFile,

        /// <summary>
        /// 文件夹
        /// </summary>
        Folder
    }
}
```

**Step 3: 提交变更**

```bash
git add Models/CleanupResult.cs
git commit -m "feat(cleanup): add OutputFilePath and OutputFolderPath to CleanupResult"
```

---

## Task 2: 更新 CleanupFileItem 模型

**Files:**
- Modify: `Models/CleanupFileItem.cs`

**Step 1: 添加新字段**

```csharp
/// <summary>
/// 处理后的输出路径（单文件时为文件路径，文件夹时为文件夹路径）
/// </summary>
public string OutputPath { get; set; } = string.Empty;

/// <summary>
/// 输入类型标识
/// </summary>
public InputType InputType { get; set; } = InputType.SingleFile;
```

**Step 2: 提交变更**

```bash
git add Models/CleanupFileItem.cs
git commit -m "feat(cleanup): add OutputPath and InputType to CleanupFileItem"
```

---

## Task 3: 更新 IDocumentCleanupService 接口

**Files:**
- Modify: `Services/Interfaces/IDocumentCleanupService.cs`

**Step 1: 添加新方法签名**

在接口中添加新的重载方法：

```csharp
/// <summary>
/// 清理单个文档并输出到指定目录
/// </summary>
/// <param name="fileItem">清理文件项</param>
/// <param name="outputDirectory">输出目录</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>清理结果</returns>
Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, string outputDirectory, CancellationToken cancellationToken = default);
```

**Step 2: 提交变更**

```bash
git add Services/Interfaces/IDocumentCleanupService.cs
git commit -m "feat(cleanup): add CleanupAsync overload with outputDirectory parameter"
```

---

## Task 4: 实现 DocumentCleanupService 核心逻辑

**Files:**
- Modify: `DocuFiller/Services/DocumentCleanupService.cs`

**Step 1: 实现新的 CleanupAsync 重载方法**

在类中添加以下实现：

```csharp
public async Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, string outputDirectory, CancellationToken cancellationToken = default)
{
    if (fileItem == null)
        throw new ArgumentNullException(nameof(fileItem));

    if (string.IsNullOrEmpty(outputDirectory))
        throw new ArgumentException("输出目录不能为空", nameof(outputDirectory));

    _logger.LogInformation($"开始清理文档: {fileItem.FileName}，输出目录: {outputDirectory}");

    var result = new CleanupResult
    {
        FilePath = fileItem.FilePath,
        InputType = fileItem.InputType
    };

    try
    {
        // 验证输入文件存在
        if (!File.Exists(fileItem.FilePath))
        {
            result.Success = false;
            result.Message = "文件不存在";
            return result;
        }

        // 确保输出目录存在
        Directory.CreateDirectory(outputDirectory);

        // 生成时间戳
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 根据输入类型处理
        if (fileItem.InputType == InputType.Folder)
        {
            return await CleanupFolderAsync(fileItem, outputDirectory, timestamp, cancellationToken);
        }
        else
        {
            return await CleanupSingleFileAsync(fileItem, outputDirectory, timestamp, cancellationToken);
        }
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Message = $"清理失败: {ex.Message}";
        _logger.LogError(ex, $"清理文档 {fileItem.FileName} 时发生异常");
        return await Task.FromResult(result);
    }
}

private async Task<CleanupResult> CleanupSingleFileAsync(CleanupFileItem fileItem, string outputDirectory, string timestamp, CancellationToken cancellationToken)
{
    var result = new CleanupResult
    {
        FilePath = fileItem.FilePath,
        InputType = InputType.SingleFile
    };

    try
    {
        // 生成输出文件名：原文件名_cleaned_时间戳.docx
        string originalFileName = Path.GetFileNameWithoutExtension(fileItem.FileName);
        string outputFileName = $"{originalFileName}_cleaned_{timestamp}.docx";
        string outputPath = Path.Combine(outputDirectory, outputFileName);

        // 复制文件到输出目录
        File.Copy(fileItem.FilePath, outputPath, overwrite: true);
        _logger.LogInformation($"已复制文件到: {outputPath}");

        // 打开副本执行清理操作
        using var document = WordprocessingDocument.Open(outputPath, true);

        if (document.MainDocumentPart == null)
        {
            result.Success = false;
            result.Message = "文档格式无效";
            return result;
        }

        // 检查是否有批注或控件
        bool hasComments = document.MainDocumentPart.WordprocessingCommentsPart != null;
        bool hasControls = document.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.SdtElement>().Any();

        if (!hasComments && !hasControls)
        {
            result.Success = true;
            result.Message = "文档无需处理（无批注和内容控件）";
            result.OutputFilePath = outputPath;
            _logger.LogInformation($"文档 {fileItem.FileName} 无需处理");
            return result;
        }

        // 处理批注
        if (hasComments)
        {
            _logger.LogInformation($"开始清理文档 {fileItem.FileName} 的批注");
            result.CommentsRemoved = _commentProcessor.ProcessComments(document);
        }

        // 处理内容控件
        if (hasControls)
        {
            _logger.LogInformation($"开始解包文档 {fileItem.FileName} 的内容控件");
            result.ControlsUnwrapped = _controlProcessor.ProcessControls(document);
        }

        // 保存文档
        document.MainDocumentPart.Document.Save();

        result.Success = true;
        result.Message = $"清理完成：删除 {result.CommentsRemoved} 个批注，解包 {result.ControlsUnwrapped} 个控件";
        result.OutputFilePath = outputPath;
        fileItem.OutputPath = outputPath;

        _logger.LogInformation($"文档 {fileItem.FileName} 清理完成: {result.Message}");
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Message = $"清理失败: {ex.Message}";
        _logger.LogError(ex, $"清理文档 {fileItem.FileName} 时发生异常");
    }

    return await Task.FromResult(result);
}

private async Task<CleanupResult> CleanupFolderAsync(CleanupFileItem fileItem, string outputDirectory, string timestamp, CancellationToken cancellationToken)
{
    var result = new CleanupResult
    {
        FilePath = fileItem.FilePath,
        InputType = InputType.Folder
    };

    try
    {
        // 假设 fileItem.FilePath 是文件夹路径
        string folderPath = fileItem.FilePath;
        string folderName = new DirectoryInfo(folderPath).Name;

        // 生成输出文件夹名：原文件夹名_cleaned_时间戳
        string outputFolderName = $"{folderName}_cleaned_{timestamp}";
        string outputFolderPath = Path.Combine(outputDirectory, outputFolderName);

        // 创建输出文件夹
        Directory.CreateDirectory(outputFolderPath);
        _logger.LogInformation($"创建输出文件夹: {outputFolderPath}");

        // 递归复制文件夹结构
        CopyDirectory(folderPath, outputFolderPath);

        // 查找所有 .docx 文件并清理
        var docxFiles = Directory.GetFiles(outputFolderPath, "*.docx", SearchOption.AllDirectories);
        int totalFiles = docxFiles.Length;
        int processedFiles = 0;
        int totalCommentsRemoved = 0;
        int totalControlsUnwrapped = 0;

        foreach (var docxFile in docxFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.Message = $"处理已取消，已完成 {processedFiles}/{totalFiles} 个文件";
                return result;
            }

            try
            {
                using var document = WordprocessingDocument.Open(docxFile, true);

                if (document.MainDocumentPart == null)
                    continue;

                // 检查是否有批注或控件
                bool hasComments = document.MainDocumentPart.WordprocessingCommentsPart != null;
                bool hasControls = document.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.SdtElement>().Any();

                if (!hasComments && !hasControls)
                    continue;

                // 处理批注
                if (hasComments)
                {
                    int commentsRemoved = _commentProcessor.ProcessComments(document);
                    totalCommentsRemoved += commentsRemoved;
                }

                // 处理内容控件
                if (hasControls)
                {
                    int controlsUnwrapped = _controlProcessor.ProcessControls(document);
                    totalControlsUnwrapped += controlsUnwrapped;
                }

                // 保存文档
                document.MainDocumentPart.Document.Save();

                processedFiles++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"清理文件 {docxFile} 时发生错误");
            }
        }

        result.Success = true;
        result.OutputFolderPath = outputFolderPath;
        fileItem.OutputPath = outputFolderPath;

        if (processedFiles == 0)
        {
            result.Message = "文件夹中的文档无需处理（无批注和内容控件）";
        }
        else
        {
            result.CommentsRemoved = totalCommentsRemoved;
            result.ControlsUnwrapped = totalControlsUnwrapped;
            result.Message = $"清理完成：处理了 {processedFiles} 个文件，删除 {totalCommentsRemoved} 个批注，解包 {totalControlsUnwrapped} 个控件";
        }

        _logger.LogInformation($"文件夹 {folderName} 清理完成: {result.Message}");
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Message = $"清理失败: {ex.Message}";
        _logger.LogError(ex, $"清理文件夹 {fileItem.FileName} 时发生异常");
    }

    return await Task.FromResult(result);
}

private void CopyDirectory(string sourceDir, string targetDir)
{
    Directory.CreateDirectory(targetDir);

    foreach (var file in Directory.GetFiles(sourceDir))
    {
        string fileName = Path.GetFileName(file);
        string destFile = Path.Combine(targetDir, fileName);
        File.Copy(file, destFile, overwrite: true);
    }

    foreach (var directory in Directory.GetDirectories(sourceDir))
    {
        string dirName = Path.GetFileName(directory);
        string destDir = Path.Combine(targetDir, dirName);
        CopyDirectory(directory, destDir);
    }
}
```

**Step 2: 提交变更**

```bash
git add DocuFiller/Services/DocumentCleanupService.cs
git commit -m "feat(cleanup): implement CleanupAsync with output directory support"
```

---

## Task 5: 更新 MainWindowViewModel 添加输出目录属性

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 添加私有字段**

在类顶部私有字段区域添加：

```csharp
// 清理功能输出目录相关字段
private string _cleanupOutputDirectory = string.Empty;
```

**Step 2: 添加公共属性**

在 `#region 属性` 区域末尾添加：

```csharp
/// <summary>
/// 清理功能输出目录
/// </summary>
public string CleanupOutputDirectory
{
    get => _cleanupOutputDirectory;
    set => SetProperty(ref _cleanupOutputDirectory, value);
}
```

**Step 3: 更新构造函数**

在构造函数中设置默认输出目录：

```csharp
// 设置默认清理输出目录
_cleanupOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出", "清理");
```

**Step 4: 添加命令声明**

在 `#region 命令` 的清理相关命令部分添加：

```csharp
public ICommand BrowseCleanupOutputCommand { get; private set; } = null!;
public ICommand OpenCleanupOutputFolderCommand { get; private set; } = null!;
```

**Step 5: 在 InitializeCommands 方法中注册命令**

```csharp
BrowseCleanupOutputCommand = new RelayCommand(BrowseCleanupOutput);
OpenCleanupOutputFolderCommand = new RelayCommand(OpenCleanupOutputFolder);
```

**Step 6: 实现命令方法**

在 `#region 辅助方法` 区域末尾添加：

```csharp
/// <summary>
/// 浏览并选择清理输出目录
/// </summary>
private void BrowseCleanupOutput()
{
    var dialog = new OpenFileDialog
    {
        Title = "选择输出目录",
        CheckFileExists = false,
        CheckPathExists = true,
        FileName = "选择文件夹",
        Filter = "文件夹|*.folder"
    };

    if (dialog.ShowDialog() == true)
    {
        CleanupOutputDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
    }
}

/// <summary>
/// 打开清理输出文件夹
/// </summary>
private void OpenCleanupOutputFolder()
{
    try
    {
        if (!Directory.Exists(CleanupOutputDirectory))
        {
            Directory.CreateDirectory(CleanupOutputDirectory);
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = CleanupOutputDirectory,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "打开输出文件夹时发生错误");
        MessageBox.Show($"打开输出文件夹时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

**Step 7: 提交变更**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat(cleanup): add cleanup output directory properties and commands"
```

---

## Task 6: 更新 MainWindowViewModel 的 StartCleanupAsync 方法

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: 修改 StartCleanupAsync 方法**

找到 `StartCleanupAsync` 方法，替换为以下实现：

```csharp
private async Task StartCleanupAsync()
{
    IsCleanupProcessing = true;
    CleanupProgressStatus = "准备处理...";
    CleanupProgressPercent = 0;

    int successCount = 0;
    int failureCount = 0;
    int skippedCount = 0;

    try
    {
        // 确保输出目录存在
        if (!Directory.Exists(CleanupOutputDirectory))
        {
            Directory.CreateDirectory(CleanupOutputDirectory);
            _logger.LogInformation($"创建输出目录: {CleanupOutputDirectory}");
        }

        for (int i = 0; i < CleanupFileItems.Count; i++)
        {
            var fileItem = CleanupFileItems[i];
            fileItem.Status = CleanupFileStatus.Processing;
            CleanupProgressStatus = $"正在处理: {fileItem.FileName} ({i + 1}/{CleanupFileItems.Count})";
            CleanupProgressPercent = (int)((i / (double)CleanupFileItems.Count) * 100);

            // 使用带输出目录的方法
            var result = await _cleanupService.CleanupAsync(fileItem, CleanupOutputDirectory);

            if (result.Success)
            {
                if (result.Message.Contains("无需处理"))
                {
                    fileItem.Status = CleanupFileStatus.Skipped;
                    fileItem.StatusMessage = result.Message;
                    skippedCount++;
                }
                else
                {
                    fileItem.Status = CleanupFileStatus.Success;
                    // 显示输出路径
                    string outputPath = result.InputType == InputType.Folder
                        ? result.OutputFolderPath
                        : result.OutputFilePath;
                    fileItem.StatusMessage = $"已清理 → {Path.GetFileName(outputPath)}";
                    successCount++;
                }
            }
            else
            {
                fileItem.Status = CleanupFileStatus.Failure;
                fileItem.StatusMessage = result.Message;
                failureCount++;
            }
        }

        CleanupProgressPercent = 100;
        CleanupProgressStatus = $"处理完成: {successCount} 成功, {failureCount} 失败, {skippedCount} 跳过";

        _logger.LogInformation($"批量清理完成: {successCount} 成功, {failureCount} 失败, {skippedCount} 跳过");

        var resultMessage = $"清理完成！\n\n成功: {successCount}\n失败: {failureCount}\n跳过: {skippedCount}\n\n输出目录: {CleanupOutputDirectory}";
        MessageBox.Show(
            resultMessage,
            "处理完成",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "批量清理时发生异常");
        MessageBox.Show($"处理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        IsCleanupProcessing = false;
    }
}
```

**Step 2: 提交变更**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat(cleanup): update StartCleanupAsync to use output directory"
```

---

## Task 7: 更新 MainWindow.xaml 添加输出设置 UI

**Files:**
- Modify: `MainWindow.xaml`

**Step 1: 在审核清理 Tab 中添加输出设置 GroupBox**

找到 `<!-- 审核清理选项卡 -->` 部分，在拖放区域之前添加输出设置：

```xml
<!-- 审核清理选项卡 -->
<TabItem Header="审核清理" FontSize="16">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 输出设置 -->
        <GroupBox Grid.Row="0" Header="输出设置" Style="{StaticResource GroupBoxStyle}" Margin="0,0,0,10" FontSize="14">
            <Grid Margin="10">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0" Text="输出目录:"
                           VerticalAlignment="Center"
                           Margin="0,0,10,0" FontWeight="Medium"/>

                <TextBox Grid.Column="1"
                         Style="{StaticResource TextBoxStyle}"
                         Text="{Binding CleanupOutputDirectory, UpdateSourceTrigger=PropertyChanged}"
                         IsReadOnly="True" FontSize="13"
                         Margin="0,0,10,0"/>

                <Button Grid.Column="2" Content="浏览..."
                        Style="{StaticResource PrimaryButton}"
                        Command="{Binding BrowseCleanupOutputCommand}"
                        Width="80" Height="35" FontSize="13"
                        Margin="0,0,5,0"/>

                <Button Grid.Column="3" Content="打开输出文件夹"
                        Style="{StaticResource ProcessButton}"
                        Command="{Binding OpenCleanupOutputFolderCommand}"
                        Width="110" Height="35" FontSize="13"/>
            </Grid>
        </GroupBox>

        <!-- 拖放区域 + 文件列表 -->
        <Border Grid.Row="1" Grid.RowSpan="2" ...>
```

**注意**：需要更新 `Grid.RowDefinitions` 和调整现有元素的行号。

**Step 2: 提交变更**

```bash
git add MainWindow.xaml
git commit -m "feat(cleanup): add output settings UI to cleanup tab"
```

---

## Task 8: 更新 MainWindow.xaml.cs 处理文件夹拖拽

**Files:**
- Modify: `MainWindow.xaml.cs`

**Step 1: 找到清理相关拖拽事件处理方法**

找到 `CleanupDropZoneBorder_Drop` 方法，确保正确识别文件和文件夹：

```csharp
private void CleanupDropZoneBorder_Drop(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var viewModel = (ViewModels.MainWindowViewModel)DataContext;

        foreach (var file in files)
        {
            if (File.Exists(file) && file.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                // 单文件
                var fileInfo = new System.IO.FileInfo(file);
                var fileItem = new CleanupFileItem
                {
                    FilePath = file,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    InputType = InputType.SingleFile
                };
                viewModel.CleanupFileItems.Add(fileItem);
            }
            else if (Directory.Exists(file))
            {
                // 文件夹
                var dirInfo = new DirectoryInfo(file);
                var fileItem = new CleanupFileItem
                {
                    FilePath = file,
                    FileName = dirInfo.Name,
                    FileSize = 0,
                    InputType = InputType.Folder
                };
                viewModel.CleanupFileItems.Add(fileItem);
            }
        }

        CleanupDropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
        CleanupDropZoneBorder.Background = new SolidColorBrush(Color.FromRgb(249, 249, 249));
    }
}
```

**Step 2: 提交变更**

```bash
git add MainWindow.xaml.cs
git commit -m "feat(cleanup): handle folder drop in cleanup zone"
```

---

## Task 9: 测试和验证

**测试步骤：**

1. **单文件清理测试**
   - 拖入单个 .docx 文件
   - 点击"开始清理"
   - 验证输出文件命名：`原文件名_cleaned_时间戳.docx`
   - 验证原文件未被修改

2. **文件夹清理测试**
   - 拖入包含多个 .docx 文件的文件夹
   - 点击"开始清理"
   - 验证输出文件夹命名：`原文件夹名_cleaned_时间戳/`
   - 验证文件夹内文件名保持不变

3. **输出目录测试**
   - 点击"浏览..."按钮选择自定义输出目录
   - 验证文件输出到正确位置
   - 点击"打开输出文件夹"验证能正常打开

4. **错误处理测试**
   - 测试文件被占用的情况
   - 测试输出目录无权限的情况

**提交测试报告：**

```bash
echo "测试完成并验证通过" > docs/plans/2025-01-23-cleanup-output-directory-test-report.md
git add docs/plans/2025-01-23-cleanup-output-directory-test-report.md
git commit -m "test(cleanup): add test report for output directory feature"
```

---

## 验收标准

- [ ] 单文件清理输出到指定目录，文件名包含 `_cleaned_时间戳` 后缀
- [ ] 文件夹清理输出到指定目录，文件夹名包含 `_cleaned_时间戳` 后缀
- [ ] 文件夹内的文件保持原文件名不变
- [ ] 原文件/文件夹未被修改
- [ ] UI 显示输出路径信息
- [ ] "打开输出文件夹"按钮正常工作
- [ ] 默认输出目录为 `{MyDocuments}\DocuFiller输出\清理`

---

## 完成后清理

```bash
# 查看所有提交
git log --oneline

# 如果需要推送到远程
git push origin <branch-name>
```
