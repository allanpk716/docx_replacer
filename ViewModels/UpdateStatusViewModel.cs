using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// 更新状态管理 ViewModel，负责状态栏更新提示、版本检查、更新下载弹窗流程和更新源设置。
    /// 使用 CT.Mvvm [ObservableProperty] + [RelayCommand] 模式。
    /// </summary>
    public partial class UpdateStatusViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly IUpdateService? _updateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateStatusViewModel> _logger;

        private CancellationTokenSource? _autoCheckCts;

        // Backing fields — CT.Mvvm [ObservableProperty] generates public properties with PascalCase names
        [ObservableProperty] private bool _isCheckingUpdate;
        [ObservableProperty] private UpdateStatus _currentUpdateStatus = UpdateStatus.None;

        /// <summary>
        /// 创建 UpdateStatusViewModel。
        /// </summary>
        /// <param name="updateService">更新服务（可选，内网环境可能不注册）</param>
        /// <param name="serviceProvider">DI 容器，用于创建下载进度窗口和更新设置窗口</param>
        /// <param name="logger">日志记录器</param>
        public UpdateStatusViewModel(
            IUpdateService? updateService,
            IServiceProvider serviceProvider,
            ILogger<UpdateStatusViewModel> logger)
        {
            _updateService = updateService;
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 启动时调用，延迟 5 秒后异步检查更新状态，初始化状态栏常驻提示。
        /// 延迟确保不阻塞 UI 启动和初始化。
        /// </summary>
        public async Task InitializeAsync()
        {
            _autoCheckCts = new CancellationTokenSource();

            try
            {
                _logger.LogInformation("自动检查更新：等待 5 秒延迟");
                await Task.Delay(5000, _autoCheckCts.Token);
                await InitializeUpdateStatusAsync();
            }
            catch (OperationCanceledException)
            {
                // 用户取消延迟，静默处理
                _logger.LogInformation("自动检查更新延迟已取消");
            }
        }

        #region 派生属性

        /// <summary>
        /// 当前应用程序版本号
        /// </summary>
        public string CurrentVersion => VersionHelper.GetCurrentVersion();

        /// <summary>
        /// 是否可以检查更新（更新源已配置且未在检查中）
        /// </summary>
        public bool CanCheckUpdate => _updateService?.IsUpdateUrlConfigured == true && !IsCheckingUpdate;

        /// <summary>
        /// 状态栏显示的更新状态消息
        /// </summary>
        public string UpdateStatusMessage
        {
            get
            {
                var baseMessage = CurrentUpdateStatus switch
                {
                    UpdateStatus.UpdateAvailable => "有新版本可用，点击更新",
                    UpdateStatus.UpToDate => "当前已是最新版本",
                    UpdateStatus.Checking => "正在检查更新...",
                    UpdateStatus.Error => "检查更新失败",
                    _ => string.Empty
                };

                // 追加源类型标识
                if (_updateService != null && CurrentUpdateStatus != UpdateStatus.None && CurrentUpdateStatus != UpdateStatus.Checking)
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
        public System.Windows.Media.Brush UpdateStatusBrush
        {
            get => CurrentUpdateStatus switch
            {
                UpdateStatus.UpdateAvailable => System.Windows.Media.Brushes.Orange,
                UpdateStatus.UpToDate => System.Windows.Media.Brushes.Green,
                UpdateStatus.Checking => System.Windows.Media.Brushes.Gray,
                UpdateStatus.Error => System.Windows.Media.Brushes.Red,
                _ => System.Windows.Media.Brushes.Gray
            };
        }

        /// <summary>
        /// 是否有可显示的更新状态（用于 UI 可见性绑定）
        /// </summary>
        public bool HasUpdateStatus => CurrentUpdateStatus != UpdateStatus.None;

        /// <summary>
        /// 是否有新版本可用（用于状态栏⚙按钮红色圆点徽章可见性绑定）
        /// </summary>
        public bool HasUpdateAvailable => CurrentUpdateStatus == UpdateStatus.UpdateAvailable;

        #endregion

        #region 属性变更副作用

        /// <summary>
        /// IsCheckingUpdate 变更时通知 CanCheckUpdate 和相关命令重新评估
        /// </summary>
        partial void OnIsCheckingUpdateChanged(bool value)
        {
            OnPropertyChanged(nameof(CanCheckUpdate));
            CheckUpdateCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// CurrentUpdateStatus 变更时通知所有派生属性刷新
        /// </summary>
        partial void OnCurrentUpdateStatusChanged(UpdateStatus value)
        {
            OnPropertyChanged(nameof(HasUpdateStatus));
            OnPropertyChanged(nameof(HasUpdateAvailable));
            OnPropertyChanged(nameof(UpdateStatusMessage));
            OnPropertyChanged(nameof(UpdateStatusBrush));
            UpdateStatusClickCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region 命令

        /// <summary>
        /// 检查更新命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCheckUpdate))]
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

                    var progressWindow = _serviceProvider.GetRequiredService<Views.DownloadProgressWindow>();
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
        /// 点击状态栏更新提示命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasUpdateStatus))]
        private async Task UpdateStatusClickAsync()
        {
            switch (CurrentUpdateStatus)
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
        /// 打开更新源设置窗口命令
        /// </summary>
        [RelayCommand]
        private void OpenUpdateSettings()
        {
            try
            {
                var settingsWindow = _serviceProvider.GetRequiredService<Views.UpdateSettingsWindow>();
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

        #endregion

        #region 私有方法

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

        #endregion
    }
}
