using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocuFiller.Models.Update;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels.Update
{
    /// <summary>
    /// 更新横幅视图模型，用于显示可用更新的通知
    /// </summary>
    public class UpdateBannerViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<UpdateBannerViewModel> _logger;
        private VersionInfo? _versionInfo;
        private bool _isVisible;

        public UpdateBannerViewModel(ILogger<UpdateBannerViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 更新版本信息
        /// </summary>
        public VersionInfo? VersionInfo
        {
            get => _versionInfo;
            set
            {
                if (SetProperty(ref _versionInfo, value))
                {
                    OnPropertyChanged(nameof(DisplayVersion));
                    OnPropertyChanged(nameof(DisplayMessage));
                    OnPropertyChanged(nameof(ShowUpdateButton));
                    InitializeCommands(); // 重新初始化命令
                }
            }
        }

        /// <summary>
        /// 是否显示横幅
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// 显示的版本号
        /// </summary>
        public string DisplayVersion => VersionInfo?.Version ?? string.Empty;

        /// <summary>
        /// 显示的消息
        /// </summary>
        public string DisplayMessage => VersionInfo != null ? $"发现新版本 {VersionInfo.Version}，请点击查看详情" : "发现可用更新";

        /// <summary>
        /// 是否显示更新按钮（当有版本信息时显示）
        /// </summary>
        public bool ShowUpdateButton => VersionInfo != null;

        /// <summary>
        /// 隐藏横幅命令
        /// </summary>
        public System.Windows.Input.ICommand HideCommand { get; private set; } = null!;

        /// <summary>
        /// 查看更新命令
        /// </summary>
        public System.Windows.Input.ICommand ViewUpdateCommand { get; private set; } = null!;

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            HideCommand = new RelayCommand(HideBanner);
            ViewUpdateCommand = new RelayCommand(ViewUpdate);
        }

        /// <summary>
        /// 隐藏横幅
        /// </summary>
        private void HideBanner()
        {
            IsVisible = false;
            _logger.LogInformation("用户隐藏了更新横幅");
        }

        /// <summary>
        /// 查看更新详情
        /// </summary>
        private void ViewUpdate()
        {
            _logger.LogInformation("用户点击了查看更新详情");
            // 这个命令应该由视图通过事件或命令绑定来处理
            // 这里可以触发一个事件或通过依赖注入获取服务
        }

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
}