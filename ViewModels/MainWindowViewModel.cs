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
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IDataParser _dataParser;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private CancellationTokenSource _cancellationTokenSource;
        
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
        
        // 集合属性
        public ObservableCollection<Dictionary<string, object>> PreviewData { get; } = new();
        public ObservableCollection<ContentControlData> ContentControls { get; } = new();
        
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IDocumentProcessor documentProcessor,
            IDataParser dataParser,
            IFileService fileService,
            IProgressReporter progressReporter)
        {
            _logger = logger;
            _documentProcessor = documentProcessor;
            _dataParser = dataParser;
            _fileService = fileService;
            _progressReporter = progressReporter;
            
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
        
        public bool CanStartProcess => !IsProcessing && !string.IsNullOrEmpty(TemplatePath) && !string.IsNullOrEmpty(DataPath);
        public bool CanCancelProcess => IsProcessing;
        
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
        }
        

        
        private void SubscribeToProgressEvents()
        {
            _progressReporter.ProgressUpdated += OnProgressUpdated;
        }
        
        private void OnProgressUpdated(object sender, ProgressEventArgs e)
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
                if (result.IsSuccess)
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
                    MessageBox.Show($"处理完成！\n生成了 {result.GeneratedFiles.Count} 个文件\n耗时：{result.Duration.TotalSeconds:F1} 秒", 
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