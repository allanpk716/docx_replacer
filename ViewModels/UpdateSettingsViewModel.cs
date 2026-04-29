using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 更新源设置 ViewModel，用于编辑 UpdateUrl 和 Channel
    /// </summary>
    public class UpdateSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IUpdateService _updateService;
        private readonly ILogger<UpdateSettingsViewModel> _logger;

        private string _updateUrl = string.Empty;
        private string _channel = "stable";
        private string _sourceTypeDisplay = string.Empty;

        /// <summary>
        /// 窗体关闭回调，由 code-behind 注入，参数为 DialogResult
        /// </summary>
        internal Action<bool?>? CloseCallback { get; set; }

        public UpdateSettingsViewModel(IUpdateService updateService, ILogger<UpdateSettingsViewModel> logger)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 从 IUpdateService 读取当前值
            _sourceTypeDisplay = _updateService.UpdateSourceType;
            _channel = _updateService.Channel;

            // EffectiveUpdateUrl 包含通道路径后缀（如 "http://host/stable/"），
            // 需要剥离尾部的 "/{channel}/" 以恢复用户输入的原始 URL
            if (_updateService.UpdateSourceType == "GitHub")
            {
                _updateUrl = string.Empty;
            }
            else
            {
                var effectiveUrl = _updateService.EffectiveUpdateUrl;
                var suffix = "/" + _channel + "/";
                if (!string.IsNullOrEmpty(effectiveUrl) && effectiveUrl.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    _updateUrl = effectiveUrl.Substring(0, effectiveUrl.Length - suffix.Length);
                }
                else
                {
                    _updateUrl = effectiveUrl;
                }
            }

            Channels = new ObservableCollection<string> { "stable", "beta" };
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        /// <summary>用户可编辑的更新源 URL，留空表示使用 GitHub Releases</summary>
        public string UpdateUrl
        {
            get => _updateUrl;
            set => SetProperty(ref _updateUrl, value);
        }

        /// <summary>当前选中的更新通道</summary>
        public string Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>当前源类型（只读显示）</summary>
        public string SourceTypeDisplay
        {
            get => _sourceTypeDisplay;
        }

        /// <summary>通道选项列表</summary>
        public ObservableCollection<string> Channels { get; }

        /// <summary>保存命令</summary>
        public ICommand SaveCommand { get; }

        /// <summary>取消命令</summary>
        public ICommand CancelCommand { get; }

        private void ExecuteSave()
        {
            try
            {
                _updateService.ReloadSource(_updateUrl, _channel);
                _logger.LogInformation("更新设置已保存：UpdateUrl={UpdateUrl}, Channel={Channel}", _updateUrl, _channel);
                CloseCallback?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存更新设置失败");
                MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            CloseCallback?.Invoke(false);
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
