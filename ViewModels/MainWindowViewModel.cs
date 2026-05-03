using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 更新状态枚举，用于状态栏常驻提示
    /// </summary>
    public enum UpdateStatus
    {
        /// <summary>初始状态，尚未检查</summary>
        None,
        /// <summary>有新版本可用</summary>
        UpdateAvailable,
        /// <summary>当前已是最新版本</summary>
        UpToDate,
        /// <summary>正在检查更新</summary>
        Checking,
        /// <summary>检查更新失败</summary>
        Error
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly IFileScanner _fileScanner;
        private readonly IDirectoryManager _directoryManager;
        private readonly IExcelDataParser _excelDataParser;
        private readonly IDocumentCleanupService _cleanupService;
        private readonly IUpdateService? _updateService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 私有字段
        private string _templatePath = string.Empty;
        private string _dataPath = string.Empty;
        private string _outputDirectory = string.Empty;
        private string _fileInfoText = "请选择模板文件和数据文件";
        private string _progressMessage = "就绪";
        private string _progressText = "0%";
        private double _progressPercentage = 0;
        private bool _isProcessing = false;
        private bool _isTopmost = false;
        private DataStatistics _dataStatistics = new();
        // 文件夹拖拽相关属性
        private string? _templateFolderPath;
        private bool _isFolderMode;
        private bool _isFolderDragOver;
        private FolderStructure? _folderStructure;
        private string? _foundDocxFilesCount;

        // 输入源类型相关
        private InputSourceType _inputSourceType = InputSourceType.None;
        private Models.FileInfo? _singleFileInfo;

        // 数据文件类型
        private DataFileType _dataFileType = DataFileType.Excel;

        // 清理功能相关字段
        private bool _isCleanupProcessing;
        private string _cleanupProgressStatus = "等待处理...";
        private int _cleanupProgressPercent;
        private string _cleanupOutputDirectory = string.Empty;

        // 更新检查相关字段
        private bool _isCheckingUpdate;
        private UpdateStatus _updateStatus = UpdateStatus.None;
        private string _updateStatusMessage = string.Empty;

        // 集合属性
        public ObservableCollection<Dictionary<string, object>> PreviewData { get; } = new();
        public ObservableCollection<ContentControlData> ContentControls { get; } = new();
        public ObservableCollection<DocuFiller.Models.FileInfo> TemplateFiles { get; } = new ObservableCollection<DocuFiller.Models.FileInfo>();
        public ObservableCollection<CleanupFileItem> CleanupFileItems { get; } = new();
        
        public MainWindowViewModel(
            IDocumentProcessor documentProcessor,
            IFileService fileService,
            IProgressReporter progressReporter,
            IFileScanner fileScanner,
            IDirectoryManager directoryManager,
            IExcelDataParser excelDataParser,
            IDocumentCleanupService cleanupService,
            ILogger<MainWindowViewModel> logger,
            IUpdateService? updateService = null)
        {
            _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _fileScanner = fileScanner ?? throw new ArgumentNullException(nameof(fileScanner));
            _directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            _excelDataParser = excelDataParser ?? throw new ArgumentNullException(nameof(excelDataParser));
            _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updateService = updateService;

            InitializeCommands();
            SubscribeToProgressEvents();

            // 设置默认输出目录
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出");

            // 设置默认清理输出目录
            _cleanupOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出", "清理");

            // 启动时自动检查更新状态（fire-and-forget，不阻塞 UI）
            _ = InitializeUpdateStatusAsync();
        }

        #region 属性
        
        public string TemplatePath
        {
            get => _templatePath;
            set
            {
                if (SetProperty(ref _templatePath, value))
                {
                    UpdateFileInfo();
                    OnPropertyChanged(nameof(CanStartProcess));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        
        public string DataPath
        {
            get => _dataPath;
            set
            {
                if (SetProperty(ref _dataPath, value))
                {
                    UpdateFileInfo();
                    OnPropertyChanged(nameof(CanStartProcess));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        
        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }
        
        public string FileInfoText
        {
            get => _fileInfoText;
            set => SetProperty(ref _fileInfoText, value);
        }
        
        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }
        
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }
        
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }
        
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    OnPropertyChanged(nameof(CanStartProcess));
                    OnPropertyChanged(nameof(CanCancelProcess));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsTopmost
        {
            get => _isTopmost;
            set => SetProperty(ref _isTopmost, value);
        }
        
        public DataStatistics DataStatistics
        {
            get => _dataStatistics;
            set => SetProperty(ref _dataStatistics, value);
        }
        
        // 文件夹拖拽相关属性
        public string? TemplateFolderPath
        {
            get => _templateFolderPath;
            set
            {
                if (SetProperty(ref _templateFolderPath, value))
                {
                    OnTemplateFolderChanged();
                }
            }
        }
        
        public bool IsFolderMode
        {
            get => _isFolderMode;
            set => SetProperty(ref _isFolderMode, value);
        }
        
        public bool IsFolderDragOver
        {
            get => _isFolderDragOver;
            set => SetProperty(ref _isFolderDragOver, value);
        }
        
        public FolderStructure? FolderStructure
        {
            get => _folderStructure;
            set => SetProperty(ref _folderStructure, value);
        }
        
        public string? FoundDocxFilesCount
        {
            get => _foundDocxFilesCount;
            set => SetProperty(ref _foundDocxFilesCount, value);
        }

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
                    CommandManager.InvalidateRequerySuggested();
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

        /// <summary>
        /// 数据文件类型
        /// </summary>
        public DataFileType DataFileType
        {
            get => _dataFileType;
            set => SetProperty(ref _dataFileType, value);
        }

        /// <summary>
        /// 数据文件类型显示文本
        /// </summary>
        public string DataFileTypeDisplay => "Excel (支持格式)";
        
        public bool CanStartProcess => !IsProcessing &&
            !string.IsNullOrEmpty(DataPath) &&
            InputSourceType != InputSourceType.None &&
            ((InputSourceType == InputSourceType.SingleFile && SingleFileInfo != null) ||
             (InputSourceType == InputSourceType.Folder && FolderStructure != null && !FolderStructure.IsEmpty));
        public bool CanCancelProcess => IsProcessing;
        public bool CanProcessFolder => !IsProcessing && IsFolderMode && FolderStructure != null && !FolderStructure.IsEmpty && !string.IsNullOrEmpty(DataPath);

        /// <summary>
        /// 清理功能处理状态
        /// </summary>
        public bool IsCleanupProcessing
        {
            get => _isCleanupProcessing;
            set
            {
                if (SetProperty(ref _isCleanupProcessing, value))
                {
                    OnPropertyChanged(nameof(CanStartCleanup));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 清理进度状态文本
        /// </summary>
        public string CleanupProgressStatus
        {
            get => _cleanupProgressStatus;
            set => SetProperty(ref _cleanupProgressStatus, value);
        }

        /// <summary>
        /// 清理进度百分比
        /// </summary>
        public int CleanupProgressPercent
        {
            get => _cleanupProgressPercent;
            set => SetProperty(ref _cleanupProgressPercent, value);
        }

        /// <summary>
        /// 是否可以开始清理
        /// </summary>
        public bool CanStartCleanup => CleanupFileItems.Count > 0 && !IsCleanupProcessing;

        /// <summary>
        /// 清理功能输出目录
        /// </summary>
        public string CleanupOutputDirectory
        {
            get => _cleanupOutputDirectory;
            set => SetProperty(ref _cleanupOutputDirectory, value);
        }

        /// <summary>
        /// 当前应用程序版本号
        /// </summary>
        public string CurrentVersion => VersionHelper.GetCurrentVersion();

        /// <summary>
        /// 是否正在检查更新
        /// </summary>
        public bool IsCheckingUpdate
        {
            get => _isCheckingUpdate;
            set
            {
                if (SetProperty(ref _isCheckingUpdate, value))
                {
                    OnPropertyChanged(nameof(CanCheckUpdate));
                }
            }
        }

        /// <summary>
        /// 是否可以检查更新（更新源已配置且未在检查中）
        /// </summary>
        public bool CanCheckUpdate => _updateService?.IsUpdateUrlConfigured == true && !IsCheckingUpdate;

        /// <summary>
        /// 更新状态枚举值
        /// </summary>
        public UpdateStatus CurrentUpdateStatus
        {
            get => _updateStatus;
            set
            {
                if (SetProperty(ref _updateStatus, value))
                {
                    OnPropertyChanged(nameof(HasUpdateStatus));
                    OnPropertyChanged(nameof(UpdateStatusMessage));
                    OnPropertyChanged(nameof(UpdateStatusBrush));
                }
            }
        }

        /// <summary>
        /// 状态栏显示的更新状态消息
        /// </summary>
        public string UpdateStatusMessage
        {
            get
            {
                var baseMessage = _updateStatus switch
                {
                    UpdateStatus.UpdateAvailable => "有新版本可用，点击更新",
                    UpdateStatus.UpToDate => "当前已是最新版本",
                    UpdateStatus.Checking => "正在检查更新...",
                    UpdateStatus.Error => "检查更新失败",
                    _ => string.Empty
                };

                // 追加源类型标识
                if (_updateService != null && _updateStatus != UpdateStatus.None && _updateStatus != UpdateStatus.Checking)
                {
                    if (_updateService.UpdateSourceType == "GitHub")
                    {
                        baseMessage += " (GitHub)";
                    }
                    else if (_updateService.UpdateSourceType == "HTTP")
                    {
                        var host = ExtractHostFromUrl(_updateService.EffectiveUpdateUrl);
                        if (!string.IsNullOrEmpty(host))
                        {
                            baseMessage += $" (内网: {host})";
                        }
                    }
                }

                return baseMessage;
            }
        }

        /// <summary>
        /// 状态栏更新状态文本颜色
        /// </summary>
        public Brush UpdateStatusBrush
        {
            get => _updateStatus switch
            {
                UpdateStatus.UpdateAvailable => Brushes.Orange,
                UpdateStatus.UpToDate => Brushes.Green,
                UpdateStatus.Checking => Brushes.Gray,
                UpdateStatus.Error => Brushes.Red,
                _ => Brushes.Gray
            };
        }

        /// <summary>
        /// 是否有可显示的更新状态（用于 UI 可见性绑定）
        /// </summary>
        public bool HasUpdateStatus => _updateStatus != UpdateStatus.None;

        #endregion
        
        #region 命令
        
        public ICommand BrowseTemplateCommand { get; private set; } = null!;
        public ICommand BrowseDataCommand { get; private set; } = null!;
        public ICommand BrowseOutputCommand { get; private set; } = null!;
        public ICommand ValidateTemplateCommand { get; private set; } = null!;
        public ICommand PreviewDataCommand { get; private set; } = null!;
        public ICommand StartProcessCommand { get; private set; } = null!;
        public ICommand CancelProcessCommand { get; private set; } = null!;
        public ICommand ExitCommand { get; private set; } = null!;
        public ICommand OpenCleanupCommand { get; private set; } = null!;

        // 清理相关命令
        public ICommand RemoveSelectedCleanupCommand { get; private set; } = null!;
        public ICommand ClearCleanupListCommand { get; private set; } = null!;
        public ICommand StartCleanupCommand { get; private set; } = null!;
        public ICommand CloseCleanupCommand { get; private set; } = null!;
        public ICommand BrowseCleanupOutputCommand { get; private set; } = null!;
        public ICommand OpenCleanupOutputFolderCommand { get; private set; } = null!;

        // 更新检查命令
        public ICommand CheckUpdateCommand { get; private set; } = null!;
        public ICommand UpdateStatusClickCommand { get; private set; } = null!;
        public ICommand OpenUpdateSettingsCommand { get; private set; } = null!;

        // 文件夹拖拽相关命令
        public ICommand SwitchToSingleModeCommand { get; private set; } = null!;
        public ICommand SwitchToFolderModeCommand { get; private set; } = null!;
        public ICommand ProcessFolderCommand { get; private set; } = null!;
        public ICommand BrowseTemplateFolderCommand { get; private set; } = null!;
        public ICommand ToggleTopmostCommand { get; private set; } = null!;
        
        #endregion
        
        #region 私有方法
        
        private void InitializeCommands()
        {
            BrowseTemplateCommand = new RelayCommand(BrowseTemplate);
            BrowseDataCommand = new RelayCommand(BrowseData);
            BrowseOutputCommand = new RelayCommand(BrowseOutput);
            ValidateTemplateCommand = new RelayCommand(ValidateTemplate, () => !string.IsNullOrEmpty(TemplatePath));
            PreviewDataCommand = new RelayCommand(PreviewDataAsync, () => !string.IsNullOrEmpty(DataPath));
            StartProcessCommand = new RelayCommand(async () => await StartProcessAsync(), () => CanStartProcess);
            CancelProcessCommand = new RelayCommand(CancelProcess, () => CanCancelProcess);
            ExitCommand = new RelayCommand(ExitApplication);
            OpenCleanupCommand = new RelayCommand(OpenCleanup);

            // 清理相关命令
            RemoveSelectedCleanupCommand = new RelayCommand(RemoveSelectedCleanup);
            ClearCleanupListCommand = new RelayCommand(ClearCleanupList);
            StartCleanupCommand = new RelayCommand(async () => await StartCleanupAsync(), () => CanStartCleanup);
            CloseCleanupCommand = new RelayCommand(CloseCleanup);
            BrowseCleanupOutputCommand = new RelayCommand(BrowseCleanupOutput);
            OpenCleanupOutputFolderCommand = new RelayCommand(OpenCleanupOutputFolder);

            // 文件夹拖拽相关命令
            SwitchToSingleModeCommand = new RelayCommand(() => IsFolderMode = false);
            SwitchToFolderModeCommand = new RelayCommand(() => IsFolderMode = true);
            ProcessFolderCommand = new RelayCommand(async () => await ProcessFolderAsync(), () => CanProcessFolder);
            BrowseTemplateFolderCommand = new RelayCommand(BrowseTemplateFolder);
            ToggleTopmostCommand = new RelayCommand(ToggleTopmost);

            // 更新检查命令
            CheckUpdateCommand = new RelayCommand(async () => await CheckUpdateAsync(), () => CanCheckUpdate);
            UpdateStatusClickCommand = new RelayCommand(async () => await OnUpdateStatusClickAsync(), () => HasUpdateStatus);
            OpenUpdateSettingsCommand = new RelayCommand(OpenUpdateSettings);
        }
        


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
        
        private async void ValidateTemplate()
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
        
        private async void PreviewDataAsync()
        {
            try
            {
                ProgressMessage = "加载数据预览...";

                var preview = await _excelDataParser.GetDataPreviewAsync(DataPath, 10);

                PreviewData.Clear();
                foreach (var item in preview)
                {
                    // 转换 Dictionary<string, FormattedCellValue> 为 Dictionary<string, object>
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
        
        private async Task StartProcessAsync()
        {
            try
            {
                // 添加调试信息
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

                // 使用 Task.Run 将处理移到后台线程，避免阻塞 UI
                var result = await Task.Run(async () =>
                {
                    return await _documentProcessor.ProcessDocumentsAsync(request);
                }, _cancellationTokenSource.Token);
                
                if (result.IsSuccess)
                {
                    MessageBox.Show($"处理完成！\n生成了 {result.SuccessfulRecords} 个文件\n耗时：{result.Duration.TotalSeconds:F1} 秒",
                        "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // 收集所有错误信息
                    var errorMessages = new List<string>();

                    // 添加 Errors 列表中的错误
                    if (result.Errors != null && result.Errors.Any())
                    {
                        errorMessages.AddRange(result.Errors);
                    }

                    // 添加 ErrorMessage（如果不在 Errors 列表中）
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (!errorMessages.Contains(result.ErrorMessage))
                        {
                            errorMessages.Add(result.ErrorMessage);
                        }
                    }

                    // 添加 Message 属性（如果有）
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
        
        private void CancelProcess()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        private void ToggleTopmost()
        {
            IsTopmost = !IsTopmost;
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private void OpenCleanup()
        {
            try
            {
                var app = (App)Application.Current;
                IServiceProvider serviceProvider = app.ServiceProvider;
                var cleanupWindow = serviceProvider.GetRequiredService<Views.CleanupWindow>();
                cleanupWindow.Owner = Application.Current.MainWindow;
                cleanupWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开清理窗口时发生错误");
                MessageBox.Show($"打开清理窗口时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开更新源设置窗口
        /// </summary>
        private void OpenUpdateSettings()
        {
            try
            {
                var app = (App)Application.Current;
                var settingsWindow = app.ServiceProvider.GetRequiredService<Views.UpdateSettingsWindow>();
                settingsWindow.Owner = Application.Current.MainWindow;
                var result = settingsWindow.ShowDialog();
                if (result == true)
                {
                    // 用户保存了设置，刷新状态栏显示
                    OnPropertyChanged(nameof(UpdateStatusMessage));
                    _logger.LogInformation("更新源设置已保存，状态栏已刷新");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开更新源设置窗口时发生错误");
                MessageBox.Show($"打开更新源设置窗口时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从 URL 中提取主机部分（去除协议和路径）
        /// </summary>
        private static string ExtractHostFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            // 去除协议
            var hostPart = url;
            if (hostPart.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                hostPart = hostPart["https://".Length..];
            else if (hostPart.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                hostPart = hostPart["http://".Length..];

            // 去除路径部分
            var slashIndex = hostPart.IndexOf('/');
            if (slashIndex >= 0)
                hostPart = hostPart[..slashIndex];

            return hostPart;
        }
        
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
        
        #endregion
        
        #region 文件夹拖拽处理方法

        /// <summary>
        /// 处理单个文件拖拽
        /// </summary>
        /// <param name="filePath">文件路径</param>
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
        /// <param name="folderPath">文件夹路径</param>
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
                TemplatePath = folderPath; // 同时更新TemplatePath，让TemplatePathTextBox显示文件夹路径
                FolderStructure = folderStructure;
                IsFolderMode = true;
                
                // 更新找到的文件数量
                FoundDocxFilesCount = folderStructure.TotalDocxCount.ToString();
                
                // 更新模板文件列表
                UpdateTemplateFilesList();

                // 设置输入源类型
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
        private async void OnTemplateFolderChanged()
        {
            if (!string.IsNullOrEmpty(TemplateFolderPath))
            {
                await HandleFolderDropAsync(TemplateFolderPath);
            }
        }
        
        /// <summary>
        /// 更新模板文件列表
        /// </summary>
        private void UpdateTemplateFilesList()
        {
            TemplateFiles.Clear();
            
            if (FolderStructure != null)
            {
                AddFilesToList(FolderStructure);
            }
        }
        
        /// <summary>
        /// 递归添加文件到列表
        /// </summary>
        /// <param name="folder">文件夹结构</param>
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
        
        /// <summary>
        /// 递归提取文件夹结构中的所有模板文件
        /// </summary>
        /// <param name="folder">文件夹结构</param>
        /// <param name="templateFiles">模板文件列表</param>
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
        
        /// <summary>
        /// 处理文件夹批量处理
        /// </summary>
        private async Task ProcessFolderAsync()
        {
            try
            {
                IsProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                // 从FolderStructure中提取所有模板文件
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

                // 关键修改：使用 Task.Run 将整个处理逻辑移到后台线程，避免阻塞 UI
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
                    // 收集所有错误信息
                    var errorMessages = new List<string>();

                    // 添加 Errors 列表中的错误
                    if (result.Errors != null && result.Errors.Any())
                    {
                        errorMessages.AddRange(result.Errors);
                    }

                    // 添加 ErrorMessage（如果不在 Errors 列表中）
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        if (!errorMessages.Contains(result.ErrorMessage))
                        {
                            errorMessages.Add(result.ErrorMessage);
                        }
                    }

                    // 添加 FailedFiles 中的错误
                    if (result.FailedFiles != null && result.FailedFiles.Any())
                    {
                        errorMessages.Add("失败的文件:");
                        errorMessages.AddRange(result.FailedFiles.Take(10)); // 最多显示10个失败的文件
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
        
        #endregion
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        #endregion

        #region 辅助方法

        private string FormatFileSize(long bytes)
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
        /// 浏览并选择文件夹
        /// </summary>
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

                // 异步处理文件夹扫描
                Task.Run(async () =>
                {
                    await HandleFolderDropAsync(selectedPath);
                });
            }
        }

        /// <summary>
        /// 从配置获取值
        /// </summary>
        private static string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                var value = System.Configuration.ConfigurationManager.AppSettings[key];
                return string.IsNullOrEmpty(value) ? defaultValue : value;
            }
            catch
            {
                return defaultValue;
            }
        }

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

        /// <summary>
        /// 异步检查更新
        /// </summary>
        private async Task CheckUpdateAsync()
        {
            if (_updateService == null || !CanCheckUpdate)
                return;

            try
            {
                IsCheckingUpdate = true;
                _logger.LogInformation("用户触发检查更新");

                var updateInfo = await _updateService.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    // 无新版本
                    _logger.LogInformation("当前已是最新版本");
                    MessageBox.Show(
                        $"当前版本 {CurrentVersion} 已是最新版本。",
                        "检查更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // 有新版本
                var newVersion = updateInfo.TargetFullRelease.Version.ToString();
                _logger.LogInformation("发现新版本: {NewVersion}，当前版本: {CurrentVersion}", newVersion, CurrentVersion);

                var result = MessageBox.Show(
                    $"发现新版本：{newVersion}\n当前版本：{CurrentVersion}\n\n是否下载并安装更新？\n（下载完成后将自动重启应用）",
                    "发现新版本",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _logger.LogInformation("用户确认下载更新: {NewVersion}", newVersion);

                    // 创建 DownloadProgressViewModel 和进度窗口
                    var totalBytes = updateInfo.TargetFullRelease.Size;
                    var progressVm = new DownloadProgressViewModel(totalBytes, newVersion);

                    var app = (App)Application.Current;
                    var progressWindow = app.ServiceProvider.GetRequiredService<Views.DownloadProgressWindow>();
                    progressWindow.SetViewModel(progressVm);

                    _logger.LogInformation("下载开始: 版本 {Version}, 总大小 {Size} 字节", newVersion, totalBytes);

                    // 在后台执行下载，进度窗口以模态方式显示
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _updateService.DownloadUpdatesAsync(updateInfo, progress =>
                            {
                                _logger.LogDebug("更新下载进度: {Progress}%", progress);
                                progressVm.UpdateProgress(progress);
                            }, progressVm.CancellationToken);

                            // 下载完成
                            progressVm.MarkCompleted();
                            _logger.LogInformation("更新下载完成，准备应用更新并重启");

                            // 短暂延迟让用户看到完成状态
                            await Task.Delay(800);

                            // 关闭进度窗口
                            progressVm.CloseCallback?.Invoke(true);

                            // 应用更新并重启
                            _updateService.ApplyUpdatesAndRestart();
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("用户取消下载更新");
                            progressVm.MarkCancelled();
                        }
                        catch (Exception downloadEx)
                        {
                            _logger.LogError(downloadEx, "下载更新时发生错误");
                            progressVm.MarkFailed(downloadEx.Message);
                        }
                    });

                    // 模态显示进度窗口，阻塞主窗口直到关闭
                    progressWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新时发生错误");
                MessageBox.Show(
                    $"检查更新时发生错误：\n{ex.Message}",
                    "更新错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        /// <summary>
        /// 启动时自动检查更新状态，初始化状态栏常驻提示
        /// </summary>
        private async Task InitializeUpdateStatusAsync()
        {
            if (_updateService == null)
            {
                _logger.LogInformation("更新服务未注册，跳过更新状态初始化");
                return;
            }

            try
            {
                CurrentUpdateStatus = UpdateStatus.Checking;

                // 更新源未配置
                if (!_updateService.IsUpdateUrlConfigured)
                {
                    _logger.LogInformation("更新源未配置，跳过自动检查");
                    CurrentUpdateStatus = UpdateStatus.None;
                    return;
                }

                // 检查是否有新版本
                var updateInfo = await _updateService.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    var newVersion = updateInfo.TargetFullRelease.Version.ToString();
                    _logger.LogInformation("自动检查发现新版本: {NewVersion}，当前版本: {CurrentVersion}", newVersion, CurrentVersion);
                    CurrentUpdateStatus = UpdateStatus.UpdateAvailable;
                }
                else
                {
                    _logger.LogInformation("自动检查完成，当前已是最新版本: {CurrentVersion}", CurrentVersion);
                    CurrentUpdateStatus = UpdateStatus.UpToDate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动检查更新时发生异常");
                CurrentUpdateStatus = UpdateStatus.Error;
            }
        }

        /// <summary>
        /// 点击状态栏更新提示时，走现有弹窗更新流程
        /// </summary>
        private async Task OnUpdateStatusClickAsync()
        {
            switch (_updateStatus)
            {
                case UpdateStatus.UpdateAvailable:
                    // 有新版本，直接走 CheckUpdateAsync 弹窗流程
                    await CheckUpdateAsync();
                    break;
                case UpdateStatus.Error:
                    // 检查失败时重试
                    await InitializeUpdateStatusAsync();
                    break;
                case UpdateStatus.UpToDate:
                    MessageBox.Show(
                        $"当前版本 {CurrentVersion} 已是最新版本。",
                        "检查更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
            }
        }

        /// <summary>
        /// 移除选中的清理项
        /// </summary>
        private void RemoveSelectedCleanup()
        {
            // 由于 XAML 中的 ListView 使用了 SelectionMode="Extended"
            // 这个方法暂时留空，需要在 code-behind 中处理选中项
            _logger.LogInformation("移除选中的清理项");
        }

        /// <summary>
        /// 清空清理列表
        /// </summary>
        private void ClearCleanupList()
        {
            CleanupFileItems.Clear();
            CleanupProgressStatus = "等待处理...";
            CleanupProgressPercent = 0;
            OnPropertyChanged(nameof(CanStartCleanup));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 开始清理
        /// </summary>
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
                            string outputPath = result.InputType == InputSourceType.Folder
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

        /// <summary>
        /// 关闭清理功能（切换到其他 Tab）
        /// </summary>
        private void CloseCleanup()
        {
            // 清理功能现在在 Tab 页中，这个方法可以留空或用于重置状态
            _logger.LogInformation("关闭清理功能");
        }

        /// <summary>
        /// 浏览并选择清理输出目录
        /// </summary>
        private void BrowseCleanupOutput()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择清理输出目录"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FolderName;
                if (string.IsNullOrEmpty(selectedPath))
                {
                    _logger.LogDebug("BrowseCleanupOutput: 用户未选择文件夹");
                    return;
                }

                CleanupOutputDirectory = selectedPath;
                _logger.LogInformation("清理输出目录已选择: {Path}", selectedPath);
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

        #endregion
    }

    /// <summary>
    /// 数据文件类型
    /// </summary>
    public enum DataFileType
    {
        Excel
    }
}
