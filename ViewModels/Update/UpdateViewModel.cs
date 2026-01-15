using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DocuFiller.Models.Update;
using DocuFiller.Services.Update;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels.Update
{
    /// <summary>
    /// 更新窗口 ViewModel
    /// </summary>
    public class UpdateViewModel : INotifyPropertyChanged
    {
        private readonly IUpdateService _updateService;
        private readonly VersionInfo _versionInfo;
        private readonly ILogger<UpdateViewModel> _logger;
        private string _downloadPath = string.Empty;

        private bool _isDownloading;
        private bool _isInstalling;
        private int _downloadProgress;
        private string _statusMessage = "准备下载...";
        private bool _canUpdate;

        /// <summary>
        /// 下载更新命令
        /// </summary>
        public ICommand DownloadCommand { get; }

        /// <summary>
        /// 安装更新命令
        /// </summary>
        public ICommand InstallCommand { get; }

        /// <summary>
        /// 跳过此版本命令
        /// </summary>
        public ICommand SkipCommand { get; }

        /// <summary>
        /// 稍后提醒命令
        /// </summary>
        public ICommand RemindLaterCommand { get; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version => _versionInfo.Version;

        /// <summary>
        /// 发布说明
        /// </summary>
        public string ReleaseNotes => FormatReleaseNotes(_versionInfo.ReleaseNotes);

        /// <summary>
        /// 发布日期
        /// </summary>
        public DateTime PublishDate => _versionInfo.PublishDate;

        /// <summary>
        /// 文件大小（MB）
        /// </summary>
        public long FileSizeMB => _versionInfo.FileSize / (1024 * 1024);

        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    OnPropertyChanged(nameof(CanUpdate));
                    if (DownloadCommand is RelayCommand downloadCmd)
                    {
                        downloadCmd.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        /// <summary>
        /// 是否正在安装
        /// </summary>
        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                if (SetProperty(ref _isInstalling, value))
                {
                    OnPropertyChanged(nameof(CanUpdate));
                    if (InstallCommand is RelayCommand installCmd)
                    {
                        installCmd.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        /// <summary>
        /// 下载进度（0-100）
        /// </summary>
        public int DownloadProgress
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
        /// 是否可以更新（下载或安装）
        /// </summary>
        public bool CanUpdate
        {
            get => _canUpdate;
            set
            {
                if (SetProperty(ref _canUpdate, value))
                {
                    if (InstallCommand is RelayCommand installCmd)
                    {
                        installCmd.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        /// <summary>
        /// 请求关闭窗口的回调
        /// </summary>
        public Action? RequestClose { get; set; }

        /// <summary>
        /// 初始化 UpdateViewModel 的新实例
        /// </summary>
        /// <param name="updateService">更新服务</param>
        /// <param name="versionInfo">版本信息</param>
        /// <param name="logger">日志记录器</param>
        public UpdateViewModel(
            IUpdateService updateService,
            VersionInfo versionInfo,
            ILogger<UpdateViewModel> logger)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _versionInfo = versionInfo ?? throw new ArgumentNullException(nameof(versionInfo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            DownloadCommand = new RelayCommand(
                async () => await StartDownload(),
                () => !IsDownloading && !IsInstalling);
            InstallCommand = new RelayCommand(
                async () => await StartInstall(),
                () => CanUpdate && !IsDownloading && !IsInstalling);
            SkipCommand = new RelayCommand(SkipVersion);
            RemindLaterCommand = new RelayCommand(RemindLater);

            // 检查是否已下载
            var tempPath = Path.Combine(Path.GetTempPath(), "DocuFiller", "Updates");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            var possibleFiles = Directory.GetFiles(tempPath, $"*{_versionInfo.Version}*.exe");
            if (possibleFiles.Length > 0)
            {
                _downloadPath = possibleFiles[0];
                StatusMessage = "更新已下载完成";
                CanUpdate = true;
                _logger.LogInformation("发现已下载的更新文件: {Path}", _downloadPath);
            }
            else
            {
                StatusMessage = "准备下载更新...";
                CanUpdate = false;
            }
        }

        /// <summary>
        /// 开始下载更新
        /// </summary>
        private async Task StartDownload()
        {
            IsDownloading = true;
            StatusMessage = "正在下载更新...";
            DownloadProgress = 0;

            var progress = new Progress<DownloadProgress>(p =>
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() =>
                    {
                        DownloadProgress = p.ProgressPercentage;
                        StatusMessage = p.Status;
                    });
                }
                else
                {
                    DownloadProgress = p.ProgressPercentage;
                    StatusMessage = p.Status;
                }
            });

            try
            {
                _logger.LogInformation("开始下载更新: Version={Version}", _versionInfo.Version);

                _downloadPath = await _updateService.DownloadUpdateAsync(_versionInfo, progress);

                _logger.LogInformation("更新下载完成: Path={Path}", _downloadPath);
                StatusMessage = "下载完成！";
                CanUpdate = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载更新失败");
                StatusMessage = $"下载失败: {ex.Message}";
                CanUpdate = false;
            }
            finally
            {
                IsDownloading = false;
            }
        }

        /// <summary>
        /// 开始安装更新
        /// </summary>
        private async Task StartInstall()
        {
            if (string.IsNullOrEmpty(_downloadPath) || !File.Exists(_downloadPath))
            {
                StatusMessage = "错误: 未找到下载的更新文件";
                _logger.LogError("安装失败: 更新文件不存在, Path={Path}", _downloadPath);
                return;
            }

            IsInstalling = true;
            StatusMessage = "正在安装更新...";

            try
            {
                _logger.LogInformation("开始安装更新: Path={Path}", _downloadPath);

                var success = await _updateService.InstallUpdateAsync(_downloadPath);

                if (!success)
                {
                    StatusMessage = "安装失败";
                    _logger.LogError("安装失败: InstallUpdateAsync 返回 false");
                }
                // 注意: 成功安装后应用会关闭，不会执行到这里
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安装更新失败");
                StatusMessage = $"安装失败: {ex.Message}";
                IsInstalling = false;
            }
        }

        /// <summary>
        /// 跳过此版本
        /// </summary>
        private void SkipVersion()
        {
            _logger.LogInformation("用户跳过版本: Version={Version}", _versionInfo.Version);
            // TODO: 可以保存跳过的版本到配置，下次不再提示
            RequestClose?.Invoke();
        }

        /// <summary>
        /// 稍后提醒
        /// </summary>
        private void RemindLater()
        {
            _logger.LogInformation("用户选择稍后提醒: Version={Version}", _versionInfo.Version);
            // TODO: 可以记录提醒时间，下次启动时检查
            RequestClose?.Invoke();
        }

        /// <summary>
        /// 格式化发布说明
        /// </summary>
        private string FormatReleaseNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return "暂无更新说明";
            }

            // 如果包含HTML标签，不做处理（假设后续可能有HTML渲染）
            if (notes.Contains("<") && notes.Contains(">"))
            {
                return notes;
            }

            // 将换行符转换为环境换行符
            return notes
                .Replace("\\n", Environment.NewLine)
                .Replace("\\r\\n", Environment.NewLine)
                .Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// 加载版本信息（预留方法）
        /// </summary>
        public Task LoadVersionInfo()
        {
            // 版本信息已在构造时注入，此方法保留用于未来的刷新功能
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow()
        {
            RequestClose?.Invoke();
        }

        #region INotifyPropertyChanged

        /// <summary>
        /// 属性更改事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性更改通知
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置属性值并触发属性更改通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否发生了更改</returns>
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
