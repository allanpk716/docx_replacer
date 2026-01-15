using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocuFiller.ViewModels
{
    public class ConverterWindowViewModel : INotifyPropertyChanged
    {
        private readonly IExcelToWordConverter _converter;
        private readonly ILogger<ConverterWindowViewModel> _logger;

        private string _outputDirectory = string.Empty;
        private string _progressMessage = "就绪";
        private double _progressPercentage = 0;
        private bool _isConverting = false;

        public ObservableCollection<string> SourceFiles { get; } = new();
        public ObservableCollection<ConvertItemViewModel> ConvertItems { get; } = new();

        public ConverterWindowViewModel(
            IExcelToWordConverter converter,
            ILogger<ConverterWindowViewModel> logger)
        {
            _converter = converter;
            _logger = logger;
            InitializeCommands();

            // 设置默认输出目录为源文件目录
            _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        #region Properties

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                if (SetProperty(ref _isConverting, value))
                {
                    OnPropertyChanged(nameof(CanStartConvert));
                }
            }
        }

        public bool CanStartConvert => !IsConverting && ConvertItems.Any(i => i.IsSelected);

        #endregion

        #region Commands

        public ICommand BrowseSourceCommand { get; private set; } = null!;
        public ICommand BrowseOutputCommand { get; private set; } = null!;
        public ICommand StartConvertCommand { get; private set; } = null!;
        public ICommand ClearListCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseOutputCommand = new RelayCommand(BrowseOutput);
            StartConvertCommand = new RelayCommand(StartConvert, () => CanStartConvert);
            ClearListCommand = new RelayCommand(ClearList);
        }

        #endregion

        #region Methods

        private void BrowseSource()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 JSON 文件",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Multiselect = true,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if (!SourceFiles.Contains(fileName))
                    {
                        SourceFiles.Add(fileName);
                        ConvertItems.Add(new ConvertItemViewModel { SourcePath = fileName });
                    }
                }
                OnPropertyChanged(nameof(CanStartConvert));
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
                OutputDirectory = dialog.FolderName;
            }
        }

        private async void StartConvert()
        {
            IsConverting = true;
            ProgressPercentage = 0;

            try
            {
                var selectedFiles = ConvertItems
                    .Where(i => i.IsSelected)
                    .Select(i => i.SourcePath)
                    .ToArray();

                var result = await _converter.ConvertBatchAsync(selectedFiles, OutputDirectory);

                // 更新转换结果
                for (int i = 0; i < result.Details.Count; i++)
                {
                    var detail = result.Details[i];
                    var item = ConvertItems.FirstOrDefault(x => x.SourcePath == detail.SourceFile);
                    if (item != null)
                    {
                        item.IsSuccess = detail.Success;
                        item.OutputPath = detail.OutputFile;
                        item.ErrorMessage = detail.ErrorMessage;
                    }

                    ProgressPercentage = (double)(i + 1) / result.Details.Count * 100;
                }

                ProgressMessage = $"转换完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量转换失败");
                MessageBox.Show($"转换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsConverting = false;
            }
        }

        private void ClearList()
        {
            SourceFiles.Clear();
            ConvertItems.Clear();
            OnPropertyChanged(nameof(CanStartConvert));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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
    }

    /// <summary>
    /// 转换项视图模型
    /// </summary>
    public class ConvertItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private bool _isSuccess;
        private string _outputPath = string.Empty;
        private string _errorMessage = string.Empty;

        public string SourcePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(SourcePath);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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
    }
}
