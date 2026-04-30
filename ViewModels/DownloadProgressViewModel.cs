using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 下载进度 ViewModel，追踪 Velopack 更新下载的进度、速度和剩余时间。
    /// 通过注入 timestamp provider 和 dispatcher wrapper 实现单元可测试性。
    /// </summary>
    public class DownloadProgressViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly long _totalBytes;
        private readonly string _version;
        private readonly CancellationTokenSource _cts;
        private readonly Func<TimeSpan> _timestampProvider;
        private readonly Action<Action> _dispatcherInvoke;

        // Speed tracking: (percent, timestamp) pairs
        private readonly List<(int percent, TimeSpan timestamp)> _progressHistory = new();
        private double _currentSpeedBytesPerSec;

        // Backing fields for bindable properties
        private int _progressPercent;
        private string _downloadSpeed = string.Empty;
        private string _remainingTime = string.Empty;
        private string _statusText = "准备下载...";
        private bool _isDownloading = true;
        private bool _isCompleted;
        private string? _errorMessage;

        /// <summary>
        /// 窗体关闭回调，由 code-behind 注入，参数为 DialogResult。
        /// 遵循 UpdateSettingsViewModel 相同的模式。
        /// </summary>
        internal Action<bool?>? CloseCallback { get; set; }

        /// <summary>
        /// 创建下载进度 ViewModel。
        /// </summary>
        /// <param name="totalBytes">更新包总字节数（来自 VelopackAsset.Size）</param>
        /// <param name="version">目标版本号</param>
        /// <param name="timestampProvider">
        /// 时间戳提供器，默认使用 DateTime.UtcNow.TimeOfDay。
        /// 单元测试中可替换为可控时间源。
        /// </param>
        /// <param name="dispatcherInvoke">
        /// Dispatcher 调度器，默认使用 Application.Current.Dispatcher.Invoke。
        /// 单元测试中可替换为直接调用。
        /// </param>
        public DownloadProgressViewModel(
            long totalBytes,
            string version,
            Func<TimeSpan>? timestampProvider = null,
            Action<Action>? dispatcherInvoke = null)
        {
            _totalBytes = totalBytes;
            _version = version ?? throw new ArgumentNullException(nameof(version));
            _cts = new CancellationTokenSource();
            _timestampProvider = timestampProvider ?? (() => DateTime.UtcNow.TimeOfDay);
            _dispatcherInvoke = dispatcherInvoke ?? (action =>
            {
                if (Application.Current?.Dispatcher != null)
                    Application.Current.Dispatcher.Invoke(action);
                else
                    action();
            });

            CancelCommand = new RelayCommand(ExecuteCancel, () => IsDownloading);
        }

        /// <summary>下载取消令牌</summary>
        public CancellationToken CancellationToken => _cts.Token;

        /// <summary>当前进度百分比 (0-100)</summary>
        public int ProgressPercent
        {
            get => _progressPercent;
            private set => SetProperty(ref _progressPercent, value);
        }

        /// <summary>下载速度显示文本，例如 "2.5 MB/s"</summary>
        public string DownloadSpeed
        {
            get => _downloadSpeed;
            private set => SetProperty(ref _downloadSpeed, value);
        }

        /// <summary>预估剩余时间显示文本，例如 "约 2 分钟"</summary>
        public string RemainingTime
        {
            get => _remainingTime;
            private set => SetProperty(ref _remainingTime, value);
        }

        /// <summary>状态文本，综合显示进度信息</summary>
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        /// <summary>是否正在下载中</summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            private set
            {
                if (SetProperty(ref _isDownloading, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>下载是否已完成</summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            private set => SetProperty(ref _isCompleted, value);
        }

        /// <summary>错误信息，下载失败或取消时设置</summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>取消下载命令</summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 由 Velopack 进度回调调用（后台线程），线程安全地更新进度状态。
        /// 通过 dispatcher 切换到 UI 线程触发 PropertyChanged。
        /// </summary>
        /// <param name="percent">下载进度百分比 (0-100)</param>
        public void UpdateProgress(int percent)
        {
            percent = Math.Clamp(percent, 0, 100);

            _dispatcherInvoke(() =>
            {
                var now = _timestampProvider();

                // Record progress history point
                _progressHistory.Add((percent, now));

                ProgressPercent = percent;
                CalculateSpeedAndEta(percent, now);
                UpdateStatusText();

                if (percent >= 100)
                {
                    IsDownloading = false;
                    IsCompleted = true;
                }
            });
        }

        /// <summary>
        /// 标记下载完成（正常结束）。
        /// </summary>
        public void MarkCompleted()
        {
            _dispatcherInvoke(() =>
            {
                IsDownloading = false;
                IsCompleted = true;
                ProgressPercent = 100;
                DownloadSpeed = string.Empty;
                RemainingTime = string.Empty;
                StatusText = "下载完成";
            });
        }

        /// <summary>
        /// 标记下载失败。
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void MarkFailed(string errorMessage)
        {
            _dispatcherInvoke(() =>
            {
                IsDownloading = false;
                ErrorMessage = errorMessage;
                StatusText = $"下载失败: {errorMessage}";
            });
        }

        /// <summary>
        /// 标记下载已取消。
        /// </summary>
        public void MarkCancelled()
        {
            _dispatcherInvoke(() =>
            {
                IsDownloading = false;
                ErrorMessage = "下载已取消";
                StatusText = "下载已取消";
            });
        }

        private void ExecuteCancel()
        {
            if (!IsDownloading) return;
            _cts.Cancel();
        }

        /// <summary>
        /// 根据进度百分比计算下载速度和预估剩余时间。
        /// 速度 = bytesDownloaded / elapsedSinceFirstProgress。
        /// 使用累计平均值，比增量更稳定。
        /// </summary>
        private void CalculateSpeedAndEta(int currentPercent, TimeSpan now)
        {
            if (_totalBytes <= 0 || _progressHistory.Count < 2)
            {
                DownloadSpeed = string.Empty;
                RemainingTime = string.Empty;
                return;
            }

            // Use first recorded point as baseline
            var first = _progressHistory[0];
            var elapsedSeconds = (now - first.timestamp).TotalSeconds;

            if (elapsedSeconds > 0 && currentPercent > first.percent)
            {
                var bytesDownloaded = _totalBytes * currentPercent / 100.0;
                _currentSpeedBytesPerSec = bytesDownloaded / elapsedSeconds;
            }

            DownloadSpeed = FormatSpeed(_currentSpeedBytesPerSec);
            RemainingTime = FormatEta(_currentSpeedBytesPerSec, _totalBytes, currentPercent);
        }

        private void UpdateStatusText()
        {
            var parts = new List<string>();
            parts.Add($"下载 {_version}: {ProgressPercent}%");
            if (!string.IsNullOrEmpty(DownloadSpeed))
                parts.Add(DownloadSpeed);
            if (!string.IsNullOrEmpty(RemainingTime))
                parts.Add(RemainingTime);
            StatusText = string.Join(" | ", parts);
        }

        /// <summary>格式化下载速度</summary>
        internal static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec <= 0) return string.Empty;

            if (bytesPerSec >= 1024 * 1024)
                return $"{bytesPerSec / (1024 * 1024):F1} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024:F1} KB/s";
            return $"{bytesPerSec:F0} B/s";
        }

        /// <summary>
        /// 根据总字节数、已下载百分比和速度计算剩余时间字符串。
        /// </summary>
        internal static string FormatEta(double bytesPerSec, long totalBytes, int currentPercent)
        {
            if (bytesPerSec <= 0 || totalBytes <= 0) return string.Empty;

            var remainingBytes = totalBytes * (100 - currentPercent) / 100.0;
            var remainingSeconds = remainingBytes / bytesPerSec;

            if (remainingSeconds < 1) return "即将完成";
            if (remainingSeconds < 60) return $"约 {(int)remainingSeconds} 秒";
            if (remainingSeconds < 3600) return $"约 {(int)(remainingSeconds / 60)} 分钟";
            return $"约 {(int)(remainingSeconds / 3600)} 小时";
        }

        public void Dispose()
        {
            _cts.Dispose();
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
