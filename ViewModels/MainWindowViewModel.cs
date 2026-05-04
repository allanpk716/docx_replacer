using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel（协调器），持有子 ViewModel 引用，管理窗口级状态和命令分发。
    /// 子 ViewModel：FillViewModel（关键词替换 Tab）、CleanupViewModel（审核清理 Tab）、UpdateStatusViewModel（更新状态）。
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainWindowViewModel> _logger;

        // 私有字段
        private bool _isTopmost = false;

        // 子 ViewModel 引用
        private readonly FillViewModel _fillViewModel;
        private readonly CleanupViewModel _cleanupViewModel;
        private readonly UpdateStatusViewModel _updateStatusViewModel;

        public MainWindowViewModel(
            FillViewModel fillViewModel,
            CleanupViewModel cleanupViewModel,
            UpdateStatusViewModel updateStatusViewModel,
            ILogger<MainWindowViewModel> logger,
            IUpdateService? updateService = null)
        {
            _fillViewModel = fillViewModel ?? throw new ArgumentNullException(nameof(fillViewModel));
            _cleanupViewModel = cleanupViewModel ?? throw new ArgumentNullException(nameof(cleanupViewModel));
            _updateStatusViewModel = updateStatusViewModel ?? throw new ArgumentNullException(nameof(updateStatusViewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();

            // 启动时自动检查更新状态（fire-and-forget，不阻塞 UI）
            _ = _updateStatusViewModel.InitializeAsync();
        }

        #region 属性

        public bool IsTopmost
        {
            get => _isTopmost;
            set => SetProperty(ref _isTopmost, value);
        }

        /// <summary>
        /// 关键词替换子 ViewModel
        /// </summary>
        public FillViewModel FillVM => _fillViewModel;

        /// <summary>
        /// 审核清理子 ViewModel
        /// </summary>
        public CleanupViewModel CleanupVM => _cleanupViewModel;

        /// <summary>
        /// 更新状态管理子 ViewModel
        /// </summary>
        public UpdateStatusViewModel UpdateStatusVM => _updateStatusViewModel;

        /// <summary>
        /// 代理属性：IsProcessing 来自 FillViewModel，供 MainWindow.xaml.cs 关闭确认使用
        /// </summary>
        public bool IsProcessing => _fillViewModel.IsProcessing;

        /// <summary>
        /// 代理属性：CancelProcessCommand 来自 FillViewModel，供 MainWindow.xaml.cs 关闭确认使用
        /// </summary>
        public ICommand? CancelProcessCommand => _fillViewModel.CancelProcessCommand;

        #endregion

        #region 命令

        public ICommand ExitCommand { get; private set; } = null!;
        public ICommand OpenCleanupCommand { get; private set; } = null!;
        public ICommand ToggleTopmostCommand { get; private set; } = null!;

        #endregion

        #region 私有方法

        private void InitializeCommands()
        {
            ExitCommand = new RelayCommand(ExitApplication);
            OpenCleanupCommand = new RelayCommand(OpenCleanup);
            ToggleTopmostCommand = new RelayCommand(ToggleTopmost);
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
    }

    /// <summary>
    /// 数据文件类型
    /// </summary>
    public enum DataFileType
    {
        Excel
    }
}
