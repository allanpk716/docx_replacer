using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Configuration;
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

        /// <summary>
        /// 可选的持久化配置读取委托，用于测试时绕过真实文件系统。
        /// 返回 (UpdateUrl, Channel) 元组，null 值表示未配置。
        /// </summary>
        private readonly Func<(string? updateUrl, string? channel)>? _readPersistentConfig;

        public UpdateSettingsViewModel(
            IUpdateService updateService,
            ILogger<UpdateSettingsViewModel> logger,
            IConfiguration configuration,
            Func<(string? updateUrl, string? channel)>? readPersistentConfig = null)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _readPersistentConfig = readPersistentConfig;

            _sourceTypeDisplay = _updateService.UpdateSourceType;

            // 优先从持久化配置文件（update-config.json）读取，fallback 到 IConfiguration。
            // Velopack 更新会覆盖安装目录下的 appsettings.json（URL 重置为空），
            // 但安装目录上一级的 update-config.json 不受影响。
            var (persistedUrl, persistedChannel) = _readPersistentConfig != null
                ? _readPersistentConfig()
                : ReadPersistentConfig();
            var rawUrl = !string.IsNullOrWhiteSpace(persistedUrl)
                ? persistedUrl
                : (configuration?["Update:UpdateUrl"] ?? "");
            _updateUrl = string.IsNullOrWhiteSpace(rawUrl) ? string.Empty : rawUrl.Trim();

            var rawChannel = !string.IsNullOrWhiteSpace(persistedChannel)
                ? persistedChannel
                : (configuration?["Update:Channel"] ?? "");
            _channel = string.IsNullOrWhiteSpace(rawChannel)
                ? _updateService.Channel
                : rawChannel.Trim();

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

        /// <summary>
        /// 从持久化配置文件（%USERPROFILE%\.docx_replacer\update-config.json）读取 UpdateUrl 和 Channel。
        /// 完全独立于 Velopack 安装目录，因此是配置的"真实来源"。
        /// </summary>
        private static (string? updateUrl, string? channel) ReadPersistentConfig()
        {
            try
            {
                var configPath = UpdateService.GetPersistentConfigPath();
                if (!File.Exists(configPath)) return (null, null);

                var json = File.ReadAllText(configPath);
                var node = JsonNode.Parse(json);
                if (node == null) return (null, null);

                return (node["UpdateUrl"]?.GetValue<string>(), node["Channel"]?.GetValue<string>());
            }
            catch
            {
                return (null, null);
            }
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
