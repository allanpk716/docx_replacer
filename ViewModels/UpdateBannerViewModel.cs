using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DocuFiller.Models.Update;
using DocuFiller.Services.Update;
using DocuFiller.ViewModels.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 顶部更新通知横幅的 ViewModel
    /// </summary>
    public class UpdateBannerViewModel : ObservableObject
    {
        private readonly IUpdateService _updateService;
        private readonly ILogger<UpdateBannerViewModel> _logger;
        private readonly string _installerPath;

        private bool _isVisible;
        private string _version = string.Empty;
        private string _channelBadge = string.Empty;
        private Brush _channelBackground = Brushes.Transparent;
        private string _summary = string.Empty;
        private bool _showWarning;
        private string _buttonText = "立即更新";
        private bool _isDownloading;
        private double _downloadProgress;
        private string _statusMessage = string.Empty;
        private bool _canInstall;
        private VersionInfo? _currentVersionInfo;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UpdateBannerViewModel(
            IUpdateService updateService,
            ILogger<UpdateBannerViewModel> logger)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 安装程序保存路径
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DocuFiller",
                "Updates");
            _installerPath = Path.Combine(appDataPath, "installer.exe");

            InitializeCommands();
            SubscribeToEvents();
        }

        #region 属性

        /// <summary>
        /// 横幅是否可见
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// 频道徽章文本
        /// </summary>
        public string ChannelBadge
        {
            get => _channelBadge;
            set => SetProperty(ref _channelBadge, value);
        }

        /// <summary>
        /// 频道背景色
        /// </summary>
        public Brush ChannelBackground
        {
            get => _channelBackground;
            set => SetProperty(ref _channelBackground, value);
        }

        /// <summary>
        /// 更新摘要
        /// </summary>
        public string Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        /// <summary>
        /// 是否显示警告
        /// </summary>
        public bool ShowWarning
        {
            get => _showWarning;
            set => SetProperty(ref _showWarning, value);
        }

        /// <summary>
        /// 按钮文本
        /// </summary>
        public string ButtonText
        {
            get => _buttonText;
            set => SetProperty(ref _buttonText, value);
        }

        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        /// <summary>
        /// 下载进度 (0-100)
        /// </summary>
        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否可以安装
        /// </summary>
        public bool CanInstall
        {
            get => _canInstall;
            set => SetProperty(ref _canInstall, value);
        }

        #endregion

        #region 命令

        public ICommand UpdateCommand { get; private set; } = null!;
        public ICommand InstallCommand { get; private set; } = null!;
        public ICommand CloseCommand { get; private set; } = null!;
        public ICommand RemindLaterCommand { get; private set; } = null!;

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示更新通知
        /// </summary>
        /// <param name="versionInfo">版本信息</param>
        public void ShowUpdate(VersionInfo versionInfo)
        {
            if (versionInfo == null)
            {
                _logger.LogWarning("ShowUpdate: versionInfo 为 null");
                return;
            }

            _logger.LogInformation("显示更新横幅: {Version} ({Channel})", versionInfo.Version, versionInfo.Channel);
            _currentVersionInfo = versionInfo;

            Version = versionInfo.Version;
            Summary = CreateSummary(versionInfo);

            // 根据频道设置样式和行为
            ApplyChannelStyling(versionInfo.Channel);

            IsVisible = true;
        }

        /// <summary>
        /// 隐藏更新通知
        /// </summary>
        public void Hide()
        {
            _logger.LogInformation("隐藏更新横幅");
            IsVisible = false;
            _currentVersionInfo = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            UpdateCommand = new RelayCommand(OnUpdate, () => !IsDownloading);
            InstallCommand = new RelayCommand(OnInstall, () => CanInstall);
            CloseCommand = new RelayCommand(OnClose);
            RemindLaterCommand = new RelayCommand(OnRemindLater);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            // 订阅更新可用事件
            _updateService.UpdateAvailable += OnUpdateAvailable;
        }

        /// <summary>
        /// 更新可用事件处理
        /// </summary>
        private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowUpdate(e.Version);
            });
        }

        /// <summary>
        /// 应用频道特定的样式
        /// </summary>
        private void ApplyChannelStyling(string channel)
        {
            switch (channel.ToLowerInvariant())
            {
                case "stable":
                    ChannelBadge = "稳定版";
                    ChannelBackground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
                    ButtonText = "立即更新";
                    ShowWarning = false;
                    break;

                case "beta":
                    ChannelBadge = "测试版";
                    ChannelBackground = new SolidColorBrush(Color.FromRgb(234, 179, 8)); // Yellow
                    ButtonText = "查看详情";
                    ShowWarning = false;
                    break;

                case "alpha":
                    ChannelBadge = "预览版";
                    ChannelBackground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    ButtonText = "查看详情";
                    ShowWarning = true;
                    break;

                default:
                    ChannelBadge = channel.ToUpperInvariant();
                    ChannelBackground = new SolidColorBrush(Colors.Gray);
                    ButtonText = "立即更新";
                    ShowWarning = false;
                    break;
            }
        }

        /// <summary>
        /// 创建更新摘要文本
        /// </summary>
        private string CreateSummary(VersionInfo versionInfo)
        {
            var lines = new List<string>
            {
                $"发现新版本 {versionInfo.Version}",
                $"发布于 {versionInfo.PublishDate:yyyy-MM-dd}"
            };

            if (!string.IsNullOrEmpty(versionInfo.ReleaseNotes))
            {
                // 取第一行或前100个字符
                var firstLine = versionInfo.ReleaseNotes.Split('\n')[0].Trim();
                lines.Add(firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine);
            }

            return string.Join(" • ", lines);
        }

        /// <summary>
        /// 更新按钮点击
        /// </summary>
        private async void OnUpdate()
        {
            if (_currentVersionInfo == null)
            {
                _logger.LogWarning("OnUpdate: 当前没有版本信息");
                return;
            }

            _logger.LogInformation("用户点击更新按钮: {Version}", _currentVersionInfo.Version);

            // 对于 Beta/Alpha，显示详情而不是直接下载
            if (_currentVersionInfo.Channel.Equals("beta", StringComparison.OrdinalIgnoreCase) ||
                _currentVersionInfo.Channel.Equals("alpha", StringComparison.OrdinalIgnoreCase))
            {
                ShowDetailsWindow();
                return;
            }

            // 对于 Stable，直接开始下载
            await StartDownloadAsync();
        }

        /// <summary>
        /// 开始下载更新
        /// </summary>
        private async System.Threading.Tasks.Task StartDownloadAsync()
        {
            if (_currentVersionInfo == null)
            {
                _logger.LogWarning("StartDownload: 当前没有版本信息");
                return;
            }

            try
            {
                IsDownloading = true;
                DownloadProgress = 0;
                StatusMessage = "正在准备下载...";
                CanInstall = false;

                _logger.LogInformation("开始下载更新: {Version}", _currentVersionInfo.Version);

                // 确保目录存在
                var directory = Path.GetDirectoryName(_installerPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 创建进度报告器
                var progress = new Progress<DownloadProgress>(p =>
                {
                    DownloadProgress = p.ProgressPercentage;
                    StatusMessage = p.Status;
                });

                var downloadedPath = await _updateService.DownloadUpdateAsync(
                    _currentVersionInfo,
                    progress);

                // 下载完成后移动文件到目标位置
                if (File.Exists(downloadedPath))
                {
                    // 确保目录存在
                    var directory = Path.GetDirectoryName(_installerPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // 如果目标文件已存在，先删除
                    if (File.Exists(_installerPath))
                    {
                        File.Delete(_installerPath);
                    }

                    // 移动文件
                    File.Move(downloadedPath, _installerPath);
                }

                _logger.LogInformation("下载完成: {InstallerPath}", _installerPath);

                // 下载完成，更新状态
                IsDownloading = false;
                CanInstall = true;
                ButtonText = "安装更新";
                StatusMessage = "下载完成，准备安装";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载更新失败");
                StatusMessage = "下载失败";
                IsDownloading = false;

                MessageBox.Show(
                    $"下载更新失败：{ex.Message}",
                    "下载失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 安装按钮点击
        /// </summary>
        private void OnInstall()
        {
            if (_currentVersionInfo == null)
            {
                _logger.LogWarning("OnInstall: 当前没有版本信息");
                return;
            }

            _logger.LogInformation("用户点击安装按钮: {Version}", _currentVersionInfo.Version);

            // 对于 Beta/Alpha，显示警告对话框
            if (_currentVersionInfo.Channel.Equals("beta", StringComparison.OrdinalIgnoreCase) ||
                _currentVersionInfo.Channel.Equals("alpha", StringComparison.OrdinalIgnoreCase))
            {
                var result = MessageBox.Show(
                    $"您即将安装 {_currentVersionInfo.Channel.ToUpperInvariant()} 版本。\n\n" +
                    $"此版本可能包含未完成的功能和已知问题。\n\n" +
                    "是否继续安装？",
                    "警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // 启动安装程序
            StartInstaller();
        }

        /// <summary>
        /// 启动安装程序
        /// </summary>
        private void StartInstaller()
        {
            try
            {
                if (!File.Exists(_installerPath))
                {
                    _logger.LogError("安装程序不存在: {Path}", _installerPath);
                    MessageBox.Show(
                        "安装程序文件不存在，请重新下载。",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                _logger.LogInformation("启动安装程序: {Path}", _installerPath);

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _installerPath,
                    UseShellExecute = true,
                    Verb = "runas" // 以管理员权限运行
                };

                System.Diagnostics.Process.Start(startInfo);

                // 退出当前应用程序
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动安装程序失败");
                MessageBox.Show(
                    $"启动安装程序失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示详情窗口
        /// </summary>
        private void ShowDetailsWindow()
        {
            _logger.LogInformation("显示更新详情窗口");

            try
            {
                if (_currentVersionInfo == null)
                {
                    _logger.LogWarning("ShowDetailsWindow: 当前没有版本信息");
                    return;
                }

                var app = (App)Application.Current;
                var logger = app.ServiceProvider.GetRequiredService<ILogger<UpdateViewModel>>();
                var updateService = app.ServiceProvider.GetRequiredService<IUpdateService>();

                var updateViewModel = new UpdateViewModel(updateService, _currentVersionInfo, logger);
                var updateWindow = new Views.Update.UpdateWindow(updateViewModel);

                updateWindow.Owner = Application.Current.MainWindow;
                updateWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示详情窗口失败");
                MessageBox.Show(
                    $"打开详情窗口失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void OnClose()
        {
            _logger.LogInformation("用户关闭更新横幅");
            Hide();
        }

        /// <summary>
        /// 稍后提醒按钮点击
        /// </summary>
        private void OnRemindLater()
        {
            _logger.LogInformation("用户选择稍后提醒");

            // 记录提醒时间，避免频繁打扰
            // TODO: 可以将此信息保存到配置文件，下次启动时检查

            Hide();
        }

        #endregion
    }
}
