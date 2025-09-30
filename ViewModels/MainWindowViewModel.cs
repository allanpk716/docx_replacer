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
using DocuFiller.Services.Interfaces;
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
        
        // 文件夹拖拽相关属性
        private string? _templateFolderPath;
        private bool _isFolderMode;
        private bool _isFolderDragOver;
        private FolderStructure? _folderStructure;
        private string? _foundDocxFilesCount;
        
        // 集合属性
        public ObservableCollection<Dictionary<string, object>> PreviewData { get; } = new();
        public ObservableCollection<ContentControlData> ContentControls { get; } = new();
        public ObservableCollection<DocuFiller.Models.FileInfo> TemplateFiles { get; } = new ObservableCollection<DocuFiller.Models.FileInfo>();
        
        public MainWindowViewModel(
            IDocumentProcessor documentProcessor,
            IDataParser dataParser,
            IFileService fileService,
            IProgressReporter progressReporter,
            IFileScanner fileScanner,
            IDirectoryManager directoryManager,
            ILogger<MainWindowViewModel> logger)
        {
            _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
            _dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _fileScanner = fileScanner ?? throw new ArgumentNullException(nameof(fileScanner));
            _directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeCommands();
            SubscribeToProgressEvents();
            
            // 设置默认输出目录
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出");
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
                    UpdateFileInfo();
                    OnPropertyChanged(nameof(CanStartProcess));
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
        public string TemplateFolderPath
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
        
        public FolderStructure FolderStructure
        {
            get => _folderStructure;
            set => SetProperty(ref _folderStructure, value);
        }
        
        public string FoundDocxFilesCount
        {
            get => _foundDocxFilesCount;
            set => SetProperty(ref _foundDocxFilesCount, value);
        }
        
        public bool CanStartProcess => !IsProcessing && !string.IsNullOrEmpty(DataPath) && 
            ((!IsFolderMode && !string.IsNullOrEmpty(TemplatePath)) || 
             (IsFolderMode && FolderStructure != null && !FolderStructure.IsEmpty));
        public bool CanCancelProcess => IsProcessing;
        public bool CanProcessFolder => !IsProcessing && IsFolderMode && FolderStructure != null && !FolderStructure.IsEmpty && !string.IsNullOrEmpty(DataPath);
        
        #endregion
        
        #region 命令
        
        public ICommand BrowseTemplateCommand { get; private set; }
        public ICommand BrowseDataCommand { get; private set; }
        public ICommand BrowseOutputCommand { get; private set; }
        public ICommand ValidateTemplateCommand { get; private set; }
        public ICommand PreviewDataCommand { get; private set; }
        public ICommand StartProcessCommand { get; private set; }
        public ICommand CancelProcessCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        
        // 文件夹拖拽相关命令
        public ICommand SwitchToSingleModeCommand { get; private set; }
        public ICommand SwitchToFolderModeCommand { get; private set; }
        public ICommand ProcessFolderCommand { get; private set; }
        
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
            
            // 文件夹拖拽相关命令
            SwitchToSingleModeCommand = new RelayCommand(() => IsFolderMode = false);
            SwitchToFolderModeCommand = new RelayCommand(() => IsFolderMode = true);
            ProcessFolderCommand = new RelayCommand(async () => await ProcessFolderAsync(), () => CanProcessFolder);
        }
        

        
        private void SubscribeToProgressEvents()
        {
            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }
        
        private void OnProgressUpdated(object? sender, ProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
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
                Title = "选择JSON数据文件",
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
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
                
                var statistics = await _dataParser.GetDataStatisticsAsync(DataPath);
                DataStatistics = statistics;
                
                var preview = await _dataParser.GetDataPreviewAsync(DataPath, 10);
                
                PreviewData.Clear();
                foreach (var item in preview)
                {
                    PreviewData.Add(item);
                }
                
                ProgressMessage = $"数据加载完成，共 {statistics.TotalRecords} 条记录";
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
                
                var result = await _documentProcessor.ProcessDocumentsAsync(request);
                
                if (result.IsSuccess)
                {
                    MessageBox.Show($"处理完成！\n生成了 {result.SuccessfulRecords} 个文件\n耗时：{result.Duration.TotalSeconds:F1} 秒", 
                        "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"处理失败：\n{string.Join("\n", result.Errors)}", 
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
                    TemplateFolderPath = TemplateFolderPath,
                    DataFilePath = DataPath,
                    OutputDirectory = OutputDirectory,
                    PreserveDirectoryStructure = true,
                    TemplateFiles = templateFiles
                };
                
                Console.WriteLine($"[DEBUG] FolderProcessRequest - TemplateFolderPath: {request.TemplateFolderPath}");
                Console.WriteLine($"[DEBUG] FolderProcessRequest - DataFilePath: {request.DataFilePath}");
                Console.WriteLine($"[DEBUG] FolderProcessRequest - TemplateFiles.Count: {request.TemplateFiles?.Count ?? 0}");
                
                ProgressMessage = "开始批量处理文件夹...";
                
                if (_cancellationTokenSource == null)
                {
                    Console.WriteLine("[DEBUG] 警告：_cancellationTokenSource为null，使用默认取消令牌");
                }
                
                var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;
                var result = await _documentProcessor.ProcessFolderAsync(request, cancellationToken);
                
                if (result.IsSuccess)
                {
                    MessageBox.Show($"批量处理完成！\n处理了 {result.TotalProcessed} 个文件\n生成了 {result.GeneratedFiles.Count} 个文件\n耗时：{result.Duration.TotalSeconds:F1} 秒", 
                        "处理完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"批量处理失败：\n{string.Join("\n", result.Errors)}", 
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
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
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

        #endregion
    }


}