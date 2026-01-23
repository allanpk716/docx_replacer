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
using DocuFiller.Models;
using DocuFiller.Models.Update;
using DocuFiller.Services.Interfaces;
using DocuFiller.Services.Update;
using DocuFiller.Utils;
using DocuFiller.Views.Update;
using DocuFiller.ViewModels.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocuFiller.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IDataParser _dataParser;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly IFileScanner _fileScanner;
        private readonly IDirectoryManager _directoryManager;
        private readonly IExcelDataParser _excelDataParser;
        private readonly IUpdateService _updateService;
        private readonly IDocumentCleanupService _cleanupService;
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
        private DataStatistics _dataStatistics = new();
        private bool _isUpdateAvailable;
        private VersionInfo? _latestVersionInfo;
        private UpdateBannerViewModel? _updateBannerViewModel;
        
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
        private DataFileType _dataFileType = DataFileType.Json;

        // 清理功能相关字段
        private bool _isCleanupProcessing;
        private string _cleanupProgressStatus = "等待处理...";
        private int _cleanupProgressPercent;

        // 集合属性
        public ObservableCollection<Dictionary<string, object>> PreviewData { get; } = new();
        public ObservableCollection<ContentControlData> ContentControls { get; } = new();
        public ObservableCollection<DocuFiller.Models.FileInfo> TemplateFiles { get; } = new ObservableCollection<DocuFiller.Models.FileInfo>();
        public ObservableCollection<CleanupFileItem> CleanupFileItems { get; } = new();
        
        public MainWindowViewModel(
            IDocumentProcessor documentProcessor,
            IDataParser dataParser,
            IFileService fileService,
            IProgressReporter progressReporter,
            IFileScanner fileScanner,
            IDirectoryManager directoryManager,
            IExcelDataParser excelDataParser,
            IUpdateService updateService,
            IDocumentCleanupService cleanupService,
            ILogger<MainWindowViewModel> logger,
            UpdateBannerViewModel? updateBannerViewModel = null)
        {
            _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
            _dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _fileScanner = fileScanner ?? throw new ArgumentNullException(nameof(fileScanner));
            _directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            _excelDataParser = excelDataParser ?? throw new ArgumentNullException(nameof(excelDataParser));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();
            SubscribeToProgressEvents();
            SubscribeToUpdateEvents();

            // 初始化更新横幅（使用现有的 logger 创建临时 UpdateBannerViewModel）
            UpdateBanner = updateBannerViewModel ?? new UpdateBannerViewModel(new LoggerFactory().CreateLogger<UpdateBannerViewModel>());

            // 设置默认输出目录
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出");

            // 启动时自动检查更新（后台运行）
            Task.Run(async () => await OnInitializedAsync());
        }

        /// <summary>
        /// 初始化后的异步操作
        /// </summary>
        private async Task OnInitializedAsync()
        {
            try
            {
                // 从配置读取是否在启动时检查更新
                var checkOnStartup = GetConfigValue("CheckUpdateOnStartup", "true");
                if (!bool.Parse(checkOnStartup))
                {
                    _logger.LogInformation("启动时自动检查更新已禁用");
                    return;
                }

                // 延迟2秒后检查，避免影响启动速度
                await Task.Delay(2000);

                _logger.LogInformation("开始启动时自动检查更新...");
                await CheckForUpdateAsync(isAutoCheck: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动时自动检查更新失败");
            }
        }

        /// <summary>
        /// 检查更新并显示横幅
        /// </summary>
        /// <param name="isAutoCheck">是否为自动检查</param>
        private async Task CheckForUpdatesAsync(bool isAutoCheck = false)
        {
            try
            {
                if (isAutoCheck)
                {
                    _logger.LogInformation("自动检查更新");
                }
                else
                {
                    _logger.LogInformation("用户手动检查更新");
                    ProgressMessage = "正在检查更新...";
                }

                var currentVersion = VersionHelper.GetCurrentVersion();
                var channel = GetConfigValue("UpdateChannel", "stable");

                var versionInfo = await _updateService.CheckForUpdateAsync(currentVersion, channel);

                if (versionInfo != null)
                {
                    _logger.LogInformation("发现新版本: {Version}", versionInfo.Version);
                    if (!isAutoCheck)
                    {
                        ProgressMessage = $"发现新版本: {versionInfo.Version}";
                    }

                    // 在 UI 线程上显示更新横幅
                    await ShowUpdateBannerAsync(versionInfo);
                }
                else
                {
                    _logger.LogInformation("当前版本已是最新: {Version}", currentVersion);
                    if (!isAutoCheck)
                    {
                        ProgressMessage = "当前版本已是最新";
                        MessageBox.Show(
                            $"当前版本 {currentVersion} 已是最新版本！",
                            "检查更新",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新时发生错误");
                if (!isAutoCheck)
                {
                    ProgressMessage = "检查更新失败";
                    MessageBox.Show(
                        $"检查更新时发生错误：{ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 显示更新横幅
        /// </summary>
        private async Task ShowUpdateBannerAsync(VersionInfo versionInfo)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    _logger.LogInformation("显示更新横幅: {Version}", versionInfo.Version);

                    // 设置更新横幅的版本信息
                    if (UpdateBanner != null)
                    {
                        UpdateBanner.VersionInfo = versionInfo;
                        UpdateBanner.IsVisible = true;
                    }

                    // 直接调用更新窗口（为了简化，我们直接显示更新窗口并隐藏横幅）
                    ShowUpdateWindow(versionInfo);
                    if (UpdateBanner != null)
                    {
                        UpdateBanner.IsVisible = false; // 隐藏横幅
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示更新横幅时发生错误");
                }
            });
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
                    // 检测文件类型
                    if (!string.IsNullOrEmpty(value))
                    {
                        var extension = Path.GetExtension(value).ToLowerInvariant();
                        DataFileType = extension == ".xlsx" ? DataFileType.Excel : DataFileType.Json;
                    }

                    UpdateFileInfo();
                    OnPropertyChanged(nameof(CanStartProcess));
                    OnPropertyChanged(nameof(DataFileTypeDisplay));
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
                }
            }
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
        public string DataFileTypeDisplay => DataFileType switch
        {
            DataFileType.Excel => "Excel (支持格式)",
            DataFileType.Json => "JSON (纯文本)",
            _ => "未知"
        };
        
        public bool CanStartProcess => !IsProcessing &&
            !string.IsNullOrEmpty(DataPath) &&
            InputSourceType != InputSourceType.None &&
            ((InputSourceType == InputSourceType.SingleFile && SingleFileInfo != null) ||
             (InputSourceType == InputSourceType.Folder && FolderStructure != null && !FolderStructure.IsEmpty));
        public bool CanCancelProcess => IsProcessing;
        public bool CanProcessFolder => !IsProcessing && IsFolderMode && FolderStructure != null && !FolderStructure.IsEmpty && !string.IsNullOrEmpty(DataPath);

        /// <summary>
        /// 是否有可用更新
        /// </summary>
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetProperty(ref _isUpdateAvailable, value);
        }

        /// <summary>
        /// 最新版本信息
        /// </summary>
        public VersionInfo? LatestVersionInfo
        {
            get => _latestVersionInfo;
            set => SetProperty(ref _latestVersionInfo, value);
        }

        /// <summary>
        /// 更新横幅视图模型
        /// </summary>
        public UpdateBannerViewModel? UpdateBanner
        {
            get => _updateBannerViewModel;
            private set => SetProperty(ref _updateBannerViewModel, value);
        }

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
        public ICommand OpenConverterCommand { get; private set; } = null!;
        public ICommand CheckForUpdateCommand { get; private set; } = null!;
        public ICommand OpenCleanupCommand { get; private set; } = null!;

        // 清理相关命令
        public ICommand RemoveSelectedCleanupCommand { get; private set; } = null!;
        public ICommand ClearCleanupListCommand { get; private set; } = null!;
        public ICommand StartCleanupCommand { get; private set; } = null!;
        public ICommand CloseCleanupCommand { get; private set; } = null!;

        // 文件夹拖拽相关命令
        public ICommand SwitchToSingleModeCommand { get; private set; } = null!;
        public ICommand SwitchToFolderModeCommand { get; private set; } = null!;
        public ICommand ProcessFolderCommand { get; private set; } = null!;
        public ICommand BrowseTemplateFolderCommand { get; private set; } = null!;
        
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
            OpenConverterCommand = new RelayCommand(OpenConverter);
            CheckForUpdateCommand = new RelayCommand(async () => await CheckForUpdateAsync());
            OpenCleanupCommand = new RelayCommand(OpenCleanup);

            // 清理相关命令
            RemoveSelectedCleanupCommand = new RelayCommand(RemoveSelectedCleanup);
            ClearCleanupListCommand = new RelayCommand(ClearCleanupList);
            StartCleanupCommand = new RelayCommand(async () => await StartCleanupAsync(), () => CanStartCleanup);
            CloseCleanupCommand = new RelayCommand(CloseCleanup);

            // 文件夹拖拽相关命令
            SwitchToSingleModeCommand = new RelayCommand(() => IsFolderMode = false);
            SwitchToFolderModeCommand = new RelayCommand(() => IsFolderMode = true);
            ProcessFolderCommand = new RelayCommand(async () => await ProcessFolderAsync(), () => CanProcessFolder);
            BrowseTemplateFolderCommand = new RelayCommand(BrowseTemplateFolder);
        }
        


        private void SubscribeToProgressEvents()
        {
            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }

        private void SubscribeToUpdateEvents()
        {
            _updateService.UpdateAvailable += OnUpdateAvailable;
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
                Filter = "支持的数据文件 (*.xlsx;*.json)|*.xlsx;*.json|Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                DataPath = dialog.FileName;
            }
        }
        
        private void BrowseOutput()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择输出目录",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹",
                Filter = "文件夹|*.folder"
            };
            
            if (dialog.ShowDialog() == true)
            {
                OutputDirectory = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
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

                if (DataFileType == DataFileType.Excel)
                {
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
                else
                {
                    // 保留原有的 JSON 预览逻辑
                    var statistics = await _dataParser.GetDataStatisticsAsync(DataPath);
                    DataStatistics = statistics;

                    var preview = await _dataParser.GetDataPreviewAsync(DataPath, 10);

                    PreviewData.Clear();
                    foreach (var item in preview)
                    {
                        PreviewData.Add(item);
                    }

                    ProgressMessage = $"JSON 数据加载完成，共 {statistics.TotalRecords} 条记录";
                }
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
                _logger.LogInformation($"[调试] 开始处理文档");
                _logger.LogInformation($"[调试] 模板路径: '{TemplatePath}'");
                _logger.LogInformation($"[调试] 数据路径: '{DataPath}'");
                _logger.LogInformation($"[调试] 输出目录: '{OutputDirectory}'");
                _logger.LogInformation($"[调试] 是否为文件夹模式: {IsFolderMode}");
                
                // 同时输出到控制台
                Console.WriteLine($"[调试] 开始处理文档");
                Console.WriteLine($"[调试] 模板路径: '{TemplatePath}'");
                Console.WriteLine($"[调试] 数据路径: '{DataPath}'");
                Console.WriteLine($"[调试] 输出目录: '{OutputDirectory}'");
                Console.WriteLine($"[调试] 是否为文件夹模式: {IsFolderMode}");
                
                // 检查处理模式
                if (IsFolderMode)
                {
                    _logger.LogInformation($"[调试] 进入文件夹模式处理");
                    Console.WriteLine($"[调试] 进入文件夹模式处理");
                    await ProcessFolderAsync();
                    return;
                }
                
                _logger.LogInformation($"[调试] 进入单文件模式处理");
                Console.WriteLine($"[调试] 进入单文件模式处理");
                
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
        
        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private void OpenConverter()
        {
            try
            {
                var app = (App)Application.Current;
                IServiceProvider serviceProvider = app.ServiceProvider;
                var converterWindow = serviceProvider.GetRequiredService<Views.ConverterWindow>();
                converterWindow.Owner = Application.Current.MainWindow;
                converterWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开转换器窗口时发生错误");
                MessageBox.Show($"打开转换器窗口时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                Console.WriteLine($"[DEBUG] HandleFolderDropAsync 被调用，文件夹路径: {folderPath}");
                
                if (!_fileScanner.IsValidFolder(folderPath))
                {
                    Console.WriteLine($"[DEBUG] 无效的文件夹路径: {folderPath}");
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
                Console.WriteLine($"[DEBUG] 设置TemplateFolderPath: {TemplateFolderPath}");
                Console.WriteLine($"[DEBUG] 设置TemplatePath: {TemplatePath}");
                Console.WriteLine($"[DEBUG] 设置FoundDocxFilesCount: {FoundDocxFilesCount}");
                Console.WriteLine($"[DEBUG] 设置IsFolderMode: {IsFolderMode}");
                
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
                
                Console.WriteLine($"[DEBUG] ProcessFolderAsync - 提取到的模板文件数量: {templateFiles.Count}");
                foreach (var file in templateFiles)
                {
                    Console.WriteLine($"[DEBUG] 模板文件: {file.FullPath}");
                }
                
                var request = new FolderProcessRequest
                {
                    TemplateFolderPath = TemplateFolderPath ?? string.Empty,
                    DataFilePath = DataPath,
                    OutputDirectory = OutputDirectory,
                    PreserveDirectoryStructure = true,
                    TemplateFiles = templateFiles
                };
                
                Console.WriteLine($"[DEBUG] FolderProcessRequest - TemplateFolderPath: {request.TemplateFolderPath}");
                Console.WriteLine($"[DEBUG] FolderProcessRequest - DataFilePath: {request.DataFilePath}");
                Console.WriteLine($"[DEBUG] FolderProcessRequest - TemplateFiles.Count: {request.TemplateFiles?.Count ?? 0}");
                
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
            // 使用 OpenFileDialog 作为临时方案，用户可以选择文件夹中的任意文件
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择包含模板文件的文件夹（选择文件夹中的任意文件）",
                Filter = "Word文档 (*.docx;*.dotx)|*.docx;*.dotx|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            var result = dialog.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(dialog.FileName))
            {
                // 获取文件所在目录
                var directory = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(directory))
                {
                    // 异步处理文件夹扫描
                    Task.Run(async () =>
                    {
                        await HandleFolderDropAsync(directory);
                    });
                }
            }
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="isAutoCheck">是否为自动检查</param>
        private async Task CheckForUpdateAsync(bool isAutoCheck = false)
        {
            try
            {
                if (isAutoCheck)
                {
                    _logger.LogInformation("自动检查更新");
                }
                else
                {
                    _logger.LogInformation("用户手动检查更新");
                    ProgressMessage = "正在检查更新...";
                }

                var currentVersion = VersionHelper.GetCurrentVersion();
                var channel = GetConfigValue("UpdateChannel", "stable");

                var versionInfo = await _updateService.CheckForUpdateAsync(currentVersion, channel);

                if (versionInfo != null)
                {
                    _logger.LogInformation("发现新版本: {Version}", versionInfo.Version);
                    if (!isAutoCheck)
                    {
                        ProgressMessage = $"发现新版本: {versionInfo.Version}";
                    }

                    // 显示更新窗口
                    ShowUpdateWindow(versionInfo);
                }
                else
                {
                    _logger.LogInformation("当前版本已是最新: {Version}", currentVersion);
                    if (!isAutoCheck)
                    {
                        ProgressMessage = "当前版本已是最新";
                        MessageBox.Show(
                            $"当前版本 {currentVersion} 已是最新版本！",
                            "检查更新",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新时发生错误");
                if (!isAutoCheck)
                {
                    ProgressMessage = "检查更新失败";
                    MessageBox.Show(
                        $"检查更新时发生错误：{ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 处理更新可用事件
        /// </summary>
        private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _logger.LogInformation("收到更新可用事件: {Version}", e.Version.Version);

                    LatestVersionInfo = e.Version;
                    IsUpdateAvailable = true;

                    // 显示更新通知对话框
                    var result = MessageBox.Show(
                        $"发现新版本 {e.Version.Version}！\n\n是否立即查看更新详情？",
                        "发现新版本",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowUpdateWindow(e.Version);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理更新可用事件时发生错误");
                }
            });
        }

        /// <summary>
        /// 显示更新窗口
        /// </summary>
        private void ShowUpdateWindow(VersionInfo versionInfo)
        {
            try
            {
                _logger.LogInformation("显示更新窗口: {Version}", versionInfo.Version);

                var app = (App)Application.Current;
                var updateViewModel = app.ServiceProvider.GetRequiredService<UpdateViewModel>();
                var updateWindow = new UpdateWindow(updateViewModel);

                updateWindow.Owner = Application.Current.MainWindow;
                updateWindow.ShowDialog();

                _logger.LogInformation("更新窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示更新窗口时发生错误");
                MessageBox.Show(
                    $"显示更新窗口时发生错误：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                for (int i = 0; i < CleanupFileItems.Count; i++)
                {
                    var fileItem = CleanupFileItems[i];
                    fileItem.Status = CleanupFileStatus.Processing;
                    CleanupProgressStatus = $"正在处理: {fileItem.FileName} ({i + 1}/{CleanupFileItems.Count})";
                    CleanupProgressPercent = (int)((i / (double)CleanupFileItems.Count) * 100);

                    var result = await _cleanupService.CleanupAsync(fileItem);

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
                            fileItem.StatusMessage = result.Message;
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

                MessageBox.Show(
                    $"清理完成！\n\n成功: {successCount}\n失败: {failureCount}\n跳过: {skippedCount}",
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

        #endregion
    }

    /// <summary>
    /// 数据文件类型
    /// </summary>
    public enum DataFileType
    {
        Json,
        Excel
    }
}