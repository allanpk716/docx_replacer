using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 关键词替换 Tab 的 ViewModel，管理模板/数据选择、文件拖放、处理进度等全部业务逻辑。
    /// 使用 CT.Mvvm [ObservableProperty] + [RelayCommand] 模式。
    /// </summary>
    public partial class FillViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly IFileScanner _fileScanner;
        private readonly IDirectoryManager _directoryManager;
        private readonly IExcelDataParser _excelDataParser;
        private readonly ILogger<FillViewModel> _logger;
        private CancellationTokenSource? _cancellationTokenSource;

        #region Backing Fields — CT.Mvvm [ObservableProperty]

        [ObservableProperty] private string _templatePath = string.Empty;
        [ObservableProperty] private string _dataPath = string.Empty;
        [ObservableProperty] private string _outputDirectory = string.Empty;
        [ObservableProperty] private string _fileInfoText = "请选择模板文件和数据文件";
        [ObservableProperty] private string _progressMessage = "就绪";
        [ObservableProperty] private string _progressText = "0%";
        [ObservableProperty] private double _progressPercentage;
        [ObservableProperty] private bool _isProcessing;
        [ObservableProperty] private bool _isFolderMode;
        [ObservableProperty] private bool _isFolderDragOver;
        [ObservableProperty] private string? _templateFolderPath;
        [ObservableProperty] private string? _foundDocxFilesCount;
        [ObservableProperty] private FolderStructure? _folderStructure;
        [ObservableProperty] private InputSourceType _inputSourceType = InputSourceType.None;
        [ObservableProperty] private DataFileType _dataFileType = DataFileType.Excel;
        [ObservableProperty] private DataStatistics _dataStatistics = new();
        [ObservableProperty] private Models.FileInfo? _singleFileInfo;

        #endregion

        #region Collections

        public ObservableCollection<Dictionary<string, object>> PreviewData { get; } = new();
        public ObservableCollection<ContentControlData> ContentControls { get; } = new();
        public ObservableCollection<Models.FileInfo> TemplateFiles { get; } = new();

        #endregion

        #region Constructor

        public FillViewModel(
            IDocumentProcessor documentProcessor,
            IFileService fileService,
            IProgressReporter progressReporter,
            IFileScanner fileScanner,
            IDirectoryManager directoryManager,
            IExcelDataParser excelDataParser,
            ILogger<FillViewModel> logger)
        {
            _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _fileScanner = fileScanner ?? throw new ArgumentNullException(nameof(fileScanner));
            _directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            _excelDataParser = excelDataParser ?? throw new ArgumentNullException(nameof(excelDataParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeToProgressEvents();

            // 设置默认输出目录
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出");
        }

        #endregion

        #region 派生属性

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
        /// 数据文件类型显示文本
        /// </summary>
        public string DataFileTypeDisplay => "Excel (支持格式)";

        /// <summary>
        /// 是否可以开始处理
        /// </summary>
        public bool CanStartProcess => !IsProcessing &&
            !string.IsNullOrEmpty(DataPath) &&
            InputSourceType != InputSourceType.None &&
            ((InputSourceType == InputSourceType.SingleFile && SingleFileInfo != null) ||
             (InputSourceType == InputSourceType.Folder && FolderStructure != null && !FolderStructure.IsEmpty));

        /// <summary>
        /// 是否可以取消处理
        /// </summary>
        public bool CanCancelProcess => IsProcessing;

        /// <summary>
        /// 是否可以处理文件夹
        /// </summary>
        public bool CanProcessFolder => !IsProcessing && IsFolderMode && FolderStructure != null && !FolderStructure.IsEmpty && !string.IsNullOrEmpty(DataPath);

        #endregion

        #region 属性变更副作用

        /// <summary>
        /// TemplatePath 变更时更新文件信息和命令状态
        /// </summary>
        partial void OnTemplatePathChanged(string value)
        {
            UpdateFileInfo();
            OnPropertyChanged(nameof(CanStartProcess));
            StartProcessCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// DataPath 变更时更新文件信息和命令状态
        /// </summary>
        partial void OnDataPathChanged(string value)
        {
            UpdateFileInfo();
            OnPropertyChanged(nameof(CanStartProcess));
            StartProcessCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// IsProcessing 变更时通知相关属性和命令
        /// </summary>
        partial void OnIsProcessingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanStartProcess));
            OnPropertyChanged(nameof(CanCancelProcess));
            StartProcessCommand.NotifyCanExecuteChanged();
            CancelProcessCommand.NotifyCanExecuteChanged();
            ProcessFolderCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// InputSourceType 变更时通知相关属性和命令
        /// </summary>
        partial void OnInputSourceTypeChanged(InputSourceType value)
        {
            OnPropertyChanged(nameof(CanStartProcess));
            OnPropertyChanged(nameof(DisplayMode));
            StartProcessCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// FolderStructure 变更时通知相关命令
        /// </summary>
        partial void OnFolderStructureChanged(FolderStructure? value)
        {
            OnPropertyChanged(nameof(CanStartProcess));
            OnPropertyChanged(nameof(CanProcessFolder));
            StartProcessCommand.NotifyCanExecuteChanged();
            ProcessFolderCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// SingleFileInfo 变更时通知相关命令
        /// </summary>
        partial void OnSingleFileInfoChanged(Models.FileInfo? value)
        {
            OnPropertyChanged(nameof(CanStartProcess));
            StartProcessCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// IsFolderMode 变更时通知相关命令
        /// </summary>
        partial void OnIsFolderModeChanged(bool value)
        {
            OnPropertyChanged(nameof(CanProcessFolder));
            ProcessFolderCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region 命令

        [RelayCommand]
        private void BrowseTemplate()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择Word模板文件",
                Filter = "Word文档 (*.docx)|*.docx|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                TemplatePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseData()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择数据文件",
                Filter = "Excel 文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                DataPath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseOutput()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择输出目录"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FolderName;
                if (string.IsNullOrEmpty(selectedPath))
                {
                    _logger.LogDebug("BrowseOutput: 用户未选择文件夹");
                    return;
                }

                OutputDirectory = selectedPath;
                _logger.LogInformation("输出目录已选择: {Path}", selectedPath);
            }
        }

        [RelayCommand(CanExecute = nameof(CanBrowseValidate))]
        private async Task ValidateTemplateAsync()
        {
            try
            {
                ProgressMessage = "验证模板文件...";

                var result = await _documentProcessor.ValidateTemplateAsync(TemplatePath);
                if (result.IsValid)
                {
                    var controls = await _documentProcessor.GetContentControlsAsync(TemplatePath);

                    ContentControls.Clear();
                    foreach (var control in controls)
                    {
                        ContentControls.Add(control);
                    }

                    ProgressMessage = $"模板验证成功，找到 {controls.Count} 个内容控件";
                    MessageBox.Show($"模板验证成功！\n找到 {controls.Count} 个内容控件", "验证结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ProgressMessage = "模板验证失败";
                    MessageBox.Show($"模板验证失败：\n{string.Join("\n", result.Errors)}", "验证结果", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证模板时发生错误");
                ProgressMessage = "验证失败";
                MessageBox.Show($"验证模板时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanBrowseValidate() => !string.IsNullOrEmpty(TemplatePath);

        [RelayCommand(CanExecute = nameof(CanBrowsePreview))]
        private async Task PreviewDataAsync()
        {
            try
            {
                ProgressMessage = "加载数据预览...";

                var preview = await _excelDataParser.GetDataPreviewAsync(DataPath, 10);

                PreviewData.Clear();
                foreach (var item in preview)
                {
                    var convertedItem = item.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.PlainText);
                    PreviewData.Add(convertedItem);
                }

                var summary = await _excelDataParser.GetDataStatisticsAsync(DataPath);
                var fileInfo = new System.IO.FileInfo(DataPath);
                DataStatistics = new DataStatistics
                {
                    TotalRecords = summary.ValidKeywordRows,
                    Fields = preview.SelectMany(d => d.Keys).Distinct().ToList(),
                    FileSizeBytes = fileInfo.Length
                };

                ProgressMessage = $"Excel 数据加载完成，共 {summary.ValidKeywordRows} 条记录";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览数据时发生错误");
                ProgressMessage = "数据加载失败";
                MessageBox.Show($"预览数据时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanBrowsePreview() => !string.IsNullOrEmpty(DataPath);

        [RelayCommand(CanExecute = nameof(CanStartProcess))]
        private async Task StartProcessAsync()
        {
            try
            {
                _logger.LogInformation("[调试] 开始处理文档");
                _logger.LogInformation("[调试] 模板路径: '{TemplatePath}'", TemplatePath);
                _logger.LogInformation("[调试] 数据路径: '{DataPath}'", DataPath);
                _logger.LogInformation("[调试] 输出目录: '{OutputDirectory}'", OutputDirectory);
                _logger.LogInformation("[调试] 是否为文件夹模式: {IsFolderMode}", IsFolderMode);

                // 检查处理模式
                if (IsFolderMode)
                {
                    _logger.LogInformation("[调试] 进入文件夹模式处理");
                    await ProcessFolderAsync();
                    return;
                }

                _logger.LogInformation("[调试] 进入单文件模式处理");

                IsProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var request = new ProcessRequest
                {
                    TemplateFilePath = TemplatePath,
                    DataFilePath = DataPath,
                    OutputDirectory = OutputDirectory,
                    OutputFileNamePattern = "{timestamp}"
                };

                var result = await Task.Run(async () =>
                {
                    return await _documentProcessor.ProcessDocumentsAsync(request, _cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);

                if (result.IsSuccess)
                {
                    MessageBox.Show($"处理完成！\n生成了 {result.SuccessfulRecords} 个文件\n耗时：{result.Duration.TotalSeconds:F1} 秒",
                        "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errorMessages = new List<string>();

                    if (result.Errors != null && result.Errors.Any())
                    {
                        errorMessages.AddRange(result.Errors);
                    }

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (!errorMessages.Contains(result.ErrorMessage))
                        {
                            errorMessages.Add(result.ErrorMessage);
                        }
                    }

                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        if (!errorMessages.Contains(result.Message))
                        {
                            errorMessages.Add(result.Message);
                        }
                    }

                    var errorText = errorMessages.Any() ? string.Join("\n", errorMessages) : "未知错误";
                    MessageBox.Show($"处理失败：\n{errorText}",
                        "处理失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                ProgressMessage = "处理已取消";
                MessageBox.Show("处理已取消", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文档时发生错误");
                MessageBox.Show($"处理文档时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand(CanExecute = nameof(CanCancelProcess))]
        private void CancelProcess()
        {
            _cancellationTokenSource?.Cancel();
        }

        [RelayCommand]
        private void SwitchToSingleMode() => IsFolderMode = false;

        [RelayCommand]
        private void SwitchToFolderMode() => IsFolderMode = true;

        [RelayCommand(CanExecute = nameof(CanProcessFolder))]
        private async Task ProcessFolderAsync()
        {
            try
            {
                IsProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var templateFiles = new List<Models.FileInfo>();
                if (FolderStructure != null)
                {
                    ExtractTemplateFiles(FolderStructure, templateFiles);
                }

                _logger.LogDebug("ProcessFolderAsync - 提取到的模板文件数量: {Count}", templateFiles.Count);
                foreach (var file in templateFiles)
                {
                    _logger.LogDebug("模板文件: {FilePath}", file.FullPath);
                }

                var request = new FolderProcessRequest
                {
                    TemplateFolderPath = TemplateFolderPath ?? string.Empty,
                    DataFilePath = DataPath,
                    OutputDirectory = OutputDirectory,
                    PreserveDirectoryStructure = true,
                    TemplateFiles = templateFiles
                };

                _logger.LogDebug("FolderProcessRequest - TemplateFolderPath: {Path}, DataFilePath: {DataPath}, TemplateFiles.Count: {Count}",
                    request.TemplateFolderPath, request.DataFilePath, request.TemplateFiles?.Count ?? 0);

                ProgressMessage = "开始批量处理文件夹...";

                var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

                var result = await Task.Run(async () =>
                {
                    return await _documentProcessor.ProcessFolderAsync(request, cancellationToken);
                }, cancellationToken);

                if (result.IsSuccess)
                {
                    MessageBox.Show($"批量处理完成！\n处理了 {result.TotalProcessed} 个文件\n生成了 {result.GeneratedFiles.Count} 个文件\n耗时：{result.Duration.TotalSeconds:F1} 秒",
                        "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errorMessages = new List<string>();

                    if (result.Errors != null && result.Errors.Any())
                    {
                        errorMessages.AddRange(result.Errors);
                    }

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (!errorMessages.Contains(result.ErrorMessage))
                        {
                            errorMessages.Add(result.ErrorMessage);
                        }
                    }

                    if (result.FailedFiles != null && result.FailedFiles.Any())
                    {
                        errorMessages.Add("失败的文件:");
                        errorMessages.AddRange(result.FailedFiles.Take(10));
                        if (result.FailedFiles.Count > 10)
                        {
                            errorMessages.Add($"... 还有 {result.FailedFiles.Count - 10} 个文件失败");
                        }
                    }

                    var errorText = errorMessages.Any() ? string.Join("\n", errorMessages) : "未知错误";
                    MessageBox.Show($"批量处理失败：\n{errorText}",
                        "处理失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                ProgressMessage = "批量处理已取消";
                MessageBox.Show("批量处理已取消", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量处理文件夹时发生错误");
                MessageBox.Show($"批量处理时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void BrowseTemplateFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择包含模板文件的文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FolderName;
                if (string.IsNullOrEmpty(selectedPath))
                {
                    _logger.LogDebug("BrowseTemplateFolder: 用户未选择文件夹");
                    return;
                }

                _logger.LogInformation("模板文件夹已选择: {Path}", selectedPath);

                _ = HandleFolderDropAsync(selectedPath);
            }
        }

        /// <summary>
        /// 模板拖放命令：判断路径是文件还是文件夹，调用对应的处理方法。
        /// 由 FileDragDrop Behavior 通过 DropCommand 附加属性调用。
        /// </summary>
        [RelayCommand]
        private async Task TemplateDrop(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            var path = paths[0];

            if (File.Exists(path) && IsDocxFile(path))
            {
                await HandleSingleFileDropAsync(path);
            }
            else if (Directory.Exists(path))
            {
                await HandleFolderDropAsync(path);
            }
            else
            {
                MessageBox.Show(
                    "请拖拽 .docx/.dotx 文件或包含 .docx 文件的文件夹！",
                    "文件类型错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 数据文件拖放命令：设置 DataPath 并触发数据预览。
        /// 由 FileDragDrop Behavior 通过 DropCommand 附加属性调用。
        /// </summary>
        [RelayCommand]
        private void DataDrop(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            var path = paths[0];

            if (!IsExcelFile(path))
            {
                MessageBox.Show("请拖拽 Excel (.xlsx) 文件！", "文件类型错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _logger.LogDebug("数据文件拖放 - 设置DataPath: {FilePath}", path);
            DataPath = path;
            _logger.LogDebug("数据文件拖放 - DataPath已设置为: {DataPath}", DataPath);
            PreviewDataCommand.Execute(null);

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 检查是否为 Excel 文件
        /// </summary>
        private static bool IsExcelFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            return Path.GetExtension(filePath).ToLowerInvariant() == ".xlsx";
        }

        #endregion

        #region 进度事件

        private void SubscribeToProgressEvents()
        {
            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }

        private void OnProgressUpdated(object? sender, ProgressEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ProgressPercentage = e.ProgressPercentage;
                ProgressText = $"{e.ProgressPercentage:F1}%";
                ProgressMessage = e.StatusMessage;

                if (e.IsCompleted)
                {
                    IsProcessing = false;
                    ProgressMessage = e.HasError ? "处理失败" : "处理完成";
                }
            });
        }

        #endregion

        #region 文件夹拖拽处理

        /// <summary>
        /// 处理单个文件拖拽
        /// </summary>
        public Task HandleSingleFileDropAsync(string filePath)
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
                    return Task.CompletedTask;
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
            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理文件夹拖拽
        /// </summary>
        public async Task HandleFolderDropAsync(string folderPath)
        {
            try
            {
                _logger.LogInformation("开始处理文件夹拖拽: {FolderPath}", folderPath);

                if (!_fileScanner.IsValidFolder(folderPath))
                {
                    _logger.LogWarning("无效的文件夹路径: {FolderPath}", folderPath);
                    MessageBox.Show("无效的文件夹路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ProgressMessage = "扫描文件夹中的模板文件...";

                var folderStructure = await _fileScanner.GetFolderStructureAsync(folderPath);

                if (folderStructure.IsEmpty)
                {
                    MessageBox.Show("文件夹中没有找到 .docx 文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TemplateFolderPath = folderPath;
                TemplatePath = folderPath;
                FolderStructure = folderStructure;
                IsFolderMode = true;

                FoundDocxFilesCount = folderStructure.TotalDocxCount.ToString();

                UpdateTemplateFilesList();

                InputSourceType = InputSourceType.Folder;
                IsFolderMode = true;

                ProgressMessage = $"找到 {folderStructure.TotalDocxCount} 个模板文件";

                _logger.LogInformation("文件夹扫描完成，找到 {Count} 个模板文件", folderStructure.TotalDocxCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文件夹拖拽时发生错误");
                ProgressMessage = "文件夹扫描失败";
                MessageBox.Show($"处理文件夹时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 模板文件夹路径改变时的处理
        /// </summary>
        public async Task HandleTemplateFolderChangedAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(TemplateFolderPath))
                {
                    await HandleFolderDropAsync(TemplateFolderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理模板文件夹变更时发生错误");
                ProgressMessage = "加载模板文件夹失败";
            }
        }

        #endregion

        #region 私有辅助方法

        private void UpdateFileInfo()
        {
            var info = new List<string>();

            if (!string.IsNullOrEmpty(TemplatePath))
            {
                if (_fileService.FileExists(TemplatePath))
                {
                    var fileSize = _fileService.GetFileSize(TemplatePath);
                    var lastModified = _fileService.GetLastWriteTime(TemplatePath);
                    info.Add($"模板文件: {Path.GetFileName(TemplatePath)}");
                    info.Add($"文件大小: {FormatFileSize(fileSize)}");
                    info.Add($"修改时间: {lastModified:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    info.Add("模板文件: 文件不存在");
                }
            }

            if (!string.IsNullOrEmpty(DataPath))
            {
                if (_fileService.FileExists(DataPath))
                {
                    var fileSize = _fileService.GetFileSize(DataPath);
                    var lastModified = _fileService.GetLastWriteTime(DataPath);
                    info.Add($"数据文件: {Path.GetFileName(DataPath)}");
                    info.Add($"文件大小: {FormatFileSize(fileSize)}");
                    info.Add($"修改时间: {lastModified:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    info.Add("数据文件: 文件不存在");
                }
            }

            FileInfoText = info.Count > 0 ? string.Join("\n", info) : "请选择模板文件和数据文件";
        }

        private void UpdateTemplateFilesList()
        {
            TemplateFiles.Clear();

            if (FolderStructure != null)
            {
                AddFilesToList(FolderStructure);
            }
        }

        private void AddFilesToList(FolderStructure folder)
        {
            foreach (var file in folder.DocxFiles)
            {
                TemplateFiles.Add(file);
            }

            foreach (var subFolder in folder.SubFolders)
            {
                AddFilesToList(subFolder);
            }
        }

        private void ExtractTemplateFiles(FolderStructure folder, List<Models.FileInfo> templateFiles)
        {
            foreach (var file in folder.DocxFiles)
            {
                templateFiles.Add(file);
            }

            foreach (var subFolder in folder.SubFolders)
            {
                ExtractTemplateFiles(subFolder, templateFiles);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 检查是否为 docx 文件
        /// </summary>
        private static bool IsDocxFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".docx" || extension == ".dotx";
        }

        #endregion
    }
}
